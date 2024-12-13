using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(SandSpiderAI))]
    internal class SandSpiderAIPatch
    {
        private static PikminAttackable pikminAttackable = new PikminAttackable
        {
            PikminGrabPath = "MeshContainer/AnimContainer/MouthTarget",
            AttackSound = null, // This will be set dynamically
            AttackAnimTrigger = "attack",
            AttackInAnyState = false,
            AttackRange = 2f,
            CheckAtGrabPos = true,
            AttackStates = new int[] { 2 }
        };

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        public static void UpdateAttacker(SandSpiderAI __instance)
        {
            if (__instance.isEnemyDead)
            {
                return;
            }
            // Set the PikminAttackable component's values
            pikminAttackable.MaxPikminEatCount = LethalMin.SpiderEatLimmit;
            pikminAttackable.AttackBuffer = LethalMin.SpiderEatBuffer;
            pikminAttackable.HarmfulToPikmin = LethalMin.LethalSpider;

            PikminAttacker pikminAttacker = __instance.GetComponentInChildren<PikminAttacker>();

            // If the object does not have a PikminAttacker component, create one
            if (pikminAttacker == null)
            {
                GameObject NodeInstance = GameObject.Instantiate(LethalMin.PikminAttackerNode, __instance.transform);
                pikminAttacker = NodeInstance.GetComponent<PikminAttacker>();
                pikminAttackable.AttackSound = __instance.attackSFX;
                pikminAttackable = pikminAttacker.SABOBJ = pikminAttackable;
                if (__instance.IsServer)
                {
                    pikminAttacker.NetworkObject.Spawn();
                    NodeInstance.transform.SetParent(__instance.transform);
                }
                if (LethalMin.DebugMode)
                {
                    LethalMin.Logger.LogInfo("PikminAttacker component created on " + __instance.enemyType.enemyName);
                }
            }

            // Call the AttackNearby
            pikminAttacker.AttackNearbyPikmin(__instance);
        }
    }
}