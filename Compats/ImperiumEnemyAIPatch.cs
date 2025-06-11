using System;
using HarmonyLib;
using Imperium.Patches.Objects;
using Imperium.Patches.Systems;
using LethalMin.Pikmin;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Compats
{
    [CompatClass("giosuel.Imperium")]
    [HarmonyPatch(typeof(EnemyAIPatch))]
    public static class ImperiumEnemyAIPatch
    {
        [HarmonyPatch(nameof(EnemyAIPatch.KillEnemyPatch))]
        [HarmonyPrefix]
        private static bool KillEnemyPatchPrefix(EnemyAI __0)
        {
            if (LethalMin.DontResimulateOracleOnPikminDeath && __0 is PikminAI)
            {
                LethalMin.Logger.LogInfo($"A pikmin died, not resimulating oracle");
                return false;
            }
            return true;
        }
    }
}
