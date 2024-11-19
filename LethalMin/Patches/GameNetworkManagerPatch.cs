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
            if (LethalMin.AnimSproutPrefab != null)
            {
                LethalMin.AnimSproutPrefab.AddComponent<AnimatedSprout>();
                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.AnimSproutPrefab);
                LethalMin.Logger.LogInfo("Added AnimatedSprout.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load AnimatedSprout.prefab!");
            }

            if (LethalMin.POMprefab != null)
            {
                LethalMin.POMprefab.AddComponent<PuffminOwnerManager>();
                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.POMprefab);
                LethalMin.Logger.LogInfo("Added PuffminOwnerManager.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load PuffminOwnerManager.prefab!");
            }

            if (LethalMin.PuffminPrefab != null)
            {
                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.PuffminPrefab);
                LethalMin.Logger.LogInfo("Added Puffmin.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load Puffmin.prefab!");
            }

            if (LethalMin.NoticeZone != null)
            {
                LethalMin.NoticeZone.AddComponent<NoticeZone>();
                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.NoticeZone);
                LethalMin.Logger.LogInfo("Added NoticeZone.prefab to network prefabs!");
            }

            if (LethalMin.EaterBehavior != null)
            {
                LethalMin.EaterBehavior.AddComponent<EaterBehavior>();
                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.EaterBehavior);
                LethalMin.Logger.LogInfo("Added EaterBehavior.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load EaterBehavior.prefab!");
            }

            if (LethalMin.PikminAttackerNode != null)
            {
                LethalMin.PikminAttackerNode.AddComponent<PikminAttacker>();
                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.PikminAttackerNode);
                LethalMin.Logger.LogInfo("Added PikminAttacker.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load PikminAttacker.prefab!");
            }

            if (LethalMin.pikminPrefab != null)
            {
                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.pikminPrefab);
                LethalMin.Logger.LogInfo("Added Pikmin.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load Pikmin.prefab!");
            }

            if (LethalMin.PmanPrefab != null)
            {
                LethalMin.PmanPrefab.AddComponent<PikminManager>();
                LethalMin.PmanPrefab.AddComponent<DebugMenu>();

                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.PmanPrefab);
                LethalMin.Logger.LogInfo("Added LethalMin.PmanPrefab.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load LethalMin.PmanPrefab.prefab!");
            }

            if ( LethalMin.leaderManagerPrefab != null)
            {
                 LethalMin.leaderManagerPrefab.AddComponent<LeaderManager>();
                NetworkManager.Singleton.AddNetworkPrefab( LethalMin.leaderManagerPrefab);
                LethalMin.Logger.LogInfo("Added LeaderManager.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load LeaderManager.prefab!");
            }

            if (LethalMin.sproutPrefab != null)
            {
                LethalMin.sproutPrefab.AddComponent<Sprout>();
                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.sproutPrefab);
                LethalMin.Logger.LogInfo("Added Sprout.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load Sprout.prefab!");
            }
            if (LethalMin.Ghost != null)
            {
                LethalMin.Ghost.AddComponent<PminGhost>();
                //NetworkManager.Singleton.AddNetworkPrefab(LethalMin.Ghost);
                LethalMin.Logger.LogInfo("Added Ghost.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load LeaderManager.prefab!");
            }


            if (LethalMin.OnionItemPrefab != null)
            {
                OnionItem onionItem = LethalMin.OnionItemPrefab.AddComponent<OnionItem>();
                PhysicsProp physicsProp = LethalMin.OnionItemPrefab.GetComponent<PhysicsProp>();

                if (physicsProp != null)
                {
                    // Copy relevant properties from PhysicsProp to onionItem
                    CopyGrabbableObjectProperties2(physicsProp, onionItem);

                    // Remove the PhysicsProp component as we no longer need it
                    Object.Destroy(physicsProp);
                }

                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.OnionItemPrefab);
                LethalMin.Logger.LogInfo("Added OnionItem.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load OnionItem.prefab!");
            }

            if (LethalMin.PikminObjectPrefab != null)
            {
                LethalMin.PikminObjectPrefab.AddComponent<PikminItem>();
                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.PikminObjectPrefab);
                LethalMin.Logger.LogInfo("Added PikminObject.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load PikminObject.prefab!");
            }
            if (LethalMin.CallminUI != null)
            {
                LethalMin.CallminUI.AddComponent<OnionMenuManager>();
                LethalMin.Logger.LogInfo("Added LethalMin.CallminUI.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load LethalMin.CallminUI.prefab!");
            }
            if (AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/ShipOnion.prefab") != null)
            {
                AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/ShipOnion.prefab").AddComponent<DualOnion>().type = AssetLoader.LoadAsset<OnionType>("Assets/LethalminAssets/Pikmin/Types 2/ShipOnion.asset");
                NetworkManager.Singleton.AddNetworkPrefab(AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/ShipOnion.prefab"));
                LethalMin.Logger.LogInfo("Added Ship ONion to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load Ship ONion.prefab!");
            }
            if (LethalMin.OnionPrefab != null)
            {
                LethalMin.OnionPrefab.AddComponent<AnimatedOnion>();
                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.OnionPrefab);
                LethalMin.Logger.LogInfo("Added Onion.prefab to network prefabs!");
            }
            else
            {
                LethalMin.Logger.LogError("Failed to load Onion.prefab!");
            }
            if (LethalMin.WhistlePrefab != null)
            {
                WhistleItem whistleItem = LethalMin.WhistlePrefab.AddComponent<WhistleItem>();
                PhysicsProp physicsProp = LethalMin.WhistlePrefab.GetComponent<PhysicsProp>();
                whistleItem.lineRenderer = LethalMin.WhistlePrefab.AddComponent<LineRenderer>();

                if (physicsProp != null)
                {
                    // Copy relevant properties from PhysicsProp to WhistleItem
                    CopyGrabbableObjectProperties(physicsProp, whistleItem);

                    // Remove the PhysicsProp component as we no longer need it
                    Object.Destroy(physicsProp);
                }

                NetworkManager.Singleton.AddNetworkPrefab(LethalMin.WhistlePrefab);
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