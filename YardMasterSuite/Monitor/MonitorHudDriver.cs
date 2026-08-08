using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// In-world IMGUI overlay for Monitor Mode telemetry.
/// Top bar = usable loco-train totals (hidden when not usable — 4.3); second bar = look-at preferred, standing fallback.
/// Always-on: Heading (1.12) + Marked (1.14) + Station zone (4.6) + Path check (3.4) + Clock.
/// No mod version chip on HUD — verify ship # in UMM Mod Manager.
/// Bundle B.1: Pos (1.13) removed from the always-on bar.
/// Active Job bar (4.8) when jobs are taken. Loco bar centered IA (4.7).
/// </summary>
public sealed class MonitorHudDriver : MonoBehaviour
{
    /// <summary>Home = set/update park mark; Shift+Home = clear. Session-only.</summary>
    private const KeyCode ParkMarkKey = KeyCode.Home;

    /// <summary>End = set path destination from look-at track; Shift+End = clear (3.4).</summary>
    private const KeyCode PathDestKey = KeyCode.End;

    /// <summary>Shift+F1 = toggle Tier 2 debug hotkeys (and bottom legend HUD).</summary>
    private const KeyCode DebugToggleKey = KeyCode.F1;

    /// <summary>Shift+F2 = toggle change-driven <c>T2 …</c> telemetry lines (off in normal play).</summary>
    private const KeyCode TelemetryLogToggleKey = KeyCode.F2;

    /// <summary>F5 = cycle DH4/DE6 licenses: real → DH4 only → DH4+DE6 → real.</summary>
    private const KeyCode LocoLicenseDebugKey = KeyCode.F5;

    /// <summary>F6 = cycle Load% for the loco you are in (off → 85% → 97% → off).</summary>
    private const KeyCode LoadDebugKey = KeyCode.F6;

    /// <summary>F7 = look-at consist: unload ↔ full load all freight.</summary>
    private const KeyCode CargoCycleKey = KeyCode.F7;

    /// <summary>F8 = cycle fluids for the loco you are in.</summary>
    private const KeyCode FluidDebugKey = KeyCode.F8;

    /// <summary>F9 = cycle coupler MU-yellow for the look-at / target car.</summary>
    private const KeyCode CouplerDebugKey = KeyCode.F9;

    /// <summary>
    /// Parked: was F10 (OS often eats) then F4. Re-enable <see cref="PollMotorDebugHotkey"/> when thermal smoke returns.
    /// </summary>
    private const KeyCode MotorDebugKey = KeyCode.F4;

    /// <summary>F11 = toggle all licenses ↔ restore real snapshot.</summary>
    private const KeyCode S282LicenseKey = KeyCode.F11;

    /// <summary>Page Up/Down = QOL turntable (not debug-gated).</summary>
    private const KeyCode TurntableCwKey = KeyCode.PageUp;
    private const KeyCode TurntableCcwKey = KeyCode.PageDown;

    private const float TurntableTapMaxSeconds = 0.22f;

    private float _turntableCwDownAt = -1f;
    private float _turntableCcwDownAt = -1f;
    private bool _turntableCwDidHold;
    private bool _turntableCcwDidHold;

    private const float RefreshSeconds = 0.1f;

    /// <summary>Remaining Align ETA — schedule-lag + arrival clamp (~1 Hz).</summary>
    private const float EtaRefreshSeconds = 1f;

    /// <summary>
    /// GUI Y just below the last visible HUD bar (top-left origin). Updated each OnGUI for AR sticky row (A.2).
    /// </summary>
    public static float LastStackBottomGuiY { get; private set; }

    private static readonly Color BarBackground = new(0.12f, 0.12f, 0.12f, 0.82f);

    private float _elapsed;
    private float _etaRefreshAt;
    private float _gcProbeAt;
    private int _gcProbeGen0;
    private string? _trainLabel;
    private string? _localLabel;
    private string? _jobLabel;
    private string? _debugHotkeyLabel;
    private string _headingLabel = "— Heading";
    private string? _parkLabel;
    private string? _stationLabel;
    private string? _pathLabel;
    private string? _facingLabel;
    private string _alwaysOnLabel = "—";
    private GUIStyle? _trainStyle;
    private GUIStyle? _localStyle;
    private GUIStyle? _jobStyle;
    private GUIStyle? _alwaysOnStyle;
    private GUIStyle? _debugHotkeyStyle;
    private Texture2D? _trainTex;
    private Texture2D? _localTex;
    private Texture2D? _jobTex;
    private Texture2D? _alwaysOnTex;
    private Texture2D? _debugHotkeyTex;

    /// <summary>Reused across OnGUI — <c>new GUIContent</c> every draw was a GC source.</summary>
    private readonly GUIContent _barMeasureContent = new();
    private string? _alwaysOnMeasureLabel;
    private float _alwaysOnMeasureWidth;
    private string? _trainMeasureLabel;
    private float _trainMeasureWidth;
    private string? _localMeasureLabel;
    private float _localMeasureWidth;
    private string? _jobMeasureLabel;
    private float _jobMeasureWidth;
    private string? _debugMeasureLabel;
    private float _debugMeasureWidth;
    private int _barMeasureScreenWidth = -1;

    private bool _hasConsistDebug;
    private bool _lastHasLoco;
    private string _lastCars = "";
    private string _lastHandbrakes = "";

    private bool _hasLocalDebug;
    private bool _lastLocalVisible;
    private string _lastHandbrake = "";
    private string _lastCoupling = "";
    private string? _lastJob;
    private string? _lastTrack;
    private string? _lastIdentityChip;

    private bool _hasLookAtDebug;
    private bool _lastLookAtVisible;
    private string _lastLookAtHandbrake = "";
    private string _lastLookAtCoupling = "";
    private string? _lastLookAtJob;
    private string? _lastLookAtTrack;
    private string? _lastLookAtIdentityChip;

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
    private SpeedLimitDebugSnapshot _lastLimitDebug;

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

    private bool _hasPathDebug;
    private string? _lastPathChip;

    private bool _hasActiveJobDebug;
    private bool _lastActiveJobVisible;
    private string? _lastActiveJobId;
    private string? _lastActiveJobBonus;
    private string? _lastActiveJobPreview;

    private bool _tier2LogsEmitting;

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
            _debugHotkeyLabel = null;
            _parkLabel = null;
            _stationLabel = null;
            _pathLabel = null;
            _facingLabel = null;
            _alwaysOnLabel = "";
            LastStackBottomGuiY = 0f;
            return;
        }

        PollParkMarkHotkey();
        PollPathDestHotkey();
        PollDebugToggleHotkey();
        PollTelemetryLogToggleHotkey();
        // QOL: turntable always available in-world (Epic 4), not behind debug gate.
        PollTurntableHotkeys();
        if (DebugHotkeyGate.Enabled)
        {
            PollFluidDebugHotkeys();
            PollCargoDebugHotkeys();
            PollS282LicenseHotkey();
            PollLocoLicenseDebugHotkey();
            PollLoadDebugHotkey();
            // Motor heat debug parked — not needed for Maps/3.7; F10 was OS-eaten.
            // Resume PollMotorDebugHotkey when thermal Tier 2 is back on the menu.
            if (MotorDebugOverride.HasAnyOverride)
            {
                MotorDebugOverride.Clear();
            }

            PollCouplerDebugHotkey();
        }

        SampleGcCadence();

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
            _debugHotkeyLabel = DebugHotkeyGate.Enabled ? DebugHotkeyHudLine.Format() : null;
            _headingLabel = TelemetryReader.CurrentHeadingLabel();
            _parkLabel = TelemetryReader.CurrentParkLabel();
            _stationLabel = TelemetryReader.CurrentStationWaypointLabel();
            _pathLabel = TelemetryReader.CurrentPathCheckLabel();
            _facingLabel = TelemetryReader.CurrentFacingLabel();
            var exitLabel = TelemetryReader.CurrentExitLabel();
            _alwaysOnLabel = AlwaysOnHudLine.Format(
                _headingLabel,
                _parkLabel,
                _stationLabel,
                _pathLabel,
                MonitorHudLine.Join(new[] { _facingLabel ?? "", exitLabel ?? "" }),
                TelemetryReader.CurrentClockLabel());
            EmitTier2DebugLinesIfEnabled();
            RefreshRemainingEtaIfDue();
        }
        finally
        {
            TelemetryReader.EndHudTick();
        }
    }

    private void RefreshRemainingEtaIfDue()
    {
        if (Time.unscaledTime - _etaRefreshAt < EtaRefreshSeconds)
        {
            return;
        }

        _etaRefreshAt = Time.unscaledTime;
        var line = RoutePlanService.RefreshRemainingEta();
        if (line != null)
        {
            Main.Log(line);
        }
    }

    /// <summary>
    /// The ~2.5 s hitch is a stop-the-world collection, so log the gen-0 rate rather than guessing
    /// from video. Ungated: it must show up in a normal play smoke, and it is one line per window.
    /// </summary>
    private void SampleGcCadence()
    {
        var now = Time.unscaledTime;
        var gen0 = System.GC.CollectionCount(0);
        if (_gcProbeAt <= 0f)
        {
            _gcProbeAt = now;
            _gcProbeGen0 = gen0;
            return;
        }

        var window = now - _gcProbeAt;
        var collections = gen0 - _gcProbeGen0;
        if (!GcCadenceProbe.ShouldLog(window, collections))
        {
            return;
        }

        _gcProbeAt = now;
        _gcProbeGen0 = gen0;
        Main.Log(HudPerfLog.FormatGcCadence(collections, window, System.GC.GetTotalMemory(false)));
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

    private void PollPathDestHotkey()
    {
        if (!Input.GetKeyDown(PathDestKey))
        {
            return;
        }

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            TelemetryReader.ClearPathDestination();
            Main.Log("T2 path: cleared");
            return;
        }

        var ok = TelemetryReader.TrySetPathDestinationFromTarget(out var message);
        Main.Log(ok ? message : $"T2 path: fail ({message})");
    }

    private void PollDebugToggleHotkey()
    {
        var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (!shift || !Input.GetKeyDown(DebugToggleKey))
        {
            return;
        }

        var on = DebugHotkeyGate.Toggle();
        if (!on)
        {
            FluidDebugOverride.Clear();
            LoadDebugOverride.Clear();
            MotorDebugOverride.Clear();
            CouplerDebugOverride.Clear();
            TelemetryReader.RestoreLicenseDebugIfNeeded();
            _debugHotkeyLabel = null;
        }
        else
        {
            _debugHotkeyLabel = DebugHotkeyHudLine.Format();
        }

        Main.Log($"T2 debug-hotkeys: {(on ? "on" : "off")}");
    }

    private void PollTelemetryLogToggleHotkey()
    {
        var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (!shift || !Input.GetKeyDown(TelemetryLogToggleKey))
        {
            return;
        }

        var on = Tier2TelemetryLogGate.Toggle();
        Main.Log($"T2 telemetry-logs: {(on ? "on" : "off")}");
    }

    private void PollFluidDebugHotkeys()
    {
        var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (shift || !Input.GetKeyDown(FluidDebugKey))
        {
            return;
        }

        var loco = PlayerManager.Car;
        if (loco == null || !loco.IsLoco || string.IsNullOrEmpty(loco.ID))
        {
            Main.Log("T2 fluid-debug: fail (sit in a loco)");
            return;
        }

        FluidDebugOverride.Cycle(loco.ID);
        Main.Log($"T2 fluid-debug [{loco.ID}]: {FluidDebugOverride.StatusFragment(loco.ID)}");
    }

    private void PollCouplerDebugHotkey()
    {
        var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (shift || !Input.GetKeyDown(CouplerDebugKey))
        {
            return;
        }

        var car = TelemetryReader.TryGetTargetCar();
        if (car == null || string.IsNullOrEmpty(car.ID))
        {
            Main.Log("T2 coupler-debug: fail (look at / stand on a car)");
            return;
        }

        CouplerDebugOverride.Cycle(car.ID);
        Main.Log($"T2 coupler-debug [{car.ID}]: {CouplerDebugOverride.StatusFragment(car.ID)}");
    }

    private void PollCargoDebugHotkeys()
    {
        if (!Input.GetKeyDown(CargoCycleKey))
        {
            return;
        }

        var ok = TelemetryReader.TryDebugCycleTargetCargo(out var message);
        Main.Log($"T2 cargo-debug cycle: {(ok ? "ok" : "fail")} ({message})");
    }

    private void PollS282LicenseHotkey()
    {
        if (!Input.GetKeyDown(S282LicenseKey))
        {
            return;
        }

        var ok = TelemetryReader.TryDebugToggleAllLicenses(out var message);
        Main.Log($"T2 license-debug: {(ok ? "ok" : "fail")} ({message})");
    }

    private void PollLocoLicenseDebugHotkey()
    {
        if (!Input.GetKeyDown(LocoLicenseDebugKey))
        {
            return;
        }

        var ok = TelemetryReader.TryDebugCycleLocoLicenses(out var message);
        Main.Log($"T2 loco-license-debug: {(ok ? "ok" : "fail")} ({message})");
    }

    private void PollLoadDebugHotkey()
    {
        if (!Input.GetKeyDown(LoadDebugKey))
        {
            return;
        }

        var loco = PlayerManager.Car;
        if (loco == null || !loco.IsLoco || string.IsNullOrEmpty(loco.ID))
        {
            Main.Log("T2 load-debug: fail (sit in a loco)");
            return;
        }

        LoadDebugOverride.Cycle(loco.ID);
        Main.Log($"T2 load-debug [{loco.ID}]: {LoadDebugOverride.StatusFragment(loco.ID)}");
    }

    private void PollMotorDebugHotkey()
    {
        if (!Input.GetKeyDown(MotorDebugKey))
        {
            return;
        }

        var loco = PlayerManager.Car;
        if (loco == null || !loco.IsLoco || string.IsNullOrEmpty(loco.ID))
        {
            Main.Log("T2 motor-debug: fail (sit in a loco)");
            return;
        }

        MotorDebugOverride.Cycle(loco.ID);
        Main.Log($"T2 motor-debug [{loco.ID}]: {MotorDebugOverride.StatusFragment(loco.ID)}");
    }

    private void PollTurntableHotkeys()
    {
        PollTurntableAxis(TurntableCwKey, ref _turntableCwDownAt, ref _turntableCwDidHold, +1f);
        PollTurntableAxis(TurntableCcwKey, ref _turntableCcwDownAt, ref _turntableCcwDidHold, -1f);

        // Hold push is applied in FixedUpdate (bar/lever simulation).
        if (!_turntableCwDidHold && !_turntableCcwDidHold)
        {
            TelemetryReader.ClearTurntableHoldPush();
        }
    }

    private void FixedUpdate()
    {
        if (!HudWorldSession.IsActive(PlayerManager.PlayerTransform != null))
        {
            TelemetryReader.ClearTurntableHoldPush();
            TelemetryReader.CancelTurntableSnapAssist();
            return;
        }

        TelemetryReader.ApplyTurntableBarSimulation(Time.fixedDeltaTime, out var status);
        if (!string.IsNullOrEmpty(status))
        {
            Main.Log($"T2 turntable: {status}");
        }

        var thermal = ThermalGovernor.Tick();
        if (thermal != null)
        {
            Main.Log(thermal);
        }

        var autobrake = AutoBrakeGovernor.Tick();
        if (autobrake != null)
        {
            Main.Log(autobrake);
        }
    }

    private void PollTurntableAxis(KeyCode key, ref float downAt, ref bool didHold, float direction)
    {
        if (Input.GetKeyDown(key))
        {
            downAt = Time.unscaledTime;
            didHold = false;
            TelemetryReader.CancelTurntableSnapAssist();
        }

        if (Input.GetKey(key) && downAt >= 0f)
        {
            var held = Time.unscaledTime - downAt;
            if (held > TurntableTapMaxSeconds)
            {
                didHold = true;
                TelemetryReader.CancelTurntableSnapAssist();
                TelemetryReader.SetTurntableHoldPush(direction);
            }
        }

        if (Input.GetKeyUp(key) && downAt >= 0f)
        {
            TelemetryReader.ClearTurntableHoldPush();
            if (!didHold)
            {
                var ok = TelemetryReader.TryBeginTurntableSnapAssist(out var message);
                Main.Log($"T2 turntable tap: {(ok ? "ok" : "fail")} ({message})");
            }

            downAt = -1f;
            didHold = false;
        }
    }

    /// <summary>
    /// Change-driven <c>T2 …</c> lines. Off in normal play: look-at and heading move with the camera,
    /// so these channels used to write to Player.log every HUD tick while looking around (0.6.50).
    /// </summary>
    private void EmitTier2DebugLinesIfEnabled()
    {
        var action = Tier2TelemetryLogGate.Decide(Tier2TelemetryLogGate.Enabled, _tier2LogsEmitting);
        _tier2LogsEmitting = action != Tier2LogAction.Skip;
        if (action == Tier2LogAction.Skip)
        {
            return;
        }

        if (action == Tier2LogAction.ResetThenEmit)
        {
            ForgetTier2DebugBaselines();
        }

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
        EmitPathCheckDebugIfNeeded();
        EmitActiveJobDebugIfNeeded();
    }

    /// <summary>Baselines went stale while logs were off — re-log one <c>init</c> line per channel.</summary>
    private void ForgetTier2DebugBaselines()
    {
        _hasConsistDebug = false;
        _hasLocalDebug = false;
        _hasLookAtDebug = false;
        _hasCouplerDebug = false;
        _hasPowerDebug = false;
        _hasLimitDebug = false;
        _hasHeadingDebug = false;
        _hasPositionDebug = false;
        _hasParkDebug = false;
        _hasStationDebug = false;
        _hasPathDebug = false;
        _hasActiveJobDebug = false;
    }

    private void EmitSpeedLimitDebugIfNeeded()
    {
        var snap = TelemetryReader.CurrentSpeedLimitDebugSnapshot();
        SpeedLimitDebugSnapshot? previous = _hasLimitDebug ? _lastLimitDebug : null;
        var line = Tier2SpeedLimitDebug.NextLogMessage(previous, snap);
        _lastLimitDebug = snap;
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

    private void EmitPathCheckDebugIfNeeded()
    {
        var chip = TelemetryReader.CurrentPathCheckLabel();
        if (_hasPathDebug && string.Equals(_lastPathChip, chip, System.StringComparison.Ordinal))
        {
            return;
        }

        _lastPathChip = chip;
        _hasPathDebug = true;
        if (chip != null)
        {
            Main.Log($"T2 path: {chip}");
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
                _lastActiveJobPreview);
        }

        var line = Tier2ActiveJobDebug.NextLogMessage(previous, snap);
        _lastActiveJobVisible = snap.Visible;
        _lastActiveJobId = snap.JobId;
        _lastActiveJobBonus = snap.BonusClock;
        _lastActiveJobPreview = snap.PreviewFragment;
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
                _lastHandbrake,
                _lastCoupling,
                _lastJob,
                _lastTrack,
                _lastIdentityChip);
        }

        var line = Tier2LocalCarDebug.NextLogMessage(previous, snap);
        _lastLocalVisible = snap.Visible;
        _lastHandbrake = snap.Handbrake;
        _lastCoupling = snap.Coupling;
        _lastJob = snap.Job;
        _lastTrack = snap.Track;
        _lastIdentityChip = snap.IdentityChip;
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
                _lastLookAtHandbrake,
                _lastLookAtCoupling,
                _lastLookAtJob,
                _lastLookAtTrack,
                _lastLookAtIdentityChip);
        }

        var line = Tier2LookAtDebug.NextLogMessage(previous, snap);
        _lastLookAtVisible = snap.Visible;
        _lastLookAtHandbrake = snap.Handbrake;
        _lastLookAtCoupling = snap.Coupling;
        _lastLookAtJob = snap.Job;
        _lastLookAtTrack = snap.Track;
        _lastLookAtIdentityChip = snap.IdentityChip;
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

        if (_barMeasureScreenWidth != Screen.width)
        {
            _barMeasureScreenWidth = Screen.width;
            _alwaysOnMeasureLabel = null;
            _trainMeasureLabel = null;
            _localMeasureLabel = null;
            _jobMeasureLabel = null;
            _debugMeasureLabel = null;
        }

        // Stack top → bottom: always-on fixed first, then loco → look-at → job.
        var y = MonitorHudStackLayout.Pad;

        y = DrawCenteredBar(
                _alwaysOnLabel,
                _alwaysOnStyle!,
                y,
                ref _alwaysOnMeasureLabel,
                ref _alwaysOnMeasureWidth)
            + MonitorHudStackLayout.Gap;

        if (_trainLabel != null)
        {
            y = DrawCenteredBar(
                    _trainLabel,
                    _trainStyle!,
                    y,
                    ref _trainMeasureLabel,
                    ref _trainMeasureWidth)
                + MonitorHudStackLayout.Gap;
        }

        if (_localLabel != null)
        {
            y = DrawCenteredBar(
                    _localLabel,
                    _localStyle!,
                    y,
                    ref _localMeasureLabel,
                    ref _localMeasureWidth)
                + MonitorHudStackLayout.Gap;
        }

        if (_jobLabel != null)
        {
            y = DrawCenteredBar(
                    _jobLabel,
                    _jobStyle!,
                    y,
                    ref _jobMeasureLabel,
                    ref _jobMeasureWidth)
                + MonitorHudStackLayout.Gap;
        }

        LastStackBottomGuiY = y;

        if (_debugHotkeyLabel != null)
        {
            var bottomY = Screen.height - MonitorHudStackLayout.Pad - MonitorHudStackLayout.BarHeight;
            DrawCenteredBar(
                _debugHotkeyLabel,
                _debugHotkeyStyle!,
                bottomY,
                ref _debugMeasureLabel,
                ref _debugMeasureWidth);
        }
    }

    private float DrawCenteredBar(
        string label,
        GUIStyle style,
        float y,
        ref string? cachedLabel,
        ref float cachedWidth)
    {
        // OnGUI can run twice per frame; stripping + CalcSize every call filled the heap (~2.5 s GC).
        if (HudBarMeasureCache.NeedsRemeasure(cachedLabel, label))
        {
            cachedLabel = label;
            _barMeasureContent.text = HudRichText.StripTags(label);
            cachedWidth = Mathf.Ceil(style.CalcSize(_barMeasureContent).x);
        }

        // Grow from content only (DESIGN_SYSTEM). Style padding is already in CalcSize —
        // do not floor to a wide min (showed empty right pad after Pos was removed).
        var width = cachedWidth;
        var x = Mathf.Max(MonitorHudStackLayout.Pad, (Screen.width - width) * 0.5f);
        GUI.Label(new Rect(x, y, width, MonitorHudStackLayout.BarHeight), label, style);
        return y + MonitorHudStackLayout.BarHeight;
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
            && _alwaysOnStyle.normal.background != null
            && _debugHotkeyStyle != null
            && _debugHotkeyStyle.normal.background != null)
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
        _debugHotkeyTex = CreateTexture(BarBackground);
        _trainStyle = CreateBarStyle(_trainTex);
        _localStyle = CreateBarStyle(_localTex);
        _jobStyle = CreateBarStyle(_jobTex);
        _alwaysOnStyle = CreateBarStyle(_alwaysOnTex);
        _debugHotkeyStyle = CreateBarStyle(_debugHotkeyTex);
    }

    private void DestroyStyles()
    {
        DestroyTexture(ref _trainTex);
        DestroyTexture(ref _localTex);
        DestroyTexture(ref _jobTex);
        DestroyTexture(ref _alwaysOnTex);
        DestroyTexture(ref _debugHotkeyTex);
        _trainStyle = null;
        _localStyle = null;
        _jobStyle = null;
        _alwaysOnStyle = null;
        _debugHotkeyStyle = null;
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
