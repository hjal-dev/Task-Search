using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using TaskSearch.Logging;
using TaskSearch.UI;
using SPT.Reflection.Patching;

namespace TaskSearch.Patches
{
    internal sealed class TasksPanelClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(TasksPanel), nameof(TasksPanel.Close));
        }

        [PatchPostfix]
        private static void Postfix(TasksPanel __instance)
        {
            try
            {
                TaskSearchController.For(__instance)?.OnPanelClosed();
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("TasksPanel.Close postfix failed", ex);
            }
        }
    }
}
