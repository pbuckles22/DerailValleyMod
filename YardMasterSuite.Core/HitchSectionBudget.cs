using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Names which HUD/cab section was hot when a hitch spike is logged (Tier 1 / CI).
/// Unity wires Stopwatch samples; this formats attribution only.
/// </summary>
public static class HitchSectionBudget
{
    /// <summary>Frame gap (ms) that counts as a spike (matches ~HitchCadenceProbe.SpikeSeconds).</summary>
    public const float FrameSpikeMs = 40f;

    /// <summary>Section ms above this is eligible to be named hot.</summary>
    public const float SectionHotMs = 8f;

    /// <summary>
    /// Hottest named section by ms. Empty names ignored. Null if none.
    /// </summary>
    public static string? PickHottest(params (string Name, float Ms)[] sections)
    {
        string? best = null;
        var bestMs = float.NegativeInfinity;
        if (sections == null)
        {
            return null;
        }

        for (var i = 0; i < sections.Length; i++)
        {
            var (name, ms) = sections[i];
            if (string.IsNullOrWhiteSpace(name) || ms < SectionHotMs)
            {
                continue;
            }

            if (ms > bestMs)
            {
                bestMs = ms;
                best = name;
            }
        }

        return best;
    }

    /// <summary>
    /// Player.log line, or null if frame under spike threshold.
    /// If measured sections cannot explain most of the frame, hot=outside (game/GC/other).
    /// </summary>
    public static string? FormatSpike(float frameDtMs, params (string Name, float Ms)[] sections)
    {
        if (frameDtMs < FrameSpikeMs)
        {
            return null;
        }

        float sum = 0f;
        if (sections != null)
        {
            for (var i = 0; i < sections.Length; i++)
            {
                if (sections[i].Ms > 0f)
                {
                    sum += sections[i].Ms;
                }
            }
        }

        var outside = frameDtMs - sum;
        if (outside < 0f)
        {
            outside = 0f;
        }

        var hot = PickHottest(sections ?? Array.Empty<(string, float)>());
        // Unexplained majority → blame outside (streaming / Unity / GC not in our timers).
        if (outside >= FrameSpikeMs && outside >= sum)
        {
            hot = "outside";
        }
        else if (hot == null && outside >= SectionHotMs)
        {
            hot = "outside";
        }

        hot ??= "none";

        var sb = new System.Text.StringBuilder(160);
        sb.Append("T2 hitch-spike: dt=").Append((int)Math.Round(frameDtMs)).Append("ms");
        sb.Append(" hot=").Append(hot);
        if (sections != null)
        {
            for (var i = 0; i < sections.Length; i++)
            {
                var (name, ms) = sections[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                sb.Append(' ').Append(name).Append('=').Append((int)Math.Round(ms)).Append("ms");
            }
        }

        if (outside >= SectionHotMs)
        {
            sb.Append(" outside=").Append((int)Math.Round(outside)).Append("ms");
        }

        return sb.ToString();
    }
}
