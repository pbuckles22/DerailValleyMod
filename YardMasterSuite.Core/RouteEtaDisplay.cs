using System;

namespace YardMasterSuite.Core;

/// <summary>Format Align Route ETA / remaining distance for HUD + logs (3.5).</summary>
public static class RouteEtaDisplay
{
    /// <summary>Second-precision chip: <c>ETA 20m34s</c> / <c>ETA 1h05m12s</c>. Arrival ⇒ <c>ETA 0s</c>.</summary>
    public static string? Format(float totalCostSeconds)
    {
        if (float.IsNaN(totalCostSeconds) || float.IsInfinity(totalCostSeconds) || totalCostSeconds < 0f)
        {
            return null;
        }

        var total = (int)Math.Floor(totalCostSeconds + 0.5f);
        if (total < 1)
        {
            return "ETA 0s";
        }

        var h = total / 3600;
        var m = (total % 3600) / 60;
        var s = total % 60;
        if (h > 0)
        {
            return $"ETA {h}h{m:00}m{s:00}s";
        }

        if (m > 0)
        {
            return $"ETA {m}m{s:00}s";
        }

        return $"ETA {s}s";
    }

    public static string? FormatRemainingDistance(float? remainingMeters)
    {
        if (remainingMeters is not float m || float.IsNaN(m) || float.IsInfinity(m) || m < 0f)
        {
            return null;
        }

        if (m < 1000f)
        {
            return $"rem {(int)Math.Round(m, MidpointRounding.AwayFromZero)}m";
        }

        return $"rem {m / 1000f:0.0}km";
    }

    /// <summary>Overall trip progress (not current-hop span).</summary>
    public static string? FormatProgress(float? tripProgress01)
    {
        if (tripProgress01 is not float p || float.IsNaN(p) || float.IsInfinity(p))
        {
            return null;
        }

        if (p < 0f)
        {
            p = 0f;
        }
        else if (p > 1f)
        {
            p = 1f;
        }

        return $"trip {(int)Math.Round(p * 100f, MidpointRounding.AwayFromZero)}%";
    }

    /// <summary>
    /// Path chip + ETA (+ optional rem / trip / pace|plan mode for HUD + logs).
    /// </summary>
    public static string? WithPathChip(
        string? pathChip,
        float totalCostSeconds,
        float? remainingMeters = null,
        float? hopProgress01 = null,
        string? etaMode = null)
    {
        if (string.IsNullOrEmpty(pathChip))
        {
            return pathChip;
        }

        var eta = Format(totalCostSeconds);
        if (eta == null)
        {
            return pathChip;
        }

        if (!string.IsNullOrEmpty(etaMode))
        {
            eta = eta + " " + etaMode;
        }

        var line = pathChip + " | " + eta;
        var rem = FormatRemainingDistance(remainingMeters);
        if (rem != null)
        {
            line += " | " + rem;
        }

        var prog = FormatProgress(hopProgress01);
        if (prog != null)
        {
            line += " | " + prog;
        }

        return line;
    }

    /// <summary>Top HUD Path chip: status + ETA only.</summary>
    public static string? HudPathChip(string? pathChip, float totalCostSeconds) =>
        WithPathChip(pathChip, totalCostSeconds);
}
