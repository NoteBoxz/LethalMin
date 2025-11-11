using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalLib.Modules;
using LethalMin.Pikmin;
using LethalMin.Routeing;
using LethalMin.Utils;
using TMPro;
using Unity.Mathematics;
using Unity.Multiplayer.Tools.NetStats;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Events;

namespace LethalMin
{
    public class PikminItem : NetworkBehaviour
    {
        public GrabbableObject ItemScript = null!;
        public Transform GrabPositionContainer = null!;
        /// <summary>
        /// false = avaible, true = not avaible
        /// </summary>
        public List<BoolValue> GrabToPositions = new List<BoolValue>();
        public int CarryStrengthNeeded = -1;
        public int TotalCarryStrength = 0;
        public List<PikminAI> PikminOnItem = new List<PikminAI>();
        public PikminAI? PrimaryPikminOnItem;
        public PikminItemCounter PikminCounter = null!;
        public PikminItemSettings settings = null!;
        public bool IsBeingCarried;
        public bool HasInitalized;
        public bool HasArrived;
        public Vector3 ArrivePosition = Vector3.zero;
        public PikminType TargetPikminTypeOnion = null!;
        public Onion? TargetOnion = null;
        public Color DefultColor = new Color(1f, 0.1176471f, 0f, 1f); // Default color for counter
        public HoarderBugItem? hoarderBugItem;

        protected PikminAI? previousPrimaryPikmin;
        protected PikminAI lastPrimaryPikminOnItem = null!;
        public PikminRoute CurrentRoute = null!;
        protected Coroutine soundRoutine = null!;
        public UnityEvent<PikminItem> OnItemGrabbed = new UnityEvent<PikminItem>();
        public UnityEvent<PikminItem> OnItemDropped = new UnityEvent<PikminItem>();
        public bool HasOwnNetworkObject = true;
        float RouteRecallInterval = 0.2f;
        public Leader? PrimaryLeader = null;
        public List<Renderer> ExtraRenderers = new List<Renderer>();
        public bool DontUseInitClientRpc = false;
        public bool AlreadyPartalInitalized = false;
        bool ShouldGrab => !ItemScript.isHeld && TotalCarryStrength >= CarryStrengthNeeded && !IsBeingCarried && IsOwner;
        bool HadItemScript = false;
        bool isRegeneratingRoute = false;
        [HideInInspector]
        public EnemyGrabbableObject hackEnemyGrabbableObject = null!;
        float TimeSinceArrived = 0f;
        bool arrivedOnSpawn = false;




        #region Initalizeation
        public void Start()
        {
            if (DontUseInitClientRpc)
            {
                ItemScript = GetComponent<GrabbableObject>();
                Initalize();
            }
        }

        /// <summary>
        /// This MUST be called right after it's spawned
        /// </summary>
        /// <param name="Ref">The Grabable object's network object</param>
        /// <param name="itemName">4 debug</param>
        [ClientRpc]
        public void InitalizeClientRpc(NetworkObjectReference Ref, string itemName = "")
        {
            if (HasInitalized)
            {
                LethalMin.Logger.LogWarning($"PikminItem {gameObject.name} has already been initialized. Skipping re-initialization.");
                return;
            }

            if (ItemScript != null)
            {
                LethalMin.Logger.LogWarning($"PikminItem {gameObject.name} already has an ItemScript");
                Initalize();
                return;
            }

            NetworkObject obj;
            if (!Ref.TryGet(out obj))
            {
                LethalMin.Logger.LogError($"{gameObject.name} Failed to get NetworkObject");
                if (IsServer)
                {
                    NetworkObject.Despawn(true);
                }
                return;
            }
            if (!obj.TryGetComponent(out ItemScript))
            {
                LethalMin.Logger.LogError($"{gameObject.name} Failed to get ItemScript");
                if (IsServer)
                {
                    NetworkObject.Despawn(true);
                }
                return;
            }

            Initalize();
        }

        public void Initalize()
        {
            LethalMin.Logger.LogDebug($"Initializing PikminItem {gameObject.name} with item {gameObject.name} ({HasOwnNetworkObject})");

            if (TryGetComponent(out GrabbableObject obj) && obj == ItemScript)
            {
                HasOwnNetworkObject = false;
            }
            else
            {
                HasOwnNetworkObject = true;
            }

            GetorCreateSettings();

            if (HasOwnNetworkObject || gameObject.name == "PikminItemNode(Clone)")
            {
                LethalMin.Logger.LogDebug($"A {HasOwnNetworkObject}, B {gameObject.name == "PikminItemNode(Clone)"} rename reasons");
                gameObject.name = ItemScript.itemProperties.itemName + " (PikminItem Node)";
            }
            CarryStrengthNeeded = settings.CarryStrength;
            ExtraRenderers = settings.ExtraRenderers;

            if (AlreadyPartalInitalized)
            {
                GrabPositionContainer.SetParent(null);
                PikminCounter.transform.SetParent(null);
                GrabPositionContainer.gameObject.SetActive(true);
                PikminCounter.gameObject.SetActive(true);
            }
            else
            {
                CreateGrabPositions();
            }
            if (hackEnemyGrabbableObject != null && hackEnemyGrabbableObject.ai != null && hackEnemyGrabbableObject.ai is MaskedPlayerEnemy)
            {
                ArrivePosition = hackEnemyGrabbableObject.ai.transform.position;
                TimeSinceArrived = Time.time;
                HasArrived = true;
                arrivedOnSpawn = true;
                LethalMin.Logger.LogMessage($"PikminItem {gameObject.name} has arrived at {ArrivePosition} ({TimeSinceArrived}) because it was spawned from a masked");
            }
            PikminManager.instance.AddPikminItem(this);
            HasInitalized = true;
        }

        public void CreateGrabPositions()
        {
            int numberOfPositions = CarryStrengthNeeded * 2; // You can adjust this number as needed
            float radius = 1f; // Adjust this based on the size of your collider

            // Get the collider of the ItemScript
            Collider itemCollider = null!;
            if (settings.OverrideGrabPostionColider != null)
            {
                itemCollider = settings.OverrideGrabPostionColider;
            }
            else
            {
                itemCollider = ItemScript.GetComponentInChildren<Collider>();
            }
            LethalMin.Logger.LogDebug($"({gameObject.name}) Colider: {itemCollider}");
            LethalMin.Logger.LogDebug($"({gameObject.name}) Center: {itemCollider.bounds.center}");
            if (itemCollider == null)
            {
                LethalMin.Logger.LogWarning($"PikminItem {gameObject.name} has no collider");
                return;
            }
            if (GrabPositionContainer == null)
            {
                GrabPositionContainer = new GameObject($"{gameObject.name}'s GrabPositions").transform;
                GrabPositionContainer.transform.rotation = quaternion.identity;
                GrabPositionContainer.localPosition = ItemScript.transform.position;
            }

            // Calculate the center and adjust the radius based on the collider bounds
            Vector3 center = ItemScript.transform.position;
            radius = Mathf.Max(itemCollider.bounds.extents.x, itemCollider.bounds.extents.z) + 0.5f;

            GrabToPositions.Clear();
            for (int i = 0; i < numberOfPositions; i++)
            {
                float angle = i * (360f / numberOfPositions);
                Vector3 pos = CalculatePositionOnCircle(center, radius, angle);
                //LethalMin.Logger.LogInfo($"Pos: {pos}");

                // Create a new GameObject for each position
                GameObject positionObject = Instantiate(LethalMin.GrabPosPrefab);
                positionObject.name = $"GrabPosition_{i}";
                positionObject.transform.position = pos;
                positionObject.transform.SetParent(GrabPositionContainer);

                // Add to the list
                GrabToPositions.Add(positionObject.GetComponent<BoolValue>());
            }
            GrabToPositions.Sort((a, b) => a.gameObject.name.CompareTo(b.gameObject.name));
        }

        private Vector3 CalculatePositionOnCircle(Vector3 center, float radius, float angleDegrees)
        {
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            float x = center.x + radius * Mathf.Cos(angleRadians);
            float z = center.z + radius * Mathf.Sin(angleRadians);
            return new Vector3(x, center.y, z);
        }
        public void CreateCounter()
        {
            if (PikminCounter != null)
                return;

            LethalMin.Logger.LogDebug($"Creating counter for {gameObject.name}");
            PikminCounter = Instantiate(LethalMin.ItemCounterPrefab).GetComponent<PikminItemCounter>();
            PikminCounter.item = this;
            PikminCounter.SetCounterColor(DefultColor);
            PikminCounter.name = $"{gameObject.name}'s Counter";
        }


        public void GetorCreateSettings()
        {
            if (AlreadyPartalInitalized)
            {
                return;
            }

            bool check = false;
            if (LethalMin.IsDependencyLoaded("Entity378.sellbodies"))
            {
                check = PikChecks.IsGrabbableBodieFixed(ItemScript);
            }
            if (LethalMin.AllowOnionsToRevivePlayers &&
                !StartOfRound.Instance.allPlayersDead &&
                ItemScript.GetComponent<RagdollGrabbableObject>() != null)
            {
                check = true;
            }
            if (!ItemScript.gameObject.TryGetComponent(out settings))
            {
                settings = ItemScript.gameObject.AddComponent<PikminItemSettings>();
                if (check)
                {
                    LethalMin.Logger.LogDebug($"GrabbableObject {ItemScript.gameObject.name} is a grabbable body!");
                    settings.CanProduceSprouts = true;
                    settings.OverrideGrabbableToEnemeis = true;
                    settings.SproutsToSpawn = settings.CarryStrength;
                }
            }
            if (settings.CarryStrength == -1)
            {
                LethalMin.Logger.LogInfo($"PikminItem {gameObject.name} has no carry strength set, setting to default");
                int numb = PikUtils.CalculatePikminItemWeight(ItemScript);
                settings.CarryStrength = numb;
                settings.SproutsToSpawn = numb;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            PikminManager.instance.RemovePikminItem(this);
            if (IsOwner)
            {
                RemoveAllPikminFromItemServerRpc();
            }
            ClearCurrentRoute();
            if (GrabPositionContainer != null)
                Destroy(GrabPositionContainer.gameObject);
            if (PikminCounter != null)
                Destroy(PikminCounter.gameObject);
        }

        #endregion





        #region Grabbing
        public BoolValue GetNearestGrabPosition(Vector3 Position)
        {
            float closestDistance = float.MaxValue;
            BoolValue closestPosition = null!;
            foreach (var position in GrabToPositions)
            {
                if (position.value == true)
                {
                    continue;
                }
                float distance = Vector3.Distance(position.transform.position, Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPosition = position;
                }
            }
            return closestPosition;
        }
        [ServerRpc]
        public void GrabPikminItemServerRpc()
        {
            if (settings.ChangeOwnershipOnCarry && OwnerClientId != PikminOnItem[0].OwnerClientId)
            {
                if (HasOwnNetworkObject)
                    ItemScript.NetworkObject.ChangeOwnership(PikminOnItem[0].OwnerClientId);

                NetworkObject.ChangeOwnership(PikminOnItem[0].OwnerClientId);
            }
            GrabPikminItemClientRpc();
        }

        [ClientRpc]
        public void GrabPikminItemClientRpc()
        {
            if (!IsOwner)
                GrabPikminItemOnLocalClient();
        }

        public virtual void GrabPikminItemOnLocalClient()
        {
            LethalMin.Logger.LogInfo($"{gameObject.name} is grabbing an item on local client");
            if (PrimaryPikminOnItem == null)
            {
                return;
            }

            if (soundRoutine != null)
            {
                StopCoroutine(soundRoutine);
                soundRoutine = null!;
            }
            soundRoutine = StartCoroutine(CarryNumerator());

            ItemScript.parentObject = PrimaryPikminOnItem.HoldPosition;
            ItemScript.hasHitGround = false;
            ItemScript.isHeldByEnemy = true;
            ItemScript.GrabItemFromEnemy(PrimaryPikminOnItem);
            ItemScript.EnablePhysics(enable: false);
            PrimaryPikminOnItem.SetCollisionMode(1);
            bool ShouldTakeItemToOnion = true;
            if (LethalMin.TakeItemsFromPikmin)
            {
                foreach (var item in ItemScript.propColliders)
                {
                    item.enabled = true;
                }
            }

            if (LethalMin.OnCompany && !LethalMin.TakeItemsToOnionOnCompany.InternalValue)
            {
                ShouldTakeItemToOnion = false;
            }

            if (settings.CanProduceSprouts && ShouldTakeItemToOnion)
            {
                (PikminType, Onion)? tuple = GetPriotizedPikminType();
                if (tuple != null)
                {
                    TargetPikminTypeOnion = tuple.Value.Item1;
                    TargetOnion = tuple.Value.Item2;
                    LethalMin.Logger.LogInfo($"{gameObject.name}: Target Pikmin Type: {TargetPikminTypeOnion}, Target Onion: {TargetOnion}");
                    PikminCounter.SetCounterColor(tuple.Value.Item1.PikminPrimaryColor * 0.75f, tuple.Value.Item1.PikminSecondaryColor * 0.75f);
                }
                else
                {
                    LethalMin.Logger.LogWarning($"No valid onions found for {gameObject.name} when grabbing item");
                    TargetPikminTypeOnion = null!;
                    TargetOnion = null!;
                    PikminCounter.SetCounterColor(DefultColor);
                }
            }
            else
            {
                TargetPikminTypeOnion = null!;
                TargetOnion = null!;
                PikminCounter.SetCounterColor(DefultColor);
            }

            foreach (PikminAI ai in PikminOnItem)
            {
                ai.SetAsCarryingItem();
            }
            IsBeingCarried = true;

            CreateRoute();

            OnItemGrabbed.Invoke(this);
        }

        public (PikminType, Onion)? GetPriotizedPikminType()
        {
            if (PikminOnItem.Count == 0)
                return null;

            // Count frequency of each PikminType
            Dictionary<PikminType, int> typeFrequency = new Dictionary<PikminType, int>();
            foreach (PikminAI pikmin in PikminOnItem)
            {
                if (pikmin.pikminType != null)
                {
                    if (!typeFrequency.ContainsKey(pikmin.pikminType))
                        typeFrequency[pikmin.pikminType] = 0;
                    typeFrequency[pikmin.pikminType]++;
                }
            }

            if (typeFrequency.Count == 0)
                return null;

            // Find the most frequent type(s)
            int maxFrequency = typeFrequency.Values.Max();
            var mostFrequentTypes = typeFrequency.Where(kv => kv.Value == maxFrequency)
                                               .Select(kv => kv.Key)
                                               .ToList();

            // Maps PikminType to (Onion, count) tuple for valid onions
            Dictionary<PikminType, (Onion, int)> typeWithOnionInfo = new Dictionary<PikminType, (Onion, int)>();

            // Outcome 1: Check if the most frequent types have valid onions
            foreach (PikminType type in mostFrequentTypes)
            {
                PikminAI pikminOfType = PikminOnItem.FirstOrDefault(p => p.pikminType == type);
                if (pikminOfType != null)
                {
                    Onion? onion = Onion.GetOnionOfPikmin(pikminOfType, true);
                    if (onion != null && onion.onionType.CanCreateSprouts)
                    {
                        int countInOnion = onion.PikminInOnion.Count(p => p.TypeID == type.PikminTypeID);
                        typeWithOnionInfo[type] = (onion, countInOnion);
                    }
                }
            }

            // If we found types with valid onions among the most frequent, return the one with fewest of its type
            if (typeWithOnionInfo.Count > 0)
            {
                var bestMatch = typeWithOnionInfo.OrderBy(kv => kv.Value.Item2).First();
                return (bestMatch.Key, bestMatch.Value.Item1);
            }

            // Outcome 2: If no onions found among most frequent types, check all types
            foreach (var kvp in typeFrequency)
            {
                PikminType type = kvp.Key;
                PikminAI pikminOfType = PikminOnItem.FirstOrDefault(p => p.pikminType == type);
                if (pikminOfType != null)
                {
                    Onion? onion = Onion.GetOnionOfPikmin(pikminOfType, true);
                    if (onion != null && onion.onionType.CanCreateSprouts)
                    {
                        int countInOnion = onion.PikminInOnion.Count(p => p.TypeID == type.PikminTypeID);
                        typeWithOnionInfo[type] = (onion, countInOnion);
                    }
                }
            }

            // If we found any types with valid onions, return the one with fewest of its type
            if (typeWithOnionInfo.Count > 0)
            {
                var bestMatch = typeWithOnionInfo.OrderBy(kv => kv.Value.Item2).First();
                return (bestMatch.Key, bestMatch.Value.Item1);
            }

            // Outcome 3: If no valid onions found among any carried pikmin types, try any available onion
            if (PikminManager.instance != null && PikminManager.instance.Onions.Count > 0)
            {
                LethalMin.Logger.LogInfo("Trying to find any available onion as fallback");

                // Use a list instead of dictionary to handle multiple onions with the same pikmin type
                List<(PikminType type, Onion onion, int count)> fallbackOnionInfo = new List<(PikminType, Onion, int)>();

                foreach (Onion onion in PikminManager.instance.Onions)
                {
                    if (onion != null && onion.onionType != null && onion.onionType.CanCreateSprouts)
                    {
                        for (int i = 0; i < onion.onionType.TypesCanHold.Length; i++)
                        {
                            // Get the type of pikmin associated with this onion
                            PikminType onionPikminType = onion.onionType.TypesCanHold[i];
                            if (onionPikminType != null)
                            {
                                // Count only pikmin of this specific type in the onion
                                int countInOnion = onion.PikminInOnion.Count(p => p.TypeID == onionPikminType.PikminTypeID);
                                fallbackOnionInfo.Add((onionPikminType, onion, countInOnion));
                            }
                        }
                    }
                }

                // If we found any valid onions, return the one with fewest pikmin inside
                if (fallbackOnionInfo.Count > 0)
                {
                    var bestMatch = fallbackOnionInfo.OrderBy(info => info.count).First();
                    LethalMin.Logger.LogInfo($"Found fallback onion with {bestMatch.count} pikmin inside");
                    return (bestMatch.type, bestMatch.onion);
                }
            }

            // Final outcome: If no valid onions found at all, just return the first most frequent type
            // But since we need an onion too, we have to return null
            return null;
        }

        public void OnPrimaryChange()
        {
            if (PrimaryPikminOnItem == null)
            {
                return;
            }

            LethalMin.Logger.LogMessage($"{gameObject.name} Switched Primary Pikmin to {PrimaryPikminOnItem.DebugID}");

            if (IsBeingCarried)
            {
                ItemScript.parentObject = PrimaryPikminOnItem.HoldPosition;
                ItemScript.GrabItemFromEnemy(PrimaryPikminOnItem);
                if (PrimaryPikminOnItem.agent.enabled == false)
                    PrimaryPikminOnItem.SetCollisionMode(1);
            }
        }
        #endregion







        #region Dropping
        public bool IsntHeldByPikmin()
        {
            return ItemScript.isHeld || (ItemScript.isHeldByEnemy && !IsBeingCarried);
        }

        /// <summary>
        /// Should be called on owner side
        /// </summary>
        public virtual void DiscardPikminItem()
        {
            //If the item is not being held by pikmin, then the drop code will not be ran, so don't bother with the rest of the function.
            Vector3 placePosition = Vector3.zero;
            bool droppedInElevator = false;
            bool LPPOInElevator = lastPrimaryPikminOnItem != null && lastPrimaryPikminOnItem.IsInShip;


            if (!IsBeingCarried || lastPrimaryPikminOnItem == null)
            {
                LethalMin.Logger.LogWarning($"{gameObject.name} is not being carried or has no last primary pikmin");
                DropPikminItemOnLocalClient(placePosition);
                DropPikminItemServerRpc(placePosition);
                return;
            }

            NetworkObjectReference PrimLeaderRef = new NetworkObjectReference();
            if (PrimaryLeader != null)
            {
                PrimLeaderRef = PrimaryLeader.NetworkObject;
            }

            //Checks if the item is dropped within a physics region, if so then call drop item with the physics region args
            NetworkObject parentObjectTo = null!;
            Vector3 hitPoint;
            NetworkObject physicsRegionOfDroppedObject = GetPhysicsRegionOfDroppedObject(lastPrimaryPikminOnItem, out hitPoint);
            if (physicsRegionOfDroppedObject != null)
            {
                LethalMin.Logger.LogInfo($"Physics region of dropped object: {physicsRegionOfDroppedObject.name}");
                placePosition = hitPoint;
                parentObjectTo = physicsRegionOfDroppedObject;
                droppedInElevator = StartOfRound.Instance.shipBounds.bounds.Contains(placePosition);
                DropPikminItemOnLocalClient(placePosition, false, droppedInElevator, LPPOInElevator, parentObjectTo.transform, PrimaryLeader);
                DropPikminItemServerRpc(placePosition, false, droppedInElevator, LPPOInElevator, parentObjectTo, PrimLeaderRef);
                return;
            }

            // If the item is dropped outside the physics region, then call drop item with the random position argsv
            Vector3 targetFloorPosition = RoundManager.Instance.RandomlyOffsetPosition(ItemScript.GetItemFloorPosition(), 1.2f, 0.4f);
            // if (ItemScript.itemProperties.allowDroppingAheadOfPlayer && !lastPrimaryPikminOnItem.IsOnShip)
            // {
            //     targetFloorPosition = DropItemAheadOfPikmin(lastPrimaryPikminOnItem);
            // }
            // else
            // {
            targetFloorPosition = ItemScript.GetItemFloorPosition();
            //}
            if (StartOfRound.Instance.shipBounds.bounds.Contains(targetFloorPosition) && !settings.DontParentWhenDropping)
            {
                targetFloorPosition = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(targetFloorPosition);
                droppedInElevator = true;
                LethalMin.Logger.LogInfo($"Dropped in elevator, adjusted target floor position: {targetFloorPosition}");
            }
            else if (!settings.DontParentWhenDropping)
            {
                targetFloorPosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(targetFloorPosition);
                droppedInElevator = false;
                LethalMin.Logger.LogInfo($"Adjusted target floor position: {targetFloorPosition}");
            }

            DropPikminItemOnLocalClient(targetFloorPosition, true, droppedInElevator, LPPOInElevator, null, PrimaryLeader);
            DropPikminItemServerRpc(targetFloorPosition, true, droppedInElevator, LPPOInElevator, new NetworkObjectReference(), PrimLeaderRef);
        }

        [ServerRpc]
        public void DropPikminItemServerRpc(Vector3 targetFloorPosition, bool matchRotationOfParent = true, bool droppedInElevator = false,
        bool pikminInElevator = false,
         NetworkObjectReference parentObject = new NetworkObjectReference(), NetworkObjectReference primaryLeader = new NetworkObjectReference())
        {
            DropPikminItemClientRpc(targetFloorPosition, matchRotationOfParent, droppedInElevator, pikminInElevator, parentObject, primaryLeader);
        }
        [ClientRpc]
        public void DropPikminItemClientRpc(Vector3 targetFloorPosition, bool matchRotationOfParent = true, bool droppedInElevator = false,
        bool pikminInElevator = false,
         NetworkObjectReference parentObject = new NetworkObjectReference(), NetworkObjectReference primaryLeader = new NetworkObjectReference())
        {
            if (IsOwner) { return; }
            NetworkObject networkObject1;
            NetworkObject networkObject2;
            Transform parentTransform = null!;
            Leader primaryLeaderScript = null!;
            if (parentObject.TryGet(out networkObject1))
            {
                parentTransform = networkObject1.transform;
            }
            if (primaryLeader.TryGet(out networkObject2))
            {
                primaryLeaderScript = networkObject2.GetComponent<Leader>();
            }

            DropPikminItemOnLocalClient(targetFloorPosition, matchRotationOfParent, droppedInElevator,
             pikminInElevator, parentTransform, primaryLeaderScript);
        }


        public virtual void DropPikminItemOnLocalClient(Vector3 targetFloorPosition, bool matchRotationOfParent = true,
        bool droppedInElevator = false, bool pikminInElevator = false, Transform? parentObject = null, Leader? PrimaryLeader = null)
        {
            LethalMin.Logger.LogInfo($"{gameObject.name} is dropping an item on local client");
            if (ItemScript == null)
            {
                LethalMin.Logger.LogError($"{gameObject.name} has no ItemScript when dropping");
                return;
            }
            if (IsBeingCarried)
            {
                LethalMin.Logger.LogInfo($"PrimaryLeader: {PrimaryLeader?.Controller.playerUsername}");

                if (parentObject == null || settings.DontParentWhenDropping)
                {
                    LethalMin.Logger.LogInfo($"{gameObject.name} dropped an item on a non-physics region parent");
                    ItemScript.heldByPlayerOnServer = false;
                    ItemScript.parentObject = null;
                    if (droppedInElevator)
                    {
                        if (!settings.DontParentWhenDropping && (!settings.ServerAuthParenting || IsServer))
                            ItemScript.transform.SetParent(StartOfRound.Instance.elevatorTransform, worldPositionStays: true);
                    }
                    else
                    {
                        if (!settings.DontParentWhenDropping && (!settings.ServerAuthParenting || IsServer))
                            ItemScript.transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);
                    }
                    PrimaryLeader?.Controller.SetItemInElevator(pikminInElevator, pikminInElevator, ItemScript);
                    ItemScript.EnablePhysics(enable: true);
                    ItemScript.EnableItemMeshes(enable: true);
                    ItemScript.isHeld = false;
                    ItemScript.isPocketed = false;
                    ItemScript.fallTime = 0f;
                    ItemScript.startFallingPosition = ItemScript.transform.parent == null ?
                    ItemScript.transform.position : ItemScript.transform.parent.InverseTransformPoint(ItemScript.transform.position);
                    ItemScript.transform.localScale = ItemScript.originalScale;
                    ItemScript.transform.localPosition = targetFloorPosition;
                    ItemScript.targetFloorPosition = targetFloorPosition;
                    ItemScript.floorYRot = (int)ItemScript.transform.eulerAngles.y;
                }
                else
                {
                    PlayerPhysicsRegion componentInChildren = parentObject.GetComponentInChildren<PlayerPhysicsRegion>();
                    if (componentInChildren != null && componentInChildren.allowDroppingItems)
                    {
                        parentObject = componentInChildren.physicsTransform;
                    }
                    LethalMin.Logger.LogInfo($"{gameObject.name} dropped an item on a physics region parent");
                    ItemScript.EnablePhysics(enable: true);
                    ItemScript.EnableItemMeshes(enable: true);
                    ItemScript.isHeld = false;
                    ItemScript.isPocketed = false;
                    ItemScript.heldByPlayerOnServer = false;
                    PrimaryLeader?.Controller.SetItemInElevator(pikminInElevator, pikminInElevator, ItemScript);
                    ItemScript.parentObject = null;
                    if (!settings.ServerAuthParenting || IsServer)
                        ItemScript.transform.SetParent(parentObject, worldPositionStays: true);
                    ItemScript.startFallingPosition = ItemScript.transform.localPosition;
                    ItemScript.transform.localScale = ItemScript.originalScale;
                    ItemScript.targetFloorPosition = targetFloorPosition;
                    ItemScript.targetFloorPosition = targetFloorPosition;
                    if (!matchRotationOfParent)
                    {
                        ItemScript.fallTime = 0f;
                    }
                    else
                    {
                        ItemScript.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                        ItemScript.fallTime = 1.1f;
                    }
                    ItemScript.OnPlaceObject();
                }

                ItemScript.isHeldByEnemy = false;
                ItemScript.DiscardItemFromEnemy();
            }

            IsBeingCarried = false;
            lastPrimaryPikminOnItem = null!;

            PikminCounter.SetCounterColor(DefultColor);

            ClearCurrentRoute();

            if (soundRoutine != null)
            {
                StopCoroutine(soundRoutine);
                soundRoutine = null!;
            }

            foreach (PikminAI pikmin in PikminOnItem)
            {
                pikmin.UnsetAsCarryingItem();
            }

            OnItemDropped.Invoke(this);
        }


        private Vector3 DropItemAheadOfPikmin(PikminAI pikmin)
        {
            Vector3 zero = Vector3.zero;
            Ray ray = new Ray(base.transform.position + Vector3.up * 0.4f, pikmin.transform.forward);
            RaycastHit hit;
            zero = ((!Physics.Raycast(ray, out hit, 1.7f, 268438273, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(1.7f) : ray.GetPoint(Mathf.Clamp(hit.distance - 0.3f, 0.01f, 2f)));
            Vector3 itemFloorPosition = ItemScript.GetItemFloorPosition(zero);
            if (itemFloorPosition == zero)
            {
                itemFloorPosition = ItemScript.GetItemFloorPosition();
            }
            return itemFloorPosition;
        }

        [ServerRpc]
        public void RemoveAllPikminFromItemServerRpc()
        {
            RemoveAllPikminFromItemClientRpc();
        }
        [ClientRpc]
        public void RemoveAllPikminFromItemClientRpc()
        {
            RemoveAllPikminFromItemOnLocalClient();
        }
        private void RemoveAllPikminFromItemOnLocalClient()
        {
            // Create a new list to store the pikmin we need to remove
            List<PikminAI> pikminToRemove = new List<PikminAI>(PikminOnItem);

            // Iterate over the new list
            foreach (var pikmin in pikminToRemove)
            {
                if (pikmin.CurrentTask != null)
                {
                    if (pikmin.CurrentTask is not CarryItemTask)
                    {
                        LethalMin.Logger.LogWarning($"Pikmin {pikmin.DebugID} current task is not carry item task???");
                        continue; // Skip if the pikmin is not carrying the item
                    }

                    pikmin.FinishTask();
                }
            }

            // Clear the original list
            PikminOnItem.Clear();
        }

        [ServerRpc]
        public void RemoveFromPikminServerRpc()
        {
            RemoveFromPikminClientRpc();
        }
        [ClientRpc]
        public void RemoveFromPikminClientRpc()
        {
            RemoveFromPikminOnLocalClient();
        }
        private void RemoveFromPikminOnLocalClient()
        {
            foreach (PikminAI ai in PikminOnItem)
            {
                if (!IsBeingCarried)
                {
                    break;
                }
                if (ai != null)
                {
                    ai.PlayAudioOnLocalClient(PikminSoundPackSounds.Lost);
                }
            }
            IsBeingCarried = false;
            ItemScript.isHeldByEnemy = false;
            lastPrimaryPikminOnItem = null!;

            PikminCounter.SetCounterColor(DefultColor);

            ClearCurrentRoute();

            RemoveAllPikminFromItemOnLocalClient();
        }

        public NetworkObject GetPhysicsRegionOfDroppedObject(PikminAI pikminDropping, out Vector3 hitPoint)
        {
            Transform transform = null!;
            RaycastHit hitInfo;
            if (pikminDropping != null && ItemScript.itemProperties.allowDroppingAheadOfPlayer)
            {
                Debug.DrawRay(pikminDropping.transform.position + Vector3.up * 0.4f, pikminDropping.transform.forward * 1.7f, Color.yellow, 1f);
                Ray ray = new Ray(pikminDropping.transform.position + Vector3.up * 0.4f, pikminDropping.transform.forward);
                Vector3 vector = ((!Physics.Raycast(ray, out hitInfo, 1.7f, 1342179585, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(1.7f) : ray.GetPoint(Mathf.Clamp(hitInfo.distance - 0.3f, 0.01f, 2f)));
                if (Physics.Raycast(vector, -Vector3.up, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
                {
                    Debug.DrawRay(vector, -Vector3.up * 80f, Color.yellow, 2f);
                    transform = hitInfo.collider.gameObject.transform;
                }
            }
            else
            {
                Ray ray = new Ray(ItemScript.transform.position, -Vector3.up);
                if (Physics.Raycast(ray, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
                {
                    Debug.DrawRay(ItemScript.transform.position, -Vector3.up * 80f, Color.blue, 2f);
                    transform = hitInfo.collider.gameObject.transform;
                }
            }
            if (transform != null)
            {
                PlayerPhysicsRegion componentInChildren = transform.GetComponentInChildren<PlayerPhysicsRegion>();
                if (componentInChildren != null && componentInChildren.allowDroppingItems && componentInChildren.itemDropCollider.ClosestPoint(hitInfo.point) == hitInfo.point)
                {
                    NetworkObject parentNetworkObject = componentInChildren.parentNetworkObject;
                    if (parentNetworkObject != null)
                    {
                        Vector3 addPositionOffsetToItems = componentInChildren.addPositionOffsetToItems;
                        hitPoint = componentInChildren.physicsTransform.InverseTransformPoint(hitInfo.point + Vector3.up * 0.04f + ItemScript.itemProperties.verticalOffset * Vector3.up + addPositionOffsetToItems);
                        return parentNetworkObject;
                    }
                    Debug.LogError("Error: physics region transform does not have network object?: " + transform.gameObject.name);
                }
            }
            hitPoint = Vector3.zero;
            return null!;
        }
        #endregion







        #region Despawning
        public int DespawnCount = 0;

        [ServerRpc(RequireOwnership = false)]
        public void IncrumentDestoryCountServerRpc()
        {
            DespawnCount++;
            if (DespawnCount >= StartOfRound.Instance.connectedPlayersAmount + 1)
            {
                LethalMin.Logger.LogInfo($"All Clients marked to despawn {gameObject.name}");
                if (ItemScript.playerHeldBy != null)
                {
                    LethalMin.Logger.LogWarning($"Item {gameObject.name} is held by a player! dropping");
                    ItemScript.playerHeldBy.DropAllHeldItems();
                }
                if (HasOwnNetworkObject)
                {
                    ItemScript.NetworkObject.Despawn(true);
                }
                else
                {
                    NetworkObject.Despawn(true);
                }
            }
        }

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            base.OnNetworkObjectParentChanged(parentNetworkObject);
            //The pikmin item node cannot "detect" if it is not it's own thing
            if (!HasOwnNetworkObject)
            {
                return;
            }
            //LethalMin.Logger.LogInfo($"ParentChange Detected: {transform.parent == null}, {IsServer}, {HasInitalized}");
            if (transform.parent == null && IsServer && HasInitalized)
            {
                LethalMin.Logger.LogWarning($"PikminItem {gameObject.name} is detached from a GrabbableObject");
                // Instead of immediately despawning, schedule it for the next frame
                StartCoroutine(DespawnNextFrame());
            }
        }

        private IEnumerator DespawnNextFrame()
        {
            yield return new WaitForSeconds(0.1f);
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }

        #endregion



        public virtual void Update()
        {
            if (ItemScript == null)
            {
                if (IsServer && HadItemScript)
                {
                    LethalMin.Logger.LogWarning($"PikminItem {gameObject.name} has no ItemScript, despawning");
                    NetworkObject.Despawn(true);
                }
                return;
            }
            else
            {
                HadItemScript = true;
            }

            if (PikminOnItem.Count > 0)
            {
                PrimaryPikminOnItem = PikminOnItem[0];
                lastPrimaryPikminOnItem = PikminOnItem[0];
                if (PrimaryPikminOnItem != previousPrimaryPikmin && PrimaryPikminOnItem != null)
                {
                    previousPrimaryPikmin = PrimaryPikminOnItem;
                    OnPrimaryChange();
                }
            }
            else
            {
                PrimaryPikminOnItem = null;
                previousPrimaryPikmin = null;
            }

            if (ShouldGrab)
            {
                int count = PikminOnItem.Count;
                PikminOnItem.RemoveAll(pikmin => pikmin == null);
                if (count != PikminOnItem.Count)
                {
                    LethalMin.Logger.LogWarning($"{gameObject.name} had null pikmin in PikminOnItem list, removed {count - PikminOnItem.Count} null pikmin");
                }
            }

            if (ShouldGrab)
            {
                GrabPikminItemOnLocalClient();
                GrabPikminItemServerRpc();
            }

            if (hoarderBugItem != null && IsBeingCarried)
            {
                hoarderBugItem.status = HoarderBugItemStatus.Stolen;
            }


            if (ItemScript.isHeld && PikminOnItem.Count > 0 && IsOwner)
            {
                LethalMin.Logger.LogInfo($"{gameObject.name}: Stopping Carry because player held item");
                RemoveFromPikminServerRpc();
            }

            if (ItemScript.isHeldByEnemy && !IsBeingCarried && PikminOnItem.Count > 0 && IsOwner)
            {
                LethalMin.Logger.LogInfo($"{gameObject.name}: Stopping Carry because enemy held item");
                RemoveAllPikminFromItemServerRpc();
            }

            if (IsBeingCarried && PrimaryPikminOnItem == null && PikminOnItem.Count > 0 && IsOwner)
            {
                LethalMin.Logger.LogInfo($"{gameObject.name}: Stopping Carry because Primary Pikmin Is Null");
                RemoveAllPikminFromItemServerRpc();
            }

            // Get each carry strength of every pikmin in the PikminOnItem list
            if (PikminOnItem.Count > 0)
            {
                var carryStrengths = PikminOnItem.Select(pikmin => pikmin.CurrentCarryStrength).ToArray();
                TotalCarryStrength = carryStrengths.Sum();
            }
            else
            {
                TotalCarryStrength = 0;
            }

            if (TotalCarryStrength < CarryStrengthNeeded && IsBeingCarried && IsOwner)
            {
                LethalMin.Logger.LogInfo($"{gameObject.name} is not carrying enough pikmin and is dropping it");
                //DropPikminItemOnLocalClient(targetFloorPosition);
                DiscardPikminItem();

                return;
            }

            if (CurrentRoute != null && IsOwner)
            {
                CurrentRoute.Update();
                if (RouteRecallInterval > 0)
                {
                    RouteRecallInterval -= Time.deltaTime;
                }
                else if (CurrentRoute != null)
                {
                    RouteRecallInterval = 0.25f + UnityEngine.Random.Range(0.1f, 0.25f);
                    if (PikminOnItem.Count > 0)
                    {
                        PrimaryLeader = PikUtils.GetLeaderFromMultiplePikmin(PikminOnItem);
                    }
                }
            }

            if (GrabPositionContainer != null)
            {
                GrabPositionContainer.transform.position = ItemScript.transform.position;
            }

            if (HasArrived && IsntHeldByPikmin()
            || HasArrived && Vector3.Distance(ArrivePosition, ItemScript.transform.position) > 5f
            || settings.CanProduceSprouts && Time.time - TimeSinceArrived > (arrivedOnSpawn ? 7f : 2f))
            {
                HasArrived = false;
                ArrivePosition = Vector3.zero;
            }

            if (PikminCounter == null)
            {
                if (AlreadyPartalInitalized)
                    LethalMin.Logger.LogWarning($"{gameObject.name} has no Pikmin Counter, creating one");
                CreateCounter();
                return;
            }

            if (GrabPositionContainer == null)
            {
                if (AlreadyPartalInitalized)
                    LethalMin.Logger.LogWarning($"{gameObject.name} has no GrabPositionContainer, creating one");
                CreateGrabPositions();
                return;
            }

            PikminCounter.gameObject.SetActive(PikminOnItem.Count > 0);
        }


        IEnumerator CarryNumerator()
        {
            const float OneAndTwoInterval = 0.75f;
            const float ThreeAndFourInterval = 0.5f;
            float interval = 0.1f;
            PikminGeneration gen = PikUtils.ConvertCfgGenerationToPikminGeneration(LethalMin.PikminSoundGeneration.InternalValue);
            switch (gen)
            {
                case PikminGeneration.Pikmin1And2:
                    interval = OneAndTwoInterval;
                    break;
                case PikminGeneration.Pikmin4:
                case PikminGeneration.Pikmin3:
                case PikminGeneration.HeyPikmin:
                    interval = ThreeAndFourInterval;
                    break;
            }
            while (PikminOnItem.Count > 0)
            {
                yield return new WaitForSeconds(interval);
                float vol = PikminOnItem.Count == 0 ? 1.0f : 1.0f / PikminOnItem.Count;
                foreach (PikminAI pikmin in PikminOnItem)
                {
                    //LethalMin.Logger.LogInfo($"{gameObject.name} is playing carry sound vol = {vol}");
                    pikmin.PlayAudioOnLocalClient("ItemCarry", true, vol);
                }
            }
        }

        public float GetSpeed(bool OverrideCarryCheck = false)
        {
            if (PikminOnItem.Count == 0 || (!IsBeingCarried && !OverrideCarryCheck))
                return 0f;

            float weight = Mathf.Max(1f, CarryStrengthNeeded);
            float maxCarriers = Mathf.Max(1f, CarryStrengthNeeded * 2); // m in formula
            float pikminTypeAndMaturitySum = 0f;

            // Calculate the sum of Tp + Sp for all Pikmin
            foreach (PikminAI pikmin in PikminOnItem)
            {
                // Get the base type speed modifier from the Pikmin's actual speed
                float typeModifier = pikmin.pikminType.GetSpeed(pikmin.CurrentGrowthStage, pikmin.ShouldRun);

                // Get maturity modifier based on growth stage
                float maturityModifier = 0f;
                switch (pikmin.CurrentGrowthStage)
                {
                    case 0: // Leaf
                        maturityModifier = 0.2f; // Proportionally smaller than bud/flower
                        break;
                    case 1: // Bud
                        maturityModifier = 0.5f; // Middle value
                        break;
                    case 2: // Flower
                        maturityModifier = 1.0f; // Full value
                        break;
                }

                // Add to sum
                pikminTypeAndMaturitySum += typeModifier + maturityModifier;
            }

            // Calculate the basic carry speed based on the formula structure
            // v = baseSpeed + (sum of modifiers - weight + 1) / maxCarriers + speedBoost
            float baseSpeed = 1.0f;
            float speedBoost = 0.0f; // Smaller boost value than Pikmin 2's 26.4

            // Formula adaptation
            float speed = baseSpeed + ((pikminTypeAndMaturitySum - weight + PikminOnItem.Count) / maxCarriers) + speedBoost;

            // Ensure minimum speed
            speed = Mathf.Max(speed, 1.0f);

            return speed;
        }

        #region Routing
        public void CreateRoute()
        {
            ClearCurrentRoute();
            if (PrimaryPikminOnItem != null)
            {
                PikminRouteRequest request = new PikminRouteRequest
                {
                    Pikmin = PrimaryPikminOnItem,
                    Intent = DetermineRouteIntent(),
                    TargetOnion = TargetOnion,
                    HandleEntrances = false
                };
                if (request.Intent == RouteIntent.ToPlayer)
                {
                    PrimaryLeader = PikUtils.GetLeaderFromMultiplePikmin(PikminOnItem);
                    request.TargetPlayer = PrimaryLeader;
                    request.CustomCheckDistance = settings.RouteToPlayerDroppingDistance;
                }
                CurrentRoute = PikminRouteManager.Instance.CreateRoute(request);
                CurrentRoute.OnNodeReached.AddListener(OnNodeReached);
                CurrentRoute.OnRouteComplete.AddListener(OnRouteEndOwnerSide);
                CurrentRoute.OnRouteInvalidated.AddListener(OnRouteInvalidated);
                if (!isRegeneratingRoute)
                    LethalMin.Logger.LogInfo($"{gameObject.name} has created a route");
                isRegeneratingRoute = false;
            }
            else
            {
                LethalMin.Logger.LogWarning($"{gameObject.name} has no primary pikmin to create a route");
            }
        }

        public virtual RouteIntent DetermineRouteIntent()
        {
            if (PrimaryPikminOnItem == null)
            {
                LethalMin.Logger.LogError($"{gameObject.name} has no primary pikmin to determine route intent");
                return RouteIntent.ToShip;
            }

            if (settings.RouteToPlayer)
            {
                return RouteIntent.ToPlayer;
            }

            // Try onion route if applicable
            if (LethalMin.TakeItemsToTheOnion && TargetOnion != null && (!LethalMin.OnCompany || LethalMin.TakeItemsToOnionOnCompany))
            {
                if (TestRoute(RouteIntent.ToOnion))
                {
                    return RouteIntent.ToOnion;
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Failed to find path to onion, setting target onion to null.");
                    TargetOnion = null!;
                    PikminCounter.SetCounterColor(DefultColor);
                }
            }
            else if(TargetOnion != null)
            {
                LethalMin.Logger.LogDebug($"Not taking item to onion due to company settings, setting target onion to null.");
                TargetOnion = null!;
                PikminCounter.SetCounterColor(DefultColor);
            }

            // Try company counter route if applicable
            if (LethalMin.OnCompany)
            {
                RouteIntent companyIntent = (!ItemScript.itemProperties.isScrap && !LethalMin.CarryNonScrapItemsToCompany)
                    ? RouteIntent.ToShip
                    : RouteIntent.ToCounter;

                if (TestRoute(companyIntent))
                {
                    return companyIntent;
                }
            }

            // Calculate distances for vehicle routing
            Vector3 distanceComparisonPoint = PrimaryPikminOnItem.isOutside ? ItemScript.transform.position :
            PikminRouteManager.Instance.EntranceExitPoints[PikminRouteManager.GetClosestEntrance(ItemScript.transform.position, false)].position;
            float closestCarDistance = Mathf.Infinity;
            foreach (PikminVehicleController vehicle in PikminManager.instance.Vehicles)
            {
                if (!vehicle.controller.backDoorOpen || vehicle.controller.carDestroyed)
                {
                    continue;
                }
                if (vehicle.IsNearByShip())
                {
                    continue;
                }
                float dist = Vector3.Distance(vehicle.transform.position, distanceComparisonPoint);
                if (dist < closestCarDistance)
                {
                    closestCarDistance = dist;
                }
            }

            float shipDistance = Vector3.Distance(StartOfRound.Instance.shipBounds.transform.position, distanceComparisonPoint);
            bool shouldPreferVehicle = PikminManager.instance.Vehicles.Count > 0
                                    && shipDistance > closestCarDistance
                                    && LethalMin.TakeItemsToTheCar;
            //LethalMin.Logger.LogInfo($"{gameObject.name} ship distance: {shipDistance}, closest car distance: {closestCarDistance}, shouldPreferVehicle: {shouldPreferVehicle}");

            // Handle indoor routing
            if (!PrimaryPikminOnItem.isOutside)
            {
                return DetermineIndoorRouteIntent(shouldPreferVehicle);
            }

            // Handle outdoor vehicle routing
            if (shouldPreferVehicle && TestRoute(RouteIntent.ToVehicle))
            {
                return RouteIntent.ToVehicle;
            }

            // Default
            return RouteIntent.ToShip;
        }

        private RouteIntent DetermineIndoorRouteIntent(bool shouldPreferVehicle)
        {
            RouteIntent primaryIntent = LethalMin.UseExitsWhenCarryingItems ? RouteIntent.ToShip : RouteIntent.ToExit;

            // Try vehicle first if preferred
            if (shouldPreferVehicle && primaryIntent == RouteIntent.ToShip)
            {
                if (TestRoute(RouteIntent.ToVehicle))
                {
                    return RouteIntent.ToVehicle;
                }
            }

            // Try primary intent (ship or exit)
            if (TestRoute(primaryIntent))
            {
                return primaryIntent;
            }

            // Try elevator as fallback
            if (PikminRouteManager.Instance.CurrentFloorData != null && TestRoute(RouteIntent.ToElevator))
            {
                return RouteIntent.ToElevator;
            }

            return RouteIntent.ToShip;
        }

        public bool TestRoute(RouteIntent intent)
        {
            PikminRouteRequest testRequest = new PikminRouteRequest
            {
                Pikmin = PrimaryPikminOnItem!,
                Intent = intent,
                TargetOnion = TargetOnion,
                HandleEntrances = false
            };
            PikminRoute testRoute;
            testRoute = PikminRouteManager.Instance.CreateRoute(testRequest);
            if (testRoute == null || !testRoute.IsFullPath)
            {
                LethalMin.Logger.LogWarning($"{gameObject.name} could not create a route to the {intent}");
                return false;
            }
            else
            {
                LethalMin.Logger.LogInfo($"{gameObject.name} successfully created a test route to the {intent}");
                testRoute.DestoryRoute();
                return true;
            }
        }

        public void OnNodeReached(RouteNode node)
        {
            LethalMin.Logger.LogInfo($"{gameObject.name} has reached a route node: {node.name}");
            if (node.entrancePoint != null && LethalMin.UseExitsWhenCarryingItems && node.entrancePoint.TryGetComponent(out EntranceTeleport entrance))
            {
                UseEntranceOwnerSide(entrance);
            }
        }

        public void ClearCurrentRoute()
        {
            if (CurrentRoute != null)
            {
                CurrentRoute.DestoryRoute();
                CurrentRoute = null!;
                LethalMin.Logger.LogInfo($"{gameObject.name} has cleared its route");
            }
        }

        public void OnRouteInvalidated(RouteValidation.InvalidationReason reason)
        {
            LethalMin.Logger.LogWarning($"{gameObject.name} route has been invalidated ({reason}), clearing route");
            isRegeneratingRoute = true;
            CreateRoute();
        }

        public void OnRouteEndOwnerSide()
        {
            if (!IsOwner)
                return;

            OnRouteEnd();
            if (NetworkObject.IsSpawned)
            {
                OnRouteEndServerRpc();
            }
        }

        [ServerRpc]
        public void OnRouteEndServerRpc()
        {
            OnRouteEndClientRpc();
        }
        [ClientRpc]
        public void OnRouteEndClientRpc()
        {
            if (!IsOwner)
                OnRouteEnd();
        }
        public void OnRouteEnd()
        {
            LethalMin.Logger.LogInfo($"{gameObject.name} has reached its route end");
            HasArrived = true;
            TimeSinceArrived = Time.time;
            ArrivePosition = ItemScript.transform.position;
            StartCoroutine(DoYays(PikminOnItem));
            ClearCurrentRoute();
            if (TargetOnion != null)
            {
                TargetOnion.SuckItemIntoOnion(this, TargetPikminTypeOnion);
            }
            RemoveAllPikminFromItemOnLocalClient();
        }

        IEnumerator DoYays(List<PikminAI> ais)
        {
            List<PikminAI> aisCopy = new List<PikminAI>(ais);
            foreach (PikminAI pikmin in aisCopy)
            {
                if (pikmin != null)
                {
                    pikmin.DoYay(true);
                    yield return new WaitForSeconds(0.02f);
                }
                else
                {
                    LethalMin.Logger.LogError($"{gameObject.name} has a null pikmin in its list when doing yays");
                }
            }
        }

        void UseEntranceOwnerSide(EntranceTeleport entrance)
        {
            if (!IsOwner)
                return;

            UseEntranceServerRpc(entrance.NetworkObject);
        }

        [ServerRpc]
        public void UseEntranceServerRpc(NetworkObjectReference Ref)
        {
            UseEntranceClientRpc(Ref);
        }

        [ClientRpc]
        public void UseEntranceClientRpc(NetworkObjectReference Ref)
        {
            if (Ref.TryGet(out NetworkObject obj) && obj.TryGetComponent(out EntranceTeleport entrance))
            {
                UseEntranceOnLocalClient(entrance, entrance.isEntranceToBuilding);
            }
            else
            {
                LethalMin.Logger.LogError($"{gameObject.name}: Could not find entrance");
            }
        }

        public void UseEntranceOnLocalClient(EntranceTeleport entrance, bool Inside)
        {
            if (!IsBeingCarried)
            {
                LethalMin.Logger.LogError($"{gameObject.name} is not carrying any pikmin, cannot use entrance");
                return;
            }
            List<PikminAI> tempList = new List<PikminAI>(PikminOnItem);
            foreach (PikminAI pikmin in tempList)
            {
                pikmin.UseEntranceOnLocalClient(entrance, Inside, false);
            }
            ItemScript.isInFactory = Inside;
            entrance.PlayAudioAtTeleportPositions();
        }
        #endregion
    }
}