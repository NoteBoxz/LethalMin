using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using LethalLib.Modules;
using LethalMin.Pikmin;
using LethalMin.Utils;
using LethalMon;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public enum PuffminIntent
    {
        Idle,
        Held,
        Thrown,
        Knockback,
        LatchedOn,
        Dead
    }
    public class PuffminAI : EnemyAI, IGenerationSwitchable
    {
        public PuffminIntent currentIntent = PuffminIntent.Idle;
        public Transform MeshContainer = null!;
        public PikminNetworkTransfrom transform2 = null!;
        public Rigidbody rb = null!;
        public Collider Pcollider = null!;
        public GameObject PuffminMeshes = null!;
        public PikminData? OrignalPikmin = null;
        public PuffminLeader? Leader = null;
        public PuffminLeader? PreviousLeader = null;
        public PlayerControllerB? PlayerChaseing = null;
        public Leader? LeaderLatchedTo = null;
        public float AttackCooldown = 0.5f;
        public float StunTimer = 1.5f;
        public float KnockbackLandBuffer = 0.5f;
        public float WhistleBuffer = 0;
        public bool IsDeadOrDying => currentIntent == PuffminIntent.Knockback || isEnemyDead;
        public bool IsAirborn => currentIntent == PuffminIntent.Held || currentIntent == PuffminIntent.Thrown || currentIntent == PuffminIntent.Knockback;
        public string DebugID = "<ID not set>";
        public System.Random enemyRandom = null!;
        public Renderer CurBodyRenderer = null!;
        public AudioClip HitSound = null!;
        public AudioClip AttackVoice = null!;
        public AudioClip NoticeVoice = null!;
        public AudioClip HoldVoice = null!;
        public AudioClip ThrowVoice = null!;
        public AudioClip HurtVoice = null!;
        public List<GameObject> TempObjects = new List<GameObject>();
        float timeOffNavMesh;
        Coroutine latchedRoutine = null!;


        #region Initalizeation
        [ClientRpc]
        public void SetPuffminDataClientRpc(PikminData data)
        {
            OrignalPikmin = data;
        }

        public override void Start()
        {
            base.Start();

            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);

            if (OrignalPikmin == null)
            {
                LethalMin.Logger.LogWarning($"LethalMin: OrignalPikmin is null for {DebugID}! Using random...");
                int RandomTypeID = enemyRandom.Next(0, LethalMin.RegisteredPikminTypes.Count);
                string nme = LethalMin.GetPikminTypeByID(RandomTypeID).PikminName;
                OrignalPikmin = new PikminData(RandomTypeID, -1, 0, $"{nme}_{PikUtils.GenerateRandomString(enemyRandom)}", $"{System.DateTime.Now}");
            }

            DebugID = "(Puffmin)" + OrignalPikmin.Value.DebugName;

            gameObject.name = DebugID;

            Instantiate(PuffminMeshes, MeshContainer);

            PikminManager.instance.PuffminAIs.Add(this);

            SwitchGeneration(PikUtils.ConvertCfgGenerationToPikminGeneration(LethalMin.PuffminGeneration.InternalValue));

            SetCollisionMode(1);

            DoTransformAnimation();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            PikminManager.instance.PuffminAIs.Remove(this);
            UnLatchPuffmin();
            RemoveLeader();
            foreach (GameObject temp in TempObjects)
            {
                if (temp == null)
                {
                    continue;
                }
                LethalMin.Logger.LogInfo($"{DebugID}: Destroying temp object {temp.name}");
                Destroy(temp);
            }
        }

        public void SwitchGeneration(PikminGeneration generation)
        {
            if (LethalMin.RandomizeGenerationModels.InternalValue)
                generation = (PikminGeneration)enemyRandom.Next(0, System.Enum.GetValues(typeof(PikminGeneration)).Length);

            PuffminModelRefernces modelRefernces = GetComponentInChildren<PuffminModelRefernces>();
            bool hasFound = false;

            void applyGeneration(PuffminModelGeneration gen)
            {
                creatureAnimator = gen.Animator;
                skinnedMeshRenderers = gen.Model.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                meshRenderers = gen.Model.gameObject.GetComponentsInChildren<MeshRenderer>();
                CurBodyRenderer = gen.BodyRenderer;
            }

            void applyDefaultGeneration()
            {
                modelRefernces.Model.SetActive(true);
                creatureAnimator = modelRefernces.Animator;
                skinnedMeshRenderers = modelRefernces.Model.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                meshRenderers = modelRefernces.Model.gameObject.GetComponentsInChildren<MeshRenderer>();
                CurBodyRenderer = modelRefernces.BodyRenderer;
            }

            foreach (PuffminModelGeneration gen in modelRefernces.Generations)
            {
                if (!PikChecks.IsGenerationValid(gen))
                {
                    LethalMin.Logger.LogError($"Generation {gen.Generation} is invaild for a Puffmin!");
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
                LethalMin.Logger.LogWarning($"Failed to find a valid generation for Puffmin! Generation: {generation}");
                applyDefaultGeneration();
            }
        }

        #endregion





        #region Behaviour Methods
        public override void Update()
        {
            ModifiedUpdate();

            HandleLocalSnapPosition();

            if (IsOwner)
            {
                creatureAnimator.SetBool("Moving", agent.velocity.magnitude > 0.1f);
            }
            else
            {
                creatureAnimator.SetBool("Moving", transform2.tempVelocity.magnitude > 0.1f);
            }

            if (IsOwner)
            {
                if (StunTimer > 0)
                {
                    StunTimer -= Time.deltaTime;
                }
                if (WhistleBuffer >= 0)
                {
                    WhistleBuffer -= Time.deltaTime;
                }
            }

            if (IsOwner)
            {
                CheckIfOnNavmesh();
            }


            if (currentBehaviourStateIndex == 2)
            {
                HandleChaseStateConstantly();
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

        public override void DoAIInterval()
        {
            base.DoAIInterval();

            const int IDLE = 0;
            const int FOLLOW = 1;
            const int CHASE = 2;

            if (Leader != null && Leader.AI.isEnemyDead)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Leader is dead! Turning back to normal...");
                TransformIntoPikminServerRpc();
                return;
            }
            if (Leader == null && PreviousLeader != null && PreviousLeader.AI.isEnemyDead)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Previous Leader is dead! Turning back to normal...");
                TransformIntoPikminServerRpc();
                return;
            }

            switch (currentBehaviourStateIndex)
            {
                case IDLE:
                    HandleIdleStateOnOwnerClient();
                    break;

                case FOLLOW:
                    HandleFollowStateOnOwnerClient();
                    break;

                case CHASE:
                    HandleChaseStateOnOwnerClient();
                    break;
            }
        }

        public void PathToPosition(Vector3 position)
        {
            if (!IsOwner)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: PathToPosition called on non-owner client!");
            }
            if (IsOwner && agent.enabled)
            {
                agent.SetDestination(position);
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
            if (!IsOwner)
                SetToIdle();
        }
        public void SetToIdle()
        {
            LethalMin.Logger.LogInfo($"{DebugID}: Setting to idle");
            SwitchToBehaviourStateOnLocalClient(0);
            SwitchToIntent(PuffminIntent.Idle);
            PlayerChaseing = null;
            SetCollisionMode(1);
            UnLatchPuffmin();
        }


        public void HandleIdleStateOnOwnerClient()
        {
            agent.stoppingDistance = 0;
            PathToPosition(transform.position);

            if (currentIntent == PuffminIntent.Held || currentIntent == PuffminIntent.Thrown
            || IsDeadOrDying || StunTimer > 0)
            {
                return;
            }

            PlayerControllerB? player = GetClosestPlayer();
            if (player != null)
            {
                SetToChasePlayerServerRpc(player.OwnerClientId);
            }
        }


        public void HandleFollowStateOnOwnerClient()
        {
            if (Leader == null || Leader.AI.isEnemyDead)
            {
                SetToIdleServerRpc();
                SetToIdle();
                return;
            }
            if (currentIntent == PuffminIntent.Held || currentIntent == PuffminIntent.Thrown)
            {
                return;
            }

            agent.stoppingDistance = 3.5f;
            PathToPosition(Leader.transform.position);
        }


        public void HandleChaseStateOnOwnerClient()
        {
            if (PlayerChaseing != null)
            {
                agent.stoppingDistance = 0;
                PathToPosition(PlayerChaseing.transform.position);
            }
        }
        public void HandleChaseStateConstantly()
        {
            if (LeaderLatchedTo != null && LeaderLatchedTo.Controller.isPlayerControlled)
            {
                return;
            }

            if (PlayerChaseing == null || !PlayerChaseing.isPlayerControlled ||
                Vector3.Distance(transform.position, PlayerChaseing.transform.position) > 25)
            {
                if (PreviousLeader != null && !PreviousLeader.AI.isEnemyDead)
                {
                    LethalMin.Logger.LogInfo($"{DebugID}: Setting to follow previous leader");

                    if (IsOwner)
                    {
                        AssignLeader(PreviousLeader);
                        AssignLeaderServerRpc(PreviousLeader.NetworkObject);
                    }
                }
                else
                {
                    LethalMin.Logger.LogInfo($"{DebugID}: Player is no longer valid! Switching to idle state...");

                    if (IsOwner)
                        SwitchToBehaviourState(0);
                }
                return;
            }

            if (AttackCooldown >= 0)
            {
                AttackCooldown -= Time.deltaTime;
                return;
            }

            if (Vector3.Distance(transform.position, PlayerChaseing.transform.position) < 2.5)
            {
                StartCoroutine(TryHitPlayer(PlayerChaseing));
            }
        }

        public void CheckIfOnNavmesh()
        {
            if (!agent.enabled || agent.isOnNavMesh || agent.isOnOffMeshLink)
            {
                return;
            }
            else if (timeOffNavMesh > 0)
            {
                timeOffNavMesh -= Time.deltaTime;
                return;
            }

            timeOffNavMesh = 4;

            if (Leader != null)
            {
                Vector3 pos = RoundManager.Instance.GetNavMeshPosition(Leader.transform.position);
                agent.Warp(pos);
                LethalMin.Logger.LogWarning($"{DebugID}: Warped to Leader: {pos} due to being off navmesh");
                return;
            }

            if (PreviousLeader != null)
            {
                Vector3 pos = RoundManager.Instance.GetNavMeshPosition(PreviousLeader.transform.position);
                agent.Warp(pos);
                LethalMin.Logger.LogWarning($"{DebugID}: Warped to Previous Leader: {pos} due to being off navmesh");
                return;
            }

            Vector3 posF = RoundManager.Instance.GetNavMeshPosition(transform.position, default, 100, NavMesh.AllAreas);
            agent.Warp(posF);
            LethalMin.Logger.LogWarning($"{DebugID}: Warped to {posF} due to being off navmesh");
        }


        public void SwitchToIntent(PuffminIntent intent)
        {
            currentIntent = intent;
        }
        #endregion





        #region Chaseing and Attacking
        public PlayerControllerB? GetClosestPlayer()
        {
            return PikUtils.GetClosestInstanceOfClassToPosition(transform.position, 10, StartOfRound.Instance.allPlayerScripts);
        }

        [ServerRpc]
        public void SetToChasePlayerServerRpc(ulong PlayerID)
        {
            if (OwnerClientId != PlayerID)
                ChangeOwnershipOfEnemy(PlayerID);
            SetToChasePlayerClientRpc(PlayerID);
        }

        [ClientRpc]
        public void SetToChasePlayerClientRpc(ulong PlayerID)
        {
            Leader? leader = LethalMin.GetLeaderViaID(PlayerID);
            if (leader == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to find leader with ID {PlayerID}!");
                return;
            }
            SetToChase(leader.Controller);
        }

        public void SetToChase(PlayerControllerB player)
        {
            LethalMin.Logger.LogInfo($"{DebugID}: Setting to chase player {player.OwnerClientId}");
            PlayerChaseing = player;
            SetCollisionMode(1);
            SwitchToBehaviourStateOnLocalClient(2);
        }

        public IEnumerator TryHitPlayer(PlayerControllerB player)
        {
            creatureVoice.PlayOneShot(AttackVoice);
            creatureAnimator.SetTrigger("Attack");
            AttackCooldown = 0.25f;
            yield return new WaitForSeconds(0.25f);
            if (Vector3.Distance(transform.position, player.transform.position) < 2.5)
            {
                if (player.IsOwner)
                    player.DamagePlayer(1, true, true, CauseOfDeath.Bludgeoning, 0, false, transform.forward);
                creatureSFX.PlayOneShot(HitSound);
            }
        }

        public void SetPuffminAsLatchedOn(Leader leader)
        {
            LethalMin.Logger.LogInfo($"{DebugID}: Setting as latched on to {leader.Controller.playerUsername}");
            if (latchedRoutine != null)
            {
                StopCoroutine(latchedRoutine);
                latchedRoutine = null!;
            }
            LeaderLatchedTo = leader;
            SetCollisionMode(0);
            SwitchToBehaviourStateOnLocalClient(2);
            SwitchToIntent(PuffminIntent.LatchedOn);
            latchedRoutine = StartCoroutine(LatchedOnRoutine());
        }
        public void UnLatchPuffmin()
        {
            if (latchedRoutine != null)
            {
                LethalMin.Logger.LogInfo($"{DebugID} stopping latch routine");
                StopCoroutine(latchedRoutine);
                latchedRoutine = null!;
            }
            if (LeaderLatchedTo != null)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Unlatching from {LeaderLatchedTo.OwnerClientId}");
                if (LeaderLatchedTo.PuffminLatchedOn.ContainsKey(this))
                {
                    Destroy(LeaderLatchedTo.PuffminLatchedOn[this].gameObject);
                    LeaderLatchedTo.PuffminLatchedOn.Remove(this);
                }
                else
                {
                    LethalMin.Logger.LogError($"{DebugID}: Failed to find self in LeaderLatchedTo's PuffminLatchedOn!");
                }
            }
            LeaderLatchedTo = null!;
        }
        IEnumerator LatchedOnRoutine()
        {
            while (LeaderLatchedTo != null && LeaderLatchedTo.Controller.isPlayerControlled)
            {
                creatureAnimator.SetTrigger("Attack");
                creatureVoice.PlayOneShot(AttackVoice);
                yield return new WaitForSeconds(0.25f);
                creatureVoice.PlayOneShot(HitSound);
                if (LeaderLatchedTo.IsOwner)
                    LeaderLatchedTo.Controller.DamagePlayer(1, true, true, CauseOfDeath.Bludgeoning, 0, false, transform.forward);
            }
            latchedRoutine = null!;
        }
        #endregion






        #region Physics
        /// <summary>
        /// Sets the collision mode of the Pikmin
        /// Should be called on every client
        /// 0 = none,
        /// 1 = agent only,
        /// 2 = rigidbody only
        /// </summary>
        /// <param name="Mode"></param>
        public virtual void SetCollisionMode(int Mode)
        {
            const int None = 0;
            const int AgentOnly = 1;
            const int RigidbodyOnly = 2;
            const int RigidbodyOwnerOnly = 3;

            LethalMin.Logger.LogDebug($"{DebugID}: Setting collision mode to {Mode}");

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
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
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
                        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        Pcollider.excludeLayers = ~LethalMin.PikminColideable;
                        rb.excludeLayers = ~LethalMin.PikminColideable;
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
                        rb.Sleep();
                    }
                    transform2.enabled = true;
                    //rb2.enabled = true;
                    break;
            }
        }

        public void HandleLocalSnapPosition()
        {
            if (currentIntent == PuffminIntent.Held && Leader != null)
            {
                transform.position = Leader.HoldPos.position;
                transform.rotation = Leader.HoldPos.rotation;
            }

            if (LeaderLatchedTo != null && LeaderLatchedTo.Controller.isPlayerControlled)
            {
                transform.position = LeaderLatchedTo.PuffminLatchedOn[this].position;
                transform.LookAt(LeaderLatchedTo.Controller.gameplayCamera.transform); // Make sure to look at the camera or player
            }
        }

        public void StartThrow()
        {
            if (Leader == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Leader is null when starting throw");
                return;
            }
            SwitchToIntent(PuffminIntent.Held);
            SetCollisionMode(0);
            creatureAnimator.SetTrigger("Hold");
            creatureVoice.PlayOneShot(HoldVoice);
        }

        public virtual void ThrowPuffmin(Vector3 Direction)
        {
            if (Leader == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Leader is null when throwing pikmin");

                if (currentIntent == PuffminIntent.Held)
                    SetToIdle();
                return;
            }
            if (OrignalPikmin == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: OrignalPikmin is null when throwing pikmin");

                if (currentIntent == PuffminIntent.Held)
                    SetToIdle();
                return;
            }

            PikminType pikminType = LethalMin.GetPikminTypeByID(OrignalPikmin.Value.TypeID);
            // Calculate throw force based on direction components
            Vector3 throwForce = new Vector3(
                Direction.x * pikminType.ThrowForce.x,
                Direction.y * pikminType.ThrowForce.y,
                Direction.z * pikminType.ThrowForce.z
            );

            LethalMin.Logger.LogDebug($"{DebugID}: Thrown with force: {throwForce}");
            SwitchToIntent(PuffminIntent.Thrown);
            SwitchToBehaviourStateOnLocalClient(0);
            SetCollisionMode(2);
            rb.position = Leader.ThrowOrigin.position;
            transform.position = Leader.ThrowOrigin.position;
            if (Direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(Direction);
                rb.rotation = Quaternion.LookRotation(Direction);
            }
            rb.AddForce(throwForce, ForceMode.Impulse);
            creatureVoice.Stop();
            creatureVoice.PlayOneShot(ThrowVoice);
            creatureAnimator.SetTrigger("Throw");
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (IsOwner && currentIntent == PuffminIntent.Thrown)
            {
                LandPuffminServerRpc(transform.position);
                LandPuffmin();
                LethalMin.Logger.LogInfo($"{DebugID}: Landed on {collision.gameObject.name}");
            }
        }

        public void OnCollisionStay(Collision collision)
        {
            if (IsOwner && currentIntent == PuffminIntent.Knockback)
            {
                if (KnockbackLandBuffer > 0)
                {
                    KnockbackLandBuffer -= Time.deltaTime;
                    return;
                }
                KnockbackLandBuffer = 0.5f;
                LandPuffminServerRpc(transform.position);
                LandPuffmin();
                LethalMin.Logger.LogInfo($"{DebugID}: Landed on {collision.gameObject.name} (knockback)");
            }
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);
            if (IsOwner && currentIntent == PuffminIntent.Thrown)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: OnCollideWithPlayer - Attempting to latch on to player {other.gameObject.name}");
                if (other.TryGetComponent(out PlayerControllerB player))
                {
                    LethalMin.Logger.LogInfo($"{DebugID}: Found player {player.OwnerClientId} to latch on to!");
                    LatchOnToLeaderServerRpc(player.GetComponent<Leader>().OwnerClientId);
                }
                LethalMin.Logger.LogInfo($"{DebugID}: Landed on {other.gameObject.name}");
            }
        }

        [ServerRpc]
        public void LatchOnToLeaderServerRpc(ulong LeaderID)
        {
            if (OwnerClientId != LeaderID)
                ChangeOwnershipOfEnemy(LeaderID);

            LatchOnToLeaderClientRpc(LeaderID);
        }
        [ClientRpc]
        public void LatchOnToLeaderClientRpc(ulong LeaderID)
        {
            Leader? lead = LethalMin.GetLeaderViaID(LeaderID);
            if (lead == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to find leader with ID {LeaderID}!");
                return;
            }
            LatchOnToLeaderOnLocalClient(lead);
        }
        public void LatchOnToLeaderOnLocalClient(Leader lead)
        {
            GameObject LatchPoint = new GameObject($"{DebugID}LatchPoint");
            LatchPoint.transform.position = transform.position;
            transform.LookAt(lead.Controller.gameplayCamera.transform);
            LatchPoint.transform.SetParent(lead.transform, true);
            float RandomOffsetX = (float)((enemyRandom.NextDouble() * 2.0) - 1.0);
            float RandomOffsetY = (float)((enemyRandom.NextDouble() * 3.0) - 1.0);
            float DeterminedOffsetZ = PikminManager.instance.LocalLeader == lead ? 1 : 0;
            LatchPoint.transform.localPosition = new Vector3(RandomOffsetX, RandomOffsetY, DeterminedOffsetZ);
            TempObjects.Add(LatchPoint);
            lead.PuffminLatchedOn.Add(this, LatchPoint.transform);
            SetPuffminAsLatchedOn(lead);
            LethalMin.Logger.LogDebug($"{DebugID}: Latching on to {lead.OwnerClientId} at position: {LatchPoint.transform.position} with offset X: {RandomOffsetX}, Y: {RandomOffsetY}");
        }

        [ServerRpc]
        public void LandPuffminServerRpc(Vector3 landPos)
        {
            LandPuffminClientRpc(landPos);
        }
        [ClientRpc]
        public void LandPuffminClientRpc(Vector3 landPos)
        {
            transform2.TeleportOnLocalClient(landPos);
            if (!IsOwner)
                LandPuffmin();
        }

        public void LandPuffmin()
        {
            SetToIdle();
            creatureAnimator.SetTrigger("Land");
            if (IsOwner)
            {
                agent.Warp(transform.position);
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

        public void ApplyKnockBack(Vector3 direction, float force)
        {
            LethalMin.Logger.LogDebug($"{DebugID}: Knockback with force: {force} and direction: {direction}");
            SwitchToIntent(PuffminIntent.Knockback);
            SwitchToBehaviourStateOnLocalClient(0);
            creatureAnimator.SetTrigger("Knockback");
            PlayerChaseing = null;
            SetCollisionMode(3);
            UnLatchPuffmin();
            RemoveLeader();
            StunTimer += 5;
            if (IsOwner)
            {
                rb.AddForce(direction * force, ForceMode.Impulse);
                rb.rotation = Quaternion.LookRotation(-direction);
            }
        }


        #endregion





        #region Death and Damage
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null!, bool playHitSFX = false, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            if (IsDeadOrDying)
            {
                return;
            }
            creatureVoice.PlayOneShot(HurtVoice);
            enemyHP -= force;
            Vector3 KnockbackDir = new Vector3(-transform.forward.x * force, force * 2, -transform.forward.z * force);
            if (playerWhoHit != null)
            {
                Vector3 playerDir = transform.position - playerWhoHit.transform.position;
                KnockbackDir = new Vector3(playerDir.normalized.x * 2, 3, playerDir.normalized.z * 2);
            }
            if (enemyHP > 0)
            {
                ApplyKnockBack(KnockbackDir, 3 + force);
            }
            else
            {
                ApplyKnockBack(KnockbackDir, force);
                KillEnemy(false);
            }
        }

        public override void KillEnemy(bool destroy = false)
        {
            if (destroy && IsServer)
            {
                LethalMin.Logger.LogInfo($"{DebugID}: Died, but due to the way zeekees programmed it we need to do dis first");
                SpawnGhostClientRpc();
                base.KillEnemy(destroy);
                return;
            }

            base.KillEnemy(destroy);

            creatureVoice.PlayOneShot(HurtVoice);

            LethalMin.Logger.LogInfo($"{DebugID}: Died");

            currentIntent = PuffminIntent.Dead;


            StartCoroutine(SpawnGhostAfterDelay());
        }

        [ClientRpc]
        public void SpawnGhostClientRpc()
        {
            SpawnGhost();
        }

        IEnumerator SpawnGhostAfterDelay(float delay = 2)
        {
            delay += enemyRandom.Next(1, 10) / 10f;
            LethalMin.Logger.LogDebug($"{DebugID}: Spawning Ghost after, {delay} seconds");
            yield return new WaitForSeconds(delay);
            SpawnGhost();
        }

        public void SpawnGhost()
        {
            if (LethalMin.TurnToNormalOnDeath)
            {
                if (IsServer)
                    TransformIntoPikminServerRpc();
                return;
            }
            LethalMin.Logger.LogInfo($"{DebugID}: Spawning Ghost");
            PikminGhost ghost = GameObject.Instantiate(LethalMin.PikminGhostPrefab, transform.position, transform.rotation).
            GetComponent<PikminGhost>();
            if (OrignalPikmin != null)
            {
                Color pikminColor = LethalMin.GetPikminTypeByID(OrignalPikmin.Value.TypeID).PikminPrimaryColor;
                Color puffminColor = new Color(0.42f, 0f, 0.42f);
                ghost.overrideColor = Color.Lerp(pikminColor, puffminColor, 0.5f);
            }
            ghost.LostType = LethalMin.GetPikminTypeByID(OrignalPikmin.HasValue ? OrignalPikmin.Value.TypeID : 0);
            ghost.InMemoryof = DebugID;
            ghost.ghostRandom = enemyRandom;
            ghost.GetComponentInChildren<AudioSource>().pitch = 0.75f;
            PikminManager.instance.EndOfGameStats.PikminKilled++;
            PikminManager.instance.FiredStats.TotalPikminLost++;
            if (NetworkObject.IsSpawned)
                IncrumentDestoryCountServerRpc();
        }


        int destoryCounter = 0;
        [ServerRpc(RequireOwnership = false)]
        public void IncrumentDestoryCountServerRpc()
        {
            destoryCounter++;
            LethalMin.Logger.LogInfo($"{DebugID}: {destoryCounter} - {StartOfRound.Instance.connectedPlayersAmount + 1}");
            if (destoryCounter >= StartOfRound.Instance.connectedPlayersAmount + 1)
            {
                NetworkObject.Despawn();
            }
        }
        #endregion





        #region Leader Managerment
        public PuffminLeader? GetClosestLeader()
        {
            return PikUtils.GetClosestInstanceOfClassToPosition(transform.position, 2.5f, PikminManager.instance.PuffminLeaders);
        }

        [ServerRpc]
        public void AssignLeaderServerRpc(NetworkObjectReference LeaderRef)
        {
            if (LeaderRef.TryGet(out NetworkObject leaderObj))
            {
                if (OwnerClientId != leaderObj.OwnerClientId)
                    ChangeOwnershipOfEnemy(leaderObj.OwnerClientId);
            }
            AssignLeaderClientRpc(LeaderRef);
        }
        [ClientRpc]
        public void AssignLeaderClientRpc(NetworkObjectReference LeaderRef)
        {
            if (IsOwner)
            {
                return;
            }
            if (LeaderRef.TryGet(out NetworkObject leaderObj))
            {
                if (leaderObj.TryGetComponent(out PuffminLeader leader))
                {
                    AssignLeader(leader);
                }
                else
                {
                    LethalMin.Logger.LogError($"{DebugID}: Failed to get PuffminLeader from {LeaderRef}");
                }
            }
            else
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to get NetworkObject from {LeaderRef}");
            }
        }

        public void AssignLeader(PuffminLeader leader)
        {
            LethalMin.Logger.LogInfo($"{DebugID}: Assigning leader {leader.gameObject.name}");
            Leader = leader;
            leader.AddPuffminToSquad(this);
            SwitchToBehaviourStateOnLocalClient(1);
            SetCollisionMode(1);
            creatureVoice.PlayOneShot(NoticeVoice);
        }


        [ServerRpc]
        public void RemoveLeaderServerRpc()
        {
            RemoveLeaderClientRpc();
        }
        [ClientRpc]
        public void RemoveLeaderClientRpc()
        {
            RemoveLeader();
        }

        public void RemoveLeader()
        {
            Leader?.RemovePuffminFromSquad(this);
            if (Leader != null)
            {
                PreviousLeader = Leader;
            }
            Leader = null;
        }
        #endregion



        #region Pikmin Transformation
        public void DoTransformAnimation()
        {
            StunTimer = 1.5f;
            creatureAnimator.Play("Spawn");
            StartCoroutine(TweenColors());
        }
        IEnumerator TweenColors()
        {
            if (OrignalPikmin == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: OrignalPikmin is null when tweening colors!");
                yield break;
            }
            Renderer storedRenderer = CurBodyRenderer;
            Texture? StoredTexture = null;
            Color OrignalColor = storedRenderer.material.color;
            Color StartColor = LethalMin.GetPikminTypeByID(OrignalPikmin.Value.TypeID).PikminPrimaryColor;
            Color EndColor = new Color(0.42f, 0f, 0.42f); // Purple color in normalized RGB format (0-1)
            if (storedRenderer.material.mainTexture != null)
            {
                StoredTexture = storedRenderer.material.mainTexture;
                storedRenderer.material.mainTexture = null;
            }
            storedRenderer.material.color = StartColor;
            float elapsedTime = 0f;
            while (elapsedTime < 1)
            {
                storedRenderer.material.color = Color.Lerp(StartColor, EndColor, elapsedTime);
                elapsedTime += Time.deltaTime / 2;
                yield return null;
            }
            storedRenderer.material.color = OrignalColor;
            if (StoredTexture != null)
            {
                storedRenderer.material.mainTexture = StoredTexture;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TransformIntoPikminServerRpc()
        {
            if (OrignalPikmin == null)
            {
                LethalMin.Logger.LogError($"{DebugID}: OrignalPikmin is null when tranforming!");
                NetworkObject.Despawn(true);
                return;
            }
            //TransformIntoPikminClientRpc();
            PikminSpawnProps props = new PikminSpawnProps();
            props.GrowthStage = OrignalPikmin.Value.GrowthStage;
            props.OverrideDebugID = OrignalPikmin.Value.DebugName;
            PikminManager.instance.SpawnPikminOnServer(LethalMin.GetPikminTypeByID(OrignalPikmin.Value.TypeID), transform.position, transform.rotation, props);
            NetworkObject.Despawn(true);
        }
        [ClientRpc]
        public void TransformIntoPikminClientRpc()
        {
            TransformIntoPikmin();
        }

        public void TransformIntoPikmin()
        {

        }
        #endregion
    }
}
