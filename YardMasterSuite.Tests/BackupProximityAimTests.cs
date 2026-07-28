using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class BackupProximityAimTests
{
    [Fact]
    public void FrontIntent_matches_loco_forward()
    {
        BackupProximityAim.FrontIntent(0f, 0f, 1f, out var x, out var y, out var z);
        Assert.Equal(0f, x, precision: 4);
        Assert.Equal(0f, y, precision: 4);
        Assert.Equal(1f, z, precision: 4);
    }

    [Fact]
    public void RearIntent_is_opposite_loco_forward()
    {
        BackupProximityAim.RearIntent(0f, 0f, 1f, out var x, out var y, out var z);
        Assert.Equal(0f, x, 3);
        Assert.Equal(0f, y, 3);
        Assert.Equal(-1f, z, 3);
    }

    [Fact]
    public void TipAlignment_prefers_rear_outward()
    {
        BackupProximityAim.RearIntent(0f, 0f, 1f, out var ix, out var iy, out var iz);
        var rear = BackupProximityAim.TipAlignment(0, 0, -1, ix, iy, iz);
        var front = BackupProximityAim.TipAlignment(0, 0, 1, ix, iy, iz);
        Assert.True(rear > front);
        Assert.Equal(1f, rear, 3);
    }

    [Fact]
    public void ApproachCone_rejects_parallel_track_side()
    {
        Assert.True(BackupProximityAim.IsInApproachCone(0, 0, 20, 0, 0, 1));
        Assert.False(BackupProximityAim.IsInApproachCone(20, 0, 0, 0, 0, 1));
    }
}
