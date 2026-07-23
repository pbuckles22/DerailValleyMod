using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure compass heading for Monitor HUD (no Unity / game refs).
/// Unity world north = +Z; degrees increase clockwise toward +X.
/// Display uses a 16-point rose (N, NNE, NE, ENE, …) — no degree readout.
/// </summary>
public static class HeadingDisplay
{
    private const float MinForwardSqr = 1e-8f;
    private const double Rad2Deg = 180.0 / Math.PI;
    private const double SectorDegrees = 22.5;

    /// <summary>16-point compass, index 0 = N centered on 0°.</summary>
    private static readonly string[] Points =
    {
        "N", "NNE", "NE", "ENE",
        "E", "ESE", "SE", "SSE",
        "S", "SSW", "SW", "WSW",
        "W", "WNW", "NW", "NNW",
    };

    public static float? FromForward(float x, float z)
    {
        if (x * x + z * z < MinForwardSqr)
        {
            return null;
        }

        var heading = Math.Atan2(x, z) * Rad2Deg;
        if (heading < 0)
        {
            heading += 360.0;
        }

        return (float)heading;
    }

    public static string? ToCompassPoint(float? degrees)
    {
        if (degrees is null)
        {
            return null;
        }

        // Each point owns a 22.5° sector centered on n×22.5 (N: 348.75–11.25, …).
        var normalized = degrees.Value % 360f;
        if (normalized < 0)
        {
            normalized += 360f;
        }

        var index = (int)Math.Floor((normalized + SectorDegrees / 2.0) / SectorDegrees) % Points.Length;
        if (index < 0)
        {
            index += Points.Length;
        }

        return Points[index];
    }

    public static string Format(float? degrees)
    {
        var point = ToCompassPoint(degrees);
        return point is null ? "— Heading" : $"Heading {point}";
    }

    public static string FormatPoint(string? compassPoint) =>
        compassPoint is null ? "— Heading" : $"Heading {compassPoint}";
}
