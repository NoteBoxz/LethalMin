using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Events;
using LethalMon.Behaviours;
using LethalModDataLib.Helpers;

namespace LethalMin
{
    // Enums and Flags
    public enum PState
    {
        Idle,
        Following,
        Airborn,
        Working,
        Drowning,
        Attacking,
        Leaveing,
        Stuck,
        Posisened,
    }

    public class PikminAI : EnemyAI, IDebuggable
    {
        #region Fields and Properties
        public PikminType PminType = null!; // <- this should never be null after initalizeing, if it is, then it's a bug.
        public System.Random? enemyRandom; // I broken rng in lethal company because I forgot to call AddEnemy() before initializing.
        [IDebuggable.Debug] public LeaderManager? currentLeader; // The current player the pikmin is following
        public LeaderManager? previousLeader; // The previous player the pikmin was following
        private float stateChangeBuffer = 1f; // Time buffer before changing state
        private float stateChangeTimer = 0f; // The actual timer that counts down.
        public NetworkObject? currentLeaderNetworkObject; // The netwrok object of the current leader
        private NetworkObject? previousLeaderNetworkObject; //I have no idea what this was used for
        public Rigidbody? rb; // this also should never be null under any reason.
        private SphereCollider? Pcollider;
        private GameObject?[]? Plants; // the things ontop of pikmin's heads
        private GameObject? Ghost; // boo
        public GameObject? Mesh, IdleGlow;
        public float ThrowForce = 25;
        public ProjectileProperties ProjectileProps { get; private set; }
        public bool isHeldOrThrown;
        public bool isHeld, InitialUP, InitalUR; //UP = UpdatePosition, UR = UpdateRotation
        public bool HideMeshOnStart;
        private float movementThreshold = 0.15f; // Adjust this value to fine-tune movement detection
        public float[] PlantSpeeds = { 7, 10, 15 }; // these are auto changed when the Pikmin type is initalized, but I kept them here for now
        private float GROW_TIME = 10f; // this used to be a const but I changed for RNG purposes
        public int GrowStage;
        public float GrowTimer;
        private bool CanGrabItems, CanAttack = true;
        [IDebuggable.Debug] Vector3 AgentDestionation; // for DB
        [IDebuggable.Debug] Vector3 AgentViolovity; // for DB

        [IDebuggable.Debug] public float Speed;
        [IDebuggable.Debug] public float Acceleration;
        [IDebuggable.Debug] public bool isKine, IsUp, IsUr; // For DB
        private ParticleSystem? ThrowSparkle, ThrowTrail; // Unused because I don't know how to use unity's particle effects
        // Useing custom animator, audio source variables and keeping the ones in EnemyAI null to prevent the game from having a stroke.
        public Animator? LocalAnim;
        public AudioSource? LocalSFX;
        public AudioSource? LocalVoice;
        [IDebuggable.Debug] public Onion? TargetOnion;
        public bool HasInitalized;
        //Mineshaft related fields
        private bool MineshaftInside;
        [IDebuggable.Debug] public bool IsOnElevator { get; private set; }
        [IDebuggable.Debug] public bool IsOnUpperLevel { get; private set; }
        [IDebuggable.Debug] public bool IsOnLowerLevel { get; private set; }
        //Mineshaft related fields end
        public float itemDetectionRange = 5f;
        public float itemDetectionAngle = 360f; // Unused :(
        [IDebuggable.Debug] public PikminItem targetItem;
        public static List<GameObject> PikminItemsInMap = new List<GameObject>();
        public static List<GameObject> PikminItemsExclusion = new List<GameObject>();
        public Transform? HoldPos;
        public string? uniqueDebugId;
        public bool PreDefinedType = false;
        public GameObject? PminColider;
        public bool SpawnedNatuarlly = true; // unused


        bool HasFoundCaryTarget, HasFoundGrabTarget;
        float InShipBuffer;
        [IDebuggable.Debug] public bool IsInShip, CallingHandleItemCarying;
        [IDebuggable.Debug] public string? CarryingItemTo;
        public Vector3 targetScrapPosition = Vector3.zero; // For now, move towards (0, 0, 0)
        Quaternion targetCarryRotaion = Quaternion.identity;
        bool PlayingEnter;
        private const float teleportDelay = 1f; // 1 second delay
        private bool isCheckingNavMesh = false;
        public bool KncockedBack, CannotEscape, IsThrown;
        public bool IsDying, FinnaBeDed, RandomizedSnapTo, Invincible;
        float DedTimer;
        public float drowningTimer;
        public PlayerControllerB? whistlingPlayer;
        public AudioSource? DrowingAud;
        public Transform? SnapToPos;
        Vector3? PositionOffset, rotationOffset;
        public Coroutine? SnapTopPosition;
        public bool IsOnItem;
        public bool HasPlayedLift;
        public bool IsColideingWithTargetEnemy;
        public int ClientsInitalizedOn;
        [IDebuggable.Debug] public (Vector3?, int) AssingedGoToNode = (null, -1);
        bool IsGettingAsinged;
        // I should remove this
        public enum Cstate
        {
            Walking,
            Waiting,
            Carrying,
            none,
        }
        [IDebuggable.Debug] public Cstate stk = Cstate.none;
        // end of I should remove this
        MineshaftElevatorController? elevator;
        public EnemyAI? EnemyAttacking;
        public PikminDamager? EnemyDamager;
        public NetworkTransform? transform2;
        [IDebuggable.Debug] public bool IsDrowing, IsLeaderOnElevator, HasCustomScripts;
        public bool IsWhistled;
        float KnockBackBuffer, AttackTimer;
        bool ShouldDoKBcheck;
        float SnapToBuffer, SnapToBuffer2;
        public Vector3 formationTarget;
        [IDebuggable.Debug] string CurState = ""; // for DB
        MaskedPlayerEnemy FakeLeader = null!; // unsused for now
        public float InternalAirbornTimer;
        public float AttackBuffer;
        int KnockBackResistance = 3;
        public Animator IdleGlowAnim;
        public bool IsLeftBehind;

        //because vector3.distance will never work because the pikmin can never reach the onion on the Y axis.
        private float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = b.x - a.x;
            float dz = b.z - a.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // prolly bad practace, subject to change
        #region StartEvents
        public UnityEvent OnDoAIInterval = new UnityEvent();
        public UnityEvent OnHandleIdleState = new UnityEvent();
        public UnityEvent OnHandleFollowingState = new UnityEvent();
        public UnityEvent OnHandleAirbornState = new UnityEvent();
        public UnityEvent OnHandleDrowningState = new UnityEvent();
        public UnityEvent OnHandleWorkingState = new UnityEvent();
        public UnityEvent OnHandleAttackingState = new UnityEvent();
        public UnityEvent OnHandleLeavingState = new UnityEvent();
        public UnityEvent OnHandleLeaderLost = new UnityEvent();
        public UnityEvent OnCheckForNearbyPlayers = new UnityEvent();
        public UnityEvent OnAssignLeader = new UnityEvent();
        public UnityEvent OnAssignLeaderServerRpc = new UnityEvent();
        public UnityEvent OnAssignLeaderResponseClientRpc = new UnityEvent();
        public UnityEvent OnFindLeaderManagerForPlayer = new UnityEvent();
        public UnityEvent OnSetDrowningClientRpc = new UnityEvent();
        public UnityEvent OnStopDrowningClientRpc = new UnityEvent();
        public UnityEvent OnIsNearDestination = new UnityEvent();
        public UnityEvent OnRemoveFromItemServerRpc = new UnityEvent();
        public UnityEvent OnSetTargetItemServerRpc = new UnityEvent();
        public UnityEvent OnSetTargetItemClientRpc = new UnityEvent();
        public UnityEvent OnDetectNearbyItems = new UnityEvent();
        public UnityEvent OnMoveTowardsItem = new UnityEvent();
        public UnityEvent OnCheckLineOfSightForItem = new UnityEvent();
        public UnityEvent OnOnCollideWithEnemy = new UnityEvent();
        public UnityEvent OnLandPikminClientRpc = new UnityEvent();
        public UnityEvent OnLatchOntoEnemyClientRpc = new UnityEvent();
        #endregion

        #region EndEvents
        public UnityEvent OnDoAIIntervalEnd = new UnityEvent();
        public UnityEvent OnHandleIdleStateEnd = new UnityEvent();
        public UnityEvent OnHandleFollowingStateEnd = new UnityEvent();
        public UnityEvent OnHandleAirbornStateEnd = new UnityEvent();
        public UnityEvent OnHandleDrowningStateEnd = new UnityEvent();
        public UnityEvent OnHandleWorkingStateEnd = new UnityEvent();
        public UnityEvent OnHandleAttackingStateEnd = new UnityEvent();
        public UnityEvent OnHandleLeavingStateEnd = new UnityEvent();
        public UnityEvent OnHandleLeaderLostEnd = new UnityEvent();
        public UnityEvent OnCheckForNearbyPlayersEnd = new UnityEvent();
        public UnityEvent OnAssignLeaderEnd = new UnityEvent();
        public UnityEvent OnAssignLeaderServerRpcEnd = new UnityEvent();
        public UnityEvent OnAssignLeaderResponseClientRpcEnd = new UnityEvent();
        public UnityEvent OnFindLeaderManagerForPlayerEnd = new UnityEvent();
        public UnityEvent OnSetDrowningClientRpcEnd = new UnityEvent();
        public UnityEvent OnStopDrowningClientRpcEnd = new UnityEvent();
        public UnityEvent OnIsNearDestinationEnd = new UnityEvent();
        public UnityEvent OnRemoveFromItemServerRpcEnd = new UnityEvent();
        public UnityEvent OnSetTargetItemServerRpcEnd = new UnityEvent();
        public UnityEvent OnSetTargetItemClientRpcEnd = new UnityEvent();
        public UnityEvent OnDetectNearbyItemsEnd = new UnityEvent();
        public UnityEvent OnMoveTowardsItemEnd = new UnityEvent();
        public UnityEvent OnCheckLineOfSightForItemEnd = new UnityEvent();
        public UnityEvent OnOnCollideWithEnemyEnd = new UnityEvent();
        public UnityEvent OnLandPikminEnd = new UnityEvent();
        public UnityEvent OnLatchOntoEnemyEnd = new UnityEvent();
        #endregion
        //end of bad practace, subject to change

        Vector3 randoVect; // this does not work for some reason
        public bool HasInteractedWithPlayers; // unused for now
        public float IdleTimer;
        [IDebuggable.Debug] public bool IsInCar;
        public VehicleController? TargetCar;
        public float AttackBufferB;
        float OnionBuffer;
        BoxCollider? TargetCarNavMeshSurface;
        [IDebuggable.Debug] public bool CachedIsPlayerInCar = false;
        Transform? TargetCarPos;
        List<GameObject> TempObjects = new List<GameObject>();
        Vector3 MineshaftMainEntrancePosition;
        NetworkVariable<bool> newIsMoving = new NetworkVariable<bool>(false);
        bool IsCallingCLOSFI;
        GameObject NoticeColider;
        [SerializeField] private GameObject scanNode;
        #endregion





        #region Initialization and Setup
        public override void Start()
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo("Pikmin AI is starting");

            HasInitalized = false;

            InternalAirbornTimer = LethalMin.FallTimerValue;

            base.Start();

            if (!IsServer)
            {
                //agent.updatePosition = false;
                //agent.updateRotation = false;
            }
            transform2 = GetComponent<NetworkTransform>();
            PminColider = transform.Find("PikminColision").gameObject;
            NoticeColider = transform.Find("WhistleDetection").gameObject;
            scanNode = transform.Find("ScanNode").gameObject;

            // Immediate initializations
            GROW_TIME = UnityEngine.Random.Range(30, 659);
            GrowTimer = GROW_TIME;

            stateChangeBuffer = LethalMin.AttentionTimer;

            // Because the EnemyAI class uses unnessary methods for moving and syncing position in my case
            moveTowardsDestination = false;
            movingTowardsTargetPlayer = false;
            updatePositionThreshold = 9000;
            syncMovementSpeed = 0f;

            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);

            // Rigidbody Components
            Pcollider = GetComponent<SphereCollider>();
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.Sleep();

            // Agent components
            InitalUR = agent.updateRotation;
            InitialUP = agent.updatePosition;

            //This does not work for whatever reason
            randoVect = new Vector3(
            UnityEngine.Random.Range(-999, 999),
            UnityEngine.Random.Range(-999, 999),
            UnityEngine.Random.Range(-999, 999));

            // Defer some initializations
            StartCoroutine(LateInitialize());

        }
        private IEnumerator LateInitialize()
        {
            yield return new WaitForSeconds(0.1f);  // Wait for one frame

            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"Pikmin is now being spawned");

            Ghost = LethalMin.Ghost;

            //Because EnemyAI is dumb
            creatureAnimator = null;

            // Finalization 
            yield return new WaitForSeconds(0.1f);  // Wait for one frame
            enemyBehaviourStates = new EnemyBehaviourState[Enum.GetValues(typeof(PState)).Length];

            GetPikminItemsInMapList();
            InitalizeType(); // It is very important that this method is ran BEFORE referncing the PminType variable
            InitializeProjectileProperties();
            CheckForOnion();

            //Create DebugName
            string randomPart = GenerateRandomString(5);
            uniqueDebugId = $"{PminType.GetName()}_{randomPart}_{NetworkObjectId}";
            gameObject.name = uniqueDebugId;

            //checking if the dungon is a mineshaft
            if (!PreDefinedType)
            {
                ReqeustPlayBornSoundClientRpc();
            }

            HasInitalized = true;
            CheckClientInitalizedClientRpc();

            CheckForOnion();

            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"Pikmin {uniqueDebugId} has been spawned with type {PminType} and index {thisEnemyIndex}");

            StartCoroutine(WaitToCheckForMineshaft());

            yield return null;  // Wait another frame
        }

        IEnumerator WaitToCheckForMineshaft()
        {
            //Wait until the dungeon is fully generated and the elevator is found
            while (RoundManager.Instance.currentDungeonType == -1 || !RoundManager.Instance.dungeonFinishedGeneratingForAllPlayers)
            {
                yield return new WaitForSeconds(0.1f);
            }
            if (RoundManager.Instance.currentDungeonType == 4 && RoundManager.Instance.currentMineshaftElevator != null)
            {
                MineshaftInside = true;
                elevator = RoundManager.Instance.currentMineshaftElevator;
                MineshaftMainEntrancePosition = RoundManager.FindMainEntrancePosition();
            }
        }

        /// <summary>
        /// Initializes the Pikmin type, setting up its appearance, scripts, and sound effects.
        /// It is the most important part of the Pikmin AI system.
        /// </summary>
        /// <remarks>
        /// This method performs the following tasks:
        /// - Selects a random Pikmin type if not predefined
        /// - Sets up custom scripts for the Pikmin
        /// - Initializes the Pikmin's mesh and glow effects
        /// - Sets up growth stages and animation
        /// - Configures speed settings
        /// - Sets up sound effects for various actions
        /// </remarks>
        public void InitalizeType()
        {
            if (!PreDefinedType)
            {
                if (LethalMin.NaturalTypes.Count == 0)
                {
                    LethalMin.Logger.LogWarning("No natural types found, this should not happen");
                    PminType = LethalMin.RegisteredPikminTypes[0];
                }
                else
                {
                    PminType = LethalMin.NaturalTypes[enemyRandom.Next(0, LethalMin.NaturalTypes.Count)];
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"Picked {PminType} for ramdo");
                }
            }

            //For custom scripts
            GameObject ScriptContainer = transform.Find("PikminScriptContainer").gameObject;
            if (PminType.PikminScripts != null && PminType.PikminScripts.Length > 0)
            {
                HasCustomScripts = true;
                List<NetworkBehaviour> CreatedScripts = ScriptContainer.GetComponents<NetworkBehaviour>().ToList();
                if (PminType.PikminScripts.Length > CreatedScripts.Count)
                {
                    LethalMin.Logger.LogWarning($"Pikmin {PminType.GetName()} has more scripts than the container, this should not happen");
                }
                else
                {
                    for (int i = 0; i < CreatedScripts.Count; i++)
                    {
                        if (LethalMin.DebugMode)
                            LethalMin.Logger.LogInfo($"Checking script {CreatedScripts[i].GetType().Name}");
                        //Check if a created script is not in the pikmin type
                        if (!PminType.PikminScripts.Select(script => script.GetType()).Contains(CreatedScripts[i].GetType()))
                        {
                            //Destroy the script
                            Destroy(CreatedScripts[i]);
                            CreatedScripts.RemoveAt(i);
                            if (LethalMin.DebugMode)
                                LethalMin.Logger.LogInfo("Destroying script");
                        }
                        else
                        {
                            //Enable the script
                            CreatedScripts[i].enabled = true;
                            HasCustomScripts = true;
                            if (LethalMin.DebugMode)
                                LethalMin.Logger.LogInfo("Enabling script");
                        }
                    }
                    if (PminType.PikminScripts.Length > 0)
                        LethalMin.Logger.LogMessage($"Pikmin {PminType.GetName()} has {CreatedScripts.Count} scripts");
                }
            }
            else
            {
                Destroy(ScriptContainer);
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogMessage($"Pikmin {PminType.GetName()} has no scripts");
            }

            //4 Emersion
            PminType.MeshData.ToggleMeshVisibility(!HideMeshOnStart);
            Mesh = Instantiate(PminType.MeshPrefab, transform);

            string? PGP = null;
            if (string.IsNullOrEmpty(PminType.PikminGlowPath) && PminType.GrowthStagePaths.Length >= 0)
            {
                PGP = PminType.GrowthStagePaths[0].RemoveAfterLastSlash();
            }
            else if (!string.IsNullOrEmpty(PminType.PikminGlowPath))
            {
                PGP = PminType.PikminGlowPath;
            }
            if (PGP != null)
            {
                IdleGlow = Instantiate(LethalMin.IdelGlowPrefab, Mesh.transform.Find(PGP));
                IdleGlowAnim = IdleGlow.GetComponent<Animator>();
                if (PminType.PikminGlow != null)
                    IdleGlow.GetComponentInChildren<SpriteRenderer>().sprite = PminType.PikminGlow;
                if (!PminType.ReplaceGlowFXwithDefult && PminType.PikminGlow == null)
                    IdleGlow.GetComponentInChildren<SpriteRenderer>().sprite = null;
                IdleGlow.GetComponentInChildren<SpriteRenderer>().color = PminType.PikminColor;

                IdleGlow.transform.SetParent(Mesh.transform.Find(PGP));
            }

            // Set up plant references
            Plants = new GameObject[PminType.GrowthStagePaths.Length];
            for (int i = 0; i < PminType.GrowthStagePaths.Length; i++)
            {
                Plants[i] = Mesh.transform.Find(PminType.GrowthStagePaths[i]).gameObject;
            }
            LocalAnim = Mesh.transform.Find(PminType.AnimPath).GetComponent<Animator>();
            Mesh.transform.Find(PminType.AnimPath).gameObject.AddComponent<PikminAnimEvents>().AI = this;
            GetComponentInChildren<ScanNodeProperties>(true).headerText = $"{PminType.GetName()}";

            PlantSpeeds = new float[PminType.Speeds.Length];
            for (int i = 0; i < PminType.Speeds.Length; i++)
            {
                PlantSpeeds[i] = PminType.Speeds[i] * PminType.SpeedMultipliers[i];
            }

            UpdateGrowthVisuals();
            ThrowForce = PminType.ThrowForce * PminType.ThrowForceMultiplier;

            transform.Find("MapDot/MapDot (1)").GetComponent<Renderer>().material.color = PminType.PikminColor;

            itemDetectionRange = PminType.ItemDetectionRange;

            if (PminType.soundPack != null && PminType.soundPack.FillEmptyWithDefault)
            {
                if (PminType.soundPack.AttackVoiceLine.Length == 0)
                {
                    PminType.soundPack.AttackVoiceLine = LethalMin.AttackSFX;
                }
                if (PminType.soundPack.BornVoiceLine.Length == 0)
                {
                    PminType.soundPack.BornVoiceLine = LethalMin.BornSFX;
                }
                if (PminType.soundPack.ExitOnionVoiceLine.Length == 0)
                {
                    PminType.soundPack.ExitOnionVoiceLine = LethalMin.ExitOnionSFX;
                }
                if (PminType.soundPack.EnterOnionVoiceLine.Length == 0)
                {
                    PminType.soundPack.EnterOnionVoiceLine = LethalMin.EnterOnionSFX;
                }
                if (PminType.soundPack.ItemNoticeVoiceLine.Length == 0)
                {
                    PminType.soundPack.ItemNoticeVoiceLine = LethalMin.ItemNoticeSFX;
                }
                if (PminType.soundPack.GhostVoiceLine.Length == 0)
                {
                    PminType.soundPack.GhostVoiceLine = LethalMin.GhostSFX;
                }
                if (PminType.soundPack.CarryVoiceLine.Length == 0)
                {
                    PminType.soundPack.CarryVoiceLine = LethalMin.CarrySFX;
                }
                if (PminType.soundPack.LostVoiceLine.Length == 0)
                {
                    PminType.soundPack.LostVoiceLine = LethalMin.LostSFX;
                }
                if (PminType.soundPack.LiftVoiceLine.Length == 0)
                {
                    PminType.soundPack.LiftVoiceLine = new[] { LethalMin.LiftSFX };
                }
                if (PminType.soundPack.HurtVoiceLine.Length == 0)
                {
                    PminType.soundPack.HurtVoiceLine = new[] { LethalMin.DeadSFX };
                }
                if (PminType.soundPack.CrushedVoiceLine.Length == 0)
                {
                    PminType.soundPack.CrushedVoiceLine = new[] { LethalMin.DeadSFX };
                }
                if (PminType.soundPack.NoticeVoiceLine.Length == 0)
                {
                    PminType.soundPack.NoticeVoiceLine = new[] { LethalMin.NoticeSFX };
                }
                if (PminType.soundPack.ThrowVoiceLine.Length == 0)
                {
                    PminType.soundPack.ThrowVoiceLine = new[] { LethalMin.ThrowSFX };
                }
                if (PminType.soundPack.HoldVoiceLine.Length == 0)
                {
                    PminType.soundPack.HoldVoiceLine = new[] { LethalMin.HoldSFX };
                }
                if (PminType.soundPack.YayVoiceLine.Length == 0)
                {
                    PminType.soundPack.YayVoiceLine = LethalMin.YaySFX;
                }
            }
            if (PminType.soundPack?.ThrowSFX.Length == 0)
            {
                PminType.soundPack.ThrowSFX = LethalMin.PlayerThrowSound;
            }
            if (PminType.soundPack?.HitSFX.Length == 0)
            {
                PminType.soundPack.HitSFX = LethalMin.RealHitSFX;
            }
        }

        /// <summary>
        /// for EMERSION
        /// </summary>
        public void ToggleMeshVisibility(bool visible)
        {
            LethalMin.Logger.LogInfo($"Toggling mesh visibility {visible}");
            Renderer[] renderers;

            renderers = Mesh.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }

        private void InitializeProjectileProperties()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            ProjectileProps = new ProjectileProperties
            {
                mass = rb.mass,
                drag = rb.drag,
                initialSpeed = ThrowForce
            };
        }


        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[UnityEngine.Random.Range(0, s.Length)]).ToArray());
        }


        [ClientRpc]
        public void CheckClientInitalizedClientRpc()
        {
            if (HasInitalized)
            {
                if (!IsServer)
                    ClientsInitalizedOn++;
                AddClientsInitalizedServerRpc();
            }
        }


        [ServerRpc(RequireOwnership = false)]
        public void AddClientsInitalizedServerRpc()
        {
            ClientsInitalizedOn++;
        }


        private void OnEnable()
        {
            if (PminColider == null) { PminColider = transform.Find("PikminColision").gameObject; }
        }

        private void OnDisable()
        {

        }
        #endregion

        [ServerRpc(RequireOwnership = false)]
        public void SetWhistleingPlayerServerRpc(NetworkObjectReference player)
        {
            if (player.TryGet(out NetworkObject noticeZoneObject))
            {
                PlayerControllerB players = noticeZoneObject.GetComponent<PlayerControllerB>();
                whistlingPlayer = players;
                IsWhistled = true;
            }
        }
        [ServerRpc(RequireOwnership = false)]
        public void SetWhistleingPlayerServerRpc()
        {
            whistlingPlayer = null;
            IsWhistled = false;
        }


        #region Core Update Logic
        public override void DoAIInterval()
        {
            if (IsServer)
                CheckIfOnNavMesh();

            if (isEnemyDead || FinnaBeDed || IsDying || currentBehaviourStateIndex != (int)PState.Leaveing && StartOfRound.Instance.allPlayersDead)
            {
                return;
            }
            if (HasCustomScripts)
                OnDoAIInterval.Invoke();

            //base.DoAIInterval();
            if (currentBehaviourStateIndex != (int)PState.Leaveing && StartOfRound.Instance.allPlayersDead)
            {
                // If all players are dead, switch to the leaveing state

                //SwitchToBehaviourClientRpc((int)PState.Leaveing);
                //HandleLeavingState();
                //return;
            }

            switch (currentBehaviourStateIndex)
            {
                case (int)PState.Idle:
                    HandleIdleState();
                    break;
                case (int)PState.Following:
                    HandleFollowingState();
                    break;
                case (int)PState.Airborn:
                    HandleAirbornState();
                    break;
                case (int)PState.Drowning:
                    HandleDrowningState();
                    break;
                case (int)PState.Working:
                    HandleWorkingState();
                    break;
                case (int)PState.Attacking:
                    HandleAttackingState();
                    break;
                case (int)PState.Leaveing:
                    HandleLeavingState();
                    break;
            }

            if (HasCustomScripts)
                OnDoAIIntervalEnd.Invoke();
        }

        private void HandleIdleState()
        {
            if (HasCustomScripts)
                OnHandleIdleState.Invoke();

            if (SnapToPos != null && !CannotEscape)
            {
                //Remove SnapToPos
                UnSnapPikmin(false);
            }

            agent.stoppingDistance = 0;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.ResetPath();
            agent.velocity = Vector3.zero;

            NoticeColider.name = "WhistleDetection";
            if (IsWhistled && whistlingPlayer != null)
            {
                NoticeInstant(whistlingPlayer);
                return;
            }
            else if (IsWhistled)
            {
                IsWhistled = false;
                whistlingPlayer = null;
            }
            if (currentLeader != null)
            {
                NoticeInstant(currentLeader.Controller);
            }

            if (IdleTimer > 0)
            {
                IdleTimer -= Time.deltaTime;
            }
            else
            {
                SetIntClientRpc("IdelInt", enemyRandom.Next(0, 9));
                IdleTimer = enemyRandom.Next(1, 5) - 0.5f;
            }

            if (targetItem == null && CanGrabItems && PminType.CanCarryItems)
            {
                DetectNearbyItems();
            }
            if (EnemyAttacking == null && CanAttack && PminType.CanAttackWithoutLatchingOn)
            {
                DetectNearbyEnemies();
            }
            if (HasCustomScripts)
                OnHandleIdleStateEnd.Invoke();
        }

        private void HandleFollowingState()
        {
            if (HasCustomScripts)
                OnHandleFollowingState.Invoke();
            if (targetItem != null)
            {
                //Drop current item

                LethalMin.Logger.LogWarning($"{uniqueDebugId} is following a player but still has an item, dropping.");
                ReleaseItemServerRpc();
            }
            if (CanGrabItems == false || CanAttack == false)
            {
                //Reset actions
                CanGrabItems = true;
                CanAttack = true;
            }
            if (SnapToPos != null && !CannotEscape)
            {
                //Remove SnapToPos
                UnSnapPikmin();
            }
            //Lost condisions
            if (currentLeaderNetworkObject == null || !currentLeaderNetworkObject.IsSpawned)
            {
                HandleLeaderLost();
                return;
            }
            if (currentLeader != null && currentLeader.Controller.isPlayerDead)
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo("Laeder Died!");
                HandleLeaderLost();
                return;
            }
            if (currentLeader != null && currentLeader.Controller.disconnectedMidGame)
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo("Laeder Disconected!");
                HandleLeaderLost();
                return;
            }
            if (currentLeader == null)
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo("Laeder Disconected!");
                HandleLeaderLost();
                return;
            }
            VehicleController NextCar = null!;
            //Set Target Car
            if (LethalMin.TargetCar)
            {
                CachedIsPlayerInCar = PikminManager.IsPlayerInCar(currentLeader.Controller, out NextCar);

                if (CachedIsPlayerInCar && NextCar != null)
                    SetVehicleController(NextCar);
            }
            //Navigation methods
            if (LethalMin.TargetCar && TargetCar != null && TargetCar.backDoorOpen && CachedIsPlayerInCar)
            {
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                agent.stoppingDistance = 1;

                if (TargetCarPos != null)
                {
                    agent.SetDestination(TargetCarPos.transform.position);
                }
                else if (TargetCarNavMeshSurface != null)
                {
                    agent.SetDestination(TargetCarNavMeshSurface.transform.position);
                }
            }
            else if (IsLeaderOnElevator || !LethalMin.SmartMinMov && !LethalMin.SmarterMinMov)
            {
                agent.obstacleAvoidanceType = LethalMin.PikminDefultAvoidanceType;
                agent.stoppingDistance = 2;
                agent.SetDestination(targetPlayer.transform.position);
            }
            else if (LethalMin.SmarterMinMov)
            {
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                agent.stoppingDistance = 0;
                agent.SetDestination(formationTarget);
            }
            else if (LethalMin.SmartMinMov)
            {
                agent.obstacleAvoidanceType = LethalMin.PikminDefultAvoidanceType;
                agent.stoppingDistance = 2;
                // Calculate position behind the target
                Vector3 directionToAgent = (transform.position - targetPlayer.transform.position).normalized;
                Vector3 targetBehindPosition = targetPlayer.transform.position + directionToAgent * agent.stoppingDistance;

                // Find nearest valid NavMesh position
                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetBehindPosition, out hit, agent.stoppingDistance, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
            else
            {
                agent.obstacleAvoidanceType = LethalMin.PikminDefultAvoidanceType;
                agent.stoppingDistance = 2;
                agent.SetDestination(targetPlayer.transform.position);
            }
            NoticeColider.name = "NONE";
            if (HasCustomScripts)
                OnHandleFollowingStateEnd.Invoke();
        }

        private void HandleAirbornState()
        {
            if (HasCustomScripts)
                OnHandleAirbornState.Invoke();
            if (rb.isKinematic)
            {
                //Switch back to idle state if the Pikmin is kinematic
                SwitchToBehaviourClientRpc((int)(PState.Idle));
            }
            if (HasCustomScripts)
                OnHandleAirbornStateEnd.Invoke();
        }

        private void HandleDrowningState()
        {
            if (HasCustomScripts)
                OnHandleDrowningState.Invoke();
            if (IsServer)
            {
                if (targetItem != null)
                {
                    //Drop held item
                    ReleaseItemServerRpc();
                }
                if (currentLeader != null)
                {
                    //Lose leader
                    currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
                    targetPlayer = null;
                    currentLeader = null;
                    currentLeaderNetworkObject = null;
                }

                if (whistlingPlayer != null)
                {
                    // Move towards the whistling player
                    agent.SetDestination(whistlingPlayer.transform.position);
                }
                else
                {
                    // Stay in place
                    agent.SetDestination(transform.position);
                }
            }
            NoticeColider.name = "WhistleDetection";
            if (HasCustomScripts)
                OnHandleDrowningStateEnd.Invoke();
        }

        private void HandleWorkingState()
        {
            if (HasCustomScripts)
                OnHandleWorkingState.Invoke();
            if (CarryingItemTo == "CaveDweller")
            {
                agent.stoppingDistance = 2f;
            }
            else
            {
                agent.stoppingDistance = 0;
            }
            agent.obstacleAvoidanceType = LethalMin.PikminCarryingAvoidanceType;
            if (currentLeader != null)
            {
                // re=assinge leaader
                PlayerControllerB tempB = currentLeader.Controller;
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
                targetPlayer = null;
                currentLeader = null;
                currentLeaderNetworkObject = null;
                AssignLeader(tempB);
            }
            if (targetItem == null)
            {
                //switch back to idle state if the held item is null
                agent.updateRotation = false;
                SwitchToBehaviourClientRpc((int)PState.Idle);
                ReleaseItemServerRpc();

            }
            //Buffer the state change timer
            if (stateChangeTimer <= 0f)
            {
                if (IsWhistled && whistlingPlayer != null)
                {
                    NoticeInstant(whistlingPlayer);
                }
                stateChangeTimer = stateChangeBuffer;
            }
            else if (IsWhistled)
            {
                stateChangeTimer -= Time.deltaTime;
            }
            NoticeColider.name = "WhistleDetectionWhistle";
            if (HasCustomScripts)
                OnHandleWorkingStateEnd.Invoke();
        }

        private void HandleAttackingState()
        {
            if (HasCustomScripts)
                OnHandleAttackingState.Invoke();
            if (SnapToPos == null)
            {
                if (EnemyAttacking != null && currentBehaviourStateIndex == (int)PState.Attacking
                && Vector3.Distance(transform.position, EnemyAttacking.transform.position) <= 1f)
                {
                    if (AttackBufferB >= 0f)
                    {
                        AttackBufferB -= Time.deltaTime;
                    }
                    else
                    {
                        SetTriggerClientRpc("Attack");
                        AttackBufferB = 0.7f;
                    }
                }
                if (EnemyAttacking != null && !EnemyAttacking.isEnemyDead && Vector3.Distance(transform.position, EnemyAttacking.transform.position) <= LethalMin.ChaseRange)
                {
                    //Chase and attack the enemy if not latched on.
                    agent.updatePosition = InitialUP;
                    agent.updateRotation = InitalUR;
                    agent.stoppingDistance = 0;
                    agent.obstacleAvoidanceType = LethalMin.PikminDefultAvoidanceType;
                    agent.SetDestination(EnemyAttacking.transform.position);
                }
                else if (EnemyAttacking != null)
                {
                    // The enemy has been destroyed or the Pikmin has been detached.
                    agent.speed = PlantSpeeds[GrowStage];
                    if (EnemyAttacking.isEnemyDead)
                    {
                        SetIntClientRpc("YayInt", enemyRandom.Next(0, 3));
                        SetTriggerClientRpc("Yay");
                        ReqeustYaySFXClientRpc();
                    }
                    // Set the state to Idle
                    SwitchToBehaviourClientRpc((int)PState.Idle);
                    EnemyAttacking = null;
                }
                else
                {
                    agent.speed = PlantSpeeds[GrowStage];
                    SwitchToBehaviourClientRpc((int)PState.Idle);
                    EnemyAttacking = null;
                }
            }
            NoticeColider.name = "WhistleDetectionWhistle";
            if (HasCustomScripts)
                OnHandleAttackingStateEnd.Invoke();
        }

        private void HandleLeavingState()
        {
            if (HasCustomScripts)
                OnHandleLeavingState.Invoke();
            if (targetItem != null)
            {
                //Drop held item
                ReleaseItemServerRpc();
            }
            if (currentLeader != null)
            {
                //Lose leader
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
                targetPlayer = null;
                currentLeader = null;
                currentLeaderNetworkObject = null;
            }
            if (SnapToPos != null && !CannotEscape)
            {
                //Remove SnapToPos
                UnSnapPikmin();
            }
            agent.stoppingDistance = 0;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            if (HasCustomScripts)
                OnHandleLeavingStateEnd.Invoke();
        }

        private void HandleLeaderLost()
        {
            if (HasCustomScripts)
                OnHandleLeaderLost.Invoke();
            if (IsServer && currentLeader != null)
            {
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
            }
            targetPlayer = null;
            currentLeader = null;
            currentLeaderNetworkObject = null;
            agent.updateRotation = false;
            SwitchToBehaviourClientRpc((int)PState.Idle);
            ReqeustLostSFXClientRpc();
            if (HasCustomScripts)
                OnHandleLeaderLostEnd.Invoke();
        }

        private float CalculateDynamicSpeed(Vector3 targetPosition)
        {
            float baseSpeed = PlantSpeeds[GrowStage];
            float distance = Vector3.Distance(transform.position, targetPosition);
            float maxSpeedMultiplier = 50f; // Adjust this value to change the maximum speed increase
            float speedMultiplier = Mathf.Clamp(distance / 10f, 1f, maxSpeedMultiplier); // Adjust 10f to change how quickly speed increases with distance
            return baseSpeed + 10 * speedMultiplier;
        }
        private Vector3 GetRandomPositionInBounds(BoxCollider Bounds)
        {
            if (Bounds == null)
            {
                LethalMin.Logger.LogWarning($"Pikmin {uniqueDebugId}: Bounds is null?!?!");
                return transform.position;
            }

            Vector3 extents = Bounds.size / 2f;
            Vector3 point = new Vector3(
                UnityEngine.Random.Range(-extents.x, extents.x),
                UnityEngine.Random.Range(-extents.y, extents.y),
                UnityEngine.Random.Range(-extents.z, extents.z)
            );

            // Convert local point to world space
            Vector3 randomPoint = Bounds.transform.TransformPoint(point);

            // Ensure the point is on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            else
            {
                // If no valid NavMesh position found, return the original random point
                return randomPoint;
            }
        }
        public override void Update()
        {
            base.Update();
            //Client and server side logic

            if (HasInitalized)
                CheckAnim();

            transform.localScale = new Vector3(LethalMin.PikminScale, LethalMin.PikminScale, LethalMin.PikminScale);

            if (IdleGlowAnim != null)
                IdleGlowAnim.SetBool("IsIdle", currentBehaviourStateIndex == (int)PState.Idle && !KncockedBack && !IsThrown && !IsDying && !FinnaBeDed && !IsDrowing);

            if (currentBehaviourStateIndex != (int)PState.Idle)
                LocalAnim?.SetInteger("IdelInt", 0);

            //Debuggers
            if (rb != null)
                isKine = rb.isKinematic;
            IsUp = agent.updatePosition;
            IsUr = agent.updateRotation;

            if (HasInitalized && LethalMin.DebugMode)
                CurState = $"{currentBehaviourStateIndex} - {((PState)currentBehaviourStateIndex).ToString()}";

            AgentDestionation = agent.destination;
            AgentViolovity = agent.velocity;

            //Constantly the rotaion
            if (SnapToPos != null)
            {
                //inSpecialAnimation = true;
                if (rotationOffset != null && PositionOffset != null && RandomizedSnapTo)
                {
                    rb.rotation = Quaternion.Euler(
                    SnapToPos.rotation.eulerAngles.x + rotationOffset.Value.x,
                    SnapToPos.rotation.eulerAngles.y + rotationOffset.Value.y,
                    SnapToPos.rotation.eulerAngles.z + rotationOffset.Value.z
                    );
                }
                else
                {
                    rb.rotation = SnapToPos.rotation;
                }
                //Buffer the position
                if (SnapToBuffer2 >= 0f)
                {
                    SnapToBuffer2 -= Time.deltaTime;
                }
                else
                {
                    if (rotationOffset != null && PositionOffset != null && RandomizedSnapTo)
                    {
                        rb.position = new Vector3(SnapToPos.position.x + PositionOffset.Value.x,
                        SnapToPos.position.y + PositionOffset.Value.y, SnapToPos.position.z + PositionOffset.Value.z);
                    }
                    else
                    {
                        rb.position = SnapToPos.position;
                    }

                    SnapToBuffer2 = 0.01f;
                }
                //Buffer the position
                if (SnapToBuffer >= 0f)
                {
                    SnapToBuffer -= Time.deltaTime;
                }
                else
                {
                    SnapToBuffer = 0.1f;
                }

            }

            if (!IsServer) { return; }
            //Server only logic
            Speed = agent.speed;
            Acceleration = agent.acceleration;
            if (currentBehaviourStateIndex != (int)PState.Following)
            {
                CachedIsPlayerInCar = false;
                TargetCar = null;
            }
            if (CachedIsPlayerInCar && IsInCar)
            {
                if (TargetCarPos != null)
                {
                    agent.Warp(TargetCarPos.transform.position);
                }
                else if (TargetCarNavMeshSurface != null)
                {
                    agent.Warp(TargetCarNavMeshSurface.transform.position);
                }
            }

            PminColider.SetActive(!IsDying && !FinnaBeDed && !isEnemyDead && !isHeld);

            HandleGrowing();

            //Airborn buffer incase the pikmin is falling out of bounds.
            if (currentBehaviourStateIndex == (int)PState.Airborn)
            {
                if (InternalAirbornTimer >= 0)
                {
                    InternalAirbornTimer -= Time.deltaTime;
                }
                else
                {
                    ProcessCollisionServerRpc("Air");
                }
            }
            if (targetItem != null && IsOnItem && targetItem.PikminOnItem >= targetItem.PikminNeedOnItem && currentBehaviourStateIndex == (int)PState.Working)
            {
                HandleItemCarrying();
                CallingHandleItemCarying = true;
                if (!HasPlayedLift)
                {
                    ReqeustLiftSFXClientRpc();
                    HasPlayedLift = true;
                }
            }
            else if (!CallingHandleItemCarying && targetItem != null)
            {
                stk = Cstate.Walking;
                MoveTowardsItem();
            }
            else if (CallingHandleItemCarying && targetItem != null && IsOnItem && targetItem.PikminOnItem < targetItem.PikminNeedOnItem && currentBehaviourStateIndex == (int)PState.Working)
            {
                agent.Warp(transform.position);
                agent.updatePosition = InitialUP;
                agent.updateRotation = InitalUR;
                IsOnItem = false;
                HasPlayedLift = false;
                agent.SetDestination(targetItem.GetGoToPos(AssingedGoToNode, uniqueDebugId));
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"{uniqueDebugId}: Reset to moving towards");
                CallingHandleItemCarying = false;
            }
            else
            {
                CallingHandleItemCarying = false;
            }

            if (currentBehaviourStateIndex == (int)PState.Leaveing)
            {
                if (TargetOnion == null || IsLeftBehind)
                {
                    agent.SetDestination(new Vector3(randoVect.x, transform.position.y, randoVect.z));
                }
                else
                {
                    Vector3 targetPosition = TargetOnion.transform.position;
                    float dynamicSpeed = CalculateDynamicSpeed(targetPosition);
                    agent.speed = dynamicSpeed;
                    //agent.stoppingDistance = 0f;
                    agent.SetDestination(targetPosition);

                    if (HorizontalDistance(transform.position, targetPosition) <= 0.2f && !PlayingEnter ||
                     PminType.InstaEnterOnion && !PlayingEnter || OnionBuffer >= 10f && !PlayingEnter)
                    {
                        agent.updatePosition = false;
                        agent.updateRotation = false;
                        transform.position = targetPosition;
                        ReqeustPlayEnterOnionClientRpc();
                        PlayAnimClientRpc("EnterOnion");
                        StartCoroutine(DestoryMin());
                        PlayingEnter = true;
                    }
                    OnionBuffer += Time.deltaTime;
                }
            }

            if (currentBehaviourStateIndex == (int)PState.Attacking && SnapToPos != null && !CannotEscape)
            {
                // The Pikmin is attacking an enemy
                if (EnemyAttacking != null && !EnemyAttacking.isEnemyDead)
                {
                    if (AttackTimer > 0f && !LocalAnim.GetBool("IsAttacking"))
                    {
                        AttackTimer -= Time.deltaTime;
                    }
                    else if (!LocalAnim.GetBool("IsAttacking"))
                    {
                        AttackTimer = 0.4f;
                        if (LethalMin.DebugMode)
                            LethalMin.Logger.LogInfo($"{uniqueDebugId}: hit air");
                        if (previousLeader != null)
                        {
                            EnemyDamager.HitInAirQoutes(PminType.GetDamage(), previousLeader.Controller, true, 1);
                        }
                        else
                        {
                            EnemyDamager.HitInAirQoutes(PminType.GetDamage(), null, true, 1);
                        }
                        ReqeustAttackAndHitSFXClientRpc();
                    }
                }
                else
                {
                    // The enemy has been destroyed or the Pikmin has been detached
                    if (EnemyDamager != null)
                    {
                        EnemyDamager.LatchOffServerRpc(this);
                        EnemyDamager = null;
                    }
                    EnemyAttacking = null;
                    UnSnapPikmin(false, true);
                    SetIntClientRpc("YayInt", enemyRandom.Next(0, 3));
                    SetTriggerClientRpc("Yay");
                    ReqeustYaySFXClientRpc();
                }
            }

            // Update drowning timer
            if (IsDrowing)
            {
                if (whistlingPlayer == null)
                {
                    drowningTimer -= Time.deltaTime;
                }
                else
                {
                    drowningTimer -= Time.deltaTime / 2;
                }

                if (drowningTimer <= 0)
                {
                    // Kill the Pikmin
                    DeathBuffer = true;
                    KillEnemyOnOwnerClient();
                }
            }


            if (IsDying && DedTimer >= 0)
            {
                DedTimer -= Time.deltaTime;
            }
            else if (IsDying)
            {
                DedTimer = 0;
                DeathBuffer = true;
                KillEnemyOnOwnerClient();
            }

            if (isHeld)// || SnapToPos != null)
            {
                transform2.Interpolate = false;
            }
            else
            {
                transform2.Interpolate = true;
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (RoundManager.Instance.SpawnedEnemies.Contains(this))
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"Removed {uniqueDebugId} from the list of spawned enemies");
                RoundManager.Instance.SpawnedEnemies.Remove(this);
            }
            if (!IsServer) { return; }
            if (targetItem != null)
            {
                //Drop held item
                ReleaseItemServerRpc();
            }
            if (currentLeader != null)
            {
                //Lose leader
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
                targetPlayer = null;
                currentLeader = null;
                currentLeaderNetworkObject = null;
            }
            if (TempObjects.Count > 0)
            {
                int initalcount = TempObjects.Count;
                foreach (var tempObject in TempObjects)
                {
                    if (tempObject != null)
                        Destroy(tempObject);
                }
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"Destroyed {initalcount} temp objects");
            }
        }
        public void LateUpdate()
        {
            IsInShip = StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(transform.position) && isOutside;
            IsInCar = TargetCarNavMeshSurface != null && TargetCarNavMeshSurface.bounds.Contains(transform.position);

            // Check if the Pikmin is in the mineshaft
            if (MineshaftInside)
            {
                float distanceToElevator = Vector3.Distance(transform.position, elevator.elevatorInsidePoint.position);
                float elevatorThreshold = 1f;
                if (currentLeader != null)
                {
                    float distanceToElevator2 = Vector3.Distance(currentLeader.Controller.transform.position, elevator.elevatorInsidePoint.position);
                    IsLeaderOnElevator = distanceToElevator2 < elevatorThreshold + 0.6f;
                }
                else
                {
                    IsLeaderOnElevator = false;
                }
                IsOnElevator = distanceToElevator < elevatorThreshold;
                // Check if Pikmin is above or below the elevator
                IsOnUpperLevel = transform.position.y > MineshaftMainEntrancePosition.y - 20;
                IsOnLowerLevel = !IsOnUpperLevel;
            }
            else
            {
                // Not in a dungeon with an elevator, reset all flags
                IsOnElevator = false;
                IsOnUpperLevel = false;
                IsOnLowerLevel = false;
            }

            if (LethalMin.SpeedMultiplier != 1)
            {
                for (int i = 0; i < PlantSpeeds.Length; i++)
                {
                    PlantSpeeds[i] = PminType.Speeds[i] * LethalMin.SpeedMultiplier;
                }
                HasMultipliedSpeed = true;
            }
            else if (HasMultipliedSpeed)
            {
                for (int i = 0; i < PlantSpeeds.Length; i++)
                {
                    PlantSpeeds[i] = PminType.Speeds[i];
                }
                HasMultipliedSpeed = false;
            }

            scanNode.SetActive(LethalMin.ScanMin);
        }
        bool HasMultipliedSpeed;
        #endregion





        #region Movement and Navigation    
        private void CheckAnim()
        {
            LocalAnim?.SetBool("IsMoving", newIsMoving.Value);

            if (!IsServer) { return; }

            newIsMoving.Value = agent.velocity.magnitude > movementThreshold
            && agent.updatePosition == InitialUP && agent.updateRotation == InitalUR;

            bool newIsAttacking = currentBehaviourStateIndex == (int)PState.Attacking && SnapToPos != null;
            bool newiscarry = currentBehaviourStateIndex == (int)PState.Working && targetItem != null && IsOnItem && targetItem.PikminOnItem >= targetItem.PikminNeedOnItem;

            if (LocalAnim.GetBool("IsCarrying") != newiscarry)
            {
                UpdateAnimBoolClientRpc("IsCarrying", newiscarry);
            }

            if (LocalAnim.GetBool("IsAttacking") != newIsAttacking)
            {
                UpdateAnimBoolClientRpc("IsAttacking", newIsAttacking);
            }
        }
        [ClientRpc]
        public void UpdateAnimBoolClientRpc(string Name, bool Value)
        {
            if (!HasInitalized) { return; }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Set Bool: {Name} - {Value}");
            LocalAnim.SetBool(Name, Value);
        }
        [ClientRpc]
        public void SetTriggerClientRpc(string Name)
        {
            if (!HasInitalized) { return; }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Set Trigger: {Name}");
            LocalAnim.SetTrigger(Name);
        }
        [ClientRpc]
        public void SetIntClientRpc(string Name, int Value)
        {
            if (!HasInitalized) { return; }
            LocalAnim.SetInteger(Name, Value);
        }
        [ClientRpc]
        public void PlayAnimClientRpc(string Name)
        {
            if (!HasInitalized) { return; }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Play Anim: {Name}");
            LocalAnim.Play(Name);
            if (Name == "ExitOnion" || Name == "Pluck")
                ToggleMeshVisibility(true);
        }
        public bool HasArrivedAtDestonation(float Offest)
        {
            return Vector3.Distance(transform.position, agent.destination) <= agent.stoppingDistance + Offest;
        }
        public bool HasArrivedAtDestonation(float Offest, Vector3 Target)
        {
            return Vector3.Distance(transform.position, Target) <= agent.stoppingDistance + Offest;
        }
        public void CheckIfOnNavMesh()
        {
            if (!agent.isOnNavMesh && !isCheckingNavMesh)
            {
                StartCoroutine(DelayedNavMeshCheck());
            }
        }
        private IEnumerator DelayedNavMeshCheck()
        {
            isCheckingNavMesh = true;
            yield return new WaitForSeconds(teleportDelay);

            if (!agent.isOnNavMesh)
            {
                // Pikmin is still not on a NavMesh after the delay, attempt to teleport
                Vector3 teleportPosition = FindTeleportPosition();
                if (teleportPosition != Vector3.zero)
                {
                    if (agent.enabled)
                    {
                        agent.Warp(teleportPosition);
                    }
                    else
                    {
                        transform.position = teleportPosition;
                    }
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Teleported Pikmin to: {teleportPosition}");
                }
                else
                {
                    LethalMin.Logger.LogWarning("Failed to find a valid teleport position for Pikmin");
                }
            }
            else
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Pikmin found its way back to NavMesh without teleportation");
            }

            isCheckingNavMesh = false;
        }
        private Vector3 FindTeleportPosition()
        {
            // Try current leader position
            if (currentLeader != null)
            {
                Vector3 leaderNavMeshPoint = FindNearestNavMeshPoint(currentLeader.transform.position);
                if (leaderNavMeshPoint != currentLeader.transform.position)
                {
                    return leaderNavMeshPoint;
                }
            }

            // Try previous leader position (if we have one)
            if (previousLeader != null)
            {
                Vector3 prevLeaderNavMeshPoint = FindNearestNavMeshPoint(previousLeader.transform.position);
                if (prevLeaderNavMeshPoint != previousLeader.transform.position)
                {
                    return prevLeaderNavMeshPoint;
                }
            }

            // If both failed, find nearest NavMesh point
            return FindNearestNavMeshPoint(transform.position);
        }
        private Vector3 FindNearestNavMeshPoint(Vector3 sourcePosition)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(sourcePosition, out hit, 100f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return sourcePosition; // Return original position if no NavMesh point found
        }


        #endregion





        #region Player and Leader Interaction

        IEnumerator DelayedLeaderAssing(NetworkObject leaderNetworkObject)
        {
            yield return new WaitForSeconds(0.01f);
            SyncLeaderServerRpc(leaderNetworkObject);
            if (targetItem != null)
                RemoveFromItemServerRpc();

            agent.updateRotation = InitalUR;
            agent.updatePosition = InitialUP;
            if (EnemyDamager != null)
            {
                EnemyDamager.LatchOffServerRpc(this);
                EnemyDamager = null;
            }
            EnemyAttacking = null;
            SwitchToBehaviourClientRpc((int)PState.Following);
            //if(LethalMin.DebugMode)
            LethalMin.Logger.LogInfo($"({uniqueDebugId}) Switched state!");
        }

        [ServerRpc]
        private void SyncLeaderServerRpc(NetworkObjectReference leaderNetworkObjectRef, bool PlayNoticeAnim = true)
        {
            if (!HasInitalized) { return; }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Syncing leader with server...");
            if (leaderNetworkObjectRef.TryGet(out NetworkObject leaderNetworkObject))
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Successfully resolved NetworkObject: {leaderNetworkObject.name}");

                if (currentLeaderNetworkObject != null)
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Removing Pikmin from old leader...");
                    LeaderManager oldLeader = currentLeaderNetworkObject.GetComponent<LeaderManager>();
                    previousLeader = oldLeader;
                    previousLeaderNetworkObject = currentLeaderNetworkObject;
                    oldLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
                }

                currentLeaderNetworkObject = leaderNetworkObject;
                LeaderManager newLeader = currentLeaderNetworkObject.GetComponent<LeaderManager>();
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Attempting to add Pikmin to new leader: {newLeader.name}");
                newLeader.AddPikmin(new NetworkObjectReference(NetworkObject));
                if (PlayNoticeAnim)
                    SetTriggerClientRpc("Notice");
                SyncLeaderClientRpc(leaderNetworkObjectRef, PlayNoticeAnim);
            }
            else
            {
                LethalMin.Logger.LogError("Failed to resolve NetworkObject from NetworkObjectReference");
            }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Synced leader with server!");
        }

        [ClientRpc]
        private void SyncLeaderClientRpc(NetworkObjectReference leaderNetworkObjectRef, bool PlayNoticeAnim = true)
        {
            if (!HasInitalized) { return; }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Syncing leader with Client...");
            IsThrown = false;
            if (leaderNetworkObjectRef.TryGet(out NetworkObject leaderNetworkObject))
            {
                currentLeaderNetworkObject = leaderNetworkObject;
                currentLeader = leaderNetworkObject.GetComponent<LeaderManager>();
                targetPlayer = currentLeader.Controller;
                HasInteractedWithPlayers = true;
                HasPlayedLift = false;
            }
            if (PlayNoticeAnim)
            {
                if (PminType.soundPack != null)
                {
                    if (PminType.soundPack.NoticeVoiceLine.Length == 1)
                    {
                        LocalVoice.PlayOneShot(PminType.soundPack.NoticeVoiceLine[0]);
                        WalkieTalkie.TransmitOneShotAudio(LocalVoice, PminType.soundPack.NoticeVoiceLine[0]);
                    }
                    else if (PminType.soundPack.NoticeVoiceLine.Length > 1)
                    {
                        LocalVoice.PlayOneShot(PminType.soundPack.NoticeVoiceLine[enemyRandom.Next(0, PminType.soundPack.NoticeVoiceLine.Length)]);
                        WalkieTalkie.TransmitOneShotAudio(LocalVoice, PminType.soundPack.NoticeVoiceLine[enemyRandom.Next(0, PminType.soundPack.NoticeVoiceLine.Length)]);
                    }
                }
                else
                {
                    LocalVoice.PlayOneShot(LethalMin.NoticeSFX);
                    WalkieTalkie.TransmitOneShotAudio(LocalVoice, LethalMin.NoticeSFX);
                }
            }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Synced leader with Client!");
        }
        public void NoticeInstant(PlayerControllerB newLeader, bool PlayNoticeAnim = true)
        {
            if (currentBehaviourStateIndex == (int)PState.Leaveing) { return; }
            if (IsDrowing) { return; }
            if (IsGettingAsinged) { return; }
            if (CannotEscape) { return; }
            if (!(currentBehaviourStateIndex == (int)PState.Idle ||
            currentBehaviourStateIndex == (int)PState.Working ||
             currentBehaviourStateIndex == (int)PState.Attacking))
            {
                return;
            }
            if (newLeader.disconnectedMidGame)
            {
                return;
            }
            SetWhistleingPlayerServerRpc();
            AssignLeader(newLeader, PlayNoticeAnim);
        }
        public void AssignLeader(PlayerControllerB newLeader, bool PlayNoticeAnim = true)
        {
            if (currentBehaviourStateIndex == (int)PState.Leaveing) { return; }
            if (newLeader != null && currentLeader == null && !IsGettingAsinged)
            {
                IsGettingAsinged = true;
                AssignLeaderServerRPC(new NetworkObjectReference(newLeader.NetworkObject), PlayNoticeAnim);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AssignLeaderServerRPC(NetworkObjectReference leaderref, bool PlayNoticeAnim = true)
        {
            if (!HasInitalized)
            {
                AssignLeaderResponseClientRpc(); return;
            }
            if (HasCustomScripts)
                OnAssignLeaderServerRpc.Invoke();
            PlayerControllerB newLeader = null!;
            leaderref.TryGet(out NetworkObject LeaderOBJ);
            if (LeaderOBJ != null)
                newLeader = LeaderOBJ.GetComponent<PlayerControllerB>();
            if (newLeader != null && !IsDying && !FinnaBeDed)
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found player {newLeader.name}");
                // Find the LeaderManager associated with this PlayerControllerB
                LeaderManager leaderManager = FindLeaderManagerForPlayer(newLeader);

                if (leaderManager != null)
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found player with leader manager {newLeader.name}");
                    NetworkObject leaderNetworkObject = leaderManager.GetComponent<NetworkObject>();
                    if (leaderNetworkObject != null)
                    {
                        if (SnapTopPosition != null && CannotEscape)
                        {
                            AssignLeaderResponseClientRpc();
                            return;
                        }
                        if (SnapTopPosition != null && !CannotEscape)
                        {
                            StopCoroutine(SnapTopPosition);
                            if (EnemyDamager != null)
                            {
                                EnemyDamager.LatchOffServerRpc(this);
                                EnemyDamager = null;
                            }
                            EnemyAttacking = null;
                            rotationOffset = null;
                            PositionOffset = null;
                            SnapToPos = null;
                            SnapTopPosition = null;
                        }
                        if (LethalMin.DebugMode)
                            LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found player with  Network Object {newLeader.name}");
                        SyncLeaderServerRpc(leaderNetworkObject, PlayNoticeAnim);
                        
                        if (targetItem != null)
                            ReleaseItemServerRpc();


                        agent.updatePosition = InitialUP;
                        agent.updateRotation = InitalUR;
                        SwitchToBehaviourClientRpc((int)PState.Following);
                    }
                    else
                    {
                        if (LethalMin.DebugMode)
                            LethalMin.Logger.LogInfo($"({uniqueDebugId}) Player {newLeader.name} does not have a network object!!!");
                    }
                }
                else
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Player {newLeader.name} does not have a leader manager!!!");
                }
            }
            AssignLeaderResponseClientRpc();
            if (HasCustomScripts)
                OnAssignLeaderServerRpcEnd.Invoke();
        }

        [ClientRpc]
        private void AssignLeaderResponseClientRpc()
        {
            if (HasCustomScripts)
                OnAssignLeaderResponseClientRpc.Invoke();
            IsGettingAsinged = false;
            if (HasCustomScripts)
                OnAssignLeaderResponseClientRpcEnd.Invoke();
        }

        private LeaderManager FindLeaderManagerForPlayer(PlayerControllerB player)
        {
            if (HasCustomScripts)
                OnFindLeaderManagerForPlayer.Invoke();
            // Option 1: If LeaderManager is a component on the same GameObject as PlayerControllerB
            LeaderManager leaderManager = player.GetComponent<LeaderManager>();
            if (leaderManager != null)
            {
                if (HasCustomScripts)
                    OnFindLeaderManagerForPlayerEnd.Invoke();
                return leaderManager;
            }

            // Option 2: If LeaderManager is on a child GameObject
            leaderManager = player.GetComponentInChildren<LeaderManager>();
            if (leaderManager != null)
            {
                if (HasCustomScripts)
                    OnFindLeaderManagerForPlayerEnd.Invoke();
                return leaderManager;
            }

            // Option 3: If there's a known relationship between PlayerControllerB and LeaderManager
            // For example, if LeaderManager is always on a child object named "LeaderManager"
            Transform leaderManagerTransform = player.transform.Find("LeaderManager");
            if (leaderManagerTransform != null)
            {
                leaderManager = leaderManagerTransform.GetComponent<LeaderManager>();
                if (leaderManager != null)
                {
                    if (HasCustomScripts)
                        OnFindLeaderManagerForPlayerEnd.Invoke();
                    return leaderManager;
                }
            }

            // Option 4: If there's no direct relationship, we might need to search all LeaderManagers in the scene
            LeaderManager[] allLeaderManagers = FindObjectsOfType<LeaderManager>();
            foreach (LeaderManager lm in allLeaderManagers)
            {
                if (lm.Controller == player)
                {
                    if (HasCustomScripts)
                        OnFindLeaderManagerForPlayerEnd.Invoke();
                    return lm;
                }
            }

            // If we couldn't find a LeaderManager, return null
            LethalMin.Logger.LogWarning("Could not find LeaderManager for player: " + player.name);
            return null;
        }

        #endregion





        #region Drowning

        [ClientRpc]
        public void SetDrowingClientRpc()
        {
            if (PminType.IsResistantToWater) { LethalMin.Logger.LogWarning("Why tf is a pikmin that cannot drown, drowning?????"); return; }
            if (LethalMin.UselessblueMinValue) { return; }

            if (HasCustomScripts)
                OnSetDrowningClientRpc.Invoke();

            if (currentBehaviourStateIndex == (int)PState.Airborn)
            {
                LandPikminClientRpc();
            }
            if (IsServer && targetItem != null)
            {
                ReleaseItemServerRpc();
            }
            if (IsServer && currentLeader != null)
            {
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
                targetPlayer = null;
                currentLeader = null;
                currentLeaderNetworkObject = null;
            }
            if (EnemyDamager != null)
            {
                EnemyDamager.LatchOffServerRpc(this);
                EnemyDamager = null;
            }
            EnemyAttacking = null;
            UnSnapPikmin();
            drowningTimer = enemyRandom.Next(5, 10);
            SwitchToBehaviourClientRpc((int)PState.Drowning);
            DrowingAud.Play();
            UpdateAnimBoolClientRpc("IsDrowing", true);
            SetTriggerClientRpc("SetDrowning");
            IsDrowing = true;
            whistlingPlayer = null;

            if (HasCustomScripts)
                OnSetDrowningClientRpc.Invoke();
        }

        [ClientRpc]
        public void StopDrowingClientRpc()
        {
            if (HasCustomScripts)
                OnStopDrowningClientRpc.Invoke();
            if (IsServer && targetItem != null)
            {
                ReleaseItemServerRpc();
            }
            if (IsServer && currentLeader != null)
            {
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
                targetPlayer = null;
                currentLeader = null;
                currentLeaderNetworkObject = null;
            }
            drowningTimer = enemyRandom.Next(5, 10);
            SwitchToBehaviourClientRpc((int)PState.Idle);
            DrowingAud.Stop();
            UpdateAnimBoolClientRpc("IsDrowing", false);
            IsDrowing = false;
            AssignLeader(whistlingPlayer);
            whistlingPlayer = null;
            if (HasCustomScripts)
                OnStopDrowningClientRpcEnd.Invoke();
        }

        #endregion





        #region Item Detection and Interaction

        public static void RefreshPikminItemsInMapList()
        {
            PikminItemsInMap.Clear();
            PikminItem[] allGrabbables = UnityEngine.Object.FindObjectsOfType<PikminItem>();
            foreach (PikminItem grabbable in allGrabbables)
            {
                if (PikminItemsExclusion.Contains(grabbable.gameObject)) { continue; }
                if (grabbable == null) { continue; }
                if (grabbable.Root == null) { continue; }
                if (!grabbable.Root.deactivated)
                {
                    PikminItemsInMap.Add(grabbable.gameObject);
                }
            }
        }
        public static void RefreshPikminEnemiesInMapList()
        {
            NonPikminEnemies.Clear();
            EnemyAI[] allEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            foreach (EnemyAI item in allEnemies)
            {
                if (item.GetComponent<PikminAI>() != null) { continue; }
                if (item.isEnemyDead) { continue; }
                NonPikminEnemies.Add(item.gameObject);
            }
        }
        public static void GetPikminItemsInMapList()
        {
            if (PikminManager.GetPikminItemsInMap().Count == 0)
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo("PikminItemsInMap is empty, refreshing list...");
                RefreshPikminItemsInMapList();
            }
            else
            {
                PikminItemsInMap = PikminManager.GetPikminItemsInMap();
                PikminItemsExclusion = PikminManager.PikminItemsExclusion;
            }

            if (PikminManager.GetNonPikminEnemies().Count == 0)
            {
                RefreshPikminEnemiesInMapList();
            }
            else
            {
                NonPikminEnemies = PikminManager.GetNonPikminEnemies();
            }
        }
        private bool IsNearDestination(Vector3 position, float thresholdA = 1f, float thresholdB = 5f)
        {
            // Check if near any InsideShipPosition
            if (RoundManager.Instance.currentLevel.sceneName != "CompanyBuilding" && isOutside)
            {
                foreach (var shipPos in StartOfRound.Instance.insideShipPositions)
                {
                    if (Vector3.Distance(position, shipPos.position) <= thresholdA)
                    {
                        return true;
                    }
                }
            }

            // Check if near the main entrance
            if (RoundManager.Instance.currentLevel.sceneName != "CompanyBuilding" && !isOutside)
            {
                Vector3 mainEntrancePos = RoundManager.FindMainEntrancePosition();
                if (Vector3.Distance(position, mainEntrancePos) <= thresholdB)
                {
                    return true;
                }
            }

            //Check if near counter at company bulding
            if (RoundManager.Instance.currentLevel.sceneName == "CompanyBuilding")
            {
                Vector3 counterPos = targetScrapPosition = GameObject.FindObjectOfType<DepositItemsDesk>().triggerCollider.transform.position;
                if (Vector3.Distance(position, counterPos) <= thresholdB)
                {
                    return true;
                }
            }

            return false;
        }

        [ServerRpc]
        public void RemoveFromItemServerRpc()
        {
            if (HasCustomScripts)
                OnRemoveFromItemServerRpc.Invoke();
            if (targetItem != null)
            {
                //LethalMin.Logger.LogInfo($"Removing {targetItem.name} from item");
                HasFoundCaryTarget = false;
                HasFoundGrabTarget = false;
                targetItem.RemovePikminServerRpc(NetworkObjectId);
                if (AssingedGoToNode != (null, -1))
                {
                    if (AssingedGoToNode.Item1.HasValue)
                        targetItem.ReleaseGoToPosition(AssingedGoToNode.Item1.Value, AssingedGoToNode.Item2);
                    AssingedGoToNode = (null, -1);
                }
                SetTargetItemServerRpc(); // This will clear the target item
            }
            if (HasCustomScripts)
                OnRemoveFromItemServerRpcEnd.Invoke();
        }


        [ServerRpc]
        private void SetTargetItemServerRpc(NetworkObjectReference itemRef)
        {
            if (HasCustomScripts)
                OnSetTargetItemServerRpc.Invoke();
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Attempting to set target item with NetworkObjectReference");

            if (itemRef.TryGet(out NetworkObject itemObject))
            {
                targetItem = itemObject.GetComponent<PikminItem>();
                if (targetItem != null)
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Target item set on server: {targetItem.name}");
                }
                else
                {
                    LethalMin.Logger.LogError($"Found NetworkObject but it doesn't have a PikminItem component: {itemObject.name}");
                    PikminItemsExclusion.Add(targetItem.gameObject);
                    targetItem = null;
                }
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to resolve NetworkObjectReference");
                PikminItemsExclusion.Add(targetItem.gameObject);
                targetItem = null;
            }

            SetTargetItemClientRpc(itemRef);
            if (HasCustomScripts)
                OnSetTargetItemServerRpcEnd.Invoke();
        }

        [ServerRpc]
        private void SetTargetItemServerRpc()
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Clearing target item on server");
            targetItem = null;
            SetTargetItemClientRpc();
        }

        [ClientRpc]
        private void SetTargetItemClientRpc(NetworkObjectReference itemRef)
        {
            if (HasCustomScripts)
                OnSetTargetItemClientRpc.Invoke();
            if (itemRef.TryGet(out NetworkObject itemObject))
            {
                targetItem = itemObject.GetComponent<PikminItem>();
                if (targetItem != null)
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Target item set on client: {targetItem.name}");
                }
                else
                {
                    LethalMin.Logger.LogError($"Found NetworkObject but it doesn't have a PikminItem component: {itemObject.name}");
                    targetItem = null;
                }
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to resolve NetworkObjectReference on client");
                targetItem = null;
            }

            // If the Pikmin is no longer working on an item, switch to Idle state
            if (targetItem == null && currentBehaviourStateIndex == (int)PState.Working)
            {
                agent.updateRotation = false;
                SwitchToBehaviourClientRpc((int)PState.Idle);
            }
            if (HasCustomScripts)
                OnSetTargetItemClientRpcEnd.Invoke();
        }

        [ClientRpc]
        private void SetTargetItemClientRpc()
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Clearing target item on client");
            targetItem = null;

            // If the Pikmin is no longer working on an item, switch to Idle state
            if (currentBehaviourStateIndex == (int)PState.Working)
            {
                agent.updateRotation = false;
                SwitchToBehaviourClientRpc((int)PState.Idle);
            }
        }
        private void DetectNearbyItems()
        {
            if (!IsServer) return; // Only run this on the server
            if (HasCustomScripts)
                OnDetectNearbyItems.Invoke();
            try
            {
                GameObject nearestItem = null;
                if (!IsCallingCLOSFI)
                    nearestItem = CheckLineOfSightForItem();
                if (nearestItem != null)
                {
                    PikminItem newTargetItem = nearestItem.GetComponent<PikminItem>();
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found item: {nearestItem.name}");
                    if (newTargetItem != null)
                    {
                        NetworkObject itemNetworkObject = newTargetItem.GetComponent<NetworkObject>();
                        if (itemNetworkObject != null)
                        {
                            if (LethalMin.DebugMode)
                                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Setting target item: {newTargetItem.name}");
                            SetTargetItemServerRpc(new NetworkObjectReference(itemNetworkObject));
                            agent.updateRotation = InitalUR;
                            SwitchToBehaviourClientRpc((int)PState.Working);
                            ReqeustItemNoticeClientRpc();
                            if (LethalMin.DebugMode)
                                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Switched to working");
                        }
                        else
                        {
                            LethalMin.Logger.LogError($"Found item doesn't have NetworkObject component: {nearestItem.name}");
                            PikminItemsInMap.Remove(nearestItem);
                        }
                    }
                    else
                    {
                        LethalMin.Logger.LogError($"Found item doesn't have PikminItem component: {nearestItem.name}");
                        PikminItemsInMap.Remove(nearestItem);
                    }
                }
            }
            catch (Exception ex)
            {
                LethalMin.Logger.LogError(ex);
            }
            if (HasCustomScripts)
                OnDetectNearbyItemsEnd.Invoke();
        }
        private void MoveTowardsItem()
        {
            if (!IsServer) return; // Only run this on the server
            if (HasCustomScripts)
                OnMoveTowardsItem.Invoke();
            if (targetItem != null)
            {
                if (AssingedGoToNode == (null, -1))
                {
                    // Only get a new GoTo position if we don't have one assigned
                    AssingedGoToNode = targetItem.GetNearestAvailableGoToPosition(transform.position);
                }
                if (targetItem.Root.isHeld || targetItem.Root.isHeldByEnemy && targetItem.PikminOnItem < targetItem.PikminNeedOnItem
            || !LethalMin.HundradOnOne && targetItem.PikminOnItem >= targetItem.MaxPikminOnItem ||
            Vector3.Distance(transform.position, targetItem.transform.position) >= LethalMin.ChaseRange)
                {
                    ReleaseItemServerRpc();
                    return;
                }

                if (!IsOnItem || targetItem.PikminOnItem < targetItem.MaxPikminOnItem && targetItem.ManEater)
                {
                    agent.SetDestination(targetItem.GetGoToPos(AssingedGoToNode));
                    //if(LethalMin.DebugMode)
                    //LethalMin.Logger.LogInfo("Set Dest Called on line 1535");
                }

                if (HasArrivedAtDestonation(1f, targetItem.GetGoToPos(AssingedGoToNode)) && !IsOnItem)
                {
                    if (IsNearDestination(targetItem.transform.position))
                    {
                        ReleaseItemServerRpc();
                        return;
                    }
                    // Pikmin has reached the GoTo point, grab the item
                    GrabItem(targetItem);
                }
                else
                {
                    // Move towards the assigned GoTo point
                }
            }
            else
            {
                // If target item is null, switch back to Idle state
                AssingedGoToNode = (null, -1);
                agent.updateRotation = false;
                SwitchToBehaviourClientRpc((int)PState.Idle);
            }
            if (HasCustomScripts)
                OnMoveTowardsItemEnd.Invoke();
        }
        private GameObject CheckLineOfSightForItem()
        {
            if (HasCustomScripts)
                OnCheckLineOfSightForItem.Invoke();
            IsCallingCLOSFI = true;
            GameObject closestItem = null;
            float closestDistance = float.MaxValue;

            //if(LethalMin.DebugMode)
            //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Starting CheckLineOfSightForItem. Items to check: {PikminItemsInMap.Count}");

            for (int i = 0; i < PikminItemsInMap.Count; i++)
            {
                GameObject item = PikminItemsInMap[i];
                if (item == null)
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Removing null item at index {i}");
                    PikminItemsInMap.RemoveAt(i--);
                    continue;
                }

                Vector3 directionToItem = item.transform.position - eye.position;
                float distanceToItem = directionToItem.sqrMagnitude;

                //if(LethalMin.DebugMode)
                // LethalMin.Logger.LogInfo($"({uniqueDebugId}) Checking item: {item.name}, Distance: {Mathf.Sqrt(distanceToItem)}");

                // Early distance check
                if (distanceToItem > itemDetectionRange * itemDetectionRange)
                {
                    //if(LethalMin.DebugMode)
                    //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Item {item.name} is too far. Skipping.");
                    continue;
                }

                PikminItem pikminItem = item.GetComponent<PikminItem>();
                if (pikminItem?.Root == null)
                {
                    //if(LethalMin.DebugMode)
                    //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Item {item.name} has no PikminItem or Root. Skipping.");
                    continue;
                }

                if (LethalMin.GetParsedPickupBlacklist().Contains(pikminItem.Root.itemProperties.itemName))
                {
                    //if(LethalMin.DebugMode)
                    //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Item {item.name} is blacklisted. Skipping.");
                    continue;
                }

                if (pikminItem.Root.isInShipRoom && RoundManager.Instance.currentLevel.sceneName != "CompanyBuilding")
                {
                    //if(LethalMin.DebugMode)
                    //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Item {item.name} is in ship room. Skipping.");
                    continue;
                }

                if (pikminItem.Root.isHeld || pikminItem.Root.isHeldByEnemy)
                {
                    //if(LethalMin.DebugMode)
                    //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Item {item.name} is held. Skipping.");
                    continue;
                }

                if (IsNearDestination(item.transform.position))
                {
                    //if(LethalMin.DebugMode)
                    //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Item {item.name} is near destination. Skipping.");
                    continue;
                }

                //if(LethalMin.DebugMode)
                //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Performing linecast for item: {item.name}");
                if (Physics.Linecast(eye.position, item.transform.position, out RaycastHit hit, LethalMin.Instance.PikminColideable))
                {
                    if (hit.collider != null && !hit.collider.gameObject.name.Contains("Anomaly") &&
                        (hit.collider.gameObject.transform.parent == null || !hit.collider.gameObject.transform.parent.name.Contains("Anomaly")))
                    {
                        //if(LethalMin.DebugMode)
                        //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Linecast blocked for item {item.name} by: {hit.collider.gameObject.name}");
                        continue;
                    }
                }

                if (distanceToItem < closestDistance)
                {
                    closestItem = item;
                    closestDistance = distanceToItem;
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) New closest item: {item.name} at distance {Mathf.Sqrt(distanceToItem)}");
                }
            }

            if (closestItem != null)
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Final closest item: {closestItem.name} at distance {Mathf.Sqrt(closestDistance)}");
            }
            else
            {
                //if(LethalMin.DebugMode)
                //LethalMin.Logger.LogInfo($"({uniqueDebugId}) No valid items found in line of sight");
            }
            IsCallingCLOSFI = false;
            if (HasCustomScripts)
                OnCheckLineOfSightForItemEnd.Invoke();
            return closestItem;
        }

        /// <summary>
        ///  unused :(
        /// </summary>
        /// <returns></returns>
        public static LayerMask CameraLayerMask()
        {
            // Get the culling mask of the gameplay camera
            Camera gameplayCamera = StartOfRound.Instance.localPlayerController.gameplayCamera; // Assuming the main camera is the gameplay camera
            LayerMask gameplayCameraCullingMask = gameplayCamera.cullingMask;

            // Create a mask with all layers
            LayerMask allLayers = ~0;

            // Invert the gameplay camera's culling mask to get the excluded layers
            LayerMask excludedLayers = ~gameplayCameraCullingMask;

            // Create our custom mask by removing the excluded layers from all layers
            LayerMask customMask = allLayers & ~excludedLayers;

            return customMask;
        }

        private void GrabItem(PikminItem item)
        {
            if (targetItem != null)
            {
                item.AddPikminServerRpc(NetworkObjectId);
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Called grab item");
            }
        }
        private void HandleItemCarrying()
        {
            if (targetItem == null || targetItem.Root == null)
            {
                // Handle the case where targetItem or its Root is null
                //LethalMin.Logger.LogWarning("targetItem or its Root is null in HandleItemCarrying");
                ReleaseItemServerRpc();
                return;
            }

            if (targetItem.PikminOnItem >= targetItem.PikminNeedOnItem)
            {
                PikminAI firstPikmin = targetItem.PikminOnItemList[0];

                stk = Cstate.Carrying;
                if (firstPikmin == this)
                {
                    // Calculate the adjusted speed
                    float baseSpeed = PlantSpeeds[GrowStage];
                    float adjustedSpeed;

                    int regularPikminCount = targetItem.PikminOnItemList.Count - targetItem.PurplesOnItemList.Count;
                    int purplePikminCount = targetItem.PurplesOnItemList.Count;
                    float totalCarryingPower = regularPikminCount + (purplePikminCount * targetItem.PurpleMultiplier);

                    if (targetItem.PikminNeedOnItem == 1)
                    {
                        // For items that only require one pikmin
                        float baseCarrySpeed = baseSpeed * 0.8f; // Slightly slower speed
                        float pikminRatio = regularPikminCount / targetItem.PikminNeedOnItem;
                        float speedBonus = pikminRatio - 1;

                        // Apply purple Pikmin slowdown effect
                        float purpleSlowdownFactor = 1f - (purplePikminCount * 0.1f); // 10% slowdown per purple Pikmin

                        adjustedSpeed = baseCarrySpeed * (1 + speedBonus) * purpleSlowdownFactor;

                        adjustedSpeed = Mathf.Max(adjustedSpeed, baseSpeed * 0.5f);
                        // Cap the speed at 200% of the base speed
                        if (LethalMin.CapCarrySpeed)
                            adjustedSpeed = Mathf.Min(adjustedSpeed, baseSpeed * 2f);
                    }
                    else
                    {
                        // For items that require more than one pikmin
                        float baseCarrySpeed = baseSpeed * 0.5f; // Start at half speed
                        float pikminRatio = regularPikminCount / targetItem.PikminNeedOnItem;
                        float speedBonus = pikminRatio - 1;

                        // Apply purple Pikmin slowdown effect
                        float purpleSlowdownFactor = 1f - (purplePikminCount * 0.1f); // 10% slowdown per purple Pikmin

                        adjustedSpeed = baseCarrySpeed * (1 + speedBonus) * purpleSlowdownFactor;

                        adjustedSpeed = Mathf.Max(adjustedSpeed, baseSpeed * 0.5f);
                        // Cap the speed at 200% of the base speed
                        if (LethalMin.CapCarrySpeed)
                            adjustedSpeed = Mathf.Min(adjustedSpeed, baseSpeed * 2f);
                    }

                    // Set the agent's speed
                    agent.speed = adjustedSpeed;

                    // This is the first Pikmin, responsible for moving the item
                    if (!HasFoundCaryTarget)
                    {
                        GetItemTarget();
                    }
                    if (CarryingItemTo == "CaveDweller")
                    {
                        if (previousLeader == null)
                            targetScrapPosition = StartOfRound.Instance.localPlayerController.transform.position;
                        if (previousLeader != null)
                            targetScrapPosition = previousLeader.transform.position;
                    }
                    if (CarryingItemTo == "Car")
                    {
                        if (TargetCarPos != null)
                            targetScrapPosition = TargetCarPos.transform.position;
                    }

                    agent.SetDestination(targetScrapPosition);
                    agent.updateRotation = false;
                    transform.rotation = targetCarryRotaion;

                    // Move the item along with the Pikmin
                    targetItem.Root.transform.position = transform.position;

                    if (isOutside && RoundManager.Instance.currentLevel.sceneName != "CompanyBuilding")
                    {
                        if (IsInShip || IsInCar)
                        {
                            InShipBuffer += Time.deltaTime;
                        }
                        if (HasArrivedAtDestonation(0.5f, targetScrapPosition) && CarryingItemTo != "CaveDweller"
                        || (IsInShip && InShipBuffer >= PminType.DropItemInShipBuffer)
                        || HasArrivedAtDestonation(0, targetScrapPosition) && CarryingItemTo == "CaveDweller" && LethalMin.DontFormidOak)
                        {
                            if (LethalMin.DebugMode)
                                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Arrived at Ship");
                            InShipBuffer = 0;
                            targetItem.HandleArrivedClientRpc();
                            targetItem.RemoveAllPikminAndUnparent();
                            CallingHandleItemCarying = false;
                        }
                    }
                    else if (isOutside && RoundManager.Instance.currentLevel.sceneName == "CompanyBuilding")
                    {
                        if (HasArrivedAtDestonation(2.5f, targetScrapPosition) && CarryingItemTo != "CaveDweller"
                        || HasArrivedAtDestonation(0f, targetScrapPosition) && CarryingItemTo == "CaveDweller" && LethalMin.DontFormidOak)
                        {
                            if (LethalMin.DebugMode)
                                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Arrived at Counter");
                            targetItem.HandleArrivedClientRpc();
                            targetItem.RemoveAllPikminAndUnparent();
                            CallingHandleItemCarying = false;
                        }
                    }
                    else if (!MineshaftInside || MineshaftInside && !IsOnLowerLevel)
                    {
                        if (HasArrivedAtDestonation(4, targetScrapPosition) && CarryingItemTo != "CaveDweller"
                        || HasArrivedAtDestonation(0, targetScrapPosition) && CarryingItemTo == "CaveDweller" && LethalMin.DontFormidOak)
                        {
                            if (LethalMin.DebugMode)
                                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Arrived at MainEntrance");
                            targetItem.HandleArrivedClientRpc();
                            targetItem.RemoveAllPikminAndUnparent();
                            CallingHandleItemCarying = false;
                        }
                    }
                    else
                    {
                        if (HasArrivedAtDestonation(0.5f, targetScrapPosition) && CarryingItemTo != "CaveDweller"
                        || HasArrivedAtDestonation(0f, targetScrapPosition) && CarryingItemTo == "CaveDweller" && LethalMin.DontFormidOak)
                        {
                            if (LethalMin.DebugMode)
                                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Arrived at Elevator");
                            targetItem.HandleArrivedClientRpc();
                            targetItem.RemoveAllPikminAndUnparent();
                            CallingHandleItemCarying = false;
                        }
                    }
                }
                else
                {
                    //if(LethalMin.DebugMode)
                    //LethalMin.Logger.LogInfo(agent.updatePosition);
                    if (AssingedGoToNode == (null, -1))
                    {
                        AssingedGoToNode = targetItem.GetNearestAvailableGoToPosition(transform.position);
                    }
                    // This is not the first Pikmin, maintain relative position
                    agent.updatePosition = false;
                    agent.updateRotation = false;
                    transform.rotation = CalculateYAxisRotation(targetItem.Root.transform.position);
                    Vector3 pos = targetItem.GetGoToPos(AssingedGoToNode, uniqueDebugId);
                    transform.position = new Vector3(pos.x, pos.y - HoldPos.localPosition.y, pos.z);
                    rb.position = new Vector3(pos.x, pos.y - HoldPos.localPosition.y, pos.z);
                }
            }
            else
            {
                stk = Cstate.Waiting;
            }
        }

        private void GetItemTarget()
        {
            if (targetItem.GetComponentInParent<CaveDwellerPhysicsProp>() != null)
            {
                if (previousLeader == null)
                    targetScrapPosition = StartOfRound.Instance.localPlayerController.transform.position;
                if (previousLeader != null)
                    targetScrapPosition = previousLeader.transform.position;
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found Player Pos at {targetScrapPosition}");
                CarryingItemTo = "CaveDweller";
                HasFoundCaryTarget = true;
                return;
            }
            if (isOutside && RoundManager.Instance.currentLevel.sceneName != "CompanyBuilding")
            {
                if (LethalMin.GoToCar)
                {
                    GetNearestCar();
                    Vector3 shippos = StartOfRound.Instance.insideShipPositions[UnityEngine.Random.Range(0, StartOfRound.Instance.insideShipPositions.Length)].position;
                    //Check if the car is closer than the ship
                    if (TargetCar != null && TargetCarPos != null &&
                    Vector3.Distance(transform.position, TargetCar.transform.position) < Vector3.Distance(transform.position, shippos)
                    && TargetCar.backDoorOpen)
                    {
                        targetScrapPosition = TargetCarPos.transform.position;
                        if (LethalMin.DebugMode)
                            LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found Car Pos at {targetScrapPosition}");
                        CarryingItemTo = "Car";
                        HasFoundCaryTarget = true;
                        return;
                    }
                }
                targetScrapPosition = StartOfRound.Instance.insideShipPositions[UnityEngine.Random.Range(0, StartOfRound.Instance.insideShipPositions.Length)].position;
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found Ship Pos at {targetScrapPosition}");
                CarryingItemTo = "Ship";
                HasFoundCaryTarget = true;
            }
            else if (isOutside && RoundManager.Instance.currentLevel.sceneName == "CompanyBuilding")
            {
                targetScrapPosition = GameObject.FindObjectOfType<DepositItemsDesk>().triggerCollider.transform.position;
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found Counter Pos at {targetScrapPosition}");
                CarryingItemTo = "Counter";
                HasFoundCaryTarget = true;
            }
            else if (!MineshaftInside)
            {
                Vector3 mainEntrancePosition = RoundManager.FindMainEntrancePosition();
                Vector3 fireExitPosition = FindNearestFireExit();

                if (LethalMin.OnlyExit && fireExitPosition != Vector3.zero
                || !LethalMin.OnlyMain && fireExitPosition != Vector3.zero
                && Vector3.Distance(transform.position, fireExitPosition) < Vector3.Distance(transform.position, mainEntrancePosition))
                {
                    targetScrapPosition = fireExitPosition;
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found Fire Exit Pos at {targetScrapPosition}");
                    CarryingItemTo = "FireExit";
                }
                else
                {
                    targetScrapPosition = GetPositionInFrontOfMainEntrance(mainEntrancePosition);
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found Main Entrance Pos at {targetScrapPosition}");
                    CarryingItemTo = "Main";
                }
                HasFoundCaryTarget = true;
            }
            else if (MineshaftInside && IsOnUpperLevel)
            {
                Vector3 mainEntrancePosition = RoundManager.FindMainEntrancePosition();
                targetScrapPosition = GetPositionInFrontOfMainEntrance(mainEntrancePosition);
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found Main Entrance Pos at {targetScrapPosition}");
                CarryingItemTo = "Main(Mineshaft)";
            }
            else
            {
                Vector3 fireExitPosition = FindNearestFireExit();
                targetScrapPosition = RoundManager.Instance.currentMineshaftElevator.elevatorInsidePoint.position;
                if (LethalMin.OnlyExit && fireExitPosition != Vector3.zero
                || !LethalMin.OnlyMain && Vector3.Distance(transform.position, fireExitPosition) <
                 Vector3.Distance(transform.position, RoundManager.Instance.currentMineshaftElevator.elevatorInsidePoint.position))
                {
                    targetScrapPosition = fireExitPosition;
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found Fire Exit Pos at {targetScrapPosition}");
                    CarryingItemTo = "FireExit (Mineshaft)";
                }
                else
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found Elevator Pos at {targetScrapPosition}");
                    CarryingItemTo = "Elevator";
                    HasFoundCaryTarget = true;
                }
            }
            targetCarryRotaion = CalculateYAxisRotation(targetItem.Root.transform.position);
        }
        private bool IsPathPossible(Vector3 destination)
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
            {
                return path.status == NavMeshPathStatus.PathComplete;
            }
            return false;
        }
        private Quaternion CalculateYAxisRotation(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0; // This ensures we only rotate on the Y axis
            if (direction != Vector3.zero)
            {
                return Quaternion.LookRotation(direction);
            }
            return transform.rotation;
        }
        private Vector3 FindNearestFireExit()
        {
            EntranceTeleport[] allEntrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            EntranceTeleport nearestFireExit = null;
            List<EntranceTeleport> allExits = new List<EntranceTeleport>();
            float nearestDistance = float.MaxValue;

            foreach (EntranceTeleport entrance in allEntrances)
            {
                // Check if it's a fire exit (entrance ID is not 0)
                if (entrance.entranceId != 0)
                {
                    allExits.Add(entrance);
                }
            }

            if (allExits.Count == 0)
            {
                LethalMin.Logger.LogWarning("No fire exit found. Returning root.");
                return Vector3.zero;
            }

            nearestFireExit = allExits
                .OrderBy(exit => Vector3.Distance(transform.position, exit.transform.position))
                .First();

            if (nearestFireExit != null)
            {
                return nearestFireExit.transform.position;
            }
            else
            {
                LethalMin.Logger.LogWarning("No fire exit found. Returning root.");
                return Vector3.zero;
            }
        }
        private void GetNearestCar()
        {
            if (PikminManager._currentCars == null || PikminManager._currentCars.Length == 0)
            {
                return;
            }
            VehicleController NextCar = null;
            if (PikminManager._currentCars.Length == 1)
            {
                NextCar = PikminManager._currentCars[0];
            }
            else
            {
                NextCar = PikminManager._currentCars
                    .Where(x => x != null)
                    .OrderBy(x => Vector3.Distance(x.transform.position, transform.position))
                    .FirstOrDefault();
            }

            if (NextCar != null)
            {
                SetVehicleController(NextCar);
            }
            else
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogWarning($"Pikmin {uniqueDebugId}: No valid car found");
            }
        }
        public void SetVehicleController(VehicleController NextCar)
        {
            if (TargetCar != null && TargetCar == NextCar)
            {
                return;
            }
            if (TargetCarPos != null)
                Destroy(TargetCarPos.gameObject);

            TargetCar = NextCar;
            TargetCarNavMeshSurface = TargetCar.transform.Find("InsideTruckNavBounds").GetComponent<BoxCollider>();
            TargetCarPos = new GameObject($"Pos for {uniqueDebugId} (car)").transform;
            TargetCarPos.transform.position = GetRandomPositionInBounds(TargetCarNavMeshSurface);
            TargetCarPos.transform.SetParent(TargetCarNavMeshSurface.transform);
            TempObjects.Add(TargetCarPos.gameObject);
            if (LethalMin.DebugMode)
            {
                LethalMin.Logger.LogInfo($"Pikmin {uniqueDebugId} is now following a car NC {NextCar} TC{TargetCar.name}" +
                $" TCS {TargetCarNavMeshSurface.name} at position {TargetCarPos.transform.position}");
            }
        }

        private Vector3 GetPositionInFrontOfMainEntrance(Vector3 mainEntrancePosition)
        {
            // Assuming the main entrance faces outward (negative Z direction)
            Vector3 entranceForward = -Vector3.forward;

            // Move the position 2 units in front of the entrance
            Vector3 positionInFront = mainEntrancePosition + entranceForward * 2f;

            // Perform a raycast to ensure the position is on the ground
            RaycastHit hit;
            if (Physics.Raycast(positionInFront + Vector3.up * 5f, Vector3.down, out hit, 10f, LayerMask.GetMask("Ground")))
            {
                return hit.point;
            }

            // If raycast fails, return the original position
            return positionInFront;
        }

        [ServerRpc]
        public void ReleaseItemServerRpc(bool SwitchStates = true)
        {
            if (targetItem != null)
            {
                if (HasFoundGrabTarget)
                {
                    targetItem.ReleaseGoToPosition(AssingedGoToNode.Item1.Value, AssingedGoToNode.Item2);
                    HasFoundGrabTarget = false;
                }
            }
            agent.Warp(rb.transform.position);
            agent.updatePosition = InitialUP;
            agent.updateRotation = InitalUR;
            IsOnItem = false;
            CanGrabItems = false;
            HasPlayedLift = false;
            ReleaseItemClientRpc();
            agent.SetDestination(transform.position);
            agent.updateRotation = false;
            RemoveFromItemServerRpc();
            stk = Cstate.none;
            if (SwitchStates)
                SwitchToBehaviourClientRpc((int)PState.Idle);
        }
        [ClientRpc]
        public void ReleaseItemClientRpc()
        {
            agent.speed = PlantSpeeds[GrowStage];
            IsOnItem = false;
            HasPlayedLift = false;
            CanGrabItems = false;
        }

        #endregion





        #region Aiming and Throwing
        [ClientRpc]
        public void ThrowPikminClientRpc(Vector3 startPos, Vector3 throwForward, float force, Quaternion throwRotation)
        {
            rb.position = startPos;
            rb.rotation = throwRotation;
            transform.rotation = throwRotation;
            agent.updatePosition = false;
            agent.updateRotation = false;
            isHeldOrThrown = true;
            IsThrown = true;
            isHeld = false;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.WakeUp();
            Pcollider.enabled = true;
            if (IsServer && currentLeader != null) { currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject)); }
            EnemyAttacking = null;
            targetPlayer = null;
            currentLeader = null;
            currentLeaderNetworkObject = null;
            SetTriggerClientRpc("Thrown");

            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.excludeLayers = ~LethalMin.Instance.PikminColideable;

            // Use the provided throwForward vector for the throw direction
            Vector3 throwDirection = throwForward.normalized;
            rb.AddForce(throwDirection * force, ForceMode.Impulse);

            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"Force: {throwDirection * force}");
            SwitchToBehaviourClientRpc((int)PState.Airborn);
            ReqeustThrowSFXClientRpc();
        }

        [ClientRpc]
        public void ReqeustThrowSFXClientRpc()
        {
            if (!HasInitalized) { return; }
            LocalVoice.Stop();
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.ThrowSFX, true, false);
                PlaySFX(ref LethalMin.PlayerThrowSound, true, true);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.ThrowVoiceLine, true, false);
                PlaySFX(ref PminType.soundPack.ThrowSFX, true, true);
            }
        }

        [ClientRpc]
        public void ReqeustPlayExitOnionClientRpc()
        {
            if (!HasInitalized) { return; }
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.ExitOnionSFX);
            }
            else
            {

                PlaySFX(ref PminType.soundPack.ExitOnionVoiceLine);
            }
        }

        [ClientRpc]
        public void ReqeustHurtSFXClientRpc()
        {
            if (!HasInitalized) { return; }
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.DeadSFX);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.HurtVoiceLine);
            }
        }

        [ClientRpc]
        public void ReqeustItemNoticeClientRpc()
        {
            if (!HasInitalized) { return; }
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.ItemNoticeSFX);
            }
            else
            {

                PlaySFX(ref PminType.soundPack.ItemNoticeVoiceLine);
            }
        }

        [ClientRpc]
        public void ReqeustAttackAndHitSFXClientRpc()
        {
            if (!HasInitalized) { return; }
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.AttackSFX);
                PlaySFX(ref LethalMin.RealHitSFX);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.AttackVoiceLine);
                PlaySFX(ref PminType.soundPack.HitSFX);
            }
        }
        [ClientRpc]
        public void ReqeustAttackSFXClientRpc()
        {
            if (!HasInitalized) { return; }
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.AttackSFX);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.AttackVoiceLine);
            }
        }

        [ClientRpc]
        public void ReqeustHitSFXClientRpc()
        {
            if (!HasInitalized) { return; }
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.RealHitSFX);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.HitSFX);
            }
        }


        [ClientRpc]
        public void ReqeustLiftSFXClientRpc()
        {
            if (!HasInitalized) { return; }

            float baseVolume = 0.45f;
            float scalingFactor = 1f;

            if (targetItem != null && targetItem.PikminOnItemList.Count > 1)
            {
                // Calculate a scaling factor based on the number of Pikmin
                scalingFactor = Mathf.Sqrt(1f / targetItem.PikminOnItemList.Count);
            }

            // Apply the scaling factor to the base volume
            float adjustedVolume = baseVolume * scalingFactor;

            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.LiftSFX, true, false, adjustedVolume);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.LiftVoiceLine, true, false, adjustedVolume);
            }
        }

        [ClientRpc]
        public void ReqeustLostSFXClientRpc()
        {
            if (!HasInitalized) { return; }
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.LostSFX);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.LostVoiceLine);
            }
        }

        [ClientRpc]
        public void ReqeustPlayEnterOnionClientRpc()
        {
            if (!HasInitalized) { return; }
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.EnterOnionSFX);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.EnterOnionVoiceLine);
            }
        }

        [ClientRpc]
        public void ReqeustPlayBornSoundClientRpc()
        {
            if (!HasInitalized) { return; }
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.BornSFX);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.BornVoiceLine);
            }
        }

        [ClientRpc]
        public void ReqeustYaySFXClientRpc()
        {
            if (!HasInitalized) { return; }
            LocalVoice.Stop();
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.YaySFX, true, false, 0.4f);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.YayVoiceLine, true, false, 0.4f);
            }
        }

        [ClientRpc]
        public void ReqeustHoldSFXClientRpc()
        {
            if (!HasInitalized) { return; }
            if (PminType.soundPack == null)
            {
                PlaySFX(ref LethalMin.HoldSFX);
            }
            else
            {
                PlaySFX(ref PminType.soundPack.HoldVoiceLine);
            }
        }


        public void PlaySFX(ref AudioClip[] Clips, bool PlayOnWalkie = true, bool AudibleToEnemies = false, float Volume = 1)
        {
            if (!HasInitalized) { return; }
            if (Clips == null || Clips != null && Clips.Length == 0) { return; }
            if (Clips.Length == 1)
            {
                LocalVoice.PlayOneShot(Clips[0]);
            }
            else if (Clips.Length > 1)
            {
                AudioClip ChosenClip = Clips[enemyRandom.Next(0, Clips.Length)];
                LocalVoice.PlayOneShot(ChosenClip, Volume);
                if (PlayOnWalkie)
                {
                    WalkieTalkie.TransmitOneShotAudio(LocalVoice, ChosenClip, Volume);
                }
                if (AudibleToEnemies && !LethalMin.LethaDogs1Value && IsServer)
                {
                    RoundManager.Instance.PlayAudibleNoise(transform.position, 10, Volume, 0, IsInShip && StartOfRound.Instance.hangarDoorsClosed);
                }
            }
        }

        public void PlaySFX(ref AudioClip[] Clips, int Index, bool PlayOnWalkie = true, bool AudibleToEnemies = false, float Volume = 1)
        {
            if (Clips == null || Clips != null && Clips.Length == 0) { return; }
            if (Clips.Length == 1)
            {
                LocalVoice.PlayOneShot(Clips[0]);
            }
            else if (Clips.Length > 1)
            {
                AudioClip ChosenClip = Clips[Index];
                LocalVoice.PlayOneShot(ChosenClip, Volume);
                if (PlayOnWalkie)
                {
                    WalkieTalkie.TransmitOneShotAudio(LocalVoice, ChosenClip, Volume);
                }
                if (AudibleToEnemies && !LethalMin.LethaDogs1Value && IsServer)
                {
                    RoundManager.Instance.PlayAudibleNoise(transform.position, 10, Volume, 0, IsInShip && StartOfRound.Instance.hangarDoorsClosed);
                }
            }
        }

        public void PlaySFX(ref AudioClip Clip, bool PlayOnWalkie = true, bool AudibleToEnemies = false, float Volume = 1)
        {
            if (Clip != null)
            {
                LocalVoice.PlayOneShot(Clip, Volume);
                if (PlayOnWalkie)
                {
                    WalkieTalkie.TransmitOneShotAudio(LocalVoice, Clip, Volume);
                }
                if (AudibleToEnemies && !LethalMin.LethaDogs1Value && IsServer)
                {
                    RoundManager.Instance.PlayAudibleNoise(transform.position, 10, Volume, 0, IsInShip && StartOfRound.Instance.hangarDoorsClosed);
                }
            }
        }


        [ClientRpc]
        public void SetComponentsForAimingClientRpc(bool isAiming)
        {
            LethalMin.Logger.LogInfo(isAiming);
            if (isAiming)
            {
                isHeldOrThrown = true;
                isHeld = true;
                agent.updateRotation = false;
                agent.updatePosition = false;
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.Sleep();
                Pcollider.enabled = false;
                LethalMin.Logger.LogInfo("Set Trigger");
                SetTriggerClientRpc("Aim");
                PlayAnimClientRpc("Hold");
                ReqeustHoldSFXClientRpc();
            }
            else
            {
                isHeldOrThrown = true;
                IsThrown = true;
                isHeld = false;
                agent.updateRotation = false;
                agent.updatePosition = false;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.detectCollisions = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.None;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.WakeUp();
                Pcollider.enabled = true;
            }
        }

        [ClientRpc]
        public void StartRotationEasingClientRpc()
        {
            StartCoroutine(EaseXRotationToZero());
        }

        private IEnumerator EaseXRotationToZero()
        {
            float duration = 0.5f; // Adjust this value to change how long the easing takes
            float elapsedTime = 0f;
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0f, startRotation.eulerAngles.y, startRotation.eulerAngles.z);

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            transform.rotation = targetRotation;
        }


        #endregion





        #region Growing Logic
        private void HandleGrowing()
        {
            if (!IsServer) return; // Only run this on the server

            GrowTimer -= Time.deltaTime;

            if (GrowTimer <= 0f && GrowStage < 2) // Assuming max growth stage is 2
            {
                GrowTimer = GROW_TIME; // Reset the timer
                IncrementGrowStageServerRpc();
            }
        }

        [ServerRpc]
        private void IncrementGrowStageServerRpc()
        {
            GrowStage++;
            if (GrowStage > PminType.GrowthStagePaths.Length - 1)
            {
                GrowStage = PminType.GrowthStagePaths.Length - 1;
            }
            UpdateGrowStageClientRpc(GrowStage);
        }

        [ClientRpc]
        public void UpdateGrowStageClientRpc(int newGrowStage)
        {
            GrowStage = newGrowStage;
            UpdateGrowthVisuals();
        }

        private void UpdateGrowthVisuals()
        {
            if (PlantSpeeds.Length > Plants.Length)
            {
                PlantSpeeds = PlantSpeeds.Take(Plants.Length).ToArray();
            }
            if (PlantSpeeds.Length < Plants.Length)
            {
                float[] Temp = new float[Plants.Length];
                for (int i = 0; i < PlantSpeeds.Length; i++)
                {
                    Temp[i] = PlantSpeeds[i];
                }
                PlantSpeeds = Temp;
            }
            agent.speed = PlantSpeeds[GrowStage];
            foreach (var item in Plants)
            {
                item.SetActive(Plants.IndexOf(item) == GrowStage);
            }
        }
        #endregion





        #region Collision and Landing
        public bool CantAttack(EnemyAI Enemy)
        {
            return
            KncockedBack || Enemy == null || Enemy != null && Enemy.isEnemyDead
            // Check for enemy's conditions to attack
            || Enemy != null && Enemy.enemyType == enemyType
            //Check if enemy is a pikmin
            || LethalMin.PassiveToManEater && Enemy.enemyType.enemyName == LethalMin.ManeaterName && Enemy.GetComponent<CaveDwellerAI>().babyContainer.activeSelf
            //Maneater Checks
            || !Enemy.enemyType.canDie || !PminType.CanLatchOnToEnemies || LethalMin.IsDependencyLoaded("LethalMon") && LETHALMON_CantAttack(Enemy)
            //Blacklist Checks
            || LethalMin.GetParsedAttackBlacklist().Contains(Enemy.enemyType.enemyName);
        }
        public bool LETHALMON_CantAttack(EnemyAI Enemy)
        {
            TamedEnemyBehaviour enemy = Enemy.GetComponent<TamedEnemyBehaviour>();
            if (enemy == null || enemy != null && !enemy.IsTamed)
            {
                return false;
            }
            return
            (!LethalMin.FriendlyFireOmon && enemy.ownerPlayer == previousLeader?.Controller) ||
            (!LethalMin.FriendlyFireOmon && enemy.ownerPlayer == previousLeader?.Controller) ||
            (!LethalMin.FriendlyFireMon && enemy.IsTamed);
        }
        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy)
        {
            if (HasCustomScripts)
                OnOnCollideWithEnemy.Invoke();
            if (!IsServer || CantAttack(collidedEnemy))
            {
                return;
            }
            if (!rb.isKinematic && EnemyAttacking == null && currentBehaviourStateIndex == (int)PState.Airborn)
            {
                ProcessEnemyCollisionServerRpc(new NetworkObjectReference(collidedEnemy.NetworkObject), transform.position);
            }
            else if (SnapToPos == null && currentBehaviourStateIndex == (int)PState.Attacking)
            {
                if (AttackBuffer >= 0f)
                {
                    AttackBuffer -= Time.deltaTime;
                }
                else
                {
                    SetTriggerClientRpc("Attack");
                    AttackBuffer = 0.7f;
                }
            }
            if (HasCustomScripts)
                OnOnCollideWithEnemyEnd.Invoke();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsServer)
            {
                if (!KncockedBack && currentBehaviourStateIndex != (int)PState.Attacking)
                {
                    ProcessCollisionServerRpc(collision.gameObject.name);
                }
            }
        }
        private void OnCollisionStay(Collision collision)
        {
            if (IsServer)
            {
                if (KncockedBack && ShouldDoKBcheck)
                {
                    KnockBackBuffer += Time.deltaTime;
                    if (KnockBackBuffer >= 0.5f)
                    {
                        ProcessCollisionServerRpc(collision.gameObject.name);
                        ShouldDoKBcheck = false;
                    }
                }
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            if (IsServer)
            {
                KnockBackBuffer = 0f;
            }
        }


        [ClientRpc]
        private void LandPikminClientRpc()
        {
            if (HasCustomScripts)
                OnLandPikminClientRpc.Invoke();
            // Resets Physics
            rb.isKinematic = true;
            rb.useGravity = false;
            Pcollider.enabled = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.detectCollisions = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.Sleep();
            rb.excludeLayers = 0;

            // Resets Agent
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Landed at {rb.transform.position}");
            agent.Warp(rb.transform.position);
            agent.updateRotation = InitalUR;
            agent.updatePosition = InitialUP;

            isHeldOrThrown = false;
            IsThrown = false;
            isHeld = false;

            if (!KncockedBack)
            {
                SetTriggerClientRpc("Land");
                //PlayAnimClientRpc("Idle");
            }
            else
            {
                SetTriggerClientRpc("Land");
                //PlayAnimClientRpc("Lay");
            }
            if (IsServer)
            {
                KncockedBack = false;
                KnockBackBuffer = 0;
            }

            agent.updateRotation = false;
            if (!IsDrowing)
            {
                SwitchToBehaviourClientRpc((int)PState.Idle);
            }
            else
            {
                SwitchToBehaviourClientRpc((int)PState.Drowning);
            }
            if (HasCustomScripts)
                OnLandPikminEnd.Invoke();
        }
        [ServerRpc]
        private void ProcessCollisionServerRpc(string collisionObjectName)
        {
            if (rb.isKinematic == false)
            {
                InternalAirbornTimer = LethalMin.FallTimerValue;
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Landed on: {collisionObjectName}");
                IdleTimer = enemyRandom.Next(1, 5) - 0.5f;
                SetIntClientRpc("IdelInt", 0);
                LandPikminClientRpc();
                if (FinnaBeDed)
                    IsDying = true;
            }
        }

        #endregion





        #region Death and Damage 😭
        public void KnockbackOnEnemy(Vector3 knockbackForce, bool IsLethal, bool KillOnLanding, float DeathTimer = 0, float Fource = 0)
        {
            if (LethalMin.StrudyMinValue)
            {
                return;
            }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"{uniqueDebugId} has {KnockBackResistance} resistance");
            if (currentBehaviourStateIndex == (int)PState.Attacking && SnapToPos != null)
            {
                if (KnockBackResistance <= 0)
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"{uniqueDebugId} fell off");
                    ApplyKnockbackServerRpc(knockbackForce, false, false, 0);
                }
                else
                {
                    KnockBackResistance -= enemyRandom.Next(2) + (int)Fource;
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"{uniqueDebugId} now has {KnockBackResistance} resistance");
                }
            }
            else
            {
                ApplyKnockbackServerRpc(knockbackForce, IsLethal && !LethalMin.InvinciMinValue, KillOnLanding, DeathTimer);
            }
        }
        public void KnockbackOnEnemy(Vector3 knockbackForce, Transform SnapfToPos, bool IsLethal, float DeathTimer = 0, bool ecape = false)
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"{uniqueDebugId} has {KnockBackResistance} resistance");

            if (currentBehaviourStateIndex == (int)PState.Attacking && SnapToPos != null)
            {
                if (KnockBackResistance <= 0)
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"{uniqueDebugId} fell off");
                    ApplyKnockbackServerRpc(knockbackForce, false, false, 0);
                }
                else
                {
                    KnockBackResistance -= enemyRandom.Next(15);
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"{uniqueDebugId} now has {KnockBackResistance} resistance");
                }
            }
            else
            {
                SnapPikminToPosition(SnapfToPos, ecape, IsLethal && !LethalMin.InvinciMinValue, DeathTimer, false);
            }
        }


        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            if (Invincible) { return; }
            if (KncockedBack) { return; }
            if (!LethalMin.FriendlyFire && playerWhoHit != null && playerWhoHit == currentLeader?.Controller)
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"{uniqueDebugId} was hit by their leader: {playerWhoHit?.name}");
                return;
            }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"{uniqueDebugId} was hit!");
            if (SnapToPos != null)
            {
                KnockbackOnEnemy(new Vector3(force, force, force), false, false, 3);
            }
            else
            {
                SnapPikminToPosition(null, false, true, 1f);
            }
        }
        public override void HitFromExplosion(float distance)
        {
            base.HitFromExplosion(distance);
            if (Invincible || PminType.IsresistantToExsplosions) { return; }
            ApplyKnockbackServerRpc(new Vector3(-distance, -distance, -distance), true, false, 3);
        }
        public void UnSnapPikmin(bool DestorySnapToPos = false, bool UsePhysics = false, bool ResetToInitial = true)
        {
            if (DestorySnapToPos)
                Destroy(SnapToPos.gameObject);
            rotationOffset = null;
            PositionOffset = null;
            SnapToPos = null;
            SnapTopPosition = null;
            if (ResetToInitial)
            {
                if (UsePhysics)
                {
                    ApplyPhysicsServerRpc();
                }
                else
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    Pcollider.enabled = false;
                    rb.detectCollisions = false;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                    rb.interpolation = RigidbodyInterpolation.None;
                    rb.Sleep();
                    agent.updatePosition = InitialUP;
                    agent.updateRotation = InitalUR;
                }
            }
        }
        public void SnapPikminToPosition(Transform Positio, bool Escapeable, bool IsLethal, float DeathTimer, bool Randomize = false)
        {
            if (currentBehaviourStateIndex == (int)PState.Leaveing || KncockedBack || !IsServer) { return; }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"({uniqueDebugId}) Snaping to position ({Positio?.gameObject.name})");
            if (Positio == null)
            {
                LethalMin.Logger.LogWarning($"({uniqueDebugId}) Tried to snap to a null position");
                Transform temppos = new GameObject($"Pos for {uniqueDebugId}").transform;
                temppos.transform.position = transform.position;
                Positio = temppos;
                TempObjects.Add(temppos.gameObject);
            };
            if (IsDrowing) { StopDrowingClientRpc(); }
            if (targetItem != null)
            {
                ReleaseItemServerRpc();
            }
            if (currentLeader != null)
            {
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
            }
            targetPlayer = null;
            currentLeader = null;
            currentLeaderNetworkObject = null;


            // Disable rigidbody and collider
            rb.isKinematic = true;
            rb.useGravity = false;
            Pcollider.enabled = false;
            rb.detectCollisions = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.Sleep();

            // Disable agent updates
            agent.updatePosition = false;
            agent.updateRotation = false;

            SnapPikminToPositionClientRpc(Escapeable, IsLethal && !LethalMin.InvinciMinValue, DeathTimer);

            SnapToPos = Positio;
            if (Randomize)
            {
                rotationOffset = new Vector3(enemyRandom.Next(-360, 360), enemyRandom.Next(-360, 360), enemyRandom.Next(-360, 360));
                PositionOffset = new Vector3(enemyRandom.Next(-5, 5) / 10, enemyRandom.Next(-5, 5) / 10, enemyRandom.Next(-5, 5) / 10);
                RandomizedSnapTo = true;
            }
            else
            {
                RandomizedSnapTo = false;
            }
        }

        [ClientRpc]
        public void SnapPikminToPositionClientRpc(bool Escapeable, bool IsLethal, float DeathTimer)
        {
            if (currentBehaviourStateIndex == (int)PState.Leaveing) { return; }

            if (IsLethal)
            {
                IsDying = true;
            }
            if (IsLethal && !Escapeable)
            {
                SetTriggerClientRpc("Lay");
            }
            if (!Escapeable)
                ReqeustHurtSFXClientRpc();
            CannotEscape = !Escapeable && !LethalMin.InvinciMinValue;
            DedTimer = DeathTimer;
        }

        [ServerRpc]
        public void ApplyKnockbackServerRpc(Vector3 knockbackForce, bool IsLethal, bool KillOnLanding, float DeathTimer = 0)
        {
            if (currentBehaviourStateIndex == (int)PState.Leaveing) { return; }
            if (LethalMin.StrudyMinValue && !IsLethal)
            {
                return;
            }
            else if (LethalMin.StrudyMinValue && !LethalMin.InvinciMinValue)
            {
                KillEnemyOnOwnerClient();
                return;
            }
            if (IsDrowing) { StopDrowingClientRpc(); }
            // Release any held item
            if (targetItem != null)
            {
                ReleaseItemServerRpc();
            }

            // Enable rigidbody and collider
            rb.isKinematic = false;
            rb.useGravity = true;
            Pcollider.enabled = true;
            rb.detectCollisions = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.WakeUp();

            // Disable agent updates
            agent.updatePosition = false;
            agent.updateRotation = false;

            if (IsLethal && !KillOnLanding && !LethalMin.InvinciMinValue)
            {
                IsDying = true;
            }
            if (IsLethal && KillOnLanding && !LethalMin.InvinciMinValue)
            {
                FinnaBeDed = true;
            }
            DedTimer = DeathTimer;

            // Apply the knockback force
            rb.AddForce(knockbackForce, ForceMode.Impulse);
            if (currentLeader != null)
            {
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
            }
            if (EnemyDamager != null)
            {
                EnemyDamager.LatchOffServerRpc(this);
                EnemyDamager = null;
            }
            targetPlayer = null;
            currentLeader = null;
            currentLeaderNetworkObject = null;
            agent.updateRotation = false;
            EnemyAttacking = null;
            UnSnapPikmin(false, false, false);
            // Set the state to Airborn
            SwitchToBehaviourClientRpc((int)PState.Airborn);

            // Trigger the knockback animation
            UpdateAnimBoolClientRpc("Ded", IsLethal);
            SetTriggerClientRpc("Knockback");
            TriggerKnockbackAnimationClientRpc(IsLethal && !LethalMin.InvinciMinValue);
        }

        [ServerRpc]
        public void ApplyPhysicsServerRpc()
        {
            if (currentBehaviourStateIndex == (int)PState.Leaveing) { return; }

            if (IsDrowing) { StopDrowingClientRpc(); }
            // Release any held item
            if (targetItem != null)
            {
                ReleaseItemServerRpc();
            }

            // Enable rigidbody and collider
            rb.isKinematic = false;
            rb.useGravity = true;
            Pcollider.enabled = true;
            rb.detectCollisions = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.WakeUp();

            // Disable agent updates
            agent.updatePosition = false;
            agent.updateRotation = false;

            if (currentLeader != null)
            {
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
            }

            if (EnemyDamager != null)
            {
                EnemyDamager.LatchOffServerRpc(this);
                EnemyDamager = null;
            }
            targetPlayer = null;
            currentLeader = null;
            currentLeaderNetworkObject = null;
            agent.updateRotation = false;
            KncockedBack = false;
            EnemyAttacking = null;
            rotationOffset = null;
            PositionOffset = null;
            SnapToPos = null;
            SnapTopPosition = null;
            SwitchToBehaviourClientRpc((int)PState.Airborn);
        }

        [ClientRpc]
        private void TriggerKnockbackAnimationClientRpc(bool IsLethal)
        {
            KncockedBack = true;
            ShouldDoKBcheck = true;
            CanGrabItems = false;
            CanAttack = false;
        }
        public bool DeathBuffer;
        public override void KillEnemy(bool destroy = false)
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"Killed called for {uniqueDebugId}");
            if (Invincible) { return; }
            if (LethalMin.InvinciMinValue) { return; }
            if (!IsDying && !DeathBuffer && !destroy)
            {
                SnapPikminToPosition(null, false, true, 1.5f);
            }
            if (!DeathBuffer && !destroy)
            {
                return;
            }
            PikminManager.Instance.IncrementPikminKilled();
            base.KillEnemy();
            agent.speed = 0f;
            LocalSFX.Stop();
            LocalSFX.Stop();
            if (targetItem != null)
            {
                ReleaseItemServerRpc();
            }
            if (currentLeader != null)
            {
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
            }
            targetPlayer = null;
            currentLeader = null;
            currentLeaderNetworkObject = null;
            agent.updateRotation = false;
            SpawnGhostClientRpc();
            if (PikminManager.Instance != null)
            {
                PikminManager.Instance.DespawnPikminClientRpc(new NetworkObjectReference(NetworkObject));
            }
            else
            {
                if (IsServer)
                    NetworkObject.Despawn(true);
            }
        }

        [ClientRpc]
        public void SpawnGhostClientRpc()
        {
            GameObject G = Instantiate(Ghost, transform.position, Quaternion.identity);
            if (G.GetComponent<PminGhost>() == null)
            {
                G.AddComponent<PminGhost>();
            }
            G.GetComponent<PminGhost>().pmintype = PminType;
        }
        #endregion





        #region Onion handleing and returning 

        public void CheckForOnion()
        {
            if (!HasInitalized)
            {
                return;
            }
            Onion[] onions = PikminManager._currentOnions;
            if (onions.Length == 0)
            {
                TargetOnion = null;
                return;
            }
            if (TargetOnion != null)
            {
                return;
            }
            if (PminType.TargetOnion == null)
            {
                return;
            }

            foreach (Onion onion in onions)
            {
                if (onion.type == PminType.TargetOnion
                || onion.type.TypesCanHold.ToList().Contains(PminType))
                {
                    TargetOnion = onion;
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"{name} Found {onion.type.TypeName} Onion");
                    break;
                }
            }
            if (TargetOnion == null)
            {
                onions = FindObjectsOfType<Onion>();

                foreach (Onion onion in onions)
                {
                    if (onion.type == PminType.TargetOnion
                    || onion.type.TypesCanHold.ToList().Contains(PminType))
                    {
                        TargetOnion = onion;
                        if (LethalMin.DebugMode)
                            LethalMin.Logger.LogInfo($"{name} Found {onion.type.TypeName} Onion");
                        break;
                    }
                }
            }
        }
        public void CheckForOnion(Onion[] onions)
        {
            if (!HasInitalized)
            {
                return;
            }
            if (onions.Length == 0)
            {
                TargetOnion = null;
                return;
            }
            if (TargetOnion != null)
            {
                return;
            }
            if (PminType.TargetOnion == null)
            {
                return;
            }

            foreach (Onion onion in onions)
            {
                if (onion.type == PminType.TargetOnion
                || onion.type.TypesCanHold.ToList().Contains(PminType))
                {
                    TargetOnion = onion;
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"{name} Found {onion.type.TypeName} Onion");
                    break;
                }
            }
        }
        public IEnumerator DestoryMin()
        {
            yield return new WaitForSeconds(0.9f);
            DestoryPikmin();
        }
        public void DestoryPikmin()
        {
            agent.speed = 0f;
            LocalSFX.Stop();
            LocalSFX.Stop();
            if (targetItem != null)
            {
                ReleaseItemServerRpc();
            }
            if (currentLeader != null)
            {
                currentLeader.RemovePikminServerRpc(new NetworkObjectReference(NetworkObject));
            }
            targetPlayer = null;
            currentLeader = null;
            currentLeaderNetworkObject = null;
            agent.updateRotation = false;
            if (PikminManager.Instance != null)
            {
                PikminManager.Instance.DespawnPikminClientRpc(new NetworkObjectReference(NetworkObject));
            }
            else
            {
                if (IsServer)
                    NetworkObject.Despawn(true);
            }
        }
        #endregion





        #region Attacking
        public static List<GameObject> NonPikminEnemies = new List<GameObject>();

        private void DetectNearbyEnemies()
        {
            if (!IsServer) return;
            if (!HasInitalized) { return; }
            if (targetItem != null && !LethalMin.PrioitizeAttacking) { return; }
            if (NonPikminEnemies == null || NonPikminEnemies.Count == 0)
            {
                //LethalMin.Logger.LogWarning($"({uniqueDebugId}) NonPikminEnemies list is null or empty.");
                return;
            }

            GameObject nearestEnemyObject = CheckLineOfSight(NonPikminEnemies, width: 360, range: 12, proximityAwareness: -1);
            if (nearestEnemyObject == null)
            {
                //LethalMin.Logger.LogWarning($"({uniqueDebugId}) No enemy found in line of sight.");
                return;
            }

            EnemyAI nearestEnemy = nearestEnemyObject.GetComponent<EnemyAI>();
            if (nearestEnemy == null)
            {
                //LethalMin.Logger.LogWarning($"({uniqueDebugId}) Nearest enemy does not have an EnemyAI component.");
                return;
            }

            if (CantAttack(nearestEnemy))
            {
                //LethalMin.Logger.LogWarning($"({uniqueDebugId}) Nearest enemy is not attackable.");
                return;
            }

            //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Found Enemy {nearestEnemy.name}");

            PikminDamager damager = nearestEnemy.GetComponent<PikminDamager>();

            if (damager != null)
            {
                if (targetItem != null)
                    ReleaseItemServerRpc(false);
                EnemyDamager = damager;
                EnemyAttacking = nearestEnemy;
                SwitchToBehaviourClientRpc((int)PState.Attacking);
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Enemy Targeting {nearestEnemy.name}");
            }
            else
            {
                //LethalMin.Logger.LogInfo($"({uniqueDebugId}) Enemy {nearestEnemy.name} does not have a pikmin damager!!!");
            }
        }

        [ServerRpc]
        private void ProcessEnemyCollisionServerRpc(NetworkObjectReference enemyRef, Vector3 collisionPoint)
        {
            if (rb.isKinematic == false && enemyRef.TryGet(out NetworkObject obj))
            {
                EnemyAI enemy = obj.GetComponent<EnemyAI>();
                EnemyAttacking = enemy;
                EnemyDamager = enemy.GetComponent<PikminDamager>();
                if (EnemyDamager == null) { return; }
                EnemyDamager.LatchOnServerRpc(this);
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"({uniqueDebugId}) Landed on enemy: {enemy.enemyType.enemyName}");
                SkinnedMeshRenderer enemyMesh = enemy.skinnedMeshRenderers[0];
                Vector3 latchPosition;

                if (enemyMesh != null)
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Landed on enemy mesh {enemyMesh.gameObject.name}");
                    latchPosition = GetNearestVertexPosition(enemyMesh, collisionPoint);
                }
                else
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"({uniqueDebugId}) Landed on enemy hitbox");
                    latchPosition = collisionPoint;
                }

                GameObject goToPoint = new GameObject($"PikminLatchPoint_{uniqueDebugId}");
                goToPoint.transform.position = latchPosition;
                goToPoint.transform.rotation = enemy.transform.rotation;
                goToPoint.transform.SetParent(enemy.transform, true);
                TempObjects.Add(goToPoint);
                KnockBackResistance = enemyRandom.Next(PminType.MinKnockBackResistance, PminType.MaxKnockBackResistance);
                SnapPikminToPosition(goToPoint.transform, true, false, 0f);
                LocalAnim.ResetTrigger("Land");
                SetTriggerClientRpc("Land");
                LatchOntoEnemyClientRpc(new NetworkObjectReference(enemy.NetworkObject));
            }
        }

        [ClientRpc]
        private void LatchOntoEnemyClientRpc(NetworkObjectReference enemyRef)
        {
            if (HasCustomScripts)
                OnLatchOntoEnemyClientRpc.Invoke();
            EnemyAI enemy = null;
            IsThrown = false;
            if (enemyRef.TryGet(out NetworkObject obj))
            {
                enemy = obj.GetComponent<EnemyAI>();
            }
            if (enemy != null)
            {

                SwitchToBehaviourClientRpc((int)PState.Attacking);
            }
            else
            {
                LethalMin.Logger.LogWarning($"({uniqueDebugId}) Could not find enemy: {enemy.enemyType.enemyName}");
            }

            if (HasCustomScripts)
                OnLatchOntoEnemyEnd.Invoke();
        }
        private Vector3 GetNearestVertexPosition(SkinnedMeshRenderer skinnedMeshRenderer, Vector3 targetPosition)
        {
            Mesh mesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(mesh);
            Vector3[] vertices = mesh.vertices;

            Vector3 nearestVertex = vertices[0];
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldSpaceVertex = skinnedMeshRenderer.transform.TransformPoint(vertices[i]);
                float distance = Vector3.Distance(worldSpaceVertex, targetPosition);

                if (distance < nearestDistance)
                {
                    nearestVertex = worldSpaceVertex;
                    nearestDistance = distance;
                }
            }

            return nearestVertex;
        }
        public Vector3 GetRandomVertexPosition(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            Mesh mesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(mesh);
            Vector3[] vertices = mesh.vertices;
            int randomIndex = UnityEngine.Random.Range(0, vertices.Length);
            return skinnedMeshRenderer.transform.TransformPoint(vertices[randomIndex]);
        }
        #endregion
    }
}