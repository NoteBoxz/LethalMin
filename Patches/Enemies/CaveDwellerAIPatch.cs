using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Diagnostics;
using Unity.Multiplayer.Tools.MetricTypes;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(CaveDwellerAI))]
    internal class CaveDwellerAIPatch
    {
        [HarmonyPatch(nameof(CaveDwellerAI.DetectNoise))]
        [HarmonyPrefix]
        public static bool DetectNoisePrefix(CaveDwellerAI __instance, Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
        {
            if (noiseID == WhistleItem.WhistleAudioID || noiseID == PikminAI.PikminSoundID)
                return false;

            return true;
        }
        [HarmonyPatch(nameof(CaveDwellerAI.StartTransformationAnim))]
        [HarmonyPrefix]
        public static void StartTransformationAnimPrefix(CaveDwellerAI __instance)
        {
            if (!__instance.TryGetComponent(out CaveDwellerPikminItem itm))
            {
                return;
            }

            List<PikminAI> pikminToRemove = new List<PikminAI>(itm.PikminOnItem);
            foreach (PikminAI pikmin in pikminToRemove)
            {
                Vector3 direction = pikmin.transform.position - __instance.transform.position;
                direction += Vector3.up * 2f;
                pikmin.LandBuffer = 0.05f;
                pikmin.ApplyKnockBack(direction, 5);
            }
            itm.settings.GrabableToPikmin = false;
            itm.StartCoroutine(WaitToDisable(itm));
        }

        private static IEnumerator WaitToDisable(CaveDwellerPikminItem itm)
        {
            yield return new WaitUntil(() => itm.PikminOnItem.Count == 0);
            itm.enabled = false;
            LethalMin.Logger.LogInfo($"CaveDwellerPikminItem {itm.name} disabled");
        }
    }
}