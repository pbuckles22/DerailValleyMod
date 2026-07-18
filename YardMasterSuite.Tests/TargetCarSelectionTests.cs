using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class TargetCarSelectionTests
{
    [Fact]
    public void Resolve_prefers_look_at_over_standing()
    {
        Assert.Equal(TargetCarSource.LookAt, TargetCarSelection.Resolve(hasStandingCar: true, hasLookAtCar: true));
    }

    [Fact]
    public void Resolve_uses_look_at_when_not_standing()
    {
        Assert.Equal(TargetCarSource.LookAt, TargetCarSelection.Resolve(hasStandingCar: false, hasLookAtCar: true));
    }

    [Fact]
    public void Resolve_none_when_no_standing_or_look_at()
    {
        Assert.Equal(TargetCarSource.None, TargetCarSelection.Resolve(hasStandingCar: false, hasLookAtCar: false));
    }

    [Fact]
    public void Resolve_standing_when_not_looking_at_a_car()
    {
        Assert.Equal(TargetCarSource.Standing, TargetCarSelection.Resolve(hasStandingCar: true, hasLookAtCar: false));
    }
}
