using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Diagnostics;

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
                GameObject PomInstance = GameObject.Instantiate(LethalMin.POMprefab, __instance.transform);
                PuffminOwnerManager pom = PomInstance.GetComponent<PuffminOwnerManager>();
                pom.NetworkObject.Spawn();
                PomInstance.transform.SetParent(__instance.transform);
                pom.InitializeClientRpc(__instance.NetworkObject);

                GameObject ZoneInstance = GameObject.Instantiate(LethalMin.NoticeZone, __instance.transform);
                NoticeZone zone = ZoneInstance.GetComponent<NoticeZone>();
                zone.NetworkObject.Spawn();
                ZoneInstance.transform.SetParent(__instance.transform);
                zone.CanConvertPikmin = true;
                zone.InstantNotice = true;
                zone.UseCheckSpher = true;
                ZoneInstance.GetComponent<Renderer>().material.color = new Color(0.5f, 0f, 0.5f, 0.5f);
                ZoneInstance.AddComponent<MeshNoiseDistorter>().distortionStrength = 0.25f;
                zone.enemy = __instance;
                pom.noticeZone = zone;
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

        [HarmonyPrefix]
        [HarmonyPatch("OnCollideWithPlayer")]
        private static bool mothafuker()
        {
            return false;
        }
    }
}