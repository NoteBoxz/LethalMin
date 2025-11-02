using System.Collections;
using System.Collections.Generic;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class PuffminLeader : NetworkBehaviour
    {
        public EnemyAI AI = null!;
        public PuffminAI? PuffminHolding = null;
        public Transform HoldPos = null!;
        public List<PuffminAI> PuffminInSquad = new List<PuffminAI>();
        public PuffminNoticeZone noticeZone = null!;
        public AudioSource WhistleAudioSource = null!;
        public AudioClip WhistleClip = null!;
        public Transform ThrowOrigin = null!;
        public bool lastOutsideValue = false;
        public bool IsWhistleing = false;
        float tweenDuration = 1.5f;
        public float minWhistleZoneRadius = 1f;
        public float maxWhistleZoneRadius = 15f;
        public bool UseOverrideThrowDirection = false;
        public Vector3 OverrideThrowDirection = Vector3.zero;
        Coroutine whistleZoneTween = null!;

        public void Start()
        {
            if (AI == null)
                AI = GetComponent<EnemyAI>();

            lastOutsideValue = AI.isOutside;
            PikminManager.instance.PuffminLeaders.Add(this);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            PikminManager.instance.PuffminLeaders.Remove(this);
        }

        public void Update()
        {
            if (IsOwner && PuffminInSquad.Count > 0 && lastOutsideValue != AI.isOutside)
            {
                lastOutsideValue = AI.isOutside;
                SetPuffminToEntranceServerRpc(AI.isOutside);
            }
        }

        #region AI Functions
        public void StartWhistleing()
        {
            if (!IsOwner)
            {
                LethalMin.Logger.LogError($"{gameObject.name}: AIFunc 'StartWhistleing' called by non-owner client!");
                return;
            }

            StartWhistleingOnLocalClient();
            if (IsServer)
            {
                StartWhistleingClientRpc();
            }
            else
            {
                StartWhistleingServerRpc();
            }
        }

        [ServerRpc]
        private void StartWhistleingServerRpc()
        {
            StartWhistleingClientRpc();
        }
        [ClientRpc]
        private void StartWhistleingClientRpc()
        {
            if (IsOwner)
            {
                return;
            }
            StartWhistleingOnLocalClient();
        }
        public void StartWhistleingOnLocalClient()
        {
            if (whistleZoneTween != null)
            {
                StopCoroutine(whistleZoneTween);
                whistleZoneTween = null!;
            }
            noticeZone.gameObject.SetActive(true);
            whistleZoneTween = StartCoroutine(TweenSize());
            WhistleAudioSource.PlayOneShot(WhistleClip);
            IsWhistleing = true;
            LethalMin.Logger.LogDebug($"{gameObject.name}: Started Whistleing");
        }

        public void StopWhistleing()
        {
            if (!IsOwner)
            {
                LethalMin.Logger.LogError($"{gameObject.name}: AIFunc 'StopWhistleing' called by non-owner client!");
                return;
            }

            StopWhistleingOnLocalClient();
            if (IsServer)
            {
                StopWhistleingClientRpc();
            }
            else
            {
                StopWhistleingServerRpc();
            }
        }

        [ServerRpc]
        private void StopWhistleingServerRpc()
        {
            StopWhistleingClientRpc();
        }
        [ClientRpc]
        private void StopWhistleingClientRpc()
        {
            if (IsOwner)
            {
                return;
            }
            StopWhistleingOnLocalClient();
        }
        public void StopWhistleingOnLocalClient()
        {
            if (whistleZoneTween != null)
            {
                StopCoroutine(whistleZoneTween);
                whistleZoneTween = null!;
            }
            noticeZone.gameObject.SetActive(false);
            IsWhistleing = false;
            LethalMin.Logger.LogDebug($"{gameObject.name}: Stopped Whistleing");
        }


        public IEnumerator TweenSize(bool Reversed = false)
        {
            float startingSize = Reversed ? maxWhistleZoneRadius : minWhistleZoneRadius;
            float targetSize = Reversed ? minWhistleZoneRadius : maxWhistleZoneRadius;
            float elapsedTime = 0f;

            noticeZone.transform.localScale = new Vector3(startingSize, startingSize, startingSize);

            while (elapsedTime < tweenDuration)
            {
                float size = Mathf.Lerp(startingSize, targetSize, elapsedTime / tweenDuration);
                noticeZone.transform.localScale = new Vector3(size, size, size);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            noticeZone.transform.localScale = new Vector3(targetSize, targetSize, targetSize);
            whistleZoneTween = null!;
        }

        public void StartThrow()
        {
            if (!IsOwner)
            {
                LethalMin.Logger.LogError($"{gameObject.name}: AIFunc 'StartThrow' called by non-owner client!");
                return;
            }
            if (PuffminInSquad.Count == 0)
            {
                LethalMin.Logger.LogWarning($"{gameObject.name}: AIFunc 'StartThrow' called with no puffmin in squad!");
                return;
            }

            PuffminAI? ClosestPuffmin = PikUtils.GetClosestInstanceOfClassToPosition(transform.position, float.PositiveInfinity, PuffminInSquad);
            if (ClosestPuffmin == null)
            {
                LethalMin.Logger.LogWarning($"{gameObject.name}: AIFunc 'StartThrow' called with no puffmin in squad!");
                return;
            }

            StartThrowOnLocalClient(ClosestPuffmin);
            if (IsServer)
            {
                StartThrowClientRpc(ClosestPuffmin.NetworkObject);
            }
            else
            {
                StartThrowServerRpc(ClosestPuffmin.NetworkObject);
            }
        }

        [ServerRpc]
        private void StartThrowServerRpc(NetworkObjectReference PuffRef)
        {
            StartThrowClientRpc(PuffRef);
        }
        [ClientRpc]
        private void StartThrowClientRpc(NetworkObjectReference PuffRef)
        {
            if (IsOwner)
            {
                return;
            }
            if (!PuffRef.TryGet(out NetworkObject puffObj))
            {
                LethalMin.Logger.LogError($"{gameObject.name}: AIFunc 'StartThrow' called with invalid puffmin reference!");
                return;
            }
            if (!puffObj.TryGetComponent(out PuffminAI puffmin))
            {
                LethalMin.Logger.LogError($"{gameObject.name}: AIFunc 'StartThrow' called with invalid puffmin script!");
                return;
            }
            StartThrowOnLocalClient(puffmin);
        }
        private void StartThrowOnLocalClient(PuffminAI ClosestPuffmin)
        {
            PuffminHolding = ClosestPuffmin;
            ClosestPuffmin.StartThrow();
            LethalMin.Logger.LogDebug($"{gameObject.name}: Started Throw");
        }


        public void DoThrow()
        {
            if (!IsOwner)
            {
                LethalMin.Logger.LogError($"{gameObject.name}: AIFunc 'DoThrow' called by non-owner client!");
                return;
            }
            if (PuffminHolding == null)
            {
                LethalMin.Logger.LogWarning($"{gameObject.name}: AIFunc 'DoThrow' called with no puffmin in hand!");
                return;
            }
            Vector3 direct = UseOverrideThrowDirection ? OverrideThrowDirection : HoldPos.transform.forward;
            DoThrowOnLocalClient(direct);
            if (IsServer)
            {
                DoThrowClientRpc(direct);
            }
            else
            {
                DoThrowServerRpc(direct);
            }
        }

        [ServerRpc]
        private void DoThrowServerRpc(Vector3 Direction)
        {
            DoThrowClientRpc(Direction);
        }
        [ClientRpc]
        private void DoThrowClientRpc(Vector3 Direction)
        {
            if (IsOwner)
            {
                return;
            }
            DoThrowOnLocalClient(Direction);
        }
        private void DoThrowOnLocalClient(Vector3 Direction)
        {
            if (PuffminHolding == null)
            {
                LethalMin.Logger.LogWarning($"{gameObject.name}: AIFunc 'DoThrow' called with no puffmin in hand!");
                return;
            }
            PuffminHolding.ThrowPuffmin(Direction);
            PuffminHolding.RemoveLeader();
            PuffminHolding.WhistleBuffer = 3;
            PuffminHolding = null;
            LethalMin.Logger.LogDebug($"{gameObject.name}: did throw");
        }

        #endregion


        #region Squad Management
        public void AddPuffminToSquad(PuffminAI puffmin)
        {
            PuffminInSquad.Add(puffmin);
        }

        public void RemovePuffminFromSquad(PuffminAI puffmin)
        {
            PuffminInSquad.Remove(puffmin);
        }

        [ServerRpc]
        public void SetPuffminToEntranceServerRpc(bool isOutside)
        {
            SetPuffminToEntranceClientRpc(isOutside);
        }
        [ClientRpc]
        public void SetPuffminToEntranceClientRpc(bool isOutside)
        {
            SetPuffminToEntrance(isOutside);
        }

        public void SetPuffminToEntrance(bool isOutside)
        {
            LethalMin.Logger.LogInfo($"{gameObject.name}: Setting {PuffminInSquad.Count} puffmin to entrance: {isOutside}");
            foreach (PuffminAI puff in PuffminInSquad)
            {
                puff.isOutside = isOutside;

                if (IsOwner)
                {
                    puff.agent.Warp(transform.position);
                }
                puff.transform2.TeleportOnLocalClient(transform.position, transform.rotation);
            }
        }
        #endregion
    }
}
