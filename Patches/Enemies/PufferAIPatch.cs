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
    [HarmonyPatch(typeof(PufferAI))]
    internal class PufferAIPatch
    {
        public static bool HasMadeZone;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPufferAI(PufferAI __instance)
        {
            try
            {
                if (HasMadeZone) { LethalMin.Logger.LogDebug("Already made zone for PufferAI"); return; }
                SphereCollider zone = __instance.smokePrefab.AddComponent<SphereCollider>();
                zone.isTrigger = true;
                zone.radius = 25f;
                PikminEffectTrigger trig = __instance.smokePrefab.AddComponent<PikminEffectTrigger>();
                trig.Mode = PikminEffectMode.Limited;
                trig.EffectType = PikminEffectType.Scatter;
                trig.HazardType = PikminHazard.Poison;
                LethalMin.Logger.LogDebug($"Injected effect trigger to Puffer's Smoke prefab");
                HasMadeZone = true;
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"LethalMin: PufferAI.StartPufferAI: {e}");
            }
        }

    }
}