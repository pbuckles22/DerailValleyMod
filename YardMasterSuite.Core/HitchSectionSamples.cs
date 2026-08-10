namespace YardMasterSuite.Core;

/// <summary>
/// Last-frame Unity timings (ms) for hitch attribution. Cleared when consumed into a spike log.
/// </summary>
public static class HitchSectionSamples
{
    public static float ArGuiMs { get; set; }

    public static float ConsumeArGuiMs()
    {
        var ms = ArGuiMs;
        ArGuiMs = 0f;
        return ms;
    }
}
