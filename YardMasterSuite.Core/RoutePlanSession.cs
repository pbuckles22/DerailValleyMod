using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Frozen route plan for Align Route (3.5). Computed only on explicit user actions
/// (Set dest / Recheck / Align) — never on the HUD tick.
/// </summary>
public static class RoutePlanSession
{
    private static PathPlanResult? _plan;
    private static string? _plannedOriginTrackId;
    private static string? _exitCue;
    private static string? _statusMessage;
    private static bool _stale;
    private static float? _remainingCostSeconds;
    private static float? _plannedTravelSeconds;
    private static float? _remainingMeters;
    private static float? _plannedMeters;
    private static float? _driveMetersAtPlan;
    private static float? _tripProgress01;
    private static float? _hopProgress01;
    private static string? _etaMode;
    private static Dictionary<string, int>? _junctionSnapshot;

    public static bool HasPlan => _plan != null && !_stale;

    public static bool IsStale => _stale;

    public static PathPlanResult? Plan => _stale ? null : _plan;

    public static string? PlannedOriginTrackId => _plannedOriginTrackId;

    /// <summary>e.g. <c>Exit NE</c> — bring loco to that side of the origin track.</summary>
    public static string? ExitCue => _stale ? null : _exitCue;

    /// <summary>HUD / desk status (e.g. left planned path).</summary>
    public static string? StatusMessage => _statusMessage;

    /// <summary>
    /// Remaining seconds for ETA chip (plan-scaled from Drive). ~1 s refresh.
    /// </summary>
    public static float? RemainingCostSeconds => _stale ? null : _remainingCostSeconds;

    /// <summary>
    /// Physical corridor travel time (track length / track speed + switch traversal),
    /// excluding Dijkstra-only lane preference penalties.
    /// </summary>
    public static float? PlannedTravelSeconds => _stale ? null : _plannedTravelSeconds;

    public static float? RemainingMeters => _stale ? null : _remainingMeters;

    public static float? PlannedMeters => _stale ? null : _plannedMeters;

    /// <summary>Session Drive meters when the plan was set (odometer baseline).</summary>
    public static float? DriveMetersAtPlan => _stale ? null : _driveMetersAtPlan;

    /// <summary>
    /// Overall corridor progress 0..1 (HUD <c>trip N%</c>) from original vs remaining travel ETA.
    /// </summary>
    public static float? TripProgress01 => _stale ? null : _tripProgress01;

    /// <summary>Progress along current hop only (logs / debug).</summary>
    public static float? HopProgress01 => _stale ? null : _hopProgress01;

    /// <summary><c>lag</c> = plan + schedule lag (clamped); <c>plan</c> = class/geometry only; legacy <c>pace</c>.</summary>
    public static string? EtaMode => _stale ? null : _etaMode;

    /// <summary>Plan-junction selectedBranch at freeze time (stale fallback: no throws ⇒ still Path OK).</summary>
    public static IReadOnlyDictionary<string, int>? JunctionSnapshot =>
        _stale ? null : _junctionSnapshot;

    /// <summary>Cost used for ETA chip: remaining if known, else full plan cost.</summary>
    public static float? EtaCostSeconds
    {
        get
        {
            if (_stale || _plan == null)
            {
                return null;
            }

            return _remainingCostSeconds ?? _plannedTravelSeconds ?? _plan.TotalCost;
        }
    }

    public static void SetPlan(
        PathPlanResult plan,
        string? originTrackId,
        string? exitCue = null,
        float? travelEtaSeconds = null)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _plannedOriginTrackId = string.IsNullOrWhiteSpace(originTrackId)
            ? null
            : originTrackId!.Trim();
        _exitCue = string.IsNullOrWhiteSpace(exitCue) ? null : exitCue!.Trim();
        _stale = false;
        _statusMessage = null;
        _plannedTravelSeconds = travelEtaSeconds is float eta && eta > 0f
            ? eta
            : plan.TotalCost;
        _remainingCostSeconds = _plannedTravelSeconds;
        _remainingMeters = null;
        _plannedMeters = null;
        _driveMetersAtPlan = null;
        _tripProgress01 = 0f;
        _hopProgress01 = 0f;
        _etaMode = "plan";
        _junctionSnapshot = null;
    }

    /// <summary>Refresh Exit compass without clearing the frozen plan (live loco→pin).</summary>
    public static void SetExitCue(string? exitCue)
    {
        if (_plan == null || _stale)
        {
            return;
        }

        _exitCue = string.IsNullOrWhiteSpace(exitCue) ? null : exitCue!.Trim();
    }

    /// <summary>Record corridor junction branches after Set dest / Align (for no-throw Path OK).</summary>
    public static void SetJunctionSnapshot(IReadOnlyDictionary<string, int>? snapshot)
    {
        if (_plan == null || _stale)
        {
            return;
        }

        _junctionSnapshot = new Dictionary<string, int>(StringComparer.Ordinal);
        if (snapshot == null)
        {
            return;
        }

        foreach (var kv in snapshot)
        {
            _junctionSnapshot[kv.Key] = kv.Value;
        }
    }

    public static void SetDriveBaseline(float sessionDriveMeters)
    {
        if (_plan == null || _stale)
        {
            return;
        }

        _driveMetersAtPlan = sessionDriveMeters < 0f ? 0f : sessionDriveMeters;
    }

    public static void SetRemainingEta(
        float seconds,
        float? remainingMeters,
        float? plannedMeters,
        float tripProgress01,
        float hopProgress01,
        string etaMode)
    {
        if (_plan == null || _stale)
        {
            return;
        }

        _remainingCostSeconds = seconds < 0f ? 0f : seconds;
        _remainingMeters = remainingMeters is float m && m >= 0f ? m : null;
        if (plannedMeters is float pm && pm > 0f)
        {
            _plannedMeters = pm;
        }

        _tripProgress01 = tripProgress01 < 0f ? 0f : (tripProgress01 > 1f ? 1f : tripProgress01);
        _hopProgress01 = hopProgress01 < 0f ? 0f : (hopProgress01 > 1f ? 1f : hopProgress01);
        _etaMode = string.IsNullOrWhiteSpace(etaMode) ? "plan" : etaMode.Trim();
    }

    /// <summary>Player left the planned corridor — clear live chips, keep dest for Recheck.</summary>
    public static void MarkStale(string message)
    {
        if (_plan == null)
        {
            return;
        }

        _stale = true;
        _statusMessage = string.IsNullOrWhiteSpace(message) ? "path stale" : message!.Trim();
        _remainingCostSeconds = null;
        _plannedTravelSeconds = null;
        _remainingMeters = null;
        _plannedMeters = null;
        _driveMetersAtPlan = null;
        _tripProgress01 = null;
        _hopProgress01 = null;
        _etaMode = null;
        _junctionSnapshot = null;
    }

    public static void Clear()
    {
        _plan = null;
        _plannedOriginTrackId = null;
        _exitCue = null;
        _statusMessage = null;
        _stale = false;
        _remainingCostSeconds = null;
        _plannedTravelSeconds = null;
        _remainingMeters = null;
        _plannedMeters = null;
        _driveMetersAtPlan = null;
        _tripProgress01 = null;
        _hopProgress01 = null;
        _etaMode = null;
        _junctionSnapshot = null;
    }
}

/// <summary>
/// Session memo of computed routes (origin track → dest track). Avoids re-Dijkstra
/// when Check/Align repeats the same pair. Cleared with destination clear.
/// </summary>
public static class RouteMemo
{
    private static readonly Dictionary<string, PathPlanResult> Cache =
        new(StringComparer.Ordinal);

    public static bool TryGet(string? origin, string? dest, out PathPlanResult? plan)
    {
        plan = null;
        var key = Key(origin, dest);
        if (key == null)
        {
            return false;
        }

        if (!Cache.TryGetValue(key, out var hit))
        {
            return false;
        }

        plan = hit;
        return true;
    }

    public static void Put(string? origin, string? dest, PathPlanResult plan)
    {
        var key = Key(origin, dest);
        if (key == null || plan == null)
        {
            return;
        }

        Cache[key] = plan;
    }

    public static void Clear() => Cache.Clear();

    private static string? Key(string? origin, string? dest)
    {
        var o = origin?.Trim();
        var d = dest?.Trim();
        if (string.IsNullOrEmpty(o) || string.IsNullOrEmpty(d))
        {
            return null;
        }

        return o + ">" + d;
    }
}
