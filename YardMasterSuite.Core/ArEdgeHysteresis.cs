using System;

namespace YardMasterSuite.Core;

/// <summary>Left/right turn-cue side for behind-camera sticky markers.</summary>
public enum ArHorizontalEdge
{
    None = 0,
    Left = 1,
    Right = 2,
}

/// <summary>
/// Angular deadband so looking almost directly away does not flip L↔R every frame (Bundle A.3).
/// Uses atan2(viewRight, −viewForward) so thresholds are distance-independent.
/// </summary>
public static class ArEdgeHysteresis
{
    /// <summary>~7° clear bearing past dead-ahead to pick an initial side.</summary>
    public const float EnterRadians = 0.12f;

    /// <summary>~3° past center required to flip from the held side.</summary>
    public const float HoldRadians = 0.05f;

    /// <summary>
    /// Signed bearing from directly behind the camera: + = target is to the right.
    /// </summary>
    public static float BehindBearingRadians(float viewRight, float viewForward)
    {
        var behindAxis = -viewForward;
        if (behindAxis < 1e-3f)
        {
            behindAxis = 1e-3f;
        }

        return (float)Math.Atan2(viewRight, behindAxis);
    }

    public static ArHorizontalEdge Resolve(
        float viewRight,
        float viewForward,
        ArHorizontalEdge previous,
        float enterRad = EnterRadians,
        float holdRad = HoldRadians)
    {
        if (holdRad > enterRad)
        {
            holdRad = enterRad;
        }

        var bearing = BehindBearingRadians(viewRight, viewForward);

        switch (previous)
        {
            case ArHorizontalEdge.Left:
                return bearing > holdRad ? ArHorizontalEdge.Right : ArHorizontalEdge.Left;
            case ArHorizontalEdge.Right:
                return bearing < -holdRad ? ArHorizontalEdge.Left : ArHorizontalEdge.Right;
            default:
                if (bearing > enterRad)
                {
                    return ArHorizontalEdge.Right;
                }

                if (bearing < -enterRad)
                {
                    return ArHorizontalEdge.Left;
                }

                return ArHorizontalEdge.Left;
        }
    }

    /// <summary>Legacy absolute-lateral resolve (tests / callers that only have viewRight).</summary>
    public static ArHorizontalEdge Resolve(
        float viewRight,
        ArHorizontalEdge previous,
        float enterAbs,
        float holdAbs)
    {
        if (holdAbs > enterAbs)
        {
            holdAbs = enterAbs;
        }

        switch (previous)
        {
            case ArHorizontalEdge.Left:
                return viewRight > holdAbs ? ArHorizontalEdge.Right : ArHorizontalEdge.Left;
            case ArHorizontalEdge.Right:
                return viewRight < -holdAbs ? ArHorizontalEdge.Left : ArHorizontalEdge.Right;
            default:
                if (viewRight > enterAbs)
                {
                    return ArHorizontalEdge.Right;
                }

                if (viewRight < -enterAbs)
                {
                    return ArHorizontalEdge.Left;
                }

                return ArHorizontalEdge.Left;
        }
    }
}
