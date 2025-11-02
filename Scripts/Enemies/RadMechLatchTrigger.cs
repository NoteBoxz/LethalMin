using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class RadMechLatchTrigger : PikminLatchTrigger
    {
        public RadMechPikminEnemy radMechPikminEnemy = null!;

        //The rad mech's latch trigger is so small that using this does nothing
        // public void OnTriggerEnter(Collider other)
        // {
        //     if (!radMechPikminEnemy.attemptingPikminGrab)
        //     {
        //         return;
        //     }
        //     if (other.TryGetComponent(out PikminCollisionDetect detect))
        //     {
        //         if (!detect.mainPikmin.IsOwner || radMechPikminEnemy.PikminGrabbed.Contains(detect.mainPikmin))
        //         {
        //             return;
        //         }
        //         if (TryLatch(detect.mainPikmin, transform.position, true, true))
        //         {
        //             radMechPikminEnemy.GrabPikminServerRpc(detect.mainPikmin.NetworkObject);
        //         }
        //     }
        // }

        public override bool TryLatch(PikminAI pikmin, Vector3 Point, bool IsDirectHit = true, bool DoCheckOnly = false)
        {
            if (!radMechPikminEnemy.attemptingPikminGrab)
            {
                return false;
            }
            if (pikmin.IsInShip)
            {
                return false;
            }
            if (pikmin.IsDeadOrDying || pikmin.IsAirborn || pikmin.CurrentLatchTrigger != null)
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
            if (!radMechPikminEnemy.PikminGrabbed.Contains(pikmin))
                radMechPikminEnemy.PikminGrabbed.Add(pikmin);
            pikmin.PlayAudioOnLocalClient(Pikmin.PikminSoundPackSounds.Hurt);
        }
    }
}
