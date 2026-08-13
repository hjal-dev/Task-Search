using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using TaskSearch.Logging;
using TaskSearch.UI;
using SPT.Reflection.Patching;

namespace TaskSearch.Patches
{
    internal sealed class TasksPanelShowQuestsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(TasksPanel), nameof(TasksPanel.ShowQuests));
        }

        [PatchPostfix]
        private static void Postfix(TasksPanel __instance)
        {
            if (!TaskSearchConfig.Enabled.Value)
            {
                return;
            }

            try
            {
                TaskSearchController.For(__instance)?.OnQuestListRebuilt();
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("TasksPanel.ShowQuests postfix failed", ex);
            }
        }
    }
}
