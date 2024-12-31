using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Linq;
using System.Reflection.Emit;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(RedLocustBees))]
    internal class RedLocustBeesPatch
    {
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefix(RedLocustBees __instance)
        {
            if (!LethalMin.LethalBees || !LethalMin.MeanBees || __instance.IsServer)
            {
                return true;
            }
            if (StartOfRound.Instance.allPlayersDead || !__instance.hasSpawnedHive || __instance.daytimeEnemyLeaving)
            {
                return true;
            }
            switch (__instance.currentBehaviourStateIndex)
            {
                case 2:
                    if (__instance.IsHivePlacedAndInLOS())
                    {
                        if (__instance.wasInChase)
                        {
                            return true;
                        }
                        __instance.lastKnownHivePosition = __instance.hive.transform.position + Vector3.up * 0.5f;
                        PikminAI pikminAI = null!;
                        pikminAI = LethalMin.FindNearestPikmin(__instance.hive.transform.position, __instance.defenseDistance, 1).FirstOrDefault();
                        if (pikminAI != null && Vector3.Distance(pikminAI.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                        {
                            __instance.SetDestinationToPosition(pikminAI.transform.position);
                            __instance.SwitchToBehaviourState(1);
                            if (pikminAI.previousLeader != null)
                            {
                                __instance.SwitchOwnershipOfBeesToClient(pikminAI.previousLeader?.Controller);
                            }
                            else if (pikminAI.currentLeader != null)
                            {
                                __instance.SwitchOwnershipOfBeesToClient(pikminAI.currentLeader?.Controller);
                            }
                            else{
                                LethalMin.Logger.LogWarning("No leader found for PikminAI when chaseing.");
                            }
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
            }
            return true;
        }


        static bool CheckForPikmin(RedLocustBees instance)
        {
            var nearestPikmin = LethalMin.FindNearestPikmin(instance.transform.position, 16, 1).FirstOrDefault();
            return nearestPikmin != null && Vector3.Distance(nearestPikmin.transform.position, instance.hive.transform.position) > instance.defenseDistance + 5f;
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        private static void DoAIIntervalPostfix(RedLocustBees __instance)
        {
            if (!LethalMin.LethalBees || !__instance.IsServer)
            {
                //LethalMin.Logger.LogInfo("Not the server.");
                return;
            }
            if (StartOfRound.Instance.allPlayersDead || !__instance.hasSpawnedHive || __instance.daytimeEnemyLeaving || __instance.hive == null)
            {
                return;
            }
            ShockPikmin(__instance);

            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    PikminAI pikminAI3 = LethalMin.FindNearestPikmin(__instance.transform.position, 16, 1).FirstOrDefault();
                    if (pikminAI3 != null && Vector3.Distance(pikminAI3.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                    {
                        __instance.SetDestinationToPosition(pikminAI3.transform.position);
                        __instance.SwitchToBehaviourState(1);
                        __instance.SwitchOwnershipOfBeesToClient(pikminAI3.previousLeader?.Controller);
                    }
                    break;
                case 1:
                    if (__instance.targetPlayer == null || !__instance.PlayerIsTargetable(__instance.targetPlayer) || Vector3.Distance(__instance.targetPlayer.transform.position, __instance.hive.transform.position) > (float)__instance.defenseDistance + 5f)
                    {

                    }
                    else if (__instance.hive.GetComponentInChildren<PikminItem>().PikminOnItem > 0)
                    {
                        __instance.SwitchToBehaviourState(2);
                    }
                    break;
            }
        }

        public static void ShockPikmin(RedLocustBees __instance)
        {
            List<PikminAI> MinsInWay = LethalMin.FindNearestPikmin(__instance.transform.position, 3.4f, LethalMin.BeesShockCount);
            if (MinsInWay.Count > 0)
            {
                LethalMin.Logger.LogInfo($"Found {MinsInWay.Count} Pikmin in range.");
                __instance.BeesZap();
                foreach (var item in MinsInWay)
                {
                    if (!item.IsDying && !item.FinnaBeDed && !item.isEnemyDead &&
                    !item.isHeld && !LethalMin.IsPikminResistantToHazard(item.PminType, HazardType.Electric))
                    {
                        // Calculate knockback direction (away from the turret)
                        Vector3 knockbackDirection = (item.transform.position - __instance.transform.position).normalized;

                        // Set knockback force
                        float knockbackForce = 10f; // Adjust this value as needed

                        // Calculate knockback vector
                        Vector3 knockbackVector = knockbackDirection * knockbackForce;

                        // Add a small upward force
                        knockbackVector += Vector3.up * 2f;

                        if (LethalMin.SuperLethalBees)
                            item.ReqeustHurtSFXClientRpc();

                        // Apply knockback
                        item.ApplyKnockbackServerRpc(knockbackVector, LethalMin.SuperLethalBees, false, 2.5f);

                        // Optionally, make the item face away from the turret
                        item.transform.rotation = Quaternion.LookRotation(knockbackDirection);
                    }
                }

            }
        }
    }
}