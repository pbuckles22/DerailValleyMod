using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// First HUD freeze (0.6.33 mini-map smoke): look-at loco → T2 power/limit loco + scan=1600m → ~2 s.
/// 0.6.34 moved cost to cab; top loco bar must stay standing-only. Full look-at warm = FAIL (0.6.36).
/// </summary>
public class UsableLocoTrainGateTests
{
    [Fact]
    public void Smoke_033_look_at_loco_alone_does_not_enable_loco_gadget_bar()
    {
        Assert.False(UsableLocoTrainGate.AllowLocoGadgetBar(hasStandingCar: false));
    }

    [Fact]
    public void Standing_on_train_enables_loco_gadget_bar()
    {
        Assert.True(UsableLocoTrainGate.AllowLocoGadgetBar(hasStandingCar: true));
    }

    [Fact]
    public void Look_at_still_wins_inspect_target_over_standing()
    {
        // Second bar / couplers — unchanged; only top loco HUD is gated.
        Assert.Equal(
            TargetCarSource.LookAt,
            TargetCarSelection.Resolve(hasStandingCar: true, hasLookAtCar: true));
    }

    [Fact]
    public void Smoke_033_look_at_loco_may_schedule_warm_but_not_gadget_bar()
    {
        // Warm flag is permission only — must not imply synchronous ScanPostedBoards on look-at.
        Assert.False(UsableLocoTrainGate.AllowLocoGadgetBar(hasStandingCar: false));
        Assert.True(UsableLocoTrainGate.AllowLimitScanWarm(
            hasStandingCar: false,
            lookAtIsLoco: true));
    }

    [Fact]
    public void Limit_scan_warm_off_when_not_standing_and_not_looking_at_loco()
    {
        Assert.False(UsableLocoTrainGate.AllowLimitScanWarm(
            hasStandingCar: false,
            lookAtIsLoco: false));
    }

    [Fact]
    public void Standing_allows_limit_scan_warm()
    {
        Assert.True(UsableLocoTrainGate.AllowLimitScanWarm(
            hasStandingCar: true,
            lookAtIsLoco: false));
    }
}
