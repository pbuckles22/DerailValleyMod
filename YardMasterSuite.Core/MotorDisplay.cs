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
/// Cab MU temperature band from <c>MultipleUnitStateObserver.TemperatureState</c>
/// (Warning = TM TEMP yellow; Critical = overheat critical).
/// </summary>
public enum MotorCabTempBand
{
    Nominal = 0,
    Warning = 1,
    Critical = 2,
    WarningAndCritical = 3,
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
    /// Prefer <paramref name="cabTempBand"/> (MU Warning/Critical) so Hot matches cab TM TEMP.
    /// Fallback Hot: temperature ≥ TM <paramref name="overheatingThreshold"/> (critical).
    /// <paramref name="tmFuseOn"/> is the cab TM knife (false → Dead even at idle / 0% throttle —
    /// TMS often stays "OK" until power is demanded).
    /// </summary>
    public static MotorStatus? StatusFromSignals(
        float? tmsState,
        float? temperature,
        float? overheatingThreshold,
        float? workingMotors,
        float? totalMotors,
        MotorCabTempBand? cabTempBand = null,
        bool? tmFuseOn = null)
    {
        var hasWorkingCount =
            workingMotors is not null
            && totalMotors is > 0f
            && workingMotors.Value + 0.01f < totalMotors.Value;

        // Knife off must win at idle — do not wait for TMS to flip under load.
        if (tmFuseOn == false
            || tmsState is TmsFuseOff or TmsHasDead
            || hasWorkingCount)
        {
            return MotorStatus.Dead;
        }

        if (cabTempBand is MotorCabTempBand.Warning
            or MotorCabTempBand.Critical
            or MotorCabTempBand.WarningAndCritical)
        {
            return MotorStatus.Hot;
        }

        if (temperature is not null
            && overheatingThreshold is > 0f
            && temperature.Value >= overheatingThreshold.Value)
        {
            return MotorStatus.Hot;
        }

        // Temperature alone must not imply OK (masks knife-off while TMS idle).
        if (tmsState is TmsOk || cabTempBand is MotorCabTempBand.Nominal)
        {
            return MotorStatus.Ok;
        }

        if (temperature is not null && tmFuseOn != false)
        {
            return MotorStatus.Ok;
        }

        return null;
    }

    public static string Format(MotorStatus? status) =>
        FormatCore(status, richText: false, governorActive: false, flashOn: false, forcedHeatPercent: null);

    public static string FormatHud(MotorStatus? status) =>
        FormatCore(status, richText: true, governorActive: false, flashOn: false, forcedHeatPercent: null);

    /// <summary>
    /// HUD Motors chip with optional debug heat % and governor flash (blink when actively capping).
    /// </summary>
    public static string FormatHud(
        MotorStatus? status,
        bool governorActive,
        bool flashOn,
        float? forcedHeatPercent = null) =>
        FormatCore(status, richText: true, governorActive, flashOn, forcedHeatPercent);

    private static string FormatCore(
        MotorStatus? status,
        bool richText,
        bool governorActive,
        bool flashOn,
        float? forcedHeatPercent)
    {
        if (status is null && forcedHeatPercent is null)
        {
            return "— Motors";
        }

        string text;
        string color;

        if (forcedHeatPercent is not null)
        {
            text = $"Motors Hot {forcedHeatPercent.Value:0}%";
            color = HotColor;
        }
        else
        {
            switch (status)
            {
                case MotorStatus.Ok:
                    text = "Motors OK";
                    color = OkColor;
                    break;
                case MotorStatus.Hot:
                    text = "Motors Hot";
                    color = HotColor;
                    break;
                case MotorStatus.Dead:
                    text = "Motors Dead";
                    color = DeadColor;
                    break;
                default:
                    return "— Motors";
            }
        }

        if (governorActive)
        {
            text = flashOn ? $"{text} ▼GOV" : text;
            if (flashOn)
            {
                color = "#FF6600";
            }
        }

        return Colorize(text, color, richText);
    }

    private static string Colorize(string text, string color, bool richText) =>
        richText ? $"<color={color}>{text}</color>" : text;
}