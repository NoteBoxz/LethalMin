using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;
using LethalMin.Pikmin;
using LethalMin.Utils;
using UnityEngine.AI;
using GameNetcodeStuff;

namespace LethalMin
{
    public class PurplePikminAI : PikminAI
    {
        public AudioClip LandSound = null!;
        public AudioClip SlamSound = null!; // Sound to play when starting the slam
        public float SlamDelay = 0.3f; // Delay before the slam happens (seconds)
        [Range(10f, 50f)] public float SlamForce = 30f; // Force of the downward slam
        private Coroutine? slamRoutine = null;
        private bool IsInSlam = false;

        public override void Start()
        {
            base.Start();
            trajectoryModifier = new PurplePikminTrajectoryModifier();
        }

        // Override the ThrowPikmin method to add our special slam behavior
        public override void ThrowPikmin(Vector3 Direction)
        {
            // Call the base implementation first to start normal throw
            base.ThrowPikmin(Direction);

            // Start the slam coroutine
            if (slamRoutine == null)
            {
                slamRoutine = StartCoroutine(SlamAfterDelay());
            }
        }

        private IEnumerator SlamAfterDelay()
        {
            // Wait for the Pikmin to reach the apex of its trajectory
            yield return new WaitForSeconds(SlamDelay);

            // Make sure we're still being thrown and not already landed
            if (CurrentIntention == Pintent.Thrown)
            {
                // Play the slam sound
                if (SlamSound != null)
                {
                    PlayAudioOnLocalClient(PikminSoundPackSounds.Attack);
                    creatureSFX.PlayOneShot(SlamSound);
                }

                // Cancel horizontal velocity and add a strong downward force
                SlamDown();

                // Play a smashing animation
                PlayAnimation(animController.AnimPack.EditorStandingAttackAnim);
            }

            slamRoutine = null;
        }

        public void SlamDown()
        {
            if (CurrentIntention == Pintent.Thrown)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0); // Cancel horizontal movement
                rb.AddForce(Vector3.down * SlamForce, ForceMode.Impulse);
                IsInSlam = true;
            }
        }

        public override void LandPikmin()
        {
            base.LandPikmin();
            if (IsInSlam)
            {
                DoSlam();
            }
            IsInSlam = false;

            // If we had a slam routine running, stop it
            if (slamRoutine != null)
            {
                StopCoroutine(slamRoutine);
                slamRoutine = null;
            }
        }

        public void DoSlam()
        {
            creatureSFX.PlayOneShot(LandSound);
            WalkieTalkie.TransmitOneShotAudio(creatureSFX, LandSound);
            RoundManager.Instance.PlayAudibleNoise(transform.position, 10);
            PikUtils.ShakeNearbyPlayers(ScreenShakeType.Big, transform.position, 10f);
            PikUtils.StunNearbyEnemies(transform.position, 10f, 1f);
        }

        public override void LandPikminOnEnemy(Collider hitbox, PikminLatchTrigger latchTrigger, PikminEnemy enemy)
        {
            base.LandPikminOnEnemy(hitbox, latchTrigger, enemy);
            if (CurrentLatchTrigger == null)
            {
                return;
            }
                creatureSFX.PlayOneShot(LandSound);
                PikUtils.ShakeNearbyPlayers(ScreenShakeType.Big, transform.position, 10f);
                enemy.enemyScript.SetEnemyStunned(true, 1);
                RoundManager.Instance.PlayAudibleNoise(transform.position, 10);
                IsInSlam = false;

                // If we had a slam routine running, stop it
                if (slamRoutine != null)
                {
                    StopCoroutine(slamRoutine);
                    slamRoutine = null;
                }
        }
    }
}