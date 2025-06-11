using System.Collections;

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class SandSpiderPikminEnemy : PikminEnemy
    {
        List<NetworkObjectReference> PikminRefs = new List<NetworkObjectReference>();
        SandSpiderAI sandSpiderAI = null!;
        float CheckInterval = 0.25f;
        float BiteCooldown = 1.5f;
        public float BiteResetCooldown = 3.5f;
        public int BiteLimmit = 2;

        protected override void Start()
        {
            base.Start();
            sandSpiderAI = enemyScript as SandSpiderAI ?? throw new System.Exception("SandSpiderPE: enemyScript is not a SandSpiderAI");
            if (sandSpiderAI == null)
            {
                enabled = false;
            }
            if (LethalMin.UseConfigsForEnemies)
            {
                BiteCooldown = LethalMin.SandSpider_BiteCooldown.InternalValue;
            }
        }

        void LateUpdate()
        {
            if (!IsOwner)
            {
                return;
            }
            if (sandSpiderAI.onWall)
            {
                return;
            }
            if (enemyScript.isEnemyDead)
            {
                return;
            }
            if (enemyScript.currentBehaviourStateIndex != 2)
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
                BiteResetCooldown = LethalMin.SandSpider_BiteCooldown.InternalValue;
                BiteLimmit = LethalMin.SandSpider_BiteLimit.InternalValue;
            }

            if (CheckInterval > 0)
            {
                CheckInterval -= Time.deltaTime;
            }
            else
            {
                DoCheckInterval();
                CheckInterval = sandSpiderAI.AIIntervalTime + Random.Range(-0.015f, 0.015f);
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
                if (Vector3.Distance(ai.transform.position, transform.position) < 3f)
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
                pikminAI.DeathSnapToPos = sandSpiderAI.mouthTarget;
                pikminAI.OverrideDelay = 0.8f;
                pikminAI.HitEnemy(1);
            }
            sandSpiderAI.creatureAnimator.SetTrigger("attack");
            sandSpiderAI.overrideAnimation = 0.8f;
            sandSpiderAI.creatureSFX.PlayOneShot(sandSpiderAI.attackSFX);
            WalkieTalkie.TransmitOneShotAudio(sandSpiderAI.creatureSFX, sandSpiderAI.attackSFX);
        }
    }
}
