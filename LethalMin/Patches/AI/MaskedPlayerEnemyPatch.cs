using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using LethalMon.Behaviours;
using System.Linq;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    internal class MaskedPlayerEnemyPatch
    {
        // Postpatch the start method and spawn in the PuffminOwnerManager
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void Start(MaskedPlayerEnemy __instance)
        {
            if (__instance.NetworkObject == null)
            {
                LethalMin.Logger.LogError("MaskedPlayerEnemy has no NetworkObject.");
                return;
            }
            if (!LethalMin.PuffMask)
            {
                return;
            }

            if (LethalMin.IsDependencyLoaded("LethalMon") && LETHALMON_ISTAMED(__instance)) { return; }
            if (!__instance.NetworkObject.IsSpawned)
            {
                __instance.StartCoroutine(WaitTillSpawnedThenPuffminOwnerManager(__instance));
            }
            else
            {
                CreatePOM(__instance);
            }
        }
        private static void CreatePOM(MaskedPlayerEnemy __instance)
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
                pom.InitalizeRefsClientRpc(zone.NetworkObject, pom.NetworkObject, __instance.NetworkObject);
            }
        }
        private static System.Collections.IEnumerator WaitTillSpawnedThenPuffminOwnerManager(MaskedPlayerEnemy __instance)
        {
            if (__instance.NetworkObject == null)
            {
                LethalMin.Logger.LogError("MaskedPlayerEnemy has no NetworkObject.");
                yield break;
            }
            while (!__instance.NetworkObject.IsSpawned)
            {
                yield return new WaitForSeconds(0.1f);
            }
            CreatePOM(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoAIInterval")]
        private static void DoAIInterval(MaskedPlayerEnemy __instance)
        {
            if (!LethalMin.PuffMask) { return; }
            if (__instance.isEnemyDead) { return; }
            if (!__instance.IsServer) { return; }
            if (LethalMin.IsDependencyLoaded("LethalMon") && LETHALMON_ISTAMED(__instance)) { return; }
            if (GetNumberOfWhistleabelePikmin(__instance.transform.position, LethalMin.MaskedWhistleRange) > 0)
            {
                __instance.GetComponentInChildren<PuffminOwnerManager>().DoWhistle();
            }

            if (__instance.targetPlayer != null && !__instance.targetPlayer.isPlayerDead)
            {
                __instance.GetComponentInChildren<PuffminOwnerManager>().DoThrow();
            }
        }

        private static int GetNumberOfWhistleabelePikmin(Vector3 position, float range)
        {
            var PuffminEnemies = PikminManager.GetPuffminEnemies();
            var PikminEnemies = PikminManager.GetPikminEnemies();
            if ((PuffminEnemies == null || PuffminEnemies.Count == 0) && (PikminEnemies == null || PikminEnemies.Count == 0))
            {
                //Logger.LogWarning("No Pikmin enemies found.");
                return 0;
            }

            var nearbyPuffmin = PuffminEnemies?
                .Where(p => p != null && Vector3.Distance(p.transform.position, position) <= range 
                && (p.currentBehaviourStateIndex == (int)PuffState.idle || p.currentBehaviourStateIndex == (int)PuffState.attacking)
                && p.SnapTopPos == null)
                .ToList() ?? new List<PuffminAI>();

            var nearbyPikmin = PikminEnemies?
                .Where(p => p != null && Vector3.Distance(p.transform.position, position) <= range 
                && !LethalMin.IsPikminResistantToHazard(p.PminType, HazardType.Spore) 
                && p.currentBehaviourStateIndex == (int)PState.Idle 
                && !p.CannotEscape && !p.IsDying)
                .ToList() ?? new List<PikminAI>();

            return nearbyPikmin.Count + nearbyPuffmin.Count;
        }

        public static bool LETHALMON_ISTAMED(MaskedPlayerEnemy __instance)
        {
            TamedEnemyBehaviour enemy = __instance.GetComponent<TamedEnemyBehaviour>();
            if (enemy == null) return false;
            return enemy.IsTamed;
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