using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;
using System.Collections;
using System;
using System.Linq;

namespace LethalMin
{
    public class PikminItem : NetworkBehaviour, IDebuggable
    {
        public GameObject CounterPrefab;
        public GameObject Counter;
        public TMP_Text PikminOn, PikminNeed, Devider;
        public GrabbableObject Root;
        public List<PikminAI> PikminOnItemList = new List<PikminAI>();
        [IDebuggable.Debug] public int PikminOnItem, PikminNeedOnItem;
        public List<int> GrowthStagesOnItem = new List<int>();
        public float SFXInterval = 0.2f;
        private bool isInitialized = false;
        private bool wasGrabbed = false;
        public CaveDwellerAI ManEater;
        private NetworkObject rootNetworkObject;
        [IDebuggable.Debug] Vector3 ObjectPosition;
        public Color basecolor;
        public Color CurColor;
        public bool CanBeConvertedIntoSprouts;
        public bool DontParentToObjects;
        public PikminType FavoredType = null!;

        [ClientRpc]
        public void SetCurColorClientRpc(Color color)
        {
            CurColor = color;
        }

        #region Unity Lifecycle Methods
        public void Start()
        {
            basecolor = new Color(1, 0.282352941f, 0);
            CurColor = basecolor;
            PikminOnItemList = new List<PikminAI>();
            CounterPrefab = LethalMin.CounterPrefab;
            RespawnCounter();
        }

        private void CheckAndDespawnIfParentDestroyed()
        {
            if (!IsServer)
            {
                return;
            }
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                if (transform.parent == null || transform.parent.gameObject == null)
                {
                    // Parent has been destroyed, despawn this NetworkObject
                    if (IsServer)
                        NetworkObject.Despawn(true);
                    LethalMin.Logger.LogInfo($"Pikminitem {name} despawned due to destroyed parent");
                }
            }
        }
        public override void OnNetworkDespawn()
        {
            if (Counter != null)
                Destroy(Counter);
        }
        public void OnDestory()
        {
            if (Counter != null)
                Destroy(Counter);
        }
        private void RespawnCounter()
        {
            LethalMin.Logger.LogInfo($"{name} is respawning counter");
            if (CounterPrefab != null)
            {
                Counter = Instantiate(CounterPrefab, transform.position, Quaternion.identity);
                Counter.transform.SetParent(null);
                Counter.gameObject.name = $"{gameObject.name}'s Counter";
                Counter.AddComponent<LookAtMainCamera>();

                // Re-assign references
                PikminNeed = Counter.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
                Devider = Counter.transform.Find("Text (TMP) (1)").GetComponent<TMP_Text>();
                PikminOn = Counter.transform.Find("Text (TMP) (2)").GetComponent<TMP_Text>();
            }
            else
            {
                LethalMin.Logger.LogError("CounterPrefab is not assigned!");
            }
        }
        void Update()
        {
            if (IsServer)
            {
                if (Root == null && isInitialized)
                {
                    Destroy(Counter);
                    NetworkObject.Despawn(true);
                    return;
                }
                CheckIfGrabbed();
            }
            if (Counter == null)
            {
                RespawnCounter();
                return;
            }
            if (PikminNeed == null)
            {
                PikminNeed = Counter.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
            }
            if (PikminOn == null)
            {
                PikminOn = Counter.transform.Find("Text (TMP) (2)").GetComponent<TMP_Text>();
            }
            PikminNeed.text = PikminNeedOnItem.ToString();
            PikminOn.text = PikminOnItem.ToString();
            if (PikminOnItem < PikminNeedOnItem)
            {
                PikminOn.color = Color.Lerp(CurColor, Color.black, 0.5f);
            }
            else
            {
                PikminOn.color = CurColor;
            }
            Devider.color = CurColor;
            PikminNeed.color = CurColor;
            if (PikminOnItem >= PikminNeedOnItem)
            {
                if (!isParented)
                {
                    ParentToFirstPikmin();
                }
            }
            else if (isParented && IsServer)
            {
                if (Root != null && RoundManager.Instance != null)
                {
                    Vector3 targetFloorPosition = RoundManager.Instance.RandomlyOffsetPosition(Root.GetItemFloorPosition(), 1.2f, 0.4f);
                    UnparentItemServerRpc(targetFloorPosition);
                }
                else
                {
                    // Handle the case where Root or RoundManager.Instance is null
                    LethalMin.Logger.LogWarning("Root or RoundManager.Instance is null in PikminItem.Update()");
                    // You might want to set a default position or skip the unparenting in this case
                }
            }
            if (!isInitialized || Root == null)
            {
                Counter.SetActive(false);
                return; // Skip update if not properly initialized
            }
            UpdateGoToPositions();
            Counter.SetActive(PikminOnItem > 0);
            Counter.transform.position = new Vector3(ObjectPosition.x, ObjectPosition.y + LethalMin.ItemCounterYPositionOffsetValue, ObjectPosition.z);
        }
        void LateUpdate()
        {
            ObjectPosition = transform.position;
            CheckAndDespawnIfParentDestroyed();

            if (PikminOnItemList.Count > 0)
            {
                PikminOnItem = CalculatePikminOnItems();
            }
            else if (PikminOnItemList.Count == 1)
            {
                PikminOnItem = PikminOnItemList[0].PminType.CarryStrength;
            }
            else
            {
                PikminOnItem = 0;
            }

            if (PikminOnItem < PikminNeedOnItem) { return; }
            if (SFXInterval >= 0)
            {
                SFXInterval -= Time.deltaTime;
            }
            else
            {
                foreach (PikminAI pikmin in PikminOnItemList)
                {
                    if (pikmin.PminType.soundPack == null)
                    {
                        int index = UnityEngine.Random.Range(0, LethalMin.CarrySFX.Length);
                        pikmin.PlaySFX(ref LethalMin.CarrySFX, index, true, true, 0.4f);
                    }
                    else
                    {
                        int index = UnityEngine.Random.Range(0, pikmin.PminType.soundPack.CarryVoiceLine.Length);
                        pikmin.PlaySFX(ref pikmin.PminType.soundPack.CarryVoiceLine, index, true, true, 0.4f);
                    }
                }
                SFXInterval = 0.5f;
            }
        }

        private int CalculatePikminOnItems()
        {
            int count = 0;
            foreach (PikminAI pikmin in PikminOnItemList)
            {
                count += pikmin.PminType.CarryStrength;
            }
            return count;
        }

        [ClientRpc]
        public void DestoryCounterOnClientRpc()
        {
            if (Counter != null)
                Destroy(Counter);
        }
        #endregion





        #region Initialization and Setup

        [ServerRpc(RequireOwnership = false)]
        public void SetRootServerRpc(NetworkObjectReference rootRef)
        {
            if (rootRef.TryGet(out NetworkObject root))
            {
                rootNetworkObject = root;
                SetRootClientRpc(rootRef);
            }
        }

        [ClientRpc]
        private void SetRootClientRpc(NetworkObjectReference rootRef)
        {
            LethalMin.Logger.LogInfo($"syncing {rootRef}");
            if (rootRef.TryGet(out NetworkObject root))
            {
                LethalMin.Logger.LogInfo($"syncing {root.name} rootNetworkObject");
                rootNetworkObject = root;
                if (root.GetComponent<GrabbableObject>() != null)
                {
                    LethalMin.Logger.LogInfo($"syncing {root.name} Root");
                    Initialize(root.GetComponent<GrabbableObject>());
                }
            }
        }

        private List<Vector3> goToPositions = new List<Vector3>();
        private List<bool> goToPositionsOccupied = new List<bool>();
        public int MaxPikminOnItem;
        public void SyncRoot()
        {
            if (IsServer && rootNetworkObject != null)
            {
                SetRootClientRpc(new NetworkObjectReference(rootNetworkObject));
            }
        }
        public void Initialize(GrabbableObject root)
        {
            Root = root;
            name = root.name + "(PikminNode)";
            LethalMin.Logger.LogInfo($"{name} has {root.itemProperties.weight}");

            PikminNeedOnItem = Mathf.Max(
    (root.itemProperties.weight - 1f) * 100f <= 3f ? 1 :
    Mathf.CeilToInt(((root.itemProperties.weight - 1f) * 100f - 3f) / 10f) + 1 - LethalMin.ItemRequireSubracterValue, 1);
            isInitialized = true;

            CreateGoToPositions();
        }

        private void CreateGoToPositions()
        {
            Collider rootCollider = Root.GetComponent<Collider>();
            float radius = 0.5f; // Default radius if no collider is found

            if (rootCollider != null)
            {
                LethalMin.Logger.LogMessage($"Got radius {radius} for {Root.name}");
                radius = Mathf.Max(rootCollider.bounds.extents.x, rootCollider.bounds.extents.z) + 0.5f;
            }
            else
            {
                LethalMin.Logger.LogWarning($"No collider found on {Root.name}. Using default radius.");
            }

            int positionCount = Mathf.Min(50, PikminNeedOnItem * 5);

            goToPositions.Clear();
            goToPositionsOccupied.Clear();

            for (int i = 0; i < positionCount; i++)
            {
                float angle = i * (360f / positionCount);
                Vector3 position = ObjectPosition + Quaternion.Euler(0, angle, 0) * (Vector3.forward * radius);
                position.y = ObjectPosition.y;

                goToPositions.Add(position);
                goToPositionsOccupied.Add(false);
            }
            MaxPikminOnItem = positionCount;
            LethalMin.Logger.LogMessage($"created {positionCount} GoTo Positions");
            //CreateDebugCubes();
        }
        public void UpdateGoToPositions()
        {
            if (Root == null || goToPositions.Count == 0) return;

            Vector3 itemPosition = ObjectPosition;
            int positionCount = goToPositions.Count;
            float radius = CalculateRadius();

            for (int i = 0; i < positionCount; i++)
            {
                //Vector3 oldPosition = goToPositions[i];
                // Recalculate the position based on the item's new position
                float angle = i * (360f / positionCount);
                Vector3 newPosition = itemPosition + Quaternion.Euler(0, angle, 0) * (Vector3.forward * radius);
                newPosition.y = itemPosition.y; // Ensure the y-position matches the item
                goToPositions[i] = newPosition;

                // Check if the position has changed significantly
                // if (Vector3.Distance(oldPosition, newPosition) > 0.001f)
                // {
                //     LethalMin.Logger.LogInfo($"GoToPosition {i} changed: Old: {oldPosition}, New: {newPosition}");
                //     goToPositions[i] = newPosition;
                // }
            }
            //UpdateDebugCubes();
        }

        private float CalculateRadius()
        {
            Collider rootCollider = Root.GetComponent<Collider>();
            if (rootCollider != null)
            {
                return Mathf.Max(rootCollider.bounds.extents.x, rootCollider.bounds.extents.z) + 1f;
            }
            return 0.5f; // Default radius if no collider is found
        }
        private List<GameObject> debugCubes = new List<GameObject>();
        private void CreateDebugCubes()
        {
            // Remove existing debug cubes
            foreach (var cube in debugCubes)
            {
                if (cube != null)
                {
                    Destroy(cube);
                }
            }
            debugCubes.Clear();

            // Create new debug cubes for each GoToPosition
            for (int i = 0; i < goToPositions.Count; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"{name} GoTo {i}";
                cube.transform.position = goToPositions[i];
                cube.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f); // Small cube size

                // Remove collider to avoid interference
                Destroy(cube.GetComponent<Collider>());

                // Set color based on occupation status
                Renderer renderer = cube.GetComponent<Renderer>();
                renderer.material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
                debugCubes.Add(cube);
            }
        }
        private void UpdateDebugCubes()
        {
            for (int i = 0; i < goToPositions.Count; i++)
            {
                GameObject cube = debugCubes[i];
                cube.transform.position = goToPositions[i];
                cube.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                Renderer renderer = cube.GetComponent<Renderer>();
                renderer.material.color = goToPositionsOccupied[i] ? Color.red : Color.green;
            }
        }
        public (Vector3 position, int index) GetNearestAvailableGoToPosition(Vector3 pikminPosition)
        {
            if (goToPositions.Count == 0)
            {
                return (ObjectPosition, -1);
            }

            float nearestDistance = float.MaxValue;
            int nearestIndex = -1;

            for (int i = 0; i < goToPositions.Count; i++)
            {
                if (!goToPositionsOccupied[i])
                {
                    float distance = Vector3.Distance(pikminPosition, goToPositions[i]);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestIndex = i;
                    }
                }
            }

            if (nearestIndex != -1)
            {
                goToPositionsOccupied[nearestIndex] = true;
                return (goToPositions[nearestIndex], nearestIndex);
            }

            // If all positions are occupied or no positions available, return the item's position
            LethalMin.Logger.LogInfo("LOL!!");
            return (ObjectPosition, -1);
        }
        public Vector3 GetGoToPos((Vector3? position, int index) Pair, string DebugName = "???")
        {
            //LethalMin.Logger.LogInfo($"{DebugName}: Has the goto pos of ({Pair.position.Value}) ({Pair.index})");
            if (Pair.index == -1 && Pair.position.HasValue)
            {
                if (LethalMin.DebugMode)
                    //LethalMin.Logger.LogWarning($"{DebugName}: Invalid GoTo position index: {Pair.index} With a valid position: {Pair.position.Value}");
                    return ObjectPosition;
            }
            else if (Pair.index == -1 && !Pair.position.HasValue)
            {
                if (LethalMin.DebugMode)
                    //LethalMin.Logger.LogWarning($"{DebugName}: Invalid GoTo position index: {Pair.index} with no valid position");
                    return ObjectPosition;
            }
            else if (Pair.index != -1 && !Pair.position.HasValue || Pair.index != -1 && Pair.position.HasValue)
            {
                if (DebugName != "???")
                {
                    //LethalMin.Logger.LogInfo($"{DebugName}: Has a non negetive Pair.index ({Pair.position.Value}) ({Pair.index})");
                }
                return goToPositions[Pair.index];
            }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogWarning($"{DebugName}: Failed to get goto position");
            return ObjectPosition;
        }
        public void ReleaseGoToPosition(Vector3 position, int Index)
        {
            if (Index == -1)
            {
                Index = goToPositions.FindIndex(pos => Vector3.Distance(pos, position) < 0.01f);
            }
            if (Index != -1)
            {
                goToPositionsOccupied[Index] = false;
            }
            else
            {
                LethalMin.Logger.LogWarning($"Attempted to release a GoTo position that doesn't exist: {position}");
            }
        }

        [ClientRpc]
        public void SetIsBodyClientRpc()
        {
            CanBeConvertedIntoSprouts = true;
        }
        #endregion





        #region Item Parenting and Unparenting 
        public bool UsePikminAsParent;
        private void ParentToFirstPikmin()
        {
            if (PikminOnItemList.Count > 0 && Root != null)
            {
                PikminAI firstPikmin = PikminOnItemList[0];
                firstPikminG = firstPikmin;
                if (ManEater != null)
                {
                    ManEater.GetComponentInChildren<EaterBehavior>().CarriedByPikmin(PikminOnItemList.ToArray());
                }
                Root.GrabItemFromEnemy(firstPikmin);
                Root.hasHitGround = false;
                if (!UsePikminAsParent)
                {
                    Root.parentObject = firstPikmin.HoldPos;

                    if (Root.NetworkObject != null && IsServer)
                        Root.transform.SetParent(firstPikmin.HoldPos, worldPositionStays: true);
                }
                else
                {
                    Root.parentObject = firstPikmin.HoldPos;

                    if (Root.NetworkObject != null && IsServer)
                        Root.transform.SetParent(firstPikmin.transform, worldPositionStays: true);
                }
                Root.EnablePhysics(enable: false);
                isParented = true;
                // Call ServerRpc to sync across network
                ParentToFirstPikminServerRpc(firstPikmin.NetworkObjectId);
            }
        }
        DepositItemsDesk HomeDepo;
        private void UnparentItem(Vector3 targetFloorPosition)
        {
            if (Root != null)
            {
                LethalMin.Logger.LogInfo("DroppinItemTo: " + targetFloorPosition);
                Vector3 placePosition = default(Vector3);
                NetworkObject parentObjectTo = null!;
                bool matchRotationOfParent = true;
                Root.parentObject = null;
                if (firstPikminG != null && ManEater != null)
                {
                    ManEater.GetComponentInChildren<EaterBehavior>().DroppedByPikmin(firstPikminG);
                }
                if (!DontParentToObjects)
                {
                    if (firstPikminG != null)
                    {
                        Vector3 hitPoint;
                        NetworkObject physicsRegionOfDroppedObject = GetPhysicsRegionOfDroppedObject(firstPikminG, out hitPoint);
                        if (physicsRegionOfDroppedObject != null)
                        {
                            placePosition = hitPoint;
                            parentObjectTo = physicsRegionOfDroppedObject;
                            matchRotationOfParent = false;
                            PlaceGrabbableObject(parentObjectTo.transform, placePosition, matchRotationOfParent, Root);
                            Root.DiscardItemFromEnemy();
                            isParented = false;
                            firstPikminG = null!;
                            return;
                        }
                    }
                    if (firstPikminG != null && firstPikminG.IsInShip)
                    {
                        //Parent To Ship
                        if (Root.NetworkObject != null && IsServer)
                            Root.transform.SetParent(StartOfRound.Instance.elevatorTransform, worldPositionStays: true);
                    }
                    else if (firstPikminG != null && firstPikminG.IsOnElevator)
                    {
                        //Parent To Elevator
                        if (Root.NetworkObject != null && IsServer)
                            Root.transform.SetParent(RoundManager.Instance.currentMineshaftElevator.elevatorInsidePoint, worldPositionStays: true);
                    }
                    else
                    {
                        //Parent To Root/Prop Container
                        if (StartOfRound.Instance.propsContainer == null)
                        {
                            if (Root.NetworkObject != null && IsServer)
                                Root.transform.SetParent(null, worldPositionStays: true);
                        }
                        else
                        {
                            if (Root.NetworkObject != null && IsServer)
                                Root.transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);
                        }
                    }
                }
                else
                {
                    //Parent To Root/Prop Container
                    if (StartOfRound.Instance.propsContainer == null)
                    {
                        if (Root.NetworkObject != null && IsServer)
                            Root.transform.SetParent(null, worldPositionStays: true);
                    }
                    else
                    {
                        if (Root.NetworkObject != null && IsServer)
                            Root.transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);
                    }
                }
                Root.EnablePhysics(enable: true);
                Root.fallTime = 0f;
                if (Root.transform.parent != null)
                {
                    Root.startFallingPosition = Root.transform.parent.InverseTransformPoint(ObjectPosition);
                    Root.targetFloorPosition = Root.transform.parent.InverseTransformPoint(targetFloorPosition);
                }
                else
                {
                    Root.startFallingPosition = Root.transform.InverseTransformPoint(ObjectPosition);
                    Root.targetFloorPosition = Root.transform.InverseTransformPoint(targetFloorPosition);
                }
                Root.floorYRot = -1;
                Root.transform.localScale = Root.originalScale;
                Root.DiscardItemFromEnemy();
                isParented = false;
                if (firstPikminG != null && firstPikminG.IsInShip)
                {
                    if (firstPikminG == null || firstPikminG.previousLeader == null || firstPikminG.previousLeader.Controller == null)
                    {
                        StartOfRound.Instance.localPlayerController.SetItemInElevator(droppedInShipRoom: true, droppedInElevator: true, Root);
                    }
                    else
                    {
                        firstPikminG.previousLeader.Controller.SetItemInElevator(droppedInShipRoom: true, droppedInElevator: true, Root);
                    }
                }
                LethalMin.Logger.LogInfo($"Dropped Item with {Root.transform.localPosition.y - Root.targetFloorPosition.y} distance from floor ({Root.parentObject}) parent '{Root.isHeld}' Is held '{Root.hasHitGround}' has hit gound");
                firstPikminG = null!;
            }
        }
        public void PlaceGrabbableObject(Transform parentObject, Vector3 positionOffset, bool matchRotationOfParent, GrabbableObject placeObject)
        {
            PlayerPhysicsRegion componentInChildren = parentObject.GetComponentInChildren<PlayerPhysicsRegion>();
            if (componentInChildren != null && componentInChildren.allowDroppingItems)
            {
                parentObject = componentInChildren.physicsTransform;
            }
            LethalMin.Logger.LogInfo($"{firstPikminG.uniqueDebugId} is placeing item");
            placeObject.EnablePhysics(enable: true);
            placeObject.EnableItemMeshes(enable: true);
            placeObject.isHeld = false;
            placeObject.isPocketed = false;
            placeObject.heldByPlayerOnServer = false;
            if (firstPikminG.previousLeader == null || firstPikminG.previousLeader.Controller == null)
            {
                //LethalMin.Logger.LogInfo($"Setting item in elevator with loco, is in ship = {firstPikminG.IsInShip}");
                //StartOfRound.Instance.localPlayerController.SetItemInElevator(firstPikminG.IsInShip, firstPikminG.IsInShip, Root);
            }
            else
            {
                //LethalMin.Logger.LogInfo($"Setting item in elevator with prev, is in ship = {firstPikminG.IsInShip}");
                //firstPikminG.previousLeader.Controller.SetItemInElevator(firstPikminG.IsInShip, firstPikminG.IsInShip, Root);
            }
            placeObject.parentObject = null;
            if (Root.NetworkObject != null && IsServer)
                placeObject.transform.SetParent(parentObject, worldPositionStays: true);
            placeObject.startFallingPosition = placeObject.transform.localPosition;
            placeObject.transform.localScale = placeObject.originalScale;
            placeObject.transform.localPosition = positionOffset;
            placeObject.targetFloorPosition = positionOffset;
            if (!matchRotationOfParent)
            {
                placeObject.fallTime = 0f;
            }
            else
            {
                placeObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                placeObject.fallTime = 1.1f;
            }
            placeObject.OnPlaceObject();
        }
        public NetworkObject GetPhysicsRegionOfDroppedObject(PikminAI PikminDropping, out Vector3 hitPoint)
        {
            Transform transform = null;
            RaycastHit hitInfo;
            Ray ray = new Ray(base.transform.position, -Vector3.up);
            if (Physics.Raycast(ray, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawRay(base.transform.position, -Vector3.up * 80f, Color.blue, 2f);
                transform = hitInfo.collider.gameObject.transform;
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
                        hitPoint = componentInChildren.physicsTransform.InverseTransformPoint(hitInfo.point + Vector3.up * 0.04f + Root.itemProperties.verticalOffset * Vector3.up + addPositionOffsetToItems);
                        return parentNetworkObject;
                    }
                    Debug.LogError("Error: physics region transform does not have network object?: " + transform.gameObject.name);
                }
            }
            hitPoint = Vector3.zero;
            return null;
        }
        [ServerRpc(RequireOwnership = false)]
        private void ParentToFirstPikminServerRpc(ulong pikminNetworkObjectId)
        {
            ParentToFirstPikminClientRpc(pikminNetworkObjectId);
        }

        PikminAI firstPikminG;

        [ClientRpc]
        private void ParentToFirstPikminClientRpc(ulong pikminNetworkObjectId)
        {
            if (IsServer) return; // Server already handled this

            NetworkObject pikminNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[pikminNetworkObjectId];
            PikminAI firstPikmin = pikminNetworkObject.GetComponent<PikminAI>();
            firstPikminG = firstPikmin;

            if (firstPikmin != null && Root != null)
            {
                Root.GrabItemFromEnemy(firstPikmin);
                Root.hasHitGround = true;
                Root.parentObject = firstPikmin.HoldPos;
                if (Root.NetworkObject != null && IsServer)
                    Root.transform.SetParent(firstPikmin.HoldPos, worldPositionStays: true);
                Root.EnablePhysics(enable: false);
                isParented = true;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void UnparentItemServerRpc(Vector3 targetFloorPosition)
        {
            UnparentItemClientRpc(targetFloorPosition);
        }

        [ClientRpc]
        private void UnparentItemClientRpc(Vector3 targetFloorPosition)
        {
            UnparentItem(targetFloorPosition);
        }
        #endregion





        #region Pikmin Management       
        private bool isParented = false;

        private void CheckIfGrabbed()
        {
            if ((Root.isHeldByEnemy && PikminOnItem <= PikminNeedOnItem || Root.isHeld) && !wasGrabbed)
            {
                wasGrabbed = true;
                RemoveAllPikminFromItem();
            }
            else if (!(Root.isHeldByEnemy && PikminOnItem <= PikminNeedOnItem || Root.isHeld))
            {
                wasGrabbed = false;
            }
        }
        [ClientRpc]
        public void HandleArrivedClientRpc()
        {
            //Do A flip
            StartCoroutine(Flip());
        }
        public IEnumerator Flip()
        {
            foreach (PikminAI pikmin in PikminOnItemList.ToArray())
            {
                pikmin.SetIntClientRpc("YayInt", pikmin.enemyRandom.Next(0, 3));
                pikmin.SetTriggerClientRpc("Yay");
                pikmin.ReqeustYaySFXClientRpc();
                yield return new WaitForSeconds(0.02f);
            }
        }
        public void RemoveAllPikminAndUnparent()
        {
            // Unparent the item
            if (Root != null)
            {
                Vector3 targetFloorPosition = RoundManager.Instance.RandomlyOffsetPosition(Root.GetItemFloorPosition(), 1.2f, 0.4f);
                UnparentItemServerRpc(targetFloorPosition);
            }

            // Remove all Pikmin from the item
            foreach (PikminAI pikmin in PikminOnItemList.ToArray())
            {
                pikmin.ReleaseItemServerRpc();
            }
            for (int i = 0; i < goToPositionsOccupied.Count; i++)
            {
                goToPositionsOccupied[i] = false;
            }
        }

        public void RemoveAllPikminAndUnparent(Vector3 KnockbackForce)
        {
            // Unparent the item
            if (Root != null)
            {
                Vector3 targetFloorPosition = RoundManager.Instance.RandomlyOffsetPosition(Root.GetItemFloorPosition(), 1.2f, 0.4f);
                UnparentItemServerRpc(targetFloorPosition);
            }

            // Remove all Pikmin from the item
            foreach (PikminAI pikmin in PikminOnItemList.ToArray())
            {
                pikmin.ReleaseItemServerRpc();
                pikmin.ApplyKnockbackServerRpc(KnockbackForce, false, false, 0);
            }
            for (int i = 0; i < goToPositionsOccupied.Count; i++)
            {
                goToPositionsOccupied[i] = false;
            }
        }
        public void RemoveAllPikminFromItem()
        {
            foreach (PikminAI pikmin in PikminOnItemList.ToArray()) // Use ToArray to avoid modifying the list while iterating
            {
                pikmin.ReleaseItemServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddPikminServerRpc(ulong pikminNetworkObjectId)
        {
            // Add the Pikmin to the list on the server
            NetworkObject pikminNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[pikminNetworkObjectId];
            PikminAI pikmin = pikminNetworkObject.GetComponent<PikminAI>();

            if (pikmin != null && !PikminOnItemList.Contains(pikmin))
            {
                PikminOnItemList.Add(pikmin);
                pikmin.IsOnItem = true;

                // Synchronize with clients
                AddPikminClientRpc(pikminNetworkObjectId);
            }
            else if (pikmin != null)
            {
                pikmin.IsOnItem = true;
            }
        }

        [ClientRpc]
        private void AddPikminClientRpc(ulong pikminNetworkObjectId)
        {
            if (IsServer) return; // Server already handled this

            NetworkObject pikminNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[pikminNetworkObjectId];
            PikminAI pikmin = pikminNetworkObject.GetComponent<PikminAI>();

            if (pikmin != null && !PikminOnItemList.Contains(pikmin))
            {
                PikminOnItemList.Add(pikmin);
                pikmin.IsOnItem = true;
                LethalMin.Logger.LogInfo(pikmin + " Grabbed " + name);
            }
            else if (pikmin != null)
            {
                pikmin.IsOnItem = true;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemovePikminServerRpc(ulong pikminNetworkObjectId)
        {
            NetworkObject pikminNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[pikminNetworkObjectId];
            PikminAI pikmin = pikminNetworkObject.GetComponent<PikminAI>();

            if (pikmin != null && PikminOnItemList.Contains(pikmin))
            {
                PikminOnItemList.Remove(pikmin);
                pikmin.IsOnItem = false;

                // Synchronize with clients
                RemovePikminClientRpc(pikminNetworkObjectId);
            }
        }

        [ClientRpc]
        private void RemovePikminClientRpc(ulong pikminNetworkObjectId)
        {
            if (IsServer) return; // Server already handled this

            NetworkObject pikminNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[pikminNetworkObjectId];
            PikminAI pikmin = pikminNetworkObject.GetComponent<PikminAI>();

            if (pikmin != null && PikminOnItemList.Contains(pikmin))
            {
                PikminOnItemList.Remove(pikmin);
                pikmin.IsOnItem = false;
            }
        }

        public float CalculateSpeed()
        {
            float speed = 0;

            if (PikminOnItemList.Count == 0)
            {
                return 0; // Changed this to return 0 instead of accessing an empty list
            }

            foreach (PikminAI pikmin in PikminOnItemList)
            {
                speed += pikmin.PlantSpeeds[pikmin.GrowStage] / PikminNeedOnItem * 1f;
            }

            // clamp the speed to be a minmum of 1
            speed = Math.Max(1, speed);

            return speed;
        }
        #endregion
        public Onion TargetOnion;
        public AnimatedOnion TargetAnimatedOnion;
        public PikminType TargetType;
        [ClientRpc]
        public void SetTargetOnionClientRpc(NetworkObjectReference onionRef)
        {
            if (onionRef.TryGet(out NetworkObject onionNO))
            {
                TargetOnion = onionNO.GetComponent<Onion>();
            }
        }
        [ClientRpc]
        public void SuckIntoOnionClientRpc()
        {
            //Remove any and all Pikmin
            if (IsServer && PikminOnItemList.Count > 0)
            {
                HandleArrivedClientRpc();
                RemoveAllPikminAndUnparent();
            }
            if (!LethalMin.AllowProduction)
            {
                return;
            }
            if (TargetOnion == null)
            {
                LethalMin.Logger.LogWarning("Target Onion is null");
                return;
            }
            if (TargetOnion.GetComponent<AnimatedOnion>() == null)
            {
                LethalMin.Logger.LogWarning("Target Onion is not an Animated Onion");
                return;
            }
            if (Root.GetComponent<EnemyAI>() != null)
                Root.GetComponent<EnemyAI>().enabled = false;
            Root.enabled = false; // Disable the root object
            TargetAnimatedOnion = TargetOnion.GetComponent<AnimatedOnion>();


            // Create an overlay material
            Material glowMaterial = new Material(Shader.Find("HDRP/Unlit"));

            // Add overlay meshes
            foreach (Renderer renderer in Root.GetComponentsInChildren<Renderer>())
            {
                if (renderer.gameObject.layer == LayerMask.NameToLayer("MapRadar")
                || renderer.gameObject.layer == LayerMask.NameToLayer("ScanNode"))
                {
                    continue;
                }

                GameObject overlay = new GameObject("GlowOverlay");
                overlay.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                overlay.transform.SetParent(renderer.transform, true);

                if (renderer is MeshRenderer meshRenderer)
                {
                    MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        MeshFilter overlayMesh = overlay.AddComponent<MeshFilter>();
                        overlayMesh.sharedMesh = meshFilter.sharedMesh;
                        Renderer overlayRenderer = overlay.AddComponent<MeshRenderer>();
                        SetupOverlayRenderer(overlayRenderer, glowMaterial);
                    }
                }
                else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    if (skinnedMeshRenderer.sharedMesh != null)
                    {
                        SkinnedMeshRenderer overlayRenderer = overlay.AddComponent<SkinnedMeshRenderer>();
                        overlayRenderer.sharedMesh = skinnedMeshRenderer.sharedMesh;
                        overlayRenderer.bones = skinnedMeshRenderer.bones;
                        overlayRenderer.rootBone = skinnedMeshRenderer.rootBone;
                        SetupOverlayRenderer(overlayRenderer, glowMaterial);
                    }
                }
            }
            TargetAnimatedOnion.DoVacumeClientRpc();
            StartCoroutine(DoGlowAnimationPart());
            StartCoroutine(DoShakeAnimationPart());
            StartCoroutine(DoScaleAndMoveAnimationPart());
        }


        private void SetupOverlayRenderer(Renderer overlayRenderer, Material glowMaterial)
        {
            overlayRenderer.material = new Material(glowMaterial);
            overlayRenderer.material.SetFloat("_SurfaceType", 1); // 1 is for Transparent
            overlayRenderer.material.SetFloat("_BlendMode", 0); // 0 is for Alpha
            overlayRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            overlayRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            overlayRenderer.material.SetInt("_ZWrite", 0);
            overlayRenderer.material.renderQueue = 3000; // Transparent render queue
            overlayRenderer.material.SetShaderPassEnabled("SHADOWCASTER", false);
            overlayRenderer.material.SetColor("_UnlitColor", new Color(1, 1, 1, 0)); // Start transparent
        }

        public IEnumerator DoGlowAnimationPart()
        {
            float duration = 1.0f;
            float elapsed = 0;

            // Get all overlay renderers
            Renderer[] overlays = Root.GetComponentsInChildren<Renderer>()
                .Where(r => r.gameObject.name == "GlowOverlay")
                .ToArray();

            // Fade in glow
            while (elapsed < duration)
            {
                float alpha = Mathf.Lerp(0, 0.5f, elapsed / duration);

                foreach (Renderer overlay in overlays)
                {
                    Color color = overlay.material.color;
                    overlay.material.color = new Color(color.r, color.g, color.b, alpha);

                    // If it's a SkinnedMeshRenderer, update its bones
                    if (overlay is SkinnedMeshRenderer skinnedOverlay)
                    {
                        SkinnedMeshRenderer originalRenderer = overlay.transform.parent.GetComponent<SkinnedMeshRenderer>();
                        if (originalRenderer != null)
                        {
                            skinnedOverlay.bones = originalRenderer.bones;
                            skinnedOverlay.rootBone = originalRenderer.rootBone;
                        }
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        public IEnumerator DoShakeAnimationPart()
        {
            //Shake the item rotaion wise
            for (int i = 0; i < 100; i++)
            {
                Root.transform.localEulerAngles = new Vector3(
                    Mathf.Sin(Time.time * 100) * 5,
                    Mathf.Sin(Time.time * 100) * 5,
                    Mathf.Sin(Time.time * 100) * 5
                );
                yield return new WaitForSeconds(0.02f);
            }
        }
        public IEnumerator DoScaleAndMoveAnimationPart()
        {
            yield return new WaitForSeconds(0.5f);
            // Move the item up and scale it down
            for (int i = 0; i < 100; i++)
            {
                Root.transform.position = Vector3.Lerp(Root.transform.position, TargetAnimatedOnion.SucPoint.position, i / 100f);
                Root.transform.localScale = Vector3.Lerp(Root.transform.localScale, Vector3.zero, i / 100f);
                if (Vector3.Distance(Root.transform.position, TargetAnimatedOnion.SucPoint.position) < 0.1f)
                {
                    break;
                }
                yield return new WaitForSeconds(0.02f);
            }
            //Destroy the object
            if (IsServer)
            {
                TargetOnion.AddToTypesToSpawnServerRpc(TargetType.PikminTypeID, PikminNeedOnItem);
                Root.NetworkObject.Despawn(true);
            }
        }

    }
}