namespace YardMasterSuite.Core;

/// <summary>Facing / reverse cue for Align Route preview (3.5). Informational only.</summary>
public static class RouteFacingDisplay
{
    public static string? Format(PathPlanResult? plan)
    {
        if (plan == null
            || plan.Status == PathCheckStatus.NoDestination
            || plan.Status == PathCheckStatus.NoOrigin
            || plan.Status == PathCheckStatus.NoPath)
        {
            return null;
        }

        if (plan.ReverseCount <= 0)
        {
            return "Facing OK";
        }

        if (plan.LastHopRequiresReverse && plan.ReverseCount == 1)
        {
            return "Reverse into dest";
        }

        return $"{plan.ReverseCount} reverses";
    }
}
