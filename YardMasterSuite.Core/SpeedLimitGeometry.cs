namespace YardMasterSuite.Core;

/// <summary>
/// Track geometry → posted speed (km/h), matching
/// <c>SignPlacer.CurveSegmentInfo.GetMaxSpeedForRadius</c> / DVRouteManager.
/// </summary>
public static class SpeedLimitGeometry
{
    /// <summary>
    /// Maps minimum curve radius (meters) to the in-game speed-board ladder (km/h).
    /// </summary>
    public static float? MaxSpeedForMinRadius(float minRadiusMeters)
    {
        if (float.IsNaN(minRadiusMeters)
            || float.IsInfinity(minRadiusMeters)
            || minRadiusMeters <= 0f)
        {
            return null;
        }

        if (minRadiusMeters < 50f)
        {
            return 10f;
        }

        if (minRadiusMeters < 70f)
        {
            return 20f;
        }

        if (minRadiusMeters < 95f)
        {
            return 30f;
        }

        if (minRadiusMeters < 130f)
        {
            return 40f;
        }

        if (minRadiusMeters < 170f)
        {
            return 50f;
        }

        if (minRadiusMeters < 230f)
        {
            return 60f;
        }

        if (minRadiusMeters < 360f)
        {
            return 70f;
        }

        if (minRadiusMeters < 700f)
        {
            return 80f;
        }

        if (minRadiusMeters < 900f)
        {
            return 90f;
        }

        if (minRadiusMeters < 1200f)
        {
            return 100f;
        }

        return 120f;
    }
}
