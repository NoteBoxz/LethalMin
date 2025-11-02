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
    [HarmonyPatch(typeof(EnemyAICollisionDetectPatch))]
    public static class ImperiuEnemyAICollisionDetectPatch
    {
        [HarmonyPatch(nameof(EnemyAICollisionDetectPatch.DetectNoisePatch))]
        [HarmonyPrefix]
        private static bool DetectNoisePatchPrefix(EnemyAICollisionDetect __instance, Vector3 noisePosition, float noiseLoudness, int timesNoisePlayedInOneSpot, int noiseID)
        {
            if (LethalMin.DontDoAudibleNoiseCalcuationsForPikmin && noiseID == PikminAI.PikminSoundID)
            {
                return false;
            }
            return true;
        }
    }
}
