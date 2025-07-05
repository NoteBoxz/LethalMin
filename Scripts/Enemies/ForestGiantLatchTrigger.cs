using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class ForestGiantLatchTrigger : PikminLatchTrigger
    {
        public ForestGiantPikminEnemy forestGiantPikminEnemy = null!;

        public override bool TryLatch(PikminAI pikmin, Vector3 Point, bool IsDirectHit = true, bool DoCheckOnly = false)
        {
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
            if (!forestGiantPikminEnemy.PikminGrabbed.Contains(pikmin))
                forestGiantPikminEnemy.PikminGrabbed.Add(pikmin);
            pikmin.PlayAudioOnLocalClient(Pikmin.PikminSoundPackSounds.Hurt);
        }
    }
}
