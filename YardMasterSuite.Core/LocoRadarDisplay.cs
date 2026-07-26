using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Compact captions for other-loco AR radar markers (4.10): type · meters · place.
/// Place is track FullDisplayID when it already embeds a city (e.g. <c>SM-T12P</c>);
/// otherwise city YardID is included (e.g. <c>FF #Y</c>).
/// </summary>
public static class LocoRadarDisplay
{
    /// <summary>Short type token for AR (e.g. <c>DE2</c>), or null when unknown.</summary>
    public static string? ShortTypeId(string? typeId)
    {
        var id = typeId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        const string locoPrefix = "Loco";
        if (id!.StartsWith(locoPrefix, StringComparison.OrdinalIgnoreCase)
            && id.Length > locoPrefix.Length)
        {
            id = id.Substring(locoPrefix.Length).TrimStart();
        }

        return string.IsNullOrEmpty(id) ? null : id;
    }

    /// <summary>
    /// True when track display already embeds a city/yard code like <c>SM-T12P</c> / <c>FF-A1</c>
    /// (letters, dash, rest). <c>#Y</c> and bare spur ids return false.
    /// </summary>
    public static bool TrackIncludesCity(string? trackDisplay)
    {
        var t = trackDisplay?.Trim();
        if (string.IsNullOrEmpty(t))
        {
            return false;
        }

        var dash = t!.IndexOf('-');
        if (dash < 2 || dash > 4 || dash >= t.Length - 1)
        {
            return false;
        }

        for (var i = 0; i < dash; i++)
        {
            if (!char.IsLetter(t[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// True for real station YardIDs (<c>FF</c>, <c>SM</c>, <c>HB</c>). Rejects spur junk (<c>#Y</c>, <c>Y</c>).
    /// </summary>
    public static bool IsUsableCityYardId(string? cityYardId)
    {
        var c = cityYardId?.Trim();
        if (string.IsNullOrEmpty(c) || c!.Length < 2 || c.Length > 4)
        {
            return false;
        }

        if (c[0] == '#')
        {
            return false;
        }

        for (var i = 0; i < c.Length; i++)
        {
            if (!char.IsLetter(c[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Build place token: track alone when it includes city; otherwise <c>{city} {track}</c> or city only.
    /// </summary>
    public static string? FormatPlace(string? trackDisplay, string? cityYardId)
    {
        var track = trackDisplay?.Trim();
        var city = IsUsableCityYardId(cityYardId) ? cityYardId!.Trim() : null;
        if (string.IsNullOrEmpty(track))
        {
            return city;
        }

        if (TrackIncludesCity(track))
        {
            return track;
        }

        if (string.IsNullOrEmpty(city))
        {
            return track;
        }

        // Only treat city as already embedded when track is City-Rest (not "#Y".StartsWith("#Y")).
        if (track!.StartsWith(city + "-", StringComparison.OrdinalIgnoreCase))
        {
            return track;
        }

        return $"{city} {track}";
    }

    /// <summary>
    /// AR caption under the loco icon, e.g. <c>DE2 145m SM-O6I</c> or <c>DE2 179m FF #Y</c>.
    /// </summary>
    public static string FormatCaption(string? typeId, float distanceMeters, string? placeLabel)
    {
        var meters = (int)Math.Round(Math.Max(0f, distanceMeters), MidpointRounding.AwayFromZero);
        var type = ShortTypeId(typeId);
        var place = placeLabel?.Trim();
        if (string.IsNullOrEmpty(place))
        {
            place = null;
        }

        if (type != null && place != null)
        {
            return $"{type} {meters}m {place}";
        }

        if (type != null)
        {
            return $"{type} {meters}m";
        }

        if (place != null)
        {
            return $"{meters}m {place}";
        }

        return $"{meters}m";
    }
}
