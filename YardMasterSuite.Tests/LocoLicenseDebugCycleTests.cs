using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class LocoLicenseDebugCycleTests
{
    [Theory]
    [InlineData(LocoLicenseDebugPhase.Real, LocoLicenseDebugPhase.Dh4Only)]
    [InlineData(LocoLicenseDebugPhase.Dh4Only, LocoLicenseDebugPhase.Dh4AndDe6)]
    [InlineData(LocoLicenseDebugPhase.Dh4AndDe6, LocoLicenseDebugPhase.Real)]
    public void Next_cycles_real_dh4_both(LocoLicenseDebugPhase current, LocoLicenseDebugPhase expected)
    {
        Assert.Equal(expected, LocoLicenseDebugCycle.Next(current));
    }

    [Theory]
    [InlineData(LocoLicenseDebugPhase.Real, "real loco licenses")]
    [InlineData(LocoLicenseDebugPhase.Dh4Only, "DH4 only")]
    [InlineData(LocoLicenseDebugPhase.Dh4AndDe6, "DH4+DE6")]
    public void StatusFragment_matches_phase(LocoLicenseDebugPhase phase, string expected)
    {
        Assert.Equal(expected, LocoLicenseDebugCycle.StatusFragment(phase));
    }

    [Theory]
    [InlineData(LocoLicenseDebugPhase.Real, false, false)]
    [InlineData(LocoLicenseDebugPhase.Dh4Only, true, false)]
    [InlineData(LocoLicenseDebugPhase.Dh4AndDe6, true, true)]
    public void Desired_flags_for_override_phases(LocoLicenseDebugPhase phase, bool wantDh4, bool wantDe6)
    {
        Assert.Equal(wantDh4, LocoLicenseDebugCycle.WantDh4(phase));
        Assert.Equal(wantDe6, LocoLicenseDebugCycle.WantDe6(phase));
    }
}
