using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using YardMasterSuite.Monitor;

namespace YardMasterSuite;

/// <summary>
/// UMM entry — same pattern as derail-valley-modding/template-umm.
/// </summary>
public static class Main
{
    private static Harmony? _harmony;
    private static GameObject? _hudRoot;
    private static UnityModManager.ModEntry? _modEntry;

    /// <summary>UMM logger → Player.log. Used for Tier 2 discrete debug lines.</summary>
    internal static void Log(string message) => _modEntry?.Logger.Log(message);

    // https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        try
        {
            _modEntry = modEntry;
            _harmony = new Harmony(modEntry.Info.Id);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload;

            if (modEntry.Enabled)
            {
                EnsureHud();
            }
        }
        catch (Exception ex)
        {
            modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
            _harmony?.UnpatchAll(modEntry.Info.Id);
            DestroyHud();
            return false;
        }

        return true;
    }

    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        _modEntry = modEntry;
        if (value)
        {
            EnsureHud();
            modEntry.Logger.Log("Yard Master Suite enabled (Monitor HUD).");
        }
        else
        {
            DestroyHud();
            modEntry.Logger.Log("Yard Master Suite disabled.");
        }

        return true;
    }

    private static bool OnUnload(UnityModManager.ModEntry modEntry)
    {
        DestroyHud();
        _harmony?.UnpatchAll(modEntry.Info.Id);
        _harmony = null;
        _modEntry = null;
        return true;
    }

    private static void EnsureHud()
    {
        if (_hudRoot != null)
        {
            return;
        }

        try
        {
            _hudRoot = new GameObject("YardMasterSuite_MonitorHud");
            UnityEngine.Object.DontDestroyOnLoad(_hudRoot);
            _hudRoot.AddComponent<MonitorHudDriver>();
        }
        catch (Exception ex)
        {
            DestroyHud();
            _modEntry?.Logger.LogException("Failed to create Monitor HUD:", ex);
            throw;
        }
    }

    private static void DestroyHud()
    {
        if (_hudRoot == null)
        {
            return;
        }

        UnityEngine.Object.Destroy(_hudRoot);
        _hudRoot = null;
    }
}
