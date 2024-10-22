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
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StarDamage(EnemyAI __instance)
        {
            if (__instance.enemyType != LethalMin.pikminEnemyType && __instance.enemyType.canDie)
                __instance.gameObject.AddComponent<PikminDamager>().RootScript = __instance;
        }
    }
}