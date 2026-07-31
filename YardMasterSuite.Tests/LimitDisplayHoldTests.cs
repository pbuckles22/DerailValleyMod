using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LimitDisplayHoldTests
{
    [Fact]
    public void Tighter_limit_applies_immediately()
    {
        var next = LimitDisplayHold.Step(
            candidateKmh: 50f,
            candidateAuthority: LimitAuthority.Recommended,
            heldKmh: 90f,
            heldAuthority: LimitAuthority.Posted,
            heldAgeSeconds: 0.1f);
        Assert.Equal(50f, next.LimitKmh);
        Assert.Equal(LimitAuthority.Recommended, next.Authority);
    }

    [Fact]
    public void Looser_limit_waits_five_seconds()
    {
        var early = LimitDisplayHold.Step(
            candidateKmh: 90f,
            candidateAuthority: LimitAuthority.Posted,
            heldKmh: 50f,
            heldAuthority: LimitAuthority.Recommended,
            heldAgeSeconds: 2f);
        Assert.Equal(50f, early.LimitKmh);
        Assert.Equal(LimitAuthority.Recommended, early.Authority);

        var later = LimitDisplayHold.Step(
            candidateKmh: 90f,
            candidateAuthority: LimitAuthority.Posted,
            heldKmh: 50f,
            heldAuthority: LimitAuthority.Recommended,
            heldAgeSeconds: LimitDisplayHold.LoosenHoldSeconds);
        Assert.Equal(90f, later.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, later.Authority);
    }

    [Fact]
    public void Recommended_becomes_Posted_immediately_when_board_is_taken()
    {
        var next = LimitDisplayHold.Step(
            candidateKmh: 50f,
            candidateAuthority: LimitAuthority.Posted,
            heldKmh: 50f,
            heldAuthority: LimitAuthority.Recommended,
            heldAgeSeconds: 0f);
        Assert.Equal(50f, next.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, next.Authority);
    }

    [Fact]
    public void Posted_does_not_flip_back_to_Recommended_for_same_number()
    {
        var next = LimitDisplayHold.Step(
            candidateKmh: 50f,
            candidateAuthority: LimitAuthority.Recommended,
            heldKmh: 50f,
            heldAuthority: LimitAuthority.Posted,
            heldAgeSeconds: 0f);
        Assert.Equal(50f, next.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, next.Authority);
    }

    [Fact]
    public void NumberChanged_detects_whole_kmh()
    {
        Assert.True(LimitDisplayHold.NumberChanged(50f, 60f));
        Assert.False(LimitDisplayHold.NumberChanged(50f, 50.2f));
        Assert.True(LimitDisplayHold.NumberChanged(null, 50f));
    }
}

public class SpeedLimitAuthorityFormatTests
{
    [Fact]
    public void Format_appends_posted_and_recommended_labels()
    {
        Assert.Equal(
            "Limit 90 (Posted)",
            SpeedLimitDisplay.Format(90f, LimitTrend.None, LimitAuthority.Posted));
        Assert.Equal(
            "Limit 50 (Recommended)",
            SpeedLimitDisplay.Format(50f, LimitTrend.None, LimitAuthority.Recommended));
    }
}
