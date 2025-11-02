using System.Collections;
using System.Collections.Generic;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class BlobLatchTrigger : PikminLatchTrigger
    {
        public BlobPikminEnemy blobPikminEnemy = null!;
        public void OnTriggerEnter(Collider other)
        {
            if (!blobPikminEnemy.IsOwner)
            {
                return;
            }
            if (other.TryGetComponent(out PikminCollisionDetect detect))
            {
                if (PikChecks.IsPikminResistantToHazard(detect.mainPikmin, PikminHazard.Sticky))
                {
                    return;
                }
                if (LethalMin.UseConfigsForEnemies && LethalMin.Blob_KillPikmin.InternalValue)
                {
                    detect.mainPikmin.DeathSnapToPos = transform;
                    detect.mainPikmin.KillEnemyServerRpc(false);
                    return;
                }
                if (LethalMin.UseConfigsForEnemies && !LethalMin.Blob_TrapPikmin.InternalValue)
                {
                    return;
                }
                Vector3 Offset = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
                Vector3 StickPos = transform.position + Offset;
                if (TryLatch(detect.mainPikmin, StickPos, true, true))
                {
                    if (blobPikminEnemy.PikminTrapped.Contains(detect.mainPikmin))
                    {
                        blobPikminEnemy.PikminTrapped.Remove(detect.mainPikmin);
                        LethalMin.Logger.LogInfo($"BlobPE: Pikmin {detect.mainPikmin.DebugID} skipping latch because it is already trapped");
                        return;
                    }
                    blobPikminEnemy.StickPikminServerRpc(detect.mainPikmin.NetworkObject, StickPos, blobPikminEnemy.LatchTriggers.IndexOf(this));
                    blobPikminEnemy.StickPikmin(detect.mainPikmin, StickPos, this);
                }
            }
        }

        public override bool TryLatch(PikminAI pikmin, Vector3 Point, bool IsDirectHit = true, bool DoCheckOnly = false)
        {
            if (blobPikminEnemy.StickCoolDown > 0)
            {
                return false; // can't latch if cooldown is active
            }
            if (blobPikminEnemy.IsFrozen)
            {
                return false;
            }
            if (PikChecks.IsPikminResistantToHazard(pikmin, PikminHazard.Sticky))
            {
                return false;
            }
            if (LethalMin.UseConfigsForEnemies && !LethalMin.Blob_TrapPikmin.InternalValue)
            {
                return false;
            }
            return base.TryLatch(pikmin, Point, IsDirectHit, DoCheckOnly);
        }

        public override void LatchPikmin(PikminAI pikmin, Vector3 LandPos, bool IsDirectHit = true)
        {
            pikmin.CallResetMethods(
                RemoveLeader: true,
                DropItem: true,
                RemoveEnemy: true,
                CollisionMode: 1,
                Unlatch: true,
                RemoveTask: true,
                RemoveOverridePositions: true,
                SetLayingFalse: true
            );
            base.LatchPikmin(pikmin, LandPos, IsDirectHit);
            pikmin.Laying = true;
            pikmin.creatureAnimator.Play("Pikmin_Lay");
            pikmin.PlayAudioOnLocalClient(Pikmin.PikminSoundPackSounds.Hurt);
        }

        public override void UnlatchPikmin(PikminAI pikmin)
        {
            if (pikmin is IcePikminAI IPA && blobPikminEnemy.IcePikminTrapped.Contains(IPA))
            {
                blobPikminEnemy.IcePikminTrapped.Remove(IPA);
            }
            blobPikminEnemy.StickCoolDown = 1; // reset cooldown when unlatching
            base.UnlatchPikmin(pikmin);
        }
    }
}
