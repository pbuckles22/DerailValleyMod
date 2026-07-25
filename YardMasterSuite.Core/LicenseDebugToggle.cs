namespace YardMasterSuite.Core;

/// <summary>F11 license debug: real career licenses ↔ grant all.</summary>
public enum LicenseDebugMode
{
    Real = 0,
    AllGranted = 1,
}

/// <summary>Pure toggle helper for F11 all-licenses override.</summary>
public static class LicenseDebugToggle
{
    public static LicenseDebugMode Next(LicenseDebugMode current) =>
        current == LicenseDebugMode.Real
            ? LicenseDebugMode.AllGranted
            : LicenseDebugMode.Real;

    public static string StatusFragment(LicenseDebugMode mode) =>
        mode == LicenseDebugMode.AllGranted ? "all licenses" : "real licenses";
}
