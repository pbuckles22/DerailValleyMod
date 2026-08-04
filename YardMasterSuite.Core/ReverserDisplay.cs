namespace YardMasterSuite.Core;

/// <summary>
/// Lead-cab reverser letter chip: <c>R</c> reverse · <c>N</c> neutral · <c>F</c> forward.
/// Uses DV <c>ReverserControl.Value</c> (neutral = 0.5).
/// </summary>
public static class ReverserDisplay
{
    /// <summary>Red — reverse.</summary>
    public const string ReverseColor = "#FF5555";

    /// <summary>Yellow — neutral.</summary>
    public const string NeutralColor = "#FFD400";

    /// <summary>Green — forward.</summary>
    public const string ForwardColor = "#55FF55";

    public static string Format(float? reverser01) =>
        FormatCore(reverser01, richText: false);

    public static string FormatHud(float? reverser01) =>
        FormatCore(reverser01, richText: true);

    private static string FormatCore(float? reverser01, bool richText)
    {
        var direction = ProximityTravelDirectionGate.FromReverser(reverser01);
        var letter = direction switch
        {
            ProximityTravelDirection.Reverse => "R",
            ProximityTravelDirection.Neutral => "N",
            ProximityTravelDirection.Forward => "F",
            _ => "—",
        };

        if (!richText || letter == "—")
        {
            return letter;
        }

        var color = direction switch
        {
            ProximityTravelDirection.Reverse => ReverseColor,
            ProximityTravelDirection.Neutral => NeutralColor,
            _ => ForwardColor,
        };
        return $"<color={color}>{letter}</color>";
    }
}
