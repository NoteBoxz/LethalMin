using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Diagnostics;
using Unity.Multiplayer.Tools.MetricTypes;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    internal class MaskedPlayerEnemyPatch
    {
        // Postpatch the start method and spawn in the PuffminOwnerManager
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void Start(MaskedPlayerEnemy __instance)
        {
            if (__instance.IsServer)
            {
                // Create Owner Manager
                GameObject PomInstance = GameObject.Instantiate(LethalMin.POMprefab, __instance.transform);
                PuffminOwnerManager pom = PomInstance.GetComponent<PuffminOwnerManager>();

                // Sync the Owner Manager
                pom.NetworkObject.Spawn();
                PomInstance.transform.SetParent(__instance.transform);

                // Create Notice Zone
                GameObject ZoneInstance = GameObject.Instantiate(LethalMin.NoticeZone, __instance.transform);
                NoticeZone zone = ZoneInstance.GetComponent<NoticeZone>();

                // Sync the Notice Zone
                zone.NetworkObject.Spawn();
                ZoneInstance.transform.SetParent(__instance.transform);

                // Sync the refs
                pom.InitalizeRefsClientRpc( zone.NetworkObject, pom.NetworkObject, __instance.NetworkObject);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoAIInterval")]
        private static void DoAIInterval(MaskedPlayerEnemy __instance)
        {
            if (LethalMin.FindNearestIdlePikmin(__instance.transform.position, 15f, 1).Count > 0
             || LethalMin.FindNearestPuffmin(__instance.transform.position, 15f, 1).Count > 0)
            {
                __instance.GetComponentInChildren<PuffminOwnerManager>().DoWhistle();
            }

            if (__instance.targetPlayer != null && !__instance.targetPlayer.isPlayerDead)
            {
                __instance.GetComponentInChildren<PuffminOwnerManager>().DoThrow();
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch("KillEnemy")]
        private static void KillEnemy(MaskedPlayerEnemy __instance)
        {
            if (!__instance.IsServer) return;

            foreach (var item in __instance.GetComponentInChildren<PuffminOwnerManager>().followingPuffmin)
            {
                item.TurnIntoPikmin();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("TeleportMaskedEnemyServerRpc")]
        private static void teleportation(MaskedPlayerEnemy __instance)
        {
            __instance.GetComponentInChildren<PuffminOwnerManager>().TeleportPuffminToOwner();
        }
    }
}