using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Perf smoke @ 0.6.33: look-at loco → T2 power/limit loco + scan=1600m → ~2 s freeze.
/// Top loco bar must require standing/on-train, not mere look-at.
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
}
