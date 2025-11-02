using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class BlobPikminEnemy : PikminEnemy
    {
        BlobAI blobAI = null!;
        public List<PikminAI> PikminTrapped = new List<PikminAI>();
        public List<IcePikminAI> IcePikminTrapped = new List<IcePikminAI>();
        public float StickCoolDown = 0;

        protected override void Start()
        {
            base.Start();
            blobAI = enemyScript as BlobAI ?? throw new System.Exception("BlobPE: enemyScript is not a BlobAI");
            if (blobAI == null)
            {
                enabled = false;
                return;
            }

            CanShakeOffPikmin = false;
            PikminLatchTrigger[] PikminLatchTriggers = gameObject.GetComponentsInChildren<PikminLatchTrigger>(true);
            LatchTriggers.Clear();
            foreach (PikminLatchTrigger plt in PikminLatchTriggers)
            {
                BlobLatchTrigger blt = plt.gameObject.AddComponent<BlobLatchTrigger>();
                blt.StateCondisions.AddRange(Enum.GetValues(typeof(Pintent)).Cast<Pintent>());
                blt.StateCondisions.Remove(Pintent.Stuck);
                blt.StateToSet = LatchTriggerStateToSet.Stuck;
                blt.WhistleTime = 0.5f;
                blt.blobPikminEnemy = this;
                blt.AllowBaseLatchOn = false;
                LatchTriggers.Add(blt);
                Destroy(plt);
            }
            LatchTriggers.RemoveAll(x => x == null);
        }

        void LateUpdate()
        {
            FreezeCounter = IcePikminTrapped.Count * 0.1f;
            if (FreezeCounter >= 1)
            {
                FreezeDuration = 10f;
                blobAI.timeSinceHittingLocalPlayer = 0f;
            }
            if (StickCoolDown > 0)
            {
                StickCoolDown -= Time.deltaTime;
            }
            if (IsOwner && FreezeCounter >= 1 && !IsFrozen)
            {
                FreezeEnemyServerRpc(transform.position, transform.rotation.eulerAngles);
                FreezeEnemy(transform.position, transform.rotation.eulerAngles);
                LethalMin.Logger.LogInfo($"{enemyScript.gameObject.name} frozen by Ice Pikmin due to freeze counter: {FreezeCounter}");
                return;
            }
        }

        public override void HitEnemy(float Damage, PikminAI? pikminWhoHit = null)
        {
            //base.HitEnemy(Damage, pikminWhoHit);
        }

        [ServerRpc]
        public void StickPikminServerRpc(NetworkObjectReference PikRef, Vector3 StickPos, int Index)
        {
            StickPikminClientRpc(PikRef, StickPos, Index);
        }
        [ClientRpc]
        public void StickPikminClientRpc(NetworkObjectReference PikRef, Vector3 StickPos, int Index)
        {
            if (IsOwner)
            {
                return;
            }
            if (PikRef.TryGet(out NetworkObject? netObj) && netObj.TryGetComponent(out PikminAI pikmin) && !PikUtils.IsOutOfRange(LatchTriggers, Index))
            {
                BlobLatchTrigger trigger = (BlobLatchTrigger)LatchTriggers[Index];
                StickPikmin(pikmin, StickPos, trigger);
            }
            else
            {
                LethalMin.Logger.LogError($"BlobPE: Failed to get pikmin from network object reference {PikRef}");
            }
        }
        public void StickPikmin(PikminAI pikmin, Vector3 StickPos, BlobLatchTrigger trigger)
        {
            PikminTrapped.Add(pikmin);
            if (pikmin is IcePikminAI IPA)
                IcePikminTrapped.Add(IPA);
            trigger.TryLatch(pikmin, StickPos);
        }
    }
}