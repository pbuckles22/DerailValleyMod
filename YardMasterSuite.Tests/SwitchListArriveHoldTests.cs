using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SwitchListArriveHoldTests
{
    [Fact]
    public void Hold_keeps_arrived_through_brief_foul()
    {
        var hold = new SwitchListArriveHold(holdSeconds: 2f);
        Assert.Equal(
            ConsistClearanceStatus.Cleared,
            hold.Apply(t: 10f, ConsistClearanceStatus.Cleared, "S-0421-SW"));
        Assert.Equal(
            ConsistClearanceStatus.Cleared,
            hold.Apply(t: 11f, ConsistClearanceStatus.Fouling, "S-0421-SW"));
        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            hold.Apply(t: 12.1f, ConsistClearanceStatus.Fouling, "S-0421-SW"));
    }

    [Fact]
    public void Hold_resets_when_pin_changes()
    {
        var hold = new SwitchListArriveHold(holdSeconds: 5f);
        hold.Apply(t: 0f, ConsistClearanceStatus.Cleared, "J1");
        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            hold.Apply(t: 1f, ConsistClearanceStatus.Fouling, "J2"));
    }

    [Fact]
    public void FormatDiag_includes_pin_and_gates()
    {
        var line = SwitchListArriveHold.FormatDiag(
            "S-0421-SW",
            ConsistClearanceStatus.Fouling,
            ConsistClearanceStatus.Fouling,
            past: ConsistClearanceStatus.Cleared,
            nearMeters: 6.2f,
            nearRadius: 35f,
            holding: false);
        Assert.Contains("pin=S-0421-SW", line);
        Assert.Contains("past=Cleared", line);
        Assert.Contains("near=6.2", line);
        Assert.Contains("raw=Fouling", line);
        Assert.Contains("len=20.0m", SwitchListArriveHold.FormatDiag(
            "S-0421-SW",
            ConsistClearanceStatus.Cleared,
            ConsistClearanceStatus.Cleared,
            ConsistClearanceStatus.Fouling,
            nearMeters: 6.2f,
            nearRadius: 35f,
            holding: false,
            consistLengthMeters: 20f,
            offsetMeters: 22f));
        Assert.Contains("offset=22.0m", SwitchListArriveHold.FormatDiag(
            "S-0421-SW",
            ConsistClearanceStatus.Cleared,
            ConsistClearanceStatus.Cleared,
            ConsistClearanceStatus.Fouling,
            6.2f,
            35f,
            false,
            20f,
            22f));
    }
}
