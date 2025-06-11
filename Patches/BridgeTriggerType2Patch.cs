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
        [HarmonyPatch(nameof(BridgeTriggerType2.AddToBridgeInstabilityServerRpc))]
        [HarmonyPrefix]
        private static void DoPfall(BridgeTriggerType2 __instance)
        {
            if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Server || (!__instance.NetworkManager.IsServer && !__instance.NetworkManager.IsHost))
            {
                return; // Skip original method if not on server
            }
            
            if (__instance.bridgeFell && __instance.TryGetComponent(out PikminBridgeTrigger pikminBridgeTrigger))
            {
                pikminBridgeTrigger.KnockoffPikmin();
            }
        }

    }
}