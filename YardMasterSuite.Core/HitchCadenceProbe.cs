namespace YardMasterSuite.Core;

/// <summary>
/// Lightweight hitch detector for Player.log (drive-tick only).
/// </summary>
public static class HitchCadenceProbe
{
    /// <summary>Log when frame gap exceeds this (seconds).</summary>
    public const float SpikeSeconds = 0.04f;

    /// <summary>At most one spike log per this many seconds (logging itself hitch-taxes).</summary>
    public const float MinLogIntervalSeconds = 1f;

    public static string? NextSpikeMessage(float now, float lastFrameAt, out float nextLastFrameAt) =>
        NextSpikeMessage(now, lastFrameAt, lastLogAt: -999f, out nextLastFrameAt, out _);

    public static string? NextSpikeMessage(
        float now,
        float lastFrameAt,
        float lastLogAt,
        out float nextLastFrameAt,
        out float nextLogAt)
    {
        nextLastFrameAt = now;
        nextLogAt = lastLogAt;
        if (lastFrameAt < 0f)
        {
            return null;
        }

        var dt = now - lastFrameAt;
        if (dt < SpikeSeconds)
        {
            return null;
        }

        if (now - lastLogAt < MinLogIntervalSeconds)
        {
            return null;
        }

        nextLogAt = now;
        return $"T2 hitch-spike: dt={dt * 1000f:0}ms";
    }
}
