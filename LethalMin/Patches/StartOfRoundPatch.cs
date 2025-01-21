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
using LethalMin.Patches.OtherMods;
using Unity.AI.Navigation;

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
            
            // if (LethalMin.IsDependencyLoaded("MelanieMelicious.furniturePack0"))
            // {
            //     GameObject PocketDimention = GameObject.Find("PocketRoom0(Clone)").transform.Find("Room0").gameObject;
            //     if (PocketDimention != null)
            //     {
            //         GameObject cubeA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //         Renderer renderer = cubeA.GetComponent<Renderer>();
            //         renderer.material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
            //         renderer.material.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            //         renderer.material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
            //         cubeA.transform.SetParent(PocketDimention.transform, true);
            //         cubeA.transform.localScale = new Vector3(17f, 1.1f, 7f);
            //         cubeA.transform.localPosition = new Vector3(1.3f, 0.4f, -1.5f);


            //         // Add NavMeshSurface component
            //         NavMeshSurface surface = cubeA.AddComponent<NavMeshSurface>();

            //         // Configure NavMeshSurface settings
            //         surface.collectObjects = CollectObjects.Children;
            //         surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            //         //surface.layerMask = LayerMask.GetMask("Default"); // Adjust this to match your layer setup
            //         surface.overrideVoxelSize = true;
            //         surface.voxelSize = 0.1f;
            //         surface.overrideTileSize = true;
            //         surface.tileSize = 16;

            //         // Bake the NavMeshSurface
            //         surface.BuildNavMesh();

            //         GameObject.Destroy(cubeA.GetComponent<Collider>());
            //         GameObject.Destroy(cubeA.GetComponent<Renderer>());
            //     }
            // }
        }

        [HarmonyPatch("ShipLeave")]
        [HarmonyPrefix]
        private static void SavePikmin()
        {
            PikminManager.Instance.HandlePikminWhenShipLeaving();
        }

        [HarmonyPatch("ShipHasLeft")]
        [HarmonyPostfix]
        private static void SyncEndgameData()
        {
            if (NetworkManager.Singleton.IsServer)
                PikminManager.Instance.SyncEndgameDataServerRpc();

            PikminManager.Instance.RemoveAllRadiuses();
            PikminManager.CurrentFloorData.Clear();
            PikminManager.DefultFloorData = null;
        }

        [HarmonyPatch("SetShipReadyToLand")]
        [HarmonyPostfix]
        private static void SaveOnions()
        {
            if (LethalMin.IsDependencyLoaded("Piggy.LCOffice"))
            {
                ResetPiggyElevator();
            }
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
            PikminManager.Instance.StartCoroutine(PikminManager.Instance.DespawnSprouts());
            PikminManager.Instance.StartCoroutine(PikminManager.Instance.DespawnOnions());
            GameObject.FindAnyObjectByType<PikminHUD>().UpdateHUD();
            if (!NetworkManager.Singleton.IsServer) { return; }
            PikminManager.Instance.SpawnShipPhaseOnionsServerRpc();
        }

        public static void ResetPiggyElevator()
        {
            PiggyElevatorSystemPatch.HasCreatedNavMeshOnElevate = false;
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