using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace YardMasterSuite;

/// <summary>
/// UMM entry — same pattern as derail-valley-modding/template-umm.
/// </summary>
public static class Main
{
    // https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        Harmony? harmony = null;

        try
        {
            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnToggle = OnToggle;
        }
        catch (Exception ex)
        {
            modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
            harmony?.UnpatchAll(modEntry.Info.Id);
            return false;
        }

        return true;
    }

    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        // Phase 0: no patches yet — toggle is a no-op success so UMM can enable/disable cleanly.
        if (value)
        {
            modEntry.Logger.Log("Yard Master Suite enabled.");
        }
        else
        {
            modEntry.Logger.Log("Yard Master Suite disabled.");
        }

        return true;
    }
}
