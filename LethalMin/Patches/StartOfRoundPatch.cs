using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using System.IO;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("OnPlayerConnectedClientRpc")]
        [HarmonyPostfix]
        private static void OnPlayerConnectedClientRpcPostfix()
        {
            PikminManager.Instance.SyncAllPikminItems();
        }


        [HarmonyPatch("StartGame")]
        [HarmonyPostfix]
        private static void Imp()
        {
            PikminManager.Instance.OnGameStarted();
            GameObject.FindAnyObjectByType<PikminHUD>().RefreshLeaderScript();
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void Imp2()
        {
            CreatePikminManager();
        }

        [HarmonyPatch("ShipLeave")]
        [HarmonyPrefix]
        private static void SavePikmin()
        {
            PikminManager.Instance.HandlePikminWhenShipLeaving();
        }


        // In StartOfRoundPatch.cs

        [HarmonyPatch("ShipHasLeft")]
        [HarmonyPostfix]
        private static void SyncEndgameData()
        {
            if (NetworkManager.Singleton.IsServer)
                PikminManager.Instance.SyncEndgameDataServerRpc();
        
            PikminManager.Instance.DespawnSprouts();
        }

        [HarmonyPatch("SetShipReadyToLand")]
        [HarmonyPostfix]
        private static void SaveOnions()
        {
            PikminManager.Instance.FuseOnions();    
            PikminManager.Instance.SaveOnionData();
            PikminManager.Instance.StartCoroutine(PikminManager.Instance.DespawnOnions());
            GameObject.FindAnyObjectByType<PikminHUD>().UpdateHUD();
        }

        private static void CreatePikminManager()
        {
            if (!NetworkManager.Singleton.IsServer) { return; }
            if (PikminManager.Instance == null)
            {
                GameObject pikminManagerObject = UnityEngine.Object.Instantiate(LethalMin.PmanPrefab);
                pikminManagerObject.GetComponent<NetworkObject>().Spawn();
                LethalMin.Logger.LogInfo("PikminManager created and spawned across the network!");
            }
            else
            {
                LethalMin.Logger.LogInfo("PikminManager already exists in the scene.");
            }
        }

    }
}