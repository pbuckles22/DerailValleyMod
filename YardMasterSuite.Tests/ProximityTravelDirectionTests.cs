using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ProximityTravelDirectionTests
{
    [Theory]
    [InlineData(0f, ProximityTravelDirection.Reverse)]
    [InlineData(0.49f, ProximityTravelDirection.Reverse)]
    [InlineData(0.5f, ProximityTravelDirection.Neutral)]
    [InlineData(0.51f, ProximityTravelDirection.Forward)]
    [InlineData(1f, ProximityTravelDirection.Forward)]
    public void FromReverser_matches_dv_neutral_half(float value, ProximityTravelDirection expected)
    {
        Assert.Equal(expected, ProximityTravelDirectionGate.FromReverser(value));
    }

    [Fact]
    public void FromReverser_null_is_unknown()
    {
        Assert.Equal(ProximityTravelDirection.Unknown, ProximityTravelDirectionGate.FromReverser(null));
        Assert.Equal(ProximityTravelDirection.Unknown, ProximityTravelDirectionGate.FromReverser(float.NaN));
    }

    [Theory]
    [InlineData(ProximityTravelDirection.Reverse, true)]
    [InlineData(ProximityTravelDirection.Forward, true)]
    [InlineData(ProximityTravelDirection.Neutral, false)]
    [InlineData(ProximityTravelDirection.Unknown, false)]
    public void ShouldShowChip_only_when_in_gear(ProximityTravelDirection direction, bool expected)
    {
        Assert.Equal(expected, ProximityTravelDirectionGate.ShouldShowChip(direction));
    }

    [Fact]
    public void ChipLabel_front_or_rear()
    {
        Assert.Equal("Rear", ProximityTravelDirectionGate.ChipLabel(ProximityTravelDirection.Reverse));
        Assert.Equal("Front", ProximityTravelDirectionGate.ChipLabel(ProximityTravelDirection.Forward));
    }

    [Fact]
    public void UseFrontTip_only_when_forward()
    {
        Assert.False(ProximityTravelDirectionGate.UseFrontTip(ProximityTravelDirection.Reverse));
        Assert.True(ProximityTravelDirectionGate.UseFrontTip(ProximityTravelDirection.Forward));
        Assert.False(ProximityTravelDirectionGate.UseFrontTip(ProximityTravelDirection.Neutral));
    }
}
