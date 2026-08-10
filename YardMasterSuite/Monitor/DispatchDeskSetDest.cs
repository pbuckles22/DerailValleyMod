using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Shared Route Set dest — normal track or synthetic Turntable token.
/// Multi-leg TT uses the same Switch List + Align core as jobs.
/// </summary>
internal static class DispatchDeskSetDest
{
    public const string TurntableToken = "Turntable";

    public static bool IsTurntableToken(string? trackOrToken) =>
        string.Equals(trackOrToken?.Trim(), TurntableToken, System.StringComparison.OrdinalIgnoreCase);

    /// <summary>Resolve UI track selection to a real graph track id (Turntable → FoT pick).</summary>
    public static bool TryResolveTrackId(string yard, string trackOrToken, out string trackId, out string? error)
    {
        trackId = "";
        error = null;
        if (string.IsNullOrWhiteSpace(yard) || string.IsNullOrWhiteSpace(trackOrToken))
        {
            error = "pick city + track";
            return false;
        }

        if (!IsTurntableToken(trackOrToken))
        {
            trackId = trackOrToken.Trim();
            return true;
        }

        if (!TelemetryReader.TryGetPlayerPosition(out var ox, out _, out var oz))
        {
            error = "T2 path: no player position";
            return false;
        }

        var tt = TurntableLocator.TryResolveTrackId(yard.Trim(), ox, oz);
        if (string.IsNullOrWhiteSpace(tt))
        {
            error = "T2 path: no turntable in " + yard;
            return false;
        }

        trackId = tt!;
        return true;
    }

    /// <summary>
    /// Set dest + Compute. For Turntable with NoPath, bind multi-leg Switch List (shared Align/Next).
    /// Does not Align — caller presses Align Route / Align step / Next.
    /// </summary>
    public static string Run(string yard, string trackOrToken)
    {
        if (!TryResolveTrackId(yard, trackOrToken, out var trackId, out var err))
        {
            return err ?? "pick city + track";
        }

        var wantTt = IsTurntableToken(trackOrToken);
        if (!wantTt)
        {
            // New single-track dest must not keep a stale TT/job list (smoke: Align threw 10km).
            SwitchListSession.Clear();
        }

        RouteDestSession.Set(yard, trackId);
        Main.Log(
            "T2 route: dest set city="
            + yard
            + " track="
            + trackId
            + (wantTt ? " kind=Turntable" : " kind=track"));

        var line = RoutePlanService.Compute(wantTt ? "tt" : "set");
        if (wantTt && IsNoPath(line))
        {
            return BindTurntableMultiStep(yard, trackId, line);
        }

        if (wantTt)
        {
            // Direct path OK — optional single-step list for Arrived UX consistency.
            var rev = RouteFacingResolver.IsTargetBehind(RoutePlanSession.Plan);
            var steps = SwitchListPlanner.BuildTownTurntable(
                yard,
                trackId,
                pivotTrackId: null,
                pivotNeedsReverse: false,
                turntableNeedsReverse: rev);
            if (steps != null && steps.Count > 0)
            {
                SwitchListSession.Bind("tt:" + yard, steps);
                // Stay on Route — steps draw there too (no forced Per job tab).
                Main.Log(
                    "T2 switch-list: loaded tt:"
                    + yard
                    + " · 1 step · "
                    + steps[0].Label);
            }
        }

        LogRouteSnapshot("dest", line);
        return line;
    }

    private static string BindTurntableMultiStep(string yard, string ttTrackId, string noPathLine)
    {
        var origin = TelemetryReaderOrigin.TryGet();
        if (origin == null)
        {
            LogRouteSnapshot("dest-nopath", noPathLine);
            return noPathLine;
        }

        var pivot = RoutePlanService.TryFindFirstPivotTrackId(origin, ttTrackId, yard);
        if (string.IsNullOrWhiteSpace(pivot))
        {
            Main.Log("T2 path: TT multi-step no pivot from " + origin + " → " + ttTrackId);
            LogRouteSnapshot("dest-nopath", noPathLine);
            return noPathLine;
        }

        // Probe pivot leg facing using live cab→pin, not topological ReverseCount.
        RouteDestSession.Set(yard, pivot);
        RoutePlanService.Compute("tt-probe");
        var pivotRev = RouteFacingResolver.IsTargetBehind(RoutePlanSession.Plan);

        var steps = SwitchListPlanner.BuildTownTurntable(
            yard,
            ttTrackId,
            pivot,
            pivotNeedsReverse: pivotRev,
            turntableNeedsReverse: false,
            insertFacingBeforeTurntable: false);
        if (steps == null || steps.Count == 0)
        {
            return "T2 path: could not build TT Switch List";
        }

        SwitchListSession.Bind("tt:" + yard, steps);
        // Stay on Route so Set Forward / Pivot list is visible without switching tabs.
        Main.Log(
            "T2 switch-list: loaded tt:"
            + yard
            + " · "
            + steps.Count
            + " steps · TT "
            + ttTrackId
            + " via pivot "
            + pivot);

        var step = SwitchListSession.CurrentStep;
        if (step == null || string.IsNullOrEmpty(step.DestTrackId))
        {
            return "T2 path: no active TT step";
        }

        RouteDestSession.Set(step.DestYardId, step.DestTrackId);
        var line = RoutePlanService.Compute("tt-list");
        LogRouteSnapshot("dest-list", line + " · " + step.Index + "/" + steps.Count + " " + step.Label);
        return line + " · Switch List " + step.Index + "/" + steps.Count + " " + step.Label;
    }

    internal static void LogRouteSnapshot(string phase, string? detail = null)
    {
        var plan = RoutePlanSession.Plan;
        var facing = SwitchListDriveFacing.SetWord(RouteFacingResolver.IsTargetBehind(plan));
        var exit = RouteFacingResolver.TryGetExitCue(plan) ?? RoutePlanSession.ExitCue ?? "—";
        var path = PathCheckDisplay.Format(plan?.ToCheckResult()) ?? "—";
        var step = SwitchListSession.CurrentStep;
        var stepPart = step != null
            ? " step=" + step.Index + " '" + step.Label + "'"
            : "";
        Main.Log(
            "T2 route: "
            + phase
            + " path="
            + path
            + " facing="
            + facing
            + " exit="
            + exit
            + " dest="
            + (RouteDestSession.YardId ?? "—")
            + "/"
            + (RouteDestSession.TrackId ?? "—")
            + stepPart
            + (string.IsNullOrEmpty(detail) ? "" : " · " + detail));
    }

    private static bool IsNoPath(string? line) =>
        !string.IsNullOrEmpty(line)
        && line!.IndexOf("no path", System.StringComparison.OrdinalIgnoreCase) >= 0;
}
