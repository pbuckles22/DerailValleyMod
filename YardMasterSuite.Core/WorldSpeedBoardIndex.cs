using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Persistent track-keyed posted boards (**1.17**). Used to <b>seed</b> Limit from a board
/// already behind the loco (cold start). Not re-injected into Next (that caused opposite-travel
/// / streamed ghosts with no facing check).
/// </summary>
public sealed class WorldSpeedBoardIndex
{
    public readonly struct Pin
    {
        public Pin(
            int trackId,
            float kmh,
            float worldX,
            float worldY,
            float worldZ,
            float travelX,
            float travelZ)
        {
            TrackId = trackId;
            Kmh = kmh;
            WorldX = worldX;
            WorldY = worldY;
            WorldZ = worldZ;
            TravelX = travelX;
            TravelZ = travelZ;
        }

        public int TrackId { get; }
        public float Kmh { get; }
        public float WorldX { get; }
        public float WorldY { get; }
        public float WorldZ { get; }

        /// <summary>Travel direction when the board governed (for same-direction seed only).</summary>
        public float TravelX { get; }
        public float TravelZ { get; }
    }

    private readonly Dictionary<long, Pin> _byKey = new Dictionary<long, Pin>();
    private readonly Dictionary<int, List<long>> _keysByTrack = new Dictionary<int, List<long>>();

    public int Count => _byKey.Count;

    public void Clear()
    {
        _byKey.Clear();
        _keysByTrack.Clear();
    }

    public void Remember(
        int trackId,
        float kmh,
        float worldX,
        float worldY,
        float worldZ,
        float travelX,
        float travelZ)
    {
        if (trackId == 0 || !IsFinite(kmh) || kmh <= 0f)
        {
            return;
        }

        if (!IsFinite(worldX) || !IsFinite(worldY) || !IsFinite(worldZ))
        {
            return;
        }

        if (!TryNormalize(travelX, travelZ, out var tx, out var tz))
        {
            return;
        }

        var key = MakeKey(trackId, kmh, worldX, worldY, worldZ);
        var pin = new Pin(trackId, kmh, worldX, worldY, worldZ, tx, tz);
        if (_byKey.ContainsKey(key))
        {
            _byKey[key] = pin;
            return;
        }

        _byKey[key] = pin;
        if (!_keysByTrack.TryGetValue(trackId, out var list))
        {
            list = new List<long>(4);
            _keysByTrack[trackId] = list;
        }

        list.Add(key);
    }

    public IReadOnlyList<Pin> ForTrack(int trackId)
    {
        if (!_keysByTrack.TryGetValue(trackId, out var keys) || keys.Count == 0)
        {
            return Array.Empty<Pin>();
        }

        var result = new List<Pin>(keys.Count);
        for (var i = 0; i < keys.Count; i++)
        {
            if (_byKey.TryGetValue(keys[i], out var pin))
            {
                result.Add(pin);
            }
        }

        return result;
    }

    /// <summary>True when pin was remembered while traveling roughly the same way.</summary>
    public static bool SameTravel(Pin pin, float travelX, float travelZ)
    {
        if (!TryNormalize(travelX, travelZ, out var tx, out var tz))
        {
            return false;
        }

        return (pin.TravelX * tx) + (pin.TravelZ * tz) >= 0.5f;
    }

    public static long MakeKey(int trackId, float kmh, float worldX, float worldY, float worldZ)
    {
        var whole = (int)Math.Round(kmh, MidpointRounding.AwayFromZero);
        var cx = (int)Math.Floor(worldX / 25f);
        var cy = (int)Math.Floor(worldY / 25f);
        var cz = (int)Math.Floor(worldZ / 25f);
        unchecked
        {
            long h = trackId;
            h = (h * 397) ^ whole;
            h = (h * 397) ^ cx;
            h = (h * 397) ^ cy;
            h = (h * 397) ^ cz;
            return h;
        }
    }

    private static bool TryNormalize(float x, float z, out float nx, out float nz)
    {
        var len = (float)Math.Sqrt((x * x) + (z * z));
        if (len < 1e-4f)
        {
            nx = nz = 0f;
            return false;
        }

        nx = x / len;
        nz = z / len;
        return true;
    }

    private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
}
