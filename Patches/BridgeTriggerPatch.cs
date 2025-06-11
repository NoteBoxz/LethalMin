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
        [HarmonyPatch(nameof(BridgeTrigger.BridgeFallClientRpc))]
        [HarmonyPrefix]
        private static void DoPfall(BridgeTrigger __instance)
        {
            if ((object)__instance.NetworkManager == null || !__instance.NetworkManager.IsListening)
            {
                return;
            }
            if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || (!__instance.NetworkManager.IsClient && !__instance.NetworkManager.IsHost))
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