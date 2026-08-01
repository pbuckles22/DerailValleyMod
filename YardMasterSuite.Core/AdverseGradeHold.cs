using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Safety-biased grade for braking plans (**1.16**).
/// <para>
/// The raw reading is noisy — Player.log 0.5.59 shows −2.6 % → −0.3 % → +0.1 % across three
/// consecutive frames. Grade sizes every warning window, so following that noise moved the window
/// edge under a board sitting on it and blinked the Brake chip on and off.
/// </para>
/// <para>
/// A steeper descent applies at once (it can only mean less room); a friendlier reading has to be
/// earned at <see cref="RecoveryPercentPerSecond"/>.
/// </para>
/// </summary>
public static class AdverseGradeHold
{
    /// <summary>How fast the held grade may climb back toward a friendlier reading (% per second).</summary>
    public const float RecoveryPercentPerSecond = 0.4f;

    /// <summary>
    /// <paramref name="heldPercent"/> is the previous result (null on the first frame).
    /// Negative percent is downhill.
    /// </summary>
    public static float Step(float? heldPercent, float currentPercent, float elapsedSeconds)
    {
        if (heldPercent is not float held || currentPercent <= held)
        {
            return currentPercent;
        }

        var recovered = held + (RecoveryPercentPerSecond * Math.Max(0f, elapsedSeconds));
        return recovered >= currentPercent ? currentPercent : recovered;
    }
}
