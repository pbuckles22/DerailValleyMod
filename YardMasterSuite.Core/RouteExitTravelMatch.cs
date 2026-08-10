namespace YardMasterSuite.Core;

/// <summary>Whether loco travel matches the planned Exit compass cue.</summary>
public static class RouteExitTravelMatch
{
    /// <summary>
    /// <paramref name="exitCue"/> like <c>Exit NNW</c> or bare <c>NNW</c>.
    /// <paramref name="travelPoint"/> is current travel compass (16-point).
    /// </summary>
    public static string Format(string? exitCue, string? travelPoint, float speedKmh)
    {
        var exit = NormalizePoint(exitCue);
        var travel = string.IsNullOrWhiteSpace(travelPoint) ? null : travelPoint!.Trim().ToUpperInvariant();
        if (exit == null)
        {
            return "—";
        }

        if (speedKmh < 1f || travel == null)
        {
            return "idle→" + exit;
        }

        if (string.Equals(exit, travel, System.StringComparison.Ordinal))
        {
            return "match " + travel;
        }

        // Opposite-ish: 8 sectors apart on 16-point rose ≈ reverse.
        if (AreRoughlyOpposite(exit, travel))
        {
            return "opposite " + travel + " (want " + exit + ")";
        }

        return "drift " + travel + " (want " + exit + ")";
    }

    public static string? NormalizePoint(string? exitCue)
    {
        if (string.IsNullOrWhiteSpace(exitCue))
        {
            return null;
        }

        var t = exitCue!.Trim();
        if (t.StartsWith("Exit ", System.StringComparison.OrdinalIgnoreCase))
        {
            t = t.Substring(5).Trim();
        }

        return string.IsNullOrEmpty(t) ? null : t.ToUpperInvariant();
    }

    private static bool AreRoughlyOpposite(string a, string b)
    {
        var ia = IndexOf(a);
        var ib = IndexOf(b);
        if (ia < 0 || ib < 0)
        {
            return false;
        }

        var d = System.Math.Abs(ia - ib);
        if (d > 8)
        {
            d = 16 - d;
        }

        return d >= 7;
    }

    private static int IndexOf(string point)
    {
        // Same order as HeadingDisplay Points.
        switch (point)
        {
            case "N": return 0;
            case "NNE": return 1;
            case "NE": return 2;
            case "ENE": return 3;
            case "E": return 4;
            case "ESE": return 5;
            case "SE": return 6;
            case "SSE": return 7;
            case "S": return 8;
            case "SSW": return 9;
            case "SW": return 10;
            case "WSW": return 11;
            case "W": return 12;
            case "WNW": return 13;
            case "NW": return 14;
            case "NNW": return 15;
            default: return -1;
        }
    }
}
