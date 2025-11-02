using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    public class GrabbableObjectPatch
    {
        public static HashSet<Item> ItemsModified = new HashSet<Item>();

        [HarmonyPatch(nameof(GrabbableObject.Start))]
        [HarmonyPrefix]
        private static void StartPatch(GrabbableObject __instance)
        {
            if (__instance.GetComponentInChildren<PikminItem>() == null)
                CreatePikminItemOnGrabbableObject(__instance);
        }

        public static PikminItem? CreatePikminItemOnGrabbableObject(Item Iprop, bool OverrideGrabableChecks = false)
        {
            if (Iprop == null)
            {
                LethalMin.Logger.LogError("Item is null. Cannot create PikminItem.");
                return null;
            }
            if (ItemsModified.Contains(Iprop))
            {
                return null;
            }
            if (Iprop.spawnPrefab == null)
            {
                LethalMin.Logger.LogWarning($"{Iprop.itemName}'s spawnPrefab is null. Cannot create PikminItem.");
                return null;
            }
            GrabbableObject __instance = Iprop.spawnPrefab.GetComponentInChildren<GrabbableObject>();
            if (__instance == null)
            {
                LethalMin.Logger.LogWarning($"{Iprop.itemName}'s GrabbableObject is null. Cannot create PikminItem.");
                return null;
            }
            if (LethalMin.IsDependencyLoaded("NoteBoxz.LethalMinLibrary"))
            {
                TryReplacaeLibSettings(__instance);
            }
            if (__instance.TryGetComponent(out PikminItemSettings settings))
            {
                if (settings.DontInitalizeOnStartup)
                {
                    LethalMin.Logger.LogInfo($"Skipping PikminItem creation for {Iprop.itemName} due to settings.");
                    return null;
                }
            }
            ItemsModified.Add(Iprop);
            return CreatePikminItemOnGrabbableObject(__instance, OverrideGrabableChecks, true);
        }
        public static PikminItem? CreatePikminItemOnGrabbableObject(GrabbableObject __instance, bool OverrideGrabableChecks = false, bool AddDirectlyToObject = false)
        {
            if (!AddDirectlyToObject && !__instance.IsServer)
            {
                return null;
            }
            if (__instance == null)
            {
                LethalMin.Logger.LogError("GrabbableObject is null. Cannot create PikminItem.");
                return null;
            }
            if(__instance.itemProperties == null)
            {
                LethalMin.Logger.LogWarning($"GrabbableObject {__instance.name} has no itemProperties. Cannot create PikminItem.");
                return null;
            }
            if (__instance.GetComponentInChildren<PikminItem>() != null)
            {
                // If a PikminItem already exists, no need to create another one
                LethalMin.Logger.LogWarning($"PikminItem already exists for {__instance.name}. Skipping creation.");
                __instance.GetComponentInChildren<PikminItem>().InitalizeClientRpc(__instance.NetworkObject, __instance.name);
                return null;
            }
            bool check = false;
            if (LethalMin.IsDependencyLoaded("Entity378.sellbodies"))
            {
                check = PikChecks.IsGrabbableBodieFixed(__instance);
            }
            if (OverrideGrabableChecks)
            {
                check = true;
            }
            if (!PikChecks.IsItemValid(__instance) && !check)
            {
                LethalMin.Logger.LogWarning($"Item {__instance.name} is not valid for Pikmin! Skipping...");
                return null;
            }
            if (LethalMin.IsDependencyLoaded("NoteBoxz.LethalMinLibrary"))
            {
                TryReplacaeLibSettings(__instance);
            }
            if (!AddDirectlyToObject)
            {
                LethalMin.Logger.LogInfo($"Creating PikminItemNode for {__instance.name} at {__instance.transform.position}...");
                GameObject Node = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PikminItemNode.prefab");
                GameObject nodeInstance = GameObject.Instantiate(Node, __instance.transform);
                NetworkObject networkObject = nodeInstance.GetComponent<NetworkObject>();
                PikminItem pikminItemNode = nodeInstance.GetComponent<PikminItem>();
                networkObject.Spawn();
                networkObject.TrySetParent(__instance.NetworkObject, true);
                pikminItemNode.InitalizeClientRpc(__instance.NetworkObject, __instance.name);
                return pikminItemNode;
            }
            else
            {
                LethalMin.Logger.LogInfo($"Adding PikminItem Compoent to {__instance.name}");
                PikminItem pikminItemNode = __instance.gameObject.AddComponent<PikminItem>();
                pikminItemNode.DontUseInitClientRpc = true;
                pikminItemNode.ItemScript = __instance;
                pikminItemNode.DefultColor = new Color(1, 0.117647059f, 0f);
                PikminItemSettings settings = null!;
                if (!__instance.gameObject.TryGetComponent(out settings))
                {
                    settings = __instance.gameObject.AddComponent<PikminItemSettings>();
                    settings.CarryStrength = PikUtils.CalculatePikminItemWeight(__instance);
                    bool check2 = false;
                    if (LethalMin.IsDependencyLoaded("Entity378.sellbodies"))
                    {
                        check2 = PikChecks.IsGrabbableBodieFixed(__instance);
                    }
                    if (check2)
                    {
                        LethalMin.Logger.LogDebug($"GrabbableObject {__instance.gameObject.name} is a grabbable body!");
                        settings.CanProduceSprouts = true;
                        settings.OverrideGrabbableToEnemeis = true;
                        settings.SproutsToSpawn = settings.CarryStrength;
                    }
                }
                // ;)
                if (__instance.name == "Magic7Ball")
                    settings.CarryStrength = 7;
                pikminItemNode.CarryStrengthNeeded = settings.CarryStrength;
                pikminItemNode.settings = settings;
                pikminItemNode.CreateGrabPositions();
                pikminItemNode.GrabPositionContainer.gameObject.SetActive(false);
                pikminItemNode.GrabPositionContainer.transform.SetParent(pikminItemNode.transform);
                pikminItemNode.CreateCounter();
                pikminItemNode.PikminCounter.gameObject.SetActive(false);
                pikminItemNode.PikminCounter.transform.SetParent(pikminItemNode.transform);
                pikminItemNode.AlreadyPartalInitalized = true;
                return pikminItemNode;
            }
        }
        public static void TryReplacaeLibSettings(GrabbableObject __instance)
        {
            if (__instance.TryGetComponent(out LethalMinLibrary.PikminItemSettings settings)
            && !__instance.TryGetComponent(out PikminItemSettings _))
            {
                settings.Initialize();
            }
        }
    }
}
