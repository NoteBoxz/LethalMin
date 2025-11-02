using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(MouthDogAI))]
    internal class MouthDogAIPatch
    {
        [HarmonyPatch("OnCollideWithEnemy")]
        [HarmonyPrefix]
        private static bool OnCollideWithPikmin(MouthDogAI __instance, Collider other, EnemyAI collidedEnemy)
        {
            // There is this stupid bug with these mofos that makes it so they kill pikmin when in their passive state
            if (collidedEnemy != null && collidedEnemy is PikminAI)
            {
                // if (__instance.currentBehaviourStateIndex == 2 && !__instance.inLunge)
                // {
                //     __instance.transform.LookAt(other.transform.position);
                //     __instance.transform.localEulerAngles = new Vector3(0f, __instance.transform.eulerAngles.y, 0f);
                //     __instance.inLunge = true;
                //     __instance.EnterLunge();
                // }
                return false; // Skip the original method
            }
            else
            {
                return true; // Continue with the original method if no Pikmin found
            }
        }
    }
}