using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Smooth sticky ↔ on-object AR marker glide (A.3).
/// Freeze start on mode edge; lerp to live target over <see cref="DefaultDurationSeconds"/>.
/// </summary>
public static class ArMarkerTransition
{
    public const float DefaultDurationSeconds = 1f;

    public static float StepProgress(
        float current,
        float deltaSeconds,
        float durationSeconds = DefaultDurationSeconds)
    {
        if (durationSeconds <= 1e-4f)
        {
            return 1f;
        }

        return Math.Min(1f, current + Math.Max(0f, deltaSeconds) / durationSeconds);
    }

    /// <summary>Ease-in-out cubic for a continuous glide (no mid-screen “stop”).</summary>
    public static float EaseInOutCubic(float t)
    {
        if (t <= 0f)
        {
            return 0f;
        }

        if (t >= 1f)
        {
            return 1f;
        }

        return t < 0.5f
            ? 4f * t * t * t
            : 1f - (float)Math.Pow(-2f * t + 2f, 3) / 2f;
    }

    public static void Lerp(
        float ax,
        float ay,
        float bx,
        float by,
        float progress01,
        out float x,
        out float y)
    {
        var s = EaseInOutCubic(progress01);
        x = ax + (bx - ax) * s;
        y = ay + (by - ay) * s;
    }
}
