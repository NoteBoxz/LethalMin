using System.Collections.Generic;

using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(CompanyMonsterCollisionDetect))]
    public class CompanyMonsterCollisionDetectPatch
    {
        [HarmonyPatch(nameof(CompanyMonsterCollisionDetect.OnTriggerEnter))]
        [HarmonyPostfix]
        public static void OnTriggerEnterPostfix(CompanyMonsterCollisionDetect __instance, Collider other)
        {
            if (!LethalMin.Company_GrabsPikmin)
            {
                return;
            }

            PikminCollisionDetect pikminDetection = other.GetComponent<PikminCollisionDetect>();
            if (pikminDetection != null)
            {
                PikminAI pikminAI = pikminDetection.mainPikmin;
                if (pikminAI.IsDeadOrDying)
                {
                    return;
                }
                if (!pikminAI.IsOwner)
                {
                    return;
                }

                CompanyPikminEnemy companyPikminEnemy = Object.FindObjectOfType<CompanyPikminEnemy>();
                companyPikminEnemy.KillPikminServerRpc(pikminAI.NetworkObject, companyPikminEnemy.Detects.IndexOf(__instance));
            }
        }
    }
}