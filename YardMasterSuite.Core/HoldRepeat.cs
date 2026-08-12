namespace YardMasterSuite.Core;

/// <summary>
/// Keyboard-style hold repeat: fire on press, pause, then fire on an interval while held.
/// Matches cab "hold = multiple presses" feel for on-consist lever redirect.
/// </summary>
public static class HoldRepeat
{
    /// <summary>Delay after the first step before auto-repeat starts.</summary>
    public const float DefaultInitialDelaySeconds = 0.35f;

    /// <summary>Interval between auto-repeat steps while held (~12.5 Hz).</summary>
    public const float DefaultIntervalSeconds = 0.08f;

    /// <summary>
    /// Whether this frame should emit one discrete step.
    /// Call once per axis side; reset <paramref name="nextFireAt"/> when the side is released.
    /// </summary>
    /// <param name="pressedThisFrame">True on the first frame of the press.</param>
    /// <param name="isHeld">True while the key/button remains down.</param>
    /// <param name="timeHeld">Seconds held (0 on the press frame).</param>
    /// <param name="nextFireAt">Per-binding schedule; 0 when idle.</param>
    public static bool ShouldFire(
        bool pressedThisFrame,
        bool isHeld,
        float timeHeld,
        ref float nextFireAt,
        float initialDelaySeconds = DefaultInitialDelaySeconds,
        float intervalSeconds = DefaultIntervalSeconds)
    {
        if (!isHeld)
        {
            nextFireAt = 0f;
            return false;
        }

        var delay = initialDelaySeconds > 0f ? initialDelaySeconds : DefaultInitialDelaySeconds;
        var interval = intervalSeconds > 0f ? intervalSeconds : DefaultIntervalSeconds;

        if (pressedThisFrame)
        {
            nextFireAt = delay;
            return true;
        }

        if (timeHeld + 0.0001f < nextFireAt)
        {
            return false;
        }

        nextFireAt = timeHeld + interval;
        return true;
    }
}
