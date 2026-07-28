namespace YardMasterSuite.Core;

/// <summary>Travel gear for 4.12 direction-gated proximity (DV reverser).</summary>
public enum ProximityTravelDirection
{
    Unknown = 0,
    Reverse = 1,
    Neutral = 2,
    Forward = 3,
}

/// <summary>
/// Maps DV <c>ReverserControl.Value</c> (neutral = 0.5) to which proximity chip to show.
/// Reverse → Rear; Forward → Front; Neutral/Unknown → omit.
/// </summary>
public static class ProximityTravelDirectionGate
{
    /// <summary>Matches <c>DV.Simulation.Controllers.ReverserControl.NEUTRAL_VALUE</c>.</summary>
    public const float NeutralValue = 0.5f;

    public static ProximityTravelDirection FromReverser(float? value)
    {
        if (value is null || float.IsNaN(value.Value))
        {
            return ProximityTravelDirection.Unknown;
        }

        var v = value.Value;
        if (v < NeutralValue)
        {
            return ProximityTravelDirection.Reverse;
        }

        if (v > NeutralValue)
        {
            return ProximityTravelDirection.Forward;
        }

        return ProximityTravelDirection.Neutral;
    }

    public static bool ShouldShowChip(ProximityTravelDirection direction) =>
        direction == ProximityTravelDirection.Reverse || direction == ProximityTravelDirection.Forward;

    public static bool UseFrontTip(ProximityTravelDirection direction) =>
        direction == ProximityTravelDirection.Forward;

    public static string ChipLabel(ProximityTravelDirection direction) =>
        direction == ProximityTravelDirection.Forward ? "Front" : "Rear";
}
