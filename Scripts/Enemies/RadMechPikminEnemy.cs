using System.Collections;

using System.Collections.Generic;
using System.Linq;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class RadMechPikminEnemy : PikminEnemy
    {
        public List<PikminAI> PikminGrabbed = new List<PikminAI>();
        RadMechAI radMechAI = null!;
        RadMechLatchTrigger handTrigger = null!;
        public int GrabLimmit = 10;
        public bool waitingToAttemptPikminGrab = false;
        public bool attemptingPikminGrab = false;
        public float attemptPikminGrabTimer = 0.4f;
        float grabCheckInterval = 0.1f;
        Coroutine? torchPikminCoroutine = null;

        protected override void Start()
        {
            base.Start();
            radMechAI = enemyScript as RadMechAI ?? throw new System.Exception("RadMechPE: enemyScript is not a RadMechAI");
            if (radMechAI == null)
            {
                enabled = false;
                return;
            }


            PikminLatchTrigger plt = LatchTriggers[0];
            RadMechLatchTrigger rmlt = plt.gameObject.AddComponent<RadMechLatchTrigger>();
            rmlt.StateCondisions.AddRange(System.Enum.GetValues(typeof(Pintent)).Cast<Pintent>());
            rmlt.StateCondisions.Remove(Pintent.Stuck);
            rmlt.StateToSet = LatchTriggerStateToSet.Stuck;
            rmlt.WhistleTime = -255f;
            rmlt.radMechPikminEnemy = this;
            rmlt.AllowBaseLatchOn = false;
            rmlt.OverrideLatchObject = radMechAI.holdPlayerPoint;
            handTrigger = rmlt;
            LatchTriggers.Remove(plt);
            LatchTriggers.Add(rmlt);
            Destroy(plt);
        }

        public void LateUpdate()
        {
            if (radMechAI.isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }
            if (!radMechAI.ventAnimationFinished)
            {
                return;
            }
            //Server Only
            if (!radMechAI.IsServer)
            {
                return;
            }
            if (radMechAI.currentBehaviourStateIndex == 2 && radMechAI.finishingFlight && (!radMechAI.inSky || radMechAI.flyTimer > 10f))
            {
                return;
            }
            if (LethalMin.UseConfigsForEnemies && !LethalMin.RadMech_GrabsPikmin)
            {
                return;
            }
            if (LethalMin.UseConfigsForEnemies)
            {
                GrabLimmit = LethalMin.RadMech_GrabLimmit.InternalValue;
            }

            if (attemptingPikminGrab)
            {
                if (grabCheckInterval > 0f)
                {
                    grabCheckInterval -= Time.deltaTime;
                }
                else
                {
                    grabCheckInterval = 0.1f + Random.Range(-0.015f, 0.015f);
                    GrabClosePikmin();
                }
            }
            if (!radMechAI.inSpecialAnimation)
            {
                AttemptPikminGrabIfClose();
            }
        }

        public void AttemptPikminGrabIfClose()
        {
            // Early exit conditions
            if (!radMechAI.IsServer ||
                radMechAI.inSpecialAnimation ||
                radMechAI.currentBehaviourStateIndex == 2 ||
                radMechAI.waitingToAttemptGrab ||
                radMechAI.inTorchPlayerAnimation ||
                (radMechAI.attemptingGrab && !attemptingPikminGrab))
            {
                waitingToAttemptPikminGrab = false;
                return;
            }

            // State machine for grabbing pikmin
            if (attemptingPikminGrab)
            {
                // Currently in the middle of a grab attempt waiting if any pikmin were grabbed
                attemptPikminGrabTimer -= Time.deltaTime;
                LethalMin.Logger.LogInfo($"RadMechPE: Attempting Pikmin Grab Timer: {attemptPikminGrabTimer}");

                if (PikminGrabbed.Count > 0 && attemptPikminGrabTimer < 0.75f)
                {
                    BeginTorchPikminServerRpc();
                }
                if (attemptPikminGrabTimer < 0f)
                {
                    FinishAttemptPikminGrab();
                }
            }
            else if (waitingToAttemptPikminGrab)
            {
                // Ready to grab but waiting for the right moment
                if (!radMechAI.takingStep)
                {
                    waitingToAttemptPikminGrab = false;
                    StartPikminGrabAttempt();
                }
            }
            else if (attemptPikminGrabTimer < 0f)
            {
                // Look for pikmin to grab
                foreach (PikminAI ai in PikminManager.instance.PikminAIs)
                {
                    if (Vector3.Distance(transform.position, ai.transform.position) >= 5.2)
                    {
                        continue;
                    }
                    if (ai.IsDeadOrDying || ai.IsAirborn || ai.CurrentLatchTrigger != null)
                    {
                        continue;
                    }
                    if (PikChecks.IsPikminResistantToHazard(ai, PikminHazard.Fire, false))
                    {
                        continue;
                    }

                    waitingToAttemptPikminGrab = true;
                    radMechAI.disableWalking = true;
                    break;
                }

                // Reset timer if no pikmin found
                attemptPikminGrabTimer = 0.4f;
            }
            else
            {
                // Cooldown before next check for pikmin
                attemptPikminGrabTimer -= Time.deltaTime;
            }
        }

        public void GrabClosePikmin()
        {
            foreach (PikminAI ai in PikminManager.instance.PikminAIs)
            {
                if (PikminGrabbed.Count >= GrabLimmit)
                {
                    break;
                }
                if (Vector3.Distance(transform.position, ai.transform.position) >= 7f)
                {
                    continue;
                }
                if (PikminGrabbed.Contains(ai))
                {
                    continue;
                }
                if (ai.IsDeadOrDying || ai.IsAirborn || ai.CurrentLatchTrigger != null || !handTrigger.TryLatch(ai, handTrigger.transform.position, true, true))
                {
                    continue;
                }
                GrabPikminServerRpc(ai.NetworkObject);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void GrabPikminServerRpc(NetworkObjectReference PikRef)
        {
            GrabPikminClientRpc(PikRef);
        }

        [ClientRpc]
        public void GrabPikminClientRpc(NetworkObjectReference PikRef)
        {
            if (PikRef.TryGet(out NetworkObject? netObj) && netObj.TryGetComponent(out PikminAI pikminAI))
            {
                GrabPikmin(pikminAI);
            }
            else
            {
                LethalMin.Logger.LogError("RadMechPE: GrabPikminClientRpc: PikminAI not found or invalid reference.");
            }
        }

        public void GrabPikmin(PikminAI pikminAI)
        {
            if (PikminGrabbed.Contains(pikminAI))
            {
                return;
            }
            LethalMin.Logger.LogInfo($"RadMechPE: GrabPikmin: Pikmin {pikminAI.gameObject.name} grabbed by Rad Mech.");
            PikminGrabbed.Add(pikminAI);
            handTrigger.LatchPikmin(pikminAI, handTrigger.transform.position, true);
        }

        [ServerRpc]
        private void BeginTorchPikminServerRpc()
        {
            radMechAI.inTorchPlayerAnimation = true;
            radMechAI.inSpecialAnimation = true;
            radMechAI.agent.enabled = false;
            radMechAI.attemptingGrab = false;
            attemptingPikminGrab = false;
            int enemyYRot = (int)base.transform.eulerAngles.y;
            if (Physics.Raycast(radMechAI.centerPosition.position, radMechAI.centerPosition.forward, out var _, 6f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                enemyYRot = (int)RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(radMechAI.centerPosition.position, 20f, 5);
            }
            BeginTorchPikminClientRpc();//(transform.position, enemyYRot);
        }

        [ClientRpc]
        public void BeginTorchPikminClientRpc()//(Vector3 enemyPosition, int enemyYRot)
        {
            BeginTorchPikmin();//(enemyPosition, enemyYRot);
        }

        private void BeginTorchPikmin()//(Vector3 enemyPosition, int enemyYRot)
        {
            foreach (PikminAI pik in PikminGrabbed)
            {

            }
            LethalMin.Logger.LogInfo("RadMechPE: BeginTorchPikminClientRpc: Starting torch animation.");
            if (torchPikminCoroutine != null)
            {
                StopCoroutine(torchPikminCoroutine);
                torchPikminCoroutine = null;
            }
            torchPikminCoroutine = StartCoroutine(TorchPikminAnimation());//(enemyPosition, enemyYRot));
        }

        private IEnumerator TorchPikminAnimation()//(Vector3 enemyPosition, int enemyYRot)
        {
            radMechAI.creatureAnimator.SetBool("AttemptingGrab", value: true);
            radMechAI.creatureAnimator.SetBool("GrabSuccessful", value: true);
            radMechAI.creatureAnimator.SetBool("GrabUnsuccessful", value: false);
            float startTime = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => radMechAI.blowtorchActivated || Time.realtimeSinceStartup - startTime > 6f);
            startTime = Time.realtimeSinceStartup;
            List<PikminAI> pikminToTorch = new List<PikminAI>(PikminGrabbed);
            int DeadCount = 0;
            float TourchDelay = 0.25f / (float)pikminToTorch.Count;
            foreach (PikminAI pikmin in pikminToTorch)
            {
                yield return new WaitForSeconds(TourchDelay);
                if (pikmin == null || pikmin.IsDeadOrDying)
                {
                    continue;
                }
                if (!PikChecks.IsPikminResistantToHazard(pikmin, PikminHazard.Fire))
                {
                    pikmin.KillEnemy(true);
                }
                DeadCount++;
                LethalMin.Logger.LogDebug($"RadMechPE: TorchPikminAnimation: Pikmin {pikmin.gameObject.name} killed by torch. {DeadCount}-{pikminToTorch.Count} remaining.");
            }
            LethalMin.Logger.LogInfo($"RadMechPE: TorchPikminAnimation: Torch animation finished. {DeadCount} Pikmin killed.");
            yield return new WaitForSeconds(1.5f);
            CancelTorchPikminAnimation();
            if (base.IsServer)
            {
                radMechAI.inTorchPlayerAnimation = false;
                radMechAI.inSpecialAnimation = false;
                radMechAI.agent.enabled = true;
            }
            torchPikminCoroutine = null;
        }

        public void CancelTorchPikminAnimation()
        {
            LethalMin.Logger.LogInfo("RadMechPE: CancelTorchPikminAnimation: Cancelling torch animation.");
            radMechAI.inTorchPlayerAnimation = false;
            radMechAI.inSpecialAnimation = false;
            radMechAI.disableWalking = false;
            radMechAI.attemptGrabTimer = 5f;
            attemptPikminGrabTimer = 5f;

            if (base.IsServer)
            {
                radMechAI.agent.enabled = true;
            }
            radMechAI.creatureAnimator.SetBool("GrabSuccessful", value: false);
            radMechAI.creatureAnimator.SetBool("AttemptingGrab", value: false);
            radMechAI.creatureAnimator.SetBool("GrabUnsuccessful", value: false);
            foreach (PikminAI pik in PikminGrabbed)
            {
                if (pik == null)
                {
                    continue;
                }
                pik.ApplyPhysics(true);
                pik.LandBuffer = 0.5f;
            }
            PikminGrabbed.Clear();
            if (radMechAI.blowtorchActivated)
            {
                radMechAI.DisableBlowtorch();
            }
            if (torchPikminCoroutine != null)
            {
                StopCoroutine(torchPikminCoroutine);
            }
        }


        public void StartPikminGrabAttempt()
        {
            LethalMin.Logger.LogInfo("RadMechPE: StartPikminGrabAttemptClientRpc:(Server) Starting Pikmin grab attempt.");
            attemptingPikminGrab = true;
            radMechAI.attemptingGrab = true;
            radMechAI.attemptGrabTimer = 1.5f;
            attemptPikminGrabTimer = 1.5f;
            radMechAI.creatureAnimator.SetBool("AttemptingGrab", value: true);
            radMechAI.creatureAnimator.SetBool("GrabUnsuccessful", value: false);
            radMechAI.creatureAnimator.SetBool("GrabSuccessful", value: false);
            radMechAI.disableWalking = true;
            StartPikminGrabAttemptClientRpc();
        }
        [ClientRpc]
        public void StartPikminGrabAttemptClientRpc()
        {
            LethalMin.Logger.LogInfo("RadMechPE: StartPikminGrabAttemptClientRpc: Starting Pikmin grab attempt.");
            attemptingPikminGrab = true;
            radMechAI.attemptingGrab = true;
            radMechAI.creatureAnimator.SetBool("AttemptingGrab", value: true);
            radMechAI.creatureAnimator.SetBool("GrabUnsuccessful", value: false);
            radMechAI.creatureAnimator.SetBool("GrabSuccessful", value: false);
            radMechAI.disableWalking = true;
        }


        public void FinishAttemptPikminGrab()
        {
            LethalMin.Logger.LogInfo("RadMechPE: StartPikminGrabAttemptClientRpc:(Server) Finished Pikmin grab attempt.");
            attemptingPikminGrab = false;
            radMechAI.attemptingGrab = false;
            radMechAI.disableWalking = false;
            radMechAI.attemptGrabTimer = 5f;
            attemptPikminGrabTimer = 5f;
            radMechAI.creatureAnimator.SetBool("GrabUnsuccessful", value: true);
            radMechAI.creatureAnimator.SetBool("AttemptingGrab", value: false);
            FinishAttemptingPikminGrabClientRpc();
        }

        [ClientRpc]
        public void FinishAttemptingPikminGrabClientRpc()
        {
            LethalMin.Logger.LogInfo("RadMechPE: StartPikminGrabAttemptClientRpc: Finished Pikmin grab attempt.");
            attemptingPikminGrab = false;
            radMechAI.attemptingGrab = false;
            radMechAI.disableWalking = false;
            radMechAI.creatureAnimator.SetBool("GrabUnsuccessful", value: true);
            radMechAI.creatureAnimator.SetBool("AttemptingGrab", value: false);
        }
    }
}
