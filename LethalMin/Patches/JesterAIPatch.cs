using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(JesterAI))]
    internal class JesterAIPatch
    {
        private static PikminAttackable pikminAttackable = new PikminAttackable
        {
            PikminGrabPath = "MeshContainer/AnimContainer/metarig/BoxContainer/spine.004/spine.005/spine.006/GrabBodyPoint",
            AttackSound = null, // This will be set dynamically
            AttackAnimTrigger = "KillPlayer",
            AttackInAnyState = false,
            AttackRange = 3f,
            AttackStates = new int[] { 2 }
        };

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        public static void UpdateAttacker(JesterAI __instance)
        {
            if (StartOfRound.Instance.livingPlayers == 0 || __instance.isEnemyDead)
            {
                return;
            }
            // Set the PikminAttackable component's values
            pikminAttackable.MaxPikminEatCount = LethalMin.JesterEatLimmit;
            pikminAttackable.AttackBuffer = LethalMin.JesterEatBuffer;
            pikminAttackable.HarmfulToPikmin = LethalMin.LethalJester;

            PikminAttacker pikminAttacker = __instance.GetComponentInChildren<PikminAttacker>();

            // If the object does not have a PikminAttacker component, create one
            if (pikminAttacker == null)
            {
                GameObject NodeInstance = GameObject.Instantiate(LethalMin.PikminAttackerNode, __instance.transform);
                pikminAttacker = NodeInstance.GetComponent<PikminAttacker>();
                pikminAttackable.AttackSound = __instance.killPlayerSFX;
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