namespace YardMasterSuite.Core;

/// <summary>
/// F5 loco-license debug cycle for 3.1b: real → DH4 only → DH4+DE6 → real.
/// (Game id is <c>DH4</c>, not DE4.)
/// </summary>
public enum LocoLicenseDebugPhase
{
    Real = 0,
    Dh4Only = 1,
    Dh4AndDe6 = 2,
}

/// <summary>Pure next-phase / desired-flag helper for F5 DH4/DE6 license override.</summary>
public static class LocoLicenseDebugCycle
{
    public static LocoLicenseDebugPhase Next(LocoLicenseDebugPhase current) =>
        current switch
        {
            LocoLicenseDebugPhase.Real => LocoLicenseDebugPhase.Dh4Only,
            LocoLicenseDebugPhase.Dh4Only => LocoLicenseDebugPhase.Dh4AndDe6,
            _ => LocoLicenseDebugPhase.Real,
        };

    public static string StatusFragment(LocoLicenseDebugPhase phase) =>
        phase switch
        {
            LocoLicenseDebugPhase.Dh4Only => "DH4 only",
            LocoLicenseDebugPhase.Dh4AndDe6 => "DH4+DE6",
            _ => "real loco licenses",
        };

    public static bool WantDh4(LocoLicenseDebugPhase phase) =>
        phase is LocoLicenseDebugPhase.Dh4Only or LocoLicenseDebugPhase.Dh4AndDe6;

    public static bool WantDe6(LocoLicenseDebugPhase phase) =>
        phase == LocoLicenseDebugPhase.Dh4AndDe6;
}
