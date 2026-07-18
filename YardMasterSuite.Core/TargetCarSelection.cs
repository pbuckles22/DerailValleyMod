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
/// Look-at wins over standing so you can inspect yard cars from a car roof.
/// Standing is the fallback when the crosshair is not on a car.
/// </summary>
public static class TargetCarSelection
{
    public static TargetCarSource Resolve(bool hasStandingCar, bool hasLookAtCar)
    {
        if (hasLookAtCar)
        {
            return TargetCarSource.LookAt;
        }

        if (hasStandingCar)
        {
            return TargetCarSource.Standing;
        }

        return TargetCarSource.None;
    }
}
