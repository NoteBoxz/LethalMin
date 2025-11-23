using GameNetcodeStuff;
using HarmonyLib;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class StartOfRoundPatch
    {
        [HarmonyPatch(nameof(StartOfRound.Start))]
        [HarmonyPostfix]
        private static void CreatePikminManager(StartOfRound __instance)
        {
            try
            {
                GameObject env = GameObject.Find("Environment");
                LethalMin.enviormentStartPos = env.transform.position;
                LethalMin.SSRenviourment = env;
                ItemArrivalZone.CreateZoneOnObject(__instance.shipAnimatorObject.gameObject, ItemArrivalZone.ArrivalZoneType.Ship);

                SaveManager.settings.path = GameNetworkManager.Instance.currentSaveFileName + "_LethalMinSave";
                if (!__instance.IsServer)
                {
                    return;
                }
                LethalMin.Logger.LogInfo("Creating PikminManager");
                if (PikminManager.instance == null)
                {
                    GameObject obj = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PikminManager.prefab");
                    NetworkObject netObj = GameObject.Instantiate(obj).GetComponent<NetworkObject>();
                    netObj.Spawn();
                }
                else
                {
                    LethalMin.Logger.LogWarning("PikminManager already exists");
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogFatal($"Failed to create PikminManager due to: {e}");
            }
        }

        [HarmonyPatch(nameof(StartOfRound.SetShipReadyToLand))]
        [HarmonyPostfix]
        public static void StartPikminManager(StartOfRound __instance)
        {
            PikminManager.instance.OnGameEnded();
        }


        [HarmonyPatch(nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        private static void StartPikminManagerA(StartOfRound __instance, ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (sceneName == __instance.currentLevel.sceneName && __instance.localPlayerController.OwnerClientId == clientId)
            {
                PikminManager.instance.OnGameStarted();
            }
        }

        [HarmonyPatch(nameof(StartOfRound.ShipLeave))]
        [HarmonyPrefix]
        private static void EndGame()
        {
            PikminManager.instance.OnGameEnd();
        }

        [HarmonyPatch(nameof(StartOfRound.SetShipReadyToLand))]
        [HarmonyPostfix]
        public static void SetShipReadyToLandPostfix(StartOfRound __instance)
        {
            //We then wait until the pikminmanager has finished saving. 
            //Becasue this is a post fix SaveGame should have been called before this.
            PikminManager.instance.StartCoroutine(PikminManager.instance.WaitToDespawnObjects());
        }

        [HarmonyPatch(nameof(StartOfRound.EndOfGameClientRpc))]
        [HarmonyPrefix]
        public static void EndOfGameClientRpcPrefix(StartOfRound __instance)
        {
            if (PikChecks.IsClientRpcPrefixValid(__instance) == false)
            {
                return;
            }

            // Hide every object that will be saved (We don't want to despawn the objects before they can be saved)
            foreach (Sprout spr in Object.FindObjectsOfType<Sprout>())
            {
                foreach (Renderer render in spr.GetComponentsInChildren<Renderer>())
                {
                    render.enabled = false;
                }
            }

            foreach (Onion oni in Object.FindObjectsOfType<Onion>())
            {
                if (oni.DontDespawnOnGameEnd)
                {
                    continue;
                }
                foreach (Renderer render in oni.GetComponentsInChildren<Renderer>())
                {
                    render.enabled = false;
                }
                foreach (AudioSource audio in oni.GetComponentsInChildren<AudioSource>())
                {
                    audio.enabled = false; // Disable audio to prevent sound from playing when despawning
                }
            }
        }

        [HarmonyPatch(nameof(StartOfRound.EndPlayersFiredSequenceClientRpc))]
        [HarmonyPrefix]
        public static void PurgeSave(StartOfRound __instance)
        {
            if (PikChecks.IsClientRpcPrefixValid(__instance) == false)
            {
                return;
            }
            
            PikminManager.instance.ClearSavedData();
        }

        [HarmonyPatch(nameof(StartOfRound.unloadSceneForAllPlayers))]
        [HarmonyPostfix]
        public static void unloadSceneForAllPlayersPostfix(StartOfRound __instance)
        {
            if (__instance.IsServer)
            {
                PikminManager.instance.SyncGameStatsClientRpc(PikminManager.instance.EndOfGameStats, PikminManager.instance.FiredStats);
            }
        }


        [HarmonyPatch(nameof(StartOfRound.FirePlayersAfterDeadlineClientRpc))]
        [HarmonyPostfix]
        public static void FirePlayersAfterDeadlineClientRpcPostfix(StartOfRound __instance)
        {
            if (HUDManager.Instance.EndOfRunStatsText.text.Contains("Pikmin Raised: "))
            {
                LethalMin.Logger.LogWarning("Pikmin Raised already exists");
                return;
            }
            HUDManager.Instance.EndOfRunStatsText.text += $"\nPikmin Raised: {PikminManager.instance.FiredStats.TotalPikminRaised}\n" + $"Pikmin Lost: {PikminManager.instance.FiredStats.TotalPikminLost}\n";
        }


        [HarmonyPatch(nameof(StartOfRound.SetPlayerSafeInShip))]
        [HarmonyPostfix]
        public static void SetPlayerSafeInShipPostFix(StartOfRound __instance)
        {
            try
            {
                if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
                {
                    return;
                }
                PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
                if (playerControllerB.isPlayerDead && playerControllerB.spectatedPlayerScript != null)
                {
                    playerControllerB = playerControllerB.spectatedPlayerScript;
                }

                if (__instance.hangarDoorsClosed && GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
                {
                    foreach (var pikminAI in PikminManager.instance.PikminAIs)
                    {
                        pikminAI.EnableEnemyMesh(true);
                    }
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Failed to show pikmin within ship! {e}");
            }
        }
    }
}
