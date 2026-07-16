using UnityEngine;

namespace YardMasterSuite.Monitor;

/// <summary>
/// In-world IMGUI overlay for Monitor Mode telemetry.
/// </summary>
public sealed class MonitorHudDriver : MonoBehaviour
{
    private const float RefreshSeconds = 0.1f;

    private float _elapsed;
    private string _label = "— km/h";

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed < RefreshSeconds)
        {
            return;
        }

        _elapsed = 0f;
        _label = TelemetryReader.CurrentSpeedLabel();
    }

    private void OnGUI()
    {
        const float pad = 12f;
        const float width = 160f;
        const float height = 28f;

        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 16,
            padding = new RectOffset(10, 10, 4, 4),
        };

        GUI.Label(new Rect(pad, pad, width, height), _label, style);
    }
}
