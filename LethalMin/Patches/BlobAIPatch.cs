using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(BlobAI))]
    internal class BlobAIPatch
    {
        public static PikminDamager damager;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void GetDamagerz(BlobAI __instance)
        {
            // Find all child GameObjects with EnemyAiCollisionDetect script
            EnemyAICollisionDetect[] collisionDetects = __instance.gameObject.GetComponentsInChildren<EnemyAICollisionDetect>(true);

            foreach (EnemyAICollisionDetect collisionDetect in collisionDetects)
            {
                GameObject childObject = collisionDetect.gameObject;

                // Check if the BlobPikminKiller script is not already attached
                if (childObject.GetComponent<BlobPikminKiller>() == null)
                {
                    // Add the BlobPikminKiller script to the child GameObject
                    childObject.AddComponent<BlobPikminKiller>();
                }
            }
        }
    }
}