using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Always-on sticky yard (HUD tick) — MF/MFMB fence rules via <see cref="YardMiniMapYardStick"/>.
/// Owns Limit FILO town-change warm. Desk Set Dest Turntable reads <see cref="CurrentYardId"/>.
/// </summary>
internal static class StickyYardHost
{
    private static string? _stickyYard;
    private static readonly List<string> InZoneYards = new();
    private static readonly List<string> FenceSatellites = new();
    private static float _fenceCheckAt;

    public static string? CurrentYardId => _stickyYard;

    public static void Reset()
    {
        _stickyYard = null;
        InZoneYards.Clear();
        FenceSatellites.Clear();
        _fenceCheckAt = 0f;
    }

    /// <summary>Call from world HUD tick (~10 Hz). FoT office fence at most 2 Hz.</summary>
    public static void Tick()
    {
        TelemetryReader.CollectInZoneYardIds(InZoneYards);
        if (InZoneYards.Count == 0)
        {
            if (_stickyYard != null)
            {
                var from = _stickyYard;
                _stickyYard = null;
                FenceSatellites.Clear();
                TelemetryReader.OnLimitFiloTownChanged(from, null);
            }

            return;
        }

        var now = Time.unscaledTime;
        float px = 0f, pz = 0f;
        var havePlayer = TelemetryReader.TryGetPlayerPosition(out px, out _, out pz);
        if (now >= _fenceCheckAt)
        {
            _fenceCheckAt = now + 0.5f;
            FenceSatellites.Clear();
            if (havePlayer)
            {
                for (var i = 0; i < InZoneYards.Count; i++)
                {
                    var id = InZoneYards[i];
                    if (!YardMiniMapYardStick.IsSatelliteYard(id))
                    {
                        continue;
                    }

                    if (!TelemetryReader.TryGetOfficeForYard(id, out var ox, out var oz))
                    {
                        continue;
                    }

                    if (YardMiniMapYardStick.IsInsideOfficeFence(px, pz, ox, oz))
                    {
                        FenceSatellites.Add(id);
                    }
                }
            }
        }

        TelemetryReader.TryGetInZoneYardAndOffice(out var nearest, out _, out _);
        var resolved = YardMiniMapYardStick.Resolve(
            _stickyYard,
            InZoneYards,
            nearest,
            FenceSatellites);

        if (!string.Equals(_stickyYard, resolved, System.StringComparison.OrdinalIgnoreCase))
        {
            LogResolveDetail(resolved, nearest, havePlayer, px, pz);
            TelemetryReader.OnLimitFiloTownChanged(_stickyYard, resolved);
            // First sticky city known — start session map pump (not world-enter spawn).
            if (string.IsNullOrWhiteSpace(_stickyYard)
                && !string.IsNullOrWhiteSpace(resolved)
                && !PathGraphBuilder.HasReadyCache)
            {
                PathGraphBuilder.EnsureMappingStarted();
                Main.Log("T2 path: map warm on sticky city " + resolved);
            }
        }

        _stickyYard = resolved;
    }

    private static void LogResolveDetail(string? resolved, string? nearest, bool havePlayer, float px, float pz)
    {
        var zones = string.Join(",", InZoneYards);
        var fence = FenceSatellites.Count == 0 ? "—" : string.Join(",", FenceSatellites);
        var dist = "—";
        if (havePlayer && TelemetryReader.TryGetOfficeForYard("MFMB", out var ox, out var oz))
        {
            var dx = px - ox;
            var dz = pz - oz;
            dist = ((int)Mathf.Sqrt(dx * dx + dz * dz)).ToString();
        }

        Main.Log(
            "T2 sticky: detail sticky=" + (_stickyYard ?? "—")
            + " → " + (resolved ?? "—")
            + " nearest=" + (nearest ?? "—")
            + " in=[" + zones + "]"
            + " fence=[" + fence + "]"
            + " distMFMB=" + dist);
    }
}
