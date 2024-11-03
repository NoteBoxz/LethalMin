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
        public System.Random? enemyRandom;
        public bool IsHeld, IsThrown;
        NetworkVariable<bool> newIsMoving = new NetworkVariable<bool>(false);
        SphereCollider Pcollider = null!;
        public bool HasFreeWill = true;
        public PikminType OriginalType = null!;
        public bool IsDying = false;
        public bool PreDefinedType = false;
        [IDebuggable.Debug] public bool HasInitalized = false;
        [IDebuggable.Debug] public string statename = "";
        public override void Start()
        {
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
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
                LethalMin.Logger.LogInfo($"Puffmin is now being spawned");

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
                PrevOwnerAI = OwnerAI;
                OwnerAI = null;
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
                    PrevOwnerAI = OwnerAI;
                    OwnerAI = null;
                    return;
                }
            }

            agent.stoppingDistance = 3.5f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.SetDestination(OwnerAI.transform.position);
            agent.speed = 5;
        }

        public void Attacking()
        {
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

        public void AssignOwner(EnemyAI newOwnerAI)
        {
            if (newOwnerAI == null)
            {
                LethalMin.Logger.LogWarning("Tried to assign a null owner to a Puffmin");
                return;
            }
            StartCoroutine(AssignOwnerCoroutine(newOwnerAI));
        }
        private IEnumerator AssignOwnerCoroutine(EnemyAI newOwnerAI)
        {
            yield return new WaitUntil(() => HasInitalized);
            LethalMin.Logger.LogInfo($"Puffmin assigned to {newOwnerAI.name}");
            if (newOwnerAI.GetComponentInChildren<PuffminOwnerManager>() != null)
            {
                newOwnerAI.GetComponentInChildren<PuffminOwnerManager>().AddPuffmin(this);
            }
            LocalVoice.PlayOneShot(LethalMin.NoticeSFX);
            OwnerAI = newOwnerAI;
            SwitchToBehaviourClientRpc((int)PuffState.following);
        }

        public void TurnIntoPikmin()
        {
            if (!IsServer) { return; }
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
                LethalMin.Logger.LogWarning("Puffmin NetworkObject is not spawned");
                Destroy(gameObject);
            }
        }

        public void HoldPuffmin(Transform SnapTopPos)
        {
            if (IsHeld || IsThrown) { return; }
            LethalMin.Logger.LogInfo("Puffmin is being held");
            this.SnapTopPos = SnapTopPos;
            IsHeld = true;
            LocalVoice.PlayOneShot(LethalMin.HoldSFX);
            SetTriggerClientRpc("Hold");
        }
        public void ThrowPuffmin(Vector3 StartPos, Vector3 ThrowForward)
        {
            if (IsThrown) { return; }
            LethalMin.Logger.LogInfo("Puffmin is being thrown");
            LocalVoice.Stop();
            LocalVoice.PlayOneShot(LethalMin.ThrowSFX);
            IsThrown = true;
            IsHeld = false;
            SnapTopPos = null!;
            if (OwnerAI != null && OwnerAI.GetComponentInChildren<PuffminOwnerManager>() != null)
            {
                OwnerAI.GetComponentInChildren<PuffminOwnerManager>().RemovePuffmin(this);
            }

            transform.position = StartPos;
            transform.rotation = Quaternion.LookRotation(ThrowForward);
            transform2.Teleport(transform.position, transform.rotation, transform.localScale);

            ToggleColisionMode(true);

            rb.AddForce(ThrowForward * 20, ForceMode.Impulse);
            //rb.AddForce(Vector3.up * 5, ForceMode.Impulse);

            SetTriggerClientRpc("Throw");
            SwitchToBehaviourClientRpc((int)PuffState.airborn);
        }
        public void OnCollisionEnter(Collision collision)
        {
            if (IsThrown && IsThrown)
            {
                SetTriggerClientRpc("Land");
                agent.Warp(rb.position);
                ToggleColisionMode(false);
                SwitchToBehaviourClientRpc((int)PuffState.idle);
                IsThrown = false;
                IsHeld = false;
                LethalMin.Logger.LogInfo($"Puffmin landed on {collision.collider.name}");
            }
        }
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
            if (!IsHeld && !IsThrown)
            {
                PlayerControllerB targetplayer = null!;
                if (targetplayer = GetClosestPlayer(transform.position, 2))
                {
                    if (!IsHitting)
                    {
                        IsHitting = true;
                        StartCoroutine(Hitting(targetplayer));
                    }
                }
            }
        }
        bool IsHitting = false;
        private IEnumerator Hitting(PlayerControllerB targetplayer)
        {
            DoHitYellClientRpc();
            SetTriggerClientRpc("AttackStanding");
            yield return new WaitForSeconds(0.4f);
            LethalMin.Logger.LogInfo("Puffmin HIT!!!!");
            DoHitClientRpc();
            targetplayer.DamagePlayer(2, false, true, CauseOfDeath.Bludgeoning);
            IsHitting = false;
        }

        [ClientRpc]
        public void DoHitYellClientRpc()
        {
            LocalVoice.PlayOneShot(LethalMin.AttackSFX[enemyRandom.Next(0, LethalMin.AttackSFX.Count())]);
        }
        [ClientRpc]
        public void DoHitClientRpc()
        {
            LocalSFX.PlayOneShot(LethalMin.PuffHit);
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
                    LethalMin.Logger.LogInfo($"Puffmin found its way back to NavMesh without teleportation");
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