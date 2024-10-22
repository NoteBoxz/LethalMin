using HarmonyLib;
using LethalMin;
using UnityEngine;
namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    public class TerminalPatch
    {
        [HarmonyPatch("BeginUsingTerminal")]
        [HarmonyPostfix]
        public static void HandleTerminalPikminHud()
        {
            if (PikminHUD.pikminHUDInstance.PikminExists)
            {
                HUDManager.Instance.PingHUDElement(PikminHUD.pikminHUDInstance.element, 0, 1, 0);
            }
            else
            {
                HUDManager.Instance.PingHUDElement(PikminHUD.pikminHUDInstance.element, 0, 0, 0);
            }
        }
        [HarmonyPatch("QuitTerminal")]
        [HarmonyPostfix]
        public static void HandleTerminalPikminHud2()
        {
            if (PikminHUD.pikminHUDInstance.PikminExists)
            {
                HUDManager.Instance.PingHUDElement(PikminHUD.pikminHUDInstance.element, 0, 0, 1);
            }
            else
            {
                HUDManager.Instance.PingHUDElement(PikminHUD.pikminHUDInstance.element, 0, 0, 0);
            }
        }
    }
}