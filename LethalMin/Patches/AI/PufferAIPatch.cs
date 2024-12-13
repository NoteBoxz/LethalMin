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
            if (HasMadeZone) { LethalMin.Logger.LogInfo("Already made zone for PufferAI"); return; }
            if (!__instance.IsServer) return;
            SphereCollider zone = __instance.smokePrefab.AddComponent<SphereCollider>();
            zone.isTrigger = true;
            zone.radius = 25f;
            SporePoisonZone sporePoisonZone = __instance.smokePrefab.AddComponent<SporePoisonZone>();
            HasMadeZone = true;
        }

    }
}