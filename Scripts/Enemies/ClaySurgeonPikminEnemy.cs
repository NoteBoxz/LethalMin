using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class ClaySurgeonPikminEnemy : PikminEnemy
    {
        List<NetworkObjectReference> PikminRefs = new List<NetworkObjectReference>();
        ClaySurgeonAI claySurgeonAI = null!;
        float CheckInterval = 0.2f;
        float SnippingCooldown = 1.5f;
        public float SnipResetCooldown = 1f;
        public int SnipLimmit = 3;

        protected override void Start()
        {
            base.Start();
            claySurgeonAI = enemyScript as ClaySurgeonAI ?? throw new System.InvalidOperationException("ClaySurgeonPE: enemyScript is not a ClaySurgeonAI");
            if (claySurgeonAI == null)
            {
                enabled = false;
            }
            if (LethalMin.UseConfigsForEnemies)
            {
                SnippingCooldown = LethalMin.ClaySurgeon_SnipCooldown.InternalValue;
            }
        }

        void LateUpdate()
        {
            if (!IsOwner)
            {
                return;
            }
            if (SnippingCooldown > 0)
            {
                SnippingCooldown -= Time.deltaTime;
                return;
            }

            if (LethalMin.UseConfigsForEnemies)
            {
                SnipResetCooldown = LethalMin.ClaySurgeon_SnipCooldown.InternalValue;
                SnipLimmit = LethalMin.ClaySurgeon_SnipLimit.InternalValue;
            }

            if (CheckInterval > 0)
            {
                CheckInterval -= Time.deltaTime;
            }
            else
            {
                DoCheckInterval();
                CheckInterval = claySurgeonAI.AIIntervalTime + Random.Range(-0.015f, 0.015f);
            }
        }

        public void DoCheckInterval()
        {
            PikminRefs.Clear();
            foreach (PikminAI ai in PikminManager.instance.PikminAIs)
            {
                if (ai.IsDeadOrDying || ai.IsAirborn || ai.CurrentGrowthStage == 0 || ai.CurrentLatchTrigger != null)
                {
                    continue;
                }
                if (Vector3.Distance(ai.transform.position, transform.position) < 3.5f)
                {
                    PikminRefs.Add(ai.NetworkObject);
                }
                if (PikminRefs.Count >= SnipLimmit)
                {
                    break;
                }
            }
            if (PikminRefs.Count > 0)
            {
                SnippingCooldown = SnipResetCooldown;
                SnipNearbyPikminServerRpc(transform.position, PikminRefs.ToArray());
            }
        }

        [ServerRpc]
        public void SnipNearbyPikminServerRpc(Vector3 position, NetworkObjectReference[] Pikmins)
        {
            SnipNearbyPikminClientRpc(position, Pikmins);
        }
        [ClientRpc]
        public void SnipNearbyPikminClientRpc(Vector3 position, NetworkObjectReference[] Pikmins)
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
                    LethalMin.Logger.LogError("Failed to get PikminAI from NetworkObjectReference in SnipNearbyPikminClientRpc");
                }
            }
            if (PikminsB.Count > 0)
            {
                SnipNearbyPikmin(position, PikminsB);
            }
            else
            {
                LethalMin.Logger.LogError("Failed to get PikminAI from NetworkObjectReference in SnipNearbyPikminClientRpc");
            }
        }

        public void SnipNearbyPikmin(Vector3 position, List<PikminAI> Pikmins)
        {
            SnippingCooldown = SnipResetCooldown;
            foreach (PikminAI pikminAI in Pikmins)
            {
                Vector3 direct = (pikminAI.transform.position - position).normalized;
                direct += Vector3.up * 2f;
                pikminAI.ApplyKnockBack(direct, 5);
                pikminAI.SetGrowth(0);
                pikminAI.LandBuffer = 0.1f;
            }
            claySurgeonAI.creatureAnimator.SetTrigger("snip");
            claySurgeonAI.creatureSFX.PlayOneShot(claySurgeonAI.snipScissors);
        }
    }
}
