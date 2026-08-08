namespace YardMasterSuite.Core;

/// <summary>
/// Strip Unity rich-text tags for HUD width measure. Pure so Tier 1 can lock the GC-sensitive path.
/// </summary>
public static class HudRichText
{
    public static string StripTags(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0)
        {
            return text ?? "";
        }

        var sb = new System.Text.StringBuilder(text.Length);
        var inTag = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '<')
            {
                inTag = true;
                continue;
            }

            if (ch == '>')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// When an OnGUI bar may re-measure width. OnGUI runs multiple times per frame; re-stripping
/// rich text + CalcSize every call filled the managed heap and produced a ~2.5 s GC cadence
/// (GeminiDocs rhythmic stutter diagnosis, 2026-08-07 video).
/// </summary>
public static class HudBarMeasureCache
{
    public static bool NeedsRemeasure(string? cachedLabel, string label) =>
        cachedLabel is null
        || !string.Equals(cachedLabel, label, System.StringComparison.Ordinal);

    public static bool NeedsRemeasure(
        string? cachedLabel,
        string label,
        int cachedScreenWidth,
        int screenWidth) =>
        cachedScreenWidth != screenWidth || NeedsRemeasure(cachedLabel, label);
}
