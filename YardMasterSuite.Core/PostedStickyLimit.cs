using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Which posted number the HUD owns between boards (**1.16**).
/// <para>
/// A restriction is released only by <b>passing</b> a new board, never by another board merely
/// becoming the nearest one behind us. 0.5.50 derail: after taking <c>'4 -2.1'=40</c>, a <c>'6'=60</c>
/// board 273 m back became "nearest behind" and raised Limit to 60 mid-descent; the player was
/// still in the 40 zone and stress-derailed at 60 km/h.
/// </para>
/// </summary>
public static class PostedStickyLimit
{
    /// <summary>
    /// <paramref name="takenKmh"/> is a board whose tires we just passed (authoritative).
    /// <paramref name="seedKmh"/> is the nearest board behind — used only to seed a cold start
    /// (fresh session, teleport, re-railed), never to overwrite a live sticky.
    /// </summary>
    public static float? Resolve(float? sticky, float? takenKmh, float? seedKmh) =>
        takenKmh ?? sticky ?? seedKmh;
}

/// <summary>
/// Detects the ahead → behind transition that means "we just passed this board".
/// Keyed by board instance id; only governing boards should be observed.
/// </summary>
public sealed class BoardTakeDetector
{
    private readonly Dictionary<int, bool> _wasAhead = new();

    /// <summary>Board km/h when this observation completes a pass, else null.</summary>
    public float? Observe(int boardId, float kmh, float alongMeters)
    {
        var ahead = alongMeters > 0f;
        var seenAhead = _wasAhead.TryGetValue(boardId, out var wasAhead) && wasAhead;
        _wasAhead[boardId] = ahead;
        return !ahead && seenAhead ? kmh : null;
    }

    /// <summary>Forget sides — travel reversed, loco changed, or session left.</summary>
    public void Reset() => _wasAhead.Clear();
}
