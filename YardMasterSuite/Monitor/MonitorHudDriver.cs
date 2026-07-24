using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// In-world IMGUI overlay for Monitor Mode telemetry.
/// Top bar = usable loco-train totals (hidden when not usable — 4.3); second bar = look-at preferred, standing fallback.
/// Always-on: Heading (1.12) + Marked (1.14) + Station zone (4.6).
/// No mod version chip on HUD — verify ship # in UMM Mod Manager.
/// Bundle B.1: Pos (1.13) removed from the always-on bar.
/// Active Job bar (4.8) when jobs are taken. Loco bar centered IA (4.7).
/// </summary>
public sealed class MonitorHudDriver : MonoBehaviour
{
    /// <summary>Home = set/update park mark; Shift+Home = clear. Session-only.</summary>
    private const KeyCode ParkMarkKey = KeyCode.Home;

    private const float RefreshSeconds = 0.1f;

    /// <summary>
    /// GUI Y just below the last visible HUD bar (top-left origin). Updated each OnGUI for AR sticky row (A.2).
    /// </summary>
    public static float LastStackBottomGuiY { get; private set; }

    private static readonly Color BarBackground = new(0.12f, 0.12f, 0.12f, 0.82f);

    private float _elapsed;
    private string? _trainLabel;
    private string? _localLabel;
    private string? _jobLabel;
    private string _headingLabel = "— Heading";
    private string? _parkLabel;
    private string? _stationLabel;
    private string _alwaysOnLabel = "—";
    private GUIStyle? _trainStyle;
    private GUIStyle? _localStyle;
    private GUIStyle? _jobStyle;
    private GUIStyle? _alwaysOnStyle;
    private Texture2D? _trainTex;
    private Texture2D? _localTex;
    private Texture2D? _jobTex;
    private Texture2D? _alwaysOnTex;

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
    private string? _lastJob;
    private string? _lastTrack;
    private string? _lastCargo;
    private string? _lastLocoType;

    private bool _hasLookAtDebug;
    private bool _lastLookAtVisible;
    private string _lastLookAtPipe = "";
    private string _lastLookAtHandbrake = "";
    private string _lastLookAtCoupling = "";
    private string _lastLookAtCarNumber = "";
    private string? _lastLookAtJob;
    private string? _lastLookAtTrack;
    private string? _lastLookAtCargo;
    private string? _lastLookAtLocoType;

    private bool _hasCouplerDebug;
    private bool _lastCouplerVisible;
    private string _lastCouplerLine = "";

    private bool _hasPowerDebug;
    private bool _lastPowerHasLoco;
    private string _lastPowerLoad = "";
    private string _lastPowerMotors = "";
    private string _lastPowerFuel = "";
    private string _lastPowerOil = "";

    private bool _hasLimitDebug;
    private bool _lastLimitHasLoco;
    private string _lastLimitSpeed = "";
    private string _lastLimit = "";

    private bool _hasHeadingDebug;
    private string? _lastHeadingPoint;

    private bool _hasPositionDebug;
    private int? _lastPosX;
    private int? _lastPosZ;

    private bool _hasParkDebug;
    private bool _lastParkHasMark;
    private string? _lastParkReturnPoint;

    private bool _hasStationDebug;
    private bool _lastStationInZone;
    private string? _lastStationYardId;
    private string? _lastStationWalkPoint;

    private bool _hasNextStationDebug;
    private bool _lastNextStationVisible;
    private string? _lastNextStationLabel;

    private bool _hasActiveJobDebug;
    private bool _lastActiveJobVisible;
    private string? _lastActiveJobId;
    private string? _lastActiveJobBonus;
    private string? _lastActiveJobZone;

    private void OnDisable()
    {
        // Styles touch GUI.skin — only build them from OnGUI (EnsureStyles).
        DestroyStyles();
    }

    private void Update()
    {
        if (!HudWorldSession.IsActive(PlayerManager.PlayerTransform != null))
        {
            _trainLabel = null;
            _localLabel = null;
            _jobLabel = null;
            _parkLabel = null;
            _stationLabel = null;
            _alwaysOnLabel = "";
            LastStackBottomGuiY = 0f;
            return;
        }

        PollParkMarkHotkey();

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
            _jobLabel = TelemetryReader.CurrentActiveJobHudLineOrNull();
            _headingLabel = TelemetryReader.CurrentHeadingLabel();
            _parkLabel = TelemetryReader.CurrentParkLabel();
            _stationLabel = TelemetryReader.CurrentStationWaypointLabel();
            _alwaysOnLabel = AlwaysOnHudLine.Format(
                _headingLabel,
                _parkLabel,
                _stationLabel);
            EmitConsistDebugIfNeeded();
            EmitLocalCarDebugIfNeeded();
            EmitLookAtDebugIfNeeded();
            EmitCouplerDebugIfNeeded();
            EmitPowerDebugIfNeeded();
            EmitSpeedLimitDebugIfNeeded();
            EmitHeadingDebugIfNeeded();
            EmitPositionDebugIfNeeded();
            EmitParkDebugIfNeeded();
            EmitStationWaypointDebugIfNeeded();
            EmitNextStationDebugIfNeeded();
            EmitActiveJobDebugIfNeeded();
        }
        finally
        {
            TelemetryReader.EndHudTick();
        }
    }

    private void PollParkMarkHotkey()
    {
        if (!Input.GetKeyDown(ParkMarkKey))
        {
            return;
        }

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            TelemetryReader.ClearParkMark();
            return;
        }

        TelemetryReader.TrySetParkMarkAtPlayer();
    }

    private void EmitSpeedLimitDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentSpeedLimitDebugSnapshot();
        SpeedLimitDebugSnapshot? previous = null;
        if (_hasLimitDebug)
        {
            previous = new SpeedLimitDebugSnapshot(_lastLimitHasLoco, _lastLimitSpeed, _lastLimit);
        }

        var line = Tier2SpeedLimitDebug.NextLogMessage(previous, snap);
        _lastLimitHasLoco = snap.HasLoco;
        _lastLimitSpeed = snap.Speed;
        _lastLimit = snap.Limit;
        _hasLimitDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EmitHeadingDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentHeadingDebugSnapshot();
        HeadingDebugSnapshot? previous = null;
        if (_hasHeadingDebug)
        {
            previous = new HeadingDebugSnapshot(_lastHeadingPoint);
        }

        var line = Tier2HeadingDebug.NextLogMessage(previous, snap);
        _lastHeadingPoint = snap.CompassPoint;
        _hasHeadingDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EmitPositionDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentPositionDebugSnapshot();
        PositionDebugSnapshot? previous = null;
        if (_hasPositionDebug)
        {
            previous = new PositionDebugSnapshot(_lastPosX, _lastPosZ);
        }

        var line = Tier2PositionDebug.NextLogMessage(previous, snap);
        _lastPosX = snap.X;
        _lastPosZ = snap.Z;
        _hasPositionDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EmitParkDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentParkDebugSnapshot();
        ParkDebugSnapshot? previous = null;
        if (_hasParkDebug)
        {
            previous = new ParkDebugSnapshot(_lastParkHasMark, _lastParkReturnPoint);
        }

        var line = Tier2ParkDebug.NextLogMessage(previous, snap);
        _lastParkHasMark = snap.HasMark;
        _lastParkReturnPoint = snap.ReturnPoint;
        _hasParkDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EmitStationWaypointDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentStationWaypointDebugSnapshot();
        StationWaypointDebugSnapshot? previous = null;
        if (_hasStationDebug)
        {
            previous = new StationWaypointDebugSnapshot(
                _lastStationInZone,
                _lastStationYardId,
                _lastStationWalkPoint);
        }

        var line = Tier2StationWaypointDebug.NextLogMessage(previous, snap);
        _lastStationInZone = snap.InZone;
        _lastStationYardId = snap.YardId;
        _lastStationWalkPoint = snap.WalkPoint;
        _hasStationDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EmitNextStationDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentNextStationDebugSnapshot();
        NextStationDebugSnapshot? previous = null;
        if (_hasNextStationDebug)
        {
            previous = new NextStationDebugSnapshot(_lastNextStationVisible, _lastNextStationLabel);
        }

        var line = Tier2NextStationDebug.NextLogMessage(previous, snap);
        _lastNextStationVisible = snap.Visible;
        _lastNextStationLabel = snap.Label;
        _hasNextStationDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EmitActiveJobDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentActiveJobDebugSnapshot();
        ActiveJobDebugSnapshot? previous = null;
        if (_hasActiveJobDebug)
        {
            previous = new ActiveJobDebugSnapshot(
                _lastActiveJobVisible,
                _lastActiveJobId,
                _lastActiveJobBonus,
                _lastActiveJobZone);
        }

        var line = Tier2ActiveJobDebug.NextLogMessage(previous, snap);
        _lastActiveJobVisible = snap.Visible;
        _lastActiveJobId = snap.JobId;
        _lastActiveJobBonus = snap.BonusClock;
        _lastActiveJobZone = snap.ZoneFragment;
        _hasActiveJobDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EmitPowerDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentPowerDebugSnapshot();
        PowerDebugSnapshot? previous = null;
        if (_hasPowerDebug)
        {
            previous = new PowerDebugSnapshot(
                _lastPowerHasLoco,
                _lastPowerLoad,
                _lastPowerMotors,
                _lastPowerFuel,
                _lastPowerOil);
        }

        var line = Tier2PowerDebug.NextLogMessage(previous, snap);
        _lastPowerHasLoco = snap.HasLoco;
        _lastPowerLoad = snap.Load;
        _lastPowerMotors = snap.Motors;
        _lastPowerFuel = snap.Fuel;
        _lastPowerOil = snap.Oil;
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
                _lastTrack,
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
        _lastTrack = snap.Track;
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
                _lastLookAtTrack,
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
        _lastLookAtTrack = snap.Track;
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
        if (!HudWorldSession.IsActive(PlayerManager.PlayerTransform != null))
        {
            LastStackBottomGuiY = 0f;
            return;
        }

        EnsureStyles();

        // Stack top → bottom, all centered: loco → look-at → active job → always-on nav.
        var y = MonitorHudStackLayout.Pad;

        if (_trainLabel != null)
        {
            y = DrawCenteredBar(_trainLabel, _trainStyle!, y) + MonitorHudStackLayout.Gap;
        }

        if (_localLabel != null)
        {
            y = DrawCenteredBar(_localLabel, _localStyle!, y) + MonitorHudStackLayout.Gap;
        }

        if (_jobLabel != null)
        {
            y = DrawCenteredBar(_jobLabel, _jobStyle!, y) + MonitorHudStackLayout.Gap;
        }

        y = DrawCenteredBar(_alwaysOnLabel, _alwaysOnStyle!, y);
        LastStackBottomGuiY = y;
    }

    private float DrawCenteredBar(string label, GUIStyle style, float y)
    {
        var measure = StripRichText(label);
        // Grow from content only (DESIGN_SYSTEM). Style padding is already in CalcSize —
        // do not floor to a wide min (showed empty right pad after Pos was removed).
        var width = Mathf.Ceil(style.CalcSize(new GUIContent(measure)).x);
        var x = Mathf.Max(MonitorHudStackLayout.Pad, (Screen.width - width) * 0.5f);
        GUI.Label(new Rect(x, y, width, MonitorHudStackLayout.BarHeight), label, style);
        return y + MonitorHudStackLayout.BarHeight;
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
            && _jobStyle != null
            && _jobStyle.normal.background != null
            && _alwaysOnStyle != null
            && _alwaysOnStyle.normal.background != null)
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
        _jobTex = CreateTexture(BarBackground);
        _alwaysOnTex = CreateTexture(BarBackground);
        _trainStyle = CreateBarStyle(_trainTex);
        _localStyle = CreateBarStyle(_localTex);
        _jobStyle = CreateBarStyle(_jobTex);
        _alwaysOnStyle = CreateBarStyle(_alwaysOnTex);
    }

    private void DestroyStyles()
    {
        DestroyTexture(ref _trainTex);
        DestroyTexture(ref _localTex);
        DestroyTexture(ref _jobTex);
        DestroyTexture(ref _alwaysOnTex);
        _trainStyle = null;
        _localStyle = null;
        _jobStyle = null;
        _alwaysOnStyle = null;
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
