using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Parses Derail Valley speed-board text (SignDebug / signText) into km/h.
/// Board digits are tens of km/h ("6" → 60, "12" → 120). Values that already look like
/// full km/h (e.g. "80") pass through when digit×10 would exceed 120.
/// </summary>
public static class SpeedLimitBoardParser
{
    public const float MaxPostedKmh = 120f;

    public static float? ParseKmh(string? text)
    {
        if (text is null || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var token = text!.Trim();
        var newline = token.IndexOfAny(new[] { '\n', '\r' });
        if (newline >= 0)
        {
            token = token.Substring(0, newline).Trim();
        }

        var slash = token.IndexOf('/');
        if (slash >= 0)
        {
            token = token.Substring(0, slash).Trim();
        }

        if (!int.TryParse(token, out var n) || n <= 0)
        {
            return null;
        }

        var asDigitTimesTen = n * 10f;
        if (asDigitTimesTen <= MaxPostedKmh)
        {
            return asDigitTimesTen;
        }

        // Already full km/h (e.g. Floor(80).ToString() → "80").
        if (n <= MaxPostedKmh)
        {
            return n;
        }

        return null;
    }
}
