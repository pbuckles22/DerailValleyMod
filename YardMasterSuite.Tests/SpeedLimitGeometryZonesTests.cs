using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// A116 deep-dive: the geometry-ahead scan reuses this exact zone finder to synthesize an
/// <c>AheadBoard</c> from curve radius alone, so a route-ahead restriction can be caught before its
/// posted-board sign prop streams in. <see cref="SpeedLimitGeometryZones.TryGoverningZone"/> must not
/// only find the tightest sustained zone (already covered by <c>GoverningLimitKmh</c>) but also report
/// exactly where it starts and ends along the arc, in the units the caller measured it in.
/// </summary>
public class SpeedLimitGeometryZonesTests
{
    [Fact]
    public void No_zone_when_arcs_empty()
    {
        var found = SpeedLimitGeometryZones.TryGoverningZone(
            new List<SpeedLimitGeometryZones.ArcSample>(),
            out _, out _, out _);
        Assert.False(found);
    }

    [Fact]
    public void Single_sustained_zone_reports_its_own_span()
    {
        // 200 m at a radius mapping to 40 km/h (95–130 m bucket).
        var arcs = new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 110f, lengthMeters: 200f),
        };

        var found = SpeedLimitGeometryZones.TryGoverningZone(
            arcs, out var limitKmh, out var start, out var end);

        Assert.True(found);
        Assert.Equal(40f, limitKmh);
        Assert.Equal(0f, start);
        Assert.Equal(200f, end);
    }

    [Fact]
    public void Tightest_zone_wins_over_a_looser_one_and_reports_its_own_offset()
    {
        // 500 m open (80 kmh bucket), then 100 m of a 30 kmh curve further along the track.
        var arcs = new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 500f, lengthMeters: 500f),
            new(radiusMeters: 80f, lengthMeters: 100f),
        };

        var found = SpeedLimitGeometryZones.TryGoverningZone(
            arcs, out var limitKmh, out var start, out var end);

        Assert.True(found);
        Assert.Equal(30f, limitKmh);
        Assert.Equal(500f, start);
        Assert.Equal(600f, end);
    }

    [Fact]
    public void Micro_kink_shorter_than_min_zone_length_is_ignored()
    {
        // Only 5 m of a tight curve — shorter than MinZoneLengthMeters (15 m) — must not win; the
        // sustained 80 kmh zones either side of it are what should govern instead.
        var arcs = new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 500f, lengthMeters: 100f),
            new(radiusMeters: 40f, lengthMeters: 5f),
            new(radiusMeters: 500f, lengthMeters: 100f),
        };

        var found = SpeedLimitGeometryZones.TryGoverningZone(
            arcs, out var limitKmh, out var start, out var end);

        Assert.True(found);
        Assert.Equal(80f, limitKmh);
        Assert.Equal(0f, start);
        Assert.Equal(100f, end);
    }

    [Fact]
    public void Two_equally_tight_zones_report_the_earliest_one()
    {
        // Two separate 30 kmh zones — the scan must anchor on the first (earliest-reached) one so
        // the synthetic ahead-board never sits farther out than the real restriction requires.
        var arcs = new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 80f, lengthMeters: 50f),
            new(radiusMeters: 500f, lengthMeters: 300f),
            new(radiusMeters: 80f, lengthMeters: 50f),
        };

        var found = SpeedLimitGeometryZones.TryGoverningZone(
            arcs, out var limitKmh, out var start, out var end);

        Assert.True(found);
        Assert.Equal(30f, limitKmh);
        Assert.Equal(0f, start);
        Assert.Equal(50f, end);
    }

    [Fact]
    public void Result_matches_governing_limit_kmh_for_the_same_arcs()
    {
        var arcs = new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 300f, lengthMeters: 400f),
            new(radiusMeters: 60f, lengthMeters: 80f),
            new(radiusMeters: 900f, lengthMeters: 200f),
        };

        var expected = SpeedLimitGeometryZones.GoverningLimitKmh(arcs);
        var found = SpeedLimitGeometryZones.TryGoverningZone(arcs, out var limitKmh, out _, out _);

        Assert.True(found);
        Assert.Equal(expected, limitKmh);
    }
}
