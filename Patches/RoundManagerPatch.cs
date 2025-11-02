using System;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMin.Routeing;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    public class RoundManagerPatch
    {
        public static bool CallOnGameLoaded = false;

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

                CallOnGameLoaded = true;
            }
            catch (Exception e)
            {
                LethalMin.Logger.LogError($"Error in FinishGeneratingNewLevelClientRpcPrefix: {e}");
            }
        }

        [HarmonyPatch(nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPostfix]
        public static void FinishGeneratingNewLevelClientRpcPostfix(RoundManager __instance)
        {
            try
            {
                if (CallOnGameLoaded)
                {
                    CallOnGameLoaded = false;
                    PikminManager.instance.OnGameLoaded();
                }
            }
            catch (Exception e)
            {
                LethalMin.Logger.LogError($"Error in FinishGeneratingNewLevelClientRpcPostfix: {e}");
            }
        }

        [HarmonyPatch(nameof(RoundManager.SetExitIDs))]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPostfix]
        private static void SetExitIDs(RoundManager __instance)
        {
            try
            {
                EntranceTeleport[] entrances = GameObject.FindObjectsOfType<EntranceTeleport>();
                PikminRouteManager.Instance.RefreshEntrancePairs(entrances);
            }
            catch (Exception e)
            {
                LethalMin.Logger.LogError($"Error in SetExitIDs Postfix: {e}");
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
