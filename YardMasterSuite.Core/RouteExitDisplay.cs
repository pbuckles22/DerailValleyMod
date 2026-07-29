namespace YardMasterSuite.Core;

/// <summary>Which way to leave the origin track toward the planned path (3.5).</summary>
public static class RouteExitDisplay
{
    /// <summary>
    /// Compass exit from origin toward the next path point (XZ delta).
    /// Null when unknown / zero length.
    /// </summary>
    public static string? Format(float fromX, float fromZ, float towardX, float towardZ)
    {
        var dx = towardX - fromX;
        var dz = towardZ - fromZ;
        var heading = HeadingDisplay.FromForward(dx, dz);
        if (heading == null)
        {
            return null;
        }

        var point = HeadingDisplay.ToCompassPoint(heading);
        return string.IsNullOrEmpty(point) ? null : $"Exit {point}";
    }
}
