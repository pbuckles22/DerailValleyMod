namespace YardMasterSuite.Core;

/// <summary>
/// Traction-motor cab-light status for the train HUD bar (OK / Hot / Dead).
/// </summary>
public enum MotorStatus
{
    Ok,
    Hot,
    Dead,
}

/// <summary>
/// Pure motor-status formatting for the train HUD bar.
/// Mirrors cab TM lamp intent: green OK, yellow Hot (over-temp), red Dead (fuse off / dead TM).
/// </summary>
public static class MotorDisplay
{
    /// <summary>Green — motors online and cool enough.</summary>
    public const string OkColor = "#55FF55";

    /// <summary>Yellow — matches Load / MU warning tone.</summary>
    public const string HotColor = "#FFD400";

    /// <summary>Red — fuse off or dead traction motor(s).</summary>
    public const string DeadColor = "#FF5555";

    /// <summary>
    /// Game TMS signal: fuse on and all traction motors alive.
    /// </summary>
    public const float TmsOk = 1f;

    /// <summary>
    /// Game TMS signal: power fuse off.
    /// </summary>
    public const float TmsFuseOff = 0f;

    /// <summary>
    /// Game TMS signal: fuse on but at least one TM is dead.
    /// </summary>
    public const float TmsHasDead = -1f;

    /// <summary>
    /// Resolve cab-equivalent motor status from typed sim signals.
    /// Dead wins over Hot; null when no usable TM signals are present.
    /// </summary>
    public static MotorStatus? StatusFromSignals(
        float? tmsState,
        float? temperature,
        float? overheatingThreshold,
        float? workingMotors,
        float? totalMotors)
    {
        var hasWorkingCount =
            workingMotors is not null
            && totalMotors is > 0f
            && workingMotors.Value + 0.01f < totalMotors.Value;

        if (tmsState is TmsFuseOff or TmsHasDead || hasWorkingCount)
        {
            return MotorStatus.Dead;
        }

        if (temperature is not null
            && overheatingThreshold is > 0f
            && temperature.Value > overheatingThreshold.Value)
        {
            return MotorStatus.Hot;
        }

        if (tmsState is TmsOk || temperature is not null)
        {
            return MotorStatus.Ok;
        }

        return null;
    }

    public static string Format(MotorStatus? status) =>
        FormatCore(status, richText: false);

    public static string FormatHud(MotorStatus? status) =>
        FormatCore(status, richText: true);

    private static string FormatCore(MotorStatus? status, bool richText)
    {
        if (status is null)
        {
            return "— Motors";
        }

        return status.Value switch
        {
            MotorStatus.Ok => Colorize("Motors OK", OkColor, richText),
            MotorStatus.Hot => Colorize("Motors Hot", HotColor, richText),
            MotorStatus.Dead => Colorize("Motors Dead", DeadColor, richText),
            _ => "— Motors",
        };
    }

    private static string Colorize(string text, string color, bool richText) =>
        richText ? $"<color={color}>{text}</color>" : text;
}
