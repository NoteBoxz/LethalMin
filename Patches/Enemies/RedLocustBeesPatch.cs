using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Diagnostics;
using Unity.Multiplayer.Tools.MetricTypes;
using LethalMin.Utils;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(RedLocustBees))]
    internal class RedLocustBeesPatch
    {
        [HarmonyPatch(nameof(RedLocustBees.DoAIInterval))]
        [HarmonyPostfix]
        public static void DoAIIntervalPostfix(RedLocustBees __instance)
        {
            try
            {
                if (StartOfRound.Instance.allPlayersDead || !__instance.hasSpawnedHive || __instance.daytimeEnemyLeaving)
                {
                    return;
                }

                if (__instance.currentBehaviourStateIndex == 0)
                {
                    if (__instance.IsHiveMissing())
                    {
                        return;
                    }
                    PikminAI? pikminAIB3 = GetZappablePikmin(__instance, 16);
                    if (pikminAIB3 != null && Vector3.Distance(pikminAIB3.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                    {
                        var leader = LethalMin.GetLeaderViaID(pikminAIB3.OwnerClientId);
                        if (leader?.Controller == null)
                        {
                            return;
                        }
                        PlayerControllerB? playerControllerB3 = leader.Controller;
                        __instance.GetComponent<RedLocustBeesPikminEnemy>().SetToChasePikmin(playerControllerB3, pikminAIB3);
                    }
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Failed to patch RedLocustBees DoAIInterval due to: {e}");
            }
        }

        public static PikminAI? GetZappablePikmin(RedLocustBees redLocustBees, float Range)
        {
            foreach (PikminAI ai in PikminManager.instance.PikminAIs)
            {
                if (!PikminIsZappable(ai))
                {
                    continue;
                }
                if (Vector3.Distance(ai.transform.position, redLocustBees.transform.position) < Range)
                {
                    return ai;
                }
            }
            return null;
        }

        public static bool PikminIsZappable(PikminAI ai)
        {
            if (ai.IsDeadOrDying || ai.IsAirborn || ai.CurrentLatchTrigger != null
            || PikChecks.IsPikminResistantToHazard(ai, PikminHazard.Electricity) || ai.Laying)
            {
                return false;
            }
            return true;
        }
    }
}