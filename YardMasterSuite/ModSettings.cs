using UnityModManagerNet;

namespace YardMasterSuite;

/// <summary>UMM Mod Manager options (gear icon). Persisted as Settings.xml under the mod folder.</summary>
public class ModSettings : UnityModManager.ModSettings, IDrawable
{
    [Draw(
        "Show nearest locos",
        Tooltip = "Amber AR markers for other locos within 600 m (up to 3). Turn off when your main loco is enough and you want less clutter.")]
    public bool ShowNearestLocos = true;

    public override void Save(UnityModManager.ModEntry modEntry) =>
        Save(this, modEntry);

    public void OnChange() =>
        Monitor.TelemetryReader.InvalidateLocoRadarCache();
}
