using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(EnemyAICollisionDetect))]
    internal class EnemyAICollisionDetectPatch
    {
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPrefix]
        private static bool OnTriggerStay2(Collider other, EnemyAICollisionDetect __instance)
        {
            if (other == null || __instance == null)
            {
                return true;
            }
            if (other.name == "PikminColision" && !__instance.onlyCollideWhenGrounded && __instance.canCollideWithEnemies)
            {
                PikminAI targetmin = other.GetComponentInParent<PikminAI>();
                if (targetmin != null)
                    //LethalMin.Logger.LogInfo($"{__instance.mainScript.name} Coilided with Pikmin!");

                    __instance.mainScript.OnCollideWithEnemy(other, targetmin);
                return false;
            }
            return true;
        }
    }
}