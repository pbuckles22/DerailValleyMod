using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SwitchListPlannerTests
{
    private static JobSummary Freight(
        string originTrack,
        string destTrack,
        bool turnAround = false,
        string? turntable = null) =>
        new()
        {
            JobId = "SM-FH-12",
            JobTypeLabel = "FH",
            OriginYardId = "CS",
            DestYardId = "SM",
            OriginTrackId = originTrack,
            DestTrackId = destTrack,
            NeedsTurnAround = turnAround,
            TurntableTrackId = turntable,
        };

    [Fact]
    public void Build_freight_prep_transit_delivery()
    {
        var steps = SwitchListPlanner.Build(Freight("CS-A1L", "SM-C5O"));
        Assert.NotNull(steps);
        Assert.Equal(3, steps!.Count);
        Assert.Equal(SwitchListStepKind.Prep, steps[0].Kind);
        Assert.Equal("CS-A1L", steps[0].DestTrackId);
        Assert.Equal("Prep → CS-A1L", steps[0].Label);
        Assert.Equal(SwitchListStepKind.Transit, steps[1].Kind);
        Assert.Equal("SM-C5O", steps[1].DestTrackId);
        Assert.Equal(SwitchListStepKind.Delivery, steps[2].Kind);
        Assert.Equal("SM-C5O", steps[2].DestTrackId);
        Assert.Equal(1, steps[0].Index);
        Assert.Equal(3, steps[2].Index);
    }

    [Fact]
    public void Build_inserts_turnaround_when_flagged()
    {
        var steps = SwitchListPlanner.Build(
            Freight("CS-A1L", "SM-C5O", turnAround: true, turntable: "CS-TT"));
        Assert.NotNull(steps);
        Assert.Equal(4, steps!.Count);
        Assert.Equal(SwitchListStepKind.Prep, steps[0].Kind);
        Assert.Equal(SwitchListStepKind.TurnAround, steps[1].Kind);
        Assert.Equal("CS-TT", steps[1].DestTrackId);
        Assert.Equal("Turn around → CS-TT", steps[1].Label);
        Assert.Equal(SwitchListStepKind.Transit, steps[2].Kind);
        Assert.Equal(SwitchListStepKind.Delivery, steps[3].Kind);
    }

    [Fact]
    public void Build_fail_closed_without_tracks()
    {
        Assert.Null(SwitchListPlanner.Build(new JobSummary { JobId = "X" }));
        Assert.Null(SwitchListPlanner.Build(Freight("", "SM-C5O")));
        Assert.Null(SwitchListPlanner.Build(Freight("CS-A1L", "  ")));
        Assert.Null(SwitchListPlanner.Build(
            Freight("CS-A1L", "SM-C5O", turnAround: true, turntable: null)));
    }

    [Fact]
    public void Build_uses_arrival_track_for_transit_when_set()
    {
        var job = Freight("CS-A1L", "SM-C5O");
        job = new JobSummary
        {
            JobId = job.JobId,
            OriginTrackId = job.OriginTrackId,
            DestTrackId = job.DestTrackId,
            DestYardId = "SM",
            DestArrivalTrackId = "SM-I1P",
        };
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.Equal("SM-I1P", steps![1].DestTrackId);
        Assert.Equal("SM-C5O", steps[2].DestTrackId);
    }

    [Fact]
    public void BuildTownTurntable_tt_only()
    {
        var steps = SwitchListPlanner.BuildTownTurntable("SW", "#Y-#S1774#T");
        Assert.NotNull(steps);
        Assert.Single(steps!);
        Assert.Equal(SwitchListStepKind.TurnAround, steps[0].Kind);
        Assert.Equal("#Y-#S1774#T", steps[0].DestTrackId);
        Assert.Equal("SW", steps[0].DestYardId);
    }

    [Fact]
    public void BuildTownTurntable_pivot_then_tt()
    {
        var steps = SwitchListPlanner.BuildTownTurntable("SW", "#Y-#S1774#T", "#Y-#S23#T");
        Assert.NotNull(steps);
        Assert.Equal(2, steps!.Count);
        Assert.Equal(SwitchListStepKind.Transit, steps[0].Kind);
        Assert.Equal("#Y-#S23#T", steps[0].DestTrackId);
        Assert.Equal("Set Forward · Pivot → #Y-#S23#T", steps[0].Label);
        Assert.Equal(SwitchListStepKind.TurnAround, steps[1].Kind);
        Assert.Equal("#Y-#S1774#T", steps[1].DestTrackId);
        Assert.Equal("Set Forward · Turn around → #Y-#S1774#T", steps[1].Label);
    }

    [Fact]
    public void BuildTownTurntable_fail_closed_without_tt()
    {
        Assert.Null(SwitchListPlanner.BuildTownTurntable("SW", null));
        Assert.Null(SwitchListPlanner.BuildTownTurntable("SW", "  "));
    }
}

public class SwitchListSessionTests
{
    [Fact]
    public void Bind_and_align_target_advances()
    {
        var steps = SwitchListPlanner.Build(new JobSummary
        {
            JobId = "HB-FH-1",
            OriginTrackId = "HB-A1L",
            DestTrackId = "FF-C2O",
        });
        Assert.NotNull(steps);

        SwitchListSession.Clear();
        Assert.False(SwitchListSession.HasActive);

        SwitchListSession.Bind("HB-FH-1", steps!);
        Assert.True(SwitchListSession.HasActive);
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.Equal("HB-A1L", SwitchListSession.CurrentAlignTrackId);

        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.Equal("FF-C2O", SwitchListSession.CurrentAlignTrackId);

        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(2, SwitchListSession.CurrentIndex);
        Assert.False(SwitchListSession.TryAdvance());
        Assert.True(SwitchListSession.IsComplete);

        SwitchListSession.Clear();
        Assert.False(SwitchListSession.HasActive);
    }
}
