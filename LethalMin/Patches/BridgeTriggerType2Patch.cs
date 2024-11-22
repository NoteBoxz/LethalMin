using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(BridgeTriggerType2))]
    internal class BridgeTriggerType2Patch
    {
        [HarmonyPatch("AddToBridgeInstabilityServerRpc")]
        [HarmonyPostfix]
        private static void DoPfall(BridgeTriggerType2 __instance)
        {
            if (__instance.bridgeFell && __instance.GetComponent<PikminBridgeTrigger>() != null)
            {
                __instance.GetComponent<PikminBridgeTrigger>().KnockoffPikmin();
            }
        }

    }
}