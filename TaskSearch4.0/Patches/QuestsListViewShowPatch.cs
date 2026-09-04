using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using TaskSearch.Logging;
using TaskSearch.UI;
using SPT.Reflection.Patching;

namespace TaskSearch.Patches
{
    internal sealed class QuestsListViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(QuestsListView), nameof(QuestsListView.Show));
        }

        [PatchPostfix]
        private static void Postfix(QuestsListView __instance, TraderClass trader)
        {
            try
            {
                TraderTaskSearchController.Attach(__instance, trader);
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("QuestsListView.Show postfix failed", ex);
            }
        }
    }
}
