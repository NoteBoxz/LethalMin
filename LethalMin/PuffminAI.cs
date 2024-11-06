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
using System.Diagnostics;

namespace LethalMin
{
    enum PuffState
    {
        idle,
        following,
        attacking,
        airborn
    }
    public class PuffminAI : EnemyAI, IDebuggable
    {
        public Animator? LocalAnim;
        public AudioSource? LocalSFX;
        public AudioSource? LocalVoice;
        [IDebuggable.Debug] public EnemyAI? OwnerAI = null!;
        [IDebuggable.Debug] public EnemyAI? PrevOwnerAI = null!;
        public Rigidbody rb = null!;
        GameObject? Ghost;
        public NetworkTransform? transform2 = null!;
        GameObject? PminColider, scanNode, NoticeColider;
        public float InternalAirbornTimer, AbTimer;
        public System.Random enemyRandom = null!;
        public bool IsHeld, IsThrown;
        NetworkVariable<bool> newIsMoving = new NetworkVariable<bool>(false);
        SphereCollider Pcollider = null!;
        [IDebuggable.Debug] public bool HasFreeWill = true;
        public PikminType OriginalType = null!;
        public bool IsDying = false;
        public bool PreDefinedType = false;
        [IDebuggable.Debug] public bool HasInitalized = false;
        [IDebuggable.Debug] public string statename = "";
        public string DebugID = "";
        public override void Start()
        {
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            DebugID = $"Puffmin {thisEnemyIndex}";
            //Check if any player is null in the Players List
            if (Players.Count == 0 || Players.Any(p => p == null))
                Players = FindObjectsOfType<PlayerControllerB>().ToList();
            InternalAirbornTimer = LethalMin.FallTimerValue;

            base.Start();

            transform2 = GetComponent<NetworkTransform>();
            LocalAnim = GetComponentInChildren<Animator>();
            PminColider = transform.Find("PuffminColision").gameObject;
            NoticeColider = transform.Find("WhistleDetection").gameObject;
            scanNode = transform.Find("ScanNode").gameObject;
            NoticeColider.name = "WhistleDetectionWhistle";

            // Because the EnemyAI class uses unnessary methods for moving and syncing position in my case
            moveTowardsDestination = false;
            movingTowardsTargetPlayer = false;
            updatePositionThreshold = 9000;
            syncMovementSpeed = 0f;

            // Rigidbody Components
            Pcollider = GetComponent<SphereCollider>();
            rb = GetComponent<Rigidbody>();
            ToggleColisionMode(false);

            StartCoroutine(LateInitialize());
        }
        private IEnumerator LateInitialize()
        {
            yield return new WaitForSeconds(0.1f);  // Wait for one frame

            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"{DebugID} is now being spawned");

            Ghost = LethalMin.Ghost;

            //Because EnemyAI is dumb
            creatureAnimator = null;

            // Finalization 
            yield return new WaitForSeconds(0.1f);  // Wait for one frame
            enemyBehaviourStates = new EnemyBehaviourState[Enum.GetValues(typeof(PuffState)).Length];

            if (!PreDefinedType)
            {
                if (LethalMin.NaturalTypes.Count == 0)
                {
                    LethalMin.Logger.LogWarning("No natural types found, this should not happen");
                    OriginalType = LethalMin.RegisteredPikminTypes[0];
                }
                else
                {
                    OriginalType = LethalMin.NaturalTypes[enemyRandom.Next(0, LethalMin.NaturalTypes.Count)];
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"Picked {OriginalType} for ramdo");
                }
            }

            HasInitalized = true;
            yield return null;  // Wait another frame
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (IsDying)
            {
                return;
            }
            switch (currentBehaviourStateIndex)
            {
                case (int)PuffState.idle:
                    Idle();
                    break;
                case (int)PuffState.following:
                    Following();
                    break;
                case (int)PuffState.attacking:
                    Attacking();
                    break;
                case (int)PuffState.airborn:
                    Airborn();
                    break;
            }
        }

        public void Idle()
        {
            targetPlayer = CheckLineOfSightForClosestPlayer(360, 5);
            if (targetPlayer != null)
            {
                SwitchToBehaviourClientRpc((int)PuffState.attacking);
                if (OwnerAI != null && OwnerAI.GetComponentInChildren<PuffminOwnerManager>() != null)
                    OwnerAI.GetComponentInChildren<PuffminOwnerManager>().RemovePuffmin(this);
                return;
            }

            agent.stoppingDistance = 0;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
            agent.velocity = Vector3.zero;
        }

        public void Following()
        {
            //Follow the ownerAI
            if (OwnerAI == null)
            {
                SwitchToBehaviourClientRpc((int)PuffState.idle);
                return;
            }
            if (HasFreeWill)
            {
                targetPlayer = CheckLineOfSightForClosestPlayer(360, 20);
                if (targetPlayer != null)
                {
                    SwitchToBehaviourClientRpc((int)PuffState.attacking);
                    if (OwnerAI != null && OwnerAI.GetComponentInChildren<PuffminOwnerManager>() != null)
                        OwnerAI.GetComponentInChildren<PuffminOwnerManager>().RemovePuffmin(this);
                    return;
                }
            }

            agent.stoppingDistance = 3.5f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.SetDestination(OwnerAI.transform.position);
            agent.speed = 5;
        }
        public bool IsOnCooldown;
        public void Attacking()
        {
            if (SnapTopPos != null || IsOnCooldown)
            {
                return;
            }

            if (targetPlayer == null || targetPlayer.isPlayerDead || Vector3.Distance(targetPlayer.transform.position, transform.position) > 10)
            {
                if (PrevOwnerAI != null && !PrevOwnerAI.isEnemyDead)
                {
                    SwitchToBehaviourClientRpc((int)PuffState.following);
                    AssignOwner(PrevOwnerAI);
                    return;
                }
                SwitchToBehaviourClientRpc((int)PuffState.idle);
                return;
            }
            agent.stoppingDistance = 0.5f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.SetDestination(targetPlayer.transform.position);
            agent.speed = 10;
        }

        public void Airborn()
        {
            if (InternalAirbornTimer > 0)
            {
                InternalAirbornTimer -= Time.deltaTime;
            }
            else
            {
                if (AbTimer > 0)
                {
                    AbTimer -= Time.deltaTime;
                }
                else
                {
                    agent.Warp(rb.position);
                    ToggleColisionMode(false);
                    InternalAirbornTimer = LethalMin.FallTimerValue;
                    SetTriggerClientRpc("Land");
                    AbTimer = 0;
                    currentBehaviourStateIndex = (int)PuffState.idle;
                }
            }
        }
        bool CanBeAssinged = true;
        public void AssignOwner(EnemyAI newOwnerAI)
        {
            if (!CanBeAssinged || IsDying) { return; }
            if (newOwnerAI == null)
            {
                LethalMin.Logger.LogWarning($"Tried to assign a null owner to a {DebugID}");
                return;
            }
            StartCoroutine(AssignOwnerCoroutine(newOwnerAI));
        }
        private IEnumerator AssignOwnerCoroutine(EnemyAI newOwnerAI)
        {
            yield return new WaitUntil(() => HasInitalized);
            CanBeAssinged = false;
            LethalMin.Logger.LogInfo($"{DebugID} assigned to {newOwnerAI.name}");
            UnLatchPuffminToPosition();
            if (newOwnerAI.GetComponentInChildren<PuffminOwnerManager>() != null)
            {
                newOwnerAI.GetComponentInChildren<PuffminOwnerManager>().AddPuffmin(this);
            }
            DoNoticeSFXClientRpc();
            OwnerAI = newOwnerAI;
            SwitchToBehaviourClientRpc((int)PuffState.following);
            yield return new WaitForSeconds(2f);
            CanBeAssinged = true;
        }

        public bool IsTurningIntoPikmin = false;
        public void TurnIntoPikmin()
        {
            if (!IsServer || IsTurningIntoPikmin) { return; }
            IsTurningIntoPikmin = true;
            GameObject SproutInstance = Instantiate(LethalMin.pikminPrefab, transform.position, transform.rotation);
            PikminAI SproteScript = SproutInstance.GetComponent<PikminAI>();
            SproteScript.isOutside = false;
            SproteScript.NetworkObject.Spawn();
            PikminManager.Instance.SpawnPikminClientRpc(SproteScript.NetworkObject);
            PikminManager.Instance.CreatePikminClientRPC(SproteScript.NetworkObject, OriginalType.PikminTypeID, isOutside);

            if (NetworkObject.IsSpawned)
            {
                PikminManager.Instance.DespawnPikminClientRpc(NetworkObject);
            }
            else
            {
                LethalMin.Logger.LogWarning("{DebugID} NetworkObject is not spawned");
                Destroy(gameObject);
            }
        }
        public void HoldPuffmin(Transform SnapTopPos)
        {
            if (IsHeld || IsThrown || SnapTopPos != null) { return; }
            LethalMin.Logger.LogInfo("{DebugID} is being held");
            this.SnapTopPos = SnapTopPos;
            IsHeld = true;
            DoHoldSFXClientRpc();
            SetTriggerClientRpc("Hold");
        }
        public void ThrowPuffmin(Vector3 StartPos, Vector3 ThrowForward)
        {
            if (IsThrown) { return; }
            LethalMin.Logger.LogInfo("{DebugID} is being thrown");
            DoThrowSFXClientRpc();
            IsThrown = true;
            IsHeld = false;
            SnapTopPos = null!;

            transform.position = StartPos;
            transform.rotation = Quaternion.LookRotation(ThrowForward);
            transform2.Teleport(transform.position, transform.rotation, transform.localScale);

            ToggleColisionMode(true);

            rb.AddForce(ThrowForward * 25, ForceMode.Impulse);

            SetTriggerClientRpc("Throw");
            SwitchToBehaviourClientRpc((int)PuffState.airborn);
        }
        public void OnCollisionEnter(Collision collision)
        {
            if (IsThrown && IsThrown)
            {
                LethalMin.Logger.LogInfo("{DebugID} landed");
                SetTriggerClientRpc("Land");
                agent.Warp(rb.position);
                ToggleColisionMode(false);
                SwitchToBehaviourClientRpc((int)PuffState.idle);
                IsThrown = false;
                IsHeld = false;
                IsKnockedBack = false;
            }
        }
        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);
            if (IsThrown && IsThrown && !IsKnockedBack)
            {
                if (other.GetComponent<MaskedPlayerEnemy>() != null)
                    return;
                LethalMin.Logger.LogInfo($"{DebugID} landed on player {other.name}");
                IsThrown = false;
                IsHeld = false;
                SetTriggerClientRpc("Land");
                agent.Warp(rb.position);
                ToggleColisionMode(false);
                SyncPlayerLatchedOnClientRpc(other.GetComponent<PlayerControllerB>().NetworkObject);
                LatchPuffminToPosition(other.transform, true, true);
                SwitchToBehaviourClientRpc((int)PuffState.attacking);
            }
        }
        [ClientRpc]
        public void SyncPlayerLatchedOnClientRpc(NetworkObjectReference playerRef)
        {
            if (playerRef.TryGet(out NetworkObject playerNO))
            {
                PlayerLatchedOn = playerNO.GetComponent<PlayerControllerB>();
            }
        }
        [ClientRpc]
        public void SyncPlayerLatchedOnClientRpc()
        {
            PlayerLatchedOn = null!;
        }
        public PlayerControllerB PlayerLatchedOn = null!;
        private float wiggleThreshold = 5f; // Degrees of rotation to consider a wiggle
        private float wiggleTimeFrame = 1f; // Timeframe in seconds to detect wiggle
        private float lastWiggleTime = 0f;
        private int WiggleTimes = 0;
        private int WigglesNeeded = 3;
        private Quaternion lastRotation;

        private void DetectWiggle()
        {
            if (PlayerLatchedOn == null) return;

            Quaternion currentRotation = PlayerLatchedOn.transform.rotation;
            float angleDifference = Quaternion.Angle(lastRotation, currentRotation);

            if (angleDifference > wiggleThreshold)
            {
                if (Time.time - lastWiggleTime < wiggleTimeFrame)
                {
                    // Player is wiggling
                    WiggleTimes++;
                    if (WiggleTimes > WigglesNeeded)
                    {
                        OnPlayerWiggle();
                        WiggleTimes = 0;
                    }
                }
                lastWiggleTime = Time.time;
            }

            lastRotation = currentRotation;
        }
        private void OnPlayerWiggle()
        {
            // Handle the wiggle event, e.g., unlatch the Puffmin
            Vector3 KnockbackForce = (transform.position - PlayerLatchedOn.transform.position).normalized;
            PrevOwnerAI = null;
            UnLatchPuffminToPosition();
            StartCoroutine(SetOnCooldown());
            ApplyKnockbackServerRpc(KnockbackForce, false, false, 3);
            LethalMin.Logger.LogInfo("Player wiggled enough to unlatch the Puffmin!");
        }

        public void LatchPuffminToPosition(Transform BaseParent, bool IsLethal, bool IsEscapeable)
        {
            //Create a new LatchToPoint Gameobject
            GameObject LatchPoint = new GameObject("LatchPoint");
            LatchPoint.transform.position = transform.position;
            LatchPoint.transform.rotation = transform.rotation;
            LatchPoint.transform.LookAt(PlayerLatchedOn.gameplayCamera.transform);
            LatchPoint.transform.SetParent(BaseParent);
            //Set the puffmin's parent to the LatchPoint
            CurTempLatchPoint = LatchPoint.transform;
            SnapTopPos = LatchPoint.transform;
            WigglesNeeded = enemyRandom.Next(15, 60);
        }
        public void UnLatchPuffminToPosition()
        {
            if (CurTempLatchPoint != null && SnapTopPos == CurTempLatchPoint)
            {
                SnapTopPos = null!;
                Destroy(CurTempLatchPoint.gameObject);
                SyncPlayerLatchedOnClientRpc();
            }
        }
        private IEnumerator SetOnCooldown()
        {
            IsOnCooldown = true;
            yield return new WaitForSeconds(enemyRandom.Next(3, 12));
            IsOnCooldown = false;
        }
        Transform CurTempLatchPoint = null!;
        Transform SnapTopPos = null!;
        bool HasStartedSnapTo = false;
        public override void Update()
        {
            base.Update();
            if (!IsServer)
            {
                return;
            }
            if (SnapTopPos != null)
            {
                if (!HasStartedSnapTo)
                {
                    ToggleColisionMode(false, true);
                    HasStartedSnapTo = true;
                }
                transform.position = SnapTopPos.position;
                transform.rotation = SnapTopPos.rotation;
            }
            else if (HasStartedSnapTo)
            {
                if (!IsThrown)
                    ToggleColisionMode(false);
                HasStartedSnapTo = false;
            }
            statename = $"{currentBehaviourStateIndex} - {((PuffState)currentBehaviourStateIndex).ToString()}";
        }

        public void LateUpdate()
        {
            CheckAnim();
            if (!IsServer)
            {
                return;
            }
            CheckIfOnNavMesh();
            if (PlayerLatchedOn != null)
            {
                DetectWiggle();
                if (Vector3.Distance(PlayerLatchedOn.transform.position, transform.position) > 10)
                {
                    Vector3 KnockbackForce = (transform.position - PlayerLatchedOn.transform.position).normalized;
                    PrevOwnerAI = null;
                    UnLatchPuffminToPosition();
                    StartCoroutine(SetOnCooldown());
                    ApplyKnockbackServerRpc(KnockbackForce, false, false, 3);
                    LethalMin.Logger.LogInfo($"{DebugID} was too far to be latched on!");
                    PlayerLatchedOn = null!;
                }
            }
            if (!HasFreeWill && currentBehaviourStateIndex == (int)PuffState.following)
                return;
            if (!IsHeld && !IsThrown)
            {
                if (PlayerLatchedOn != null)
                {
                    if (!IsHitting)
                    {
                        DoHitClientRpc(PlayerLatchedOn.NetworkObject, 1);
                    }
                }
                else
                {
                    PlayerControllerB targetplayer = null!;
                    if (targetplayer = GetClosestPlayer(transform.position, 2))
                    {
                        if (!IsHitting)
                        {
                            DoHitClientRpc(targetplayer.NetworkObject, 1);
                        }
                    }
                }
            }
        }
        bool IsHitting = false;

        [ClientRpc]
        public void DoHitClientRpc(NetworkObjectReference playerRef, int damage)
        {
            PlayerControllerB targetplayer = null!;
            if (playerRef.TryGet(out NetworkObject targetplayerNetworkObject))
            {
                targetplayer = targetplayerNetworkObject.GetComponent<PlayerControllerB>();
            }
            else
            {
                LethalMin.Logger.LogWarning("Failed to get player from NetworkObjectReference");
                return;
            }

            IsHitting = true;

            StartCoroutine(Hitting(targetplayer, damage));
        }

        private IEnumerator Hitting(PlayerControllerB targetplayer, int damage)
        {
            LocalVoice.PlayOneShot(LethalMin.AttackSFX[enemyRandom.Next(0, LethalMin.AttackSFX.Count())]);
            LocalAnim.SetTrigger("AttackStanding");
            yield return new WaitForSeconds(0.4f);
            LethalMin.Logger.LogInfo("{DebugID} HIT!!!!");
            LocalSFX.PlayOneShot(LethalMin.PuffHit);
            targetplayer.DamagePlayer(damage, false, true, CauseOfDeath.Bludgeoning);
            IsHitting = false;
        }

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            Vector3 knockbackForce = -transform.forward * force;
            ApplyKnockbackServerRpc(knockbackForce, true, false, 3);
        }
        public override void HitFromExplosion(float distance)
        {
            base.HitFromExplosion(distance);
            ApplyKnockbackServerRpc(new Vector3(-distance, -distance, -distance), true, false, 3);
        }

        public bool IsKnockedBack;

        [ServerRpc]
        public void ApplyKnockbackServerRpc(Vector3 knockbackForce, bool IsLethal, bool KillOnLanding, float DeathTimer = 0)
        {
            if (IsKnockedBack) { return; }
            LethalMin.Logger.LogInfo($"{DebugID} is being knocked back");
            IsThrown = true;
            IsKnockedBack = true;
            IsHeld = false;
            SnapTopPos = null!;
            if (OwnerAI == null)
            {
                LethalMin.Logger.LogWarning($"{DebugID} has no owner");
            }
            if (OwnerAI != null && OwnerAI.GetComponentInChildren<PuffminOwnerManager>() == null)
            {
                LethalMin.Logger.LogWarning($"{DebugID} owner has no PuffminOwnerManager");
            }

            if (OwnerAI != null && OwnerAI.GetComponentInChildren<PuffminOwnerManager>() != null)
            {
                OwnerAI.GetComponentInChildren<PuffminOwnerManager>().RemovePuffmin(this);
            }
            UnLatchPuffminToPosition();
            transform.rotation = Quaternion.LookRotation(knockbackForce);
            ToggleColisionMode(true);

            rb.AddForce(knockbackForce, ForceMode.Impulse);
            rb.AddForce(Vector3.up * 5, ForceMode.Impulse);

            SetTriggerClientRpc("KncockBack");
            UpdateAnimBoolClientRpc("Ded", IsLethal);
            SwitchToBehaviourClientRpc((int)PuffState.airborn);
            if (IsLethal)
            {
                StartCoroutine(Die(DeathTimer));
            }
        }
        private IEnumerator Die(float DeathTimer)
        {
            IsDying = true;
            yield return new WaitForSeconds(DeathTimer);
            if (LethalMin.ConvertPuffminOnDeath)
            {
                TurnIntoPikmin();
            }
            else
            {
                SpawnGhostClientRpc();
                if (NetworkObject.IsSpawned)
                {
                    PikminManager.Instance.DespawnPikminClientRpc(NetworkObject);
                }
                else
                {
                    LethalMin.Logger.LogWarning("{DebugID} NetworkObject is not spawned");
                    Destroy(gameObject);
                }
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
            G.GetComponent<PminGhost>().pmintype = OriginalType;
            G.GetComponent<AudioSource>().pitch = LocalVoice.pitch;
        }

        [ClientRpc]
        public void DoNoticeSFXClientRpc()
        {
            LocalVoice.PlayOneShot(LethalMin.NoticeSFX);
        }
        [ClientRpc]
        public void DoHoldSFXClientRpc()
        {
            LocalVoice.PlayOneShot(LethalMin.HoldSFX);
        }
        [ClientRpc]
        public void DoThrowSFXClientRpc()
        {
            LocalVoice.Stop();
            LocalVoice.PlayOneShot(LethalMin.ThrowSFX);
        }

        [ClientRpc]
        public void UpdateAnimBoolClientRpc(string Name, bool Value)
        {
            LocalAnim.SetBool(Name, Value);
        }
        [ClientRpc]
        public void SetTriggerClientRpc(string Name)
        {
            LocalAnim.SetTrigger(Name);
        }
        [ClientRpc]
        public void SetIntClientRpc(string Name, int Value)
        {
            LocalAnim.SetInteger(Name, Value);
        }
        [ClientRpc]
        public void PlayAnimClientRpc(string Name)
        {
            LocalAnim.Play(Name);
        }

        private void CheckAnim()
        {
            LocalAnim?.SetBool("IsMoving", newIsMoving.Value);

            if (!IsServer) { return; }

            newIsMoving.Value = agent.velocity.magnitude > 0.15f
            && agent.updatePosition == true && agent.updateRotation == true;
        }
        public void ToggleColisionMode(bool RbMode, bool DisableBoth = false)
        {
            // StackTrace stackTrace = new StackTrace(true);
            // StackFrame callingFrame = stackTrace.GetFrame(1); // Get the frame of the calling method
            // string callingMethod = callingFrame.GetMethod().Name;
            // int lineNumber = callingFrame.GetFileLineNumber();

            // LethalMin.Logger.LogInfo($"ToggleColisionMode called from {callingMethod} at line {lineNumber}");
            // LethalMin.Logger.LogInfo($"Toggling Colision Mode to {RbMode} - {DisableBoth}");
            if (DisableBoth)
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
                rb.isKinematic = true;
                rb.useGravity = false;
                Pcollider.enabled = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.detectCollisions = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.excludeLayers = 0;
                rb.Sleep();
                return;
            }

            if (!RbMode)
            {
                agent.updatePosition = true;
                agent.updateRotation = true;
                rb.isKinematic = true;
                rb.useGravity = false;
                Pcollider.enabled = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.detectCollisions = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.excludeLayers = 0;
                rb.Sleep();
            }
            else
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.detectCollisions = true;
                rb.constraints = RigidbodyConstraints.None;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.excludeLayers = ~LethalMin.Instance.PikminColideable;
                rb.WakeUp();
                Pcollider.enabled = true;
            }
        }

        bool isCheckingNavMesh;
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
            yield return new WaitForSeconds(1.5f);

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
                        LethalMin.Logger.LogInfo($"Teleported Puffmin to: {teleportPosition}");
                }
                else
                {
                    LethalMin.Logger.LogWarning("Failed to find a valid teleport position for Pikmin");
                }
            }
            else
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"{DebugID} found its way back to NavMesh without teleportation");
            }

            isCheckingNavMesh = false;
        }
        private Vector3 FindTeleportPosition()
        {
            // Try current leader position
            if (OwnerAI != null && !OwnerAI.isEnemyDead)
            {
                return OwnerAI.transform.position;
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

        public static List<PlayerControllerB> Players = new List<PlayerControllerB>();
        public static PlayerControllerB GetClosestPlayer(Vector3 position, float MaxRange, bool DoLinecast = true)
        {
            PlayerControllerB closestPlayer = null!;
            float closestDistance = float.MaxValue;
            foreach (var player in Players)
            {
                if (player == null || player.isPlayerDead) { continue; }
                float distance = Vector3.Distance(position, player.transform.position);
                if (distance < closestDistance && distance < MaxRange)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
            if (DoLinecast && closestPlayer != null)
            {
                RaycastHit hit;
                if (!Physics.Linecast(position, closestPlayer.transform.position, out hit,
                StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    return closestPlayer;
                }
            }
            return null!;
        }
    }
}