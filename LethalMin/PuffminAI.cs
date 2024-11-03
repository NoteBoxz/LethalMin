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
        public EnemyAI? OwnerAI = null!;
        public EnemyAI? PrevOwnerAI = null!;
        public Rigidbody rb = null!;
        GameObject? Ghost;
        public NetworkTransform? transform2;
        GameObject? PminColider, scanNode, NoticeColider;
        public float InternalAirbornTimer, AbTimer;
        public System.Random? enemyRandom;
        public bool IsHeld, IsThrown;
        NetworkVariable<bool> newIsMoving = new NetworkVariable<bool>(false);
        SphereCollider Pcollider = null!;
        public bool HasFreeWill = true;
        public PikminType OriginalType = null!;
        public bool IsDying = false;
        public override void Start()
        {
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            OriginalType = LethalMin.RegisteredPikminTypes.Values.ToList()[enemyRandom.Next(0, LethalMin.RegisteredPikminTypes.Count)];
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
                LethalMin.Logger.LogInfo($"Pikmin is now being spawned");

            Ghost = LethalMin.Ghost;

            //Because EnemyAI is dumb
            creatureAnimator = null;

            // Finalization 
            yield return new WaitForSeconds(0.1f);  // Wait for one frame
            enemyBehaviourStates = new EnemyBehaviourState[Enum.GetValues(typeof(PuffState)).Length];

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
                ToggleColisionMode(false);
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
                ToggleColisionMode(false);
                return;
            }
            if (HasFreeWill)
            {
                targetPlayer = CheckLineOfSightForClosestPlayer(360, 20);
                if (targetPlayer != null)
                {
                    SwitchToBehaviourClientRpc((int)PuffState.attacking);
                    ToggleColisionMode(false);
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
            if (targetPlayer == null || targetPlayer.isPlayerDead || Vector3.Distance(targetPlayer.transform.position, transform.position) > 20)
            {
                if (PrevOwnerAI != null && !PrevOwnerAI.isEnemyDead)
                {
                    SwitchToBehaviourClientRpc((int)PuffState.following);
                    ToggleColisionMode(false);
                    OwnerAI = PrevOwnerAI;
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
                    ToggleColisionMode(false);
                    InternalAirbornTimer = LethalMin.FallTimerValue;
                    AbTimer = 0;
                    currentBehaviourStateIndex = (int)PuffState.idle;
                }
            }
        }

        public void AssignOwner(EnemyAI newOwnerAI)
        {
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
            PikminManager.Instance.SpawnPikminClientRpc(new NetworkObjectReference(SproteScript.NetworkObject));
            PikminManager.Instance.CreatePikminClientRPC(new NetworkObjectReference(SproteScript.NetworkObject), OriginalType.PikminTypeID, isOutside);

            NetworkObject.Despawn(true);
        }

        public override void Update()
        {
            base.Update();
            if (!IsServer)
            {
                return;
            }
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