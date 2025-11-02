using System;
using HarmonyLib;
using Imperium.Patches.Systems;
using LethalMin.Pikmin;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Compats
{
    [CompatClass("giosuel.Imperium")]
    [HarmonyPatch(typeof(RoundManagerPatch))]
    public static class ImperiumRoundManagerPatch
    {
        [HarmonyPatch(nameof(RoundManagerPatch.PlayAudibleNoisePatch))]
        [HarmonyPrefix]
        private static bool PlayAudibleNoisePatchPrefix(Vector3 noisePosition, float noiseRange, float noiseLoudness, int timesPlayedInSameSpot, bool noiseIsInsideClosedShip, int noiseID)
        {
            if (LethalMin.DontDoAudibleNoiseCalcuationsForPikmin && noiseID == PikminAI.PikminSoundID)
            {
                return false;
            }
            return true;
        }
    }
}
