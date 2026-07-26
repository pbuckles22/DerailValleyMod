namespace YardMasterSuite.Core;

/// <summary>
/// Coupler end state for HUD.
/// Open = clear. Loose = tow link unfinished (red).
/// Linked = tow-ready when MU not required (white +).
/// MuWarning = loco↔loco tow-ready, MU open (yellow *).
/// MuTeam = loco↔loco fully coupled with MU (blue +).
/// </summary>
public enum CouplerLinkStatus
{
    Open = 0,
    Linked = 1,
    MuWarning = 2,
    Loose = 3,
    MuTeam = 4,
}
