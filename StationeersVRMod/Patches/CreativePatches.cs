/*using Assets.Scripts.UI;
using HarmonyLib;
using ImGuiNET;
using StationeersVR.Utilities;
using UnityEngine;

namespace StationeersVR.Patches
{
    internal class CreativePatches
    {
        [HarmonyPatch(typeof(ImGui), nameof(ImGui.setcu.GetCursorPos))]
        public static class ImGui_GetCursorPos_Patch
        {
            [HarmonyPrefix]
            static bool Prefix(ref Vector2 __result)
            {
                ModLog.Error("igGetCursorPos #1");
                Vector2 pos = SimpleGazeCursor.GetRayCastMode();
                Vector2 result = default(Vector2);
                ImGuiNative.igGetCursorPos(&pos);
                __result = Camera.main.ScreenPointToRay(pos).GetPoint(InputMouse.MaxInteractDistance);
                return false;
            }
        }
    }
}*/
