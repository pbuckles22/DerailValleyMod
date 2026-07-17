namespace YardMasterSuite.Core;

/// <summary>
/// Pure front/rear coupler formatting for Monitor HUD.
/// </summary>
public static class CouplingDisplay
{
    public static string Format(bool? frontCoupled, bool? rearCoupled)
    {
        if (frontCoupled is null || rearCoupled is null)
        {
            return "— cpl";
        }

        return $"F{Mark(frontCoupled.Value)} R{Mark(rearCoupled.Value)}";
    }

    private static char Mark(bool coupled) => coupled ? '+' : '-';
}
