namespace YardMasterSuite.Core;

/// <summary>
/// Usable link = mechanical + tightened + air hose + cocks open both sides.
/// Mid-couple (any order) → Loose. Loco↔loco usable without MU → MuWarning.
/// Loco↔loco with MU → MuTeam. Otherwise usable → Linked.
/// </summary>
public static class CouplingLink
{
    public static bool IsUsableLink(
        bool mechanicallyCoupled,
        bool tightened,
        bool airHoseConnected,
        bool cocksOpenBothSides) =>
        mechanicallyCoupled && tightened && airHoseConnected && cocksOpenBothSides;

    /// <summary>
    /// True when something is started on this end but the tow link is not fully ready
    /// (air-only, metal-only, this cock open, MU early, etc. — any order).
    /// </summary>
    public static bool HasMidCoupleProgress(
        bool mechanicallyCoupled,
        bool tightened,
        bool airHoseConnected,
        bool cocksOpenBothSides,
        bool cockOpenThisEnd,
        bool muCableConnected) =>
        mechanicallyCoupled
        || tightened
        || airHoseConnected
        || cocksOpenBothSides
        || cockOpenThisEnd
        || muCableConnected;

    public static CouplerLinkStatus Resolve(
        bool mechanicallyCoupled,
        bool tightened,
        bool airHoseConnected,
        bool cocksOpenBothSides,
        bool cockOpenThisEnd,
        bool muCablePresent,
        bool muCableConnected)
    {
        if (IsUsableLink(mechanicallyCoupled, tightened, airHoseConnected, cocksOpenBothSides))
        {
            if (muCablePresent)
            {
                return muCableConnected
                    ? CouplerLinkStatus.MuTeam
                    : CouplerLinkStatus.MuWarning;
            }

            return CouplerLinkStatus.Linked;
        }

        if (HasMidCoupleProgress(
                mechanicallyCoupled,
                tightened,
                airHoseConnected,
                cocksOpenBothSides,
                cockOpenThisEnd,
                muCableConnected))
        {
            return CouplerLinkStatus.Loose;
        }

        return CouplerLinkStatus.Open;
    }

    /// <summary>True when the end is usable for train continuity (MU open still counts).</summary>
    public static bool IsUsable(CouplerLinkStatus status) =>
        status is CouplerLinkStatus.Linked
            or CouplerLinkStatus.MuWarning
            or CouplerLinkStatus.MuTeam;
}
