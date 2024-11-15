using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Diagnostics;
using Unity.Multiplayer.Tools.MetricTypes;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(SpikeRoofTrap))]
    internal class SpikeRoofTrapPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPostfix(SpikeRoofTrap __instance)
        {
            if (!__instance.IsServer) { return; }
            
            PikminProtector protector = __instance.gameObject.GetComponent<PikminProtector>();
            protector.HazardTypez = new HazardType[1];
            protector.HazardTypez[0] = HazardType.Crush;
        }

        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPrefix]
        public static void OnTriggerStayPostfix(Collider other, SpikeRoofTrap __instance)
        {
            EnemyAICollisionDetect component3 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (component3 != null && component3.mainScript != null && component3.mainScript.IsOwner && component3.mainScript.enemyType.canDie && !component3.mainScript.isEnemyDead)
            {
                if (component3.mainScript.enemyType == LethalMin.pikminEnemyType)
                {
                    __instance.GetComponent<PikminProtector>().ProtectPikmin(component3.mainScript.GetComponent<PikminAI>());
                }
            }
        }

        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPostfix]
        public static void OnTriggerStayPrefix(Collider other, SpikeRoofTrap __instance)
        {
            __instance.GetComponent<PikminProtector>().UnprotectPikmin();
        }

    }
}