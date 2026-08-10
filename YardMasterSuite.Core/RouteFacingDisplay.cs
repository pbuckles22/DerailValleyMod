namespace YardMasterSuite.Core;

/// <summary>Facing / reverse cue for Align Route preview (3.5). Informational only.</summary>
public static class RouteFacingDisplay
{
    /// <summary>
    /// Drive-set chip from live cab→pin polarity. Optional stub count is topological only.
    /// </summary>
    public static string? Format(PathPlanResult? plan, bool isTargetBehind)
    {
        if (plan == null
            || plan.Status == PathCheckStatus.NoDestination
            || plan.Status == PathCheckStatus.NoOrigin
            || plan.Status == PathCheckStatus.NoPath)
        {
            return null;
        }

        var word = SwitchListDriveFacing.SetWord(isTargetBehind);

        // Stub reverse-into hops remain visible (pathfind topology), not drive-set.
        if (plan.ReverseCount > 0)
        {
            return word + " (stub " + plan.ReverseCount + ")";
        }

        return word;
    }
}
