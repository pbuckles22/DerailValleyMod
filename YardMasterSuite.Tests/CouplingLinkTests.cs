using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class CouplingLinkTests
{
    [Fact]
    public void IsUsableLink_false_when_any_required_part_missing()
    {
        Assert.False(CouplingLink.IsUsableLink(false, true, true, true));
        Assert.False(CouplingLink.IsUsableLink(true, false, true, true));
        Assert.False(CouplingLink.IsUsableLink(true, true, false, true));
        Assert.False(CouplingLink.IsUsableLink(true, true, true, false));
    }

    [Fact]
    public void IsUsableLink_true_without_mu()
    {
        Assert.True(CouplingLink.IsUsableLink(true, true, true, true));
    }

    [Fact]
    public void Resolve_open_only_when_fully_clear()
    {
        Assert.Equal(
            CouplerLinkStatus.Open,
            CouplingLink.Resolve(
                false, false, false, false, cockOpenThisEnd: false,
                muCablePresent: true, muCableConnected: false));
    }

    [Theory]
    [InlineData(false, false, true, false, false, false)]
    [InlineData(true, false, false, false, false, false)]
    [InlineData(true, true, false, false, false, false)]
    [InlineData(true, true, true, false, false, false)]
    [InlineData(false, false, false, false, true, false)]
    [InlineData(false, false, false, false, false, true)]
    [InlineData(true, false, true, true, true, false)]
    public void Resolve_loose_for_any_mid_couple_order(
        bool mech,
        bool tight,
        bool air,
        bool cocksBoth,
        bool cockThis,
        bool muConnected)
    {
        Assert.Equal(
            CouplerLinkStatus.Loose,
            CouplingLink.Resolve(
                mech, tight, air, cocksBoth, cockThis,
                muCablePresent: true, muCableConnected: muConnected));
    }

    [Fact]
    public void Resolve_mu_warning_when_usable_loco_pair_mu_open()
    {
        Assert.Equal(
            CouplerLinkStatus.MuWarning,
            CouplingLink.Resolve(
                true, true, true, true, cockOpenThisEnd: true,
                muCablePresent: true, muCableConnected: false));
    }

    [Fact]
    public void Resolve_mu_team_when_usable_loco_pair_mu_connected()
    {
        Assert.Equal(
            CouplerLinkStatus.MuTeam,
            CouplingLink.Resolve(
                true, true, true, true, cockOpenThisEnd: true,
                muCablePresent: true, muCableConnected: true));
    }

    [Fact]
    public void Resolve_linked_white_when_usable_and_mu_not_required()
    {
        Assert.Equal(
            CouplerLinkStatus.Linked,
            CouplingLink.Resolve(
                true, true, true, true, cockOpenThisEnd: true,
                muCablePresent: false, muCableConnected: false));
    }

    [Fact]
    public void IsUsable_true_for_tow_ready_states()
    {
        Assert.True(CouplingLink.IsUsable(CouplerLinkStatus.Linked));
        Assert.True(CouplingLink.IsUsable(CouplerLinkStatus.MuWarning));
        Assert.True(CouplingLink.IsUsable(CouplerLinkStatus.MuTeam));
        Assert.False(CouplingLink.IsUsable(CouplerLinkStatus.Open));
        Assert.False(CouplingLink.IsUsable(CouplerLinkStatus.Loose));
    }
}
