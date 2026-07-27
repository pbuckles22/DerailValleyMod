using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ThreeGateTests
{
    [Fact]
    public void TryApply_aborts_integrity_without_calling_write()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(
            integrityOk: false,
            stateRegistryOk: true,
            safetyOk: true,
            softWrite: () =>
            {
                calls++;
                return true;
            });

        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.Integrity, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void TryApply_aborts_state_registry_without_calling_write()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(
            integrityOk: true,
            stateRegistryOk: false,
            safetyOk: true,
            softWrite: () =>
            {
                calls++;
                return true;
            });

        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.StateRegistry, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void TryApply_aborts_safety_without_calling_write()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(
            integrityOk: true,
            stateRegistryOk: true,
            safetyOk: false,
            softWrite: () =>
            {
                calls++;
                return true;
            });

        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.Safety, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void TryApply_aborts_when_soft_write_returns_false()
    {
        var result = ThreeGate.TryApply(
            integrityOk: true,
            stateRegistryOk: true,
            safetyOk: true,
            softWrite: () => false);

        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.SoftWrite, result.AbortReason);
    }

    [Fact]
    public void TryApply_aborts_when_soft_write_throws()
    {
        var result = ThreeGate.TryApply(
            integrityOk: true,
            stateRegistryOk: true,
            safetyOk: true,
            softWrite: () => throw new InvalidOperationException("boom"));

        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.SoftWrite, result.AbortReason);
    }

    [Fact]
    public void TryApply_applies_when_all_gates_pass_and_write_succeeds()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(
            integrityOk: true,
            stateRegistryOk: true,
            safetyOk: true,
            softWrite: () =>
            {
                calls++;
                return true;
            });

        Assert.True(result.Applied);
        Assert.Equal(ThreeGateAbortReason.None, result.AbortReason);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void TryApply_throws_when_soft_write_null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ThreeGate.TryApply(true, true, true, softWrite: null!));
    }
}
