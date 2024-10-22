using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;
using Unity.Netcode;
using System.Collections.Generic;
using GameNetcodeStuff;


namespace LethalMin
{
    public class WhistleItem : GrabbableObject
    {
        public GameObject WhistleZone;

        [SerializeField] private float whistleZoneRadius = 5f;
        [SerializeField] private float whistleZoneHeight = 0.1f;
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
        private Coroutine WhistleCoroutine;
        public LineRenderer lineRenderer;
        [SerializeField] private int linePoints = 10;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private AudioClip DissSFX;
        private Transform EndPoint;
        private AudioSource aud;
        [SerializeField] private Animator anim;
        public override void EquipItem()
        {
            base.EquipItem();
        }
        public override void Start()
        {
            base.Start();
            anim = GetComponentInChildren<Animator>();
            aud = GetComponent<AudioSource>();
            EndPoint = transform.Find("mesh/EndPoint");
            WhistleZone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            WhistleZone.GetComponent<Collider>().enabled = false;
            Renderer whistleZoneRenderer = WhistleZone.GetComponent<Renderer>();
            whistleZoneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            whistleZoneRenderer.receiveShadows = false;
            whistleZoneRenderer.material = LethalMin.lineMaterial;
            lineMaterial = LethalMin.lineMaterial;
            WhistleZone.transform.SetParent(null);
            collidersAndRoomMask = LethalMin.Instance.PikminColideable;
            DissSFX = LethalMin.DissSFX;
            WhistleZone.SetActive(false);

            // Create and set up the custom input action
            whistleAction = new InputAction("Whistle", InputActionType.Button, LethalMin.WhisleAction);
            whistleAction.started += ctx => OnWhistleStarted();
            whistleAction.performed += ctx => OnWhistlePerformed();
            whistleAction.canceled += ctx => OnWhistleCanceled();
            whistleAction.Enable();

            removeAllPikminAction = new InputAction("Dismiss", InputActionType.Button, LethalMin.DismissAction);
            removeAllPikminAction.started += ctx => OnDismiss();
            removeAllPikminAction.Enable();

            // Set up LineRenderer
            lineRenderer.positionCount = linePoints;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = lineMaterial;
            lineRenderer.enabled = false;


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

            // Call the server RPC to initialize the whistle on all clients
            if (IsServer)
            {
                InitializeWhistleServerRpc();
            }
        }
        [ServerRpc(RequireOwnership = false)]
        private void InitializeWhistleServerRpc()
        {
            InitializeWhistleClientRpc();
        }
        [ClientRpc]
        private void InitializeWhistleClientRpc()
        {
            if (!IsServer)
                InitializeWhistle();
        }
        private void InitializeWhistle()
        {
            WhistleZone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            WhistleZone.GetComponent<Collider>().enabled = false;
            Renderer whistleZoneRenderer = WhistleZone.GetComponent<Renderer>();
            whistleZoneRenderer.material = LethalMin.lineMaterial;
            whistleZoneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            whistleZoneRenderer.receiveShadows = false;
            WhistleZone.transform.SetParent(null);
            WhistleZone.SetActive(false);

            lineRenderer.positionCount = linePoints;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = LethalMin.lineMaterial;
            lineRenderer.enabled = false;
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
                yield return null;
            }
            whistleZoneRadius = endRadius;
        }
        // IEnumerator WhistleTimer()
        // {
        //     aud.Play();
        //     yield return new WaitForSeconds(WhistleSFX.length);
        //     OnWhistleCanceled();
        // }
        private void OnWhistleStarted()
        {
            if (playerCamera == null || playerHeldBy == null || !isHeld || isPocketed)
            {
                return;
            }
            if (base.IsOwner)
            {
                anim.SetBool("whistleing", true);
                tweenCoroutine = StartCoroutine(TweenWhistleZoneRadius(minWhistleZoneRadius, maxWhistleZoneRadius));
                aud.volume = LethalMin.WhistleVolume;
                aud.Play();
                //WhistleCoroutine = StartCoroutine(WhistleTimer());
            }
        }
        // In WhistleItem.cs

        private void OnDismiss()
        {
            if (playerCamera == null || playerHeldBy == null || !isHeld || isPocketed)
            {
                return;
            }
            if (base.IsOwner)
            {
                LeaderManager[] allLeaderManagers = FindObjectsOfType<LeaderManager>();
                foreach (LeaderManager lm in allLeaderManagers)
                {
                    if (lm.Controller == playerHeldBy)
                    {
                        anim.SetTrigger("diss");
                        float loudness = 1f;
                        aud.PlayOneShot(DissSFX, loudness);
                        WalkieTalkie.TransmitOneShotAudio(aud, DissSFX, loudness);
                        if (!LethalMin.LethaDogs2Value)
                            RoundManager.Instance.PlayAudibleNoise(WhistleZone.transform.position, maxWhistleZoneRadius, 1, 1, isInElevator && StartOfRound.Instance.hangarDoorsClosed);

                        // Call the new method to dismiss Pikmin
                        lm.DismissAllExceptSelectedTypeServerRpc();
                    }
                }
            }
        }
        private void OnWhistlePerformed()
        {
            if (playerCamera == null || playerHeldBy == null || !isHeld || isPocketed)
            {
                return;
            }
            if (base.IsOwner)
            {
                isWhistling = true;
                WhistleZone.SetActive(true);
                lineRenderer.enabled = true;

                if (aud != null)
                {
                    aud.volume = LethalMin.WhistleVolume;
                    if (!LethalMin.LethaDogs2Value)
                        RoundManager.Instance.PlayAudibleNoise(WhistleZone.transform.position, maxWhistleZoneRadius, 1, 1, isInElevator && StartOfRound.Instance.hangarDoorsClosed);

                    if (playerHeldBy != null)
                    {
                        playerHeldBy.timeSinceMakingLoudNoise = 0f;
                    }
                }
            }
        }

        private void OnWhistleCanceled()
        {
            if (playerCamera == null || playerHeldBy == null || !isHeld || isPocketed)
            {
                return;
            }
            if (base.IsOwner)
            {
                anim.SetBool("whistleing", false);
                aud.Stop();
                isWhistling = false;
                lineRenderer.enabled = false;
                if (tweenCoroutine != null)
                {
                    StopCoroutine(tweenCoroutine);

                }
                if (WhistleCoroutine != null)
                {
                    StopCoroutine(WhistleCoroutine);
                }
            }
        }
        IEnumerator CheckForPikminInWhistleZone()
        {
            // Get all colliders within the whistle zone radius
            Collider[] colliders = Physics.OverlapSphere(WhistleZone.transform.position, whistleZoneRadius);

            foreach (Collider collider in colliders)
            {
                if (collider == null) { continue; }
                if (collider.name != "PikminColision") { continue; }
                yield return new WaitForSeconds(0.01f);
                // Check if the collider belongs to a PikminAI
                PikminAI pikminAI = collider.GetComponentInParent<PikminAI>();
                if (pikminAI != null && pikminAI.IsInCallableState() && !pikminAI.CannotEscape)
                {
                    // Assign the LeaderManager from the player holding the whistle to the Pikmin
                    if (playerHeldBy != null)
                    {
                        pikminAI.AssignLeader(playerHeldBy);
                    }
                }
                else if (pikminAI != null && pikminAI.IsDrowing)
                {
                    if (!IsServer)
                    {
                        if (pikminAI.whistlingPlayer == null)
                            SavePikminServerRpc(new NetworkObjectReference(pikminAI.NetworkObject), new NetworkObjectReference(playerHeldBy.NetworkObject));
                    }
                    else
                    {
                        pikminAI.whistlingPlayer = playerHeldBy;
                    }
                }
            }
        }
        [ServerRpc(RequireOwnership = false)]
        public void SavePikminServerRpc(NetworkObjectReference Pikref, NetworkObjectReference Playref)
        {
            if (Pikref.TryGet(out NetworkObject Pobj) && Playref.TryGet(out NetworkObject Plobj))
            {
                Pobj.GetComponent<PikminAI>().whistlingPlayer = Plobj.GetComponent<PlayerControllerB>();
            }
        }
        private void UpdateWhistleZonePosition()
        {
            if (playerCamera == null || playerHeldBy == null)
            {
                return;
            }

            // Calculate the ray from the camera
            Ray cameraRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 rayDirection = cameraRay.direction;

            // Calculate the position in front of the camera
            Vector3 startPosition = playerCamera.transform.position;
            Vector3 endPosition = startPosition + rayDirection * whistleZoneOffset;

            // Raycast from the camera position towards the end position
            if (Physics.Raycast(startPosition, rayDirection, out RaycastHit hit, whistleZoneOffset, collidersAndRoomMask))
            {
                // If we hit something, place the whistle zone at the hit point
                WhistleZone.transform.position = hit.point + hit.normal * (whistleZoneHeight / 2);
            }
            else
            {
                // If we didn't hit anything, do a downward raycast from the end position
                if (Physics.Raycast(endPosition, Vector3.down, out hit, maxRaycastDistance, collidersAndRoomMask))
                {
                    WhistleZone.transform.position = hit.point + hit.normal * (whistleZoneHeight / 2);
                }
                else
                {
                    // If we still didn't hit anything, just use the end position
                    WhistleZone.transform.position = endPosition;
                }
            }
        }
        private void UpdateLineRenderer()
        {
            if (lineRenderer == null || WhistleZone == null) return;

            Vector3 startPoint = EndPoint.position;
            Vector3 endPoint = WhistleZone.transform.position;

            for (int i = 0; i < linePoints; i++)
            {
                float t = i / (float)(linePoints - 1);
                Vector3 point = Vector3.Lerp(startPoint, endPoint, t);

                // Add a slight arc to the line
                float arc = Mathf.Sin(t * Mathf.PI) * 0.5f;
                point += Vector3.up * arc;

                lineRenderer.SetPosition(i, point);
            }
        }
        public override void Update()
        {
            base.Update();
            WhistleZone.SetActive(isWhistling);
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                    lineRenderer.positionCount = linePoints;
                    lineRenderer.startWidth = lineWidth;
                    lineRenderer.endWidth = lineWidth;
                    lineRenderer.material = lineMaterial;
                    lineRenderer.enabled = false;
                }
            }
            lineRenderer.enabled = isWhistling;
            playerCamera = playerHeldBy == null ? Camera.main : playerHeldBy.gameplayCamera;
            WhistleZone.transform.localScale = new Vector3(whistleZoneRadius, whistleZoneRadius, whistleZoneRadius);
            if (isWhistling && base.IsOwner && isHeld && !isPocketed)
            {
                UpdateWhistleZonePosition();
                StartCoroutine(CheckForPikminInWhistleZone());
                UpdateLineRenderer();
            }
            else if (isWhistling && (!isHeld || isPocketed))
            {
                isWhistling = false;
                lineRenderer.enabled = false;
            }
        }
        private void OnDestroy()
        {
            if (tweenCoroutine != null)
            {
                StopCoroutine(tweenCoroutine);
            }
            if (WhistleCoroutine != null)
            {
                StopCoroutine(WhistleCoroutine);
            }
            if (WhistleZone != null)
            {
                Destroy(WhistleZone);
            }

            // Disable and dispose of the input action
            if (whistleAction != null)
            {
                whistleAction.Disable();
                whistleAction.Dispose();
            }
            if (lineRenderer != null)
            {
                Destroy(lineRenderer);
            }
        }
    }
}