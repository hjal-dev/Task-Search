using System;
using System.Collections.Generic;
using BepInEx;
using TaskSearch.Eft;
using TaskSearch.Logging;
using TaskSearch.Patches;
using SPT.Reflection.Patching;

namespace TaskSearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("EscapeFromTarkov.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "com.hj.tasksearch";
        internal const string PluginName = "TaskSearch";
        internal const string PluginVersion = "1.9.1";

        private void Awake()
        {
            TaskSearchLog.Initialize(Logger);
            TaskSearchConfig.Initialize(Config);

            if (!TaskSearchConfig.Enabled.Value)
            {
                Logger.LogInfo($"{PluginName} is disabled in the config; no patches applied.");
                return;
            }

            EftLocalization.Initialize();

            int applied = ApplyPatches();

            if (applied == 0)
            {
                Logger.LogError(
                    $"{PluginName} could not apply any patches. Quest search is inactive. " +
                    "This usually means this doesn't match SPT 4.0.x.");
                return;
            }

            Logger.LogInfo($"{PluginName} {PluginVersion} initialized ({applied} patches).");
        }

        private int ApplyPatches()
        {
            List<ModulePatch> patches = new List<ModulePatch>
            {
                new TasksPanelShowPatch(),
                new TasksPanelShowQuestsPatch(),
                new TasksPanelClosePatch(),
                new QuestsListViewShowPatch(),
                new QuestsListViewUpdateVisibilityPatch(),
                new QuestsListViewClosePatch()
            };

            patches.Add(new PlayerQuestsWindowInputPatch());

            int applied = 0;

            foreach (ModulePatch patch in patches)
            {
                try
                {
                    patch.Enable();
                    applied++;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to enable {patch.GetType().Name}: {ex.Message}");
                }
            }

            return applied;
        }
    }
}
