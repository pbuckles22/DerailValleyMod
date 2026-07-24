namespace YardMasterSuite.Core;

/// <summary>
/// Whether Monitor HUD / AR may draw. Launcher and menus have no player transform.
/// </summary>
public static class HudWorldSession
{
    public static bool IsActive(bool playerTransformPresent) => playerTransformPresent;
}
