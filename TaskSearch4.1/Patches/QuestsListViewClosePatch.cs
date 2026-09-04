using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using TaskSearch.Logging;
using TaskSearch.UI;
using SPT.Reflection.Patching;

namespace TaskSearch.Patches
{
    internal sealed class QuestsListViewClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(QuestsListView), nameof(QuestsListView.Close));
        }

        [PatchPostfix]
        private static void Postfix(QuestsListView __instance)
        {
            try
            {
                TraderTaskSearchController.For(__instance)?.OnClosed();
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("QuestsListView.Close postfix failed", ex);
            }
        }
    }
}
