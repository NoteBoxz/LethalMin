using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;
using LethalMin.Pikmin;
using LethalMin.Utils;
using UnityEngine.AI;
using GameNetcodeStuff;

namespace LethalMin
{
    public class RockPikminAI : PikminAI
    {
        public AudioClip[] bounceSounds = new AudioClip[0]; // Array of bounce sounds to play on bounce
        private Leader? leaderBeforeEmbed = null;
        private bool hasBounced = false;
        private bool IsEmbeded = false;
        private bool Stunned = false;
        private bool SelfThrown = false;

        public override void Update()
        {
            base.Update();
            if (IsOwner && Stunned)
            {
                agent.speed = 0;
            }
        }

        public override PikminEnemy? GetClosestEnemy(float overrideDetectionRadius = -1)
        {
            PikminEnemy? enemy = base.GetClosestEnemy(overrideDetectionRadius);

            if (enemy == null || enemy.GetComponentInChildren<PikminLatchTrigger>() == null)
            {
                return null;
            }

            return enemy;
        }

        public override void AttackEnemyWhenNear()
        {
            if (TargetEnemy == null)
            {
                return;
            }
            if (Stunned || IsEmbeded || hasBounced)
            {
                return;
            }
            if (agent.enabled && Vector3.Distance(transform.position, TargetEnemy.transform.position) < 5f + TargetEnemy.enemyScript.agent.radius + agent.radius)
            {
                SelfThrown = true;
                DoJumpOnLocalClient((TargetEnemy.transform.position - transform.position).normalized);
            }
        }


        public override void OnCollisionEnter(Collision collision)
        {
            if (!IsOwner || CurrentIntention != Pintent.Thrown || currentBehaviourStateIndex == PANIC)
            {
                return;
            }

            if (hasBounced)
            {
                LethalMin.Logger.LogInfo($"{gameObject.name} has already bounced, not bouncing again on collision with OBJ:{collision.gameObject.name}.");
                base.OnCollisionEnter(collision);
                return;
            }

            PikminLatchTrigger latchTrigger = collision.gameObject.GetComponent<PikminLatchTrigger>();
            if (latchTrigger != null)
            {
                BounceOffObject(collision);
            }
            else
            {
                base.OnCollisionEnter(collision);
            }
        }
        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy = null!)
        {
            if (!IsOwner || CurrentIntention != Pintent.Thrown)
            {
                return;
            }

            if (hasBounced)
            {
                LethalMin.Logger.LogInfo($"{gameObject.name} has already bounced, not bouncing again on collision with ENY:{collidedEnemy.gameObject.name}.");
                return;
            }

            if (collidedEnemy != null
            && collidedEnemy.enemyType != enemyType
            && !collidedEnemy.isEnemyDead)
            {
                BounceOffObject(other, true);

                PikminEnemy Penemy = collidedEnemy.GetComponentInChildren<PikminEnemy>();
                if (Penemy == null)
                {
                    return;
                }
                float damage = !SelfThrown ? 0.1f : pikminType.GetAttackStrength(CurrentGrowthStage);
                Penemy.HitEnemyServerRpc(damage, NetworkObject);
            }
        }


        [ServerRpc]
        public void ApplyKnockBackNoResetServerRpc(Vector3 direction, float force)
        {
            ApplyKnockBackNoResetClientRpc(direction, force);
        }
        [ClientRpc]
        public void ApplyKnockBackNoResetClientRpc(Vector3 direction, float force)
        {
            ApplyKnockBackNoReset(direction, force);
        }
        public void ApplyKnockBackNoReset(Vector3 direction, float force)
        {
            StopThrow(false);
            SetCollisionMode(3);
            ChangeIntent(Pintent.Knockedback);
            PlayAnimation(animController.AnimPack.EditorKnockbackAnim);
            PlayAudioOnLocalClient(PikminSoundPackSounds.Knockback);
            if (IsOwner)
            {
                rb.rotation = Quaternion.LookRotation(-direction);
                rb.AddForce(direction * force, ForceMode.Impulse);
            }
        }


        [ServerRpc]
        public void DoBounceServerRpc(Vector3 bounceDirection, float bounceForce, bool DontExitCurrentState = false)
        {
            DoBounceClientRpc(bounceDirection, bounceForce, DontExitCurrentState);
        }
        [ClientRpc]
        private void DoBounceClientRpc(Vector3 bounceDirection, float bounceForce, bool DontExitCurrentState = false)
        {
            if (!IsOwner)
                DoBounce(bounceDirection, bounceForce, DontExitCurrentState);
        }

        private void DoBounce(Vector3 bounceDirection, float bounceForce, bool DontExitCurrentState = false)
        {
            if (hasBounced) return;

            // Normalize and add upward component to bounce direction
            bounceDirection += Vector3.up * 0.5f;
            bounceDirection.Normalize();

            // Apply knockback in the bounce direction
            if (DontExitCurrentState)
            {
                ApplyKnockBackNoReset(bounceDirection, bounceForce);
                PlayAudioOnLocalClient(PikminSoundPackSounds.HitSFX, false);
            }
            else
            {
                ApplyKnockBack(bounceDirection, bounceForce);
            }

            // Mark that we've bounced and change intention
            if (IsOwner)
            {
                hasBounced = true;
                Stunned = true;
            }

            // Play a random bounce sound if available
            if (bounceSounds.Length > 0)
            {
                creatureSFX.PlayOneShot(bounceSounds[Random.Range(0, bounceSounds.Length)], 1f);
            }

            LethalMin.Logger.LogInfo($"{gameObject.name} bounced with velocity {rb.velocity} in direction {bounceDirection}. Bounce force: {bounceForce}");
        }
        public void BounceOffObject(Collision collision, bool DontExitCurrentState = false)
        {
            if (hasBounced) return;

            // Get the normal vector of the collision (direction to bounce)
            Vector3 bounceDirection = collision.contacts[0].normal;
            float bounceForce = 7; //rb.velocity.magnitude * 0.5f;

            DoBounce(bounceDirection, bounceForce, DontExitCurrentState);
            DoBounceServerRpc(bounceDirection, bounceForce, DontExitCurrentState);
        }
        public void BounceOffObject(Collider other, bool DontExitCurrentState = false)
        {
            if (hasBounced) return;

            // Calculate bounce direction from the center of the collider
            Vector3 bounceDirection = (transform.position - other.transform.position);
            float bounceForce = 7; //rb.velocity.magnitude * 0.5f;

            DoBounce(bounceDirection, bounceForce, DontExitCurrentState);
            DoBounceServerRpc(bounceDirection, bounceForce, DontExitCurrentState);
        }


        public override void LandPikmin()
        {
            LethalMin.Logger.LogDebug($"{gameObject.name} is landing after bouncing. HasBounced: {hasBounced}, IsDeadOrDying: {IsDeadOrDying}");
            SelfThrown = false;
            if (hasBounced && !IsDeadOrDying)
            {
                StartCoroutine(WaitToGetUp());
                hasBounced = false;
                animController.PlayLandAnim();

                SetCollisionMode(1);
                if (CurrentIntention != Pintent.Attack)
                {
                    SetToIdle();
                    LethalMin.Logger.LogDebug($"{gameObject.name} is now idle after landing, not in attack state.");
                }
                return;
            }
            base.LandPikmin();
        }
        IEnumerator WaitToGetUp()
        {
            float RNG = (float)enemyRandom.NextDouble(); // Get a random float between 0 and 1 for randomness in wait time
            yield return new WaitForSeconds(RNG * 2); // Wait for a short time before allowing to get up again
            if (!Stunned)
            {
                yield break;
            }
            PlayAnimation(animController.AnimPack.EditorGetUpAnim);
            PlayAudioOnLocalClient(PikminSoundPackSounds.GetUp);
            Stunned = false;
            LethalMin.Logger.LogInfo($"{gameObject.name} is now getting up after bouncing.");
        }


        public override void OnAvoidHazard(PikminHazard hazard, Object? instance = null)
        {
            base.OnAvoidHazard(hazard, instance);
            if (hazard == PikminHazard.Crush && !IsEmbeded && IsOwner)
            {
                LethalMin.Logger.LogInfo($"Avoided spike trap: leader ({leader?.gameObject.name})-[{leader?.OwnerClientId}]");
                EmbedIntoGroundServerRpc(leader == null ? 9999999999 : leader.OwnerClientId);
            }
            if (hazard == PikminHazard.Bullet && IsOwner)
            {
                Vector3 InstancePos = instance != null ? ((GameObject)instance).transform.position : Vector3.zero;
                Vector3 Direction = (transform.position - InstancePos).normalized;
                ApplyKnockBackServerRpc(Direction, 4f);
            }
        }
        [ServerRpc]
        public void EmbedIntoGroundServerRpc(ulong PrevLeaderID = 9999999999)
        {
            EmbedIntoGroundClientRpc(PrevLeaderID);
        }
        [ClientRpc]
        private void EmbedIntoGroundClientRpc(ulong PrevLeaderID = 9999999999)
        {
            if (LethalMin.GetLeaderViaID(PrevLeaderID))
            {
                leaderBeforeEmbed = LethalMin.GetLeaderViaID(PrevLeaderID);
                LethalMin.Logger.LogInfo($"{gameObject.name} embedding into ground, previous leader: {leaderBeforeEmbed?.gameObject.name}");
            }
            EmbedIntoGround();
        }
        public void EmbedIntoGround()
        {
            if (IsEmbeded)
            {
                return;
            }

            modelContainer.transform.localPosition = new Vector3(0, -1.25f, 0); // Adjust the position to embed into the ground
            CallResetMethods();
            SwitchToBehaviourStateOnLocalClient(IDLE);
            ChangeIntent(Pintent.Stuck);

            StartCoroutine(WaitToUnembed()); // Start the coroutine to unembed after a delay
            IsEmbeded = true;
            agent.enabled = false; // Disable the agent to stop movement

            LethalMin.Logger.LogInfo($"{gameObject.name} has embedded into the ground.");
        }
        private IEnumerator WaitToUnembed()
        {
            float RNG = (float)enemyRandom.NextDouble();
            yield return new WaitForSeconds(RNG * enemyRandom.Next(7) + 5); // Wait for a short time before allowing to get up again

            if (!IsEmbeded)
            {
                yield break; // If already unembedded, exit
            }

            modelContainer.transform.localPosition = Vector3.zero;
            SetToIdle();

            PlayAnimation(animController.AnimPack.EditorNoticeAnim);
            PlayAudioOnLocalClient(PikminSoundPackSounds.GetUp);
            if (IsOwner)
                agent.enabled = true; // Re-enable the agent to allow movement

            if (leaderBeforeEmbed != null && leaderBeforeEmbed.Controller.isPlayerControlled)
            {
                AssignLeader(leaderBeforeEmbed, true, false);
                leaderBeforeEmbed = null;
            }

            IsEmbeded = false;
            LethalMin.Logger.LogInfo($"{gameObject.name} has unembedded from the ground.");
        }
    }
}