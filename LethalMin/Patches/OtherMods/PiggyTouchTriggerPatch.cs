using HarmonyLib;
using PiggyVarietyMod.Patches;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalMin.Patches.OtherMods
{
    [HarmonyPatch(typeof(CustomTouchInteractTrigger))]
    public static class PiggyTouchTriggerPatch
    {
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPrefix]
        public static void OnTriggerStayPrefix(CustomTouchInteractTrigger __instance, Collider other)
        {
            if (__instance.isKillTrigger)
            {
                if (__instance.teslaGate != null && other.GetComponentInParent<PikminAI>() != null)
                {
                    __instance.GetComponent<PikminProtector>().ProtectPikmin(other.GetComponentInParent<PikminAI>());
                }
            }
        }
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPostfix]
        public static void OnTriggerStayPostfix(CustomTouchInteractTrigger __instance, Collider other)
        {
            if (__instance.isKillTrigger)
            {
                if (__instance.teslaGate != null)
                    __instance.GetComponent<PikminProtector>().UnprotectPikmin();
            }
        }
    }
}