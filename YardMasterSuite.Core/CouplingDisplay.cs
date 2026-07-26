namespace YardMasterSuite.Core;

/// <summary>
/// Pure front/rear coupler formatting for Monitor HUD.
/// Red * = tow not ready. Yellow * = loco↔loco tow-ready, MU open.
/// White + = tow-ready (car/freight). Blue + = loco↔loco with MU.
/// Plain/log: loose *, MU-open *Y, linked/team +.
/// </summary>
public static class CouplingDisplay
{
    /// <summary>Red — tow link unfinished (loco or car).</summary>
    public const string NoGoColor = "#FF5555";

    /// <summary>Same as <see cref="NoGoColor"/>.</summary>
    public const string LooseColor = NoGoColor;

    /// <summary>Yellow — loco↔loco ready to tow, MU not connected.</summary>
    public const string MuWarningColor = "#FFD400";

    /// <summary>Blue — loco↔loco fully coupled with MU.</summary>
    public const string MuTeamColor = "#55AAFF";

    public static string Format(CouplerLinkStatus? front, CouplerLinkStatus? rear) =>
        FormatCore(front, rear, richText: false);

    public static string FormatHud(CouplerLinkStatus? front, CouplerLinkStatus? rear) =>
        FormatCore(front, rear, richText: true);

    private static string FormatCore(CouplerLinkStatus? front, CouplerLinkStatus? rear, bool richText)
    {
        if (front is null || rear is null)
        {
            return "— Couplers";
        }

        return $"Couplers {Side("F", front.Value, richText)} {Side("R", rear.Value, richText)}";
    }

    private static string Side(string letter, CouplerLinkStatus status, bool richText)
    {
        switch (status)
        {
            case CouplerLinkStatus.Linked:
                return $"{letter}+";
            case CouplerLinkStatus.MuTeam:
                var team = $"{letter}+";
                return richText ? $"<color={MuTeamColor}>{team}</color>" : team;
            case CouplerLinkStatus.Loose:
                var loose = $"{letter}*";
                return richText ? $"<color={NoGoColor}>{loose}</color>" : loose;
            case CouplerLinkStatus.MuWarning:
                return richText
                    ? $"<color={MuWarningColor}>{letter}*</color>"
                    : $"{letter}*Y";
            default:
                return $"{letter}-";
        }
    }
}
