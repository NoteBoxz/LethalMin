using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
namespace LethalMin
{
    public class PuffminOwnerManager : NetworkBehaviour, IDebuggable
    {
        public EnemyAI Controller = null!;
        [IDebuggable.Debug] public List<PuffminAI> followingPuffmin = new List<PuffminAI>();
        [SerializeField] private float whistleZoneRadius = 5f;
        [SerializeField] private float minWhistleZoneRadius = 1f;
        [SerializeField] private float maxWhistleZoneRadius = 10f;
        [SerializeField] private float tweenDuration = 1f;
        private Coroutine tweenCoroutine;
        public NoticeZone? noticeZone;
        public AudioSource? whistleSound;
        public bool HasInteractedWithPuffmin;

        void Awake()
        {
            whistleSound = gameObject.AddComponent<AudioSource>();
            whistleSound.clip = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Whisle/Audio/maskedwhistle.wav");
            whistleSound.spatialBlend = 1f;
            whistleSound.minDistance = 5f;
            whistleSound.maxDistance = 30f;
        }

        [ClientRpc]
        public void InitializeClientRpc(NetworkObjectReference controller)
        {
            controller.TryGet(out NetworkObject NOEcontroller);
            EnemyAI Econtroller = NOEcontroller.GetComponent<EnemyAI>();

            Controller = Econtroller;
        }

        public void AddPuffmin(PuffminAI puffmin)
        {
            if (followingPuffmin.Contains(puffmin))
                return;

            followingPuffmin.Add(puffmin);
            puffmin.AssignOwner(Controller);
        }
        public void RemovePuffmin(PuffminAI puffmin)
        {
            if (followingPuffmin.Contains(puffmin))
                followingPuffmin.Remove(puffmin);

            puffmin.PrevOwnerAI = puffmin.OwnerAI;
            puffmin.OwnerAI = null;
        }
        void LateUpdate()
        {
            noticeZone.gameObject.SetActive(isdoingwhistle);
            if (!HasInteractedWithPuffmin)
            {
                if (followingPuffmin.Count > 0)
                    HasInteractedWithPuffmin = true;
            }

            if (HasInteractedWithPuffmin && isdoingwhistle)
            {
                //UpdateWhistleZonePosition();
            }
            else
            {
                //ResetWhistleZonePosition();
            }
            //maxWhistleZoneRadius = LethalMin.WhistleMaxRadius.Value / 2;
        }

        public bool isAiming = false;
        public void DoThrow()
        {
            followingPuffmin.RemoveAll(puffmin => puffmin == null || puffmin.NetworkObject == null);
            if (followingPuffmin.Count == 0)
            {
                LethalMin.Logger.LogWarning("No Puffmin available to throw or NetworkObject is null.");
                isAiming = false;
                return;
            }
            if (isAiming)
                return;
            isAiming = true;
            //get the closest puffmin
            PuffminAI closestPuffmin = null;
            float closestDistance = float.MaxValue;
            foreach (PuffminAI puffmin in followingPuffmin)
            {
                if (puffmin.HasInitalized == false)
                {
                    continue;
                }
                if (puffmin.IsHeld || puffmin.IsThrown)
                {
                    continue;
                }
                float distance = Vector3.Distance(puffmin.transform.position, Controller.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPuffmin = puffmin;
                }
            }
            if (closestPuffmin == null)
            {
                LethalMin.Logger.LogWarning("No Puffmin available to throw or NetworkObject is null.");
                isAiming = false;
                return;
            }
            closestPuffmin.HoldPuffmin(Controller.eye);
            StartCoroutine(WaitToThrowPuffmin(closestPuffmin));
        }
        private IEnumerator WaitToThrowPuffmin(PuffminAI puffmin)
        {
            yield return new WaitUntil(() => Controller.targetPlayer == null
            || Controller.targetPlayer.isPlayerDead
            || Vector3.Distance(Controller.transform.position, Controller.targetPlayer.transform.position) < 20);

            puffmin.ThrowPuffmin(Controller.eye.position, Controller.transform.forward);
            RemovePuffmin(puffmin);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0, 1.0f));
            isAiming = false;
        }

        bool isdoingwhistle = false;
        public void DoWhistle()
        {
            if (isdoingwhistle)
                return;

            tweenCoroutine = StartCoroutine(TweenWhistleZoneRadius(minWhistleZoneRadius, maxWhistleZoneRadius));
        }
        private IEnumerator TweenWhistleZoneRadius(float startRadius, float endRadius)
        {
            isdoingwhistle = true;
            whistleSound.Play();
            float elapsedTime = 0f;
            while (elapsedTime < tweenDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / tweenDuration);
                float currentRadius = Mathf.Lerp(startRadius, endRadius, t);
                whistleZoneRadius = currentRadius;
                UpdateWhistleZoneScale();
                yield return null;
            }
            whistleZoneRadius = endRadius;
            UpdateWhistleZoneScale();
            yield return new WaitForSeconds(1f);
            float elapsedTime2 = 0f;
            while (elapsedTime2 < tweenDuration)
            {
                elapsedTime2 += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime2 / tweenDuration);
                float currentRadius = Mathf.Lerp(endRadius, 0, t);
                whistleZoneRadius = currentRadius;
                UpdateWhistleZoneScale();
                yield return null;
            }
            whistleZoneRadius = 0;
            UpdateWhistleZoneScale();
            yield return new WaitForSeconds(3f);
            isdoingwhistle = false;
        }

        private void UpdateWhistleZoneScale()
        {
            if (noticeZone != null)
            {
                noticeZone.transform.localScale = new Vector3(whistleZoneRadius, whistleZoneRadius, whistleZoneRadius);
            }
        }
        private void UpdateWhistleZonePosition()
        {
            const float checkRange = 25f; // The range to check for Puffmin
            float whistleZoneOffset = 5f; // How far in front of the player the whistle zone should be

            bool puffminInRange = false;
            foreach (GameObject puffmin in PikminManager._currentPuffminEnemies)
            {
                if (Vector3.Distance(Controller.transform.position, puffmin.transform.position) <= checkRange)
                {
                    puffminInRange = true;
                    whistleZoneOffset = Vector3.Distance(Controller.transform.position, puffmin.transform.position);
                    break;
                }
            }

            if (puffminInRange)
            {
                Vector3 forward = Controller.transform.forward;
                Vector3 targetPosition = Controller.transform.position + forward * whistleZoneOffset;
                noticeZone.transform.position = targetPosition;
            }
            else
            {
                ResetWhistleZonePosition();
            }
        }

        private void ResetWhistleZonePosition()
        {
            noticeZone.transform.localPosition = Vector3.zero;
        }

        public void TeleportPuffminToOwner()
        {
            followingPuffmin.RemoveAll(puffmin => puffmin == null || puffmin.NetworkObject == null);
            foreach (PuffminAI puffmin in followingPuffmin)
            {
                if (puffmin.IsHeld) continue;
                
                puffmin.agent.Warp(Controller.transform.position);
                puffmin.transform2.Teleport(Controller.transform.position, Controller.transform.rotation, puffmin.transform.localScale);
            }
        }

    }
}