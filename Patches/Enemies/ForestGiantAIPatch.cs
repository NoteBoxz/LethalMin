using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(ForestGiantAI))]
    public class ForestGiantAIPatch
    {
        [HarmonyPatch(nameof(ForestGiantAI.BeginEatPlayer))]
        [HarmonyPostfix]
        public static void BeginEatPikmin(ForestGiantAI __instance)
        {
            if (LethalMin.UseConfigsForEnemies && !LethalMin.ForestGiant_GrabsPikmin)
            {
                return;
            }
            if (!__instance.TryGetComponent(out ForestGiantPikminEnemy forestGiantPikminEnemy)) return;
            List<NetworkObjectReference> pikminRefs = new List<NetworkObjectReference>();

            foreach (PikminAI pikmin in PikminManager.instance.PikminAIs)
            {
                if (!pikmin.IsOwner)
                {
                    continue;
                }
                if (Vector3.Distance(pikmin.transform.position, forestGiantPikminEnemy.transform.position) > 10f)
                {
                    continue;
                }
                if (forestGiantPikminEnemy.handTrigger.TryLatch(pikmin, forestGiantPikminEnemy.transform.position, true, true))
                {
                    pikminRefs.Add(pikmin.NetworkObject);
                }
                if (pikminRefs.Count >= forestGiantPikminEnemy.GrabLimmit)
                {
                    break;
                }
            }

            if (pikminRefs.Count > 0)
            {
                forestGiantPikminEnemy.GrabPikminServerRpc(pikminRefs.ToArray());
            }
        }

        [HarmonyPatch(nameof(ForestGiantAI.StopKillAnimation))]
        [HarmonyPostfix]
        public static void StopKillAnimation(ForestGiantAI __instance)
        {
            if (!__instance.TryGetComponent(out ForestGiantPikminEnemy forestGiantPikminEnemy)) return;

            forestGiantPikminEnemy.StopKillAnimation();
        }
    }
}
