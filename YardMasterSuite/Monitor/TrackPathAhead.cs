using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// The route the consist will actually travel: walk forward from the loco's track through each
/// junction as it is currently thrown (<c>selectedBranch</c>) and record how far along the path
/// every track starts (**1.16**).
/// <para>
/// Replaces "same <see cref="RailTrack"/> as the loco" plus a straight-line dot product, which both
/// admitted boards that were never on our route (log: <c>'7 4'=40 along=1398m lat=-777m track=y</c>)
/// and hid real boards sitting just past the next switch on a different track object.
/// </para>
/// <para>
/// Distances use Bezier <c>span</c> (arc), not chord from the track entry. Facing travel is the
/// route tangent at the board — loco heading on a curve rejects governing boards
/// (<c>fDot=-0.39</c> at ~12 m → Recommended only at 0.6 m).
/// </para>
/// </summary>
internal static class TrackPathAhead
{
    /// <summary>
    /// Stop walking after this many tracks regardless of distance. The 0.5.59 severe-drop scan can
    /// reach 6 km; 48 short yard/mainline segments truncated that route before its warning horizon.
    /// </summary>
    public const int MaxHops = 128;

    internal readonly struct Segment
    {
        public Segment(
            float entryDistanceMeters,
            Vector3 entryPosition,
            float lengthMeters,
            bool travelIncreasingSpan,
            Vector3 travelHint,
            RailTrack track)
        {
            EntryDistanceMeters = entryDistanceMeters;
            EntryPosition = entryPosition;
            LengthMeters = lengthMeters;
            TravelIncreasingSpan = travelIncreasingSpan;
            TravelHint = travelHint;
            Track = track;
        }

        /// <summary>Path distance from the loco to where this track begins (negative for our own).</summary>
        public float EntryDistanceMeters { get; }

        /// <summary>World position where the path enters this track.</summary>
        public Vector3 EntryPosition { get; }

        public float LengthMeters { get; }

        /// <summary>
        /// True when we walk in→out (span grows along travel). False when we walk out→in.
        /// </summary>
        public bool TravelIncreasingSpan { get; }

        /// <summary>Flat entry→exit direction for this hop (chord fallback / missing tangent).</summary>
        public Vector3 TravelHint { get; }

        /// <summary>
        /// The track this segment covers. Its <c>curve</c> is loaded for the whole route (unlike
        /// posted board sign props, which stream in only near the train) — this is what lets the
        /// geometry-ahead scan see a tight curve long before its board exists (A116 deep-dive).
        /// </summary>
        public RailTrack Track { get; }
    }

    /// <summary>
    /// Fills <paramref name="into"/> with track instance id → segment for the route ahead.
    /// Returns false when topology is unavailable (caller keeps its legacy corridor behaviour).
    /// </summary>
    public static bool TryBuild(
        RailTrack? start,
        Vector3 locoPosition,
        Vector3 travelForward,
        float maxDistanceMeters,
        Dictionary<int, Segment> into)
    {
        into.Clear();
        if (start == null)
        {
            return false;
        }

        try
        {
            if (!TryEndpoints(start, out var startIn, out var startOut, out var startLength))
            {
                return false;
            }

            // Which end are we heading for? Compare travel with the vector to each endpoint.
            var flat = new Vector3(travelForward.x, 0f, travelForward.z);
            var towardOut = Vector3.Dot(flat, Flat(startOut - locoPosition));
            var towardIn = Vector3.Dot(flat, Flat(startIn - locoPosition));
            var forward = towardOut >= towardIn;

            var track = start;
            var entryPosition = forward ? startIn : startOut;
            var length = startLength;
            // Arc already covered on our own track (negative entry). Chord underestimates on curves.
            var entryDistance = -StartTrackCoveredMeters(start, locoPosition, length, travelIncreasingSpan: forward);

            for (var hop = 0; hop < MaxHops; hop++)
            {
                var id = track.GetInstanceID();
                if (into.ContainsKey(id))
                {
                    break;
                }

                var exitPosition = ExitPositionOf(track, entryPosition);
                var hint = Flat(exitPosition - entryPosition);
                if (hint.sqrMagnitude > 1e-8f)
                {
                    hint.Normalize();
                }
                else
                {
                    hint = Flat(flat).sqrMagnitude > 1e-8f ? Flat(flat).normalized : Vector3.forward;
                }

                // forward=true → heading for out → span increases along travel.
                into[id] = new Segment(
                    entryDistance,
                    entryPosition,
                    length,
                    travelIncreasingSpan: forward,
                    travelHint: hint,
                    track: track);

                var exitDistance = entryDistance + length;
                if (exitDistance >= maxDistanceMeters)
                {
                    break;
                }

                var next = NextTrack(track, forward);
                if (next == null || !TryEndpoints(next, out var nextIn, out var nextOut, out var nextLength))
                {
                    break;
                }

                // We enter the next track at whichever of its ends meets our exit.
                var enterAtIn = (nextIn - exitPosition).sqrMagnitude <= (nextOut - exitPosition).sqrMagnitude;
                forward = enterAtIn;
                entryPosition = enterAtIn ? nextIn : nextOut;
                entryDistance = exitDistance;
                length = nextLength;
                track = next;
            }

            return into.Count > 0;
        }
        catch
        {
            into.Clear();
            return false;
        }
    }

    /// <summary>
    /// Path distance from the loco to a board attached to <paramref name="boardTrack"/>.
    /// Negative means behind us. False when the board's track is not on our route.
    /// </summary>
    public static bool TryDistance(
        Dictionary<int, Segment> path,
        RailTrack? boardTrack,
        Vector3 boardPosition,
        out float distanceMeters) =>
        TrySample(path, boardTrack, boardPosition, out distanceMeters, out _);

    /// <summary>
    /// Arc distance along the route plus the route travel direction at the board (flat XZ).
    /// </summary>
    public static bool TrySample(
        Dictionary<int, Segment> path,
        RailTrack? boardTrack,
        Vector3 boardPosition,
        out float distanceMeters,
        out Vector3 travelForward)
    {
        distanceMeters = 0f;
        travelForward = Vector3.zero;
        if (boardTrack == null || !path.TryGetValue(boardTrack.GetInstanceID(), out var segment))
        {
            return false;
        }

        if (!TryClosestOnTrack(boardTrack, boardPosition, out var spanMeters, out var tangent))
        {
            var withinTrack = Vector3.Distance(segment.EntryPosition, boardPosition);
            if (segment.LengthMeters > 0f && withinTrack > segment.LengthMeters)
            {
                withinTrack = segment.LengthMeters;
            }

            distanceMeters = segment.EntryDistanceMeters + withinTrack;
            travelForward = segment.TravelHint;
            return true;
        }

        var alongTrack = TrackPathSpan.WithinTrackMeters(
            spanMeters,
            segment.LengthMeters,
            segment.TravelIncreasingSpan);
        distanceMeters = segment.EntryDistanceMeters + alongTrack;
        travelForward = segment.TravelIncreasingSpan ? tangent : -tangent;
        travelForward.y = 0f;
        if (travelForward.sqrMagnitude < 1e-8f)
        {
            travelForward = segment.TravelHint;
        }

        return true;
    }

    private static float StartTrackCoveredMeters(
        RailTrack track,
        Vector3 locoPosition,
        float lengthMeters,
        bool travelIncreasingSpan)
    {
        if (TryClosestOnTrack(track, locoPosition, out var span, out _))
        {
            return TrackPathSpan.WithinTrackMeters(span, lengthMeters, travelIncreasingSpan);
        }

        if (!TryEndpoints(track, out var inPos, out var outPos, out _))
        {
            return 0f;
        }

        var entry = travelIncreasingSpan ? inPos : outPos;
        var covered = Vector3.Distance(entry, locoPosition);
        return lengthMeters > 0f && covered > lengthMeters ? lengthMeters : covered;
    }

    private static bool TryClosestOnTrack(
        RailTrack track,
        Vector3 worldPosition,
        out float spanMeters,
        out Vector3 tangent)
    {
        spanMeters = 0f;
        tangent = Vector3.zero;
        try
        {
            var closest = RailTrack.GetClosestPoint(track, worldPosition, 0f);
            if (closest.Item1 is not { } point)
            {
                return false;
            }

            spanMeters = (float)point.span;
            tangent = point.forward;
            tangent.y = 0f;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static RailTrack? NextTrack(RailTrack track, bool forward)
    {
        var junction = forward ? track.outJunction : track.inJunction;
        if (junction != null)
        {
            var stem = junction.inBranch?.track;
            if (ReferenceEquals(stem, track))
            {
                var branches = junction.outBranches;
                if (branches == null || branches.Count == 0)
                {
                    return null;
                }

                var index = junction.selectedBranch;
                if (index < 0 || index >= branches.Count)
                {
                    index = 0;
                }

                return branches[index]?.track;
            }

            // Coming off a diverging leg back onto the stem.
            return stem;
        }

        if (forward)
        {
            return track.outIsConnected ? track.outBranch?.track : null;
        }

        return track.inIsConnected ? track.inBranch?.track : null;
    }

    private static Vector3 ExitPositionOf(RailTrack track, Vector3 entryPosition)
    {
        if (!TryEndpoints(track, out var inPos, out var outPos, out _))
        {
            return entryPosition;
        }

        return (inPos - entryPosition).sqrMagnitude <= (outPos - entryPosition).sqrMagnitude
            ? outPos
            : inPos;
    }

    private static bool TryEndpoints(
        RailTrack track,
        out Vector3 inPosition,
        out Vector3 outPosition,
        out float lengthMeters)
    {
        inPosition = Vector3.zero;
        outPosition = Vector3.zero;
        lengthMeters = 0f;

        var curve = track.curve;
        if (curve == null || curve.pointCount < 2)
        {
            return false;
        }

        var first = curve[0];
        var last = curve[curve.pointCount - 1];
        if (first == null || last == null)
        {
            return false;
        }

        inPosition = first.position;
        outPosition = last.position;
        lengthMeters = curve.length;
        if (lengthMeters <= 0f)
        {
            lengthMeters = Vector3.Distance(inPosition, outPosition);
        }

        return lengthMeters > 0f;
    }

    private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
}
