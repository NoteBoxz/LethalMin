using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem.Utilities;

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
            PikminManager.Instance.SyncAllWhistles();
            if (StartOfRound.Instance.mostRecentlyJoinedClient)
                PikminManager.Instance.SpawnShipPhaseOnionsServerRpc();
        }


        [HarmonyPatch("StartGame")]
        [HarmonyPostfix]
        private static void Imp()
        {
            PikminManager.Instance.OnGameStarted();
            GameObject.FindAnyObjectByType<PikminHUD>().RefreshLeaderScript();
        }

        [HarmonyPatch("EndPlayersFiredSequenceClientRpc")]
        [HarmonyPostfix]
        public static void PurgeSave()
        {
            if (NetworkManager.Singleton.IsServer && LethalMin.PurgeAfterFire)
            {
                if (LethalMin.IsUsingModLib())
                {
                    LethalMin.Logger.LogInfo($"Purging save data.");
                    OnionEzSaveData NewSaveData = new OnionEzSaveData();
                    NewSaveData.Load();
                    NewSaveData.OnionsCollected = new List<int>();
                    NewSaveData.OnionsFused = new Dictionary<int, int[]>();
                    NewSaveData.PikminStored = new List<OnionPikminStorage>();
                    NewSaveData.PikminLeftLastRound = 0;
                    NewSaveData.Save();
                }
                else
                {
                    LethalMin.Logger.LogInfo($"Purging save data. Save file number: {GameNetworkManager.Instance.saveFileNum}");
                    DeleteFileButtonPatch.DeleteLethalMinSaveFile(GameNetworkManager.Instance.saveFileNum);
                }
            }
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
            PikminManager.Instance.RemoveAllRadiuses();
        }

        [HarmonyPatch("SetShipReadyToLand")]
        [HarmonyPostfix]
        private static void SaveOnions()
        {
            PikminManager.Instance.FuseOnions();
            if (LethalMin.IsUsingModLib())
            {
                LethalMin.Logger.LogMessage("Using ModLib, saving EZOnion data.");
                PikminManager.Instance.SaveEZOnionData();
            }
            else
            {
                PikminManager.Instance.SaveOnionData();
            }
            PikminManager.Instance.StartCoroutine(PikminManager.Instance.DespawnOnions());
            GameObject.FindAnyObjectByType<PikminHUD>().UpdateHUD();
            if (!NetworkManager.Singleton.IsServer) { return; }
            PikminManager.Instance.SpawnShipPhaseOnionsServerRpc();
        }

        private static void CreatePikminManager()
        {
            if (!NetworkManager.Singleton.IsServer) { return; }
            if (PikminManager.Instance == null)
            {
                GameObject pikminManagerObject = UnityEngine.Object.Instantiate(LethalMin.PmanPrefab);
                pikminManagerObject.name = "Pikmin Manager";
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