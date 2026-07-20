using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// In-world IMGUI overlay for Monitor Mode telemetry.
/// Top bar = usable loco-train totals (hidden when not usable — 4.3); second bar = look-at preferred, standing fallback.
/// </summary>
public sealed class MonitorHudDriver : MonoBehaviour
{
    private const float RefreshSeconds = 0.1f;
    private const float Pad = 12f;
    private const float Height = 28f;
    private const float Gap = 4f;

    private static readonly Color BarBackground = new(0.12f, 0.12f, 0.12f, 0.82f);

    private float _elapsed;
    private string? _trainLabel;
    private string? _localLabel;
    private GUIStyle? _trainStyle;
    private GUIStyle? _localStyle;
    private GUIStyle? _versionStyle;
    private Texture2D? _trainTex;
    private Texture2D? _localTex;

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
    private string? _lastCargo;
    private string? _lastLocoType;

    private bool _hasLookAtDebug;
    private bool _lastLookAtVisible;
    private string _lastLookAtPipe = "";
    private string _lastLookAtHandbrake = "";
    private string _lastLookAtCoupling = "";
    private string _lastLookAtCarNumber = "";
    private string _lastLookAtJob = "";
    private string? _lastLookAtCargo;
    private string? _lastLookAtLocoType;

    private bool _hasCouplerDebug;
    private bool _lastCouplerVisible;
    private string _lastCouplerLine = "";

    private bool _hasPowerDebug;
    private bool _lastPowerHasLoco;
    private string _lastPowerLoad = "";
    private string _lastPowerMotors = "";

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
        TelemetryReader.BeginHudTick();
        try
        {
            _trainLabel = TelemetryReader.CurrentTrainHudLineOrNull();
            _localLabel = TelemetryReader.CurrentLocalCarHudLineOrNull();
            EmitConsistDebugIfNeeded();
            EmitLocalCarDebugIfNeeded();
            EmitLookAtDebugIfNeeded();
            EmitCouplerDebugIfNeeded();
            EmitPowerDebugIfNeeded();
        }
        finally
        {
            TelemetryReader.EndHudTick();
        }
    }

    private void EmitPowerDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentPowerDebugSnapshot();
        PowerDebugSnapshot? previous = null;
        if (_hasPowerDebug)
        {
            previous = new PowerDebugSnapshot(_lastPowerHasLoco, _lastPowerLoad, _lastPowerMotors);
        }

        var line = Tier2PowerDebug.NextLogMessage(previous, snap);
        _lastPowerHasLoco = snap.HasLoco;
        _lastPowerLoad = snap.Load;
        _lastPowerMotors = snap.Motors;
        _hasPowerDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
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
                _lastJob,
                _lastCargo,
                _lastLocoType);
        }

        var line = Tier2LocalCarDebug.NextLogMessage(previous, snap);
        _lastLocalVisible = snap.Visible;
        _lastPipe = snap.Pipe;
        _lastHandbrake = snap.Handbrake;
        _lastCoupling = snap.Coupling;
        _lastCarNumber = snap.CarNumber;
        _lastJob = snap.Job;
        _lastCargo = snap.Cargo;
        _lastLocoType = snap.LocoType;
        _hasLocalDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EmitLookAtDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentLookAtDebugSnapshot();
        LocalCarDebugSnapshot? previous = null;
        if (_hasLookAtDebug)
        {
            previous = new LocalCarDebugSnapshot(
                _lastLookAtVisible,
                _lastLookAtPipe,
                _lastLookAtHandbrake,
                _lastLookAtCoupling,
                _lastLookAtCarNumber,
                _lastLookAtJob,
                _lastLookAtCargo,
                _lastLookAtLocoType);
        }

        var line = Tier2LookAtDebug.NextLogMessage(previous, snap);
        _lastLookAtVisible = snap.Visible;
        _lastLookAtPipe = snap.Pipe;
        _lastLookAtHandbrake = snap.Handbrake;
        _lastLookAtCoupling = snap.Coupling;
        _lastLookAtCarNumber = snap.CarNumber;
        _lastLookAtJob = snap.Job;
        _lastLookAtCargo = snap.Cargo;
        _lastLookAtLocoType = snap.LocoType;
        _hasLookAtDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EmitCouplerDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentCouplerDebugSnapshot();
        CouplerDebugSnapshot? previous = null;
        if (_hasCouplerDebug)
        {
            previous = new CouplerDebugSnapshot(_lastCouplerVisible, _lastCouplerLine);
        }

        var line = Tier2CouplerDebug.NextLogMessage(previous, snap);
        _lastCouplerVisible = snap.Visible;
        _lastCouplerLine = snap.Coupling;
        _hasCouplerDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        // Compact version chip — always visible so deploys are confirmable without a loco bar.
        var version = $"v{Main.ModVersion}";
        var versionSize = _versionStyle!.CalcSize(new GUIContent(version));
        var topBottom = Pad;

        if (_trainLabel != null)
        {
            // Rich-text color tags (Load / Motors) inflate CalcSize; measure plain text.
            var trainMeasure = StripRichText(_trainLabel);
            var trainWidth = Mathf.Max(520f, _trainStyle!.CalcSize(new GUIContent(trainMeasure)).x + 12f);
            GUI.Label(new Rect(Pad, Pad, trainWidth, Height), _trainLabel, _trainStyle);
            GUI.Label(
                new Rect(Pad + trainWidth + 6f, Pad + 4f, versionSize.x + 8f, Height - 8f),
                version,
                _versionStyle);
            topBottom = Pad + Height + Gap;
        }
        else
        {
            GUI.Label(new Rect(Pad, Pad + 4f, versionSize.x + 8f, Height - 8f), version, _versionStyle);
            // Keep second bar under the chip row when the loco gadget bar is hidden (4.3).
            topBottom = Pad + Height + Gap;
        }

        if (_localLabel == null)
        {
            return;
        }

        var measure = StripRichText(_localLabel);
        var localWidth = Mathf.Max(520f, _localStyle!.CalcSize(new GUIContent(measure)).x + 12f);
        GUI.Label(new Rect(Pad, topBottom, localWidth, Height), _localLabel, _localStyle);
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
        _trainStyle = CreateBarStyle(_trainTex);
        _localStyle = CreateBarStyle(_localTex);
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
        _trainStyle = null;
        _localStyle = null;
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
