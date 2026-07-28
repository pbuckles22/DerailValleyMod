using System;

namespace YardMasterSuite.Core;

/// <summary>1.15 consist free-motion chip severity.</summary>
public enum FreeMotionSeverity
{
    None = 0,
    Yellow = 1,
    Red = 2,
}

/// <summary>Lead/trailing loco control snapshot for free-motion compare (pure).</summary>
public readonly struct LocoControlSnapshot
{
    public LocoControlSnapshot(
        bool engineOn,
        float reverser,
        float throttle,
        float brake,
        float independentBrake = 0f)
    {
        EngineOn = engineOn;
        Reverser = reverser;
        Throttle = throttle;
        Brake = brake;
        IndependentBrake = independentBrake;
    }

    public bool EngineOn { get; }
    public float Reverser { get; }
    public float Throttle { get; }
    public float Brake { get; }
    public float IndependentBrake { get; }
}

/// <summary>
/// Pure 1.15 consist free-motion: quiet when synced; yellow if off/neutral (and brakes match);
/// red if brake fight or on + in gear + controls mismatch lead.
/// </summary>
public static class ConsistFreeMotion
{
    /// <summary>Matches DV <c>ReverserControl.NEUTRAL_VALUE</c>.</summary>
    public const float NeutralReverser = 0.5f;

    /// <summary>Absolute tolerance for throttle / brake / reverser match.</summary>
    public const float ControlEpsilon = 0.05f;

    public const string YellowColor = "#FFD400";
    public const string RedColor = "#FF5555";

    public const string YellowLabel = "MU idle";
    public const string RedLabel = "MU desync";

    public static bool IsNeutralReverser(float reverser) =>
        Math.Abs(reverser - NeutralReverser) <= ControlEpsilon;

    public static bool BrakesMatch(LocoControlSnapshot lead, LocoControlSnapshot other) =>
        NearlyEqual(lead.Brake, other.Brake)
        && NearlyEqual(lead.IndependentBrake, other.IndependentBrake);

    public static bool ControlsMatch(LocoControlSnapshot lead, LocoControlSnapshot other) =>
        lead.EngineOn == other.EngineOn
        && NearlyEqual(lead.Reverser, other.Reverser)
        && NearlyEqual(lead.Throttle, other.Throttle)
        && BrakesMatch(lead, other);

    /// <summary>
    /// Compare one trailing unit to the lead.
    /// Matching → None. Brake/ind-brake fight → Red (even if off/neutral).
    /// Either unit off or Neutral (brakes OK) → Yellow.
    /// On + in gear on both + other mismatch → Red.
    /// </summary>
    public static FreeMotionSeverity CompareUnit(LocoControlSnapshot lead, LocoControlSnapshot other)
    {
        if (ControlsMatch(lead, other))
        {
            return FreeMotionSeverity.None;
        }

        // Brakes fight free motion whether or not the engine is running.
        if (!BrakesMatch(lead, other))
        {
            return FreeMotionSeverity.Red;
        }

        // Soft awareness from either cab when someone is off/Neutral.
        if (!other.EngineOn || IsNeutralReverser(other.Reverser)
            || !lead.EngineOn || IsNeutralReverser(lead.Reverser))
        {
            return FreeMotionSeverity.Yellow;
        }

        return FreeMotionSeverity.Red;
    }

    public static FreeMotionSeverity Aggregate(params FreeMotionSeverity[] severities)
    {
        var worst = FreeMotionSeverity.None;
        if (severities == null)
        {
            return worst;
        }

        foreach (var s in severities)
        {
            if (s > worst)
            {
                worst = s;
            }
        }

        return worst;
    }

    public static string Format(FreeMotionSeverity severity) =>
        severity switch
        {
            FreeMotionSeverity.Yellow => YellowLabel,
            FreeMotionSeverity.Red => RedLabel,
            _ => string.Empty,
        };

    public static string FormatHud(FreeMotionSeverity severity)
    {
        var text = Format(severity);
        if (text.Length == 0)
        {
            return string.Empty;
        }

        var color = severity == FreeMotionSeverity.Red ? RedColor : YellowColor;
        return $"<color={color}>{text}</color>";
    }

    private static bool NearlyEqual(float a, float b) =>
        Math.Abs(a - b) <= ControlEpsilon;
}
