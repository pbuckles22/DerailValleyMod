namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 personal map-position checks.
/// </summary>
public readonly struct PositionDebugSnapshot
{
    public PositionDebugSnapshot(int? x, int? z)
    {
        X = x;
        Z = z;
    }

    public int? X { get; }
    public int? Z { get; }

    public string FormatFragment() =>
        X is null || Z is null
            ? "— Pos"
            : PositionDisplay.Format(X, Z);
}

public static class Tier2PositionDebug
{
    public const string Prefix = "T2 pos";
    public const int ChangeThresholdUnits = 50;

    public static string? NextLogMessage(PositionDebugSnapshot? previous, PositionDebugSnapshot current)
    {
        if (previous is null)
        {
            return $"{Prefix} init: {current.FormatFragment()}";
        }

        var prior = previous.Value;
        if (prior.X == current.X && prior.Z == current.Z)
        {
            return null;
        }

        if (prior.X is int ax && prior.Z is int az
            && current.X is int bx && current.Z is int bz)
        {
            var dx = System.Math.Abs(bx - ax);
            var dz = System.Math.Abs(bz - az);
            if (dx < ChangeThresholdUnits && dz < ChangeThresholdUnits)
            {
                return null;
            }
        }

        return $"{Prefix} change: {current.FormatFragment()}";
    }
}
