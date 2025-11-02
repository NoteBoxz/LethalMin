using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class ForestGiantPikminEnemy : PikminEnemy
    {
        public List<PikminAI> PikminGrabbed = new List<PikminAI>();
        ForestGiantAI forestGiantAI = null!;
        public ForestGiantLatchTrigger handTrigger = null!;
        public int GrabLimmit = 25;
        public Coroutine? eatPikminCoroutine = null;

        protected override void Start()
        {
            base.Start();
            forestGiantAI = enemyScript as ForestGiantAI ?? throw new System.Exception("ForestGiantPE: enemyScript is not a ForestGiantAI");
            if (forestGiantAI == null)
            {
                enabled = false;
                return;
            }


            ForestGiantLatchTrigger rmlt = forestGiantAI.holdPlayerPoint.gameObject.AddComponent<ForestGiantLatchTrigger>();
            rmlt.StateCondisions.AddRange(System.Enum.GetValues(typeof(Pintent)).Cast<Pintent>());
            rmlt.StateCondisions.Remove(Pintent.Stuck);
            rmlt.StateToSet = LatchTriggerStateToSet.Stuck;
            rmlt.WhistleTime = -255f;
            rmlt.forestGiantPikminEnemy = this;
            rmlt.AllowBaseLatchOn = false;
            rmlt.OverrideLatchObject = forestGiantAI.holdPlayerPoint;
            handTrigger = rmlt;
        }

        public void LateUpdate()
        {
            if (LethalMin.UseConfigsForEnemies)
            {
                GrabLimmit = LethalMin.ForestGiant_GrabLimit.InternalValue;
            }
        }

        public IEnumerator EatPikminAnimation()
        {
            yield return new WaitForSeconds(0.01f * 10);
            yield return new WaitForSeconds(0.2f);
            yield return new WaitForSeconds(4.4f);
            LethalMin.Logger.LogInfo($"EatPikminAnimation: {PikminGrabbed.Count} Pikmin grabbed");
            foreach (PikminAI pik in PikminGrabbed)
            {
                yield return new WaitForSeconds(0.1f);
                if (pik != null)
                {
                    pik.KillEnemy(true);
                }
            }
            yield return new WaitForSeconds(3f);
            DropAnyPikminLeft();
            eatPikminCoroutine = null;
        }

        private void DropAnyPikminLeft()
        {
            foreach (PikminAI pik in PikminGrabbed)
            {
                if (pik != null && !pik.IsDeadOrDying)
                {
                    pik.ApplyPhysics(true);
                }
            }
            PikminGrabbed.Clear();
        }

        public void StopKillAnimation()
        {
            if (eatPikminCoroutine != null)
            {
                StopCoroutine(eatPikminCoroutine);
                eatPikminCoroutine = null;
            }
            DropAnyPikminLeft();
        }


        [ServerRpc(RequireOwnership = false)]
        public void GrabPikminServerRpc(NetworkObjectReference[] Pref)
        {
            GrabPikminClientRpc(Pref);
        }
        [ClientRpc]
        public void GrabPikminClientRpc(NetworkObjectReference[] Prefs)
        {
            List<PikminAI> pikmins = new List<PikminAI>();
            foreach (NetworkObjectReference Pref in Prefs)
            {
                if (Pref.TryGet(out NetworkObject netObj) && netObj.TryGetComponent(out PikminAI pikminAI))
                {
                    pikmins.Add(pikminAI);
                }
                else
                {
                    LethalMin.Logger.LogError($"GrabPikminClientRpc: Failed to get PikminAI from {Pref}");
                }
            }
            if (pikmins.Count > 0)
            {
                GrabPikmin(pikmins.ToArray());
            }
            else
            {
                LethalMin.Logger.LogError($"GrabPikminClientRpc: No valid PikminAI found in {PikUtils.ParseListToString(Prefs)}");
            }
        }

        public void GrabPikmin(PikminAI[] pikmins)
        {
            foreach (PikminAI pikmin in pikmins)
            {
                PikminGrabbed.Add(pikmin);
                handTrigger.LatchPikmin(pikmin, forestGiantAI.holdPlayerPoint.position);
            }
            if (eatPikminCoroutine != null)
            {
                StopCoroutine(eatPikminCoroutine);
                eatPikminCoroutine = null;
            }
            eatPikminCoroutine = StartCoroutine(EatPikminAnimation());
        }

    }
}
