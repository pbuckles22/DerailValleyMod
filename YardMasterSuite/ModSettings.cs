using System;
using System.Xml.Serialization;
using UnityModManagerNet;

namespace YardMasterSuite;

/// <summary>
/// UMM Mod Manager options (gear icon). Persisted as Settings.xml under the mod folder.
/// The XML names must not collide with <see cref="UnityModManager.ModSettings"/>: both types were
/// mapped as <c>ModSettings</c> in the empty namespace, so every load and every save threw
/// (0.6.50 Player.log) — options silently fell back to defaults and UMM's own save path blew up.
/// </summary>
[XmlRoot("YardMasterSuiteSettings")]
[XmlType("YardMasterSuiteSettings")]
public class ModSettings : UnityModManager.ModSettings, IDrawable
{
    [Draw(
        "Show nearest locos",
        Tooltip = "Amber AR markers for other locos within 600 m (up to 3). Turn off when your main loco is enough and you want less clutter.")]
    public bool ShowNearestLocos = true;

    /// <summary>Never let a serializer failure escape into UMM's Mod Manager GUI.</summary>
    public override void Save(UnityModManager.ModEntry modEntry)
    {
        try
        {
            Save(this, modEntry);
        }
        catch (Exception ex)
        {
            modEntry?.Logger.LogException("Failed to save Yard Master Suite settings:", ex);
        }
    }

    public void OnChange() =>
        Monitor.TelemetryReader.InvalidateLocoRadarCache();
}
