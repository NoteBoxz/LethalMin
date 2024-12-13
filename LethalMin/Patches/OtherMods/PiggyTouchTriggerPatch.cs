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
        public static void OnTriggerStayPrefix(Collider collider, CustomTouchInteractTrigger __instance)
        {
            if (__instance.isKillTrigger)
            {
                if (__instance.teslaGate != null && collider.GetComponentInParent<PikminAI>() != null)
                {
                    __instance.teslaGate.GetComponent<PikminProtector>().ProtectPikmin(collider.GetComponentInParent<PikminAI>());
                }
            }
        }
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPostfix]
        public static void OnTriggerStayPostfix(Collider collider, CustomTouchInteractTrigger __instance)
        {
            if (__instance.isKillTrigger)
            {
                if (__instance.teslaGate != null)
                    __instance.teslaGate.GetComponent<PikminProtector>().UnprotectPikmin();
            }
        }
    }
}