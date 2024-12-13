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
                EnemyAICollisionDetect component3 = collider.gameObject.GetComponent<EnemyAICollisionDetect>();
                if (__instance.GetComponentInParent<PikminProtector>() != null && collider.GetComponentInParent<PikminAI>() != null)
                {
                    __instance.GetComponentInParent<PikminProtector>().ProtectPikmin(component3.GetComponent<PikminAI>());
                }
            }
        }
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPostfix]
        public static void OnTriggerStayPostfix(Collider collider, CustomTouchInteractTrigger __instance)
        {
            if (__instance.isKillTrigger)
            {
                if (__instance.GetComponentInParent<PikminProtector>() != null)
                    __instance.GetComponentInParent<PikminProtector>().UnprotectPikmin();
            }
        }
    }
}