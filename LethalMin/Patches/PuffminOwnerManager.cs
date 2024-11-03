using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace LethalMin
{
    public class PuffminOwnerManager : NetworkBehaviour
    {
        public EnemyAI Controller = null!;
        public List<PuffminAI> followingPuffmin = new List<PuffminAI>();
        [SerializeField] private float whistleZoneRadius = 5f;
        [SerializeField] private float minWhistleZoneRadius = 1f;
        [SerializeField] private float maxWhistleZoneRadius = 15f;
        [SerializeField] private float tweenDuration = 0.5f;
        private Coroutine tweenCoroutine;
        public NoticeZone? noticeZone;

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
            puffmin.OwnerAI = Controller;
        }
        public void RemovePuffmin(PuffminAI puffmin)
        {
            if (followingPuffmin.Contains(puffmin))
                followingPuffmin.Remove(puffmin);

            puffmin.OwnerAI = null;
        }

        public void DoWhistle()
        {

            tweenCoroutine = StartCoroutine(TweenWhistleZoneRadius(minWhistleZoneRadius, maxWhistleZoneRadius));

        }
        private IEnumerator TweenWhistleZoneRadius(float startRadius, float endRadius)
        {
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
        }

        private void UpdateWhistleZoneScale()
        {
            if (noticeZone != null)
            {
                noticeZone.transform.localScale = new Vector3(whistleZoneRadius, whistleZoneRadius, whistleZoneRadius);
            }
        }

    }
}