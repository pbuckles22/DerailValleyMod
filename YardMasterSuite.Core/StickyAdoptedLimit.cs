using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Session sticky for a look-ahead Recommended adopt (**1.16**).
/// Tightens immediately; never replaces a tighter sticky with a looser Resolve result
/// (that clobber caused 30↔60 while the HUD loosen-hold still showed 30).
/// Clears only when the posted board is taken (posted ≤ sticky).
/// </summary>
public static class StickyAdoptedLimit
{
    public readonly struct State
    {
        public State(float? kmh, float? gradePercent)
        {
            Kmh = kmh;
            GradePercent = gradePercent;
        }

        public float? Kmh { get; }
        public float? GradePercent { get; }
    }

    /// <summary>
    /// <paramref name="adoptedKmh"/> from this frame's Resolve (null when not adopting).
    /// <paramref name="gradePercent"/> current grade when adopting / refreshing sticky.
    /// </summary>
    public static State Step(
        float? previousKmh,
        float? previousGradePercent,
        float? adoptedKmh,
        float? postedKmh,
        float gradePercent)
    {
        if (adoptedKmh is float ak)
        {
            if (previousKmh is float prev && ak > prev + 0.5f)
            {
                // Looser adopt must not clobber a tighter sticky.
                return new State(prev, previousGradePercent);
            }

            var grade = previousKmh is float pk
                        && Math.Abs(pk - ak) < 0.5f
                        && previousGradePercent is float pg
                ? Math.Min(pg, gradePercent)
                : gradePercent;
            return new State(ak, grade);
        }

        if (postedKmh is float posted
            && previousKmh is float sticky
            && Round(posted) <= Round(sticky))
        {
            // Board taken — sticky Limit is now Posted.
            return new State(null, null);
        }

        // Scan miss / temporary non-adopt — keep sticky + adopt-time grade.
        return new State(previousKmh, previousGradePercent);
    }

    private static int Round(float kmh) =>
        (int)Math.Round(kmh, MidpointRounding.AwayFromZero);
}
