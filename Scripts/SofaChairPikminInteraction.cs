using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMin
{
    public class SofaChairPikminInteraction : SpecialPikminIdleInteraction
    {
        public Vector3[] SeatPositions = { new Vector3(0.75f, 1.25f, 0), new Vector3(0.5f, 4.8f, -0.7f) };
        PikminAI? lastAI1, lastAI2;
        int CachedIndex1, CachedIndex2;
        public override void OnLeaderChanged(bool isNull)
        {
            base.OnLeaderChanged(isNull);

            // useless second argemunt so my IDE does not wine
            if (isNull || leaderInSpecialAnim == null)
            {
                foreach (PikminAI ai in AIsInSpecialAnim)
                {
                    if (ai != null)
                    {
                        ai.SpecialIdlePosition = null;
                        ai.SpecialIdleRotation = null;
                        ai.SetCollisionMode(1);
                    }
                }
                AIsInSpecialAnim.Clear();
                LethalMin.Logger.LogInfo("SofaChairPikminInteraction: Leader is null or not in special anim, clearing AIsInSpecialAnim.");
                return;
            }

            if (leaderInSpecialAnim.IsOwner)
            {
                AIsInSpecialAnim = GetRangeOfClosestPikminToLeader(leaderInSpecialAnim, 2, 35);
                SyncAIsList();
                OnFillAisList();
            }
        }

        public override void OnFillAisList()
        {
            base.OnFillAisList();
            foreach (PikminAI ai in AIsInSpecialAnim)
            {
                if (ai != null)
                {
                    ai.SetCollisionMode(0); // disable collision for special anim
                }
            }
        }

        public void LateUpdate()
        {
            if (AIsInSpecialAnim.Count == 0)
            {
                return;
            }

            // just realized that I could do this... Should had done this from the start with other scripts...
            if (AIsInSpecialAnim.Contains(null!))
            {
                LethalMin.Logger.LogWarning("SofaChairPikminInteraction: AIsInSpecialAnim contains a null reference! clearing list.");
                AIsInSpecialAnim.Clear();
            }

            PikminAI ai1 = AIsInSpecialAnim[0];
            ai1.SpecialIdlePosition = transform.TransformPoint(SeatPositions[0]);
            ai1.SpecialIdleRotation = transform.rotation;
            if (lastAI1 != ai1)
            {
                lastAI1 = ai1;
                CachedIndex1 = FindIdleIndexForSpecialAnim(ai1, AnimIndexToForce);
            }
            ai1.animController.RandomIdle = CachedIndex1;
            if (ai1.CurrentIntention != Pintent.Idle)
            {
                AIsInSpecialAnim.Remove(ai1);
            }

            if (AIsInSpecialAnim.Count == 1)
            {
                return;
            }

            PikminAI ai2 = AIsInSpecialAnim[1];
            ai2.SpecialIdlePosition = transform.TransformPoint(SeatPositions[1]);
            ai2.SpecialIdleRotation = transform.rotation;
            if (lastAI2 != ai2)
            {
                lastAI2 = ai2;
                CachedIndex2 = FindIdleIndexForSpecialAnim(ai2, AnimIndexToForce);
            }
            ai2.animController.RandomIdle = CachedIndex2;
            if (ai2.CurrentIntention != Pintent.Idle)
            {
                AIsInSpecialAnim.Remove(ai2);
            }
        }
    }
}
