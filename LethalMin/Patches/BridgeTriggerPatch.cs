using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(BridgeTrigger))]
    internal class BridgeTriggerPatch
    {
        [HarmonyPatch("BridgeFallServerRpc")]
        [HarmonyPostfix]
        private static void DoPfall(BridgeTrigger __instance)
        {
            if (__instance.GetComponent<PikminBridgeTrigger>() != null)
            {
                __instance.GetComponent<PikminBridgeTrigger>().KnockoffPikmin();
            }
        }

    }
}