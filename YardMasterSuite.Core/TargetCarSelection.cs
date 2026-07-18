namespace YardMasterSuite.Core;

/// <summary>
/// Which car feeds the second HUD bar / shared target-car telemetry.
/// </summary>
public enum TargetCarSource
{
    None = 0,
    Standing = 1,
    LookAt = 2,
}

/// <summary>
/// Standing on a car always wins over look-at (CMD-01d).
/// </summary>
public static class TargetCarSelection
{
    public static TargetCarSource Resolve(bool hasStandingCar, bool hasLookAtCar)
    {
        if (hasStandingCar)
        {
            return TargetCarSource.Standing;
        }

        if (hasLookAtCar)
        {
            return TargetCarSource.LookAt;
        }

        return TargetCarSource.None;
    }
}
