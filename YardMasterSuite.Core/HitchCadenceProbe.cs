using System;
using System.Globalization;
using System.Text;

namespace YardMasterSuite.Core;

/// <summary>
/// Hitch A/B counters + GC samples for Player.log (0.4.20.4+).
/// Call sites must stay allocation-light on the hot path; summary formatting is cadence-gated.
/// </summary>
public static class HitchCadenceProbe
{
    public const float SummaryIntervalSeconds = 5f;

    /// <summary>Log Update spikes at/above this unscaled delta (seconds).</summary>
    public const float SpikeThresholdSeconds = 0.05f;

    private static readonly StringBuilder Line = new(256);

    private static float _nextSummaryAt;
    private static float _lastSpikeLogAt = -999f;

    private static long _baselineGc0;
    private static long _baselineGc1;
    private static long _baselineGc2;
    private static long _baselineHeap;
    private static bool _baselineTaken;

    public static bool Enabled { get; set; } = true;

    public static int PickCount { get; private set; }
    public static int FotRefreshCount { get; private set; }
    public static int FotKillSkipCount { get; private set; }
    public static int FotCacheHitCount { get; private set; }
    public static int LimitFromBoardCount { get; private set; }
    public static int LimitFromGeometryCount { get; private set; }
    public static int LimitNullCount { get; private set; }
    public static int SpikeCount { get; private set; }
    public static int LastRosterCount { get; private set; }
    public static int LastFotRawCount { get; private set; }
    public static double LastFotMs { get; private set; }
    public static float LastSpikeDt { get; private set; }

    public static void ResetForTests()
    {
        Enabled = true;
        PickCount = 0;
        FotRefreshCount = 0;
        FotKillSkipCount = 0;
        FotCacheHitCount = 0;
        LimitFromBoardCount = 0;
        LimitFromGeometryCount = 0;
        LimitNullCount = 0;
        SpikeCount = 0;
        LastRosterCount = 0;
        LastFotRawCount = 0;
        LastFotMs = 0;
        LastSpikeDt = 0;
        _nextSummaryAt = 0;
        _lastSpikeLogAt = -999f;
        _baselineTaken = false;
    }

    public static void NotePick(int rosterCount)
    {
        PickCount++;
        LastRosterCount = rosterCount;
    }

    public static void NoteFotCacheHit() => FotCacheHitCount++;

    public static void NoteFotKillSkip() => FotKillSkipCount++;

    public static void NoteFotRefresh(int rawSignCount, int rosterCount, double elapsedMs)
    {
        FotRefreshCount++;
        LastFotRawCount = rawSignCount;
        LastRosterCount = rosterCount;
        LastFotMs = elapsedMs;
    }

    public static void NoteLimitFromBoard() => LimitFromBoardCount++;

    public static void NoteLimitFromGeometry() => LimitFromGeometryCount++;

    public static void NoteLimitNull() => LimitNullCount++;

    /// <summary>
    /// Returns a spike line when <paramref name="unscaledDelta"/> is large; otherwise null.
    /// Debounced so a single hitch does not spam.
    /// </summary>
    public static string? NoteFrameDelta(float unscaledDelta, float now)
    {
        if (!Enabled || unscaledDelta < SpikeThresholdSeconds)
        {
            return null;
        }

        SpikeCount++;
        LastSpikeDt = unscaledDelta;
        if (now - _lastSpikeLogAt < 0.25f)
        {
            return null;
        }

        _lastSpikeLogAt = now;
        EnsureBaseline();
        var gc0 = GC.CollectionCount(0);
        var gc1 = GC.CollectionCount(1);
        var gc2 = GC.CollectionCount(2);
        var heap = GC.GetTotalMemory(false);
        return FormatSpike(unscaledDelta, gc0, gc1, gc2, heap);
    }

    public static string FormatFotRefreshLine(bool fotEnabled, int rawSignCount, int rosterCount, double elapsedMs)
    {
        EnsureBaseline();
        Line.Clear();
        Line.Append("T2 hitch-fot: enabled=")
            .Append(fotEnabled ? 1 : 0)
            .Append(" raw=")
            .Append(rawSignCount)
            .Append(" roster=")
            .Append(rosterCount)
            .Append(" ms=")
            .Append(elapsedMs.ToString("0.0", CultureInfo.InvariantCulture))
            .Append(" gc0=")
            .Append(GC.CollectionCount(0))
            .Append(" gc1=")
            .Append(GC.CollectionCount(1))
            .Append(" gc2=")
            .Append(GC.CollectionCount(2))
            .Append(" heapMB=")
            .Append(FormatHeapMb(GC.GetTotalMemory(false)));
        return Line.ToString();
    }

    public static string FormatSummary(
        float now,
        bool fotEnabled,
        bool hudDraw,
        int rosterCount)
    {
        EnsureBaseline();
        var gc0 = GC.CollectionCount(0);
        var gc1 = GC.CollectionCount(1);
        var gc2 = GC.CollectionCount(2);
        var heap = GC.GetTotalMemory(false);

        Line.Clear();
        Line.Append("T2 hitch-sum: t=")
            .Append(now.ToString("0.0", CultureInfo.InvariantCulture))
            .Append(" fot=")
            .Append(fotEnabled ? 1 : 0)
            .Append(" draw=")
            .Append(hudDraw ? 1 : 0)
            .Append(" pick=")
            .Append(PickCount)
            .Append(" fotRef=")
            .Append(FotRefreshCount)
            .Append(" fotCache=")
            .Append(FotCacheHitCount)
            .Append(" fotKill=")
            .Append(FotKillSkipCount)
            .Append(" limBoard=")
            .Append(LimitFromBoardCount)
            .Append(" limGeom=")
            .Append(LimitFromGeometryCount)
            .Append(" limNull=")
            .Append(LimitNullCount)
            .Append(" roster=")
            .Append(rosterCount)
            .Append(" lastFotMs=")
            .Append(LastFotMs.ToString("0.0", CultureInfo.InvariantCulture))
            .Append(" lastRaw=")
            .Append(LastFotRawCount)
            .Append(" spikes=")
            .Append(SpikeCount)
            .Append(" lastSpikeDt=")
            .Append(LastSpikeDt.ToString("0.000", CultureInfo.InvariantCulture))
            .Append(" dGc0=")
            .Append(gc0 - _baselineGc0)
            .Append(" dGc1=")
            .Append(gc1 - _baselineGc1)
            .Append(" dGc2=")
            .Append(gc2 - _baselineGc2)
            .Append(" heapMB=")
            .Append(FormatHeapMb(heap))
            .Append(" dHeapMB=")
            .Append(FormatHeapMb(heap - _baselineHeap));
        return Line.ToString();
    }

    /// <summary>
    /// Emits a summary line when the interval elapses. Returns the line or null.
    /// </summary>
    public static string? TickSummary(float now, bool fotEnabled, bool hudDraw, int rosterCount)
    {
        if (!Enabled)
        {
            return null;
        }

        if (_nextSummaryAt <= 0f)
        {
            _nextSummaryAt = now + SummaryIntervalSeconds;
            EnsureBaseline();
            return null;
        }

        if (now < _nextSummaryAt)
        {
            return null;
        }

        _nextSummaryAt = now + SummaryIntervalSeconds;
        return FormatSummary(now, fotEnabled, hudDraw, rosterCount);
    }

    public static string FormatImmediateDump(float now, bool fotEnabled, bool hudDraw, int rosterCount)
    {
        var body = FormatSummary(now, fotEnabled, hudDraw, rosterCount);
        const string prefix = "T2 hitch-sum: ";
        if (body.StartsWith(prefix, StringComparison.Ordinal))
        {
            return "T2 hitch-dump: " + body.Substring(prefix.Length);
        }

        return "T2 hitch-dump: " + body;
    }

    private static void EnsureBaseline()
    {
        if (_baselineTaken)
        {
            return;
        }

        _baselineGc0 = GC.CollectionCount(0);
        _baselineGc1 = GC.CollectionCount(1);
        _baselineGc2 = GC.CollectionCount(2);
        _baselineHeap = GC.GetTotalMemory(false);
        _baselineTaken = true;
    }

    private static string FormatSpike(float dt, int gc0, int gc1, int gc2, long heap)
    {
        Line.Clear();
        Line.Append("T2 hitch-spike: dt=")
            .Append(dt.ToString("0.000", CultureInfo.InvariantCulture))
            .Append(" gc0=")
            .Append(gc0)
            .Append("(+")
            .Append(gc0 - _baselineGc0)
            .Append(") gc1=")
            .Append(gc1)
            .Append("(+")
            .Append(gc1 - _baselineGc1)
            .Append(") gc2=")
            .Append(gc2)
            .Append("(+")
            .Append(gc2 - _baselineGc2)
            .Append(") heapMB=")
            .Append(FormatHeapMb(heap));
        return Line.ToString();
    }

    private static string FormatHeapMb(long bytes) =>
        (bytes / (1024d * 1024d)).ToString("0.0", CultureInfo.InvariantCulture);
}
