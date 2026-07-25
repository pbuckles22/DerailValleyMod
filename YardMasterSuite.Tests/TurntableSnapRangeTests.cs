using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class TurntableSnapRangeTests
{
    [Fact]
    public void IsWithinLockArc_two_meters_at_ten_meter_radius()
    {
        // 2m / 10m ≈ 11.46°
        Assert.True(TurntableSnapRange.IsWithinLockArc(11f, bridgeHalfLengthMeters: 10f));
        Assert.False(TurntableSnapRange.IsWithinLockArc(12f, bridgeHalfLengthMeters: 10f));
    }

    [Fact]
    public void ArcMeters_scales_with_angle_and_radius()
    {
        var meters = TurntableSnapRange.ArcMeters(90f, bridgeHalfLengthMeters: 10f);
        Assert.InRange(meters, 15.7f, 15.8f);
    }
}
