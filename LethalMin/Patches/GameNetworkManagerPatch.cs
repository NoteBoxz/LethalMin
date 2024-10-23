using HarmonyLib;
using UnityEngine;
using Unity.Netcode;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch
    {
        public static bool HasInitalized;
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Init(GameNetworkManager __instance)
        {
            if (HasInitalized == true) { LethalMin.Logger.LogWarning("Already initalized LethalMin"); return; }
            GameObject pikminPrefab = LethalMin.pikminPrefab;
            GameObject SproutPrefab = LethalMin.sproutPrefab;
            GameObject OnionPrefab = LethalMin.OnionPrefab;
            GameObject OnionItemPrefab = LethalMin.OnionItemPrefab;
            GameObject leaderManagerPrefab = LethalMin.leaderManagerPrefab;
            GameObject PikminObjectPrefab = LethalMin.PikminObjectPrefab;
            GameObject WhistlePrefab = LethalMin.WhistlePrefab;
            GameObject GhostPrefab = LethalMin.Ghost;
            GameObject OnionMeunPrefab = LethalMin.CallminUI;
            GameObject PmanPrefabb = LethalMin.PmanPrefab;
            GameObject ShipOnionPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/ShipOnion.prefab");
            GameObject AttackerPrefab = LethalMin.PikminAttackerNode;
            GameObject ManeaterPrefab = LethalMin.EaterBehavior;
            GameObject NoticePrefab = LethalMin.NoticeZone;

            if (NoticePrefab != null)
            {
                NoticePrefab.AddComponent<NoticeZone>();
                NetworkManager.Singleton.AddNetworkPrefab(NoticePrefab);
            }

            if (ManeaterPrefab != null)
            {
                ManeaterPrefab.AddComponent<EaterBehavior>();
                NetworkManager.Singleton.AddNetworkPrefab(ManeaterPrefab);
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load EaterBehavior.prefab!");
            }

            if (AttackerPrefab != null)
            {
                AttackerPrefab.AddComponent<PikminAttacker>();
                NetworkManager.Singleton.AddNetworkPrefab(AttackerPrefab);
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load PikminAttacker.prefab!");
            }

            if (pikminPrefab != null)
            {
                NetworkManager.Singleton.AddNetworkPrefab(pikminPrefab);
                LethalMin.Logger.LogInfo("Added Pikmin.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load Pikmin.prefab!");
            }

            if (PmanPrefabb != null)
            {
                PmanPrefabb.AddComponent<PikminManager>();
                PmanPrefabb.AddComponent<DebugMenu>();

                NetworkManager.Singleton.AddNetworkPrefab(PmanPrefabb);
                LethalMin.Logger.LogInfo("Added PmanPrefabb.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load PmanPrefabb.prefab!");
            }

            if (leaderManagerPrefab != null)
            {
                leaderManagerPrefab.AddComponent<LeaderManager>();
                NetworkManager.Singleton.AddNetworkPrefab(leaderManagerPrefab);
                LethalMin.Logger.LogInfo("Added LeaderManager.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load LeaderManager.prefab!");
            }

            if (SproutPrefab != null)
            {
                SproutPrefab.AddComponent<Sprout>();
                NetworkManager.Singleton.AddNetworkPrefab(SproutPrefab);
                LethalMin.Logger.LogInfo("Added Sprout.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load Sprout.prefab!");
            }
            if (GhostPrefab != null)
            {
                GhostPrefab.AddComponent<PminGhost>();
                //NetworkManager.Singleton.AddNetworkPrefab(GhostPrefab);
                LethalMin.Logger.LogInfo("Added Ghost.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load LeaderManager.prefab!");
            }


            if (OnionItemPrefab != null)
            {
                OnionItem onionItem = OnionItemPrefab.AddComponent<OnionItem>();
                PhysicsProp physicsProp = OnionItemPrefab.GetComponent<PhysicsProp>();

                if (physicsProp != null)
                {
                    // Copy relevant properties from PhysicsProp to onionItem
                    CopyGrabbableObjectProperties2(physicsProp, onionItem);

                    // Remove the PhysicsProp component as we no longer need it
                    Object.Destroy(physicsProp);
                }

                NetworkManager.Singleton.AddNetworkPrefab(OnionItemPrefab);
                LethalMin.Logger.LogInfo("Added OnionItem.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load OnionItem.prefab!");
            }

            if (PikminObjectPrefab != null)
            {
                PikminObjectPrefab.AddComponent<PikminItem>();
                NetworkManager.Singleton.AddNetworkPrefab(PikminObjectPrefab);
                LethalMin.Logger.LogInfo("Added PikminObject.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load PikminObject.prefab!");
            }
            if (OnionMeunPrefab != null)
            {
                OnionMeunPrefab.AddComponent<OnionMenuManager>();
                LethalMin.Logger.LogInfo("Added OnionMeunPrefab.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load OnionMeunPrefab.prefab!");
            }
            if (ShipOnionPrefab != null)
            {
                ShipOnionPrefab.AddComponent<DualOnion>().type = AssetLoader.LoadAsset<OnionType>("Assets/LethalminAssets/Pikmin/Types 2/ShipOnion.asset");
                NetworkManager.Singleton.AddNetworkPrefab(ShipOnionPrefab);
                LethalMin.Logger.LogInfo("Added ShipOnionPrefab.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load ShipOnionPrefab.prefab!");
            }
            if (OnionPrefab != null)
            {
                OnionPrefab.AddComponent<AnimatedOnion>();
                NetworkManager.Singleton.AddNetworkPrefab(OnionPrefab);
                LethalMin.Logger.LogInfo("Added Onion.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load Onion.prefab!");
            }
            if (WhistlePrefab != null)
            {
                WhistleItem whistleItem = WhistlePrefab.AddComponent<WhistleItem>();
                PhysicsProp physicsProp = WhistlePrefab.GetComponent<PhysicsProp>();
                whistleItem.lineRenderer = WhistlePrefab.AddComponent<LineRenderer>();

                if (physicsProp != null)
                {
                    // Copy relevant properties from PhysicsProp to WhistleItem
                    CopyGrabbableObjectProperties(physicsProp, whistleItem);

                    // Remove the PhysicsProp component as we no longer need it
                    Object.Destroy(physicsProp);
                }

                NetworkManager.Singleton.AddNetworkPrefab(WhistlePrefab);
                LethalMin.Logger.LogInfo("Added Whistle.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load Whistle.prefab!");
            }
            HasInitalized = true;
        }
        private static void CopyGrabbableObjectProperties(PhysicsProp source, WhistleItem destination)
        {
            // Copy all relevant properties from GrabbableObject
            destination.grabbable = source.grabbable;
            destination.isHeld = source.isHeld;
            destination.isHeldByEnemy = source.isHeldByEnemy;
            destination.deactivated = source.deactivated;
            destination.parentObject = source.parentObject;
            destination.targetFloorPosition = source.targetFloorPosition;
            destination.startFallingPosition = source.startFallingPosition;
            destination.floorYRot = source.floorYRot;
            destination.fallTime = source.fallTime;
            destination.hasHitGround = source.hasHitGround;
            destination.scrapValue = source.scrapValue;
            destination.itemUsedUp = source.itemUsedUp;
            destination.playerHeldBy = source.playerHeldBy;
            destination.isPocketed = source.isPocketed;
            destination.isBeingUsed = source.isBeingUsed;
            destination.isInElevator = source.isInElevator;
            destination.isInShipRoom = source.isInShipRoom;
            destination.isInFactory = source.isInFactory;
            destination.useCooldown = source.useCooldown;
            destination.currentUseCooldown = source.currentUseCooldown;
            destination.itemProperties = source.itemProperties;
            destination.insertedBattery = source.insertedBattery;
            destination.customGrabTooltip = source.customGrabTooltip;
            destination.propBody = source.propBody;
            destination.propColliders = source.propColliders;
            destination.originalScale = source.originalScale;
            destination.wasOwnerLastFrame = source.wasOwnerLastFrame;
            destination.mainObjectRenderer = source.mainObjectRenderer;
            destination.isSendingItemRPC = source.isSendingItemRPC;
            destination.scrapPersistedThroughRounds = source.scrapPersistedThroughRounds;
            destination.heldByPlayerOnServer = source.heldByPlayerOnServer;
            destination.radarIcon = source.radarIcon;
            destination.reachedFloorTarget = source.reachedFloorTarget;
            destination.grabbableToEnemies = source.grabbableToEnemies;
            destination.hasBeenHeld = source.hasBeenHeld;
        }
        private static void CopyGrabbableObjectProperties2(PhysicsProp source, OnionItem destination)
        {
            // Copy all relevant properties from GrabbableObject
            destination.grabbable = source.grabbable;
            destination.isHeld = source.isHeld;
            destination.isHeldByEnemy = source.isHeldByEnemy;
            destination.deactivated = source.deactivated;
            destination.parentObject = source.parentObject;
            destination.targetFloorPosition = source.targetFloorPosition;
            destination.startFallingPosition = source.startFallingPosition;
            destination.floorYRot = source.floorYRot;
            destination.fallTime = source.fallTime;
            destination.hasHitGround = source.hasHitGround;
            destination.scrapValue = source.scrapValue;
            destination.itemUsedUp = source.itemUsedUp;
            destination.playerHeldBy = source.playerHeldBy;
            destination.isPocketed = source.isPocketed;
            destination.isBeingUsed = source.isBeingUsed;
            destination.isInElevator = source.isInElevator;
            destination.isInShipRoom = source.isInShipRoom;
            destination.isInFactory = source.isInFactory;
            destination.useCooldown = source.useCooldown;
            destination.currentUseCooldown = source.currentUseCooldown;
            destination.itemProperties = source.itemProperties;
            destination.insertedBattery = source.insertedBattery;
            destination.customGrabTooltip = source.customGrabTooltip;
            destination.propBody = source.propBody;
            destination.propColliders = source.propColliders;
            destination.originalScale = source.originalScale;
            destination.wasOwnerLastFrame = source.wasOwnerLastFrame;
            destination.mainObjectRenderer = source.mainObjectRenderer;
            destination.isSendingItemRPC = source.isSendingItemRPC;
            destination.scrapPersistedThroughRounds = source.scrapPersistedThroughRounds;
            destination.heldByPlayerOnServer = source.heldByPlayerOnServer;
            destination.radarIcon = source.radarIcon;
            destination.reachedFloorTarget = source.reachedFloorTarget;
            destination.grabbableToEnemies = source.grabbableToEnemies;
            destination.hasBeenHeld = source.hasBeenHeld;
        }
    }
}