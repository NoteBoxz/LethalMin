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
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPrefix]
        public static void OnTriggerStayEnterPrefix(Collider collider, CustomTouchInteractTrigger __instance)
        {
            if (__instance.isKillTrigger)
            {
                EnemyAICollisionDetect component3 = collider.gameObject.GetComponent<EnemyAICollisionDetect>();
                if (component3 != null && component3.mainScript.GetComponent<PikminAI>() != null && __instance.GetComponentInParent<PikminProtector>() != null)
                {
                    PikminAI pik = component3.mainScript.GetComponent<PikminAI>();
                    __instance.GetComponentInParent<PikminProtector>().ProtectPikmin(pik);

                    if (!LethalMin.IsPikminResistantToHazard(pik.PminType, HazardType.Electric))
                    {
                        pik.DoZapDeathClientRpc();
                    }
                }
            }
        }
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPostfix]
        public static void OnTriggerStayEnterPostfix(Collider collider, CustomTouchInteractTrigger __instance)
        {
            if (__instance.isKillTrigger)
            {
                if (__instance.GetComponentInParent<PikminProtector>() != null)
                    __instance.GetComponentInParent<PikminProtector>().UnprotectPikmin();
            }
        }
    }
}