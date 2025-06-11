using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(BaboonBirdAI))]
    public class BaboonBirdAIPatch
    {
        [HarmonyPatch(nameof(BaboonBirdAI.OnCollideWithEnemy))]
        [HarmonyPrefix]
        public static bool OnCollideWithEnemyPatch(BaboonBirdAI __instance, Collider other, EnemyAI enemyScript = null!)
        {
            try
            {
                if (enemyScript != null && enemyScript is PikminAI pikmin && __instance.TryGetComponent(out BaboonBirdPikminEnemy baboonBirdPikminEnemy))
                {
                    if (__instance.IsOwner)
                        baboonBirdPikminEnemy.OnColideWithPikmin(pikmin);
                    return false;
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"LethalMin: BaboonBirdAI.OnCollideWithEnemyPatch: {e}");
                return true;
            }
            return true;
        }

    }
}
