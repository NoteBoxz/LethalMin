using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;
using System.Collections;

namespace LethalMin
{
    public class PikminItem : NetworkBehaviour, IDebuggable
    {
        public GameObject CounterPrefab;
        public GameObject Counter;
        public TMP_Text PikminOn, PikminNeed;
        public GrabbableObject Root;
        public List<PikminAI> PikminOnItemList, PurplesOnItemList;
        [IDebuggable.Debug] public int PikminOnItem, PikminNeedOnItem, PurpleMultiplier;
        public float SFXInterval = 0.2f;
        private bool isInitialized = false;
        private bool wasGrabbed = false;
        public CaveDwellerAI ManEater;
        private NetworkObject rootNetworkObject;
        [IDebuggable.Debug] Vector3 ObjectPosition;
        Color basecolor;

        #region Unity Lifecycle Methods
        public void Start()
        {
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
                PikminOn = Counter.transform.Find("Text (TMP) (2)").GetComponent<TMP_Text>();
                PikminNeed = Counter.transform.Find("Text (TMP)").GetComponent<TMP_Text>();

                basecolor = PikminOn.color;
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
            PikminOnItem = PikminOnItemList.Count + Mathf.Max(0, (PurplesOnItemList.Count * 10) - PurplesOnItemList.Count);
            if (PikminOnItem < PikminNeedOnItem)
            {
                PikminOn.color = new Color(basecolor.r - 0.1f, basecolor.b - 0.1f, basecolor.g - 0.1f);
            }
            else
            {
                PikminOn.color = basecolor;
            }
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

        #endregion





        #region Item Parenting and Unparenting 
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
                Root.parentObject = firstPikmin.HoldPos;
                Root.transform.SetParent(firstPikmin.HoldPos, worldPositionStays: true);
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
                    Root.transform.SetParent(StartOfRound.Instance.elevatorTransform, worldPositionStays: true);
                }
                else if (firstPikminG != null && firstPikminG.IsOnElevator)
                {
                    //Parent To Elevator
                    Root.transform.SetParent(RoundManager.Instance.currentMineshaftElevator.elevatorInsidePoint, worldPositionStays: true);
                }
                else
                {
                    //Parent To Root/Prop Container
                    if (StartOfRound.Instance.propsContainer == null)
                    {
                        Root.transform.SetParent(null, worldPositionStays: true);
                    }
                    else
                    {
                        Root.transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);
                    }
                }
                Root.EnablePhysics(enable: true);
                Root.fallTime = 0f;
                Root.startFallingPosition = Root.transform.parent.InverseTransformPoint(ObjectPosition);
                Root.targetFloorPosition = Root.transform.parent.InverseTransformPoint(targetFloorPosition);
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
                if (pikmin.PminType.AddTen)
                {
                    PurplesOnItemList.Add(pikmin);
                }
                PikminOnItem = PikminOnItemList.Count + Mathf.Max(0, (PurplesOnItemList.Count * 10) - PurplesOnItemList.Count);
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
                if (pikmin.PminType.AddTen)
                {
                    PurplesOnItemList.Add(pikmin);
                }
                PikminOnItem = PikminOnItemList.Count + Mathf.Max(0, (PurplesOnItemList.Count * 10) - PurplesOnItemList.Count);
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
                if (pikmin.PminType.AddTen)
                {
                    PurplesOnItemList.Remove(pikmin);
                }
                PikminOnItem = PikminOnItemList.Count + Mathf.Max(0, (PurplesOnItemList.Count * 10) - PurplesOnItemList.Count);
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
                if (pikmin.PminType.AddTen)
                {
                    PurplesOnItemList.Remove(pikmin);
                }
                PikminOnItem = PikminOnItemList.Count + Mathf.Max(0, (PurplesOnItemList.Count * 10) - PurplesOnItemList.Count);
                pikmin.IsOnItem = false;
            }
        }
        #endregion



    }
}