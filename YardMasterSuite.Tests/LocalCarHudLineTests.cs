using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LocalCarHudLineTests
{
    [Fact]
    public void Format_joins_inspect_segments_without_pipe_or_car_number()
    {
        var line = LocalCarHudLine.Format(
            "Handbrake 1",
            "Couplers F+ R-",
            "Job FH-12",
            "Track SM-O6I",
            "46t · train 184t");
        Assert.Equal(
            "Handbrake 1  |  Couplers F+ R-  |  Job FH-12  |  Track SM-O6I  |  46t · train 184t",
            line);
        Assert.DoesNotContain("Pipe", line);
        Assert.DoesNotContain("Car XX", line);
        Assert.DoesNotContain("Cargo", line);
    }

    [Fact]
    public void Format_loco_identity_chip()
    {
        var loco = LocalCarHudLine.Format(
            "Handbrake 0",
            "Couplers F- R-",
            job: null,
            track: "Track MF-C3I",
            identityChip: "Loco DE2 · 38t");
        Assert.Equal(
            "Handbrake 0  |  Couplers F- R-  |  Track MF-C3I  |  Loco DE2 · 38t",
            loco);
    }

    [Fact]
    public void Format_omits_blank_job_and_track_segments()
    {
        var line = LocalCarHudLine.Format(
            "Handbrake 0",
            "Couplers F- R-",
            job: null,
            track: null,
            identityChip: "Loco DE2 · 38t");
        Assert.Equal(
            "Handbrake 0  |  Couplers F- R-  |  Loco DE2 · 38t",
            line);
        Assert.DoesNotContain("Job", line);
        Assert.DoesNotContain("Track", line);
    }
}
