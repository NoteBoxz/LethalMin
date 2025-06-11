using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(DepositItemsDesk))]
    public class DepositItemsDeskPatch
    {
        [HarmonyPatch("INoiseListener.DetectNoise")]
        [HarmonyPrefix]
        public static bool DetectNoisePrefix(DepositItemsDesk __instance, Vector3 noisePosition, float noiseLoudness = 0.5f, int timesPlayedInOneSpot = 0, int noiseID = 0)
        {
            if (noiseID == PikminAI.PikminSoundID && !LethalMin.Company_HearsPikmin.InternalValue)
                return false;

            return true;
        }
    }
}