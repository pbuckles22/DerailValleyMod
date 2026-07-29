using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Parses Derail Valley speed-board text (SignDebug / signText) into km/h.
/// Board digits are tens of km/h ("6" → 60, "12" → 120). Values that already look like
/// full km/h (e.g. "80") pass through when digit×10 would exceed 120.
/// Dual junction boards: <c>6/4</c>, <c>3 4</c>, or stacked <c>3\n4</c> = through / diverge.
/// Grade annotations (<c>+1.2</c>, <c>-1.9</c>) are not diverge speeds.
/// </summary>
public static class SpeedLimitBoardParser
{
    public const float MaxPostedKmh = 120f;

    /// <summary>Through (left of slash) and optional diverge (right of slash).</summary>
    public readonly struct DualKmh
    {
        public DualKmh(float throughKmh, float? divergeKmh)
        {
            ThroughKmh = throughKmh;
            DivergeKmh = divergeKmh;
        }

        public float ThroughKmh { get; }
        public float? DivergeKmh { get; }
        public bool IsDual => DivergeKmh is not null;
    }

    /// <summary>
    /// Single-number boards, or the through (left) side of a dual board.
    /// Prefer <see cref="ParseDual"/> + <see cref="Pick"/> when junction branch is known.
    /// </summary>
    public static float? ParseKmh(string? text)
    {
        var dual = ParseDual(text);
        return dual?.ThroughKmh;
    }

    public static DualKmh? ParseDual(string? text)
    {
        if (text is null || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // Normalize: slash, newline, and space are dual separators (DV stacked boards).
        var token = text!.Trim()
            .Replace('\n', '/')
            .Replace('\r', '/')
            .Replace(' ', '/');

        var parts = token.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var through = ParseOne(parts[0]);
        if (through is null)
        {
            return null;
        }

        float? diverge = null;
        // Grade markers (+/-) are not diverge speeds (e.g. "4 -1.9", "6\n+1.2").
        if (parts.Length > 1 && !parts[1].Contains("-") && !parts[1].Contains("+"))
        {
            diverge = ParseOne(parts[1]);
        }

        return new DualKmh(through.Value, diverge);
    }

    /// <summary>True when text is a dual through/diverge junction board.</summary>
    public static bool IsSwitchSign(string? text)
    {
        var dual = ParseDual(text);
        return dual is { IsDual: true };
    }

    /// <summary>
    /// Junction boards: branch 0 (through) → left number; any other branch → right number when dual.
    /// </summary>
    public static float Pick(DualKmh dual, bool diverging) =>
        diverging && dual.DivergeKmh is float d ? d : dual.ThroughKmh;

    private static float? ParseOne(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !int.TryParse(token.Trim(), out var n) || n <= 0)
        {
            return null;
        }

        var asDigitTimesTen = n * 10f;
        if (asDigitTimesTen <= MaxPostedKmh)
        {
            return asDigitTimesTen;
        }

        if (n <= MaxPostedKmh)
        {
            return n;
        }

        return null;
    }
}
