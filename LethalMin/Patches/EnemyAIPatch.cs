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
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        private static ulong currentEnemy = 9999999;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StarDamage(EnemyAI __instance)
        {
            if (__instance.enemyType != null && __instance.enemyType != LethalMin.puffminEnemyType &&
                __instance.enemyType != LethalMin.pikminEnemyType && __instance.enemyType.canDie
            )
            {
                __instance.gameObject.AddComponent<PikminDamager>().RootScript = __instance;
            }
        }

        [HarmonyPatch("KillEnemyServerRpc")]
        [HarmonyPrefix]
        static void CreateItemNodeOnBody(EnemyAI __instance)
        {
            if (LethalMin.IsDependencyLoaded("Entity378.sellbodies")) return;
            if (currentEnemy == __instance.NetworkObject.NetworkObjectId) return;
            if (!__instance.IsHost) return;
            if (__instance.GetComponentInChildren<PlayerControllerB>()) return;
            currentEnemy = __instance.NetworkObject.NetworkObjectId;
            LethalMin.Logger.LogInfo("Creating item node on enemy body " + __instance.gameObject.name);
            PhysicsProp prop = __instance.gameObject.AddComponent<PhysicsProp>();
            prop.grabbableToEnemies = true;
        }
    }
}