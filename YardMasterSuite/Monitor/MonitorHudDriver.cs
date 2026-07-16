using UnityEngine;

namespace YardMasterSuite.Monitor;

/// <summary>
/// In-world IMGUI overlay for Monitor Mode telemetry (extends left → right).
/// </summary>
public sealed class MonitorHudDriver : MonoBehaviour
{
    private const float RefreshSeconds = 0.1f;

    private float _elapsed;
    private string _label = "— km/h  |  — %  |  — t";

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed < RefreshSeconds)
        {
            return;
        }

        _elapsed = 0f;
        _label = TelemetryReader.CurrentHudLine();
    }

    private void OnGUI()
    {
        const float pad = 12f;
        const float height = 28f;
        // Grow right as readouts are added (speed | grade | tonnage).
        var width = Mathf.Max(320f, 12f + GUI.skin.box.CalcSize(new GUIContent(_label)).x + 24f);

        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 16,
            padding = new RectOffset(10, 10, 4, 4),
        };

        GUI.Label(new Rect(pad, pad, width, height), _label, style);
    }
}
