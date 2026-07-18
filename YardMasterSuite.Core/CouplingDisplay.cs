namespace YardMasterSuite.Core;

/// <summary>
/// Pure front/rear coupler formatting for Monitor HUD.
/// MuWarning ends render yellow in HUD rich text (blue MU open on loco↔loco).
/// </summary>
public static class CouplingDisplay
{
    public const string MuWarningColor = "#FFD400";

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
            case CouplerLinkStatus.Loose:
                // Same glyph as MU warning; HUD yellow = MU, plain = loose chain.
                return $"{letter}*";
            case CouplerLinkStatus.MuWarning:
                var mark = $"{letter}*";
                return richText ? $"<color={MuWarningColor}>{mark}</color>" : mark;
            default:
                return $"{letter}-";
        }
    }
}
