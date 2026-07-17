using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// In-world IMGUI overlay for Monitor Mode telemetry (extends left → right).
/// </summary>
public sealed class MonitorHudDriver : MonoBehaviour
{
    private const float RefreshSeconds = 0.1f;

    private float _elapsed;
    private string _label = "— Speed  |  — Grade  |  — Mass  |  — Pipe  |  — Handbrake  |  — Couplers";
    // Primitives only on MonoBehaviour — Core types as fields break Unity AddComponent.
    private bool _hasIntegrityDebug;
    private bool _lastOnCar;
    private string _lastPipe = "";
    private string _lastHandbrake = "";
    private string _lastCoupling = "";

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed < RefreshSeconds)
        {
            return;
        }

        _elapsed = 0f;
        _label = TelemetryReader.CurrentHudLine();
        EmitIntegrityDebugIfNeeded();
    }

    private void EmitIntegrityDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentIntegrityDebugSnapshot();
        IntegrityDebugSnapshot? previous = null;
        if (_hasIntegrityDebug)
        {
            previous = new IntegrityDebugSnapshot(
                _lastOnCar,
                _lastPipe,
                _lastHandbrake,
                _lastCoupling);
        }

        var line = Tier2IntegrityDebug.NextLogMessage(previous, snap);
        _lastOnCar = snap.OnCar;
        _lastPipe = snap.Pipe;
        _lastHandbrake = snap.Handbrake;
        _lastCoupling = snap.Coupling;
        _hasIntegrityDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void OnGUI()
    {
        const float pad = 12f;
        const float height = 28f;
        // Grow right as readouts are added (speed | grade | tonnage | integrity).
        var width = Mathf.Max(520f, 12f + GUI.skin.box.CalcSize(new GUIContent(_label)).x + 24f);

        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 16,
            padding = new RectOffset(10, 10, 4, 4),
        };

        GUI.Label(new Rect(pad, pad, width, height), _label, style);
    }
}
