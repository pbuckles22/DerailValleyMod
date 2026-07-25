using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class LicenseDebugToggleTests
{
    [Theory]
    [InlineData(LicenseDebugMode.Real, LicenseDebugMode.AllGranted)]
    [InlineData(LicenseDebugMode.AllGranted, LicenseDebugMode.Real)]
    public void Next_toggles_real_and_all(LicenseDebugMode current, LicenseDebugMode expected)
    {
        Assert.Equal(expected, LicenseDebugToggle.Next(current));
    }

    [Fact]
    public void StatusFragment_labels_modes()
    {
        Assert.Equal("all licenses", LicenseDebugToggle.StatusFragment(LicenseDebugMode.AllGranted));
        Assert.Equal("real licenses", LicenseDebugToggle.StatusFragment(LicenseDebugMode.Real));
    }
}
