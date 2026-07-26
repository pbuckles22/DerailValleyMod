using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class CouplingDisplayTests
{
    [Fact]
    public void Format_shows_placeholder_when_either_end_unknown()
    {
        Assert.Equal("— Couplers", CouplingDisplay.Format(null, null));
        Assert.Equal("— Couplers", CouplingDisplay.Format(CouplerLinkStatus.Linked, null));
        Assert.Equal("— Couplers", CouplingDisplay.Format(null, CouplerLinkStatus.Open));
    }

    [Theory]
    [InlineData(CouplerLinkStatus.Linked, CouplerLinkStatus.Linked, "Couplers F+ R+")]
    [InlineData(CouplerLinkStatus.MuTeam, CouplerLinkStatus.MuTeam, "Couplers F+ R+")]
    [InlineData(CouplerLinkStatus.Linked, CouplerLinkStatus.Open, "Couplers F+ R-")]
    [InlineData(CouplerLinkStatus.Open, CouplerLinkStatus.Open, "Couplers F- R-")]
    [InlineData(CouplerLinkStatus.Loose, CouplerLinkStatus.Open, "Couplers F* R-")]
    [InlineData(CouplerLinkStatus.MuWarning, CouplerLinkStatus.Linked, "Couplers F*Y R+")]
    [InlineData(CouplerLinkStatus.MuWarning, CouplerLinkStatus.MuTeam, "Couplers F*Y R+")]
    [InlineData(CouplerLinkStatus.Loose, CouplerLinkStatus.MuWarning, "Couplers F* R*Y")]
    public void Format_plain_marks(CouplerLinkStatus front, CouplerLinkStatus rear, string expected)
    {
        Assert.Equal(expected, CouplingDisplay.Format(front, rear));
    }

    [Fact]
    public void FormatHud_locked_color_scheme()
    {
        Assert.Equal(
            $"Couplers <color={CouplingDisplay.NoGoColor}>F*</color> R-",
            CouplingDisplay.FormatHud(CouplerLinkStatus.Loose, CouplerLinkStatus.Open));
        Assert.Equal(
            $"Couplers <color={CouplingDisplay.MuWarningColor}>F*</color> R-",
            CouplingDisplay.FormatHud(CouplerLinkStatus.MuWarning, CouplerLinkStatus.Open));
        Assert.Equal(
            "Couplers F+ R-",
            CouplingDisplay.FormatHud(CouplerLinkStatus.Linked, CouplerLinkStatus.Open));
        Assert.Equal(
            $"Couplers <color={CouplingDisplay.MuTeamColor}>F+</color> R-",
            CouplingDisplay.FormatHud(CouplerLinkStatus.MuTeam, CouplerLinkStatus.Open));
    }
}
