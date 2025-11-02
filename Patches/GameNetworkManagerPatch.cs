using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    public class GameNetworkManagerPatch
    {
        public static bool HasRegistred = false;
        [HarmonyPatch(nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        private static void StartPostfix(GameNetworkManager __instance)
        {
            if (HasRegistred)
            {
                LethalMin.Logger.LogWarning("LethalMin: Already registered prefabs to Network.");
                return;
            }
            GameObject go = new GameObject("LethalMin AssetBundleLoader");
            AssetBundleLoader.instance = go.AddComponent<AssetBundleLoader>();
            GameObject.DontDestroyOnLoad(go);
            AssetBundleLoader.instance.Initialize();
            foreach (GameObject gameObject in LethalMin.assetBundle.LoadAllAssets<GameObject>())
            {
                if (gameObject.GetComponent<NetworkObject>() == null) continue;
                if (gameObject.GetComponent<NetworkObject>().enabled == false) continue;
                __instance.GetComponent<NetworkManager>().AddNetworkPrefab(gameObject);
                LethalMin.Logger.LogInfo($"Registered Prefab to Network: {gameObject.name}");
            }
            HasRegistred = true;
        }


        [HarmonyPatch(nameof(GameNetworkManager.SaveGame))]
        [HarmonyPostfix]
        private static void SaveGamePostfix(GameNetworkManager __instance)
        {
            if (StartOfRound.Instance.inShipPhase && NetworkManager.Singleton.IsServer)
                PikminManager.instance.SaveData();
        }
    }
}
