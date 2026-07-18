using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// In-world IMGUI overlay for Monitor Mode telemetry.
/// Top bar = usable loco-train totals (red null when not usable); second bar = target car.
/// </summary>
public sealed class MonitorHudDriver : MonoBehaviour
{
    private const float RefreshSeconds = 0.1f;
    private const float Pad = 12f;
    private const float Height = 28f;
    private const float Gap = 4f;

    private static readonly Color BarBackground = new(0.12f, 0.12f, 0.12f, 0.82f);
    private static readonly Color NullBarBackground = new(0.55f, 0.08f, 0.08f, 0.85f);

    private float _elapsed;
    private string _trainLabel = TrainHudLine.NullLine();
    private string? _localLabel;
    private bool _usableTrain;
    private GUIStyle? _trainStyle;
    private GUIStyle? _localStyle;
    private GUIStyle? _nullTrainStyle;
    private GUIStyle? _versionStyle;
    private Texture2D? _trainTex;
    private Texture2D? _localTex;
    private Texture2D? _nullTex;

    private bool _hasConsistDebug;
    private bool _lastHasLoco;
    private string _lastCars = "";
    private string _lastHandbrakes = "";

    private bool _hasLocalDebug;
    private bool _lastLocalVisible;
    private string _lastPipe = "";
    private string _lastHandbrake = "";
    private string _lastCoupling = "";
    private string _lastCarNumber = "";
    private string _lastJob = "";

    private void OnEnable()
    {
        RebuildStyles();
    }

    private void OnDisable()
    {
        DestroyStyles();
    }

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed < RefreshSeconds)
        {
            return;
        }

        _elapsed = 0f;
        _usableTrain = TelemetryReader.HasUsableLocoTrain();
        _trainLabel = TelemetryReader.CurrentTrainHudLine();
        _localLabel = TelemetryReader.CurrentLocalCarHudLineOrNull();
        EmitConsistDebugIfNeeded();
        EmitLocalCarDebugIfNeeded();
    }

    private void EmitConsistDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentConsistDebugSnapshot();
        ConsistDebugSnapshot? previous = null;
        if (_hasConsistDebug)
        {
            previous = new ConsistDebugSnapshot(_lastHasLoco, _lastCars, _lastHandbrakes);
        }

        var line = Tier2ConsistDebug.NextLogMessage(previous, snap);
        _lastHasLoco = snap.HasLoco;
        _lastCars = snap.Cars;
        _lastHandbrakes = snap.Handbrakes;
        _hasConsistDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EmitLocalCarDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentLocalCarDebugSnapshot();
        LocalCarDebugSnapshot? previous = null;
        if (_hasLocalDebug)
        {
            previous = new LocalCarDebugSnapshot(
                _lastLocalVisible,
                _lastPipe,
                _lastHandbrake,
                _lastCoupling,
                _lastCarNumber,
                _lastJob);
        }

        var line = Tier2LocalCarDebug.NextLogMessage(previous, snap);
        _lastLocalVisible = snap.Visible;
        _lastPipe = snap.Pipe;
        _lastHandbrake = snap.Handbrake;
        _lastCoupling = snap.Coupling;
        _lastCarNumber = snap.CarNumber;
        _lastJob = snap.Job;
        _hasLocalDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void OnGUI()
    {
        EnsureStyles();
        var trainStyle = _usableTrain ? _trainStyle! : _nullTrainStyle!;
        var trainWidth = Mathf.Max(520f, trainStyle.CalcSize(new GUIContent(_trainLabel)).x + 12f);
        GUI.Label(new Rect(Pad, Pad, trainWidth, Height), _trainLabel, trainStyle);

        // Compact version chip — confirms which DLL is loaded after a deploy.
        var version = $"v{Main.ModVersion}";
        var versionSize = _versionStyle!.CalcSize(new GUIContent(version));
        GUI.Label(
            new Rect(Pad + trainWidth + 6f, Pad + 4f, versionSize.x + 8f, Height - 8f),
            version,
            _versionStyle);

        if (_localLabel == null)
        {
            return;
        }

        // Rich-text color tags inflate CalcSize; measure the visible plain text.
        var measure = StripRichText(_localLabel);
        var localWidth = Mathf.Max(520f, _localStyle!.CalcSize(new GUIContent(measure)).x + 12f);
        GUI.Label(new Rect(Pad, Pad + Height + Gap, localWidth, Height), _localLabel, _localStyle);
    }

    private static string StripRichText(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        var inTag = false;
        foreach (var ch in text)
        {
            if (ch == '<')
            {
                inTag = true;
                continue;
            }

            if (ch == '>')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private void EnsureStyles()
    {
        if (_trainStyle != null
            && _trainStyle.normal.background != null
            && _localStyle != null
            && _localStyle.normal.background != null
            && _nullTrainStyle != null
            && _nullTrainStyle.normal.background != null
            && _versionStyle != null)
        {
            return;
        }

        RebuildStyles();
    }

    private void RebuildStyles()
    {
        DestroyStyles();
        _trainTex = CreateTexture(BarBackground);
        _localTex = CreateTexture(BarBackground);
        _nullTex = CreateTexture(NullBarBackground);
        _trainStyle = CreateBarStyle(_trainTex);
        _localStyle = CreateBarStyle(_localTex);
        _nullTrainStyle = CreateBarStyle(_nullTex);
        _versionStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.75f, 0.85f, 1f, 0.95f) },
        };
    }

    private void DestroyStyles()
    {
        DestroyTexture(ref _trainTex);
        DestroyTexture(ref _localTex);
        DestroyTexture(ref _nullTex);
        _trainStyle = null;
        _localStyle = null;
        _nullTrainStyle = null;
        _versionStyle = null;
    }

    private static GUIStyle CreateBarStyle(Texture2D background)
    {
        return new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 16,
            richText = true,
            padding = new RectOffset(10, 10, 4, 4),
            normal =
            {
                textColor = Color.white,
                background = background,
            },
        };
    }

    private static Texture2D CreateTexture(Color color)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
        };
        tex.SetPixel(0, 0, color);
        tex.Apply(false, true);
        return tex;
    }

    private static void DestroyTexture(ref Texture2D? tex)
    {
        if (tex == null)
        {
            return;
        }

        UnityEngine.Object.Destroy(tex);
        tex = null;
    }
}
