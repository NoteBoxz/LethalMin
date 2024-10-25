using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using LethalMin;
using System.Reflection;
using System.Collections;
using System.Linq;
using System;
using Unity.Netcode.Components;

namespace LethalMin
{
    public class LeaderManager : NetworkBehaviour
    {
        #region Fields and Properties
        public PlayerControllerB Controller { get; set; }
        public List<PikminAI> followingPikmin = new List<PikminAI>();
        private TrajectoryPredictor trajectoryPredictor;

        private PikminAI selectedPikmin;
        private bool isAiming;
        private bool isPlayer;
        private bool isHoldingThrowButton = false;
        private float currentThrowForce;

        private InputAction throwAction;

        private InputAction switchPikminTypeAction, switchPikminPrevTypeAction;

        private Transform throwOrigin;
        private Transform holdPosition;
        private Camera mainCamera;
        public List<PikminType> AvailableTypes { get; private set; } = new List<PikminType>();
        public int CurTypeSelectionIndex { get; private set; } = 0;
        [SerializeField] private float columnSpacing = 1.5f;
        [SerializeField] private float rowSpacing = 0.5f;
        [SerializeField] private int pikminPerRow = 5;
        private Dictionary<PikminType, List<PikminAI>> pikminByType = new Dictionary<PikminType, List<PikminAI>>();
        private NoticeZone noticeZoneInstance;
        #endregion

        #region Initialization
        public void Init()
        {
            CreateDebugCubes();
            InitializeController();
            InitializeTrajectoryPredictor();
            LogNetworkInfo();
            InitializeThrowOrigin();
            InitializeHoldPosition();
            InitializeInputAction();
            StartCoroutine(CheckBooleanValueCoroutine());
            InitalizeNoticeZonesServerRpc(new NetworkObjectReference(NetworkObject));
        }

        [ServerRpc(RequireOwnership = false)]
        private void InitalizeNoticeZonesServerRpc(NetworkObjectReference leaderRef)
        {
            if (leaderRef.TryGet(out NetworkObject leaderObject))
            {
                LeaderManager leader = leaderObject.GetComponent<LeaderManager>();
                if (noticeZoneInstance == null)
                {
                    GameObject NZ = Instantiate(LethalMin.NoticeZone, leader.Controller.transform);
                    noticeZoneInstance = NZ.GetComponent<NoticeZone>();
                    noticeZoneInstance.leader = leader.Controller;
                    noticeZoneInstance.NetworkObject.Spawn();
                    noticeZoneInstance.transform.SetParent(leader.Controller.transform);
                }
                SyncNoticeZoneClientRpc(new NetworkObjectReference(noticeZoneInstance.NetworkObject), leaderRef);
            }
        }

        [ClientRpc]
        private void SyncNoticeZoneClientRpc(NetworkObjectReference noticeZoneRef, NetworkObjectReference leaderRef)
        {
            if (noticeZoneRef.TryGet(out NetworkObject noticeZoneObject) && leaderRef.TryGet(out NetworkObject leaderObject))
            {
                NoticeZone noticeZone = noticeZoneObject.GetComponent<NoticeZone>();
                LeaderManager leader = leaderObject.GetComponent<LeaderManager>();
                if (noticeZone != null)
                {
                    leader.noticeZoneInstance = noticeZone;
                    noticeZone.leader = leader.Controller;
                    noticeZone.name = $"{leader.Controller.playerUsername}'s NoticeZone";
                    Destroy(noticeZone.GetComponent<MeshRenderer>());
                }
            }
        }

        private void InitializeController()
        {
            Controller = transform.parent.GetComponent<PlayerControllerB>();
            if (Controller == null)
            {
                LethalMin.Logger.LogError("LeaderManager: PlayerControllerB not found on this GameObject.");
                Destroy(this);
            }
        }

        private void InitializeTrajectoryPredictor()
        {
            trajectoryPredictor = GetComponent<TrajectoryPredictor>() ?? gameObject.AddComponent<TrajectoryPredictor>();
            trajectoryPredictor.SetThrowOrigin(throwOrigin);
        }

        private void LogNetworkInfo()
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"{name} IsOwner: {IsOwner}, IsLocalPlayer: {IsLocalPlayer}, OwnerClientId: {OwnerClientId}, NetworkObjectId: {NetworkObjectId}");
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"{name} IsOwner: {Controller.IsOwner}, IsLocalPlayer: {Controller.IsLocalPlayer}, OwnerClientId: {Controller.OwnerClientId}, NetworkObjectId: {Controller.NetworkObjectId}");
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo(HasNetworkObject);
        }

        private void InitializeThrowOrigin()
        {
            mainCamera = Controller.gameplayCamera;
            GameObject throwOriginObj = new GameObject("ThrowOrigin");
            throwOrigin = throwOriginObj.transform;

            if (mainCamera != null)
            {
                throwOrigin.localPosition = new Vector3(mainCamera.transform.position.x -
                 0.15f, mainCamera.transform.position.y, mainCamera.transform.position.z);
                throwOrigin.transform.rotation = mainCamera.transform.rotation;
            }
            else
            {
                throwOrigin.localPosition = new Vector3(0, 1.6f, 0.5f);
            }
            throwOrigin.transform.SetParent(mainCamera.transform, true);
            trajectoryPredictor.SetThrowOrigin(throwOrigin);
        }

        private void InitializeHoldPosition()
        {
            GameObject holdPositionObj = new GameObject("PikminHoldPosition");
            holdPosition = holdPositionObj.transform;
            holdPosition.SetParent(throwOrigin);
            holdPosition.localPosition = new Vector3(0.5f, -0.3f, 0.5f);
        }

        private void InitializeInputAction()
        {
            if (isPlayer)
            {
                throwAction = new InputAction("Throw");
                throwAction.AddBinding(LethalMin.ThrowAction);
                throwAction.performed += OnThrowStarted;
                throwAction.canceled += OnThrowCanceled;
                throwAction.Enable();
                switchPikminTypeAction = new InputAction("SwitchPikminType");
                switchPikminTypeAction.AddBinding(LethalMin.SwitchForwardAction);
                switchPikminTypeAction.canceled += OnSwitchPikminType;
                switchPikminTypeAction.Enable();
                switchPikminPrevTypeAction = new InputAction("SwitchPikminTypeBack");
                switchPikminPrevTypeAction.AddBinding(LethalMin.SwitchBackwawrdsAction);
                switchPikminPrevTypeAction.canceled += OnSwitchPrevPikminType;
                switchPikminPrevTypeAction.Enable();
            }
        }
        #endregion

        #region Input Handling
        private void OnEnable()
        {
            throwAction?.Enable();
            switchPikminTypeAction?.Enable();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            throwAction?.Disable();
            switchPikminTypeAction?.Disable();
        }

        public PikminType GetCurrentSelectedType()
        {
            if (AvailableTypes.Count > 0)
            {
                return AvailableTypes[CurTypeSelectionIndex];
            }
            else if (LethalMin.RegisteredPikminTypes.Count > 0)
            {
                return LethalMin.RegisteredPikminTypes[0];
            }
            else
            {
                return GetDefaultPikminType();
            }
        }

        public PikminType GetPreviousType()
        {
            if (AvailableTypes.Count > 0)
            {
                int prevIndex = (CurTypeSelectionIndex - 1 + AvailableTypes.Count) % AvailableTypes.Count;
                return AvailableTypes[prevIndex];
            }
            else
            {
                return LethalMin.RegisteredPikminTypes[0];
            }
        }

        public PikminType GetNextType()
        {
            if (AvailableTypes.Count > 0)
            {
                int nextIndex = (CurTypeSelectionIndex + 1) % AvailableTypes.Count;
                return AvailableTypes[nextIndex];
            }
            else
            {
                return LethalMin.RegisteredPikminTypes[0];
            }
        }

        public void UpdateAvailableTypes()
        {
            AvailableTypes.Clear();
            foreach (PikminType type in LethalMin.RegisteredPikminTypes.Values)
            {
                if (GetFollowingPikminByType(type).Count > 0)
                {
                    AvailableTypes.Add(type);
                }
            }

            // Ensure CurTypeSelectionIndex is within bounds
            if (AvailableTypes.Count > 0)
            {
                CurTypeSelectionIndex = Mathf.Clamp(CurTypeSelectionIndex, 0, AvailableTypes.Count - 1);
            }
            else
            {
                CurTypeSelectionIndex = 0;
            }
        }

        private void OnSwitchPikminType(InputAction.CallbackContext context)
        {
            UpdateAvailableTypes();
            if (AvailableTypes.Count > 0)
            {
                CurTypeSelectionIndex = (CurTypeSelectionIndex + 1) % AvailableTypes.Count;
                PikminHUD.pikminHUDInstance.UpdateHUD();
            }
        }

        private void OnSwitchPrevPikminType(InputAction.CallbackContext context)
        {
            UpdateAvailableTypes();
            if (AvailableTypes.Count > 0)
            {
                // Use the total count of available types to ensure proper wrapping
                CurTypeSelectionIndex = (CurTypeSelectionIndex - 1 + AvailableTypes.Count) % AvailableTypes.Count;
                PikminHUD.pikminHUDInstance.UpdateHUD();
            }
        }

        private PikminAI GetNearestPikminOfType(PikminType type)
        {
            PikminAI nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (PikminAI pikmin in followingPikmin)
            {
                if (pikmin.PminType == type)
                {
                    float distance = Vector3.Distance(Controller.transform.position, pikmin.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearest = pikmin;
                        nearestDistance = distance;
                    }
                }
            }

            return nearest;
        }


        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private void OnThrowStarted(InputAction.CallbackContext context)
        {
            isHoldingThrowButton = true;
            isAiming = true;
            selectedPikmin = GetNearestPikminOfType(GetCurrentSelectedType());
            if (selectedPikmin != null && selectedPikmin.NetworkObject != null)
            {
                currentThrowForce = selectedPikmin.ThrowForce;
                trajectoryPredictor.SetTrajectoryVisible(true);
                SetPikminComponentsForAiming(selectedPikmin, true);
            }
            else
            {
                //LethalMin.Logger.LogWarning("No Pikmin available to throw or NetworkObject is null.");
                isHoldingThrowButton = false;
                isAiming = false;
            }
        }

        private void OnThrowCanceled(InputAction.CallbackContext context)
        {
            if (!IsServer)
            {
                // StartCoroutine(DelayThrow());
                // return;
            }
            if (selectedPikmin != null && selectedPikmin.NetworkObject != null)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                ThrowPikminServerRpc(selectedPikmin.NetworkObject, cameraForward);
                SetPikminComponentsForAiming(selectedPikmin, false);
            }
            else
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogWarning("No Pikmin selected or NetworkObject is null when attempting to throw.");
            }
            isHoldingThrowButton = false;
            isAiming = false;
            trajectoryPredictor.SetTrajectoryVisible(false);
        }

        IEnumerator DelayThrow()
        {
            yield return new WaitForSeconds(0.01f);
            if (selectedPikmin != null && selectedPikmin.NetworkObject != null)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                ThrowPikminServerRpc(selectedPikmin.NetworkObject, cameraForward);
                SetPikminComponentsForAiming(selectedPikmin, false);
            }
            else
            {
                LethalMin.Logger.LogWarning("No Pikmin selected or NetworkObject is null when attempting to throw.");
            }
            isHoldingThrowButton = false;
            isAiming = false;
            trajectoryPredictor.SetTrajectoryVisible(false);
        }
        #endregion

        #region Update Logic
        int initattempts = 0;
        bool HasPlayerBeenDefined;
        private void Update()
        {
            //if(LethalMin.DebugMode)
            //LethalMin.Logger.LogInfo($"ControllerName: {Controller.gameObject.name},som {StartOfRound.Instance.localPlayerController == Controller}");

            if (Controller != null)
            {
                isPlayer = StartOfRound.Instance.localPlayerController == Controller;
                if (isPlayer && !HasPlayerBeenDefined)
                {
                    HasPlayerBeenDefined = true;
                    InitializeInputAction();
                }
                UpdateThrowOrigin();
                UpdateSelectedPikmin();
                UpdateTrajectoryIfAiming();
                UpdateTransformPosition();
            }
            else
            {
                initattempts++;
                Init();
                if (initattempts > 100)
                {
                    LethalMin.Logger.LogFatal(gameObject.name + " hates pikmin :(");
                    Destroy(gameObject);
                }
            }
        }

        public void LateUpdate()
        {
            if(noticeZoneInstance != null){
                noticeZoneInstance.transform.localScale = new Vector3(LethalMin.PlayerNoticeRange, LethalMin.PlayerNoticeRange, LethalMin.PlayerNoticeRange);
            }
            if (showDebugCubes)
            {
                UpdateDebugCubes();
            }
            if (followingPikmin.Count > 0 && LethalMin.SmarterMinMov)
                OrganizePikminFormation();
        }

        private void UpdateThrowOrigin()
        {
            if (mainCamera != null && throwOrigin != null)
            {
                //throwOrigin.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, mainCamera.transform.position.z + 0.2f);
                //throwOrigin.transform.rotation = mainCamera.transform.rotation;
            }
            //transform.localPosition = Controller.transform.position;
        }

        private void UpdateSelectedPikmin()
        {
            if (isHoldingThrowButton && selectedPikmin != null)
            {
                UpdatePikminAimPosition();
            }
        }

        private void UpdateTrajectoryIfAiming()
        {
            if (isAiming)
            {
                UpdateTrajectory();
            }
        }

        private void UpdateTransformPosition()
        {
            //transform.localPosition = Controller.transform.position;
        }


        [ServerRpc(RequireOwnership = false)]
        private void SyncPikminPositionServerRpc(NetworkObjectReference pikminRef, Vector3 position, Quaternion rotation, ServerRpcParams serverRpcParams = default)
        {
            // Get the client ID of the sender
            ulong senderId = serverRpcParams.Receive.SenderClientId;

            if (pikminRef.TryGet(out NetworkObject pikminObject))
            {
                PikminAI pikmin = pikminObject.GetComponent<PikminAI>();
                if (pikmin != null && pikmin.isHeld)
                {
                    // Update the Pikmin's position and rotation on the server
                    pikmin.rb.position = position;
                    pikmin.rb.rotation = rotation;
                }
            }
        }

        private float lastSyncTime = 0f;
        private const float syncInterval = 0.1f; // Sync every 0.1 seconds
        private void UpdatePikminAimPosition()
        {
            if (selectedPikmin != null && selectedPikmin.isHeld)
            {
                Vector3 newPosition = holdPosition.position;
                Vector3 cameraForward = mainCamera.transform.forward;
                Quaternion newRotation = Quaternion.LookRotation(cameraForward);
                selectedPikmin.rb.position = newPosition;
                selectedPikmin.rb.rotation = newRotation;
                SyncPikminPositionServerRpc(new NetworkObjectReference(selectedPikmin.NetworkObject), newPosition, newRotation);
            }
        }

        private void UpdateTrajectory()
        {
            if (selectedPikmin == null || trajectoryPredictor == null)
            {
                //LethalMin.Logger.LogError("Selected Pikmin or Trjectory Predictor is null!");
                return;
            }

            Vector3 startPosition = throwOrigin.position;
            Vector3 direction = throwOrigin.forward;

            ProjectileProperties properties = new ProjectileProperties
            {
                direction = direction,
                initialPosition = startPosition,
                initialSpeed = selectedPikmin.ProjectileProps.initialSpeed,
                mass = selectedPikmin.ProjectileProps.mass,
                drag = selectedPikmin.ProjectileProps.drag
            };

            trajectoryPredictor.PredictTrajectory(properties);
        }
        private IEnumerator CheckBooleanValueCoroutine()
        {
            while (true)
            {
                bool currentState = Controller.isInsideFactory;
                if (currentState != previousBooleanState)
                {
                    if (currentState == true)
                    {
                        TeleportPikminToPlayerServerRpc(true, false);
                        if (LethalMin.DebugMode)
                            LethalMin.Logger.LogInfo("Player entered factory. Teleporting Pikmin.");
                    }
                    else
                    {
                        TeleportPikminToPlayerServerRpc(true, true);
                        if (LethalMin.DebugMode)
                            LethalMin.Logger.LogInfo("Player exited factory. Teleporting Pikmin.");
                    }
                    previousBooleanState = currentState;
                }
                yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
            }
        }
        #endregion

        #region Player Logic
        private bool previousBooleanState = false;
        #endregion

        #region Pikmin Management
        private List<GameObject> debugCubes = new List<GameObject>();
        private bool showDebugCubes = false;
        private void CreateDebugCubes()
        {
            if (!showDebugCubes) { return; }
            // Remove existing debug cubes
            DestroyDebugCubes();

            // Create new debug cubes for each formation position
            for (int row = 0; row < 20; row++) // Assuming a maximum of 20 rows
            {
                for (int col = 0; col < pikminPerRow; col++)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"FormationDebugCube_R{row}_C{col}";
                    cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // Small cube size

                    // Remove collider to avoid interference
                    Destroy(cube.GetComponent<Collider>());

                    // Set material and color
                    Renderer renderer = cube.GetComponent<Renderer>();
                    renderer.material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
                    renderer.material.color = Color.yellow;

                    debugCubes.Add(cube);
                }
            }
        }

        private void UpdateDebugCubes()
        {
            if (!showDebugCubes) return;

            Vector3 leaderPosition = Controller.transform.position;
            Vector3 leaderForward = Controller.transform.forward;
            Vector3 leaderRight = Controller.transform.right;

            int cubeIndex = 0;
            for (int row = 0; row < 20; row++) // Assuming a maximum of 20 rows
            {
                for (int col = 0; col < pikminPerRow; col++)
                {
                    if (cubeIndex >= debugCubes.Count) return; // Safety check

                    Vector3 position = leaderPosition
                        - leaderForward * (row + 1) * rowSpacing
                        + leaderRight * (col - (pikminPerRow - 1) / 2f) * columnSpacing;

                    debugCubes[cubeIndex].transform.position = position;
                    cubeIndex++;
                }
            }
        }

        private void DestroyDebugCubes()
        {
            foreach (var cube in debugCubes)
            {
                if (cube != null)
                {
                    Destroy(cube);
                }
            }
            debugCubes.Clear();
        }
        private void OrganizePikminFormation()
        {
            Vector3 leaderPosition = Controller.transform.position;
            Vector3 leaderForward = Controller.transform.forward;
            Vector3 leaderRight = Controller.transform.right;

            int currentRow = 0;
            int currentColumn = 0;
            PikminType currentType = GetCurrentSelectedType();

            // Clear and repopulate pikminByType
            pikminByType.Clear();
            foreach (PikminAI pikmin in followingPikmin)
            {
                if (!pikminByType.ContainsKey(pikmin.PminType))
                {
                    pikminByType[pikmin.PminType] = new List<PikminAI>();
                }
                pikminByType[pikmin.PminType].Add(pikmin);
            }

            // Organize Pikmin of the current type first
            if (pikminByType.ContainsKey(currentType))
            {
                foreach (PikminAI pikmin in pikminByType[currentType])
                {
                    AssignFormationPosition(pikmin, leaderPosition, leaderForward, leaderRight, ref currentRow, ref currentColumn);
                }
            }

            // Then organize the rest of the Pikmin
            foreach (var kvp in pikminByType)
            {
                if (kvp.Key != currentType)
                {
                    foreach (PikminAI pikmin in kvp.Value)
                    {
                        AssignFormationPosition(pikmin, leaderPosition, leaderForward, leaderRight, ref currentRow, ref currentColumn);
                    }
                }
            }
        }

        private void AssignFormationPosition(PikminAI pikmin, Vector3 leaderPosition, Vector3 leaderForward, Vector3 leaderRight, ref int currentRow, ref int currentColumn)
        {
            Vector3 targetPosition = leaderPosition
                - leaderForward * (currentRow + 1) * rowSpacing
                + leaderRight * (currentColumn - (pikminPerRow - 1) / 2f) * columnSpacing;

            pikmin.formationTarget = targetPosition;

            currentColumn++;
            if (currentColumn >= pikminPerRow)
            {
                currentColumn = 0;
                currentRow++;
            }
        }
        [ServerRpc(RequireOwnership = false)]
        private void TeleportPikminToPlayerServerRpc(bool Setoutside = false, bool isOutside = false)
        {
            //Vector3 teleportPosition = Controller.isInsideFactory ?Controller.transform.position : GetSafeOutsidePosition();

            Vector3 teleportPosition = Controller.transform.position;
            if (Setoutside)
                SetSideClientRpc(isOutside);
            foreach (PikminAI pikmin in followingPikmin)
            {
                if (pikmin == null) continue;
                if (pikmin.agent.enabled)
                {
                    StartCoroutine(TP(pikmin, teleportPosition));
                }
                else
                {
                    pikmin.transform.position = teleportPosition;
                }
            }
        }
        [ClientRpc]
        private void SetSideClientRpc(bool isOutside = false)
        {
            foreach (PikminAI pikmin in followingPikmin)
            {
                if (pikmin == null) continue;
                pikmin.isOutside = isOutside;
            }
        }
        IEnumerator TP(PikminAI pikmin, Vector3 teleportPosition)
        {
            float formerStop = pikmin.agent.stoppingDistance;
            pikmin.agent.stoppingDistance = 0;
            yield return new WaitForSeconds(0.1f);
            pikmin.agent.Warp(teleportPosition);
            yield return new WaitForSeconds(0.1f);
            pikmin.agent.stoppingDistance = formerStop;
        }

        public void AddPikmin(NetworkObjectReference pikminRef)
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogMessage("Adding pikmin....");
            AddPikminServerRpc(pikminRef);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddPikminServerRpc(NetworkObjectReference pikminRef)
        {
            if (!IsServer)
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogError("AddPikminServerRpc called on client!");
                return;
            }

            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"AddPikminServerRpc called on {(IsHost ? "host" : "server")}");

            if (LethalMin.DebugMode)
                LethalMin.Logger.LogMessage("Adding pikmin to server....");

            if (!NetworkObject.IsSpawned)
            {
                LethalMin.Logger.LogError("LeaderManager's NetworkObject is not spawned!");
                return;
            }

            if (pikminRef.TryGet(out NetworkObject pikminObject))
            {
                PikminAI pikmin = pikminObject.GetComponent<PikminAI>();
                if (pikmin != null && !followingPikmin.Contains(pikmin))
                {
                    followingPikmin.Add(pikmin);
                    SyncPikminListClientRpc(GetPikminNetworkReferences());
                }
                else
                {
                    LethalMin.Logger.LogError($"Failed to get PikminAI component from NetworkObject: {pikminObject.name}");
                }
            }
            else
            {
                LethalMin.Logger.LogError("Failed to resolve Pikmin NetworkObject from NetworkObjectReference");
            }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogMessage("Added pikmin to server....");
        }

        [ServerRpc]
        public void RemovePikminServerRpc(NetworkObjectReference pikminRef, bool SyncList = true)
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogMessage("Removing pikmin from server....");
            if (pikminRef.TryGet(out NetworkObject pikminObject))
            {
                PikminAI pikmin = pikminObject.GetComponent<PikminAI>();
                if (pikmin != null && followingPikmin.Contains(pikmin))
                {
                    pikmin.previousLeader = this;
                    pikmin.whistlingPlayer = null;
                    pikmin.IsWhistled = false;
                    followingPikmin.Remove(pikmin);
                    if (SyncList)
                        SyncPikminListClientRpc(GetPikminNetworkReferences());
                }
            }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogMessage("Removed pikmin from server....");
        }

        [ClientRpc]
        private void SyncPikminListClientRpc(NetworkObjectReference[] pikminRefs)
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogMessage("Updating Client Pikmin List...");
            followingPikmin.Clear();
            foreach (var pikminRef in pikminRefs)
            {
                if (pikminRef.TryGet(out NetworkObject pikminObject))
                {
                    PikminAI pikmin = pikminObject.GetComponent<PikminAI>();
                    if (pikmin != null)
                    {
                        followingPikmin.Add(pikmin);
                    }
                }
            }
            PikminHUD.pikminHUDInstance.UpdateHUD();
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogMessage("Updated Client Pikmin List!");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DismissAllExceptSelectedTypeServerRpc()
        {
            PikminType selectedType = GetCurrentSelectedType();
            List<PikminAI> pikminToRemove = new List<PikminAI>();

            foreach (PikminAI pikmin in followingPikmin)
            {
                if (pikmin.PminType != selectedType)
                {
                    pikminToRemove.Add(pikmin);
                }
            }

            // If no Pikmin of the selected type, dismiss all
            if (pikminToRemove.Count == 0)
            {
                RemoveAllPikminServerRpc();
            }
            else
            {
                StartCoroutine(RemoveAllTyped(pikminToRemove));
            }

            // Update the HUD
            UpdateHUDClientRpc();
        }
        private PikminType GetDefaultPikminType()
        {
            return LethalMin.RegisteredPikminTypes.FirstOrDefault().Value;
        }
        IEnumerator RemoveAllTyped(List<PikminAI> pikminToRemove)
        {
            foreach (PikminAI pikmin in pikminToRemove)
            {
                RemovePikminServerRpc(new NetworkObjectReference(pikmin.NetworkObject), false);
                DismissPikminClientRpc(new NetworkObjectReference(pikmin.NetworkObject));
                yield return new WaitForSeconds(0.02f);
            }
            SyncPikminListClientRpc(GetPikminNetworkReferences());
        }

        [ClientRpc]
        private void DismissPikminClientRpc(NetworkObjectReference pikminRef)
        {
            if (pikminRef.TryGet(out NetworkObject pikminObject))
            {
                PikminAI pikmin = pikminObject.GetComponent<PikminAI>();
                if (pikmin != null)
                {
                    pikmin.targetPlayer = null;
                    pikmin.currentLeader = null;
                    pikmin.currentLeaderNetworkObject = null;
                    pikmin.agent.updateRotation = false;
                    pikmin.SwitchToBehaviourClientRpc((int)PState.Idle);
                }
            }
        }

        [ClientRpc]
        private void UpdateHUDClientRpc()
        {
            if (PikminHUD.pikminHUDInstance != null)
            {
                PikminHUD.pikminHUDInstance.UpdateHUD();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveAllPikminServerRpc(bool SetState = true)
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogMessage("Removing all Pikmin from leader...");

            StartCoroutine(RemoveDelayed(SetState));

            if (LethalMin.DebugMode)
                LethalMin.Logger.LogMessage("All Pikmin removed from leader.");
        }
        IEnumerator RemoveDelayed(bool SetState = true)
        {
            List<PikminAI> pikminToRemove = new List<PikminAI>(followingPikmin);
            foreach (PikminAI pikmin in pikminToRemove)
            {
                if (pikmin != null && pikmin.NetworkObject != null)
                {
                    RemovePikminServerRpc(new NetworkObjectReference(pikmin.NetworkObject), false);
                    pikmin.targetPlayer = null;
                    pikmin.currentLeader = null;
                    pikmin.currentLeaderNetworkObject = null;
                    pikmin.agent.updateRotation = false;
                    if (SetState)
                        pikmin.SwitchToBehaviourClientRpc((int)PState.Idle);
                }
                yield return new WaitForSeconds(0.02f);
            }

            SyncPikminListClientRpc(GetPikminNetworkReferences());
        }

        private NetworkObjectReference[] GetPikminNetworkReferences()
        {
            return followingPikmin
                .Where(p => p != null && p.NetworkObject != null)
                .Select(p => new NetworkObjectReference(p.NetworkObject))
                .ToArray();
        }

        public List<PikminAI> GetFollowingPikminByType(PikminType Type)
        {
            List<PikminAI> AIz = new List<PikminAI>();
            if (followingPikmin.Count > 0)
            {
                foreach (var item in followingPikmin)
                {
                    if (Type == item.PminType)
                        AIz.Add(item);
                }
            }
            return AIz;
        }
        private PikminAI GetNearestPikmin()
        {
            PikminAI nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (PikminAI pikmin in followingPikmin)
            {
                float distance = Vector3.Distance(Controller.transform.position, pikmin.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = pikmin;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
        #endregion

        #region Pikmin Throwing
        [ServerRpc(RequireOwnership = false)]
        private void ThrowPikminServerRpc(NetworkObjectReference pikminRef, Vector3 cameraForward, ServerRpcParams serverRpcParams = default)
        {
            if (pikminRef.TryGet(out NetworkObject pikminObject))
            {
                PikminAI pikmin = pikminObject.GetComponent<PikminAI>();
                if (pikmin != null)
                {
                    ulong clientId = serverRpcParams.Receive.SenderClientId;
                    PlayerControllerB player = null;

                    foreach (var palyer in StartOfRound.Instance.allPlayerScripts)
                    {
                        if (palyer.actualClientId == clientId)
                        {
                            player = palyer;
                            break;
                        }
                    }

                    if (player != null)
                    {
                        Vector3 throwPosition = player.transform.position + player.transform.forward + player.transform.up;
                        Vector3 throwForward = cameraForward; // Use the camera's forward vector
                        float throwForce = pikmin.ThrowForce;

                        // Calculate the rotation based on the throw direction
                        Quaternion throwRotation = Quaternion.LookRotation(throwForward);

                        pikmin.transform.position = throwOrigin.transform.position;
                        pikmin.transform.rotation = throwRotation;
                        pikmin.rb.position = throwOrigin.transform.position;
                        pikmin.rb.rotation = throwRotation;
                        pikmin.ThrowPikminClientRpc(throwOrigin.transform.position, throwForward, throwForce, throwRotation);
                        RemovePikminServerRpc(pikminRef);
                        pikmin.SetComponentsForAimingClientRpc(false);

                        // Start the rotation easing
                        pikmin.StartRotationEasingClientRpc();
                    }
                    else
                    {
                        LethalMin.Logger.LogError($"Failed to find PlayerControllerB for client ID: {clientId}");
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetPikminComponentsForAimingServerRpc(NetworkObjectReference pikminRef, bool isAiming)
        {
            if (pikminRef.TryGet(out NetworkObject pikminObject))
            {
                PikminAI pikmin = pikminObject.GetComponent<PikminAI>();
                if (pikmin != null)
                {
                    pikmin.SetComponentsForAimingClientRpc(isAiming);
                }
            }
        }

        private void SetPikminComponentsForAiming(PikminAI pikmin, bool isAiming)
        {
            SetPikminComponentsForAimingServerRpc(new NetworkObjectReference(pikmin.NetworkObject), isAiming);
        }
        #endregion
    }

}