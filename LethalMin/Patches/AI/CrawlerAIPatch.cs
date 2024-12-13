using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(CrawlerAI))]
    internal class CrawlerAIPatch
    {
        private static PikminAttackable pikminAttackable = new PikminAttackable
        {
            PikminGrabPath = "CrawlerModel/AnimContainer/metarig/spine/spine.003/spine.004/MouthTarget",
            AttackSound = null, // This will be set dynamically
            AttackAnimTrigger = "HitPlayer",
            AttackInAnyState = false,
            AttackRange = 2f,
            CheckAtGrabPos = true,
            AttackStates = new int[] { 1 }
        };

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        public static void UpdateAttacker(CrawlerAI __instance)
        {
            if (StartOfRound.Instance.livingPlayers == 0 || __instance.isEnemyDead)
            {
                return;
            }
            // Set the PikminAttackable component's values
            pikminAttackable.MaxPikminEatCount = LethalMin.ThumperEatLimmit;
            pikminAttackable.AttackBuffer = LethalMin.ThumperEatBuffer;
            pikminAttackable.HarmfulToPikmin = LethalMin.LethalThumper;

            PikminAttacker pikminAttacker = __instance.GetComponentInChildren<PikminAttacker>();

            // If the object does not have a PikminAttacker component, create one
            if (pikminAttacker == null)
            {
                GameObject NodeInstance = GameObject.Instantiate(LethalMin.PikminAttackerNode, __instance.transform);
                pikminAttacker = NodeInstance.GetComponent<PikminAttacker>();
                pikminAttackable.AttackSound = __instance.bitePlayerSFX;
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
            pikminAttacker?.AttackNearbyPikmin(__instance);
        }
    }
}