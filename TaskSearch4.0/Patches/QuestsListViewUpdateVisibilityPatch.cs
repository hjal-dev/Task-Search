using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using TaskSearch.Logging;
using TaskSearch.UI;
using SPT.Reflection.Patching;

namespace TaskSearch.Patches
{
    internal sealed class QuestsListViewUpdateVisibilityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(QuestsListView), nameof(QuestsListView.UpdateVisibility));
        }

        [PatchPostfix]
        private static void Postfix(QuestsListView __instance)
        {
            try
            {
                TraderTaskSearchController.For(__instance)?.OnVisibilityUpdated();
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("QuestsListView.UpdateVisibility postfix failed", ex);
            }
        }
    }
}
