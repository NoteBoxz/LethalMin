using System.Collections;
using System.Collections.Generic;
using LethalMin.Pikmin;
using UnityEngine;

namespace LethalMin
{
    public class BoomBoxPikminInteraction : MonoBehaviour
    {
        public BoomboxItem boomBoxInstance = null!;
        public float maxDistance = 15f; // max distance for pikmin to interact with boombox
        public float DistanceCheckInterval = 0.2f; // how often to check distance
        List<PikminAI?> pikminInRange = new List<PikminAI?>();
        bool LastIsPlayingMusic = false;
        Dictionary<PikminAnimationPack, int> CachedAnimIndex = new Dictionary<PikminAnimationPack, int>();
        public PikminItem PikItm = null!;

        void Start()
        {
            PikItm = GetComponentInChildren<PikminItem>();
        }

        void Update()
        {
            if (boomBoxInstance.isPlayingMusic != LastIsPlayingMusic)
            {
                LastIsPlayingMusic = boomBoxInstance.isPlayingMusic;
                OnBoomBoxToggle();
            }

            PikItm.settings.GrabableToPikmin = LethalMin.AllowOnBBtobeGrabed || !boomBoxInstance.isPlayingMusic;

            if (DistanceCheckInterval > 0f)
            {
                DistanceCheckInterval -= Time.deltaTime;
            }
            else
            {
                DistanceCheckInterval = 0.2f; // reset interval
                CheckNearbyPikmin();
            }
        }

        void OnBoomBoxToggle()
        {
            pikminInRange.RemoveAll(ai => ai == null); // clean up any null references
            if (!boomBoxInstance.isPlayingMusic)
            {
                foreach (PikminAI? ai in pikminInRange)
                {
                    if (ai == null)
                    {
                        continue; // skip null pikmin
                    }
                    ai.animController.RandomIdle = 0;
                }
                pikminInRange.Clear();
                return;
            }
        }
        void CheckNearbyPikmin()
        {
            if (!boomBoxInstance.isPlayingMusic)
            {
                pikminInRange.Clear();
                return; // don't check if boombox isn't playing music
            }
            foreach (PikminAI ai in PikminManager.instance.PikminAIs)
            {
                float dist = Vector3.Distance(ai.transform.position, boomBoxInstance.transform.position);

                if (dist < maxDistance)
                {
                    if (!pikminInRange.Contains(ai))
                    {
                        pikminInRange.Add(ai);
                    }
                }
                else
                {
                    if (pikminInRange.Contains(ai))
                    {
                        pikminInRange.Remove(ai);
                    }
                }
            }

            foreach (PikminAI? ai in pikminInRange)
            {
                if (ai == null)
                {
                    continue; // skip null pikmin
                }
                if (!CachedAnimIndex.ContainsKey(ai.animController.AnimPack))
                {
                    CachedAnimIndex[ai.animController.AnimPack] = SpecialPikminIdleInteraction.FindIdleIndexForSpecialAnim(ai, "dance");
                    LethalMin.Logger.LogInfo($"BoomBoxPikminInteraction: Cached idle index for {ai.DebugID} in pack {ai.animController.AnimPack.name} is {CachedAnimIndex[ai.animController.AnimPack]}");
                }
                ai.animController.RandomIdle = CachedAnimIndex[ai.animController.AnimPack];
            }
        }
    }
}
