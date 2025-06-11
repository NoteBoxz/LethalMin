using System.Collections;

using System.Collections.Generic;
using GameNetcodeStuff;
using LethalMin.Patches.AI;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class RedLocustBeesPikminEnemy : PikminEnemy
    {
        RedLocustBees redLocustBees = null!;
        public static int ChasePikminStateIndex = -1;
        public PikminAI? TargetPikmin = null;
        public Leader? TargetLeader = null;
        float CheckInterval = 0.25f;
        float ZapCooldown = 1.5f;
        public float ZapResetCooldown = 0.25f;


        public override void OnAddedToEnemy(EnemyAI enemy)
        {
            redLocustBees = enemyScript as RedLocustBees ?? throw new System.Exception("RedLocustBeesPE: enemyScript is not a RedLocustBees");
            if (redLocustBees == null)
            {
                enabled = false;
                return;
            }

            int Index = 0;
            for (int i = 0; i < redLocustBees.enemyBehaviourStates.Length; i++)
            {
                EnemyBehaviourState state = redLocustBees.enemyBehaviourStates[i];
                LethalMin.Logger.LogInfo($"Found RedLocustBee state: {state.name}");
                Index = i;
            }
            Index++;

            EnemyBehaviourState ChasePikminState = new EnemyBehaviourState();
            ChasePikminState.name = "pikmin_defending";// For an existing array
            EnemyBehaviourState[] currentStates = redLocustBees.enemyBehaviourStates;
            System.Array.Resize(ref currentStates, currentStates.Length + 1);
            currentStates[currentStates.Length - 1] = ChasePikminState;
            redLocustBees.enemyBehaviourStates = currentStates;
            ChasePikminStateIndex = Index;

            LethalMin.Logger.LogInfo($"Added state: {ChasePikminState.name} to RedLocustBees enemy behaviour states index {Index}");
        }

        protected override void Start()
        {
            base.Start();

            redLocustBees = enemyScript as RedLocustBees ?? throw new System.Exception("RedLocustBeesPE: enemyScript is not a RedLocustBees");
            if (redLocustBees == null)
            {
                enabled = false;
            }
        }


        public void SetToChasePikmin(PlayerControllerB ownerPlayer, PikminAI targetPikmin)
        {
            TargetPikmin = targetPikmin;
            SyncTargetPikminServerRpc(targetPikmin.NetworkObject, ownerPlayer.OwnerClientId);
            redLocustBees.SwitchToBehaviourState(ChasePikminStateIndex);
            redLocustBees.SwitchOwnershipOfBeesToClient(ownerPlayer);
            LethalMin.Logger.LogInfo($"Bees are now chasing {targetPikmin.DebugID} owned by {ownerPlayer.playerUsername}");
        }

        void LateUpdate()
        {
            if (StartOfRound.Instance.allPlayersDead || redLocustBees.daytimeEnemyLeaving)
            {
                return;
            }

            if (LethalMin.UseConfigsForEnemies)
            {
                ZapResetCooldown = LethalMin.RedLocustBees_ZapCooldown.InternalValue;
                if (!LethalMin.RedLocustBees_ZapPikmin.InternalValue
                && !LethalMin.RedLocustBees_KillPikmin.InternalValue)
                {
                    return;
                }
            }

            if (redLocustBees.currentBehaviourStateIndex == ChasePikminStateIndex)
            {
                float num = Time.deltaTime * 0.7f;
                if (redLocustBees.previousState != redLocustBees.currentBehaviourStateIndex)
                {
                    redLocustBees.previousState = redLocustBees.currentBehaviourStateIndex;
                    redLocustBees.ResetBeeZapTimer();
                    redLocustBees.SetBeeParticleMode(1);
                    if (!redLocustBees.overrideBeeParticleTarget)
                    {
                        redLocustBees.beeParticlesTarget.position = base.transform.position + Vector3.up * 1.5f;
                    }
                }
                if (redLocustBees.attackZapModeTimer > 3f)
                {
                    redLocustBees.beesZappingMode = 1;
                    redLocustBees.ResetBeeZapTimer();
                }
                redLocustBees.agent.speed = 6f;
                redLocustBees.agent.acceleration = 13f;
                redLocustBees.beesIdle.volume = Mathf.Max(redLocustBees.beesIdle.volume - num, 0f);
                if (redLocustBees.beesIdle.isPlaying && redLocustBees.beesIdle.volume <= 0f)
                {
                    redLocustBees.beesIdle.Stop();
                }
                redLocustBees.beesDefensive.volume = Mathf.Min(redLocustBees.beesDefensive.volume + num, 1f);
                if (!redLocustBees.beesDefensive.isPlaying)
                {
                    redLocustBees.beesDefensive.Play();
                }
                redLocustBees.beesAngry.volume = Mathf.Max(redLocustBees.beesAngry.volume - num, 0f);
                if (redLocustBees.beesAngry.isPlaying && redLocustBees.beesAngry.volume <= 0f)
                {
                    redLocustBees.beesAngry.Stop();
                }
            }

            if (!IsOwner)
            {
                return;
            }

            if (ZapCooldown > 0)
            {
                ZapCooldown -= Time.deltaTime;
            }
            if (CheckInterval > 0)
            {
                CheckInterval -= Time.deltaTime;
            }
            else
            {
                DoCheckInterval();
                CheckInterval = redLocustBees.AIIntervalTime + Random.Range(-0.015f, 0.015f);
            }
        }

        public void DoCheckInterval()
        {
            if (redLocustBees.currentBehaviourStateIndex == ChasePikminStateIndex)
            {
                if (TargetPikmin == null || !RedLocustBeesPatch.PikminIsZappable(TargetPikmin)
                || Vector3.Distance(TargetPikmin.transform.position, redLocustBees.hive.transform.position) > (float)redLocustBees.defenseDistance + 5f)
                {
                    StopChasePikmin();
                    if (redLocustBees.IsHiveMissing())
                    {
                        redLocustBees.SwitchToBehaviourState(2);
                    }
                    else
                    {
                        redLocustBees.SwitchToBehaviourState(0);
                    }
                }
                else if (TargetLeader != null && TargetLeader.Controller.currentlyHeldObjectServer == redLocustBees.hive)
                {
                    redLocustBees.SwitchToBehaviourState(2);
                }
                else
                {
                    redLocustBees.SetDestinationToPosition(TargetPikmin.transform.position);
                }
            }

            if (ZapCooldown > 0)
            {
                return;
            }
            if (redLocustBees.currentBehaviourStateIndex == 1
            || redLocustBees.currentBehaviourStateIndex == 2
            || redLocustBees.currentBehaviourStateIndex == ChasePikminStateIndex)
            {
                PikminAI? PikminToZap = RedLocustBeesPatch.GetZappablePikmin(redLocustBees, 3.5f);
                if (PikminToZap == null)
                {
                    return;
                }

                ZapCooldown = ZapResetCooldown;
                ZapPikminServerRpc(PikminToZap.NetworkObject);
            }
        }

        [ServerRpc]
        public void ZapPikminServerRpc(NetworkObjectReference PikRef)
        {
            ZapPikminClientRpc(PikRef);
        }
        [ClientRpc]
        public void ZapPikminClientRpc(NetworkObjectReference PikRef)
        {
            if (PikRef.TryGet(out NetworkObject? netObj) && netObj.TryGetComponent(out PikminAI pikminAI))
            {
                ZapPikmin(pikminAI);
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to zap target pikmin {PikRef} for RedLocustBees {redLocustBees.gameObject.name}");
            }
        }

        public void ZapPikmin(PikminAI targetPikmin)
        {
            if (PikChecks.IsPikminResistantToHazard(targetPikmin, PikminHazard.Electricity))
            {
                return;
            }
            Vector3 Direction = targetPikmin.transform.position - redLocustBees.transform.position;
            Direction += Vector3.up * 2f;
            if (LethalMin.UseConfigsForEnemies && LethalMin.RedLocustBees_KillPikmin)
            {
                targetPikmin.DoZapDeath();
            }
            else
            {
                targetPikmin.ApplyKnockBack(Direction, 5);
            }
            if (redLocustBees.beesZappingMode != 3)
            {
                redLocustBees.beesZappingMode = 3;
            }
        }

        public void StopChasePikmin()
        {
            LethalMin.Logger.LogInfo($"Stopped chasing {TargetPikmin?.DebugID} owned by {TargetLeader?.Controller.playerUsername}");
            TargetPikmin = null;
            TargetLeader = null;
            redLocustBees.wasInChase = false;
            ResetTargetPikminServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncTargetPikminServerRpc(NetworkObjectReference PikRef, ulong OwnerID)
        {
            SyncTargetPikminClientRpc(PikRef, OwnerID);
        }
        [ClientRpc]
        public void SyncTargetPikminClientRpc(NetworkObjectReference PikRef, ulong OwnerID)
        {
            Leader? lead = LethalMin.GetLeaderViaID(OwnerID);
            if (lead == null)
            {
                LethalMin.Logger.LogWarning($"Failed to sync Leader {PikRef} for RedLocustBees {redLocustBees.gameObject.name} owned by {OwnerID}");
            }

            if (PikRef.TryGet(out NetworkObject? netObj) && netObj.TryGetComponent(out PikminAI? pikminAI))
            {
                TargetPikmin = pikminAI;
                TargetLeader = lead;
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to sync target pikmin {PikRef} for RedLocustBees {redLocustBees.gameObject.name} owned by {OwnerID}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResetTargetPikminServerRpc()
        {
            ResetTargetPikminClientRpc();
        }
        [ClientRpc]
        public void ResetTargetPikminClientRpc()
        {
            TargetPikmin = null;
            TargetLeader = null;
        }
    }
}
