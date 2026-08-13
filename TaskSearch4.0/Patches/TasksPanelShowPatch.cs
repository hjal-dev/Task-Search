using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using TaskSearch.Logging;
using TaskSearch.UI;
using SPT.Reflection.Patching;

namespace TaskSearch.Patches
{
    internal sealed class TasksPanelShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(
                typeof(TasksPanel),
                method => method.Name == nameof(TasksPanel.Show) && method.GetParameters().Length == 4);
        }

        [PatchPostfix]
        private static void Postfix(TasksPanel __instance, AbstractQuestControllerClass questController)
        {
            if (!TaskSearchConfig.Enabled.Value)
            {
                return;
            }

            try
            {
                TaskSearchController.Attach(__instance, questController);
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("TasksPanel.Show postfix failed", ex);
            }
        }
    }
}
