using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LimitDisplayHoldTests
{
    [Fact]
    public void Tighter_limit_applies_immediately()
    {
        var next = LimitDisplayHold.Step(
            candidateKmh: 50f,
            candidateAuthority: LimitAuthority.Posted,
            heldKmh: 90f,
            heldAuthority: LimitAuthority.Posted,
            heldAgeSeconds: 0.1f);
        Assert.Equal(50f, next.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, next.Authority);
    }

    [Fact]
    public void Looser_limit_waits_five_seconds()
    {
        var early = LimitDisplayHold.Step(
            candidateKmh: 90f,
            candidateAuthority: LimitAuthority.Posted,
            heldKmh: 50f,
            heldAuthority: LimitAuthority.Posted,
            heldAgeSeconds: 2f);
        Assert.Equal(50f, early.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, early.Authority);

        var later = LimitDisplayHold.Step(
            candidateKmh: 90f,
            candidateAuthority: LimitAuthority.Posted,
            heldKmh: 50f,
            heldAuthority: LimitAuthority.Posted,
            heldAgeSeconds: LimitDisplayHold.LoosenHoldSeconds);
        Assert.Equal(90f, later.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, later.Authority);
    }

    [Fact]
    public void Geometry_becomes_Posted_immediately_for_same_number()
    {
        var next = LimitDisplayHold.Step(
            candidateKmh: 50f,
            candidateAuthority: LimitAuthority.Posted,
            heldKmh: 50f,
            heldAuthority: LimitAuthority.Geometry,
            heldAgeSeconds: 0f);
        Assert.Equal(50f, next.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, next.Authority);
    }

    [Fact]
    public void Posted_does_not_flip_to_Geometry_for_same_number()
    {
        var next = LimitDisplayHold.Step(
            candidateKmh: 50f,
            candidateAuthority: LimitAuthority.Geometry,
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

    [Fact]
    public void Standstill_keeps_held_limit_across_facing_flip()
    {
        var next = LimitDisplayHold.Step(
            candidateKmh: 80f,
            candidateAuthority: LimitAuthority.Posted,
            heldKmh: 40f,
            heldAuthority: LimitAuthority.Posted,
            heldAgeSeconds: LimitDisplayHold.LoosenHoldSeconds,
            speedKmh: 0f);
        Assert.Equal(40f, next.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, next.Authority);
    }

    [Fact]
    public void Moving_still_allows_loosen_after_hold()
    {
        var next = LimitDisplayHold.Step(
            candidateKmh: 80f,
            candidateAuthority: LimitAuthority.Posted,
            heldKmh: 40f,
            heldAuthority: LimitAuthority.Posted,
            heldAgeSeconds: LimitDisplayHold.LoosenHoldSeconds,
            speedKmh: 20f);
        Assert.Equal(80f, next.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, next.Authority);
    }
}

public class SpeedLimitAuthorityFormatTests
{
    [Fact]
    public void Format_is_plain_limit_number()
    {
        Assert.Equal("Limit 90", SpeedLimitDisplay.Format(90f));
    }
}
