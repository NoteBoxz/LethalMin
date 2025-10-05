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
    [HarmonyPatch(typeof(BridgeTriggerType2))]
    internal class BridgeTriggerType2Patch
    {
        [HarmonyPatch(nameof(BridgeTriggerType2.AddToBridgeInstabilityServerRpc))]
        [HarmonyPrefix]
        private static void DoPfallType2(BridgeTriggerType2 __instance)
        {
            if (PikChecks.IsServerRpcNoOwnershipPrefixValid(__instance) == false)
            {
                return;
            }

            if (__instance.timesTriggered + 1 >= 4 && __instance.TryGetComponent(out PikminBridgeTrigger pikminBridgeTrigger))
            {
                pikminBridgeTrigger.KnockoffPikmin();
            }
        }

    }
}