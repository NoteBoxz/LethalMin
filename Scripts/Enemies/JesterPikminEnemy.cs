using System.Collections;

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class JesterPikminEnemy : PikminEnemy
    {
        List<NetworkObjectReference> PikminRefs = new List<NetworkObjectReference>();
        JesterAI jesterAI = null!;
        float CheckInterval = 0.25f;
        float BiteCooldown = 1.5f;
        public float BiteResetCooldown = 5f;
        public int BiteLimmit = 10;

        protected override void Start()
        {
            base.Start();
            jesterAI = enemyScript as JesterAI ?? throw new System.Exception("JesterPE: enemyScript is not a JesterAI");
            if (jesterAI == null)
            {
                enabled = false;
            }
            if (LethalMin.UseConfigsForEnemies)
            {
                BiteCooldown = LethalMin.Jester_BiteCooldown.InternalValue;
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
            if (jesterAI.inKillAnimation)
            {
                return;
            }
            if (jesterAI.currentBehaviourStateIndex != 2)
            {
                return;
            }
            if (BiteCooldown > 0)
            {
                BiteCooldown -= Time.deltaTime;
                return;
            }
            if (LethalMin.UseConfigsForEnemies)
            {
                BiteResetCooldown = LethalMin.Jester_BiteCooldown.InternalValue;
                BiteLimmit = LethalMin.Jester_BiteLimit.InternalValue;
            }

            if (CheckInterval > 0)
            {
                CheckInterval -= Time.deltaTime;
            }
            else
            {
                DoCheckInterval();
                CheckInterval = jesterAI.AIIntervalTime + Random.Range(-0.015f, 0.015f);
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
                if (PikminRefs.Count >= BiteLimmit)
                {
                    break;
                }
            }
            if (PikminRefs.Count > 0)
            {
                BiteCooldown = BiteResetCooldown;
                BiteNearbyPikminServerRpc(PikminRefs.ToArray());
            }
        }

        [ServerRpc]
        public void BiteNearbyPikminServerRpc(NetworkObjectReference[] Pikmins)
        {
            BiteNearbyPikminClientRpc(Pikmins);
        }
        [ClientRpc]
        public void BiteNearbyPikminClientRpc(NetworkObjectReference[] Pikmins)
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
                    LethalMin.Logger.LogError("Failed to get PikminAI from NetworkObjectReference in BiteNearbyPikminClientRpc");
                }
            }
            if (PikminsB.Count > 0)
            {
                BiteNearbyPikmin(PikminsB);
            }
            else
            {
                LethalMin.Logger.LogError("Failed to get PikminAI from NetworkObjectReference in BiteNearbyPikminClientRpc");
            }
        }

        public void BiteNearbyPikmin(List<PikminAI> Pikmins)
        {
            BiteCooldown = BiteResetCooldown;
            foreach (PikminAI pikminAI in Pikmins)
            {
                pikminAI.DeathSnapToPos = jesterAI.grabBodyPoint;
                pikminAI.OverrideDelay = 1f + pikminAI.enemyRandom.Next(80) * 0.01f;
                pikminAI.HitEnemy(999);
            }
            jesterAI.creatureAnimator.SetTrigger("KillPlayer");
            jesterAI.creatureSFX.PlayOneShot(jesterAI.killPlayerSFX);
        }
    }
}
