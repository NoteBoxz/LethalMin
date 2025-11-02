using System.Collections;

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class MouthDogPikminEnemy : PikminEnemy
    {
        List<NetworkObjectReference> PikminRefs = new List<NetworkObjectReference>();
        MouthDogAI mouthDogAI = null!;
        float CheckInterval = 0.25f;
        float BiteCooldown = 1.5f;
        public float BiteResetCooldown = 5.5f;
        public int BiteLimmit = 7;

        protected override void Start()
        {
            base.Start();
            mouthDogAI = enemyScript as MouthDogAI ?? throw new System.Exception("MouthDogPE: enemyScript is not a MouthDogAI");
            if (mouthDogAI == null)
            {
                enabled = false;
            }
            if (LethalMin.UseConfigsForEnemies)
            {
                BiteCooldown = LethalMin.MouthDog_BiteCooldown.InternalValue;
            }
        }

        void LateUpdate()
        {
            if (!IsOwner)
            {
                return;
            }
            if (!mouthDogAI.inLunge || mouthDogAI.inKillAnimation)
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
                BiteResetCooldown = LethalMin.MouthDog_BiteCooldown.InternalValue;
                BiteLimmit = LethalMin.MouthDog_BiteLimit.InternalValue;
            }

            if (CheckInterval > 0)
            {
                CheckInterval -= Time.deltaTime;
            }
            else
            {
                DoCheckInterval();
                CheckInterval = mouthDogAI.AIIntervalTime + Random.Range(-0.015f, 0.015f);
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
                if (Vector3.Distance(ai.transform.position, mouthDogAI.transform.position) < 5f)
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
            LethalMin.Logger.LogMessage($"Biting {Pikmins.Count} Pikmin");
            BiteCooldown = BiteResetCooldown;
            foreach (PikminAI pikminAI in Pikmins)
            {
                pikminAI.DeathSnapToPos = mouthDogAI.mouthGrip;
                pikminAI.OverrideDelay = 2.5f;
                pikminAI.HitEnemy(5);
            }
            StartCoroutine(DoBiteAnim());
        }

        IEnumerator DoBiteAnim()
        {
            if (mouthDogAI.IsOwner)
            {
                mouthDogAI.agent.speed = Mathf.Clamp(mouthDogAI.agent.speed, 2f, 0f);
            }
            Debug.Log("killing pikmin A");
            mouthDogAI.creatureVoice.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
            mouthDogAI.creatureVoice.PlayOneShot(mouthDogAI.killPlayerSFX, 1f);
            mouthDogAI.creatureAnimator.SetTrigger("EndLungeKill"); //LungeKill
            mouthDogAI.inKillAnimation = true;
            Debug.Log("killing pikmin B");
            float startTime = Time.timeSinceLevelLoad;
            Quaternion rotateTo = Quaternion.Euler(new Vector3(0f, RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(transform.position + Vector3.up * 0.6f), 0f));
            Quaternion rotateFrom = transform.rotation;
            while (Time.timeSinceLevelLoad - startTime < 2f)
            {
                yield return null;
                if (mouthDogAI.IsOwner)
                {
                    mouthDogAI.transform.rotation = Quaternion.RotateTowards(rotateFrom, rotateTo, 60f * Time.deltaTime);
                }
            }
            yield return new WaitForSeconds(3.01f);
            mouthDogAI.suspicionLevel = 2;
            mouthDogAI.SwitchToBehaviourStateOnLocalClient(2);
            mouthDogAI.inKillAnimation = false;
            mouthDogAI.endingLunge = true;
            mouthDogAI.inLunge = false;
            mouthDogAI.lungeCooldown = 0.1f;
        }
    }
}
