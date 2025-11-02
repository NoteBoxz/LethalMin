using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;
using LethalMin.Pikmin;
using LethalMin.Utils;
using UnityEngine.AI;
using GameNetcodeStuff;
using Dissonance;

namespace LethalMin
{
    /// <summary>
    /// Pintent acts more like a secondary state machine for the Pikmin
    /// </summary>
    public enum Pintent
    {
        // Movement States
        Idle,
        RunTowards,
        StandingAttack,
        Attack,
        Waiting,
        Carry,
        LatchedOn,
        Leave,

        // Holding/Throwing States
        BeingHeld,
        Thrown,

        // Dying/Death States
        Dead,
        Knockedback,
        MoveableStuck,
        Panicing,
        Stuck,
        Fall
    }

    public enum PfollowMode
    {
        New,
        LegacyBehind,
        LegacyFollow,
    }

    public struct OverridePikminPosition
    {
        public OverridePikminPosition(string ID, Vector3 position, bool useTimer = true, float timeToOverride = 2.5f, float distanceThreshold = -1)
        {
            this.ID = ID;
            this.useTimer = useTimer;
            this.position = position;
            this.timeToOverride = timeToOverride;
            this.distanceThreshold = distanceThreshold;
        }
        public bool UpdateTimer()
        {
            if (!useTimer)
            {
                return false;
            }
            timeToOverride -= Time.deltaTime;
            return timeToOverride <= 0;
        }
        public bool CheckDistance(Vector3 PositionToCheck)
        {
            if (distanceThreshold < 0)
            {
                return false;
            }
            return Vector3.Distance(position, PositionToCheck) <= distanceThreshold;
        }
        public bool ShouldRemove(PikminAI aI)
        {
            return UpdateTimer() || CheckDistance(aI.transform.position);
        }
        public string ID;
        public Vector3 position;
        public bool useTimer = false;
        public float timeToOverride = 2.5f;
        public float distanceThreshold = -1;
    }

    public class PikminAI : EnemyAI, IGenerationSwitchable
    {
        [Header("Pikmin AI")]
        public PikminType pikminType = null!;
        public PikminNetworkTransfrom transform2 = null!;
        public PikminSoundPack CurrentSoundPack = null!;
        public PikminAnimatorController animController = null!;
        public Onion TargetOnion = null!;
        public PikminManager Pmanager = null!;
        public Pintent CurrentIntention;
        public Pintent PreviousIntention;
        public PikminTask? CurrentTask = null;
        public ProjectileProperties ProjectileProps;
        public Leader? leader;
        public Leader? previousLeader;
        public PikminSpawnProps SpawnProps = default;
        public bool HasBeenInitalized = false;
        public bool IsWildPikmin = true;
        public string DebugID = "<ID Not Set>";
        public Rigidbody rb = null!;
        public Collider Pcollider = null!;
        [SerializeField] protected Transform modelContainer = null!;
        public Transform HoldPosition = null!;
        public AudioSource DrowningAudioSource = null!;
        public int CurrentGrowthStage = 0;
        public int MaxGrowthStage = 2;
        public float GrowTimer = 100f;
        public Transform SproutTop = null!;
        public PikminItem? TargetItem = null!;
        public BoolValue TargetItemPoint = null!;
        public int CurrentCarryStrength = 1;
        public Transform? AgentLookTarget = null;
        public System.Random enemyRandom = null!;
        public int randomSeed = 0;
        public string BirthDate = "<BirthDate Not set>";
        public bool IsInShip;
        public bool IsOnShip;
        public PikminLatchTrigger? CurrentLatchTrigger = null;
        public PikminEnemy? TargetEnemy = null;
        public Vector3 LatchTriggerOffset;
        public Leader? LeaderWhistling;
        public PikminNoticeZone? LeaderWhistlingZone;
        public List<GameObject> TempObjects = new List<GameObject>();
        public Transform? DeathSnapToPos = null;
        public Quaternion RandomRotaion;
        public Coroutine attackRoutine = null!;
        private Coroutine panicSoundRoutine = null!;
        public List<string> DeathTimerAffecters = new List<string>();
        public PikminLinkAnimation CurrentLinkAnim = null!;
        public PikminVehicleController? CurrentVehicle = null!;
        public Transform? CurrentVehiclePoint = null!;
        public ITrajectoryModifier? trajectoryModifier = null!;
        public bool Invincible = false;
        public float DeathTimer = 1;
        public int CurrentCollisionMode = 1;
        public Vector3 PanicPosition;
        float PanicInterval = 10f;
        bool CanBeWhistledOutOfPanic;
        public float OverrideDelay = -1;
        public string CurrentPlayingKey = "";
        private static float maxAudioClusterDistance = 1.5f; // Maximum distance to consider for volume adjustment
        private static int maxAudioClusterPikmin = 10; // Maximum number of Pikmin before fully muting additional sounds
        public bool IsAirborn => CurrentIntention == Pintent.Thrown || CurrentIntention == Pintent.Knockedback || CurrentIntention == Pintent.Fall;
        public bool IsDeadOrDying => CurrentIntention == Pintent.Dead || isEnemyDead;
        public float timeSinceLastVehicleCheck = 0;
        public bool IsLeftBehind;
        public bool IsGoingToOnion;
        public bool IsDoingOnionAnimation;
        public string CurPanicAnim = "";
        public bool DontAddToOnion = false;
        public bool ForceSproutGlow = false;
        public bool Laying = false;
        public bool CanGetBackUp = false;
        public float LeaderAssesmentDelay = 0;
        public float LandBuffer = 0.5f;
        public float StuckEscapeTimer = 0;
        public OverridePikminPosition? OverrideIdlePosition = null;
        public OverridePikminPosition? OverrideFollowPosition = null;
        public Vector3? SpecialIdlePosition = null;
        public Quaternion? SpecialIdleRotation = null;
        public bool ShouldRun => CurrentIntention == Pintent.Attack || CurrentIntention == Pintent.Panicing || CurrentIntention == Pintent.RunTowards;
        PikminScanNodeProperties scanNodeProperties = null!;
        GameObject LatchRefPoint = null!;
        Vector3 LeavingPos = Vector3.zero;
        bool playLinkedAnimReversed = false;
        int targetOnionLinkIndex;
        float timeOffNavMesh = 4;
        float timeIdel = 0;
        float tmeFalling = 0;
        float timeLaying = 0;
        bool hitBeforeKillWasCalled = false;
        //This varible only exists because for some reason the agent's position gets offseted
        //when the pikmin has it's ownership changed while or before it's initalizeing.
        //I love unity :D
        public Vector3 StoredSpawnPosition = Vector3.zero;

        [HideInInspector]
        public bool UnselectableChanged = false;

        private bool _unselectable;
        public bool Unselectable
        {
            get => _unselectable;
            set
            {
                if (_unselectable != value)
                {
                    _unselectable = value;
                    UnselectableChanged = true;
                }
            }
        }
        public static int PikminSoundID = 0;
        bool wasInvisCheatOn;
        bool friednlyFire => leader == null ? LethalMin.FriendlyFire : leader.FriendlyFire.Value;
        public Coroutine? chargeRoutine = null;
        public const int IDLE = 0;
        public const int FOLLOW = 1;
        public const int WORK = 2;
        public const int PANIC = 3;
        public const int LEAVING = 4;
        public bool AddToRaisedOnInitAssign = false;





        #region Pikmin Initalizeation
        public void SpawnFromOnion(int Index, Onion onion)
        {
            Vector3 TheShadowRelm = new Vector3(transform.position.x + 999, transform.position.y + 999, transform.position.z + 999);

            if (!PikUtils.IsOutOfRange(onion.ClimbLinks, Index))
            {
                transform.position = TheShadowRelm;
                transform2.TeleportOnLocalClient(TheShadowRelm);
                SetPikminOnToAnimLink(onion.ClimbLinks[Index]);
            }
        }


        public override void Start()
        {
            base.Start();

            randomSeed = StartOfRound.Instance.randomMapSeed + thisEnemyIndex;
            enemyRandom = new System.Random(randomSeed);
            DebugID = $"_{PikUtils.GenerateRandomString(enemyRandom)}";
            BirthDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Pmanager = PikminManager.instance;
            IsWildPikmin = true;
            PikminSoundID = StartOfRound.Instance.randomMapSeed + LethalMin.RegisteredPikminTypes.Count;

            if (rb == null)
                rb = GetComponent<Rigidbody>();
            if (Pcollider == null)
                Pcollider = GetComponent<SphereCollider>();
            if (transform2 == null)
                transform2 = GetComponent<PikminNetworkTransfrom>();


            if (pikminType == null)
            {
                LethalMin.Logger.LogFatal($"{DebugID} has no Pikmin type! Make sure it is set in the TypeResover before enableing!!!");
                if (IsServer)
                {
                    NetworkObject.Despawn(true);
                }
                return;
            }

            GenerationManager.Instance.Register(this);

            InitalizePikminType(pikminType);

            //Do this to hide the enemy mesh until the spawn animation is played
            if (!string.IsNullOrEmpty(SpawnProps.SpawnAnimation))
                EnableEnemyMesh(false);

            GrowTimer = Random.Range(60, 600);
            GetGrowthObjects();
            SetGrowth(0);

            scanNodeProperties = GetComponentInChildren<PikminScanNodeProperties>();
            scanNodeProperties.headerText = $"{pikminType.PikminName}";
            scanNodeProperties.PiklopediaEntry = pikminType.PiklopediaEntry;

            Pmanager.AddPikminAI(this);

            if (!SpawnProps.Equals(default(PikminSpawnProps)))
            {
                isOutside = SpawnProps.IsOutside;

                Leader? leader = LethalMin.GetLeaderViaID(SpawnProps.PlayerID);

                if (!string.IsNullOrEmpty(SpawnProps.OverrideDebugID))
                {
                    DebugID = SpawnProps.OverrideDebugID;
                }
                if (!string.IsNullOrEmpty(SpawnProps.OverrideBirthDate))
                {
                    BirthDate = SpawnProps.OverrideBirthDate;
                }

                if (leader != null)
                {
                    if (leader.Controller.isPlayerControlled)
                    {
                        if (SpawnProps.AddToSpawnCount)
                            PikminManager.instance.EndOfGameStats.PikminRaised[leader] += 1;
                        AssignLeader(leader, true, false);
                    }
                    else
                    {
                        LethalMin.Logger.LogDebug($"{DebugID}: Leader ({leader.Controller.playerUsername}) is not controlled, not assigning");
                    }
                }

                if (SpawnProps.AddToSpawnCount)
                    PikminManager.instance.FiredStats.TotalPikminRaised += 1;

                if (SpawnProps.AddToSpawnCountForWild)
                    AddToRaisedOnInitAssign = true;

                if (SpawnProps.SpawnAnimationDelay > 0)
                {
                    StartCoroutine(PlayDelayedAnimation(SpawnProps.SpawnAnimation, SpawnProps.SpawnAnimationDelay));
                }
                else
                {
                    animController.PlayAnimation(SpawnProps.SpawnAnimation, 0);
                }

                if (SpawnProps.MovementBuffer > 0)
                {
                    StartCoroutine(DelayMovement(SpawnProps.MovementBuffer));
                }


                if (SpawnProps.SpawnSoundDelay > 0)
                {
                    StartCoroutine(PlayDelayedSound(SpawnProps.SpawnSound, SpawnProps.SpawnSoundDelay));
                }
                else
                {
                    if (CurrentSoundPack.LookUpDict.ContainsKey(SpawnProps.SpawnSound))
                        PlayAudioOnLocalClient(SpawnProps.SpawnSound, false, SpawnProps.OverrideVolume);
                }

                SetGrowth(SpawnProps.GrowthStage);

                EnableEnemyMesh(true);
            }

            LethalMin.Logger.LogDebug($"{DebugID} spawned with type {pikminType.PikminName} and ID: {DebugID} At: {transform.position} " +
             $"random seed: {randomSeed} index: {thisEnemyIndex}");
            if (IsOwner)
            {
                agent.Warp(StoredSpawnPosition);
            }

            gameObject.name = DebugID;
            HasBeenInitalized = true;
        }


        /// <summary>
        /// Initalizes the Pikmin type to the Pikmin AI (Should only be called once)
        /// </summary>
        /// <param name="type">Must not be null</param>
        public void InitalizePikminType(PikminType type)
        {
            if (HasBeenInitalized)
            {
                LethalMin.Logger.LogError($"{DebugID} already Initalized!");
                return;
            }

            pikminType = type;
            Instantiate(type.ModelPrefab, modelContainer);

            CurrentCarryStrength = type.CarryStrength;

            enemyHP = type.HP;

            CurrentSoundPack = type.SoundPack;

            ProjectileProps = new ProjectileProperties
            {
                pikminAI = this,
                mass = rb.mass,
                drag = rb.drag,
                throwForce = pikminType.ThrowForce // Add this to ProjectileProperties struct
            };

            SwitchGeneration(PikUtils.ConvertCfgGenerationToPikminGeneration(LethalMin.PikminModelGeneration.InternalValue));
            SwitchSoundGeneration(PikUtils.ConvertCfgGenerationToPikminGeneration(LethalMin.PikminSoundGeneration.InternalValue));

            transform.Find("MapDot (1)").GetComponent<Renderer>().material.color = pikminType.PikminPrimaryColor;

            //Set the Pikmin's random ID
            DebugID = $"{type.PikminName}{DebugID}";
            LethalMin.Logger.LogDebug($"InitalizedPikmin Type: {type.PikminName} with ID: {DebugID}");
        }


        /// <summary>
        /// Updates the Pikmin's generation based on the current generation
        /// </summary>
        public void SwitchGeneration(PikminGeneration generation)
        {
            if (LethalMin.RandomizeGenerationModels.InternalValue)
                generation = (PikminGeneration)Random.Range(0, System.Enum.GetValues(typeof(PikminGeneration)).Length);

            PikminModelRefernces modelRefernces = GetComponentInChildren<PikminModelRefernces>();
            bool hasFound = false;

            void applyGeneration(PikminModelGeneration gen)
            {
                creatureAnimator = gen.Animator;
                animController = gen.AnimatorController;
                skinnedMeshRenderers = gen.Model.GetComponentsInChildren<SkinnedMeshRenderer>().ToArray();
                meshRenderers = gen.Model.GetComponentsInChildren<MeshRenderer>().ToArray();
                SproutTop = gen.SproutTop.transform;
                if (gen.HeyPikminBigEyes)
                {
                    gen.HeyPikminBigEyes.SetActive(LethalMin.BigEyesEnabled.InternalValue);
                }
                if (gen.OverrideSFX != null)
                {
                    creatureSFX = gen.OverrideSFX;
                }
                if (gen.OverrideVoice != null)
                {
                    creatureVoice = gen.OverrideVoice;
                }
                PikminSproutFXManager fx = null!;
                if (!SproutTop.gameObject.TryGetComponent(out fx))
                {
                    fx = SproutTop.gameObject.AddComponent<PikminSproutFXManager>();
                }

                fx.minmin = this;
                fx.Top = SproutTop;
            }

            void useDefultGeneration()
            {
                modelRefernces.Model.SetActive(true);
                creatureAnimator = modelRefernces.Animator;
                animController = modelRefernces.AnimatorController;
                skinnedMeshRenderers = modelRefernces.Model.GetComponentsInChildren<SkinnedMeshRenderer>().ToArray();
                meshRenderers = modelRefernces.Model.GetComponentsInChildren<MeshRenderer>().ToArray();
                SproutTop = modelRefernces.SproutTop.transform;
                if (modelRefernces.HeyPikminBigEyes)
                {
                    modelRefernces.HeyPikminBigEyes.SetActive(LethalMin.BigEyesEnabled.InternalValue);
                }
                if (modelRefernces.OverrideSFX != null)
                {
                    creatureSFX = modelRefernces.OverrideSFX;
                }
                if (modelRefernces.OverrideVoice != null)
                {
                    creatureVoice = modelRefernces.OverrideVoice;
                }
                PikminSproutFXManager fx = null!;
                if (!SproutTop.gameObject.TryGetComponent(out fx))
                {
                    fx = SproutTop.gameObject.AddComponent<PikminSproutFXManager>();
                }

                fx.minmin = this;
                fx.Top = SproutTop;
            }

            foreach (PikminModelGeneration gen in modelRefernces.Generations)
            {
                if (!PikChecks.IsGenerationValid(gen))
                {
                    if (gen != null)
                        LethalMin.Logger.LogError($"Generation {gen.Generation} of type {pikminType.PikminName} is invaild for a Pikmin!");
                    continue;
                }
                gen.Model.SetActive(gen.Generation == generation);

                if (gen.Generation == generation)
                {
                    applyGeneration(gen);
                    hasFound = true;
                }
            }

            if (!hasFound)
            {
                LethalMin.Logger.LogWarning($"Failed to find a valid generation for {DebugID} Generation: {generation}");
                useDefultGeneration();
            }
        }

        public void SwitchSoundGeneration(PikminGeneration generation)
        {
            if (LethalMin.RandomizeGenerationModels.InternalValue)
                generation = (PikminGeneration)Random.Range(0, System.Enum.GetValues(typeof(PikminGeneration)).Length);


            CurrentSoundPack = pikminType.GetSoundPackByGeneration(generation);
        }

        public PikminData GetPikminData()
        {
            int TargetOnionID = -1;
            if (TargetOnion != null)
            {
                if (TargetOnion.fusedTypes.Count == 0)
                {
                    TargetOnionID = TargetOnion.onionType.OnionTypeID;
                }
                else
                {
                    foreach (OnionType type in TargetOnion.fusedTypes)
                    {
                        if (type.TypesCanHold.Contains(pikminType))
                        {
                            TargetOnionID = type.OnionTypeID;
                        }
                    }
                }
            }
            PikminData data = new PikminData(
                pikminType.PikminTypeID,
                TargetOnionID,
                CurrentGrowthStage,
                DebugID,
                BirthDate
            );
            return data;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            GenerationManager.Instance.Unregister(this);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            foreach (GameObject temp in TempObjects)
            {
                if (temp != null)
                {
                    Destroy(temp);
                }
            }

            if (IsGoingToOnion && !isEnemyDead && TargetOnion != null)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Despawned while going to onion! adding anyways...");
                TargetOnion.AddPikmin(this);
            }

            CallResetMethods(
                RemoveLeader: true,
                DropItem: true,
                RemoveEnemy: true,
                CollisionMode: -1,
                Unlatch: true,
                RemoveTask: true
            );
            Pmanager.RemovePikminAI(this);
        }
        #endregion






        #region Behavior Methods
        /// <summary>
        /// Runs every frame, should mostly be called on the owner side
        /// Should be ran on the client side when snapping to positions
        /// </summary>
        public override void Update()
        {
            ModifiedUpdate();

            UpdateLocalSnapping();

            UpdateIdleAnimation();

            if (CurrentTask != null)
            {
                CurrentTask.Update();
            }

            if (LethalMin.InviciblePikminCheat)
            {
                Invincible = true;
                wasInvisCheatOn = true;
            }
            else if (wasInvisCheatOn)
            {
                Invincible = false;
                wasInvisCheatOn = false;
            }

            if (IsOwner)
                CheckIfOnNavmesh();

            if (IsOwner && agent.enabled)
            {
                if (agent.velocity.magnitude <= 0.05f)
                {
                    timeIdel += Time.deltaTime;
                }
                else
                {
                    timeIdel = 0;
                }
            }

            scanNodeProperties.gameObject.SetActive(LethalMin.MakePikminScanable.InternalValue);

            //Falling timer
            if (IsOwner && IsAirborn)
            {
                tmeFalling += Time.deltaTime;
                if (tmeFalling >= LethalMin.TimeFallingFailSafe.InternalValue)
                {
                    Vector3 pos = FindPositionToGetIntoBounds();
                    LethalMin.Logger.LogWarning($"{DebugID}: Falling for too long! Auto Landing");
                    LandPikminServerRpc();
                    tmeFalling = 0;
                }
            }
            else
            {
                tmeFalling = 0;
            }


            ProjectileProps.direction = transform.forward;

            //Car check
            if (CurrentIntention == Pintent.Idle && Pmanager.Vehicles.Count > 0 && currentBehaviourStateIndex == FOLLOW && leader != null)
            {
                if (timeSinceLastVehicleCheck >= 0)
                {
                    timeSinceLastVehicleCheck -= Time.deltaTime;
                }
                else
                {
                    timeSinceLastVehicleCheck = 0.25f + enemyRandom.Next(-10, 10) * 0.005f;
                    CheckToSetVehicle();
                }
            }
            else if (CurrentVehicle != null)
            {
                LethalMin.Logger.LogDebug($"{DebugID}: Setting off vehicle auto");
                SetOffVehicle();
            }

            //Attack
            animController.IsAttacking = attackRoutine != null;

            //Panic
            if (currentBehaviourStateIndex == PANIC && IsOwner)
            {
                HandlePanicStateOnOwnerClientConstant();
            }

            // transform2.PositionThreshold = (float)Pmanager.PikminAIs.Count / (LethalMin.MaxPikmin.InternalValue * 10);
            // transform2.RotAngleThreshold = (float)Pmanager.PikminAIs.Count / LethalMin.MaxPikmin.InternalValue;

            //Anim
            if (IsOwner)
            {
                animController.IsMoving = agent.enabled && agent.velocity.magnitude > 0.1f;
            }
            else
            {
                animController.IsMoving = transform2.enabled && transform2.tempVelocity.magnitude > 0.1f;
            }

            animController.IsCarrying = CurrentIntention == Pintent.Carry;
            animController.IsLaying = Laying || IsDeadOrDying;

            //Death Timer
            if (DeathTimerAffecters.Count > 0)
            {
                DeathTimer -= Time.deltaTime * DeathTimerAffecters.Count;
                OverrideDelay = 0.1f;
                if (DeathTimer <= 0 && IsOwner)
                {
                    LethalMin.Logger.LogDebug($"{DebugID}: Death timer ran out!");
                    DeathTimer = 999999;
                    KillEnemyOnOwnerClient(true);
                    DeathTimerAffecters.Clear();
                }
            }

            //Only the server should update the growth stage
            if (IsServer && CurrentGrowthStage < MaxGrowthStage)
            {
                GrowTimer -= Time.deltaTime * pikminType.GrowSpeedMultiplier;
                if (GrowTimer <= 0)
                {
                    GrowTimer = Random.Range(60, 600);
                    SetGrowthClientRpc(CurrentGrowthStage + 1);
                }
            }

            //Asignment Timer
            if (LeaderAssesmentDelay >= 0)
            {
                LeaderAssesmentDelay -= Time.deltaTime;
            }

            //OverrideChecks
            if (OverrideIdlePosition != null && OverrideIdlePosition.Value.ShouldRemove(this))
            {
                OverrideIdlePosition = null;
            }
            if (OverrideFollowPosition != null && OverrideFollowPosition.Value.ShouldRemove(this))
            {
                OverrideFollowPosition = null;
            }

            //Laying Timer
            if (CanGetBackUp && Laying)
            {
                timeLaying -= Time.deltaTime;
                if (timeLaying <= 0)
                {
                    CanGetBackUp = false;
                    PlayAnimation(animController.AnimPack.EditorGetUpAnim);
                    PlayAudioOnLocalClient(PikminSoundPackSounds.GetUp);
                    CallResetMethods();
                    LethalMin.Logger.LogDebug($"{DebugID}: Pikmin got back up!");
                }
            }

            //Mostly used to make pikmin rotate to look at the item
            if (IsOwner && AgentLookTarget != null)
            {
                agent.updateRotation = false;
                Vector3 directionToTarget = AgentLookTarget.position - transform.position;
                directionToTarget.y = 0; // This ensures rotation only on Y axis

                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15 * Time.deltaTime);
                }
            }
            if (IsOwner && !agent.updateRotation && AgentLookTarget != null)
            {
                agent.updateRotation = true;
            }
        }

        public void ModifiedUpdate()
        {
            if (enemyType.isDaytimeEnemy && !daytimeEnemyLeaving)
            {
                CheckTimeOfDayToLeave();
            }
            if (stunnedIndefinitely <= 0)
            {
                if (stunNormalizedTimer >= 0f)
                {
                    stunNormalizedTimer -= Time.deltaTime / enemyType.stunTimeMultiplier;
                }
                else
                {
                    stunnedByPlayer = null;
                    if (postStunInvincibilityTimer >= 0f)
                    {
                        postStunInvincibilityTimer -= Time.deltaTime * 5f;
                    }
                }
            }
            if (!inSpecialAnimation && !ventAnimationFinished)
            {
                ventAnimationFinished = true;
                if (creatureAnimator != null)
                {
                    creatureAnimator.SetBool("inSpawningAnimation", value: false);
                }
            }
            if (!base.IsOwner)
            {
                if (currentSearch.inProgress)
                {
                    StopSearch(currentSearch);
                }
                timeSinceSpawn += Time.deltaTime;
                return;
            }
            if (isEnemyDead)
            {
                agent.enabled = false;
                return;
            }
            if (updateDestinationInterval >= 0f)
            {
                updateDestinationInterval -= Time.deltaTime;
            }
            else
            {
                DoAIInterval();
                updateDestinationInterval = AIIntervalTime + Random.Range(-0.015f, 0.015f);
            }
        }

        public void LateUpdate()
        {
            IsInShip = StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(transform.position);
            IsOnShip = StartOfRound.Instance.shipBounds.bounds.Contains(transform.position);
            scanNodeProperties.VisualNodeType = (int)LethalMin.PikminScanNodeColorType.InternalValue;

            Vector3 scale = new Vector3(LethalMin.PikminScale.InternalValue, LethalMin.PikminScale.InternalValue, LethalMin.PikminScale.InternalValue);
            transform.localScale = scale;
        }

        public virtual void PathToPosition(Vector3 position)
        {
            if (agent.enabled && (agent.isOnNavMesh || agent.isOnOffMeshLink))
            {
                agent.SetDestination(position);
            }
        }

        public virtual void PathToSelf(bool UseOverride = false)
        {
            if (UseOverride && OverrideIdlePosition != null)
            {
                agent.speed = pikminType.GetSpeed(CurrentGrowthStage, ShouldRun) * 2.5f;
                PathToPosition(OverrideIdlePosition.Value.position);
                return;
            }
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
        }

        /// <summary>
        /// An intervalled update method that runs every 0.2 seconds
        /// Called on the owner's side
        /// </summary>
        public override void DoAIInterval()
        {

            if (currentBehaviourStateIndex != WORK)
                agent.speed = pikminType.GetSpeed(CurrentGrowthStage, ShouldRun);

            // if (!PikChecks.IsPlayerConnected(OwnerClientId))
            // {
            //     LethalMin.Logger.LogError($"{DebugID}: Owner not connected, reverting ownership to server");
            //     ChangeOwnershipOfEnemy(NetworkManager.ServerClientId);
            //     agent.enabled = IsServer;
            // }

            switch (currentBehaviourStateIndex)
            {
                case IDLE:
                    HandleIdleStateOnOwnerClient();
                    break;
                case FOLLOW:
                    HandleFollowStateOnOwnerClient();
                    break;
                case WORK:
                    HandleWorkStateOnOwnerClient();
                    break;
                case PANIC:
                    HandlePanicStateOnOwnerClient();
                    break;
                case LEAVING:
                    HandleLeavingStateOnOwnerClient();
                    break;
            }
        }

        public virtual void CallResetMethods(
        bool RemoveLeader = true,
        bool DropItem = true,
        bool RemoveEnemy = true,
        int CollisionMode = 1,
        bool Unlatch = true,
        bool RemoveTask = true,
        bool RemoveOverridePositions = true,
        bool SetLayingFalse = true)
        {
            if (RemoveEnemy)
            {
                RemoveTargetEnemy();
            }
            if (DropItem)
            {
                DropItemOnLocalClient();
            }
            if (RemoveLeader)
            {
                this.RemoveLeader();
            }
            if (CurrentVehicle != null)
            {
                SetOffVehicle(false);
            }
            if (Unlatch)
            {
                TryUnlatchPikmin();
            }
            if (RemoveTask)
            {
                CurrentTask = null;
            }
            if (RemoveOverridePositions)
            {
                OverrideIdlePosition = null;
                OverrideFollowPosition = null;
            }
            if (SetLayingFalse)
            {
                Laying = false;
            }
            if (CollisionMode >= 0)
            {
                SetCollisionMode(CollisionMode);
            }
        }

        public virtual void HandleIdleStateOnOwnerClient()
        {
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.stoppingDistance = 0;
            PathToSelf(true);

            if (CurrentIntention != Pintent.Idle || Laying)
            {
                return;
            }
            if (!CurrentLatchTrigger && (!IsWildPikmin || IsWildPikmin && LethalMin.WildPikminCarry) && pikminType.CanCarryObjects)
            {
                PikminItem? itm = GetClosestPikminItem();
                if (itm != null)
                {
                    LethalMin.Logger.LogInfo($"{DebugID}: Found item: {itm.name}");
                    TargetItem = itm;
                    TargetItemPoint = itm.GetNearestGrabPosition(transform.position);
                    if (TargetItem && TargetItemPoint)
                    {
                        SetPikminToItemServerRpc(TargetItem.NetworkObject, TargetItem.GrabToPositions.IndexOf(TargetItemPoint));
                        SetPikminToItemLocalClient(TargetItem, TargetItem.GrabToPositions.IndexOf(TargetItemPoint));

                        PathToPosition(TargetItem.transform.position);
                        return;
                    }
                    else
                    {
                        TargetItem = null!;
                        TargetItemPoint = null!;
                    }
                }
            }
            if (!CurrentLatchTrigger && (!IsWildPikmin || IsWildPikmin && LethalMin.WildPikminAttack))
            {
                PikminEnemy? enm = GetClosestEnemy();
                if (enm != null)
                {
                    LethalMin.Logger.LogInfo($"{DebugID}: Found enemy: {enm.name}");
                    TargetEnemy = enm;
                    if (TargetEnemy)
                    {
                        SetPikminToEnemyServerRpc(TargetEnemy.NetworkObject);
                        SetPikminToEnemyLocalClient(TargetEnemy);

                        PathToPosition(TargetEnemy.transform.position);
                        return;
                    }
                    else
                    {
                        TargetEnemy = null!;
                    }
                }
            }
        }
        public virtual void HandleFollowStateOnOwnerClient()
        {
            // Move towards the leader
            if (PikChecks.IsLeaderInvalid(leader) || leader != null && leader.Controller.isPlayerDead)
            {
                if (leader == null)
                {
                    LethalMin.Logger.LogError($"{DebugID}: Leader is null when following");
                    RemoveLeaderServerRpc();
                    return;
                }
                if (leader.Controller.isPlayerDead)
                {
                    LethalMin.Logger.LogInfo($"{DebugID}: Leader is dead when following");
                }
                leader.AddToRemoveQueue(this);
                return;
            }

            if (TargetItem != null)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: TargetItem is not null while in follow state! Dropping...");
                DropItemServerRpc();
            }

            if (CurrentIntention == Pintent.BeingHeld || leader == null)
            {
                return;
            }
            if (OverrideFollowPosition != null)
            {
                PathToPosition(OverrideFollowPosition.Value.position);
                //LethalMin.Logger.LogInfo($"{DebugID}: Following overridden position: {OverrideFollowPosition.Value.position}");
                return;
            }
            if (leader.DirectPikminPath)
            {
                PathToPosition(leader.transform.position);
                return;
            }
            switch (LethalMin.PikminFollowMode.InternalValue)
            {
                case PfollowMode.New:

                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                    agent.stoppingDistance = 0f;
                    PathToPosition(leader.formManager.GetFormationPosition(this));
                    break;

                case PfollowMode.LegacyFollow:
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                    agent.stoppingDistance = 2;
                    PathToPosition(leader.transform.position);
                    break;

                case PfollowMode.LegacyBehind:
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                    agent.stoppingDistance = 2;
                    // Calculate position behind the target
                    Vector3 directionToAgent = (transform.position - leader.transform.position).normalized;
                    Vector3 targetBehindPosition = leader.transform.position + directionToAgent * agent.stoppingDistance;

                    // Find nearest valid NavMesh position
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(targetBehindPosition, out hit, agent.stoppingDistance, NavMesh.AllAreas))
                    {
                        PathToPosition(hit.position);
                    }
                    break;
            }
        }
        public virtual void HandleWorkStateOnOwnerClient()
        {
            if (CurrentTask != null)
            {
                CurrentTask.IntervaledUpdate();
            }
            else
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Work state with no task assigned!");
            }
        }
        public virtual void HandlePanicStateOnOwnerClient()
        {
            if (LeaderWhistling != null && LeaderWhistlingZone != null &&
            (LeaderWhistlingZone.LeaderScript == null || !LeaderWhistlingZone.Active))
            {
                LethalMin.Logger.LogInfo($"{DebugID} saving player stopped whistling");
                LeaderWhistling = null;
            }
            switch (CurrentIntention)
            {
                case Pintent.MoveableStuck:
                    if (LeaderWhistling != null)
                    {
                        if (CanBeWhistledOutOfPanic)
                        {
                            LethalMin.Logger.LogWarning($"Leader whistling: {LeaderWhistling.name}");
                            StopPanicingServerRpc();
                            break;
                        }
                        else
                        {
                            PathToPosition(LeaderWhistling.transform.position);
                        }
                    }
                    else
                    {
                        PathToSelf();
                    }
                    break;
                case Pintent.Stuck:
                    break;
                case Pintent.Panicing:
                    agent.speed = pikminType.GetSpeed(CurrentGrowthStage, true);
                    PanicInterval++;
                    if (PanicInterval >= 1)
                    {
                        PanicPosition = GetRandomPositionOnNavMesh();
                        PanicInterval = 0;
                    }
                    PathToPosition(PanicPosition);

                    if (LeaderWhistling != null)
                    {
                        if (CanBeWhistledOutOfPanic)
                        {
                            LethalMin.Logger.LogInfo($"Leader whistling: {LeaderWhistling.name}");
                            StopPanicingServerRpc();
                            break;
                        }
                        else
                        {
                            PathToPosition(LeaderWhistling.transform.position);
                        }
                    }

                    break;
                default:
                    StopPanicingServerRpc();
                    LethalMin.Logger.LogWarning($"{DebugID}: is not in valid panic intention: {CurrentIntention}");
                    break;
            }
        }
        public virtual void HandlePanicStateOnOwnerClientConstant()
        {
            switch (CurrentIntention)
            {
                case Pintent.MoveableStuck:
                    break;
                case Pintent.Stuck:
                    if (LeaderWhistling != null && StuckEscapeTimer != -255)
                    {
                        StuckEscapeTimer -= Time.deltaTime;
                        if (StuckEscapeTimer <= 0)
                        {
                            LethalMin.Logger.LogWarning($"Leader whistling: {LeaderWhistling.name}");
                            StopPanicingServerRpc();
                        }
                    }
                    break;
                case Pintent.Panicing:
                    break;
            }
        }
        public virtual void HandleLeavingStateOnOwnerClient()
        {
            if (IsLeftBehind && isOutside)
            {
                if (LeavingPos == Vector3.zero)
                {
                    LeavingPos = GetRandomPositionOnNavMesh(150);
                }

                PathToPosition(LeavingPos);

                if (Vector3.Distance(transform.position, LeavingPos) < 5f)
                {
                    LeavingPos = GetRandomPositionOnNavMesh(150);
                }
            }
            if (TargetOnion == null)
            {
                return;
            }

            agent.stoppingDistance = 0;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            Vector3 pos = TargetOnion.ClimbLinks[targetOnionLinkIndex].EndPoint.position;
            agent.speed = CalculateDynamicSpeed(pos);

            PathToPosition(pos);

            if (!IsDoingOnionAnimation && (PikUtils.HorizontalDistance(transform.position, pos) < 1f + agent.stoppingDistance ||
            pikminType.InstaEnterOnion ||
            TargetOnion is ShipOnion ||
            timeIdel >= 5))
            {
                EnterOnionServerRpc(targetOnionLinkIndex);
                IsDoingOnionAnimation = true;
            }
        }

        public virtual void SetCurrentTask(string TaskID)
        {
            LethalMin.Logger.LogInfo($"{DebugID}: Setting current task to {TaskID}");
            if (CurrentTask != null)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Current task is not null when setting new task! Intercepting current task: ({CurrentTask.GetType().Name})");
                CurrentTask.TaskIntercepted();
            }
            switch (TaskID)
            {
                case "CarryItem":
                    CurrentTask = new CarryItemTask(this);
                    break;
                case "ReturnToShip":
                    CurrentTask = new ReturnToShipTask(this);
                    break;
                case "AttackEnemy":
                    CurrentTask = new AttackEnemyTask(this);
                    break;
            }
        }

        [ServerRpc]
        public void FinishTaskServerRpc()
        {
            FinishTaskClientRpc();
        }

        [ClientRpc]
        public void FinishTaskClientRpc()
        {
            FinishTask();
        }


        public virtual void FinishTask()
        {
            LethalMin.Logger.LogInfo($"{DebugID}: Task finished");
            if (CurrentTask != null)
            {
                CurrentTask.TaskEnd(false);
                CurrentTask = null;
            }
        }

        public virtual void RemoveCurrentTask()
        {
            LethalMin.Logger.LogInfo($"{DebugID}: Removing current task");
            CurrentTask = null;
        }

        public void CheckToSetVehicle()
        {
            PikminVehicleController? vehicleController = PikUtils.GetLeaderInCar(leader);

            if (IsOwner && vehicleController == null && CurrentVehicle == null &&
            OverrideFollowPosition != null && OverrideFollowPosition.Value.ID == "MoveToVPoint")
            {
                OverrideFollowPosition = null;
                LethalMin.Logger.LogDebug($"{DebugID}: Vehicle moving to point cancelled, no vehicle found");
            }

            if (vehicleController == CurrentVehicle)
            {
                return;
            }

            if (vehicleController == null)
            {
                if (CurrentVehicle != null)
                {
                    SetOffVehicle();
                }
                return;
            }

            Transform form = vehicleController.GetAvaiblePikminPoint(this);
            if (IsOwner && Vector3.Distance(form.position, agent.transform.position) > 1f
            && CurrentVehiclePoint == null)
            {
                agent.stoppingDistance = 0f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                if (OverrideFollowPosition == null)
                    LethalMin.Logger.LogDebug($"{DebugID}: Moving to vehicle point: {form.position}");
                OverrideFollowPosition = new OverridePikminPosition("MoveToVPoint", form.position, false);
                if (chargeRoutine != null)
                {
                    StopCoroutine(chargeRoutine);
                    chargeRoutine = null;
                }
            }
            else if (IsOwner)
            {
                SetCollisionMode(0);
                CurrentVehiclePoint = form;
                OverrideFollowPosition = null;
                CurrentVehicle = vehicleController;
                GetInVehicleRpc(vehicleController.controller.NetworkObject);
                LethalMin.Logger.LogDebug($"{DebugID}: Got in to vehicle: {vehicleController.name}");
            }
        }

        [Rpc(SendTo.NotOwner)]
        public void GetInVehicleRpc(NetworkObjectReference vehicleRef)
        {
            if (vehicleRef.TryGet(out NetworkObject obj) && obj.TryGetComponent(out PikminVehicleController vehicleController))
            {
                Transform form = vehicleController.GetAvaiblePikminPoint(this);
                SetCollisionMode(0);
                CurrentVehiclePoint = form;
                OverrideFollowPosition = null;
                CurrentVehicle = vehicleController;
                LethalMin.Logger.LogDebug($"{DebugID}: Got in to vehicle (synced): {vehicleController.name}");
            }
        }

        public void SetOffVehicle(bool SetCollMode = true)
        {
            bool Check = CurrentVehicle != null;
            CurrentVehicle?.RemovePikminPoint(this);
            CurrentVehiclePoint = null;
            CurrentVehicle = null;
            transform2.TeleportOnLocalClient(transform.position, transform.rotation);
            if (IsOwner && OverrideFollowPosition != null && OverrideFollowPosition.Value.ID == "MoveToVPoint")
            {
                OverrideFollowPosition = null;
            }
            if (SetCollMode)
                SetCollisionMode(1);
            if (Check)
                LethalMin.Logger.LogInfo($"{DebugID}: Set off vehicle");
        }

        public IEnumerator DoCharge(float time)
        {
            float prevSD = agent.stoppingDistance;
            agent.stoppingDistance = 0f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            float HoldTimer = 0f;
            while (HoldTimer < 0.15f)
            {
                HoldTimer += Time.deltaTime;
                agent.speed = 0;
                yield return new WaitForEndOfFrame();
            }

            float ChargeTimer = 0;
            float CheckInterval = 0;
            while (ChargeTimer < time)
            {
                agent.speed = pikminType.GetSpeed(CurrentGrowthStage, ShouldRun) * 3.5f;
                ChargeTimer += Time.deltaTime;
                CheckInterval += Time.deltaTime;
                if (OverrideFollowPosition == null)
                {
                    break;
                }
                if (CheckInterval >= 0.2f + Random.Range(0.01f, 0.05f))
                {
                    if (!CurrentLatchTrigger)
                    {
                        PikminItem? itm = GetClosestPikminItem();
                        if (itm != null)
                        {
                            LethalMin.Logger.LogInfo($"{DebugID}: Found item (Charge): {itm.name}");
                            TargetItem = itm;
                            TargetItemPoint = itm.GetNearestGrabPosition(transform.position);
                            if (TargetItem != null && TargetItemPoint != null)
                            {
                                FindItemViaChargeServerRpc(TargetItem.NetworkObject, TargetItem.GrabToPositions.IndexOf(TargetItemPoint));
                                PathToPosition(TargetItem.transform.position);
                                OverrideFollowPosition = null;
                                chargeRoutine = null;
                                yield break;
                            }
                            else
                            {
                                TargetItem = null!;
                                TargetItemPoint = null!;
                            }
                        }
                    }
                    if (!CurrentLatchTrigger)
                    {
                        PikminEnemy? enm = GetClosestEnemy();
                        if (enm != null)
                        {
                            LethalMin.Logger.LogInfo($"{DebugID}: Found enemy (Charge): {enm.name}");
                            TargetEnemy = enm;
                            if (TargetEnemy)
                            {
                                FindEnemyViaChargeServerRpc(TargetEnemy.NetworkObject);
                                PathToPosition(TargetEnemy.transform.position);
                                OverrideFollowPosition = null;
                                chargeRoutine = null;
                                yield break;
                            }
                            else
                            {
                                TargetEnemy = null!;
                            }
                        }
                    }
                    CheckInterval = 0;
                }
                yield return new WaitForEndOfFrame();
            }

            agent.stoppingDistance = prevSD;
            agent.speed = pikminType.GetSpeed(CurrentGrowthStage, ShouldRun);
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            LethalMin.Logger.LogInfo($"{DebugID}: Charge finished after {ChargeTimer} seconds. Stopping charge.");
            OverrideFollowPosition = null;
            chargeRoutine = null;
        }

        [ServerRpc]
        public void FindItemViaChargeServerRpc(NetworkObjectReference ItemRef, int grabIndex)
        {
            FindItemViaChargeClientRpc(ItemRef, grabIndex);
        }
        [ClientRpc]
        public void FindItemViaChargeClientRpc(NetworkObjectReference ItemRef, int grabIndex)
        {
            if (ItemRef.TryGet(out NetworkObject obj) && obj.TryGetComponent(out PikminItem itm))
            {
                FindItemViaCharge(itm, grabIndex);
            }
        }

        public void FindItemViaCharge(PikminItem itm, int grabIndex)
        {
            CallResetMethods();

            TargetItem = itm;
            TargetItemPoint = itm.GetNearestGrabPosition(transform.position);
            SetPikminToItemLocalClient(itm, grabIndex);
        }

        [ServerRpc]
        public void FindEnemyViaChargeServerRpc(NetworkObjectReference EnemyRef)
        {
            FindEnemyViaChargeClientRpc(EnemyRef);
        }
        [ClientRpc]
        public void FindEnemyViaChargeClientRpc(NetworkObjectReference EnemyRef)
        {
            if (EnemyRef.TryGet(out NetworkObject obj) && obj.TryGetComponent(out PikminEnemy enm))
            {
                FindEnemyViaCharge(enm);
            }
        }

        public void FindEnemyViaCharge(PikminEnemy enm)
        {
            CallResetMethods();

            TargetEnemy = enm;
            SetPikminToEnemyLocalClient(enm);
        }


        private float CalculateDynamicSpeed(Vector3 targetPosition)
        {
            float baseSpeed = pikminType.GetSpeed(CurrentGrowthStage, ShouldRun);
            float distance = Vector3.Distance(transform.position, targetPosition);
            float maxSpeedMultiplier = 50f; // Adjust this value to change the maximum speed increase
            float speedMultiplier = Mathf.Clamp(distance / 10f, 1f, maxSpeedMultiplier); // Adjust 10f to change how quickly speed increases with distance
            return baseSpeed + 10 * speedMultiplier;
        }

        public Vector3 GetRandomPositionOnNavMesh(float sampleRadius = 10)
        {
            Vector3 randomDirection = Random.insideUnitSphere * sampleRadius;
            randomDirection += transform.position;
            NavMeshHit hit;
            Vector3 finalPosition = Vector3.zero;
            if (NavMesh.SamplePosition(randomDirection, out hit, sampleRadius, NavMesh.AllAreas))
            {
                finalPosition = hit.position;
            }
            else
            {
                finalPosition = transform.position;
            }
            return finalPosition;
        }

        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            if (CurrentCollisionMode == 1)
            {
                //Do this to activate the agent on the new owner's side
                SetCollisionMode(1);
            }
            if (CurrentCollisionMode == 3)
            {
                //Do this to activate the rigidbody on the new owner's side
                SetCollisionMode(3);
            }
        }

        [ServerRpc]
        public void SetToIdleServerRpc()
        {
            SetToIdleClientRpc();
        }
        [ClientRpc]
        public void SetToIdleClientRpc()
        {
            SetToIdle();
        }

        /// <summary>
        /// Makes the pikmin change to the idle state and cancle other actions
        /// </summary>
        public virtual void SetToIdle()
        {
            LethalMin.Logger.LogDebug($"{DebugID}: Setting to idle on local client");
            SwitchToBehaviourStateOnLocalClient(IDLE);
            ChangeIntent(Pintent.Idle);
            CallResetMethods(
                RemoveLeader: true,
                DropItem: true,
                RemoveEnemy: true,
                CollisionMode: -1,
                Unlatch: true,
                RemoveTask: true
            );
        }

        [ServerRpc]
        public void ChangeIntentServerRpc(int newIntention)
        {
            ChangeIntentClientRpc(newIntention);
        }

        [ClientRpc]
        public void ChangeIntentClientRpc(int newIntention)
        {
            ChangeIntent((Pintent)newIntention);
        }

        /// <summary>
        /// Switches the Pikmin's intention to the specified one.
        /// </summary>
        /// <param name="newIntention"></param>
        public virtual void ChangeIntent(Pintent newIntention)
        {
            PreviousIntention = CurrentIntention;
            CurrentIntention = newIntention;
        }
        #endregion






        #region Onion Management
        [ServerRpc]
        public void SetPikminToLeavingServerRpc(NetworkObjectReference OnionRef)
        {
            SetPikminToLeavingClientRpc(OnionRef);
        }
        [ClientRpc]
        public void SetPikminToLeavingClientRpc(NetworkObjectReference OnionRef)
        {
            if (OnionRef.TryGet(out NetworkObject NetObj) && NetObj.TryGetComponent(out Onion onion))
            {
                SetPikminToLeaving(onion);
            }
            else
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to get Target Onion when setting leave!");
            }
        }
        public virtual void SetPikminToLeaving(Onion? onion = null)
        {
            timeIdel = 0;
            CallResetMethods();
            SwitchToBehaviourStateOnLocalClient(LEAVING);
            ChangeIntent(Pintent.Leave);
            if (onion != null)
            {
                IsGoingToOnion = true;
                SetTargetOnion(onion);
            }
            LethalMin.Logger.LogDebug($"{DebugID}: Set to leaving state. IsGoingToOnion: {IsGoingToOnion}. " +
                $"TargetOnion: {(TargetOnion != null ? TargetOnion.name : "null")}");
        }


        [ServerRpc]
        public void SetTargetOnionServerRpc(NetworkObjectReference OnionRef)
        {
            SetTargetOnionClientRpc(OnionRef);
        }
        [ClientRpc]
        public void SetTargetOnionClientRpc(NetworkObjectReference OnionRef)
        {
            if (OnionRef.TryGet(out NetworkObject NetObj) && NetObj.TryGetComponent(out Onion onion))
            {
                SetTargetOnion(onion);
            }
            else
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to get Target Onion!");
            }
        }
        public void SetTargetOnion(Onion onion)
        {
            TargetOnion = onion;
            targetOnionLinkIndex = enemyRandom.Next(0, TargetOnion.ClimbLinks.Count);
        }


        [ServerRpc]
        public void EnterOnionServerRpc(int linkIndex)
        {
            EnterOnionClientRpc(linkIndex);
        }
        [ClientRpc]
        public void EnterOnionClientRpc(int linkIndex)
        {
            EnterOnionOnLocalClient(linkIndex);
        }
        public void EnterOnionOnLocalClient(int linkIndex)
        {
            if (!DontAddToOnion)
                TargetOnion.AddPikmin(this);
            IsGoingToOnion = false;
            PlayAudioOnLocalClient(PikminSoundPackSounds.EnterOnion.ToString(), true, 1);
            SetPikminOnToAnimLink(TargetOnion.ClimbLinks[linkIndex], true);
        }
        #endregion






        #region Leader Assignments
        [ServerRpc(RequireOwnership = false)]
        public void AssignLeaderServerRpc(NetworkObjectReference LeaderRef, bool SwitchToFollowingState = true)
        {
            Leader? leader = null;
            NetworkObject? leaderObj;
            if (LeaderRef.TryGet(out leaderObj)
            && leaderObj.TryGetComponent(out leader) && leader != null)
            {
                if (leader.OwnerClientId != OwnerClientId)
                {
                    ChangeOwnershipOfEnemy(leader.OwnerClientId);
                }

                AssignLeaderClientRpc(LeaderRef, SwitchToFollowingState);
            }
            else
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to assign leader to {leaderObj?.name}");
                if (leader == null)
                    LethalMin.Logger.LogError($"{DebugID}: Leader is null");
                if (leaderObj == null)
                    LethalMin.Logger.LogError($"{DebugID}: LeaderObj is null");
            }
        }
        [ClientRpc]
        public void AssignLeaderClientRpc(NetworkObjectReference LeaderRef, bool SwitchState = true)
        {
            Leader? leader = null;
            NetworkObject? leaderObj;
            if (LeaderRef.TryGet(out leaderObj)
            && leaderObj.TryGetComponent(out leader) && leader != null)
            {
                AssignLeader(leader, SwitchState);
            }
            else
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to assign leader to {leaderObj?.name}");
                if (leader == null)
                    LethalMin.Logger.LogError($"{DebugID}: Leader is null");
                if (leaderObj == null)
                    LethalMin.Logger.LogError($"{DebugID}: LeaderObj is null");
            }
        }

        /// <summary>
        /// Sets the Pikmin's leader to the specified leader
        /// Should be called on every client
        /// </summary>
        /// <param name="LeaderRef"></param>
        /// The leader's network object
        /// <param name="SwitchState"></param>
        public virtual void AssignLeader(Leader leader, bool SwitchState = true, bool PlayAnim = true)
        {
            if (leader == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Leader Assigning to is null when assigning leader");
                return;
            }

            if (currentBehaviourStateIndex == PANIC)
            {
                return;
            }

            if (leader.PikminInSquad.Contains(this) && leader == this.leader)
            {
                //LethalMin.Logger.LogWarning($"{DebugID}: is attempting to assign the same leader");
                return;
            }

            if (this.leader != null)
            {
                previousLeader = this.leader;
            }
            else
            {
                previousLeader = leader;
            }

            CallResetMethods(
                RemoveLeader: true,
                DropItem: true,
                RemoveEnemy: true,
                CollisionMode: 1,
                Unlatch: true,
                RemoveTask: true
            );

            //LethalMin.Logger.LogDebug($"{DebugID}: previous leader: {PikUtils.NullableName(previousLeader)}");

            this.leader = leader;
            this.isOutside = !leader.Controller.isInsideFactory;
            leader.PikminInSquad.Add(this);
            leader.OnPikminAdded(this);

            if (IsWildPikmin)
            {
                Pmanager.PikminAICounter.Add(this);
                if (AddToRaisedOnInitAssign)
                {
                    PikminManager.instance.EndOfGameStats.PikminRaised[leader] += 1;
                    PikminManager.instance.FiredStats.TotalPikminRaised += 1;
                }
                IsWildPikmin = false;
            }

            if (SwitchState)
            {
                SwitchToBehaviourStateOnLocalClient(FOLLOW);
                ChangeIntent(Pintent.Idle);
            }

            if (PlayAnim)
            {
                if (PreviousIntention == Pintent.Knockedback || CurrentIntention == Pintent.Knockedback)
                {
                    PlayAnimation(animController.AnimPack.EditorGetUpAnim);
                }
                else
                {
                    PlayAnimation(animController.AnimPack.EditorNoticeAnim);
                }
                PlayAudioOnLocalClient(PikminSoundPackSounds.Notice);
            }

            //SetCollisionMode(1);

            LethalMin.Logger.LogInfo($"{DebugID}: Assigned Leader: {leader.Controller.playerUsername}" +
            $" ({leader.Controller.OwnerClientId})");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveLeaderServerRpc()
        {
            RemoveLeaderClientRpc();
        }
        [ClientRpc]
        public void RemoveLeaderClientRpc()
        {
            RemoveLeader();
        }
        /// <summary>
        /// Removes the leader from the Pikmin if it's not null.
        /// Sets the Pikmin's leader to null even if it's already null.
        /// Should be called on every client
        /// </summary>
        public virtual void RemoveLeader()
        {
            if (leader != null)
            {
                leader.RemovePikminFromSquad(this);
                if (leader.pikminHolding == this)
                {
                    leader.StopThrow(false);
                }
            }
            if (leader != null && !PikChecks.IsPlayerConnected(leader.Controller))
            {
                previousLeader = leader;
                if (IsServer)
                    ChangeOwnershipOfEnemy(NetworkManager.ServerClientId);
            }
            StopThrow();
            leader = null;
            LethalMin.Logger.LogDebug($"{DebugID}: Removed Leader");
            //LethalMin.Logger.LogInfo($"{DebugID}: previous leader: {PikUtils.NullableName(previousLeader)}");
        }
        #endregion






        #region Physics
        /// <summary>
        /// Sets the collision mode of the Pikmin
        /// Should be called on every client
        /// 0 = none,
        /// 1 = agent only,
        /// 2 = rigidbody only
        /// 3 = rigidbody owner only
        /// </summary>
        /// <param name="Mode"></param>
        public virtual void SetCollisionMode(int Mode)
        {
            const int None = 0;
            const int AgentOnly = 1;
            const int RigidbodyOnly = 2;
            const int RigidbodyOwnerOnly = 3;

            LethalMin.Logger.LogDebug($"{DebugID}: Setting collision mode to {Mode}");

            CurrentCollisionMode = Mode;

            switch (Mode)
            {
                case None:
                    agent.enabled = false;
                    transform2.enabled = false;
                    moveTowardsDestination = false;
                    movingTowardsTargetPlayer = false;
                    updatePositionThreshold = 9000;
                    syncMovementSpeed = 0f;
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    Pcollider.enabled = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    rb.detectCollisions = false;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                    rb.interpolation = RigidbodyInterpolation.None;
                    rb.sleepThreshold = 0.005f;
                    rb.excludeLayers = 0;
                    //rb2.enabled = false;
                    rb.Sleep();
                    break;
                case AgentOnly:
                    if (IsOwner)
                    {
                        agent.enabled = true;
                    }
                    else
                    {
                        agent.enabled = false;
                    }
                    transform2.enabled = true;
                    //transform.rotation = Quaternion.Euler(0, 0, transform.rotation.z);
                    updatePositionThreshold = 1f;
                    syncMovementSpeed = 0.22f;
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    Pcollider.enabled = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    rb.detectCollisions = true;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                    rb.interpolation = RigidbodyInterpolation.None;
                    rb.sleepThreshold = 0.005f;
                    rb.excludeLayers = 0;
                    //rb2.enabled = false;
                    rb.Sleep();
                    break;
                case RigidbodyOnly:
                    agent.enabled = false;
                    moveTowardsDestination = false;
                    movingTowardsTargetPlayer = false;
                    updatePositionThreshold = 9000;
                    syncMovementSpeed = 0f;
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.detectCollisions = true;
                    rb.constraints = RigidbodyConstraints.None;
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    rb.sleepThreshold = 0f;
                    if (IsOwner)
                    {
                        Pcollider.excludeLayers = ~LethalMin.PikminColideable;
                        rb.excludeLayers = ~LethalMin.PikminColideable;
                    }
                    else
                    {
                        Pcollider.excludeLayers = ~LethalMin.PikminColideable;
                        rb.excludeLayers = ~LethalMin.PikminColideable;
                        // Pcollider.excludeLayers = ~0;
                        // rb.excludeLayers = ~0;
                    }
                    Pcollider.enabled = true;
                    //rb2.enabled = false;
                    rb.WakeUp();
                    break;
                case RigidbodyOwnerOnly:
                    agent.enabled = false;
                    if (IsOwner)
                    {
                        Pcollider.enabled = true;
                        moveTowardsDestination = false;
                        movingTowardsTargetPlayer = false;
                        updatePositionThreshold = 9000;
                        syncMovementSpeed = 0f;
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        rb.detectCollisions = true;
                        rb.constraints = RigidbodyConstraints.None;
                        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        Pcollider.excludeLayers = ~LethalMin.PikminColideable;
                        rb.excludeLayers = ~LethalMin.PikminColideable;
                        rb.sleepThreshold = 0f;
                        rb.WakeUp();
                    }
                    else
                    {
                        Pcollider.enabled = false;
                        moveTowardsDestination = false;
                        movingTowardsTargetPlayer = false;
                        updatePositionThreshold = 9000;
                        syncMovementSpeed = 0f;
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        rb.detectCollisions = false;
                        rb.constraints = RigidbodyConstraints.FreezeAll;
                        rb.interpolation = RigidbodyInterpolation.None;
                        rb.excludeLayers = 0;
                        rb.sleepThreshold = 0.005f;
                        rb.Sleep();
                    }
                    transform2.enabled = true;
                    //rb2.enabled = true;
                    break;
            }
        }

        /// <summary>
        /// snaps the pikmin to the specified position
        /// depending on it's spesfic condisions.
        /// </summary>
        public virtual void UpdateLocalSnapping()
        {
            if (CurrentLatchTrigger != null)
            {
                Vector3 vect = CurrentLatchTrigger.OverrideLookAtObject != null ?
                CurrentLatchTrigger.OverrideLookAtObject.transform.position : CurrentLatchTrigger.transform.position;

                transform.position = LatchRefPoint.transform.position;
                transform.rotation = LookAtTarget(vect);
            }

            if (CurrentIntention == Pintent.BeingHeld && leader != null)
            {
                transform.position = leader.holdPosition.transform.position;
                transform.rotation = leader.holdPosition.transform.rotation;
            }

            HandleCarrySnapping();

            if (CurrentLinkAnim != null)
            {
                AnimateOnLink();
            }


            if (DeathSnapToPos != null)
            {
                transform.position = DeathSnapToPos.position;
                transform.rotation = RandomRotaion;
                return;
            }

            if (CurrentIntention == Pintent.Idle && SpecialIdlePosition != null)
            {
                transform.position = SpecialIdlePosition.Value;
            }

            if (CurrentIntention == Pintent.Idle && SpecialIdleRotation != null)
            {
                transform.rotation = SpecialIdleRotation.Value;
            }

            if (CurrentVehicle != null && CurrentVehiclePoint != null)
            {
                transform.position = CurrentVehiclePoint.position;
            }
        }

        public virtual void HandleCarrySnapping()
        {
            if (CurrentIntention == Pintent.Carry && TargetItem != null && TargetItemPoint != null
            && TargetItem.PrimaryPikminOnItem != this)
            {
                transform.position = PikUtils.GetPositionOffsetedOnNavMesh(TargetItemPoint.transform.position);
                Vector3 directionToTarget = TargetItem.transform.position - transform.position;
                directionToTarget.y = 0; // This ensures rotation only on Y axis

                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15 * Time.deltaTime);
                }
            }
        }


        /// <summary>
        /// Starts the throw squnce
        /// Should be called on every client
        /// </summary>
        public virtual void StartThrow()
        {
            if (leader == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Leader is null when starting throw");
                return;
            }
            ChangeIntent(Pintent.BeingHeld);
            SetCollisionMode(0);
            RemoveFromLink();
            PlayAnimation(animController.AnimPack.EditorHoldAnim, 0);
            PlayAudioOnLocalClient(PikminSoundPackSounds.Prepare);
        }

        /// <summary>
        /// Throws the pikmin using physics
        /// Should be called on every client
        /// </summary>
        public virtual void ThrowPikmin(Vector3 Direction)
        {
            if (leader == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Leader is null when throwing pikmin");

                if (CurrentIntention == Pintent.BeingHeld)
                    SetToIdle();
                return;
            }

            // Calculate throw force based on direction components
            Vector3 throwForce = new Vector3(
                Direction.x * pikminType.ThrowForce.x,
                Direction.y * pikminType.ThrowForce.y,
                Direction.z * pikminType.ThrowForce.z
            );

            LethalMin.Logger.LogDebug($"{DebugID}: Thrown with force: {throwForce}");
            ChangeIntent(Pintent.Thrown);
            SwitchToBehaviourStateOnLocalClient(IDLE);
            SetCollisionMode(2);
            rb.position = leader.ThrowOrigin.position;
            transform.position = leader.ThrowOrigin.position;
            transform.rotation = leader.ThrowOrigin.rotation;
            rb.AddForce(throwForce, ForceMode.Impulse);
            creatureVoice.Stop();
            PlayAudioOnLocalClient(PikminSoundPackSounds.ThrownSFX);
            PlayAudioOnLocalClient(PikminSoundPackSounds.Thrown);
            PlayAnimation(animController.AnimPack.EditorThrowAnim);
        }

        /// <summary>
        /// Should only be called on the owner
        /// </summary>
        /// <param name="collision"></param>
        public virtual void OnCollisionEnter(Collision collision)
        {
            if (!IsOwner || CurrentIntention != Pintent.Thrown)
            {
                return;
            }

            if (pikminType.CanLatchOnToObjects)
            {
                PikminLatchTrigger latchTrigger = collision.gameObject.GetComponent<PikminLatchTrigger>();
                if (latchTrigger != null && latchTrigger.AllowBaseLatchOn && latchTrigger.TryLatch(this, collision.contacts[0].point))
                {
                    return;
                }
            }
            LethalMin.Logger.LogInfo($"{DebugID}: Collision with {collision.gameObject.name}");
            agent.Warp(transform.position);
            transform2.Teleport(transform.position, transform.rotation, transform.localScale);
            LandPikminServerRpc();
        }

        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy = null!)
        {
            base.OnCollideWithEnemy(other, collidedEnemy);

            if (!IsOwner || CurrentIntention != Pintent.Thrown || !pikminType.CanLatchOnToObjects
            || !collidedEnemy.TryGetComponent(out PikminEnemy Penemy) || !other.TryGetComponent(out PikminLatchTrigger latchTrigger)
            || !PikChecks.IsEnemyVaildToAttack(Penemy))
            {
                return;
            }

            LethalMin.Logger.LogInfo($"{DebugID}: Landed on enemy {other.gameObject.name}");
            LandPikminOnEnemy(other, latchTrigger, Penemy);
        }

        public virtual void OnCollisionStay(Collision collision)
        {
            if (!IsOwner)
            {
                return;
            }
            if (LandBuffer > 0)
            {
                LandBuffer -= Time.deltaTime;
                return;
            }
            string identifier = CurrentIntention switch
            {
                Pintent.Knockedback => "Knockback",
                Pintent.Fall => "Fall",
                _ => "Persistent"
            };
            LethalMin.Logger.LogInfo($"{DebugID}: {identifier} Collision with {collision.gameObject.name}");
            agent.Warp(transform.position);
            transform2.Teleport(transform.position, transform.rotation, transform.localScale);
            LandPikminServerRpc();
            LandBuffer = 0.5f;
        }

        [ServerRpc]
        public void LandPikminServerRpc()
        {
            LandPikminClientRpc();
        }
        [ClientRpc]
        public void LandPikminClientRpc()
        {
            LandPikmin();
        }

        /// <summary>
        /// Detected by the server,
        /// Should be called on every client
        /// </summary>
        public virtual void LandPikmin()
        {
            //LethalMin.Logger.LogInfo($"{DebugID}: Landing Pikmin");
            if (!IsDeadOrDying)
            {
                SetCollisionMode(1);
                SetToIdle();
                if (PreviousIntention == Pintent.Knockedback)
                {
                    timeLaying = (float)enemyRandom.Next(1, 25) * 0.1f;
                    LethalMin.Logger.LogInfo($"g{DebugID}: Laying for {timeLaying} seconds");
                    Laying = true;
                }
            }
            animController.PlayLandAnim();
        }

        public virtual void LandPikminOnEnemy(Collider hitbox, PikminLatchTrigger latchTrigger, PikminEnemy enemy)
        {
            if (pikminType.CanLatchOnToObjects
            && enemy != null
            && enemy.enemyScript.enemyType != enemyType
            && CurrentLatchTrigger == null
            && !enemy.enemyScript.isEnemyDead)
            {
                if (!enemy.LatchTriggers.Contains(latchTrigger))
                {
                    LethalMin.Logger.LogError($"{DebugID}: Latch trigger {latchTrigger.gameObject.name} not found in {enemy.gameObject.name}'s list");
                    return;
                }
                int Index = enemy.LatchTriggers.IndexOf(latchTrigger);
                Vector3 approximateContactPoint = hitbox.ClosestPoint(transform.position);
                if (enemy != null && latchTrigger.TryLatch(this, approximateContactPoint, true)) // Calls LatchPikmin on owner if true
                {
                    LatchPikminOnToEnemyServerRpc(enemy.NetworkObject, approximateContactPoint, Index); // Calls LatchPikmin on all clients if true
                }
            }
        }

        [ServerRpc]
        public void LatchPikminOnToEnemyServerRpc(NetworkObjectReference enemyRef, Vector3 LandPos, int Index = 0)
        {
            LatchPikminOnToEnemyClientRpc(enemyRef, LandPos, Index);
        }
        [ClientRpc]
        public void LatchPikminOnToEnemyClientRpc(NetworkObjectReference enemyRef, Vector3 LandPos, int Index = 0)
        {
            if (IsOwner)
            {
                return;
            }
            if (!enemyRef.TryGet(out NetworkObject enemyObj) || !enemyObj.TryGetComponent(out PikminEnemy enemy))
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to get enemy for latching!");
                return;
            }
            LatchPikminOnToEnemy(enemy, LandPos, Index);
        }

        public virtual void LatchPikminOnToEnemy(PikminEnemy enemy, Vector3 LandPos, int Index = 0)
        {
            enemy.LatchTriggers[Index].LatchPikmin(this, LandPos);
        }

        public void StopThrow(bool PlayAnim = true)
        {
            if (CurrentIntention != Pintent.BeingHeld)
            {
                return;
            }

            if (leader != null)
            {
                leader.StopThrow(false);
            }

            if (PlayAnim)
            {
                SetCollisionMode(1);
                ChangeIntent(Pintent.Idle);
                animController.ResetToIdleAnim();
            }
        }

        [ServerRpc]
        public void ApplyKnockBackServerRpc(Vector3 direction, float force)
        {
            ApplyKnockBackClientRpc(direction, force);
        }

        [ClientRpc]
        public void ApplyKnockBackClientRpc(Vector3 direction, float force)
        {
            ApplyKnockBack(direction, force);
        }

        /// <summary>
        /// Applies a knockback force to the Pikmin in the specified direction.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="force"></param>
        public void ApplyKnockBack(Vector3 direction, float force)
        {
            LethalMin.Logger.LogDebug($"{DebugID}: Knockback with force: {force} and direction: {direction}");
            StopThrow(false);
            SwitchToBehaviourStateOnLocalClient(IDLE);
            ChangeIntent(Pintent.Knockedback);
            CallResetMethods(
                RemoveLeader: true,
                DropItem: true,
                RemoveEnemy: true,
                CollisionMode: 3,
                Unlatch: true,
                RemoveTask: true
            );

            PlayAnimation(animController.AnimPack.EditorKnockbackAnim);
            PlayAudioOnLocalClient(PikminSoundPackSounds.Knockback);
            if (IsOwner)
            {
                rb.rotation = Quaternion.LookRotation(-direction);
                rb.AddForce(direction * force, ForceMode.Impulse);
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }


        [ServerRpc]
        public void ApplyPhysicsServerRpc(bool Use3)
        {
            ApplyPhysicsClientRpc(Use3);
        }
        [ClientRpc]
        public void ApplyPhysicsClientRpc(bool Use3)
        {
            ApplyPhysics(Use3);
        }
        /// <summary>
        /// Applies physics to the Pikmin without resetting its state.
        /// </summary>
        public void ApplyPhysics(bool Use3)
        {
            LethalMin.Logger.LogDebug($"{DebugID}: Applying Physics");
            LandBuffer = 0.25f;
            StopThrow(false);
            ChangeIntent(Pintent.Fall);
            SetCollisionMode(Use3 ? 3 : 2);
            TryUnlatchPikmin();
        }


        /// <summary>
        /// Assings the pikmin's Latch trigger to a latch
        /// </summary>
        /// <param name="latchTrigger">The latch trigger script</param>
        public void LatchPikmin(PikminLatchTrigger latchTrigger, Vector3 LandPos = default(Vector3))
        {
            LethalMin.Logger.LogInfo($"{DebugID}: Latching onto {latchTrigger.name}");
            SetCollisionMode(0);
            StuckEscapeTimer = latchTrigger.WhistleTime;
            if (latchTrigger.StateToSet == LatchTriggerStateToSet.Attack)
            {
                SetCurrentTask("AttackEnemy");
            }
            ChangeIntent(latchTrigger.GetPikminState().Item1);
            SwitchToBehaviourStateOnLocalClient(latchTrigger.GetPikminState().Item2);

            if (latchTrigger.KillTimer > 0)
            {
                EditAffecters($"{latchTrigger.gameObject.GetInstanceID()}", latchTrigger.KillTimer);
            }

            transform.position = LandPos;

            // Create a temporary object at the Pikmin's current world position
            if (LatchRefPoint == null)
            {
                GameObject tempObject = new GameObject($"{gameObject.name}LATCH");
                TempObjects.Add(tempObject);
                //cache the reference to the temp object for later use
                LatchRefPoint = tempObject;
            }
            LatchRefPoint.transform.SetParent(null);

            LatchRefPoint.transform.position = transform.position;

            // Parent the temp object to the latch trigger
            if (latchTrigger.OverrideLatchObject == null)
            {
                LatchRefPoint.transform.SetParent(latchTrigger.transform, true);
            }
            else
            {
                LatchRefPoint.transform.SetParent(latchTrigger.OverrideLatchObject.transform, true);
            }

            // Calculate the local offset
            LatchTriggerOffset = LatchRefPoint.transform.localPosition;

            CurrentLatchTrigger = latchTrigger;

            // Set the Pikmin's position to match the temp object
            transform.position = LatchRefPoint.transform.position;
            transform.rotation = LatchRefPoint.transform.rotation;
        }
        /// <summary>
        /// When unlatching a pikmin from the pikmin AI script, call THIS instead of UnlatchPikmin()
        /// Should be called on every client
        /// </summary>
        /// <returns>True if the CurrentLatchTrigger is not null, false if otherwise</returns>
        public bool TryUnlatchPikmin()
        {
            if (CurrentLatchTrigger != null)
            {
                CurrentLatchTrigger.UnlatchPikmin(this);
                return true;
            }
            else
            {
                UnlatchPikmin();
                return false;
            }
        }
        /// <summary>
        /// Do not call this directly, Use TryUnlatchPikmin() instead
        /// </summary>
        public void UnlatchPikmin()
        {
            if (CurrentLatchTrigger != null)
            {
                EditAffecters($"{CurrentLatchTrigger.gameObject.GetInstanceID()}", 0, true);
                if (PreviousIntention != Pintent.Knockedback && PreviousIntention != Pintent.Dead)
                    animController.ResetToIdleAnim();
            }

            CurrentLatchTrigger = null!;
            LatchTriggerOffset = Vector3.zero;

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null!;
            }
        }

        public void DoJump(Vector3? Direction = null!, Vector3? Target = null!)
        {
            if (!IsOwner)
            {
                return;
            }

            if (Direction == null && Target == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: DoJump called with no direction or target!");
                return;
            }
            if (Target != null)
            {
                Direction = (Target.Value - transform.position).normalized; // Calculate direction to target if target is provided
            }
            if (Direction == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Direction is null after calculation!");
                return;
            }

            DoJumpServerRpc(Direction.Value);
        }


        [ServerRpc]
        public void DoJumpServerRpc(Vector3 Direciton)
        {
            DoJumpClientRpc(Direciton);
        }
        [ClientRpc]
        public void DoJumpClientRpc(Vector3 Direction)
        {
            DoJumpOnLocalClient(Direction);
        }
        public virtual void DoJumpOnLocalClient(Vector3 Direction)
        {
            Vector3 throwForce = new Vector3(
                Direction.x * pikminType.ThrowForce.x,
                Direction.y * pikminType.ThrowForce.y,
                Direction.z * pikminType.ThrowForce.z
            );
            throwForce = throwForce + (transform.up * 3);

            LethalMin.Logger.LogDebug($"{DebugID}: jumped with force: {throwForce}");
            ChangeIntent(Pintent.Thrown);
            SetCollisionMode(2);
            StartCoroutine(DisableColiderAfterJump(0.1f)); // Disable collider for a short time to prevent collision issues during jump
            rb.position += transform.up * 1f;
            transform.rotation = Quaternion.LookRotation(Direction); // Set rotation to face the direction of the jump
            rb.AddForce(throwForce, ForceMode.Impulse);
            creatureVoice.Stop();
            PlayAudioOnLocalClient(PikminSoundPackSounds.Thrown);
            PlayAnimation(animController.AnimPack.EditorThrowAnim);
        }

        protected IEnumerator DisableColiderAfterJump(float timer)
        {
            Pcollider.enabled = false;
            yield return new WaitForSeconds(timer);
            Pcollider.enabled = true;
        }



        public void CheckIfOnNavmesh()
        {
            if (!agent.enabled || agent.isOnNavMesh || agent.isOnOffMeshLink || CurrentLinkAnim != null)
            {
                return;
            }
            if (timeOffNavMesh > 0)
            {
                timeOffNavMesh -= Time.deltaTime;
                return;
            }

            timeOffNavMesh = 4;

            Vector3 posF = FindPositionToGetIntoBounds();
            agent.Warp(posF);
            LethalMin.Logger.LogWarning($"{DebugID}: Warped to {posF} due to being off navmesh");
        }

        [ServerRpc]
        public void WarpIntoBoundsServerRpc(Vector3 Pos)
        {
            WarpIntoBoundsClientRpc(Pos);
        }
        [ClientRpc]
        public void WarpIntoBoundsClientRpc(Vector3 Pos)
        {
            WarpIntoBoundsOnLocalClient(Pos);
        }
        public virtual void WarpIntoBoundsOnLocalClient(Vector3 Pos)
        {
            LandPikmin();
            if (IsOwner)
            {
                agent.Warp(Pos);
            }
            transform2.TeleportOnLocalClient(Pos);
        }

        Vector3 FindPositionToGetIntoBounds()
        {
            if (leader != null && !PikChecks.IsLeaderInvalid(leader) && leader.Controller.isPlayerControlled)
            {
                Vector3 pos = RoundManager.Instance.GetNavMeshPosition(leader.transform.position);
                LethalMin.Logger.LogInfo($"{DebugID}: Found position to get into bounds (leader): {pos}");
                return pos;
            }

            if (previousLeader != null && !PikChecks.IsLeaderInvalid(previousLeader) && previousLeader.Controller.isPlayerControlled)
            {
                Vector3 pos = RoundManager.Instance.GetNavMeshPosition(previousLeader.transform.position);
                LethalMin.Logger.LogInfo($"{DebugID}: Found position to get into bounds (previous leader): {pos}");
                return pos;
            }

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 100, NavMesh.AllAreas))
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Found position to get into bounds (navmesh): {hit.position}");
                return hit.position;
            }

            LethalMin.Logger.LogWarning($"{DebugID}: Could not find position to get into bounds, using local leader's position");
            return PikminManager.instance.LocalLeader.transform.position;
        }


        public Quaternion LookAtTarget(Vector3 target)
        {
            Vector3 directionToTarget = target - transform.position;

            if (directionToTarget != Vector3.zero)
            {
                return Quaternion.LookRotation(directionToTarget);
            }
            return transform.rotation;
        }
        #endregion






        #region Panicing
        [ServerRpc]
        public void StartPanicingServerRpc(int mode, int type, string PanicAnim)
        {
            StartPanicingClientRpc(mode, type, PanicAnim);
        }
        [ClientRpc]
        public void StartPanicingClientRpc(int mode, int type, string PanicAnim)
        {
            PanicOnLocalClient((PikminEffectMode)mode, (PikminEffectType)type, PanicAnim);
        }
        public virtual void PanicOnLocalClient(PikminEffectMode mode, PikminEffectType type, string PanicAnim)
        {
            if (CanBeWhistledOutOfPanic && currentBehaviourStateIndex == PANIC && type == PikminEffectType.Paralized)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Overriding current panic state!!!!");
            }
            else if (currentBehaviourStateIndex == PANIC)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Panicking while already in panic state");
                return;
            }
            if (panicSoundRoutine != null)
            {
                StopCoroutine(panicSoundRoutine);
                panicSoundRoutine = null!;
            }
            CallResetMethods();
            //SetCollisionMode(1);
            CanBeWhistledOutOfPanic = mode == PikminEffectMode.Limited;
            ChangeIntent(type == PikminEffectType.Paralized ? Pintent.MoveableStuck : Pintent.Panicing);
            SwitchToBehaviourStateOnLocalClient(PANIC);
            EditAffecters($"PANICSTA_{type.ToString()}", enemyRandom.Next(5, 15));
            CurPanicAnim = PanicAnim;

            switch (PanicAnim)
            {
                case "WaterPanic":
                    PlayAnimation(animController.AnimPack.EditorDrowingAnim);
                    DrowningAudioSource.clip = CurrentSoundPack.PullSoundFromDict(PikminSoundPackSounds.Drowning, enemyRandom);
                    DrowningAudioSource.Play();
                    break;
                case "PoisonPanic":
                    PlayAnimation(animController.AnimPack.EditorPosionFlopAnim);
                    panicSoundRoutine = StartCoroutine(PlayRandomPanicSoundOnInterval(PikminSoundPackSounds.Cough, (7, 15)));
                    break;
                case "FirePanic":
                    PlayAnimation(animController.AnimPack.EditorBurnAnim);
                    panicSoundRoutine = StartCoroutine(PlayRandomPanicSoundOnInterval(PikminSoundPackSounds.Burn, (7, 15)));
                    break;
            }
            LethalMin.Logger.LogInfo($"{DebugID}: Started panicking on local client ({mode}, {type}) = ({CanBeWhistledOutOfPanic},{CurrentIntention})");
        }

        [ServerRpc]
        public void StopPanicingServerRpc()
        {
            StopPanicingClientRpc();
        }
        [ClientRpc]
        public void StopPanicingClientRpc()
        {
            StopPanicingOnLocalClient();
        }
        public virtual void StopPanicingOnLocalClient()
        {
            //LethalMin.Logger.LogInfo($"{DebugID}: Stopped panicking on local client");
            animController.ResetToIdleAnim();
            DrowningAudioSource.Stop();
            CurPanicAnim = "";
            if (panicSoundRoutine != null)
            {
                StopCoroutine(panicSoundRoutine);
                panicSoundRoutine = null!;
            }
            SetToIdle();
            List<string> tmpstr = new List<string>(DeathTimerAffecters);
            foreach (string affect in tmpstr)
            {
                if (affect.Contains("PANICSTA_"))
                {
                    EditAffecters(affect, 0, true);
                }
            }
            if (IsOwner && LeaderWhistling != null)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Automatically assigning leader after panicking");
                AssignLeaderServerRpc(LeaderWhistling.NetworkObject);
                LeaderWhistling = null;
            }
        }

        IEnumerator PlayRandomPanicSoundOnInterval(PikminSoundPackSounds sound, (int, int) interval)
        {
            while (true)
            {
                float intervalparsed = enemyRandom.Next(interval.Item1, interval.Item2) / 10;
                yield return new WaitForSeconds(intervalparsed);
                PlayAudioOnLocalClient(sound);
            }
        }
        #endregion






        #region Attacking
        public virtual PikminEnemy? GetClosestEnemy(float overrideDetectionRadius = -1)
        {
            float detectionRadius = overrideDetectionRadius == -1 ? pikminType.EnemyDetectionRange : overrideDetectionRadius;
            PikminEnemy? bestCandidate = null;
            float bestDistance = float.MaxValue;

            foreach (PikminEnemy enemy in Pmanager.PikminEnemies)
            {
                // Skip invalid enemies immediately
                if (enemy == null)
                    continue;

                if (IsInShip && !StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(enemy.transform.position))
                    continue;

                float distance = Vector3.Distance(enemy.transform.position, transform.position);
                if (distance >= detectionRadius)
                    continue;

                // Handle special case for converting dead enemies
                if (LethalMin.ConvertEnemyBodiesToItems && !LethalMin.EnemyBodyConvertBlacklistConfig.InternalValue.Contains(enemy.enemyScript.enemyType.enemyName)
                 && !LethalMin.IsDependencyLoaded("Entity378.sellbodies") &&
                    enemy.enemyScript.isEnemyDead && !enemy.enemyScript.enemyType.destroyOnDeath && !PikminManager.instance.ConvertedAIs.Contains(enemy.enemyScript))
                {
                    LethalMin.Logger.LogMessage($"{DebugID}: Converting {enemy.gameObject.name} to grabbable object");
                    PikminManager.instance.ConvertedAIs.Add(enemy.enemyScript);
                    PikminManager.instance.ConvertEnemyToGrabbableObjectServerRpc(enemy.NetworkObject);
                    continue;
                }

                if (!PikChecks.IsEnemyVaildToAttack(enemy))
                    continue;

                // If this enemy is closer than our current best, update our best
                if (distance < bestDistance)
                {
                    bestCandidate = enemy;
                    bestDistance = distance;
                }
            }

            return bestCandidate;
        }

        /// <summary>
        /// For standing attacks
        /// </summary>
        [ServerRpc]
        public void SetPikminToEnemyServerRpc(NetworkObjectReference Ref)
        {
            SetPikminToEnemyClientRpc(Ref);
        }
        [ClientRpc]
        public void SetPikminToEnemyClientRpc(NetworkObjectReference Ref)
        {
            if (IsOwner)
            {
                return;
            }
            if (Ref.TryGet(out NetworkObject obj) && obj.TryGetComponent(out PikminEnemy enm))
            {
                SetPikminToEnemyLocalClient(enm);
            }
            else
            {
                LethalMin.Logger.LogError($"{DebugID}: Could not find enemy");
            }
        }
        public void SetPikminToEnemyLocalClient(PikminEnemy enemy)
        {
            TargetEnemy = enemy;
            AgentLookTarget = enemy.transform;
            ChangeIntent(Pintent.Attack);
            SetCurrentTask("AttackEnemy");
            SwitchToBehaviourStateOnLocalClient(WORK);
        }

        public virtual void AttackEnemyWhenNear()
        {
            if (TargetEnemy == null)
            {
                return;
            }
            float HitRange = pikminType.AttackDistance + TargetEnemy.enemyScript.agent.radius + TargetEnemy.enemyScript.agent.radius;
            if (Vector3.Distance(transform.position, TargetEnemy.transform.position) < HitRange)
            {
                PlayAudioOnLocalClient(PikminSoundPackSounds.Attack);
                StartCoroutine(HitEnemyStanding(TargetEnemy));
            }
        }

        public virtual IEnumerator HitEnemyStanding(PikminEnemy Penemy)
        {
            PlayAnimation(animController.AnimPack.EditorStandingAttackAnim);

            yield return new WaitForSeconds(pikminType.AttackRate / 2);

            if (Penemy == null)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Attempted to hit a null enemy");
                yield break; // Exit if the enemy is null
            }

            if (IsOwner && (!IsWildPikmin || IsWildPikmin && LethalMin.WildPikminAttackDamage))
                Penemy.HitEnemyServerRpc(pikminType.GetAttackStrength(CurrentGrowthStage), NetworkObject);

            PlayAudioOnLocalClient(PikminSoundPackSounds.HitSFX, false);
        }

        public void RemoveTargetEnemy()
        {
            if (TargetEnemy != null && AgentLookTarget == TargetEnemy.transform)
            {
                AgentLookTarget = null;
            }
            TargetEnemy = null!;
            if (CurrentTask != null && CurrentTask is AttackEnemyTask)
            {
                RemoveCurrentTask();
            }
        }

        /// <summary>
        /// For latched attacks
        /// </summary>
        [ServerRpc]
        public void StartAttackServerRpc()
        {
            StartAttackClientRpc();
        }
        [ClientRpc]
        public void StartAttackClientRpc()
        {
            if (IsOwner && attackRoutine != null)
            {
                return;
            }

            StartAttackOnLocalClient();
        }
        public void StartAttackOnLocalClient()
        {
            LethalMin.Logger.LogInfo($"{DebugID}: Started attack on local client");
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null!;
            }
            if (attackRoutine == null)
            {
                attackRoutine = StartCoroutine(AttackRoutine());
            }
        }

        IEnumerator AttackRoutine()
        {
            while (CurrentIntention == Pintent.Attack && CurrentLatchTrigger != null)
            {
                PlayAudioOnLocalClient(PikminSoundPackSounds.Attack);
                yield return new WaitForSeconds(pikminType.AttackRate);
                if (IsOwner)
                {
                    HitCurLatchTriggerOnLocalClient();
                    HitCurLatchTriggerServerRpc();
                }
                PlayAudioOnLocalClient(PikminSoundPackSounds.HitSFX, false);
            }
            animController.ResetToIdleAnim();
            attackRoutine = null!;
        }

        [ServerRpc]
        public void HitCurLatchTriggerServerRpc()
        {
            HitCurLatchTriggerClientRpc();
        }
        [ClientRpc]
        public void HitCurLatchTriggerClientRpc()
        {
            if (IsOwner)
            {
                return;
            }
            HitCurLatchTriggerOnLocalClient();
        }
        public void HitCurLatchTriggerOnLocalClient()
        {
            if (CurrentLatchTrigger != null)
            {
                CurrentLatchTrigger.InvokePikminHit(this);
            }
        }
        #endregion






        #region Death and Damage

        /// <summary>
        /// Is called when a pikmin's damage is negated by a hazard because it's resistant to it.
        /// </summary>
        /// <param name="hazard"></param>
        public virtual void OnAvoidHazard(PikminHazard hazard, Object? instance = null)
        {

        }

        public void EditAffecters(string val, float timerAddition = 0, bool remove = false)
        {
            if (DeathTimerAffecters.Contains(val))
            {
                if (remove)
                {
                    LethalMin.Logger.LogInfo($"{DebugID}: Remvoing {val} to deathtimers");
                    DeathTimerAffecters.Remove(val);
                }
            }
            else
            {
                if (!remove)
                {
                    LethalMin.Logger.LogInfo($"{DebugID}: Adding {val} to deathtimers");
                    DeathTimer += timerAddition;
                    DeathTimerAffecters.Add(val);
                }
            }

            OverrideDelay = -1;
        }

        [ServerRpc]
        public void SetInvincibiltyServerRpc(bool val)
        {
            SetInvincibiltyClientRpc(val);
        }
        [ClientRpc]
        public void SetInvincibiltyClientRpc(bool val)
        {
            Invincible = val;
        }

        public void SetRandomRotation()
        {
            RandomRotaion = Quaternion.Euler(enemyRandom.Next(0, 360), enemyRandom.Next(0, 360), enemyRandom.Next(0, 360));
        }

        public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            if (Invincible)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Has Invincible mode when hit");
                DeathSnapToPos = null!;
                return;
            }

            if (IsDeadOrDying)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Hit While Dead or Dying");
                return;
            }

            if (previousLeader != null && playerWhoHit == previousLeader.Controller && CurrentIntention == Pintent.Attack && !friednlyFire)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Hit by previous leader in attack state, ignoring hit");
                DeathSnapToPos = null!;
                return;
            }

            if (leader != null && playerWhoHit != null && playerWhoHit == leader.Controller && !friednlyFire)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: FriendlyFire mode");
                DeathSnapToPos = null!;
                return;
            }

            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);

            if (IsWildPikmin && LethalMin.WildPikminNoDeath)
            {
                Vector3 KnockbackDirB = new Vector3(-transform.forward.x * 2, 3, -transform.forward.z * 2);
                if (playerWhoHit != null)
                {
                    Vector3 playerDir = transform.position - playerWhoHit.transform.position;
                    KnockbackDirB = new Vector3(playerDir.normalized.x * 2, 3, playerDir.normalized.z * 2);
                }
                DeathSnapToPos = null!;
                CanGetBackUp = true;
                ApplyKnockBack(KnockbackDirB, 3 + force);
                return;
            }

            if (DeathSnapToPos != null)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: is snapped to position");
                if (enemyHP - force <= 0)
                {
                    hitBeforeKillWasCalled = true;
                    KillEnemy();
                    PlayAnimation(animController.AnimPack.EditorLayingAnim);
                    PlayAudioOnLocalClient(PikminSoundPackSounds.Hurt);
                }
                else
                {
                    Vector3 FUKnockbackDir = transform.position - DeathSnapToPos.transform.position;
                    ApplyKnockBack(-FUKnockbackDir, 3);
                    DeathSnapToPos = null!;
                }
                return;
            }

            LethalMin.Logger.LogInfo($"{DebugID} was hit: {force}");

            Vector3 KnockbackDir = new Vector3(-transform.forward.x * 2, 3, -transform.forward.z * 2);
            if (playerWhoHit != null)
            {
                Vector3 playerDir = transform.position - playerWhoHit.transform.position;
                KnockbackDir = new Vector3(playerDir.normalized.x * 2, 3, playerDir.normalized.z * 2);
            }
            LandBuffer = 0.25f;

            enemyHP -= force;

            LethalMin.Logger.LogInfo($"{DebugID} HP left: {enemyHP}");

            if (enemyHP > 0)
            {
                ApplyKnockBack(KnockbackDir, 3 + force);
            }
            else
            {
                LandBuffer = 0.1f;
                ApplyKnockBack(KnockbackDir, force);
                hitBeforeKillWasCalled = true;
                KillEnemy(false);
            }
        }


        /// <summary>
        /// Applies knockback to the pikmin, and harms it depending on several values.
        /// Should be called on the owner.
        /// </summary>
        /// <param name="explosionPosition">The explosion's origon</param>
        /// <param name="killRange">The range where the pikmin will die no matter what</param>
        /// <param name="damageRange">The range where the pikmin will lose HP</param>
        /// <param name="nonLethalDamage">The ammount of HP the pikmin looses (normalized 10 = 1, 100 = 10)</param>
        public void HitFromExplosionAndSync(Vector3 explosionPosition, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0)
        {
            if (IsServer)
            {
                HitFromExplosionClientRpc(explosionPosition, killRange, damageRange, nonLethalDamage, physicsForce);
            }
            else
            {
                HitFromExplosionServerRpc(explosionPosition, killRange, damageRange, nonLethalDamage, physicsForce);
            }
        }
        [ServerRpc]
        public void HitFromExplosionServerRpc(Vector3 explosionPosition, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0)
        {
            HitFromExplosionClientRpc(explosionPosition, killRange, damageRange, nonLethalDamage, physicsForce);
        }
        [ClientRpc]
        public void HitFromExplosionClientRpc(Vector3 explosionPosition, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0)
        {
            HitFromExplosion(explosionPosition, killRange, damageRange, nonLethalDamage, physicsForce);
        }
        public void HitFromExplosion(Vector3 explosionPosition, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0)
        {
            if (Invincible)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Has Invincible mode when hit from explosion");
                return;
            }

            if (IsDeadOrDying)
            {
                return;
            }

            if (PikChecks.IsPikminResistantToHazard(pikminType, PikminHazard.Explosive))
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Pikmin is resistant to explosive hazard, avoiding damage");
                OnAvoidHazard(PikminHazard.Explosive);
                return;
            }

            float distance = Vector3.Distance(transform.position, explosionPosition);

            // Calculate direction away from explosion for knockback
            Vector3 knockbackDirection = transform.position - explosionPosition;
            knockbackDirection.Normalize();

            // Create a knockback force that sends the Pikmin away from the explosion center and upward
            Vector3 knockbackDir = new Vector3(knockbackDirection.x * 3, 3, knockbackDirection.z * 3);
            LandBuffer = 0.25f;

            // Check if within kill range - instant death
            if (distance < killRange)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Killed instantly by explosion - within kill range ({distance}/{killRange})");
                enemyHP = 0;
                ApplyKnockBack(knockbackDir, physicsForce + 4);
                hitBeforeKillWasCalled = true;
                KillEnemy(false);
                return;
            }

            // Calculate damage based on distance relative to damage range
            if (distance <= damageRange)
            {
                // Calculate normalized distance for damage scaling
                float normalizedDistance = distance / damageRange;

                // Calculate damage - more damage when closer to the explosion
                // Use nonLethalDamage parameter to scale the damage
                int damage = Mathf.CeilToInt((1 - normalizedDistance) * (nonLethalDamage / 100f) * 5);
                if (damage <= 0)
                {
                    damage = 0;
                }

                enemyHP -= damage;

                LethalMin.Logger.LogInfo($"{DebugID}: Hit from explosion (HP: {enemyHP}, Damage: {damage}, " +
                                       $"Distance: {distance}, NDist: {normalizedDistance} pForce: {physicsForce})");

                // Apply knockback force proportional to damage/distance
                float knockbackMultiplier = physicsForce + (5 * (1 - normalizedDistance));

                if (enemyHP > 0)
                {
                    ApplyKnockBack(knockbackDir, knockbackMultiplier);
                }
                else
                {
                    ApplyKnockBack(knockbackDir, knockbackMultiplier);
                    hitBeforeKillWasCalled = true;
                    KillEnemy(false);
                }
            }
        }


        [ServerRpc]
        public void DoSquishDeathServerRpc()
        {
            DoSquishDeathClientRpc();
        }
        [ClientRpc]
        public void DoSquishDeathClientRpc(bool DoOwnerCheck = true)
        {
            if (!IsOwner || !DoOwnerCheck)
                DoSquishDeath();
        }
        public void DoSquishDeath()
        {
            if (Invincible)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Has Invincible mode when doing squish death");
                return;
            }

            if (IsDeadOrDying)
            {
                return;
            }

            if (IsWildPikmin && LethalMin.WildPikminNoDeath)
            {
                Vector3 KnockbackDirB = new Vector3(-transform.forward.x * 2, 3, -transform.forward.z * 2);
                DeathSnapToPos = null!;
                CanGetBackUp = true;
                ApplyKnockBack(KnockbackDirB, 3);
                return;
            }

            LethalMin.Logger.LogInfo($"{DebugID}: Squished to death");
            hitBeforeKillWasCalled = true;
            PlayAudioOnLocalClient(PikminSoundPackSounds.Crush);
            PlayAudioOnLocalClient(PikminSoundPackSounds.CrushSFX, false);
            modelContainer.transform.localScale = new Vector3(modelContainer.transform.localScale.x, 0.1f, modelContainer.transform.localScale.z);
            KillEnemy();
        }

        [ServerRpc]
        public void DoZapDeathServerRpc()
        {
            DoZapDeathClientRpc();
        }
        [ClientRpc]
        public void DoZapDeathClientRpc(bool DoOwnerCheck = true)
        {
            if (!IsOwner || !DoOwnerCheck)
                DoZapDeath();
        }

        public void DoZapDeath()
        {
            if (Invincible)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Has Invincible mode when doing zap death");
                return;
            }

            if (IsDeadOrDying)
            {
                return;
            }

            if (IsWildPikmin && LethalMin.WildPikminNoDeath)
            {
                Vector3 KnockbackDirB = new Vector3(-transform.forward.x * 2, 3, -transform.forward.z * 2);
                DeathSnapToPos = null!;
                CanGetBackUp = true;
                ApplyKnockBack(KnockbackDirB, 3);
                return;
            }

            LethalMin.Logger.LogInfo($"{DebugID}: Zapped to death");
            hitBeforeKillWasCalled = true;
            PikminZap zap = Instantiate(LethalMin.PikminZapPrefab, transform.position, transform.rotation).GetComponent<PikminZap>();
            EnableEnemyMesh(enable: false, overrideDoNotSet: true);
            zap.LostType = pikminType;
            zap.InMemoryof = DebugID;
            zap.ghostRandom = enemyRandom;
            isEnemyDead = true;
            enemyHP = 0;
            Pmanager.EndOfGameStats.PikminKilled++;
            Pmanager.FiredStats.TotalPikminLost++;
            if (NetworkObject.IsSpawned)
                IncrumentDestoryCountServerRpc();
        }




        public override void KillEnemy(bool destroy = false)
        {
            if (Invincible)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Has Invincible mode when trying to kill");
                return;
            }

            if (IsWildPikmin && LethalMin.WildPikminNoDeath)
            {
                Vector3 KnockbackDirB = new Vector3(-transform.forward.x * 2, 3, -transform.forward.z * 2);
                DeathSnapToPos = null!;
                CanGetBackUp = true;
                ApplyKnockBack(KnockbackDirB, 3);
                return;
            }

            if (destroy && IsServer)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Died, but due to the way zeekees programmed it we need to do dis first");
                SpawnGhostClientRpc();
                base.KillEnemy(destroy);
                return;
            }

            base.KillEnemy(destroy);

            LethalMin.Logger.LogInfo($"{DebugID}: Died");

            if (pikminType.DamageDeltUponDeath > 0)
            {
                foreach (PikminEnemy Penemy in PikminManager.instance.PikminEnemies)
                {
                    if (Vector3.Distance(transform.position, Penemy.transform.position) < pikminType.DeathDamageRange)
                    {
                        Penemy.HitEnemy(pikminType.DamageDeltUponDeath * pikminType.GetAttackStrength(CurrentGrowthStage));
                    }
                }
            }

            CallResetMethods(
                RemoveLeader: true,
                DropItem: true,
                RemoveEnemy: true,
                CollisionMode: -1,
                Unlatch: true,
                RemoveTask: true,
                RemoveOverridePositions: true,
                SetLayingFalse: false
            );

            if (!hitBeforeKillWasCalled)
            {
                PlayAnimation(animController.AnimPack.EditorLayingAnim);
                PlayAudioOnLocalClient(PikminSoundPackSounds.Hurt);
            }

            CurrentIntention = Pintent.Dead;
            PreviousIntention = Pintent.Dead;

            if (OverrideDelay > 0)
            {
                StartCoroutine(SpawnGhostAfterDelay(OverrideDelay));
            }
            else
            {
                StartCoroutine(SpawnGhostAfterDelay());
            }
        }

        [ServerRpc]
        public void SpawnGhostServerRpc()
        {
            SpawnGhostClientRpc();
        }

        [ClientRpc]
        public void SpawnGhostClientRpc()
        {
            SpawnGhost();
        }

        public void SpawnGhost()
        {
            LethalMin.Logger.LogInfo($"{DebugID}: Spawning Ghost");
            PikminGhost ghost = GameObject.Instantiate(LethalMin.PikminGhostPrefab, transform.position, transform.rotation).
            GetComponent<PikminGhost>();
            ghost.LostType = pikminType;
            ghost.InMemoryof = DebugID;
            ghost.ghostRandom = enemyRandom;
            Pmanager.EndOfGameStats.PikminKilled++;
            Pmanager.FiredStats.TotalPikminLost++;
            if (NetworkObject.IsSpawned)
                IncrumentDestoryCountServerRpc();
        }

        IEnumerator SpawnGhostAfterDelay(float delay = 2)
        {
            delay += enemyRandom.Next(1, 10) / 10f;
            LethalMin.Logger.LogDebug($"{DebugID}: Spawning Ghost after, {delay} seconds");
            yield return new WaitForSeconds(delay);
            SpawnGhost();
        }

        int destoryCounter = 0;
        [ServerRpc(RequireOwnership = false)]
        public void IncrumentDestoryCountServerRpc()
        {
            destoryCounter++;
            LethalMin.Logger.LogDebug($"{DebugID}: {destoryCounter} - {StartOfRound.Instance.connectedPlayersAmount + 1}");
            if (destoryCounter >= StartOfRound.Instance.connectedPlayersAmount + 1)
            {
                NetworkObject.Despawn();
            }
        }
        #endregion






        #region Growth
        private bool HasNoGrowthStages;
        private Dictionary<int, List<GameObject>> growthObjects = new Dictionary<int, List<GameObject>>();
        public void GetGrowthObjects()
        {
            PikminModelRefernces modelReferences = GetComponentInChildren<PikminModelRefernces>();
            if (modelReferences.growthObjectsCache == null)
            {
                growthObjects = new Dictionary<int, List<GameObject>>();
            }
            else
            {
                growthObjects = modelReferences.GetGrowthObjects();
                LethalMin.Logger.LogDebug($"{DebugID}: Using Cached growth objects");
                return;
            }
            Dictionary<int, List<GameObject>> growthObjectsf = new Dictionary<int, List<GameObject>>();
            growthObjects.Clear();
            MaxGrowthStage = 0;

            void ProcessPlants(List<GameObject> plants, int generationOffset = 0)
            {
                foreach (GameObject go in plants)
                {
                    if (go == null)
                    {
                        LethalMin.Logger.LogError($"{DebugID}: Growth object is null");
                        continue;
                    }

                    int index = plants.IndexOf(go);
                    if (!growthObjects.ContainsKey(index))
                    {
                        growthObjects[index] = new List<GameObject>();
                    }
                    growthObjects[index].Add(go);

                    if (index > MaxGrowthStage)
                        MaxGrowthStage = index;

                    LethalMin.Logger.LogDebug($"{DebugID}: Found plant {go.name} at index {index}");
                }
            }

            void ProcessPlants2(List<GameObject> plants, int generationOffset = 0)
            {
                foreach (GameObject go in plants)
                {
                    if (go == null)
                    {
                        LethalMin.Logger.LogError($"{DebugID}: Growth object is null in prefab");
                        continue;
                    }

                    int index = plants.IndexOf(go);
                    if (!growthObjectsf.ContainsKey(index))
                    {
                        growthObjectsf[index] = new List<GameObject>();
                    }
                    growthObjectsf[index].Add(go);
                }
            }

            // Process base plants
            ProcessPlants(modelReferences.Plants);

            // Process plants from generations
            foreach (PikminModelGeneration gen in modelReferences.Generations)
            {
                ProcessPlants(gen.Plants);
            }

            HasNoGrowthStages = growthObjects.Count == 0;

            if (HasNoGrowthStages)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: No growth stages found");
            }
            else
            {
                //Cache growth objects for next instances' use
                if (pikminType.CachePlantObjectRefernces)
                {
                    LethalMin.Logger.LogDebug($"{DebugID}: Caching growth objects");
                    PikminModelRefernces modelRefs = pikminType.modelRefernces;
                    // Process base plants
                    ProcessPlants2(modelRefs.Plants);

                    // Process plants from generations
                    foreach (PikminModelGeneration gen in modelRefs.Generations)
                    {
                        ProcessPlants2(gen.Plants);
                    }

                    modelRefs.SetGrowthObjects(growthObjectsf);
                    LethalMin.Logger.LogDebug($"{DebugID}: Caching growth objects done");
                }

                LethalMin.Logger.LogDebug($"{DebugID}: Found {growthObjects.Count} plants across {MaxGrowthStage + 1} stages");
            }
        }

        public void SetGrowth(int Stage)
        {
            if (HasNoGrowthStages)
            {
                return;
            }
            if (Stage < 0 || Stage > MaxGrowthStage)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Invalid growth stage {Stage}");
                return;
            }
            LethalMin.Logger.LogInfo($"{DebugID}: Setting growth stage to {Stage}");
            CurrentGrowthStage = Stage;
            CurrentCarryStrength = pikminType.GetCarryStrength(Stage);
            foreach (var pair in growthObjects)
            {
                foreach (var go in pair.Value)
                {
                    go.SetActive(pair.Key == Stage);
                }
            }
        }

        [ServerRpc]
        public void SetGrowthServerRpc(int Stage)
        {
            SetGrowthClientRpc(Stage);
        }

        [ClientRpc]
        public void SetGrowthClientRpc(int Stage)
        {
            if (HasNoGrowthStages)
            {
                return;
            }
            SetGrowth(Stage);
        }
        #endregion






        #region Items
        public virtual PikminItem? GetClosestPikminItem(float overrideDetectionRadius = -1)
        {
            float detectionRadius = overrideDetectionRadius == -1 ? pikminType.ItemDetectionRange : overrideDetectionRadius;
            PikminItem? bestCandidate = null;
            float bestDistance = float.MaxValue;
            bool bestIsNotCarried = false;

            foreach (PikminItem item in Pmanager.PikminItems)
            {
                // Skip invalid items immediately
                if (item == null || item.ItemScript == null || item.settings.ExcludeFromGetItemsCheck || !item.settings.GrabableToPikmin)
                {
                    continue;
                }

                if(LethalMin.OnCompany && LethalMin.IgnoreNonScrapItemsToCompany && !item.ItemScript.itemProperties.isScrap)
                {
                    continue;
                }

                if (LethalMin.ItemBlacklistConfig.InternalValue.Contains(item.ItemScript.itemProperties.itemName))
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, item.ItemScript.transform.position);
                if (distance > detectionRadius)
                {
                    continue;
                }

                if (!PikChecks.IsPikminItemValid(item))
                {
                    continue;
                }

                bool isNotCarried = !item.IsBeingCarried;

                // Take this item if it's not carried and our best so far is carried,
                // or if both have same carried status but this one is closer
                if ((isNotCarried && !bestIsNotCarried) ||
                    (isNotCarried == bestIsNotCarried && distance < bestDistance))
                {
                    bestCandidate = item;
                    bestDistance = distance;
                    bestIsNotCarried = isNotCarried;
                }
            }

            return bestCandidate;
        }

        [ServerRpc]
        public void SetPikminToItemServerRpc(NetworkObjectReference Ref, int GrabPosIndex)
        {
            SetPikminToItemClientRpc(Ref, GrabPosIndex);
        }

        [ClientRpc]
        public void SetPikminToItemClientRpc(NetworkObjectReference Ref, int GrabPosIndex)
        {
            if (IsOwner)
            {
                return;
            }
            if (!Ref.TryGet(out NetworkObject obj))
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to get NetworkObject from reference {Ref}. Cannot set pikmin to item.");
                return;
            }
            PikminItem itm = obj.GetComponentInChildren<PikminItem>();
            if (itm != null)
            {
                SetPikminToItemLocalClient(itm, GrabPosIndex);
            }
            else
            {
                LethalMin.Logger.LogError($"{DebugID}: Item is null when trying to set to it from reference {Ref}");
            }
        }

        public void SetPikminToItemLocalClient(PikminItem itm, int GrabPosIndex)
        {
            if (itm == null)
            {
                SetToIdle();
                LethalMin.Logger.LogError($"{DebugID}: Item is null when trying to set to it");
                return;
            }
            BoolValue boolVal = itm.GrabToPositions[GrabPosIndex];
            if (boolVal == null)
            {
                SetToIdle();
                LethalMin.Logger.LogError($"{DebugID}: Item grab pos is null when trying to set to it");
                return;
            }

            LethalMin.Logger.LogInfo($"{DebugID}: targeting item {itm.gameObject.name} at {boolVal.gameObject.name}");
            boolVal.value = true;
            TargetItem = itm;
            AgentLookTarget = itm.transform;
            TargetItemPoint = boolVal;
            SetCurrentTask("CarryItem");
            SwitchToBehaviourStateOnLocalClient(WORK);
            ChangeIntent(Pintent.RunTowards);
        }

        [ServerRpc]
        public void GrabItemServerRpc()
        {
            GrabItemClientRpc();
        }
        [ClientRpc]
        public void GrabItemClientRpc()
        {
            if (!IsOwner)
                GrabItemOnLocalClient(TargetItem!);
        }

        public void GrabItemOnLocalClient(PikminItem itm)
        {
            if (itm == null)
            {
                SetToIdle();
                LethalMin.Logger.LogError($"{DebugID}: Item is null when trying to grab it");
                return;
            }


            itm.PikminOnItem.Add(this);
            TargetItem = itm;
            ChangeIntent(Pintent.Waiting);

            if (itm.IsBeingCarried)
            {
                SetAsCarryingItem(false);
            }

            LethalMin.Logger.LogInfo($"{DebugID}: Grabbed item {itm.gameObject.name} Speed is now: {itm.GetSpeed(true)}");
        }

        public void SetAsCarryingItem(bool cullAudio = true)
        {
            if (TargetItem!.PrimaryPikminOnItem != this)
                SetCollisionMode(0);
            AgentLookTarget = null;
            ChangeIntent(Pintent.Carry);
            float vol = TargetItem.PikminOnItem.Count == 0 || !cullAudio ? 1.0f : 1.0f / TargetItem.PikminOnItem.Count;
            PlayAudioOnLocalClient("ItemLift", true, vol);
        }
        public void UnsetAsCarryingItem()
        {
            if (TargetItem == null)
            {
                DropItemOnLocalClient();
                return;
            }
            SetCollisionMode(1);
            AgentLookTarget = TargetItem.transform;
            ChangeIntent(Pintent.Waiting);
        }


        [ServerRpc]
        public void DropItemServerRpc()
        {
            DropItemClientRpc();
        }
        [ClientRpc]
        public void DropItemClientRpc()
        {
            DropItemOnLocalClient();
        }

        /// <summary>
        /// Makes the pikmin disasociate itself from it's target item. if they are not null.
        /// Sets TargetItem and TargetItemPoint to null. even if they are already null.
        /// </summary>
        public void DropItemOnLocalClient()
        {
            if (TargetItem != null && AgentLookTarget == TargetItem.transform)
            {
                AgentLookTarget = null;
            }
            if (TargetItem != null && TargetItem.PikminOnItem.Contains(this))
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Dropped item {TargetItem.gameObject.name}");
                SetCollisionMode(1);
                if (IsOwner)
                {
                    agent.Warp(TargetItemPoint.transform.position);
                }
                else
                {
                    transform2.TeleportOnLocalClient(RoundManager.Instance.GetNavMeshPosition(TargetItemPoint.transform.position));
                }
                TargetItem.PikminOnItem.Remove(this);
                if (CurrentIntention == Pintent.Carry || CurrentIntention == Pintent.Waiting)
                {
                    //ChangeIntent(Pintent.Idle);
                }
            }

            TargetItem = null!;

            if (TargetItemPoint != null)
            {
                TargetItemPoint.value = false;
            }
            TargetItemPoint = null!;
        }
        #endregion






        #region Routing
        [ServerRpc]
        public void UseEntranceServerRpc(NetworkObjectReference Ref, bool Inside, bool PlaySFX = true)
        {
            UseEntranceClientRpc(Ref, Inside, PlaySFX);
        }
        [ClientRpc]
        public void UseEntranceClientRpc(NetworkObjectReference Ref, bool Inside, bool PlaySFX = true)
        {
            if (Ref.TryGet(out NetworkObject obj) && obj.TryGetComponent(out EntranceTeleport entrance))
            {
                UseEntranceOnLocalClient(entrance, Inside, PlaySFX);
            }
            else
            {
                LethalMin.Logger.LogError($"{DebugID}: Could not find entrance");
            }
        }
        public virtual void UseEntranceOnLocalClient(EntranceTeleport entrance, bool Inside, bool PlaySFX = true)
        {
            if (entrance == null)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Entrance is null");
                return;
            }

            LethalMin.Logger.LogInfo($"{DebugID}: Using entrance {entrance.name}");
            if (entrance.exitPoint == null)
            {
                entrance.FindExitPoint();
                LethalMin.Logger.LogWarning($"{DebugID}: Entrance exit point was null, trying to find it again");
            }
            if (entrance.exitPoint == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Entrance exit point is null, cannot use entrance (blocked???)");
                return;
            }

            if (PlaySFX)
            {
                entrance.PlayAudioAtTeleportPositions();
            }

            if (IsOwner && agent.enabled)
            {
                agent.Warp(entrance.exitPoint.position);
            }
            transform2.TeleportOnLocalClient(entrance.exitPoint.position);

            isOutside = !Inside;
        }

        public virtual void WarpToMatchLeaderDoors(bool isInside)
        {
            if (leader == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Leader is null when trying to warp to match leader doors");
                return;
            }

            if (IsOwner)
            {
                agent.Warp(leader.transform.position);
            }
            transform2.TeleportOnLocalClient(leader.transform.position, leader.transform.rotation);
            SetOutsideBool(!isInside);
        }


        public void SetOutsideBool(bool value)
        {
            isOutside = value;
        }
        #endregion






        #region Audio
        public void PlayAndSyncAudio(PikminSoundPackSounds sound, bool PlayOnVoice = true)
        {
            PlayAndSyncAudio(sound.ToString(), PlayOnVoice);
        }
        public void PlayAndSyncAudio(string Name, bool PlayOnVoice = true)
        {
            if (!IsOwner)
            {
                LethalMin.Logger.LogError($"{DebugID}: Attempted to play and sync audio on a client");
                return;
            }
            if (!CurrentSoundPack.IsKeyValid(Name))
            {
                LethalMin.Logger.LogError($"{DebugID}: attmpted to play an invalid sound");
                return;
            }
            PlayAudioOnLocalClient(Name, PlayOnVoice);
            if (IsServer)
            {
                PlayAudioClientRpc(Name, PlayOnVoice);
            }
            else
            {
                PlayAudioServerRpc(Name, PlayOnVoice);
            }
        }
        [ServerRpc]
        public void PlayAudioServerRpc(string Name, bool PlayOnVoice)
        {
            PlayAudioClientRpc(Name, PlayOnVoice);
        }
        [ClientRpc]
        public void PlayAudioClientRpc(string Name, bool PlayOnVoice)
        {
            if (!IsOwner)
                PlayAudioOnLocalClient(Name, PlayOnVoice);
        }
        public void PlayAudioOnLocalClient(PikminSoundPackSounds sounds, bool PlayOnVoice = true)
        {
            PlayAudioOnLocalClient(sounds.ToString(), PlayOnVoice);
        }
        public void PlayAudioOnLocalClient(string Name, bool PlayOnVoice = true, float overrideVolume = -1)
        {
            if (!CurrentSoundPack.IsKeyValid(Name, false))
            {
                LethalMin.Logger.LogWarning($"{DebugID}: attempted to play an invalid sound ({Name}), most likely due to having no sounds for it.");
                CurrentPlayingKey = "";
                return;
            }

            CurrentPlayingKey = Name;

            // Calculate the volume based on nearby Pikmin
            float volume = overrideVolume == -1f ? CalculateAdjustedVolume(Name) : overrideVolume;

            AudioSource sourceToUse = PlayOnVoice ? creatureVoice : creatureSFX;
            AudioClip clipToPlay = CurrentSoundPack.PullSoundFromDict(Name, enemyRandom);

            // Play the sound with adjusted volume
            sourceToUse.PlayOneShot(clipToPlay, volume);
            WalkieTalkie.TransmitOneShotAudio(sourceToUse, clipToPlay, volume);
            if (!LethalMin.DontMakeAudibleNoisesCheat)
                RoundManager.Instance.PlayAudibleNoise(transform.position, 10, volume, 0, IsInShip && StartOfRound.Instance.hangarDoorsClosed, PikminSoundID);
        }

        private float CalculateAdjustedVolume(string soundName)
        {
            int nearbyCount = 0;
            foreach (PikminAI pikmin in Pmanager.PikminAIs)
            {
                if (pikmin == null)
                {
                    continue;
                }

                if (pikmin != this && Vector3.Distance(transform.position, pikmin.transform.position) <= maxAudioClusterDistance
                && (pikmin.creatureSFX.isPlaying || pikmin.creatureVoice.isPlaying) && pikmin.CurrentPlayingKey == soundName)
                {
                    nearbyCount++;
                }
            }

            // Calculate volume: starts at 1, linearly decreases to 0 as nearbyCount approaches maxPikmin
            float volume = Mathf.Clamp01(1f - (float)nearbyCount / maxAudioClusterPikmin);
            return volume;
        }


        IEnumerator PlayDelayedSound(string soundName, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayAudioOnLocalClient(soundName);
        }
        #endregion






        #region Animation
        public void PlayAnimation(AnimationClip? animation, float transitionDuration = 0.25f, int layer = 0)
        {
            if (animation == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Attempted to play a null animation");
                return;
            }
            animController.PlayAnimation(animation.name, transitionDuration, layer);
        }
        IEnumerator DelayMovement(float val)
        {
            agent.updateRotation = false;
            agent.isStopped = true;
            yield return new WaitForSeconds(val);
            agent.updateRotation = true;
            agent.isStopped = false;
        }
        IEnumerator PlayDelayedAnimation(string animationName, float delay)
        {
            yield return new WaitForSeconds(delay);
            animController.PlayAnimation(animationName, 0);
        }

        float IdleAnimTimer = 0f;
        public void UpdateIdleAnimation()
        {
            if (animController.AnimPack.UseStateMachineForIdleAnims)
            {
                creatureAnimator.SetInteger("RandomIdle", animController.RandomIdle);
            }
            else if (!PikUtils.IsOutOfRange(animController.AnimPack.EditorIdleAnim, animController.RandomIdle))
            {
                animController.IdleAnim = animController.AnimPack.EditorIdleAnim[animController.RandomIdle];
            }
            else
            {
                LethalMin.Logger.LogWarning($"{DebugID}: RandomIdle is out of range: {animController.RandomIdle} / {animController.AnimPack.EditorIdleAnim.Count}");
            }

            if (currentBehaviourStateIndex != IDLE || CurrentIntention != Pintent.Idle)
            {
                if (animController.AnimPack.EditorIdleAnim.Count != 0)
                    animController.IdleAnim = animController.AnimPack.EditorIdleAnim[0];
                animController.RandomIdle = 0;
                return;
            }
            if (IdleAnimTimer > 0)
            {
                IdleAnimTimer -= Time.deltaTime;
            }
            else
            {
                IdleAnimTimer = enemyRandom.Next(5, 60);
                animController.RandomIdle = enemyRandom.Next(animController.AnimPack.EditorIdleAnim.Count);
                //LethalMin.Logger.LogInfo($"{DebugID}: Random idle animation: {animController.RandomIdle} new timer: {IdleAnimTimer}");
            }
        }

        public virtual void DoYay(bool OverrideVol = false)
        {
            if (OverrideVol)
            {
                PlayAudioOnLocalClient("Yay", true, 1);
            }
            else
            {
                PlayAudioOnLocalClient(PikminSoundPackSounds.Yay);
            }
            if (animController.AnimPack.EditorYayAnim != null && animController.AnimPack.EditorYayAnim.Count != 0)
                PlayAnimation(animController.AnimPack.EditorYayAnim[enemyRandom.Next(animController.AnimPack.EditorYayAnim.Count)]);
        }

        public void SetPikminOnToAnimLink(PikminLinkAnimation anim, bool Reversed = false)
        {
            if (CurrentLinkAnim != null)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: is attempting to link onto {anim.gameObject.name}" +
                $" while already linked onto {CurrentLinkAnim.gameObject.name}");
                return;
            }
            LethalMin.Logger.LogDebug($"{DebugID}: Is linking onto {anim.gameObject.name}");

            anim.PikminOnLink.Add(this, 0);
            CurrentLinkAnim = anim;
            playLinkedAnimReversed = Reversed;

            SetCollisionMode(0);
        }
        public void AnimateOnLink()
        {
            if (!CurrentLinkAnim.PikminOnLink.ContainsKey(this))
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Is on link that does not contain it!");
                RemoveFromLink();
                return;
            }

            CurrentLinkAnim.PikminOnLink[this] += Time.deltaTime * CurrentLinkAnim.AnimSpeedMultiplier;

            float t = CurrentLinkAnim.PikminOnLink[this];
            Vector3 ConfigScale = new Vector3(LethalMin.PikminScale.InternalValue, LethalMin.PikminScale.InternalValue, LethalMin.PikminScale.InternalValue);

            if (playLinkedAnimReversed)
            {
                // Use 1-t to reverse the movement
                float reversedT = 1 - t;
                transform.position = CurrentLinkAnim.GetPointOnPath(reversedT);
                transform.rotation = CurrentLinkAnim.GetRotationOnPath(reversedT);
                transform.localScale = Vector3.Lerp(ConfigScale, CurrentLinkAnim.StartPoint.localScale, t * 0.5f);
            }
            else
            {
                transform.position = CurrentLinkAnim.GetPointOnPath(t);
                transform.rotation = CurrentLinkAnim.GetRotationOnPath(t);
                transform.localScale = Vector3.Lerp(CurrentLinkAnim.StartPoint.localScale, ConfigScale, t * 2f);
            }

            if (CurrentLinkAnim.PikminOnLink[this] >= 1)
            {
                RemoveFromLink();
            }
        }

        public void RemoveFromLink()
        {
            if (IsServer && TargetOnion != null && TargetOnion.AllClimbLinks.Contains(CurrentLinkAnim))
            {
                Pmanager.DespawnPikmin(this);
            }
            Vector3 ConfigScale = new Vector3(LethalMin.PikminScale.InternalValue, LethalMin.PikminScale.InternalValue, LethalMin.PikminScale.InternalValue);

            if (CurrentLinkAnim == null)
            {
                return;
            }

            SetCollisionMode(1);

            if (IsOwner)
            {
                transform.localScale = ConfigScale;
                agent.Warp(CurrentLinkAnim.EndPoint.position);
            }
            else
            {
                transform2.TeleportOnLocalClient(CurrentLinkAnim.EndPoint.position, CurrentLinkAnim.EndPoint.rotation, ConfigScale);
            }

            CurrentLinkAnim.PikminOnLink.Remove(this);
            CurrentLinkAnim = null!;
        }
        #endregion






        #region Puffmin Transformation
        [ServerRpc(RequireOwnership = false)]
        public void TransformIntoPuffminServerRpc()
        {
            //TransformIntoPuffminClientRpc();
            PuffminAI ai = Instantiate(LethalMin.PuffminEnemyType.enemyPrefab, transform.position, transform.rotation).GetComponent<PuffminAI>();
            ai.NetworkObject.Spawn();
            ai.SetPuffminDataClientRpc(GetPikminData());
            NetworkObject.Despawn(this);
        }
        [ClientRpc]
        public void TransformIntoPuffminClientRpc()
        {
            TransformIntoPuffmin();
        }

        public void TransformIntoPuffmin()
        {

        }
        #endregion
    }
}