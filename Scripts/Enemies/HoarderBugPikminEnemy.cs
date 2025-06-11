using System.Collections;

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class HoarderBugPikminEnemy : PikminEnemy
    {
        List<NetworkObjectReference> PikminRefs = new List<NetworkObjectReference>();
        HoarderBugAI hoarderBugAI = null!;
        float CheckInterval = 0.25f;
        float BiteCooldown = 1.5f;
        public float BiteResetCooldown = 3.0f;
        public int BiteLimmit = 1;

        protected override void Start()
        {
            base.Start();
            hoarderBugAI = enemyScript as HoarderBugAI ?? throw new System.Exception("HoarderBugPE: enemyScript is not a HoarderBugAI");
            if (hoarderBugAI == null)
            {
                enabled = false;
            }
            if (LethalMin.UseConfigsForEnemies)
            {
                BiteCooldown = LethalMin.HoarderBug_BiteCooldown.InternalValue;
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
            if (BiteCooldown > 0)
            {
                BiteCooldown -= Time.deltaTime;
                return;
            }

            if (LethalMin.UseConfigsForEnemies)
            {
                BiteResetCooldown = LethalMin.HoarderBug_BiteCooldown.InternalValue;
                BiteLimmit = LethalMin.HoarderBug_BiteLimit.InternalValue;
            }

            if (CheckInterval > 0)
            {
                CheckInterval -= Time.deltaTime;
            }
            else
            {
                DoCheckInterval();
                CheckInterval = hoarderBugAI.AIIntervalTime + Random.Range(-0.015f, 0.015f);
            }
        }

        public void DoCheckInterval()
        {
            if (!hoarderBugAI.inChase && (!LethalMin.UseConfigsForEnemies.InternalValue || LethalMin.HoarderBug_AggroWhenPikminTakesItem.InternalValue))
            {
                for (int i = 0; i < HoarderBugAI.HoarderBugItems.Count; i++)
                {
                    HoarderBugItem itm = HoarderBugAI.HoarderBugItems[i];
                    if (itm.itemGrabbableObject == null)
                    {
                        continue;
                    }
                    if (Vector3.Distance(itm.itemGrabbableObject.transform.position, transform.position) > 30f)
                    {
                        continue;
                    }
                    PikminItem Pitm = itm.itemGrabbableObject.GetComponentInChildren<PikminItem>();
                    if (Pitm != null && Pitm.IsBeingCarried && Pitm.PrimaryLeader != null)
                    {
                        BiteCooldown = BiteResetCooldown;
                        GetAngryAtPikminItemServerRpc(Pitm.NetworkObject, Pitm.PrimaryLeader.NetworkObject);
                        break;
                    }
                }
            }
            else
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
        }

        [ServerRpc]
        public void GetAngryAtPikminItemServerRpc(NetworkObjectReference itemRef, NetworkObjectReference leaderRef)
        {
            GetAngryAtPikminItemClientRpc(itemRef, leaderRef);
        }
        [ClientRpc]
        public void GetAngryAtPikminItemClientRpc(NetworkObjectReference itemRef, NetworkObjectReference leaderRef)
        {
            if (itemRef.TryGet(out NetworkObject netObj) && netObj.TryGetComponent(out PikminItem pikminItem)
            && leaderRef.TryGet(out NetworkObject netObj2) && netObj2.TryGetComponent(out Leader leaderToBeMadAt))
            {
                GetAngryAtPikminItem(pikminItem, leaderToBeMadAt);
            }
            else
            {
                LethalMin.Logger.LogError("Failed to get PikminItem from NetworkObjectReference in GetAngryAtPikminItemClientRpc");
            }
        }
        public void GetAngryAtPikminItem(PikminItem itm, Leader leaderToBeMadAt)
        {
            LethalMin.Logger.LogInfo($"HoarderBugPE: GetAngryAtPikminItem called for {itm.gameObject.name} at {leaderToBeMadAt.Controller.playerUsername}");
            HoarderBugItem HBitm = HoarderBugAI.HoarderBugItems.Find(x => x.itemGrabbableObject == itm.ItemScript);
            itm.hoarderBugItem = HBitm;
            HBitm.status = HoarderBugItemStatus.Stolen;
            hoarderBugAI.angryAtPlayer = leaderToBeMadAt.Controller;
            hoarderBugAI.angryTimer = 4f;
            hoarderBugAI.SwitchToBehaviourStateOnLocalClient(2);
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
                pikminAI.DeathSnapToPos = hoarderBugAI.grabTarget;
                pikminAI.OverrideDelay = 0.5f;
                pikminAI.HitEnemy(1);
            }
            hoarderBugAI.creatureAnimator.SetTrigger("HitPlayer");
            hoarderBugAI.creatureSFX.PlayOneShot(hoarderBugAI.hitPlayerSFX);
            WalkieTalkie.TransmitOneShotAudio(hoarderBugAI.creatureSFX, hoarderBugAI.hitPlayerSFX);
        }
    }
}
