using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System;
using GameNetcodeStuff;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(HoarderBugAI))]
    public class HoarderBugAIPatch
    {
        private static PikminAttackable pikminAttackable = new PikminAttackable
        {
            PikminGrabPath = "HoarderBugModel/AnimContainer/Armature/Abdomen/Chest/HoldItemsTarget",
            AttackSound = null!, // This will be set dynamically
            AttackAnimTrigger = "",
            AttackInAnyState = false,
            AttackRange = 3f,
            AttackStates = new int[] { 2 }
        };

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfix(HoarderBugAI __instance)
        {
            if (__instance.CheckLineOfSightForPosition(__instance.nestPosition, 60f, 40, 0.5f))
            {
                for (int i = 0; i < HoarderBugAI.HoarderBugItems.Count; i++)
                {
                    if (HoarderBugAI.HoarderBugItems[i] == null)
                    {
                        continue;
                    }
                    if (HoarderBugAI.HoarderBugItems[i].itemGrabbableObject == null)
                    {
                        continue;
                    }
                    PikminItem ItemScript = HoarderBugAI.HoarderBugItems[i].itemGrabbableObject.GetComponentInChildren<PikminItem>();
                    if (ItemScript == null)
                    {
                        continue;
                    }
                    if (ItemScript.PikminOnItem > 0 && HoarderBugAI.HoarderBugItems[i].itemNestPosition == __instance.nestPosition)
                    {
                        HoarderBugAI.HoarderBugItems[i].status = HoarderBugItemStatus.Stolen;
                    }
                }
            }


            if (StartOfRound.Instance.livingPlayers == 0 || __instance.isEnemyDead)
            {
                return;
            }
            // Set the PikminAttackable component's values
            pikminAttackable.MaxPikminEatCount = LethalMin.HoarderBugEatLimmit;
            pikminAttackable.AttackBuffer = LethalMin.HoarderBugEatBuffer;
            pikminAttackable.HarmfulToPikmin = LethalMin.LethalBugs;

            PikminAttacker pikminAttacker = __instance.GetComponentInChildren<PikminAttacker>();

            // If the object does not have a PikminAttacker component, create one
            if (pikminAttacker == null)
            {
                GameObject NodeInstance = GameObject.Instantiate(LethalMin.PikminAttackerNode, __instance.transform);
                pikminAttacker = NodeInstance.GetComponent<PikminAttacker>();
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