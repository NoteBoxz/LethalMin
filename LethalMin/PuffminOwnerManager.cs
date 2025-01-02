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
        public NoticeZone noticeZone;
        public AudioSource whistleSound;
        public bool HasInteractedWithPuffmin;
        public int TakeDownRequests = 0;

        void Awake()
        {
            whistleSound = gameObject.AddComponent<AudioSource>();
            whistleSound.playOnAwake = false;
            whistleSound.clip = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Whisle/Audio/maskedwhistle.wav");
            whistleSound.spatialBlend = 1f;
            whistleSound.minDistance = 5f;
            whistleSound.maxDistance = 30f;
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddToTakeDownRequestsServerRpc()
        {
            TakeDownRequests++;

            if (TakeDownRequests >= StartOfRound.Instance.fullyLoadedPlayers.Count - 1)
            {
                LethalMin.Logger.LogFatal("All players have requested to take down the puffmin manager due to failed initalizeation. Deleting puffmin owner manager.");
                if (noticeZone != null)
                {
                    noticeZone.NetworkObject.transform.SetParent(null);
                    noticeZone.NetworkObject.Despawn(true);
                }

                NetworkObject.transform.SetParent(null);
                NetworkObject.Despawn(true);
            }
        }

        [ClientRpc]
        public void InitalizeRefsClientRpc(NetworkObjectReference zoneRef, NetworkObjectReference POMref, NetworkObjectReference MPEref)
        {
            NoticeZone zone = null!;
            EnemyAI __instance = null!;
            PuffminOwnerManager pom = null!;

            if (zoneRef.TryGet(out NetworkObject zoneNetworkObject))
            {
                zone = zoneNetworkObject.GetComponent<NoticeZone>();
            }
            else
            {
                LethalMin.Logger.LogError("Could not find the NoticeZone network object.");
                AddToTakeDownRequestsServerRpc();
                zone.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            if (POMref.TryGet(out NetworkObject POMNetworkObject))
            {
                pom = POMNetworkObject.GetComponent<PuffminOwnerManager>();
            }
            else
            {
                LethalMin.Logger.LogError("Could not find the PuffminOwnerManager network object.");
                AddToTakeDownRequestsServerRpc();
                zone.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            if (MPEref.TryGet(out NetworkObject MPENetworkObject))
            {
                __instance = MPENetworkObject.GetComponent<MaskedPlayerEnemy>();
            }
            else
            {
                LethalMin.Logger.LogError("Could not find the MaskedPlayerEnemy network object.");
                AddToTakeDownRequestsServerRpc();
                zone.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            zone.CanConvertPikmin = true;
            zone.InstantNotice = true;
            zone.UseCheckSpher = true;
            zone.gameObject.GetComponent<Renderer>().material.color = new Color(0.5f, 0f, 0.5f, 0.5f);
            zone.gameObject.AddComponent<MeshNoiseDistorter>().distortionStrength = 0.25f;
            zone.enemy = __instance;
            noticeZone = zone;
            Controller = __instance;
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
            CheckAndDespawnIfParentDestroyed();
            if (IsServer && noticeZone == null)
            {
                LethalMin.Logger.LogError("PuffminOwnerManager has no NoticeZone! Destorying to prevent further issues.");
                NetworkObject.Despawn(true);
                if (noticeZone != null)
                    noticeZone.NetworkObject.Despawn(true);
                return;
            }
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
            maxWhistleZoneRadius = LethalMin.MaskedWhistleRange;
            whistleSound.volume = LethalMin.MaskedWhistleVolume;
        }
        private void CheckAndDespawnIfParentDestroyed()
        {
            if (!IsServer)
            {
                return;
            }
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                if (transform.parent == null || transform.parent.gameObject == null)
                {
                    // Parent has been destroyed, despawn this NetworkObject
                    if (IsServer)
                    {
                        NetworkObject.Despawn(true);
                        if (noticeZone != null)
                            noticeZone.NetworkObject.Despawn(true);
                    }
                    LethalMin.Logger.LogInfo($"PuffminOwnerManager and notice zone {name} despawned due to destroyed parent");
                }
            }
            if (Controller == null)
            {
                LethalMin.Logger.LogWarning($"{name} has no Controller! Looking in parent...");
                if (transform.parent != null)
                    Controller = GetComponentInParent<EnemyAI>();
                if (Controller == null)
                {
                    LethalMin.Logger.LogError($"{name} still has no Controller after checking parent!");
                    if (IsServer)
                    {
                        NetworkObject.Despawn(true);
                        if (noticeZone != null)
                            noticeZone.NetworkObject.Despawn(true);
                    }
                }
            }
        }
        public bool isAiming = false;
        public void DoThrow()
        {
            followingPuffmin.RemoveAll(puffmin => puffmin == null || puffmin.NetworkObject == null);
            if (followingPuffmin.Count == 0)
            {
                //LethalMin.Logger.LogWarning("No Puffmin available to throw or NetworkObject is null.");
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
        int whistleinterval = 3;
        public void DoWhistle()
        {
            if (whistleinterval > 0)
            {
                whistleinterval--;
                return;
            }
            if (isdoingwhistle)
                return;

            DoWhistleTweenClientRpc();
        }
        [ClientRpc]
        public void DoWhistleTweenClientRpc()
        {
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
            foreach (PuffminAI puffmin in PikminManager.GetPuffminEnemies())
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