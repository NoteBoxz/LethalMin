using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;
using Unity.Netcode;
using GameNetcodeStuff;

namespace LethalMin
{
    public class WhistleItem : GrabbableObject
    {
        public NoticeZone noticeZone;

        [SerializeField] private float whistleZoneRadius = 5f;
        [SerializeField] private float whistleZoneOffset = 20f;
        [SerializeField] private float maxRaycastDistance = 20f;

        private LayerMask collidersAndRoomMask;
        private bool isWhistling = false;

        private InputAction whistleAction;
        private InputAction removeAllPikminAction;
        private Camera playerCamera;
        [SerializeField] private float minWhistleZoneRadius = 1f;
        [SerializeField] private float maxWhistleZoneRadius = 15f;
        [SerializeField] private float tweenDuration = 0.5f;
        private Coroutine tweenCoroutine;
        public LineRenderer lineRenderer;
        [SerializeField] private int linePoints = 10;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private AudioClip DissSFX;
        private Transform EndPoint;
        private AudioSource aud;
        [SerializeField] private Animator anim;
        [SerializeField] private GameObject scanNode;
        public override void Start()
        {
            base.Start();
            SetupComponents();
            SetupInputActions();
            SetupLineRenderer();
            SetupTooltips();

            if (IsServer)
            {
                InitializeWhistleServerRpc();
            }
            scanNode = transform.Find("ScanNode").gameObject;
        }

        private void SetupComponents()
        {
            anim = GetComponentInChildren<Animator>();
            aud = GetComponent<AudioSource>();
            EndPoint = transform.Find("mesh/EndPoint");
            collidersAndRoomMask = LethalMin.Instance.PikminColideable;
            DissSFX = LethalMin.DissSFX;
            lineMaterial = LethalMin.lineMaterial;
        }

        private void SetupInputActions()
        {
            if (!LethalMin.IsUsingInputUtils())
            {
                whistleAction = new InputAction("Whistle", InputActionType.Button, LethalMin.WhisleAction);
                whistleAction.started += ctx => OnWhistleStarted();
                whistleAction.performed += ctx => OnWhistlePerformed();
                whistleAction.canceled += ctx => OnWhistleCanceled();
                whistleAction.Enable();

                removeAllPikminAction = new InputAction("Dismiss", InputActionType.Button, LethalMin.DismissAction);
                removeAllPikminAction.started += ctx => OnDismiss();
                removeAllPikminAction.Enable();
            }
            else
            {
                whistleAction = LethalMin.InputClassInstace.Whistle;
                whistleAction.started += ctx => OnWhistleStarted();
                whistleAction.performed += ctx => OnWhistlePerformed();
                whistleAction.canceled += ctx => OnWhistleCanceled();
                whistleAction.Enable();

                removeAllPikminAction = LethalMin.InputClassInstace.Dismiss;
                removeAllPikminAction.started += ctx => OnDismiss();
                removeAllPikminAction.Enable();
            }
        }

        private void SetupLineRenderer()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            lineRenderer.positionCount = linePoints;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = lineMaterial;
            lineRenderer.enabled = false;
        }

        private void SetupTooltips()
        {
            if (whistleAction.controls.Count > 0)
            {
                string buttonName = whistleAction.controls[0].displayName;
                itemProperties.toolTips[0] = $"Whistle: [{buttonName}]";
            }
            else
            {
                itemProperties.toolTips[0] = "Whistle: [Not Bound]";
            }

            if (removeAllPikminAction.controls.Count > 0)
            {
                string buttonName = removeAllPikminAction.controls[0].displayName;
                itemProperties.toolTips[1] = $"Dismiss: [{buttonName}]";
            }
            else
            {
                itemProperties.toolTips[1] = "Dismiss: [Not Bound]";
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void InitializeWhistleServerRpc()
        {
            LethalMin.Logger.LogInfo("InitializeWhistleServerRpc");
            GameObject noticeZoneObj = Instantiate(LethalMin.NoticeZone);
            noticeZone = noticeZoneObj.GetComponent<NoticeZone>();
            noticeZone.NetworkObject.Spawn();
            noticeZone.gameObject.SetActive(false);
            InitializeWhistleClientRpc(noticeZone.NetworkObject);
        }

        public void SyncZone()
        {
            if (IsServer && noticeZone.NetworkObject != null)
            {
                InitializeWhistleClientRpc(noticeZone.NetworkObject);
            }
        }
        [ClientRpc]
        private void InitializeWhistleClientRpc(NetworkObjectReference noticeZoneRef)
        {
            if (noticeZoneRef.TryGet(out NetworkObject noticeZoneObj))
            {
                noticeZone = noticeZoneObj.GetComponent<NoticeZone>();
                noticeZone.gameObject.SetActive(false);
            }
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

        private void OnWhistleStarted()
        {
            if (!CanWhistle()) return;
            if (IsOwner)
            {
                anim.SetBool("whistleing", true);
                PlayWhistleServerRpc();
            }
        }

        public override void GrabItem()
        {
            base.GrabItem();
            noticeZone.SetLeaderOnServerRpc(new NetworkObjectReference(playerHeldBy.NetworkObject));
        }

        [ServerRpc]
        private void PlayWhistleServerRpc()
        {
            PlayWhistleClientRpc();
        }

        [ClientRpc]
        private void PlayWhistleClientRpc()
        {
            aud.volume = LethalMin.WhistleVolume;
            aud.Play();
        }

        private void OnDismiss()
        {
            if (!CanWhistle()) return;

            if (IsOwner)
            {
                anim.SetTrigger("diss");
                PlayDismissSoundServerRpc();
                LeaderManager[] allLeaderManagers = FindObjectsOfType<LeaderManager>();
                foreach (LeaderManager lm in allLeaderManagers)
                {
                    if (lm.Controller == playerHeldBy)
                    {
                        lm.DismissAllExceptSelectedTypeServerRpc();
                    }
                }
            }
        }

        [ServerRpc]
        private void PlayDismissSoundServerRpc()
        {
            PlayDismissSoundClientRpc();
        }

        [ClientRpc]
        private void PlayDismissSoundClientRpc()
        {
            float loudness = 1f;
            aud.PlayOneShot(DissSFX, loudness);
            WalkieTalkie.TransmitOneShotAudio(aud, DissSFX, loudness);
            if (!LethalMin.LethaDogs2Value)
                RoundManager.Instance.PlayAudibleNoise(noticeZone.transform.position, maxWhistleZoneRadius, 1, 1, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
        }

        private void OnWhistlePerformed()
        {
            if (!CanWhistle()) return;

            if (IsOwner)
            {
                SetWhistlingStateServerRpc(true);
            }
        }

        private void OnWhistleCanceled()
        {
            if (!CanWhistle()) return;
            if (IsOwner)
            {
                SetWhistlingStateServerRpc(false);
            }
        }

        [ServerRpc]
        private void SetWhistlingStateServerRpc(bool state)
        {
            isWhistling = state;
            SetWhistlingStateClientRpc(state);
        }

        [ClientRpc]
        private void SetWhistlingStateClientRpc(bool state)
        {
            isWhistling = state;
            if (noticeZone != null)
            {
                noticeZone.gameObject.SetActive(state);
                noticeZone.UseCheckSpher = true;
                noticeZone.IsActive = state;
                noticeZone.InstantNotice = true;
            }
            lineRenderer.enabled = state;
            anim.SetBool("whistleing", state);

            if (state)
            {
                aud.volume = LethalMin.WhistleVolume;
                if (!LethalMin.LethaDogs2Value && noticeZone != null)
                    RoundManager.Instance.PlayAudibleNoise(noticeZone.transform.position, maxWhistleZoneRadius, 1, 1, isInElevator && StartOfRound.Instance.hangarDoorsClosed);

                if (playerHeldBy != null)
                {
                    playerHeldBy.timeSinceMakingLoudNoise = 0f;
                }
                tweenCoroutine = StartCoroutine(TweenWhistleZoneRadius(minWhistleZoneRadius, maxWhistleZoneRadius));
            }
            else
            {
                aud.Stop();
                if (tweenCoroutine != null)
                {
                    StopCoroutine(tweenCoroutine);
                }
            }
        }
        private Quaternion syncedCameraRotation;
        private float syncInterval = 0.05f;
        private float lastSyncTime;
        [ServerRpc]
        private void SyncCameraRotationServerRpc(Quaternion rotation)
        {
            syncedCameraRotation = rotation;
            SyncCameraRotationClientRpc(rotation);
        }

        [ClientRpc]
        private void SyncCameraRotationClientRpc(Quaternion rotation)
        {
            syncedCameraRotation = rotation;
        }
        private void UpdateWhistleZonePosition()
        {
            if (playerHeldBy == null || noticeZone == null) return;

            Vector3 rayDirection = IsOwner ? playerCamera.transform.forward : syncedCameraRotation * Vector3.forward;
            Vector3 startPosition = playerCamera.transform.position;
            Vector3 endPosition = startPosition + rayDirection * whistleZoneOffset;

            if (Physics.Raycast(startPosition, rayDirection, out RaycastHit hit, whistleZoneOffset, collidersAndRoomMask))
            {
                noticeZone.transform.position = hit.point + hit.normal * 0.05f;
            }
            else if (Physics.Raycast(endPosition, Vector3.down, out hit, maxRaycastDistance, collidersAndRoomMask))
            {
                noticeZone.transform.position = hit.point + hit.normal * 0.05f;
            }
            else
            {
                noticeZone.transform.position = endPosition;
            }
        }

        private void UpdateLineRenderer()
        {
            if (lineRenderer == null || noticeZone == null) return;

            Vector3 startPoint = EndPoint.position;
            Vector3 endPoint = noticeZone.transform.position;

            for (int i = 0; i < linePoints; i++)
            {
                float t = i / (float)(linePoints - 1);
                Vector3 point = Vector3.Lerp(startPoint, endPoint, t);
                float arc = Mathf.Sin(t * Mathf.PI) * 0.5f;
                point += Vector3.up * arc;
                lineRenderer.SetPosition(i, point);
            }
        }

        public override void Update()
        {
            base.Update();
            playerCamera = playerHeldBy == null ? Camera.main : playerHeldBy.gameplayCamera;

            if (IsServer)
            {
                maxRaycastDistance = LethalMin.WhisRange;
                minWhistleZoneRadius = LethalMin.WhisMin;
                maxWhistleZoneRadius = LethalMin.WhisMax;
            }

            if (IsOwner && isHeld && !isPocketed)
            {
                if (Time.time - lastSyncTime > syncInterval)
                {
                    SyncCameraRotationServerRpc(playerCamera.transform.rotation);
                    lastSyncTime = Time.time;
                }
            }

            if (isWhistling && isHeld && !isPocketed)
            {
                UpdateWhistleZonePosition();
                UpdateLineRenderer();
            }
            else if (isWhistling && (!isHeld || isPocketed))
            {
                SetWhistlingStateServerRpc(false);
            }
        }

        public override void LateUpdate()
        {
            base.LateUpdate();
            scanNode.SetActive(!isHeld && !isHeldByEnemy);
        }

        private bool CanWhistle()
        {
            return playerCamera != null && playerHeldBy != null && isHeld && !isPocketed;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (tweenCoroutine != null)
            {
                StopCoroutine(tweenCoroutine);
            }
            if (noticeZone != null && IsServer)
            {
                noticeZone.NetworkObject.Despawn(true);
            }
            if (whistleAction != null)
            {
                whistleAction.Disable();
                whistleAction.Dispose();
            }
            if (removeAllPikminAction != null)
            {
                removeAllPikminAction.Disable();
                removeAllPikminAction.Dispose();
            }
        }
    }
}