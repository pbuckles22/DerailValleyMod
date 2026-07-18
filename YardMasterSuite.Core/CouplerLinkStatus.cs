namespace YardMasterSuite.Core;

/// <summary>
/// Coupler end state for HUD.
/// Loose = mechanically coupled but chain not tightened.
/// MuWarning = usable link, but loco↔loco blue MU open.
/// </summary>
public enum CouplerLinkStatus
{
    Open = 0,
    Linked = 1,
    MuWarning = 2,
    Loose = 3,
}
