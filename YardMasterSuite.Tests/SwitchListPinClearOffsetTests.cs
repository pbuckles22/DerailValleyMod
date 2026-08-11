using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SwitchListPinClearOffsetTests
{
    [Fact]
    public void De2ClearPastMeters_is_eighteen()
    {
        // Solo DE2 baseline: ~15 m loco + 3 m throw margin — not dynamic tip span.
        Assert.Equal(18f, SwitchListPinClearOffset.De2ClearPastMeters);
        Assert.Equal(18f, SwitchListPinClearOffset.ClearedPastMeters(7f));
        Assert.Equal(18f, SwitchListPinClearOffset.ClearedPastMeters(80f));
    }

    /// <summary>
    /// Smoke: CLEARED only after cab is ≥18 m past frog along frog→departure dir
    /// (not mid-switch; not tip-bbox projection).
    /// </summary>
    [Fact]
    public void Smoke_CabPastAlong_RequiresDe2ClearDistance()
    {
        const float need = SwitchListPinClearOffset.De2ClearPastMeters;

        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            SwitchListPinClearOffset.EvaluateCabPastAlong(
                0f, 0f, cabX: 0f, cabZ: 5f, dirX: 0f, dirZ: 1f, pastMeters: need));

        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            SwitchListPinClearOffset.EvaluateCabPastAlong(
                0f, 0f, cabX: 0f, cabZ: 17.9f, dirX: 0f, dirZ: 1f, pastMeters: need));

        Assert.Equal(
            ConsistClearanceStatus.Cleared,
            SwitchListPinClearOffset.EvaluateCabPastAlong(
                0f, 0f, cabX: 0f, cabZ: need, dirX: 0f, dirZ: 1f, pastMeters: need));
    }

    /// <summary>
    /// Approach side of frog→dest half-plane must Fouling (343 m Cleared bug: inverted dir).
    /// </summary>
    [Fact]
    public void Smoke_ApproachSide_Of_FrogToDest_Is_Fouling()
    {
        // Dir frog → dest (+Z). Cab still on approach (−Z) → negative projection.
        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            SwitchListPinClearOffset.EvaluateCabPastAlong(
                0f, 0f, cabX: 0f, cabZ: -343f, dirX: 0f, dirZ: 1f,
                pastMeters: SwitchListPinClearOffset.De2ClearPastMeters));
    }

    [Fact]
    public void ClearOffsetMeters_solo_and_consist()
    {
        Assert.Equal(22f, SwitchListPinClearOffset.ClearOffsetMeters(20f), 3);
        Assert.Equal(82f, SwitchListPinClearOffset.ClearOffsetMeters(80f), 3);
        Assert.Equal(
            ConsistSwitchClearance.DefaultMarginMeters,
            SwitchListPinClearOffset.ClearOffsetMeters(0f));
        Assert.Equal(
            ConsistSwitchClearance.DefaultMarginMeters,
            SwitchListPinClearOffset.ClearOffsetMeters(-5f));
    }

    [Fact]
    public void OffsetPinXz_moves_past_junction_along_dir()
    {
        var ok = SwitchListPinClearOffset.TryOffsetPinXz(
            jx: 0f, jz: 0f, dirX: 0f, dirZ: 1f, offsetMeters: 22f,
            out var px, out var pz);
        Assert.True(ok);
        Assert.Equal(0f, px, 3);
        Assert.Equal(22f, pz, 3);
    }

    [Fact]
    public void OffsetPinXz_fail_closed_on_zero_dir()
    {
        Assert.False(SwitchListPinClearOffset.TryOffsetPinXz(
            10f, 20f, 0f, 0f, 22f, out var px, out var pz));
        Assert.Equal(10f, px);
        Assert.Equal(20f, pz);
    }

    /// <summary>
    /// Smoke: Arrived against clear-stop — still Fouling at junction center,
    /// Cleared when player is at the offset pin (nose past gates).
    /// </summary>
    [Fact]
    public void Smoke_PinArrive_AgainstClearStop_NotJunctionCenter()
    {
        const float jx = 0f, jz = 0f;
        Assert.True(SwitchListPinClearOffset.TryOffsetPinXz(
            jx, jz, 0f, 1f, 50f, out var pinX, out var pinZ));

        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            ConsistSwitchClearance.EvaluatePinArrive(
                ConsistClearanceStatus.Fouling, pinX, pinZ, jx, jz, 35f));

        Assert.Equal(
            ConsistClearanceStatus.Cleared,
            ConsistSwitchClearance.EvaluatePinArrive(
                ConsistClearanceStatus.Fouling, pinX, pinZ, pinX, pinZ + 1f, 35f));
    }

    [Fact]
    public void TryCorridorDirAfterJunction_frog_to_departure_track()
    {
        var tracks = new[] { "A", "B", "C", "D" };
        // Hop B→C is the pin junction; clear dir = frog → C (not C→D hop).
        var edges = new[]
        {
            new PathEdge("A", "B", cost: 1f),
            new PathEdge("B", "C", "S-0421-SW", 1, 1f),
            new PathEdge("C", "D", cost: 1f),
        };
        float PosX(string id) => id switch { "A" => 0, "B" => 10, "C" => 20, "D" => 20, _ => 0 };
        float PosZ(string id) => id switch { "A" => 0, "B" => 0, "C" => 0, "D" => 50, _ => 0 };
        const float jx = 15f, jz = 0f; // frog between B and C

        Assert.True(SwitchListPinClearOffset.TryCorridorDirAfterJunction(
            tracks, edges, "S-0421-SW", jx, jz, PosX, PosZ, out var dx, out var dz));
        // Frog → C: (20-15, 0-0) = (+5, 0) — not C→D (0,+50).
        Assert.Equal(5f, dx, 3);
        Assert.Equal(0f, dz, 3);
    }

    /// <summary>
    /// Yard dual-branch: same junction twice — dir from frog to departure of the *last* hop
    /// (junction-first conflict), not the first corridor use.
    /// </summary>
    [Fact]
    public void TryCorridorDirAfterJunction_uses_last_hop_when_junction_reused()
    {
        var tracks = new[] { "A", "B", "C", "D" };
        var edges = new[]
        {
            new PathEdge("A", "B", "S-0421-SW", 0, 1f),
            new PathEdge("B", "C", cost: 1f),
            new PathEdge("C", "D", "S-0421-SW", 1, 1f),
        };
        float PosX(string id) => id switch { "A" => 0, "B" => 100, "C" => 100, "D" => 100, _ => 0 };
        float PosZ(string id) => id switch { "A" => 0, "B" => 0, "C" => 50, "D" => 100, _ => 0 };
        const float jx = 100f, jz = 80f;

        Assert.True(SwitchListPinClearOffset.TryCorridorDirAfterJunction(
            tracks, edges, "S-0421-SW", jx, jz, PosX, PosZ, out var dx, out var dz));
        // Last hop C→D: frog → D = (0, 20), not first hop A→B = (0, -80).
        Assert.Equal(0f, dx, 3);
        Assert.Equal(20f, dz, 3);
    }
}
