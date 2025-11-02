using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Pikmin;
using LethalMin.Utils;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LethalMin
{
    public class Glowmob : NetworkBehaviour
    {
        public PikminType GlowPikminType = null!;
        public GameObject Visualizer = null!;
        public InputAction glowMobAction = null!;
        public Leader leaderScript = null!;
        public List<PikminAI> PikminInGlowmob = new List<PikminAI>();
        public AudioClip HitBig = null!, HitMid = null!, HitSml = null!;
        public AudioClip ThrowBig = null!, ThrowMid = null!, ThrowSml = null!;
        public AudioClip ReadyBig = null!, ReadyMid = null!, ReadySml = null!;
        public AudioClip ChargeBig = null!, ChargeMid = null!, ChargeSml = null!;
        public AudioClip HoldBig = null!, HoldMid = null!, HoldSml = null!;
        public AudioSource GlowmobLoop = null!;
        public AudioSource SFXSource = null!;
        public Animator BurstFX = null!;
        public Light VisLight = null!;
        public bool IsDoingGlowmob = false;
        public bool IsThrown = false;
        public float GlowMobProgress = 0;
        public const float MaxGlowMobProgress = 1;
        public float Cooldown = 0;
        public float MaxCooldown = 5f;
        public int ExcpectedStrength = 1;
        public int ActualStrength = 1;
        public bool IsOwnedAndConnected => IsOwner && leaderScript == PikminManager.instance.LocalLeader;
        public bool IsOnCooldown => Cooldown > 0;
        public bool IsReady => GlowMobProgress >= MaxGlowMobProgress;
        Coroutine SetToRouteine = null!;
        public Transform holdPosition = null!;
        Vector3 TargetPos => holdPosition.position + (holdPosition.forward * forwardOffset);
        bool HasGlowInSquad => leaderScript.PikminInSquad.Any(pikmin => pikmin is GlowPikminAI);
        float forwardOffset => 4 + transform.localScale.magnitude;
        int GetNumberOfGlowsInSquad => leaderScript.PikminInSquad.Count(pikmin => pikmin is GlowPikminAI);
        float flyingTimer = 0;
        bool IsGMAheld => glowMobAction.IsPressed();
        PikminEnemy? enemyHomingTarget = null;
        Rigidbody rb = null!;
        SphereCollider sphereCollider = null!;
        Coroutine? HeldRoutine = null;
        Coroutine? HomeingRoutine = null;


        [ServerRpc]
        public void SetLeaderServerRpc(NetworkObjectReference reff)
        {
            SetLeaderClientRpc(reff);
        }
        [ClientRpc]
        public void SetLeaderClientRpc(NetworkObjectReference reff)
        {
            if (reff.TryGet(out NetworkObject objt) && objt.gameObject.TryGetComponent(out Leader leader))
            {
                SetLeader(leader);
            }
            else
            {
                LethalMin.Logger.LogError($"Glowmob: {OwnerClientId} tried to set a null leader");
                return;
            }
        }
        public void SetLeader(Leader leader)
        {
            leader.glowmob = this;
            leaderScript = leader;
            holdPosition = leaderScript.holdPosition;
            gameObject.name = $"{leaderScript.Controller.playerUsername}'s Glowmob";
            //LethalMin.Logger.LogInfo($"{gameObject.name}: is now owned by {leaderScript.Controller.playerUsername}");
        }

        public void Start()
        {
            rb = GetComponent<Rigidbody>();
            sphereCollider = GetComponent<SphereCollider>();
            SwitchCollisionMode();
            LethalMin.Logger.LogInfo($"Binding input actions for GlowMobObject");
            if (!LethalMin.UseInputUtils)
            {
                string GlowMobActionPath = LethalMin.InVRMode ? LethalMin.GlowmobVRAction.InternalValue : LethalMin.GlowmobAction.InternalValue;
                glowMobAction = new InputAction("Glowmob");
                glowMobAction.AddBinding(GlowMobActionPath);
                glowMobAction.started -= GlowmobStartedCheck;
                glowMobAction.canceled -= OnGlowmobCanceled;
                glowMobAction.started += GlowmobStartedCheck;
                glowMobAction.canceled += OnGlowmobCanceled;
                glowMobAction.Enable();
            }
            else
            {
                glowMobAction = LethalMin.InputClassInstace.Glowmob;
                glowMobAction.started -= GlowmobStartedCheck;
                glowMobAction.canceled -= OnGlowmobCanceled;
                glowMobAction.started += GlowmobStartedCheck;
                glowMobAction.canceled += OnGlowmobCanceled;
                glowMobAction.Enable();
            }
        }

        public void Update()
        {
            Visualizer.SetActive(IsDoingGlowmob || IsThrown);
            VisLight.intensity = PikminInGlowmob.Count;
            VisLight.range = PikminInGlowmob.Count * 0.5f;

            if (IsDoingGlowmob && !IsThrown)
            {
                int GlowNumb = GetNumberOfGlowsInSquad;
                if (IsOwnedAndConnected && leaderScript.Controller.isPlayerDead || GlowNumb == 0)
                {
                    StopGlowmobServerRpc();
                    StopGlowmob();
                    return;
                }
                transform.position = Vector3.Lerp(transform.position, TargetPos, Time.deltaTime * 5f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(holdPosition.forward), Time.deltaTime * 25f);
                if (leaderScript.PikminInSquad.Count > 0)
                {
                    // Calculate scale based on PikminInGlowmob count
                    float baseScale = 0f;
                    float additionalScale = PikminInGlowmob.Count * 0.05f; // 5% increase per pikmin

                    float targetScale = baseScale + additionalScale;
                    transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * 7f);
                }
                else
                {
                    transform.localScale = Vector3.one;
                }

                GlowMobProgress = (float)PikminInGlowmob.Count / GlowNumb;
            }
            else
            {
                GlowMobProgress = 0;
            }

            if (Cooldown > 0)
            {
                Cooldown -= Time.deltaTime;
            }

            if (IsThrown && IsOwner)
            {
                if (flyingTimer > 0f)
                {
                    flyingTimer -= Time.deltaTime;
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Glowmob: {OwnerClientId} is out of time to burst, bursting now");
                    BurstServerRpc();
                    Burst();
                }
            }
            else
            {
                flyingTimer = 10f;
            }
        }


        public void GlowmobStartedCheck(InputAction.CallbackContext context)
        {
            if (leaderScript.Controller.currentlyHeldObjectServer is WhistleItem || LethalMin.InVRMode && LethalMin.GlowmobDelay)
            {
                if (HeldRoutine != null)
                {
                    StopCoroutine(HeldRoutine);
                    HeldRoutine = null;
                }
                HeldRoutine = StartCoroutine(GlowmobHold(context));
            }
            else
            {
                //buffer for charge
                OnGlowmobStarted(context);
            }
        }
        IEnumerator GlowmobHold(InputAction.CallbackContext context)
        {
            yield return new WaitForSeconds(0.1f);
            if (!IsGMAheld)
            {
                yield break;
            }
            OnGlowmobStarted(context);
        }

        public void OnGlowmobStarted(InputAction.CallbackContext context)
        {
            if (!IsOwnedAndConnected)
            {
                return;
            }
            if (!HasGlowInSquad)
            {
                return;
            }
            if (IsOnCooldown)
            {
                return;
            }
            if (IsThrown)
            {
                return;
            }
            if (IsDoingGlowmob)
            {
                return;
            }
            if (leaderScript.GetSelectedType() != GlowPikminType)
            {
                return;
            }

            StartGlowmobServerRpc();
            StartGlowmob();
            HeldRoutine = null;
        }
        public void OnGlowmobCanceled(InputAction.CallbackContext context)
        {
            if (IsOwnedAndConnected && HeldRoutine != null)
            {
                StopCoroutine(HeldRoutine);
                HeldRoutine = null;
            }

            if (!IsOwnedAndConnected || !IsDoingGlowmob)
            {
                return;
            }

            if (IsReady)
            {
                PikminEnemy? enemy = FindEnemyToHomeon();
                ThrowGlowmobServerRpc(enemy != null ? enemy.NetworkObject : default(NetworkObjectReference));
                ThrowGlowmob(enemy);
            }
            else
            {
                StopGlowmobServerRpc();
                StopGlowmob();
            }
        }

        [ServerRpc]
        public void StartGlowmobServerRpc()
        {
            StartGlowmobClientRpc();
        }
        [ClientRpc]
        public void StartGlowmobClientRpc()
        {
            if (!IsOwner)
                StartGlowmob();
        }
        public void StartGlowmob()
        {
            if (IsDoingGlowmob)
            {
                LethalMin.Logger.LogWarning($"Glowmob: {OwnerClientId} is already doing glowmob");
                return;
            }
            if (SetToRouteine != null)
            {
                StopCoroutine(SetToRouteine);
            }

            LethalMin.Logger.LogInfo($"Glowmob: {OwnerClientId} is doing glowmob");

            GlowMobProgress = 0;
            IsDoingGlowmob = true;
            transform.position = TargetPos;
            transform.rotation = Quaternion.LookRotation(holdPosition.forward);
            transform.localScale = Vector3.zero;
            int Number = GetNumberOfGlowsInSquad;
            ExcpectedStrength = Number;
            if (Number < 25)
            {
                SFXSource.PlayOneShot(ChargeSml);
            }
            else if (Number < 50)
            {
                SFXSource.PlayOneShot(ChargeMid);
            }
            else
            {
                SFXSource.PlayOneShot(ChargeBig);
            }
            SetToRouteine = StartCoroutine(SetPikminInSquadToMob());
        }
        IEnumerator SetPikminInSquadToMob()
        {
            int numb = GetNumberOfGlowsInSquad;
            foreach (PikminAI pikmin in leaderScript.PikminInSquad)
            {
                if (pikmin is GlowPikminAI biolumin)
                {
                    biolumin.SetToGlowMod(this);
                    yield return new WaitForSeconds(numb * 0.00005f);
                }
            }
        }

        [ServerRpc]
        public void OnReadyServerRpc()
        {
            OnReadyClientRpc();
        }
        [ClientRpc]
        public void OnReadyClientRpc()
        {
            if (!IsOwner)
                OnReady();
        }
        public void OnReady()
        {
            LethalMin.Logger.LogInfo($"Glowmob: {OwnerClientId} is ready to throw glowmob");
            int Number = PikminInGlowmob.Count;
            if (Number < 25)
            {
                GlowmobLoop.clip = HoldSml;
                SFXSource.PlayOneShot(ReadySml);
            }
            else if (Number < 50)
            {
                GlowmobLoop.clip = HoldMid;
                SFXSource.PlayOneShot(ReadyMid);
            }
            else
            {
                GlowmobLoop.clip = HoldBig;
                SFXSource.PlayOneShot(ReadyBig);
            }
            GlowmobLoop.Play();
        }


        [ServerRpc]
        public void StopGlowmobServerRpc(bool DoingThrow = false)
        {
            StopGlowmobClientRpc(DoingThrow);
        }
        [ClientRpc]
        public void StopGlowmobClientRpc(bool DoThrow = false)
        {
            if (!IsOwner)
                StopGlowmob(DoThrow);
        }
        public void StopGlowmob(bool DoingThrow = false)
        {
            if (!IsDoingGlowmob)
            {
                LethalMin.Logger.LogWarning($"Glowmob: {OwnerClientId} is not doing glowmob");
                return;
            }

            LethalMin.Logger.LogInfo($"Glowmob: {OwnerClientId} has stopped doing glowmob");


            if (SetToRouteine != null)
            {
                StopCoroutine(SetToRouteine);
            }

            GlowmobLoop.Stop();

            GlowMobProgress = 0;
            IsDoingGlowmob = false;

            if (DoingThrow)
            {
                return;
            }

            SFXSource.Stop();

            List<PikminAI> ais = new List<PikminAI>(PikminInGlowmob);
            ais.AddRange(leaderScript.PikminInSquad.Where(pikmin => !PikminInGlowmob.Contains(pikmin)));
            foreach (PikminAI pikmin in ais)
            {
                if (pikmin is GlowPikminAI biolumin)
                {
                    biolumin.CancleGlowmob(false, true);
                }
            }
        }


        [ServerRpc]
        public void ThrowGlowmobServerRpc(NetworkObjectReference reff)
        {
            ThrowGlowmobClientRpc(reff);
        }
        [ClientRpc]
        public void ThrowGlowmobClientRpc(NetworkObjectReference reff)
        {
            if (IsOwner)
            {
                return;
            }
            if (reff.Equals(default(NetworkObjectReference)))
            {
                ThrowGlowmob(null);
                return;
            }
            if (reff.TryGet(out NetworkObject objt) && objt.gameObject.TryGetComponent(out PikminEnemy enemy))
            {
                ThrowGlowmob(enemy);
            }
        }
        public void ThrowGlowmob(PikminEnemy? enemy)
        {
            LethalMin.Logger.LogInfo($"Glowmob: {OwnerClientId} is throwing glowmob");
            StopGlowmob(true);
            IsThrown = true;
            MaxCooldown = (float)PikminInGlowmob.Count * 0.1f;
            Cooldown = MaxCooldown;
            int Number = PikminInGlowmob.Count;
            ActualStrength = Number;
            if (Number < 25)
            {
                SFXSource.PlayOneShot(ThrowSml);
            }
            else if (Number < 50)
            {
                SFXSource.PlayOneShot(ThrowMid);
            }
            else
            {
                SFXSource.PlayOneShot(ThrowBig);
            }
            if (enemy == null)
            {
                LethalMin.Logger.LogInfo($"Glowmob: {gameObject.name} is thrown without homing");
                SwitchCollisionMode(true);
                rb.AddForce(holdPosition.forward * 10f, ForceMode.Impulse);
            }
            else
            {
                LethalMin.Logger.LogMessage($"Glowmob: {gameObject.name} is homing on {enemy.gameObject.name}");
                SwitchCollisionMode(false);
                enemyHomingTarget = enemy;
                HomeingRoutine = StartCoroutine(HomingAnim());
            }
        }

        IEnumerator HomingAnim()
        {
            if (enemyHomingTarget == null)
            {
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 EHTpos = enemyHomingTarget.transform.position;
            Vector3 targetPos = new Vector3(EHTpos.x, EHTpos.y + (enemyHomingTarget.enemyScript.agent.height / 2), EHTpos.z);
            float duration = Mathf.Min(Vector3.Distance(startPos, targetPos) * 0.1f, 3f); // Adjust speed as needed
            float elapsedTime = 0f;
            WaitForEndOfFrame wait = new WaitForEndOfFrame();
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float oscillation = Mathf.Sin(elapsedTime * 5f) * 2f; // Controls the height and speed of oscillation
                Vector3 oscillatedPos = Vector3.Lerp(startPos, targetPos, t);
                oscillatedPos.y += oscillation; // Add vertical oscillation

                transform.position = oscillatedPos;
                elapsedTime += Time.deltaTime;
                yield return wait;
            }

            enemyHomingTarget = null;

            if (!IsOwner || !IsThrown)
            {
                HomeingRoutine = null;
                yield break;
            }

            BurstServerRpc();
            Burst();
            HomeingRoutine = null;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!IsOwner || !IsThrown)
            {
                return;
            }

            PikminEnemy eny = other.gameObject.GetComponentInParent<PikminEnemy>();
            if (eny == null)
                eny = other.gameObject.GetComponentInChildren<PikminEnemy>();
            if (eny != null && eny == enemyHomingTarget)
            {
                LethalMin.Logger.LogInfo($"Glowmob: {gameObject.name} has hit {eny.gameObject.name}");

                if (HomeingRoutine != null)
                {
                    StopCoroutine(HomeingRoutine);
                }
                HomeingRoutine = null;

                enemyHomingTarget = null;
                BurstServerRpc();
                Burst();
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (!IsOwner || !IsThrown)
            {
                return;
            }

            BurstServerRpc();
            Burst();
        }

        [ServerRpc]
        public void BurstServerRpc()
        {
            BurstClientRpc();
        }
        [ClientRpc]
        public void BurstClientRpc()
        {
            if (!IsOwner)
                Burst();
        }
        public void Burst()
        {
            IsThrown = false;
            int Number = PikminInGlowmob.Count;
            PikUtils.StunNearbyEnemies(transform.position, Number / 2, Number * 0.5f);
            SwitchCollisionMode(false);

            if (Number < 25)
            {
                SFXSource.PlayOneShot(HitSml);
            }
            else if (Number < 50)
            {
                SFXSource.PlayOneShot(HitMid);
            }
            else
            {
                SFXSource.PlayOneShot(HitBig);
            }

            BurstFX.Play("BurstAnim");

            List<PikminAI> ais = new List<PikminAI>(PikminInGlowmob);
            foreach (PikminAI pikmin in ais)
            {
                if (pikmin is GlowPikminAI biolumin)
                {
                    biolumin.BurstFromGlowMob();
                }
            }
        }

        public PikminEnemy? FindEnemyToHomeon()
        {
            PikminEnemy? closestEnemy = null;
            float closestDistance = float.MaxValue;
            float maxDistance = 50f; // Maximum distance to consider
            float maxAngle = 90f;    // Half of the 180-degree plane

            foreach (PikminEnemy enemy in FindObjectsOfType<PikminEnemy>())
            {
                if (enemy == null || enemy.enemyScript.isEnemyDead)
                    continue;
                if (LethalMin.AttackBlacklistConfig.InternalValue.Contains(enemy.enemyScript.enemyType.enemyName))
                    continue;

                Vector3 directionToEnemy = enemy.transform.position - transform.position;
                float distance = directionToEnemy.magnitude;

                // Check if within range
                if (distance > maxDistance)
                    continue;

                // Check if within plane (180-degree total, so 90 degrees on each side)
                float angle = Vector3.Angle(transform.forward, directionToEnemy);
                if (angle <= maxAngle && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }

            return closestEnemy;
        }

        public void SwitchCollisionMode(bool useRB = false)
        {
            if (useRB)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                sphereCollider.isTrigger = false;
            }
            else
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                sphereCollider.isTrigger = true;
            }
        }
    }
}