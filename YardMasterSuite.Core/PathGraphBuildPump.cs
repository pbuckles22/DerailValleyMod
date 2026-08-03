using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Frame-budget progress for Align station mapping (3.5 #1).
/// Pure counters — Unity/DV rebuild steps call this; desk/HUD format the banner.
/// </summary>
public sealed class PathGraphBuildPump
{
    public enum State
    {
        Idle,
        Mapping,
        Ready,
        Failed,
    }

    public State Current { get; private set; } = State.Idle;

    public int TotalUnits { get; private set; }

    public int CompletedUnits { get; private set; }

    public bool IsMapping => Current == State.Mapping;

    /// <summary>0..1 while mapping; 1 when Ready; 0 when Idle/Failed.</summary>
    public float Progress01
    {
        get
        {
            if (Current == State.Ready)
            {
                return 1f;
            }

            if (Current != State.Mapping || TotalUnits <= 0)
            {
                return 0f;
            }

            var p = (float)CompletedUnits / TotalUnits;
            if (p < 0f)
            {
                return 0f;
            }

            if (p > 1f)
            {
                return 1f;
            }

            return p;
        }
    }

    public void Begin(int totalUnits)
    {
        if (totalUnits < 0)
        {
            totalUnits = 0;
        }

        TotalUnits = totalUnits;
        CompletedUnits = 0;
        Current = State.Mapping;
    }

    /// <summary>
    /// How many units the caller may process this tick (does not advance progress).
    /// </summary>
    public int RemainingUnits
    {
        get
        {
            if (Current != State.Mapping)
            {
                return 0;
            }

            var left = TotalUnits - CompletedUnits;
            return left < 0 ? 0 : left;
        }
    }

    /// <summary>Advance completed work; clamps to <see cref="TotalUnits"/>.</summary>
    public void AddCompleted(int units)
    {
        if (Current != State.Mapping || units <= 0)
        {
            return;
        }

        CompletedUnits += units;
        if (CompletedUnits > TotalUnits)
        {
            CompletedUnits = TotalUnits;
        }
    }

    public void Complete()
    {
        CompletedUnits = TotalUnits;
        Current = State.Ready;
    }

    public void Fail()
    {
        Current = State.Failed;
    }

    public void Reset()
    {
        Current = State.Idle;
        TotalUnits = 0;
        CompletedUnits = 0;
    }

    /// <summary>Player-facing banner, e.g. <c>Station mapping… 35%</c>.</summary>
    public static string FormatBanner(float progress01)
    {
        if (float.IsNaN(progress01) || float.IsInfinity(progress01))
        {
            return "Station mapping…";
        }

        if (progress01 < 0f)
        {
            progress01 = 0f;
        }
        else if (progress01 > 1f)
        {
            progress01 = 1f;
        }

        var pct = (int)Math.Round(progress01 * 100f, MidpointRounding.AwayFromZero);
        return $"Station mapping… {pct}%";
    }
}
