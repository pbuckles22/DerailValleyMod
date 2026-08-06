using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke FAIL @ 0.6.29 (Yard MF): schematic looked like dotted stubs.
/// Cause: usable <c>#Y</c> mesh inflated AABB → named rails projected under DrawLine 0.5px cull.
/// </summary>
public class YardMiniMapSchematicFocusTests
{
    private const float Panel = 280f;

    [Fact]
    public void Smoke_mf_029_distant_y_must_not_inflate_zoom()
    {
        // Named MF spur ~80 m; office nearby. Distant #Y (inter-city) must not set zoom.
        var named = new List<(float X, float Z)>
        {
            (0f, 0f),
            (80f, 0f),
            (80f, 40f),
            (0f, 40f),
        };
        var landmarks = new List<(float X, float Z)> { (20f, 20f) }; // Office
        var extras = new List<(float X, float Z)>
        {
            (40f, 20f), // nearby #Y — OK to include
            (5000f, 5000f), // distant mesh — must NOT inflate
        };

        var focus = YardMiniMapSchematicFocus.CollectFocusPoints(
            named, extras, landmarks, extraIncludeMeters: 150f);

        Assert.Contains((40f, 20f), focus);
        Assert.DoesNotContain((5000f, 5000f), focus);

        Assert.True(YardMiniMapProjection.TryFitBounds(
            focus, padding: 120f, out var minX, out var maxX, out var minZ, out var maxZ));

        // 80 m spur across ~280px panel (with pad) must stay drawable — not sub-pixel.
        var chord = YardMiniMapSchematicFocus.ProjectedChordPixels(
            0f, 0f, 80f, 0f, minX, maxX, minZ, maxZ, Panel, Panel);
        Assert.True(
            YardMiniMapSchematicFocus.IsDrawableChord(chord),
            $"MF spur projected to {chord:F3}px — DrawLine would cull (< {YardMiniMapSchematicFocus.MinDrawableChordPixels})");
        Assert.True(chord >= 8f, $"MF spur too small on panel ({chord:F1}px) — zoom still too wide");
    }

    [Fact]
    public void Smoke_mf_029_naive_fit_all_points_collapses_short_stubs()
    {
        // Documents the FAIL mode: fitting named + distant #Y shrinks rails toward DrawLine cull.
        var all = new List<(float X, float Z)>
        {
            (0f, 0f),
            (80f, 0f),
            (5000f, 5000f),
        };
        Assert.True(YardMiniMapProjection.TryFitBounds(
            all, padding: 120f, out var minX, out var maxX, out var minZ, out var maxZ));

        var spur = YardMiniMapSchematicFocus.ProjectedChordPixels(
            0f, 0f, 80f, 0f, minX, maxX, minZ, maxZ, Panel, Panel);
        Assert.True(spur < 8f, $"Expected FAIL-mode shrink (spur={spur:F2}px)");

        // Typical short #Y connector (~8 m) falls under OnGUI DrawLine cull.
        var stub = YardMiniMapSchematicFocus.ProjectedChordPixels(
            0f, 0f, 8f, 0f, minX, maxX, minZ, maxZ, Panel, Panel);
        Assert.False(
            YardMiniMapSchematicFocus.IsDrawableChord(stub),
            $"Expected FAIL-mode: 8m stub at {stub:F3}px still drawable");
    }

    [Fact]
    public void Nearby_extra_within_include_radius_joins_focus()
    {
        var named = new List<(float X, float Z)> { (0f, 0f), (100f, 0f) };
        var extras = new List<(float X, float Z)> { (100f, 100f) }; // 100 m north of east end
        var focus = YardMiniMapSchematicFocus.CollectFocusPoints(
            named, extras, landmarks: null, extraIncludeMeters: 150f);
        Assert.Contains((100f, 100f), focus);
    }

    [Fact]
    public void Landmarks_always_in_focus_even_without_named()
    {
        var focus = YardMiniMapSchematicFocus.CollectFocusPoints(
            namedPoints: null,
            extraPoints: new[] { (999f, 999f) },
            landmarks: new[] { (10f, 10f) });
        Assert.Contains((10f, 10f), focus);
        Assert.DoesNotContain((999f, 999f), focus);
    }
}
