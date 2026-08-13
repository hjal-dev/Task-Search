using System;
using System.Reflection;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using TaskSearch.Logging;
using TaskSearch.UI;
using SPT.Reflection.Patching;

namespace TaskSearch.Patches
{
    internal sealed class PlayerQuestsWindowInputPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type windowBase = typeof(PlayerQuestsWindow).BaseType;

            return windowBase == null
                ? null
                : AccessTools.Method(windowBase, "TranslateCommand", new[] { typeof(ECommand) });
        }

        [PatchPostfix]
        private static void Postfix(object __instance, ref InputNode.ETranslateResult __result)
        {
            if (__result != InputNode.ETranslateResult.Ignore)
            {
                return;
            }

            try
            {
                if (!(__instance is PlayerQuestsWindow window))
                {
                    return;
                }

                TaskSearchController controller =
                    window.GetComponentInChildren<TaskSearchController>(includeInactive: true);

                if (controller != null && controller.IsFieldFocused)
                {
                    __result = InputNode.ETranslateResult.BlockAll;
                }
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("PlayerQuestsWindow.TranslateCommand postfix failed", ex);
            }
        }
    }
}
