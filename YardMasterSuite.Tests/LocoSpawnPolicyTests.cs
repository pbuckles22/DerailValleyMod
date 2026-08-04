using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class LocoSpawnPolicyTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 0, 0)]
    [InlineData(3, 1, 1)]
    [InlineData(3, 2, 2)]
    [InlineData(3, 3, 0)]
    [InlineData(3, -1, 2)]
    public void WrapIndex_cycles(int count, int index, int expected)
    {
        Assert.Equal(expected, LocoSpawnPolicy.WrapIndex(count, index));
    }

    [Theory]
    [InlineData(3, 1, +1, 2)]
    [InlineData(3, 2, +1, 0)]
    [InlineData(3, 0, -1, 2)]
    public void StepIndex_scrolls(int count, int index, int delta, int expected)
    {
        Assert.Equal(expected, LocoSpawnPolicy.StepIndex(count, index, delta));
    }

    [Fact]
    public void Evaluate_requires_livery_and_track()
    {
        Assert.Equal(LocoSpawnAbort.NoLiveries, LocoSpawnPolicy.Evaluate(0, false, false, false));
        Assert.Equal(LocoSpawnAbort.NoTarget, LocoSpawnPolicy.Evaluate(1, false, false, false));
        Assert.Equal(LocoSpawnAbort.Busy, LocoSpawnPolicy.Evaluate(1, true, false, true));
        Assert.Equal(LocoSpawnAbort.None, LocoSpawnPolicy.Evaluate(1, true, false, false));
    }

    [Fact]
    public void FormatPlaceChip_ok_and_blocked()
    {
        Assert.Equal("", LocoSpawnPolicy.FormatPlaceChip(false, "DH4", "SM-A1P", LocoSpawnAbort.None));
        Assert.Equal("SPAWN OK · DH4 · SM-A1P", LocoSpawnPolicy.FormatPlaceChip(true, "DH4", "SM-A1P", LocoSpawnAbort.None));
        Assert.Equal("SPAWN BLOCKED · no track target", LocoSpawnPolicy.FormatPlaceChip(true, "DH4", null, LocoSpawnAbort.NoTarget));
    }

    [Theory]
    [InlineData("LocoDH4", true)]
    [InlineData("LocoDE6", true)]
    [InlineData("HandCar", true)]
    [InlineData("LocoDE6Slug", false)]
    [InlineData("LocoDE6_Relic", false)]
    [InlineData("CabooseRed", false)]
    [InlineData(null, false)]
    public void IsEligibleSpawnLocoId_filters_specials(string? id, bool expected)
    {
        Assert.Equal(expected, LocoSpawnPolicy.IsEligibleSpawnLocoId(id));
    }
}

public class LocoSpawnPlaceSessionTests
{
    public LocoSpawnPlaceSessionTests()
    {
        LocoSpawnPlaceSession.Clear();
    }

    [Fact]
    public void Begin_sets_active_and_index()
    {
        LocoSpawnPlaceSession.Begin(2);
        Assert.True(LocoSpawnPlaceSession.IsActive);
        Assert.Equal(2, LocoSpawnPlaceSession.SelectedIndex);
        Assert.True(LocoSpawnPlaceSession.ForceRegularDirection);
    }

    [Fact]
    public void Scroll_and_aim_roundtrip()
    {
        LocoSpawnPlaceSession.Begin(0);
        LocoSpawnPlaceSession.SetSelectedIndex(1);
        LocoSpawnPlaceSession.SetTargetTrack("FF-A1P");
        LocoSpawnPlaceSession.SetAimPoint(1, 2, 3);
        Assert.Equal(1, LocoSpawnPlaceSession.SelectedIndex);
        Assert.Equal("FF-A1P", LocoSpawnPlaceSession.TargetTrackId);
        Assert.True(LocoSpawnPlaceSession.TryGetAimPoint(out var x, out var y, out var z));
        Assert.Equal(1f, x);
        Assert.Equal(2f, y);
        Assert.Equal(3f, z);
        LocoSpawnPlaceSession.ToggleFacing();
        Assert.False(LocoSpawnPlaceSession.ForceRegularDirection);
        LocoSpawnPlaceSession.Clear();
        Assert.False(LocoSpawnPlaceSession.IsActive);
    }
}
