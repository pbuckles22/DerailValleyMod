using System;
using System.Collections.Generic;
using DV.CabControls;
using DV.HUD;
using DV.Interaction.Inputs;
using DV.Simulation.Controllers;
using DV.Simulation.Fuses;
using LocoSim.Implementations;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Spike: while standing on a non-front car of the active trainset, route the player's
/// normal Rewired cab bindings (Throttle / Indy / TrainBrake / Reverser) to the front loco.
/// Numpad <c>.</c> (or <c>.</c>) toggles the front loco TM fuse from any car on the consist.
/// Fail closed off-consist; lever redirect skips when already in the front cab.
/// </summary>
internal static class OnConsistControlGovernor
{
    private static readonly List<int> LocoIndexScratch = new(8);

    // Per-action hold-repeat schedules (cab GetButtonDown alone does not auto-step).
    private static float _throttleNextFireAt;
    private static float _indyNextFireAt;
    private static float _brakeNextFireAt;
    private static float _reverserNextFireAt;

    /// <summary>True when standing on a non-front car (lever redirect armed).</summary>
    internal static bool IsArmed { get; private set; }

    /// <summary>Legend chip while redirect is active; null when fail-closed or in front cab.</summary>
    internal static string? HudLabel { get; private set; }

    /// <summary>
    /// Call from Update while world session active. Returns a discrete T2 line on arm/disarm / TM flip.
    /// </summary>
    internal static string? Tick()
    {
        var wasArmed = IsArmed;
        string? tmLog = null;
        try
        {
            var worldActive = HudWorldSession.IsActive(PlayerManager.PlayerTransform != null);
            var standing = worldActive ? PlayerManager.Car : null;
            var playerOnCar = standing != null;
            var front = TryResolveFrontLoco(standing);
            var standingIsFront = standing != null && front != null && ReferenceEquals(standing, front);
            var redirect = OnConsistControl.ShouldRedirectToFrontLoco(playerOnCar, standingIsFront);
            IsArmed = worldActive && front != null && redirect;
            HudLabel = IsArmed ? OnConsistControl.HudLegend : null;

            // TM fuse: any car on the consist (including front cab).
            if (worldActive && playerOnCar && front != null && TmFuseKeyDown())
            {
                tmLog = TryFlipTmFuse(front);
            }

            if (!IsArmed || front == null)
            {
                ResetHoldRepeat();
                return tmLog ?? ArmEdge(wasArmed, IsArmed);
            }

            var player = InputManager.NewPlayer;
            if (player == null)
            {
                ResetHoldRepeat();
                return tmLog ?? ArmEdge(wasArmed, IsArmed);
            }

            // Press + hold-repeat steps — cab notch size is 1/(notchCount-1).
            var throttleStep = ReadIncrementalStep(
                player, InputManager.Actions.ThrottleIncremental, ref _throttleNextFireAt);
            var indyStep = ReadIncrementalStep(
                player, InputManager.Actions.IndependentBrakeIncremental, ref _indyNextFireAt);
            var brakeStep = ReadIncrementalStep(
                player, InputManager.Actions.BrakeIncremental, ref _brakeNextFireAt);
            var reverserStep = ReadIncrementalStep(
                player, InputManager.Actions.ReverserIncremental, ref _reverserNextFireAt);

            var controls = front.SimController?.controlsOverrider;
            var throttle = controls?.Throttle;
            var indy = controls?.IndependentBrake;
            var brake = controls?.Brake;
            var reverser = controls?.Reverser;
            var controlsPresent =
                throttle != null || indy != null || brake != null || reverser != null;

            if (!OnConsistControl.IsSafeToWrite(
                    worldActive,
                    playerOnCar,
                    hasFrontLoco: true,
                    controlsPresent,
                    controlNotBlocked: true))
            {
                return tmLog ?? ArmEdge(wasArmed, IsArmed);
            }

            var writeThrottle = throttle != null && throttleStep != 0;
            var writeIndy = indy != null && indyStep != 0;
            var writeBrake = brake != null && brakeStep != 0;
            var writeReverser = reverser != null && reverserStep != 0;

            if (!writeThrottle && !writeIndy && !writeBrake && !writeReverser)
            {
                return tmLog ?? ArmEdge(wasArmed, IsArmed);
            }

            var desiredThrottle = writeThrottle
                ? OnConsistControl.StepLever(
                    throttle!.Value,
                    throttleStep,
                    throttle.IsNotched,
                    throttle.NotchCount)
                : 0f;
            var desiredIndy = writeIndy
                ? OnConsistControl.StepLever(
                    indy!.Value,
                    indyStep,
                    indy.IsNotched,
                    indy.NotchCount)
                : 0f;
            var desiredBrake = writeBrake
                ? OnConsistControl.StepLever(
                    brake!.Value,
                    brakeStep,
                    brake.IsNotched,
                    brake.NotchCount)
                : 0f;
            var desiredReverser = writeReverser
                ? OnConsistControl.StepReverser(reverser!.Value, reverserStep)
                : 0f;

            ThreeGate.TryApply(
                integrityOk: true,
                stateRegistryOk: controlsPresent,
                safetyOk: true,
                softWrite: () =>
                {
                    if (writeThrottle)
                    {
                        throttle!.Set(desiredThrottle);
                    }

                    if (writeIndy)
                    {
                        indy!.Set(desiredIndy);
                    }

                    if (writeBrake)
                    {
                        brake!.Set(desiredBrake);
                    }

                    if (writeReverser)
                    {
                        reverser!.Set(desiredReverser);
                    }

                    return true;
                });

            return tmLog ?? ArmEdge(wasArmed, IsArmed);
        }
        catch
        {
            return tmLog ?? ArmEdge(wasArmed, IsArmed);
        }
    }

    private static bool TmFuseKeyDown()
    {
        if (Input.GetKeyDown(KeyCode.KeypadPeriod) || Input.GetKeyDown(KeyCode.Period))
        {
            return true;
        }

        // Also honor the game's TractionMotorFuse binding if the player mapped one.
        try
        {
            var player = InputManager.NewPlayer;
            var id = InputManager.Actions.TractionMotorFuse;
            return player != null && id >= 0 && player.GetButtonDown(id);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Flip sim fuse (drives the physical knife via InteractableFuseFeeder) — same source as Motors HUD.
    /// </summary>
    private static string? TryFlipTmFuse(TrainCar front)
    {
        try
        {
            var flow = front.SimController?.simFlow ?? front.SimController?.SimulationFlow;

            // ON only — never kill motors mid-ride with an accidental Numpad .
            var fuse = TryResolveTmFuse(front, flow);
            if (fuse != null)
            {
                if (fuse.State)
                {
                    return "T2 on-consist: TM fuse already ON";
                }

                fuse.ChangeState(true);
                return "T2 on-consist: TM fuse ON";
            }

            var boxCtrl = TryResolveTmFuseControl(front);
            if (boxCtrl != null)
            {
                if (boxCtrl.Value > 0.5f)
                {
                    return "T2 on-consist: TM fuse already ON";
                }

                boxCtrl.SetValue(1f, ControlImplBase.SetValueSource.Default);
                return "T2 on-consist: TM fuse ON";
            }

            return "T2 on-consist: TM fuse control missing";
        }
        catch
        {
            return "T2 on-consist: TM fuse flip failed";
        }
    }

    private static Fuse? TryResolveTmFuse(TrainCar loco, SimulationFlow? flow)
    {
        if (flow == null)
        {
            return null;
        }

        var deadTm = loco.GetComponent<DeadTractionMotorsController>()
            ?? loco.GetComponentInChildren<DeadTractionMotorsController>(true);
        if (deadTm != null
            && !string.IsNullOrEmpty(deadTm.tmFuseId)
            && flow.TryGetFuse(deadTm.tmFuseId, out var tmFuse, canBeNull: true)
            && tmFuse != null)
        {
            return tmFuse;
        }

        var feeders = loco.GetComponentsInChildren<InteractableFuseFeeder>(true);
        for (var i = 0; i < feeders.Length; i++)
        {
            var id = feeders[i]?.fuseId?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (id!.IndexOf("tm", StringComparison.OrdinalIgnoreCase) < 0
                && id.IndexOf("traction", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (flow.TryGetFuse(id, out var fuse, canBeNull: true) && fuse != null)
            {
                return fuse;
            }
        }

        // Interior may host feeders when car root does not.
        try
        {
            var interior = loco.loadedInterior;
            if (interior != null)
            {
                feeders = interior.GetComponentsInChildren<InteractableFuseFeeder>(true);
                for (var i = 0; i < feeders.Length; i++)
                {
                    var id = feeders[i]?.fuseId?.Trim();
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    if (id!.IndexOf("tm", StringComparison.OrdinalIgnoreCase) < 0
                        && id.IndexOf("traction", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (flow.TryGetFuse(id, out var fuse, canBeNull: true) && fuse != null)
                    {
                        return fuse;
                    }
                }
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static ControlImplBase? TryResolveTmFuseControl(TrainCar loco)
    {
        try
        {
            var box = loco.GetComponent<LocoFuseBoxReference>()
                ?? loco.GetComponentInChildren<LocoFuseBoxReference>(true)
                ?? loco.loadedInterior?.GetComponentInChildren<LocoFuseBoxReference>(true)
                ?? loco.loadedExternalInteractables?.GetComponent<LocoFuseBoxReference>()
                ?? loco.loadedExternalInteractables?.GetComponentInChildren<LocoFuseBoxReference>(true);
            if (box?.tractionMotorFuse != null
                && box.tractionMotorFuse.TryGetComponent<ControlImplBase>(out var boxCtrl))
            {
                return boxCtrl;
            }

            var icm = loco.GetComponent<InteriorControlsManager>()
                ?? loco.GetComponentInChildren<InteriorControlsManager>(true)
                ?? loco.loadedInterior?.GetComponentInChildren<InteriorControlsManager>(true);
            if (icm != null
                && icm.TryGetControl(
                    InteriorControlsManager.ControlType.TractionMotorFuse,
                    out var reference)
                && reference.controlImplBase != null)
            {
                return reference.controlImplBase;
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static TrainCar? TryResolveFrontLoco(TrainCar? standing)
    {
        if (standing == null)
        {
            return null;
        }

        LocoIndexScratch.Clear();
        List<TrainCar>? cars;
        try
        {
            cars = standing.trainset?.cars;
        }
        catch
        {
            return null;
        }

        if (cars == null || cars.Count == 0)
        {
            return null;
        }

        for (var i = 0; i < cars.Count; i++)
        {
            var c = cars[i];
            if (c != null && c.IsLoco)
            {
                LocoIndexScratch.Add(c.indexInTrainset);
            }
        }

        var frontIndex = OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, LocoIndexScratch);
        if (frontIndex is null)
        {
            return null;
        }

        for (var i = 0; i < cars.Count; i++)
        {
            var c = cars[i];
            if (c != null && c.IsLoco && c.indexInTrainset == frontIndex.Value)
            {
                return c;
            }
        }

        return null;
    }

    private static void ResetHoldRepeat()
    {
        _throttleNextFireAt = 0f;
        _indyNextFireAt = 0f;
        _brakeNextFireAt = 0f;
        _reverserNextFireAt = 0f;
    }

    private static int ReadIncrementalStep(Rewired.Player player, int actionId, ref float nextFireAt)
    {
        if (actionId < 0)
        {
            nextFireAt = 0f;
            return 0;
        }

        var posHeld = player.GetButton(actionId);
        var negHeld = player.GetNegativeButton(actionId);
        if (posHeld == negHeld)
        {
            // Idle or both sides — do not step.
            nextFireAt = 0f;
            return 0;
        }

        if (posHeld)
        {
            var fire = HoldRepeat.ShouldFire(
                player.GetButtonDown(actionId),
                isHeld: true,
                (float)player.GetButtonTimePressed(actionId),
                ref nextFireAt);
            return fire ? +1 : 0;
        }

        var negFire = HoldRepeat.ShouldFire(
            player.GetNegativeButtonDown(actionId),
            isHeld: true,
            (float)player.GetNegativeButtonTimePressed(actionId),
            ref nextFireAt);
        return negFire ? -1 : 0;
    }

    private static string? ArmEdge(bool wasArmed, bool armed)
    {
        if (wasArmed == armed)
        {
            return null;
        }

        return armed
            ? "T2 on-consist: armed (cab bindings → front loco)"
            : "T2 on-consist: disarmed";
    }
}
