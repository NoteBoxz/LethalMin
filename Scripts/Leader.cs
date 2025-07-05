using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin;
using LethalMin.HUD;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.Windows;

namespace LethalMin
{
    public class Leader : NetworkBehaviour
    {
        public PlayerControllerB Controller = null!;
        public PikminNoticeZone NoticeZone = null!;
        public List<PikminAI> PikminInSquad = new List<PikminAI>();
        public Queue<NetworkObjectReference> PikminRemoveQueue = new Queue<NetworkObjectReference>();
        public Dictionary<PuffminAI, Transform> PuffminLatchedOn = new Dictionary<PuffminAI, Transform>();
        public InputAction throwAction = null!, secondaryThrowAction = null!;
        public InputAction switchPikminTypeAction = null!, switchPikminPrevTypeAction = null!;
        public InputAction throwCancelAction = null!;
        public Transform ThrowOrigin = null!;
        public Transform holdPosition = null!;
        private Camera mainCamera = null!;
        private GUIStyle debugTextStyle = null!;
        private Coroutine pikminRemoveCoroutine = null!;
        public PikminAI? pikminHolding;
        public TrajectoryPredictor predictor = null!;
        public CustomPlayerAnimationManager CustomAnimController = null!;
        public GameObject LeaflingSprout = null!;
        public Renderer LeaflingSproutMesh = null!, LeaflingLeafMesh = null!;
        public LeaderFormationManager formManager = null!;
        public Glowmob? glowmob;
        public GameObject? LeaflingGhostInstance = null;
        public NetworkVariable<bool> FriendlyFire = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        bool HasAttemptedToBindActions;
        public int CurrentWhistleIndex = 0;
        bool LastIsInsideValue = false;
        public bool IsLeafling = false;
        public bool wasLeaflingBeforeDeath = false;
        public bool DidHaveConnectedPlayer = false;
        public PikminType? LeaflingType = null;
        private readonly List<PikminType> _cachedPikminTypes = new List<PikminType>();
        public List<PikminType> GetPikminTypesInSquad()
        {
            _cachedPikminTypes.Clear();

            PikminInSquad.RemoveAll(p => p == null);
            foreach (PikminAI pikmin in PikminInSquad)
            {
                PikminType type = pikmin.pikminType;
                if (!_cachedPikminTypes.Contains(type))
                {
                    _cachedPikminTypes.Add(type);
                }
            }

            // Sort the list by pikminTypeID before returning
            _cachedPikminTypes.Sort((a, b) => a.PikminTypeID.CompareTo(b.PikminTypeID));

            return _cachedPikminTypes;
        }
        private readonly Dictionary<PikminType, int> _cachedTypeCounts = new Dictionary<PikminType, int>();
        public Dictionary<PikminType, int> GetPikminTypesInSquadWithCount()
        {
            _cachedTypeCounts.Clear();

            PikminInSquad.RemoveAll(p => p == null);
            foreach (PikminAI pikmin in PikminInSquad)
            {
                if (_cachedTypeCounts.ContainsKey(pikmin.pikminType))
                {
                    _cachedTypeCounts[pikmin.pikminType]++;
                }
                else
                {
                    _cachedTypeCounts[pikmin.pikminType] = 1;
                }
            }

            return _cachedTypeCounts;
        }
        public int CurrentTypeIndex = 0;
        public bool DirectPikminPath = false;
        public PikminType typeRevivedAs = null!;

        public void SetAsLeafling(int TypeIDToBecome = -1)
        {
            IsLeafling = true;
            Material defaultMat = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Types/Red Pikmin/Models/MI_chr_Pikmin_Red_00_M00.mat");
            PikminType? type = LethalMin.GetPikminTypeByID(TypeIDToBecome);
            LeaflingType = type;
            foreach (Renderer render in LeaflingSprout.GetComponentsInChildren<Renderer>())
            {
                render.enabled = true;

                if (render.gameObject.name.Contains("Sprou"))
                    if (type == null || type.PikminSproutOverrideMaterial == null || type.PikminSproutOverrideMaterial.Length == 0)
                    {
                        Material[] mats = render.sharedMaterials;
                        mats[0] = defaultMat;
                        render.sharedMaterials = mats;
                    }
                    else
                    {
                        Material[] mats = render.sharedMaterials;
                        Material mat = type.PikminSproutOverrideMaterial[0];
                        if (!PikUtils.IsOutOfRange(type.PikminSproutOverrideMaterial, (int)LethalMin.SproutModelGeneration.InternalValue))
                            mat = type.PikminSproutOverrideMaterial[(int)LethalMin.SproutModelGeneration.InternalValue];
                        mats[0] = mat;
                        render.sharedMaterials = mats;
                    }
            }
            LethalMin.Logger.LogDebug($"Leader {Controller.playerUsername} is now a Leafling! TypeID: {TypeIDToBecome}");
            UpdateLeafingShadowMode();
        }
        public bool UpdateLeafingShadowMode()
        {
            if (!IsLeafling)
            {
                return IsOwner;
            }
            if (IsOwner)
            {
                LeaflingSproutMesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                LeaflingLeafMesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                return true;
            }
            else
            {
                LeaflingSproutMesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                LeaflingLeafMesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                return false;
            }
        }


        #region Initalizeation
        public void Awake()
        {
            Controller = GetComponent<PlayerControllerB>();
            CurrentWhistleIndex = LethalMin.CurWhistPack.InternalValue;

            // Initialize the GUI style for debug text
            debugTextStyle = new GUIStyle();
            debugTextStyle.fontSize = 20;
            debugTextStyle.normal.textColor = Color.white;
        }
        public void Start()
        {
            InitializeThrowOrigin();
            InitializeHoldPosition();
            InitializeTrajectoryPredictor();
            InitalizePluckAnimation();
            InitalizeSprout();
            InitalizeFormationManager();
            PikminManager.instance.AddLeader(this);
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                SpawnNoticeZone();
            }
        }

        public void SpawnNoticeZone()
        {
            GameObject NoticeZone = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PikminNoticeZone.prefab");
            PikminNoticeZone PikminNoticeZone = Instantiate(NoticeZone, transform).GetComponent<PikminNoticeZone>();
            PikminNoticeZone.NetworkObject.Spawn();
            PikminNoticeZone.NetworkObject.TrySetParent(transform);
        }

        public void SpawnGlowMob(ulong ID)
        {
            GameObject GlowMob = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Types/Glow Pikmin/GlowMob/GlowMob.prefab");
            glowmob = Instantiate(GlowMob, transform).GetComponent<Glowmob>();
            glowmob.NetworkObject.SpawnWithOwnership(ID);
            glowmob.SetLeaderClientRpc(NetworkObject);
        }


        private void InitializeThrowOrigin()
        {
            mainCamera = Controller.gameplayCamera;
            GameObject throwOriginObj = new GameObject("ThrowOrigin");
            ThrowOrigin = throwOriginObj.transform;

            ThrowOrigin.transform.SetParent(mainCamera.transform, true);
            ThrowOrigin.localPosition = new Vector3(0.1f, 0, 0);

            //trajectoryPredictor.SetThrowOrigin(throwOrigin);
        }
        private void InitializeHoldPosition()
        {
            GameObject holdPositionObj = new GameObject("PikminHoldPosition");
            holdPosition = holdPositionObj.transform;
            holdPosition.SetParent(ThrowOrigin);
            holdPosition.localPosition = new Vector3(0.5f, -0.3f, 0.5f);

            //GameObject giz = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Pointer.prefab");
            //Instantiate(giz, holdPosition);
        }
        private void InitializeTrajectoryPredictor()
        {
            predictor = Instantiate(LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/TrajectoryPredictor.prefab"), transform).GetComponent<TrajectoryPredictor>();
            predictor.throwOrigin = ThrowOrigin;
        }
        private void InitalizeSprout()
        {
            LeaflingSprout = Instantiate(LethalMin.PlayerSproutPrefab, Controller.headCostumeContainer.transform.parent);
            LeaflingSprout.transform.localPosition = new Vector3(0, 0.3f, 0);
            LeaflingSprout.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            LeaflingSprout.transform.localRotation = new Quaternion(0.2164f, 0, 0, -0.9763f);
            foreach (Renderer render in LeaflingSprout.GetComponentsInChildren<Renderer>())
            {
                if (render.gameObject.name == "Sprout" && render is SkinnedMeshRenderer SMR)
                {
                    LeaflingSproutMesh = SMR;
                }
                if (render.gameObject.name == "Leaf" && render is MeshRenderer MR)
                {
                    LeaflingLeafMesh = MR;
                }
            }
            //LethalMin.Logger.LogInfo($"Leafling Sprout: {LeaflingSprout}, scene in: {LeaflingSprout.scene.name}");
        }

        private void InitalizeFormationManager()
        {
            formManager = gameObject.AddComponent<LeaderFormationManager>();
            formManager.leader = this;
            formManager.enabled = false;
        }

        private void InitializeInputAction()
        {
            LethalMin.Logger.LogInfo($"Binding input actions for {Controller.playerUsername}");
            if (!LethalMin.UseInputUtils)
            {
                string ThrowActionPath = LethalMin.InVRMode ? LethalMin.ThrowVRAction.InternalValue : LethalMin.ThrowAction.InternalValue;
                LethalMin.Logger.LogInfo($"ThrowActionPath: {ThrowActionPath}");
                throwAction = new InputAction("Throw");
                throwAction.AddBinding(ThrowActionPath);
                throwAction.performed -= OnThrowStarted;
                throwAction.canceled -= OnThrowCanceled;
                throwAction.performed += OnThrowStarted;
                throwAction.canceled += OnThrowCanceled;
                throwAction.Enable();

                string ThrowCancelActionPath = LethalMin.InVRMode ? LethalMin.ThrowCancelVRAction.InternalValue : LethalMin.ThrowCancelAction.InternalValue;
                throwCancelAction = new InputAction("Throw Cancel");
                throwCancelAction.AddBinding(ThrowCancelActionPath);
                throwCancelAction.performed -= OnThrowCancelCanceled;
                throwCancelAction.performed += OnThrowCancelCanceled;
                throwCancelAction.Enable();

                string SecondaryThrowActionPath = LethalMin.InVRMode ? LethalMin.SecondaryThrowVRAction.InternalValue : "";
                if (!string.IsNullOrEmpty(SecondaryThrowActionPath) && !string.IsNullOrWhiteSpace(SecondaryThrowActionPath))
                {
                    LethalMin.Logger.LogInfo($"SecondaryThrowActionPath: {SecondaryThrowActionPath}");
                    secondaryThrowAction = new InputAction("SecondaryThrow");
                    secondaryThrowAction.AddBinding(SecondaryThrowActionPath);
                    secondaryThrowAction.performed -= OnThrowStarted;
                    secondaryThrowAction.canceled -= OnThrowCanceled;
                    secondaryThrowAction.performed += OnThrowStarted;
                    secondaryThrowAction.canceled += OnThrowCanceled;
                    secondaryThrowAction.Enable();
                }

                string SwitchActionPath = LethalMin.InVRMode ? LethalMin.SwitchForwardVRAction.InternalValue : LethalMin.SwitchForwardAction.InternalValue;
                switchPikminTypeAction = new InputAction("SwitchPikminType");
                switchPikminTypeAction.AddBinding(SwitchActionPath);
                switchPikminTypeAction.canceled -= OnSwitchPikminType;
                switchPikminTypeAction.canceled += OnSwitchPikminType;
                switchPikminTypeAction.Enable();

                string SwitchPrevActionPath = LethalMin.InVRMode ? LethalMin.SwitchBackwawrdsVRAction.InternalValue : LethalMin.SwitchBackwawrdsAction.InternalValue;
                switchPikminPrevTypeAction = new InputAction("SwitchPikminTypeBack");
                switchPikminPrevTypeAction.AddBinding(SwitchPrevActionPath);
                switchPikminPrevTypeAction.canceled -= OnSwitchPrevPikminType;
                switchPikminPrevTypeAction.canceled += OnSwitchPrevPikminType;
                switchPikminPrevTypeAction.Enable();
            }
            else
            {
                throwAction = LethalMin.InputClassInstace.Throw;
                throwAction.performed -= OnThrowStarted;
                throwAction.canceled -= OnThrowCanceled;
                throwAction.performed += OnThrowStarted;
                throwAction.canceled += OnThrowCanceled;
                throwAction.Enable();

                throwCancelAction = LethalMin.InputClassInstace.ThrowCancel;
                throwCancelAction.performed -= OnThrowCancelCanceled;
                throwCancelAction.performed += OnThrowCancelCanceled;
                throwCancelAction.Enable();

                switchPikminTypeAction = LethalMin.InputClassInstace.SwitchRight;
                switchPikminTypeAction.canceled -= OnSwitchPikminType;
                switchPikminTypeAction.canceled += OnSwitchPikminType;
                switchPikminTypeAction.Enable();

                switchPikminPrevTypeAction = LethalMin.InputClassInstace.SwitchLeft;
                switchPikminPrevTypeAction.canceled -= OnSwitchPrevPikminType;
                switchPikminPrevTypeAction.canceled += OnSwitchPrevPikminType;
                switchPikminPrevTypeAction.Enable();
            }
        }

        public void InitalizePluckAnimation()
        {
            CustomAnimController = gameObject.AddComponent<CustomPlayerAnimationManager>();
            CustomAnimController.SetUpCustomAnimation(Controller.playerBodyAnimator, "PullLever", LethalMin.PlayerPluckAnim);
        }

        public void HandlePlayerConnected()
        {
            DidHaveConnectedPlayer = true;
            formManager.enabled = IsOwner && LethalMin.PikminFollowMode.InternalValue == PfollowMode.New;
            UpdateLeafingShadowMode();
            LethalMin.Logger.LogInfo($"Leader connection detected!");

            if (IsServer && NoticeZone != null && NoticeZone.OwnerClientId != OwnerClientId)
            {
                LethalMin.Logger.LogInfo($"Set NoticeZone OwnerClientId: {NoticeZone.OwnerClientId} to OwnerClientId: {OwnerClientId}");
                NoticeZone.NetworkObject.ChangeOwnership(OwnerClientId);
            }
            if (IsServer && glowmob != null && glowmob.OwnerClientId != OwnerClientId)
            {
                LethalMin.Logger.LogInfo($"Set Glowmob OwnerClientId: {glowmob.OwnerClientId} to OwnerClientId: {OwnerClientId}");
                glowmob.NetworkObject.ChangeOwnership(OwnerClientId);
            }
        }

        public void HandlePlayerDisconnected()
        {
            DidHaveConnectedPlayer = false;
            formManager.enabled = false;

            if (IsServer && NoticeZone != null && NoticeZone.OwnerClientId != NetworkManager.ServerClientId)
            {
                LethalMin.Logger.LogInfo($"Leader is no longer controlled, setting noticezone ownership to server");
                NoticeZone.NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
            }
            if (IsServer && glowmob != null && glowmob.OwnerClientId != NetworkManager.ServerClientId)
            {
                LethalMin.Logger.LogInfo($"Leader is no longer controlled, setting glowmob ownership to server");
                glowmob.NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
            }
            if (glowmob != null && glowmob.IsDoingGlowmob)
            {
                LethalMin.Logger.LogInfo($"Leader is no longer controlled, stopping glow mob");
                glowmob.StopGlowmob();
            }
        }

        #endregion



        void Update()
        {
            LeaflingSprout.SetActive(IsLeafling);
            if (LethalMin.PlayerNoticeZoneSizeCheat != -1)
            {
                float d = LethalMin.PlayerNoticeZoneSizeCheat;
                NoticeZone.transform.localScale = new Vector3(d, d, d);
            }

            if (IsOwner)
            {
                FriendlyFire.Value = LethalMin.FriendlyFire.InternalValue;
            }

            //throw
            if (pikminHolding != null)
            {
                predictor.PredictTrajectory(pikminHolding.ProjectileProps);
            }

            //Wrapping pikmin to entrance when player enters or leaves the factory
            if (IsOwner && Controller.isInsideFactory != LastIsInsideValue && !Controller.isPlayerDead)
            {
                LastIsInsideValue = Controller.isInsideFactory;
                SetPikminToEntrance(Controller.isInsideFactory);
                SetPikminToEntranceServerRpc(Controller.isInsideFactory);
            }

            //Need to wait before binding actions for some reason so the code is in this update loop
            if (!HasAttemptedToBindActions &&
                StartOfRound.Instance.localPlayerController != null && StartOfRound.Instance.localPlayerController == Controller)
            {
                InitializeInputAction();
                HasAttemptedToBindActions = true;
            }

            //puffmin
            if (PuffminLatchedOn.Count > 0)
            {
                UpdatePuffminWiggleDetection();
            }

            ThrowOrigin.transform.rotation = Controller.gameplayCamera.transform.rotation;
        }




        void LateUpdate()
        {
            // leafing speeds
            if (IsLeafling)
            {
                Controller.sprintMeter = Mathf.Clamp(Controller.sprintMeter, 0, 25f);
            }

            // ownership checks
            if (!DidHaveConnectedPlayer && PikChecks.IsPlayerConnected(Controller))
            {
                HandlePlayerConnected();
            }
            if (DidHaveConnectedPlayer && !PikChecks.IsPlayerConnected(Controller))
            {
                HandlePlayerDisconnected();
            }
        }






        #region Selection

        public void OnSwitchPikminType(InputAction.CallbackContext callbackContext)
        {
            if (!IsOwner)
            {
                return;
            }
            if (StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen) { return; }
            SwitchPikminType(1);
        }
        public void OnSwitchPrevPikminType(InputAction.CallbackContext callbackContext)
        {
            if (!IsOwner)
            {
                return;
            }
            if (StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen) { return; }
            SwitchPikminType(-1);
        }

        [ServerRpc]
        public void SwitchPikminTypeServerRpc(int Direction)
        {
            SwitchPikminTypeClientRpc(Direction);
        }
        [ClientRpc]
        public void SwitchPikminTypeClientRpc(int Direction)
        {
            if (IsOwner)
            {
                return;
            }
            SwitchPikminType(Direction);
        }
        public void SwitchPikminType(int Direction)
        {
            if (PikminInSquad.Count == 0)
            {
                CurrentTypeIndex = 0;
                return;
            }

            List<PikminType> list = GetPikminTypesInSquad();
            if (list.Count == 0)
            {
                CurrentTypeIndex = 0;
                return;
            }
            PikminHUDManager.instance._cacheNeedsRefresh = true;

            PikminType? lastType = GetSelectedType();

            MoveIndexIntoBounds(list);

            if (Direction == 1)
            {
                if (list.Count > 0)
                {
                    CurrentTypeIndex = (CurrentTypeIndex + 1) % list.Count;
                }
            }
            if (Direction == -1)
            {
                if (list.Count > 0)
                {
                    // Use the total count of available types to ensure proper wrapping
                    CurrentTypeIndex = (CurrentTypeIndex - 1 + list.Count) % list.Count;
                }
            }

            formManager.RecalculateFormation();

            if (IsOwner && Direction != 0 && pikminHolding != null)
            {
                if (StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen) { return; }
                if (GetSelectedType() == lastType) { return; }

                PikminAI? close = GetClosestPikminInSquadOfType(GetSelectedType());
                if (close != null)
                {
                    close.StartThrow();
                    StartThrow(close);

                    IsStartThrowRpcCalled = true;
                    StartThrowServerRpc(close.NetworkObject);
                }
            }
        }
        public void MoveIndexIntoBounds(List<PikminType> list)
        {
            if (list.Count > 0)
            {
                CurrentTypeIndex = Mathf.Clamp(CurrentTypeIndex, 0, list.Count - 1);
            }
            else
            {
                CurrentTypeIndex = 0;
            }
        }
        public PikminType? GetSelectedType(List<PikminType> list = null!)
        {
            if (list == null)
                list = GetPikminTypesInSquad();
            if (list.Count > 0 && !PikUtils.IsOutOfRange(list, CurrentTypeIndex))
            {
                return list[CurrentTypeIndex];
            }
            else
            {
                return null;
            }
        }
        public PikminType? GetPreviousType(List<PikminType> list = null!)
        {
            if (list == null)
                list = GetPikminTypesInSquad();
            if (list.Count > 0)
            {
                int prevIndex = (CurrentTypeIndex - 1 + list.Count) % list.Count;
                return list[prevIndex];
            }
            else
            {
                return null;
            }
        }
        public PikminType? GetNextType(List<PikminType> list = null!)
        {
            if (list == null)
                list = GetPikminTypesInSquad();
            if (list.Count > 0)
            {
                int nextIndex = (CurrentTypeIndex + 1) % list.Count;
                return list[nextIndex];
            }
            else
            {
                return null;
            }
        }

        public bool IsAllOfTypeUnslectable(PikminType Ptype, Dictionary<PikminType, int> typesWithCounts)
        {
            int count = typesWithCounts[Ptype];
            int unselectableCount = 0;

            PikminInSquad.RemoveAll(p => p == null);
            foreach (PikminAI ai in PikminInSquad)
            {
                if (ai.Unselectable && ai.pikminType == Ptype)
                {
                    unselectableCount++;
                }
            }

            return count == unselectableCount;
        }

        #endregion






        #region Pikmin Warping
        [ServerRpc]
        public void SetPikminToEntranceServerRpc(bool isInside)
        {
            SetPikminToEntranceClientRpc(isInside);
        }

        [ClientRpc]
        public void SetPikminToEntranceClientRpc(bool isInside)
        {
            SetPikminToEntrance(isInside);
        }

        public void SetPikminToEntrance(bool isInside)
        {
            LethalMin.Logger.LogDebug($"Setting pikmin to entrance: {isInside}");
            List<PikminAI> tempList = new List<PikminAI>(PikminInSquad);
            foreach (PikminAI pikmin in tempList)
            {
                pikmin.WarpToMatchLeaderDoors(isInside);
            }
        }
        #endregion




        #region Throwing
        bool IsStartThrowRpcCalled;
        public bool IsDoThrowRpcCalled;
        public Coroutine bufferedThrowCoroutine = null!;
        /// <summary>
        /// Called when the throw button is pressed
        /// </summary>
        /// <param name="context"></param>
        public void OnThrowStarted(InputAction.CallbackContext context)
        {
            // if (IsStartThrowRpcCalled)
            // {
            //     LethalMin.Logger.LogWarning("Start throw RPC has already been called");
            //     return;
            // }
            if (pikminHolding)
            {
                LethalMin.Logger.LogDebug("Cannot start throw while holding a pikmin");
                return;
            }
            if (IsOwner)
            {
                if (Controller.quickMenuManager.isMenuOpen) { return; }
                if (Controller.isTypingChat) { return; }
                if (Controller.inTerminalMenu) { return; }
                if (OnionHUDManager.instance.IsMenuOpen) { return; }
                if (LethalMin.InVRMode && secondaryThrowAction != null)
                {
                    if (context.action == throwAction && !secondaryThrowAction.IsPressed() ||
                       context.action == secondaryThrowAction && !throwAction.IsPressed())
                    {
                        LethalMin.Logger.LogDebug("Awaiting Second Throw Input");
                        return;
                    }
                }
                //LethalMin.Logger.LogInfo($"Throw button pressed, starting throw");
                PikminAI? close = GetClosestPikminInSquadOfType(GetSelectedType());
                if (close != null)
                {
                    close.StartThrow();
                    StartThrow(close);

                    IsStartThrowRpcCalled = true;
                    StartThrowServerRpc(close.NetworkObject);
                }
            }
        }
        /// <summary>
        /// Called when the throw button is let go
        /// </summary>
        /// <param name="context"></param>
        public void OnThrowCanceled(InputAction.CallbackContext context)
        {
            // if (IsStartThrowRpcCalled && bufferedThrowCoroutine == null)
            // {
            //     LethalMin.Logger.LogWarning("Start throw RPC has not fully gotten through yet waiting...");
            //     bufferedThrowCoroutine = StartCoroutine(OnThrowCancledBuffer(context));
            //     return;
            // }
            // if (IsDoThrowRpcCalled)
            // {
            //     LethalMin.Logger.LogWarning("Do throw RPC has already been called");
            //     return;
            // }
            if (!pikminHolding)
            {
                LethalMin.Logger.LogDebug("Cannot do throw while not holding a pikmin");
                return;
            }
            if (IsOwner)
            {
                if (Controller.quickMenuManager.isMenuOpen) { return; }
                if (Controller.isTypingChat) { return; }
                IsDoThrowRpcCalled = true;
                DoThrowServerRpc(holdPosition.transform.forward);
                pikminHolding?.ThrowPikmin(holdPosition.transform.forward);
                DoThrow();
            }
        }
        IEnumerator OnThrowCancledBuffer(InputAction.CallbackContext context)
        {
            yield return new WaitUntil(() => !IsStartThrowRpcCalled);
            LethalMin.Logger.LogInfo($"Finished waiting for Start throw RPC, starting Do throw RPC");
            OnThrowCanceled(context);
            bufferedThrowCoroutine = null!;
        }

        /// <summary>
        /// Gets the closest pikmin in the leader's squad
        /// </summary>
        /// <returns>The nearest pikmin within the squad</returns>
        public PikminAI GetClosestPikminInSquad()
        {
            PikminAI closestPikmin = null!;
            float closestDistance = float.MaxValue;
            PikminInSquad.RemoveAll(p => p == null);
            foreach (PikminAI pik in PikminInSquad)
            {
                if (pik.currentBehaviourStateIndex != PikminAI.FOLLOW)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, pik.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPikmin = pik;
                }
            }
            return closestPikmin;
        }

        public PikminAI? GetClosestPikminInSquadOfType(PikminType? type)
        {
            if (type == null)
            {
                return null!;
            }
            PikminAI closestPikmin = null!;
            float closestDistance = float.MaxValue;
            PikminInSquad.RemoveAll(p => p == null); // Clean up any null references
            foreach (PikminAI pik in PikminInSquad)
            {
                if (pik.pikminType != type)
                {
                    continue;
                }
                if (pik.currentBehaviourStateIndex != PikminAI.FOLLOW)
                {
                    continue;
                }
                if (pik.Unselectable)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, pik.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPikmin = pik;
                }
            }
            return closestPikmin;
        }

        [ServerRpc]
        public void StartThrowServerRpc(NetworkObjectReference PikRef)
        {
            //LethalMin.Logger.LogMessage("Throw Sync Request sent to server");
            StartThrowClientRpc(PikRef);
        }
        [ClientRpc]
        public void StartThrowClientRpc(NetworkObjectReference PikRef)
        {
            if (IsOwner)
            {
                return;
            }
            //LethalMin.Logger.LogInfo("Throw Sync Request received from server");
            NetworkObject? PikObj;
            PikminAI? ai;

            if (PikRef.TryGet(out PikObj) && PikObj.TryGetComponent<PikminAI>(out ai))
            {
                ai.StartThrow();
                StartThrow(ai);

                if (IsOwner)
                    IsStartThrowRpcCalled = false;
            }
        }

        public void StartThrow(PikminAI pik)
        {
            if (pikminHolding != null)
            {
                pikminHolding.creatureVoice.Stop();
                StopThrow();
            }

            pik.ProjectileProps.direction = holdPosition.transform.forward;
            pikminHolding = pik;

            predictor.trajectoryLine.material.SetColor("_ColorA", pik.pikminType.PikminPrimaryColor);
            predictor.trajectoryLine.material.SetColor("_ColorB", pik.pikminType.PikminSecondaryColor);

            predictor.hitMarker.GetComponentInChildren<Renderer>().material.color = pik.pikminType.PikminSecondaryColor;
        }

        [ServerRpc]
        public void DoThrowServerRpc(Vector3 Dir)
        {
            //LethalMin.Logger.LogInfo("Do Throw Request sent to server");
            DoThrowClientRpc(Dir);
        }
        [ClientRpc]
        public void DoThrowClientRpc(Vector3 Direct)
        {
            if (IsOwner)
            {
                return;
            }
            //LethalMin.Logger.LogInfo("Do Throw Request received from server");
            pikminHolding?.ThrowPikmin(Direct);
            DoThrow();

            if (IsOwner)
                IsDoThrowRpcCalled = false;
        }


        public void DoThrow()
        {
            if (pikminHolding != null)
                pikminHolding.RemoveLeader();
            pikminHolding = null;
            predictor.SetTrajectoryVisible(false);
        }

        [ServerRpc]
        public void StopThrowServerRpc()
        {
            StopThrowClientRpc();
        }
        [ClientRpc]
        public void StopThrowClientRpc()
        {
            if (IsOwner)
            {
                return;
            }
            pikminHolding?.creatureVoice.Stop();
            StopThrow();
        }
        public void StopThrow(bool SetCollisionMode = true)
        {
            if (SetCollisionMode)
            {
                pikminHolding?.SetCollisionMode(1);
                pikminHolding?.ChangeIntent(Pintent.Idle);
                pikminHolding?.animController.ResetToIdleAnim();
            }
            pikminHolding = null;
            predictor.SetTrajectoryVisible(false);
        }


        public void OnThrowCancelCanceled(InputAction.CallbackContext context)
        {
            if (pikminHolding == null)
            {
                return;
            }
            if (IsOwner)
            {
                if (Controller.quickMenuManager.isMenuOpen) { return; }
                if (Controller.isTypingChat) { return; }

                pikminHolding.creatureVoice.Stop();
                StopThrowServerRpc();
                StopThrow();
            }
        }
        #endregion






        #region Pikmin Removement
        public void OnPikminAdded(PikminAI ai)
        {
            SwitchPikminType(0);
        }

        /// <summary>
        /// Removes every pikmin from the leader's squad
        /// Should only be called on the owner side
        /// </summary>
        [ServerRpc]
        public void RemoveAllPikminFromSquadServerRpc()
        {
            RemoveAllPikminFromSquadClientRpc();
        }
        [ClientRpc]
        public void RemoveAllPikminFromSquadClientRpc()
        {
            if (!IsOwner)
                RemoveAllPikminFromSquadOnLocalClient();
        }
        public void RemoveAllPikminFromSquadOnLocalClient()
        {
            List<PikminAI> tempList = new List<PikminAI>(PikminInSquad);
            foreach (PikminAI pik in tempList)
            {
                if (pik != null)
                {
                    pik.PlayAudioOnLocalClient(PikminSoundPackSounds.Lost);
                    pik.SetToIdle();
                }
            }
        }
        /// <summary>
        /// Adds a Pikmin to the removal queue.
        /// A timer is started to remove the Pikmin from the squad. after 0.1 seconds.
        /// The timer is reset when another pikmin is added.
        /// </summary>
        /// <param name="pikmin"></param>
        public void AddToRemoveQueue(PikminAI pikmin)
        {
            //LethalMin.Logger.LogInfo($"Adding Pikmin to Remove Queue: {pikmin.DebugID}");
            if (pikminRemoveCoroutine != null)
            {
                pikminRemoveCoroutine = null!;
            }
            PikminRemoveQueue.Enqueue(pikmin.NetworkObject);
            pikminRemoveCoroutine = StartCoroutine(RemovePikminsFromSquad());
        }
        IEnumerator RemovePikminsFromSquad()
        {
            yield return new WaitForSeconds(0.1f);
            if (pikminRemoveCoroutine == null)
            {
                yield break;
            }
            RemovePikminFromSquadServerRpc(PikminRemoveQueue.ToArray());
            pikminRemoveCoroutine = null!;
        }

        [ServerRpc]
        public void RemovePikminFromSquadServerRpc(NetworkObjectReference PikRef)
        {
            RemovePikminFromSquadClientRpc(PikRef);
        }
        [ClientRpc]
        public void RemovePikminFromSquadClientRpc(NetworkObjectReference PikRef)
        {
            NetworkObject PikObj;
            PikminAI? Pikmin;
            if (PikRef.TryGet(out PikObj) && PikObj.TryGetComponent(out Pikmin) && Pikmin != null)
            {
                LethalMin.Logger.LogDebug($"Removing Pikmin from Squad: {Pikmin.DebugID}");
                RemovePikminFromSquad(Pikmin);
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to remove Pikmin from Squad: {PikRef}");
            }
        }
        [ServerRpc]
        public void RemovePikminFromSquadServerRpc(NetworkObjectReference[] PikRefs)
        {
            LethalMin.Logger.LogInfo($"Sending batch: {PikRefs.Length} to be removed Pikmin from Squad");
            RemovePikminFromSquadClientRpc(PikRefs);
        }
        [ClientRpc]
        public void RemovePikminFromSquadClientRpc(NetworkObjectReference[] PikRefs)
        {
            LethalMin.Logger.LogInfo($"Received batch: {PikRefs.Length} to be removed Pikmin from Squad");
            foreach (NetworkObjectReference PikRef in PikRefs)
            {
                NetworkObject PikObj;
                PikminAI? Pikmin;
                if (PikRef.TryGet(out PikObj) && PikObj.TryGetComponent(out Pikmin) && Pikmin != null)
                {
                    //LethalMin.Logger.LogInfo($"Removing Pikmin from Squad: {Pikmin.DebugID}");
                    Pikmin.SetToIdle();
                }
                else
                {
                    LethalMin.Logger.LogError($"Failed to remove Pikmin from Squad: {PikRef}");
                }
            }
            LethalMin.Logger.LogInfo($"Prosessed batch, current que: {PikminRemoveQueue.Count}");
        }
        /// <summary>
        /// Removes a Pikmin from the squad and deques it on the owner side.
        /// Should be called on every client
        /// </summary>
        /// <param name="Pikmin"></param>
        public void RemovePikminFromSquad(PikminAI Pikmin)
        {
            PikminInSquad.Remove(Pikmin);
            SwitchPikminType(0);
            if (IsOwner && PikminRemoveQueue.Contains(Pikmin.NetworkObject))
            {
                PikminRemoveQueue.Dequeue();
            }
        }
        #endregion







        #region Puffmin Latching
        private Vector3 lastRotationEuler;
        private float totalRotationChange = 0f;
        private float wiggleThreshold = 20f; // Degrees of rotation needed for a successful wiggle
        private float wiggleResetTime = 0.5f; // Time in seconds before wiggle count resets
        private float lastWiggleTime = 0f;
        private int wiggleCount = 0;
        private int requiredWiggles = 10; // Number of wiggles required to shake off a Puffmin

        private void UpdatePuffminWiggleDetection()
        {
            if (!IsOwner || PuffminLatchedOn.Count == 0)
                return;

            Vector3 currentRotation = mainCamera.transform.eulerAngles;
            float rotationDifference = Mathf.Abs(Mathf.DeltaAngle(currentRotation.y, lastRotationEuler.y));

            // Reset wiggle count if player hasn't wiggled for a while
            if (Time.time - lastWiggleTime > wiggleResetTime)
            {
                totalRotationChange = 0f;
            }

            // Add to total rotation change
            totalRotationChange += rotationDifference;

            // If enough rotation has occurred, count it as a wiggle
            if (totalRotationChange >= wiggleThreshold)
            {
                wiggleCount++;
                totalRotationChange = 0f;
                lastWiggleTime = Time.time;

                // Log wiggle for debugging
                LethalMin.Logger.LogDebug($"Wiggle detected! Count: {wiggleCount}/{requiredWiggles}");

                // If enough wiggles have been detected, try to shake off a Puffmin
                if (wiggleCount >= requiredWiggles)
                {
                    TryShakeOffPuffmin();
                    wiggleCount = 0;
                }
            }

            lastRotationEuler = currentRotation;
        }

        private void TryShakeOffPuffmin()
        {
            if (PuffminLatchedOn.Count == 0)
                return;

            ShakePuffminOffServerRpc();

            LethalMin.Logger.LogDebug($"Shaking off Puffmin!");
        }

        [ServerRpc]
        private void ShakePuffminOffServerRpc()
        {
            ShakePuffminOffClientRpc();
        }

        [ClientRpc]
        private void ShakePuffminOffClientRpc()
        {
            foreach (PuffminAI puffmin in new List<PuffminAI>(PuffminLatchedOn.Keys))
            {
                Vector3 direction = (puffmin.transform.position - transform.position).normalized;
                puffmin.ApplyKnockBack(direction, wiggleCount);
            }
        }
        #endregion


        void OnGUI()
        {
            if (Controller == null || !Controller.isPlayerControlled || Controller != StartOfRound.Instance.localPlayerController)
                return;

            // float[] Positions = { 10, 30, 50, 70 };
            //Color[] Colors = { Color.blue, Color.red, Color.green, Color.yellow };


            //debugTextStyle.normal.textColor = Colors[Controller.OwnerClientId];
            //GUI.Label(new Rect(10, 10, 300, 30), $"{(NetworkManager.Singleton.IsHost ? "Host" : "Client")} ID: {Controller.OwnerClientId} Username: {Controller.playerUsername}", debugTextStyle);

            // var allPlayers = StartOfRound.Instance.allPlayerScripts.ToList();

            // // Create a list to store player info
            // var playerInfos = new List<(PlayerControllerB player, int index, bool isLocal)>();

            // // Populate the list
            // for (int i = 0; i < allPlayers.Count; i++)
            // {
            //     var player = allPlayers[i];
            //     if (PikChecks.IsPlayerConnected(player))
            //     {
            //         playerInfos.Add((player, i, player == Controller));
            //     }
            // }

            // // Sort the list: local player first, then others
            // playerInfos.Sort((a, b) =>
            // {
            //     if (a.isLocal) return -1;
            //     if (b.isLocal) return 1;
            //     return a.index.CompareTo(b.index);
            // });

            // // Display labels
            // for (int i = 0; i < playerInfos.Count && i < Positions.Length; i++)
            // {
            //     var (player, originalIndex, isLocal) = playerInfos[i];
            //     Color textColor = Colors[originalIndex % Colors.Length];
            //     debugTextStyle.normal.textColor = textColor;

            //     string label = isLocal
            //         ? $"You: puffmin: {PuffminLatchedOn.Count}"
            //         : $"{player.playerUsername}: puffmin: {player.GetComponent<Leader>().PuffminLatchedOn.Count}";

            //     GUI.Label(new Rect(10, Positions[i], 300, 30), label, debugTextStyle);
            // }


            // float[] Positions = { 10, 30, 50, 70 };
            // Color[] Colors = { Color.blue, Color.red, Color.green, Color.yellow };

            // var allPlayers = StartOfRound.Instance.allPlayerScripts.ToList();

            // // Create a list to store player info
            // var playerInfos = new List<(PlayerControllerB player, int index, bool isLocal)>();

            // // Populate the list
            // for (int i = 0; i < allPlayers.Count; i++)
            // {
            //     var player = allPlayers[i];
            //     if (PikChecks.IsPlayerConnected(player))
            //     {
            //         playerInfos.Add((player, i, player == Controller));
            //     }
            // }

            // // Sort the list: local player first, then others
            // playerInfos.Sort((a, b) =>
            // {
            //     if (a.isLocal) return -1;
            //     if (b.isLocal) return 1;
            //     return a.index.CompareTo(b.index);
            // });

            // // Display labels
            // for (int i = 0; i < playerInfos.Count && i < Positions.Length; i++)
            // {
            //     var (player, originalIndex, isLocal) = playerInfos[i];
            //     Color textColor = Colors[originalIndex % Colors.Length];
            //     debugTextStyle.normal.textColor = textColor;

            //     string label = isLocal
            //         ? $"You: Pikmin in Squad: {PikminInSquad.Count}"
            //         : $"{player.playerUsername}: Pikmin in Squad: {player.GetComponent<Leader>().PikminInSquad.Count}";

            //     GUI.Label(new Rect(10, Positions[i], 300, 30), label, debugTextStyle);
            // }
        }
    }
}
