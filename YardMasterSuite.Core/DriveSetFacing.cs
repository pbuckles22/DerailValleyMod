namespace YardMasterSuite.Core;

/// <summary>
/// Pure math for drive-set semantics (Forward vs Reverse gear toward a world target).
/// </summary>
public static class DriveSetFacing
{
    /// <summary>
    /// True when the target offset (dx, dz) is behind the loco forward XZ (dot &lt; 0).
    /// </summary>
    public static bool IsTargetBehind(float fwdX, float fwdZ, float dx, float dz) =>
        (fwdX * dx) + (fwdZ * dz) < 0f;
}
