using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using LethalMin.Utils;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(BridgeTrigger))]
    internal class BridgeTriggerPatch
    {
        [HarmonyPatch(nameof(BridgeTrigger.BridgeFallClientRpc))]
        [HarmonyPrefix]
        private static void DoPfall(BridgeTrigger __instance)
        {
            if (PikChecks.IsClientRpcPrefixValid(__instance) == false)
            {
                return;
            }
            
            if (__instance.TryGetComponent(out PikminBridgeTrigger pikminBridgeTrigger))
            {
                pikminBridgeTrigger.KnockoffPikmin();
            }
        }

    }
}