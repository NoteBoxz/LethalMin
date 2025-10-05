using System;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    public class RoundManagerPatch
    {
        [HarmonyPatch(nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
        [HarmonyPrefix]
        private static void FinishGeneratingNewLevelClientRpcPrefix(RoundManager __instance)
        {
            try
            {
                if (PikChecks.IsClientRpcPrefixValid(__instance) == false)
                {
                    return;
                }

                PikminManager.instance.OnGameLoaded();
            }
            catch (Exception e)
            {
                LethalMin.Logger.LogError($"Error in FinishGeneratingNewLevelClientRpcPrefix: {e}");
            }
        }

        [HarmonyPatch(nameof(RoundManager.SpawnScrapInLevel))]
        [HarmonyPostfix]
        private static void SpawnOnionsInLevel(RoundManager __instance)
        {
            PikminManager.instance.SpawnOnionItems();
        }
    }
}
