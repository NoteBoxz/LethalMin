using System.Collections;
using System.Collections.Generic;
using LethalMin;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace LethalMin
{
    public class WhistleItem : GrabbableObject
    {
        public PikminNoticeZone noticeZone = null!;
        public Camera? playerCamera;
        public GameObject Line = null!;
        public Transform WhisStartPoint = null!;
        public bool isWhistling = false;
        public float whistleZoneOffset = 20f;
        public float maxRaycastDistance = 20f;
        public float minWhistleZoneRadius = 1f;
        public float maxWhistleZoneRadius = 30f;
        float tweenDuration = 0.5f;
        private LayerMask collidersAndRoomMask;
        private Coroutine? whistleZoneTween = null;
        private Coroutine? whistleAudioRutine = null;
        public AudioSource audioSource = null!;
        public AudioClip chargeSound = null!, chargeFailSound = null!;
        public InputAction WhistleAction = null!, DismissAction = null!, WhistleSwitchAction = null!, ChargeAction = null!, SecondaryChargeAction = null!;
        public WhistleSoundPack curPack = null!;
        public WhistleSoundPack[] Sounds = new WhistleSoundPack[0];
        public Animator WhistleAnim = null!;
        public NetworkVariable<int> CurrentSoundPackIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        float ChargeCooldown = 0f;
        public static int WhistleAudioID = -1;
        ScanNodeProperties sNode = null!;

        public void Awake()
        {
            if (noticeZone == null)
                noticeZone = GetComponentInChildren<PikminNoticeZone>();

            audioSource = GetComponent<AudioSource>();

            sNode = GetComponentInChildren<ScanNodeProperties>();

            collidersAndRoomMask = LethalMin.PikminColideable;
            InitializeInputAction();

            WhistleAudioID = StartOfRound.Instance.randomMapSeed * 100;
        }

        public override void Update()
        {
            base.Update();
            noticeZone.Active = isWhistling;
            noticeZone.CanSavePikmin = true;
            noticeZone.Visualizer.SetActive(noticeZone.Active);
            sNode.gameObject.SetActive(LethalMin.MakeItemsScanable && !isHeld && !isHeldByEnemy);
            audioSource.volume = LethalMin.WhistleVolume.InternalValue;
            Line.SetActive(noticeZone.Active);
            if (WhistleAnim != null)
                WhistleAnim.SetBool("whistleing", isWhistling);
            if (isWhistling)
            {
                UpdateWhistleZonePosition();
            }
            if (IsOwner && isWhistling && (!isHeld || isPocketed))
            {
                WhistleUseOnLocalClient(false);
                WhistleUseServerRpc(false);
                LethalMin.Logger.LogInfo($"WhistleItem: Whistle stopped because item is not held anymore or is pocketed.");
            }
            if (LethalMin.WhistleZoneRadiusCheat != -1)
            {
                maxRaycastDistance = LethalMin.WhistleZoneDistanceCheat;
            }
            if (LethalMin.WhistleZoneRadiusCheat != -1)
            {
                maxWhistleZoneRadius = LethalMin.WhistleZoneRadiusCheat;
            }
            if (ChargeCooldown >= 0)
            {
                ChargeCooldown -= Time.deltaTime;
            }
        }

        private void InitializeInputAction()
        {
            LethalMin.Logger.LogInfo($"Binding input actions for {name}");
            if (!LethalMin.UseInputUtils)
            {
                string WhistleActionPath = LethalMin.InVRMode ? LethalMin.WhisleVRAction.InternalValue : LethalMin.WhisleAction.InternalValue;
                WhistleAction = new InputAction("Whistle(Item)");
                WhistleAction.AddBinding(WhistleActionPath);
                WhistleAction.performed -= OnWhistleStarted;
                WhistleAction.canceled -= OnWhistleCanceled;
                WhistleAction.performed += OnWhistleStarted;
                WhistleAction.canceled += OnWhistleCanceled;
                WhistleAction.Enable();

                string DismissActionPath = LethalMin.InVRMode ? LethalMin.DismissVRAction.InternalValue : LethalMin.DismissAction.InternalValue;
                DismissAction = new InputAction("Dismiss");
                DismissAction.AddBinding(DismissActionPath);
                DismissAction.canceled -= OnDismiss;
                DismissAction.canceled += OnDismiss;
                DismissAction.Enable();

                string WhistleSwitchActionPath = LethalMin.InVRMode ? LethalMin.SwitchWhistleSoundVRAction.InternalValue : LethalMin.SwitchWhistleSoundAction.InternalValue;
                WhistleSwitchAction = new InputAction("SwitchWhistleSound");
                WhistleSwitchAction.AddBinding(WhistleSwitchActionPath);
                WhistleSwitchAction.performed -= OnWhistleSoundSwitch;
                WhistleSwitchAction.performed += OnWhistleSoundSwitch;
                WhistleSwitchAction.Enable();

                string ChargeActionPath = LethalMin.InVRMode ? LethalMin.ChargeVRAction.InternalValue : LethalMin.ChargeAction.InternalValue;
                ChargeAction = new InputAction("Charge");
                ChargeAction.AddBinding(ChargeActionPath);
                ChargeAction.canceled -= OnCharge;
                ChargeAction.canceled += OnCharge;
                ChargeAction.Enable();
                string SecondaryChargeActionPath = LethalMin.InVRMode ? LethalMin.SecondaryChargeVRAction.InternalValue : "";
                if (!string.IsNullOrEmpty(SecondaryChargeActionPath))
                {
                    SecondaryChargeAction = new InputAction("SecondaryCharge");
                    SecondaryChargeAction.AddBinding(SecondaryChargeActionPath);
                    SecondaryChargeAction.canceled -= OnCharge;
                    SecondaryChargeAction.canceled += OnCharge;
                    SecondaryChargeAction.Enable();
                }
            }
            else
            {
                WhistleAction = LethalMin.InputClassInstace.Whistle;
                WhistleAction.performed -= OnWhistleStarted;
                WhistleAction.canceled -= OnWhistleCanceled;
                WhistleAction.performed += OnWhistleStarted;
                WhistleAction.canceled += OnWhistleCanceled;
                WhistleAction.Enable();

                DismissAction = LethalMin.InputClassInstace.Dismiss;
                DismissAction.canceled -= OnDismiss;
                DismissAction.canceled += OnDismiss;
                DismissAction.Enable();

                WhistleSwitchAction = LethalMin.InputClassInstace.SwitchWhistleAud;
                WhistleSwitchAction.performed -= OnWhistleSoundSwitch;
                WhistleSwitchAction.performed += OnWhistleSoundSwitch;
                WhistleSwitchAction.Enable();

                ChargeAction = LethalMin.InputClassInstace.Charge;
                ChargeAction.canceled -= OnCharge;
                ChargeAction.canceled += OnCharge;
                ChargeAction.Enable();
            }

            SetupTooltips();
        }
        private void SetupTooltips()
        {
            itemProperties.toolTips = new string[4];
            if (WhistleAction.controls.Count > 0)
            {
                string buttonName = WhistleAction.controls[0].displayName;
                itemProperties.toolTips[0] = $"Whistle: [{buttonName}]";
            }
            else
            {
                itemProperties.toolTips[0] = "Whistle: [Not Bound]";
            }

            if (DismissAction.controls.Count > 0)
            {
                string buttonName = DismissAction.controls[0].displayName;
                itemProperties.toolTips[1] = $"Dismiss: [{buttonName}]";
            }
            else
            {
                itemProperties.toolTips[1] = "Dismiss: [Not Bound]";
            }

            if (ChargeAction.controls.Count > 0)
            {
                string buttonName = ChargeAction.controls[0].displayName;
                itemProperties.toolTips[2] = $"Charge: [{buttonName}]";
            }
            else
            {
                itemProperties.toolTips[2] = "Charge: [Not Bound]";
            }

            if (WhistleSwitchAction.controls.Count > 0)
            {
                string buttonName = WhistleSwitchAction.controls[0].displayName;
                itemProperties.toolTips[3] = $"Switch Whistle Sound: {buttonName}";
            }
            else
            {
                itemProperties.toolTips[3] = "Switch Whistle Sound: [Not Bound]";
            }

        }

        public void OnWhistleStarted(InputAction.CallbackContext callbackContext)
        {
            if (!IsOwner) { return; }
            if (isPocketed || !isHeld) { return; }
            if (StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen) { return; }
            WhistleUseOnLocalClient(true);
            WhistleUseServerRpc(true);
        }
        public void OnWhistleCanceled(InputAction.CallbackContext callbackContext)
        {
            if (!IsOwner) { return; }
            if (isPocketed || !isHeld) { return; }
            if (StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen) { return; }
            WhistleUseOnLocalClient(false);
            WhistleUseServerRpc(false);
        }

        public void OnDismiss(InputAction.CallbackContext callbackContext)
        {
            if (!IsOwner) { return; }
            if (isPocketed || !isHeld) { return; }
            if (StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen) { return; }
            DismissOnLocalClient();
            DismissUseServerRpc();
        }

        public void OnWhistleSoundSwitch(InputAction.CallbackContext callbackContext)
        {
            if (!IsOwner) { return; }
            if (isPocketed || !isHeld) { return; }
            if (StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen) { return; }

            CurrentSoundPackIndex.Value++;
            if (PikUtils.IsOutOfRange(Sounds, CurrentSoundPackIndex.Value))
            {
                CurrentSoundPackIndex.Value = 0;
            }

            LethalMin.CurWhistPack.Entry.Value = CurrentSoundPackIndex.Value;

            noticeZone.LeaderScript.CurrentWhistleIndex = LethalMin.CurWhistPack.Entry.Value;

            LethalMin.Logger.LogInfo($"Set Whistle index to: {LethalMin.CurWhistPack.Entry.Value}");
        }

        public void OnCharge(InputAction.CallbackContext callbackContext)
        {
            if (!IsOwner) { return; }
            if (isPocketed || !isHeld) { return; }
            if (StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen) { return; }
            if (PikminManager.instance.LocalLeader.glowmob != null && PikminManager.instance.LocalLeader.glowmob.IsDoingGlowmob) { return; }
            if (playerHeldBy == null || playerCamera == null) { return; }
            if (LethalMin.InVRMode && SecondaryChargeAction != null)
            {
                if (callbackContext.action == ChargeAction && !SecondaryChargeAction.IsPressed() ||
                   callbackContext.action == SecondaryChargeAction && !ChargeAction.IsPressed())
                {
                    LethalMin.Logger.LogDebug("Awaiting Second Charge Input");
                    return;
                }
            }
            if (ChargeCooldown > 0)
            {
                ChargeUse();
                ChargeUseRpc();
                return;
            }

            // Determine ray origin and direction based on VR mode (similar to UpdateWhistleZonePosition)
            Vector3 rayDirection = LethalMin.InVRMode && !LethalMin.DisableWhistleFix.InternalValue ? transform.forward : playerCamera.transform.forward;
            Vector3 startPosition = LethalMin.InVRMode && !LethalMin.DisableWhistleFix.InternalValue ? transform.position : playerCamera.transform.position;

            float chargeDistance = PikminManager.instance.Cheat_ChargeDistance.Value == -1 ? 15 : PikminManager.instance.Cheat_ChargeDistance.Value;
            Vector3 endPosition = startPosition + rayDirection * chargeDistance;
            Vector3 ChargePos = endPosition;

            // Use similar raycast approach as UpdateWhistleZonePosition (should have done that from the start)
            if (Physics.Raycast(startPosition, rayDirection, out RaycastHit hit, chargeDistance, collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.isTrigger)
                {
                    LethalMin.Logger.LogInfo($"WhistleItem: Charge raycast hit at {hit.point} by {hit.collider.gameObject.name}");
                    ChargePos = hit.point + hit.normal * 0.05f;
                }
            }
            else if (Physics.Raycast(endPosition, Vector3.down, out hit, maxRaycastDistance, collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.isTrigger)
                {
                    LethalMin.Logger.LogInfo($"WhistleItem: Charge downward raycast hit at {hit.point} by {hit.collider.gameObject.name}");
                    ChargePos = hit.point + hit.normal * 0.05f;
                }
            }

            // Sample NavMesh position
            if (NavMesh.SamplePosition(ChargePos, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            {
                ChargePos = navHit.position;
            }
            ChargeUse(ChargePos);
            ChargeUseRpc(ChargePos);
        }

        [Rpc(SendTo.NotOwner)]
        public void ChargeUseRpc(Vector3 ChargePos)
        {
            ChargeUse(ChargePos);
        }
        public void ChargeUse(Vector3 ChargePos)
        {
            ChargeCooldown = PikminManager.instance.Cheat_ChargeCoolDown.Value == -1 ? 0.1f : PikminManager.instance.Cheat_ChargeCoolDown.Value;
            LethalMin.Logger.LogInfo($"WhistleItem: ChargeUse at position {ChargePos} by {playerHeldBy?.playerUsername}");
            OverridePikminPosition pikminPosition = new OverridePikminPosition("WhisCharge", ChargePos, true, 4f, 0.1f);
            audioSource.PlayOneShot(chargeSound);
            if (WhistleAnim != null)
                WhistleAnim.SetTrigger("char");
            if (IsOwner)
            {
                if (!(LethalMin.InVRMode && LethalMin.DisableChargeMotionBlur))
                    PikminManager.instance.StartCoroutine(PikminManager.instance.TweenChargeWeight(1, 0.25f, true, 0.5f));
            }
            foreach (PikminAI ai in noticeZone.LeaderScript.PikminInSquad)
            {
                float vol = ai.leader == null || ai.leader.PikminInSquad.Count == 0 ? 1.0f : 1.0f / ai.leader.PikminInSquad.Count;
                ai.PlayAudioOnLocalClient(PikminSoundPackSounds.Charge.ToString(), true, vol);
                ai.PlayAnimation(ai.animController.AnimPack.EditorNoticeAnim);
                if (!ai.IsOwner)
                {
                    continue;
                }
                
                if (Vector3.Distance(ai.transform.position, noticeZone.LeaderScript.transform.position) > 30f)
                {
                    LethalMin.Logger.LogInfo($"WhistleItem: ChargeUse - {ai.name} is too far away, skipping charge.");
                    continue;
                }
                if (ai.OverrideFollowPosition != null && ai.OverrideFollowPosition.Value.ID == "MoveToVPoint")
                {
                    LethalMin.Logger.LogInfo($"{ai.DebugID}: Charge interrupted by vehicle");
                    continue;
                }
                ai.OverrideFollowPosition = pikminPosition;
                if (ai.chargeRoutine != null)
                {
                    ai.StopCoroutine(ai.chargeRoutine);
                    ai.chargeRoutine = null;
                }
                ai.chargeRoutine = ai.StartCoroutine(ai.DoCharge(4));
            }
        }

        [Rpc(SendTo.NotOwner)]
        public void ChargeUseRpc()
        {
            ChargeUse();
        }
        public void ChargeUse()
        {
            if (WhistleAnim != null)
                WhistleAnim.SetTrigger("diss");
            audioSource.PlayOneShot(chargeFailSound);
        }


        [ServerRpc]
        public void DismissUseServerRpc()
        {
            DismissUseClientRpc();
        }
        [ClientRpc]
        public void DismissUseClientRpc()
        {
            if (!IsOwner)
                DismissOnLocalClient();
        }
        public void DismissOnLocalClient()
        {
            curPack = Sounds[CurrentSoundPackIndex.Value];
            audioSource.PlayOneShot(curPack.DismissSound);
            if (WhistleAnim != null)
                WhistleAnim.SetTrigger("diss");
            if (noticeZone.LeaderScript != null)
            {
                List<PikminAI> PrevAIs = new List<PikminAI>(noticeZone.LeaderScript.PikminInSquad);
                noticeZone.LeaderScript.RemoveAllPikminFromSquadOnLocalClient();

                if (!LethalMin.DisperseWhenDismissed.InternalValue)
                {
                    foreach (PikminAI ai in PrevAIs)
                    {
                        if (ai != null)
                        {
                            ai.LeaderAssesmentDelay = LethalMin.DismissWindownTime;
                        }
                    }
                    return;
                }

                // Group pikmin by type
                Dictionary<PikminType, List<PikminAI>> pikminByType = new Dictionary<PikminType, List<PikminAI>>();
                foreach (PikminAI ai in PrevAIs)
                {
                    if (ai != null && ai.pikminType != null)
                    {
                        if (!pikminByType.ContainsKey(ai.pikminType))
                        {
                            pikminByType[ai.pikminType] = new List<PikminAI>();
                        }
                        pikminByType[ai.pikminType].Add(ai);
                    }
                }

                // Setup for semi-circle formation
                int typeCount = pikminByType.Count;
                if (typeCount > 0)
                {
                    // Calculate overall average position of all pikmin
                    Vector3 overallAveragePosition = Vector3.zero;
                    int totalPikmin = 0;

                    foreach (var kvp in pikminByType)
                    {
                        foreach (PikminAI ai in kvp.Value)
                        {
                            overallAveragePosition += ai.transform.position;
                            totalPikmin++;
                        }
                    }

                    if (totalPikmin > 0)
                    {
                        overallAveragePosition /= totalPikmin;

                        // Parameters for formation
                        float radius = 4f;  // Distance from player
                        float arcAngle = 180f;  // Half-circle in degrees

                        // Player position
                        Vector3 playerPosition = noticeZone.LeaderScript.transform.position;

                        // Calculate the direction from player to average pikmin position
                        Vector3 directionToAverage = (overallAveragePosition - playerPosition).normalized;
                        directionToAverage.y = 0; // Keep formation on ground plane

                        // Create a rotation that looks from the player toward the average pikmin position
                        Quaternion formationRotation;
                        if (directionToAverage != Vector3.zero)
                        {
                            formationRotation = Quaternion.LookRotation(-directionToAverage);
                        }
                        else
                        {
                            // Fallback to player's rotation if direction is zero
                            LethalMin.Logger.LogWarning("Direction to average position is zero, using player's rotation for formation.");
                            formationRotation = noticeZone.LeaderScript.transform.rotation;
                        }

                        // Create visual debug marker for average position
                        //GameObject avgMarker = PikUtils.CreateDebugCube(Color.green);
                        //avgMarker.transform.position = overallAveragePosition;
                        //avgMarker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                        // Calculate idle position for each type in a semi-circle formation
                        Dictionary<PikminType, Vector3> IdlePositions = new Dictionary<PikminType, Vector3>();

                        // Calculate positions in a semi-circle
                        int index = 0;
                        foreach (var kvp in pikminByType)
                        {
                            PikminType type = kvp.Key;

                            // Calculate position in a semi-circle
                            float angleStep = arcAngle / (typeCount - 1 > 0 ? typeCount - 1 : 1);
                            float currentAngle = -arcAngle / 2 + (index * angleStep);

                            // Convert to radians for calculation
                            float angleInRadians = currentAngle * Mathf.Deg2Rad;

                            // Calculate position using polar coordinates (converted to Cartesian)
                            // Note: For a semicircle facing away from the player in the direction of pikmin,
                            // we use -cos for Z (puts the semicircle in front of formation direction)
                            Vector3 offset = new Vector3(
                                Mathf.Sin(angleInRadians) * radius,
                                0f,  // Keep at same height as player
                                -Mathf.Cos(angleInRadians) * radius  // Negative to face away from player
                            );

                            // Transform the offset based on calculated formation rotation
                            Vector3 rotatedOffset = formationRotation * offset;

                            // Calculate final position
                            Vector3 idlePos = overallAveragePosition + rotatedOffset;

                            // Ensure position is on NavMesh
                            if (NavMesh.SamplePosition(idlePos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                            {
                                if (Physics.Linecast(overallAveragePosition + Vector3.up, hit.position + Vector3.up, out RaycastHit hitInfo, LethalMin.PikminColideable))
                                {
                                    idlePos = hitInfo.point;
                                }
                                else
                                {
                                    idlePos = hit.position;
                                }
                            }

                            // Create visual debug marker
                            //PikUtils.CreateDebugCube(type.PikminPrimaryColor).transform.position = idlePos;

                            // Store the position
                            IdlePositions[type] = idlePos;

                            index++;
                        }

                        // Apply calculated idle positions to each pikmin
                        foreach (PikminAI ai in PrevAIs)
                        {
                            if (ai != null && ai.pikminType != null && IdlePositions.ContainsKey(ai.pikminType))
                            {
                                ai.LeaderAssesmentDelay = LethalMin.DismissWindownTime;
                                ai.OverrideIdlePosition = new OverridePikminPosition("WhisDismiss", IdlePositions[ai.pikminType], true, 2f, 2f);
                            }
                        }
                    }
                }
            }
        }

        [ServerRpc]
        public void WhistleUseServerRpc(bool IsButtonDown)
        {
            WhistleUseClientRpc(IsButtonDown);
        }
        [ClientRpc]
        public void WhistleUseClientRpc(bool IsButtonDown)
        {
            if (!IsOwner)
                WhistleUseOnLocalClient(IsButtonDown);
        }
        public void WhistleUseOnLocalClient(bool IsButtonDown)
        {
            //LethalMin.Logger.LogInfo($"items.WhistleItem: ItemActivate {buttonDown}");
            isWhistling = IsButtonDown;
            if (IsButtonDown)
            {
                if (whistleZoneTween != null)
                {
                    StopCoroutine(whistleZoneTween);
                    whistleZoneTween = null;
                }
                if (whistleAudioRutine != null)
                {
                    StopCoroutine(whistleAudioRutine);
                    whistleAudioRutine = null;
                }
                whistleAudioRutine = StartCoroutine(StartWhistAudio());
                whistleZoneTween = StartCoroutine(TweenSize());
            }
            else
            {
                audioSource.Stop();
            }
        }

        IEnumerator StartWhistAudio()
        {
            curPack = Sounds[CurrentSoundPackIndex.Value];
            if (!isWhistling)
            {
                audioSource.Stop();
                whistleAudioRutine = null;
                yield break;
            }
            RoundManager.Instance.PlayAudibleNoise
            (LethalMin.WhistleMakesNoiseAtNoticeZoneCheat ? noticeZone.transform.position : transform.position,
            maxWhistleZoneRadius, 0.5f, 0, false, WhistleAudioID);
            audioSource.PlayOneShot(curPack.StartSound);
            yield return new WaitForSeconds(curPack.StartSound.length);
            if (!isWhistling)
            {
                audioSource.Stop();
                whistleAudioRutine = null;
                yield break;
            }
            audioSource.clip = curPack.LoopSound;
            audioSource.Play();
            whistleAudioRutine = null;
        }

        IEnumerator TweenSize()
        {
            //LethalMin.Logger.LogInfo($"items.WhistleItem: TweenSize {isWhistling}");
            float startingSize = minWhistleZoneRadius;
            float targetSize = maxWhistleZoneRadius;
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
            whistleZoneTween = null;
        }

        public override void GrabItem()
        {
            base.GrabItem();
            if (playerHeldBy != null)
            {
                noticeZone.LeaderScript = playerHeldBy.GetComponent<Leader>();
                playerCamera = playerHeldBy.gameplayCamera;
            }
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            playerCamera = null;
        }

        private void UpdateWhistleZonePosition()
        {
            if (playerHeldBy == null || noticeZone == null || playerCamera == null) return;

            Vector3 rayDirection = LethalMin.InVRMode && !LethalMin.DisableWhistleFix.InternalValue ? transform.forward : playerCamera.transform.forward;
            Vector3 startPosition = LethalMin.InVRMode && !LethalMin.DisableWhistleFix.InternalValue ? transform.position : playerCamera.transform.position;
            Vector3 endPosition = startPosition + rayDirection * whistleZoneOffset;

            if (Physics.Raycast(startPosition, rayDirection, out RaycastHit hit, whistleZoneOffset, collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.isTrigger)
                    noticeZone.transform.position = hit.point + hit.normal * 0.05f;
            }
            else if (Physics.Raycast(endPosition, Vector3.down, out hit, maxRaycastDistance, collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.isTrigger)
                    noticeZone.transform.position = hit.point + hit.normal * 0.05f;
            }
            else
            {
                noticeZone.transform.position = endPosition;
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            noticeZone.LeaderScript = playerHeldBy.GetComponentInChildren<Leader>();

            if (IsOwner)
                CurrentSoundPackIndex.Value = noticeZone.LeaderScript.CurrentWhistleIndex;

            curPack = Sounds[CurrentSoundPackIndex.Value];
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);
            MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                if (componentsInChildren[i].gameObject.activeSelf)
                {
                    continue; // Don't change active state of active objects
                }
                if (!componentsInChildren[i].gameObject.CompareTag("DoNotSet") && !componentsInChildren[i].gameObject.CompareTag("InteractTrigger"))
                {
                    componentsInChildren[i].enabled = enable;
                }
            }
            SkinnedMeshRenderer[] componentsInChildren2 = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int j = 0; j < componentsInChildren2.Length; j++)
            {
                if (componentsInChildren2[j].gameObject.activeSelf)
                {
                    continue; // Don't change active state of active objects
                }
                componentsInChildren2[j].enabled = enable;
            }
        }
    }
}
