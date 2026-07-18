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
    public void Resolve_open_when_not_usable()
    {
        Assert.Equal(
            CouplerLinkStatus.Open,
            CouplingLink.Resolve(true, true, false, true, muCablePresent: true, muCableConnected: false));
    }

    [Fact]
    public void Resolve_mu_warning_when_usable_but_blue_open()
    {
        Assert.Equal(
            CouplerLinkStatus.MuWarning,
            CouplingLink.Resolve(true, true, true, true, muCablePresent: true, muCableConnected: false));
    }

    [Fact]
    public void Resolve_linked_when_usable_and_mu_connected_or_not_needed()
    {
        Assert.Equal(
            CouplerLinkStatus.Linked,
            CouplingLink.Resolve(true, true, true, true, muCablePresent: false, muCableConnected: false));
        Assert.Equal(
            CouplerLinkStatus.Linked,
            CouplingLink.Resolve(true, true, true, true, muCablePresent: true, muCableConnected: true));
    }

    [Fact]
    public void IsUsable_true_for_linked_and_mu_warning()
    {
        Assert.True(CouplingLink.IsUsable(CouplerLinkStatus.Linked));
        Assert.True(CouplingLink.IsUsable(CouplerLinkStatus.MuWarning));
        Assert.False(CouplingLink.IsUsable(CouplerLinkStatus.Open));
    }
}
