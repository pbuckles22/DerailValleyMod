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

    public static bool HasPlan => _plan != null && !_stale;

    public static bool IsStale => _stale;

    public static PathPlanResult? Plan => _stale ? null : _plan;

    public static string? PlannedOriginTrackId => _plannedOriginTrackId;

    /// <summary>e.g. <c>Exit NE</c> — bring loco to that side of the origin track.</summary>
    public static string? ExitCue => _stale ? null : _exitCue;

    /// <summary>HUD / desk status (e.g. left planned path).</summary>
    public static string? StatusMessage => _statusMessage;

    public static void SetPlan(PathPlanResult plan, string? originTrackId, string? exitCue = null)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _plannedOriginTrackId = string.IsNullOrWhiteSpace(originTrackId)
            ? null
            : originTrackId!.Trim();
        _exitCue = string.IsNullOrWhiteSpace(exitCue) ? null : exitCue!.Trim();
        _stale = false;
        _statusMessage = null;
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
    }

    public static void Clear()
    {
        _plan = null;
        _plannedOriginTrackId = null;
        _exitCue = null;
        _statusMessage = null;
        _stale = false;
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
