using System.Collections;

using System.Collections.Generic;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class ButlerEnemyPikminEnemy : PikminEnemy
    {
        List<NetworkObjectReference> PikminRefs = new List<NetworkObjectReference>();
        ButlerEnemyAI butlerEnemyAI = null!;
        float CheckInterval = 0.25f;
        float StabCooldown = 1.5f;
        public float StabResetCooldown = 1f;
        public int StabLimmit = 1;

        protected override void Start()
        {
            base.Start();
            butlerEnemyAI = enemyScript as ButlerEnemyAI ?? throw new System.Exception("ButlerEnemyPE: enemyScript is not a ButlerEnemyAI");
            if (butlerEnemyAI == null)
            {
                enabled = false;
            }
            if (LethalMin.UseConfigsForEnemies)
            {
                StabCooldown = LethalMin.ButlerEnemy_StabCooldown.InternalValue;
            }
        }

        void LateUpdate()
        {
            if (!IsOwner)
            {
                return;
            }
            if (enemyScript.isEnemyDead)
            {
                return;
            }
            if (butlerEnemyAI.currentBehaviourStateIndex != 2)
            {
                return;
            }
            if (StabCooldown > 0)
            {
                StabCooldown -= Time.deltaTime;
                return;
            }
            if (LethalMin.UseConfigsForEnemies)
            {
                StabResetCooldown = LethalMin.ButlerEnemy_StabCooldown.InternalValue;
                StabLimmit = LethalMin.ButlerEnemy_StabLimit.InternalValue;
            }

            if (CheckInterval > 0)
            {
                CheckInterval -= Time.deltaTime;
            }
            else
            {
                DoCheckInterval();
                CheckInterval = butlerEnemyAI.AIIntervalTime + Random.Range(-0.015f, 0.015f);
            }
        }

        public void DoCheckInterval()
        {
            PikminRefs.Clear();
            foreach (PikminAI ai in PikminManager.instance.PikminAIs)
            {
                if (ai.IsDeadOrDying || ai.IsAirborn || ai.CurrentLatchTrigger != null)
                {
                    continue;
                }
                if (Vector3.Distance(ai.transform.position, transform.position) < 5f)
                {
                    PikminRefs.Add(ai.NetworkObject);
                }
                if (PikminRefs.Count >= StabLimmit)
                {
                    break;
                }
            }
            if (PikminRefs.Count > 0)
            {
                StabCooldown = StabResetCooldown;
                StabNearbyPikminServerRpc(PikminRefs.ToArray());
            }
        }

        [ServerRpc]
        public void StabNearbyPikminServerRpc(NetworkObjectReference[] Pikmins)
        {
            StabNearbyPikminClientRpc(Pikmins);
        }
        [ClientRpc]
        public void StabNearbyPikminClientRpc(NetworkObjectReference[] Pikmins)
        {
            List<PikminAI> PikminsB = new List<PikminAI>();
            foreach (NetworkObjectReference refPikmin in Pikmins)
            {
                if (refPikmin.TryGet(out NetworkObject netObj) && netObj.TryGetComponent(out PikminAI pikminAI))
                {
                    PikminsB.Add(pikminAI);
                }
                else
                {
                    LethalMin.Logger.LogError("Failed to get PikminAI from NetworkObjectReference in StabNearbyPikminClientRpc");
                }
            }
            if (PikminsB.Count > 0)
            {
                StabNearbyPikmin(PikminsB);
            }
            else
            {
                LethalMin.Logger.LogError("Failed to get PikminAI from NetworkObjectReference in StabNearbyPikminClientRpc");
            }
        }

        public void StabNearbyPikmin(List<PikminAI> Pikmins)
        {
            StabCooldown = StabResetCooldown;
            foreach (PikminAI pikminAI in Pikmins)
            {
                if (!PikChecks.IsPikminResistantToHazard(pikminAI, PikminHazard.Stab))
                    pikminAI.HitEnemy(1);
            }
            butlerEnemyAI.creatureAnimator.SetTrigger("Stab");
            butlerEnemyAI.creatureSFX.PlayOneShot(butlerEnemyAI.enemyType.audioClips[0]);
        }
    }
}
