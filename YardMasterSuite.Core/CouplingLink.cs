namespace YardMasterSuite.Core;

/// <summary>
/// Usable link = mechanical + tightened + air hose + cocks open both sides.
/// MU blue wires are optional for running; missing MU on loco↔loco is a warning only.
/// </summary>
public static class CouplingLink
{
    public static bool IsUsableLink(
        bool mechanicallyCoupled,
        bool tightened,
        bool airHoseConnected,
        bool cocksOpenBothSides) =>
        mechanicallyCoupled && tightened && airHoseConnected && cocksOpenBothSides;

    public static CouplerLinkStatus Resolve(
        bool mechanicallyCoupled,
        bool tightened,
        bool airHoseConnected,
        bool cocksOpenBothSides,
        bool muCablePresent,
        bool muCableConnected)
    {
        if (!IsUsableLink(mechanicallyCoupled, tightened, airHoseConnected, cocksOpenBothSides))
        {
            return CouplerLinkStatus.Open;
        }

        if (muCablePresent && !muCableConnected)
        {
            return CouplerLinkStatus.MuWarning;
        }

        return CouplerLinkStatus.Linked;
    }

    /// <summary>True when the end is usable for train continuity (MU warning still counts).</summary>
    public static bool IsUsable(CouplerLinkStatus status) =>
        status is CouplerLinkStatus.Linked or CouplerLinkStatus.MuWarning;
}
