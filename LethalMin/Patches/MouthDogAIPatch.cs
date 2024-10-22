using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(MouthDogAI))]
    internal class MouthDogAIPatch
    {
        public static PikminDamager damager;
        
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void GetDamager(MouthDogAI __instance)
        {
            damager = __instance.GetComponent<PikminDamager>();
        }
        
        [HarmonyPatch("OnCollideWithEnemy")]
        [HarmonyPrefix]
        private static bool OnCollideWithPikmin(MouthDogAI __instance, Collider other, EnemyAI collidedEnemy)
        {
            PikminAI pikmin = null;
            if (other.name == "PikminColision")
            {
                pikmin = collidedEnemy.GetComponentInParent<PikminAI>();
                if (pikmin == null)
                {
                    return true;
                }
                if (!(__instance.timeSinceHittingOtherEnemy < 1f))
                {
                    //     if (__instance.currentBehaviourStateIndex == 2 && !__instance.inLunge)
                    //     {
                    //         __instance.transform.LookAt(other.transform.position);
                    //         __instance.transform.localEulerAngles = new Vector3(0f, __instance.transform.eulerAngles.y, 0f);
                    //         __instance.inLunge = true;
                    //         __instance.EnterLunge();
                    //     }
                    if (__instance.inLunge)
                    {
                        if (damager.PikminLatchedOn.Contains(pikmin))
                        {
                            return false; // Skip the original method
                        }
                        __instance.timeSinceHittingOtherEnemy = 0f;
                        // Start the "eating" animation for the Pikmin
                        __instance.StartCoroutine(EatPikminCoroutine(__instance, pikmin));
                    }
                }
                return false; // Skip the original method
            }
            else
            {
                return true; // Continue with the original method if no Pikmin found
            }
        }

        private static IEnumerator EatPikminCoroutine(MouthDogAI mouthDog, PikminAI pikmin)
        {
            // Move the Pikmin to the mouth position
            pikmin.SnapPikminToPosition(mouthDog.mouthGrip, false, true, 1.5f, true);

            // Play the kill sound
            mouthDog.creatureVoice.PlayOneShot(mouthDog.killPlayerSFX);

            // Wait for a short duration
            yield return new WaitForSeconds(1.5f);

            mouthDog.SwitchToBehaviourStateOnLocalClient(2);
            mouthDog.EndLungeServerRpc();
        }

        [HarmonyPatch("EnterLunge")]
        [HarmonyPrefix]
        private static bool ModifyLungeTarget(MouthDogAI __instance)
        {
            if (damager != null)
                damager.ShakePikmin(new Vector3(5, 5, 5), false, false, 0);

            // Check for nearby Pikmin first
            Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, 17f, -1, QueryTriggerInteraction.Collide);
            PikminAI closestPikmin = null;
            float closestDistance = float.MaxValue;

            foreach (Collider collider in colliders)
            {
                PikminAI pikmin = collider.GetComponentInParent<PikminAI>();
                if (pikmin != null && pikmin.currentBehaviourStateIndex == (int)PState.Working && pikmin.targetItem != null)
                {
                    float distance = Vector3.Distance(__instance.transform.position, pikmin.transform.position);
                    if (distance < closestDistance)
                    {
                        closestPikmin = pikmin;
                        closestDistance = distance;
                    }
                }
            }

            if (closestPikmin != null)
            {
                LethalMin.Logger.LogInfo("Lunging at pickles");
                // Modify the lunge target to the Pikmin
                if (!__instance.IsOwner)
                {
                    __instance.ChangeOwnershipOfEnemy(NetworkManager.Singleton.LocalClientId);
                }
                __instance.SwitchToBehaviourState(3);
                __instance.endingLunge = false;
                __instance.ray = new Ray(__instance.transform.position + Vector3.up, __instance.transform.forward);
                Vector3 pos = ((!Physics.Raycast(__instance.ray, out __instance.rayHit, 17f, StartOfRound.Instance.collidersAndRoomMask)) ? __instance.ray.GetPoint(17f) : __instance.rayHit.point);
                pos = __instance.roundManager.GetNavMeshPosition(pos);
                __instance.agent.SetDestination(closestPikmin.transform.position);
                __instance.agent.speed = 13f;
                __instance.noisePositionGuess = closestPikmin.transform.position;
                return false; // Skip the original method
            }

            LethalMin.Logger.LogInfo("lunging at plawers");
            return true; // Continue with the original method if no Pikmin found
        }

        // New method to make MouthDog chase Pikmin

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static void ChasePikmin(MouthDogAI __instance)
        {
            if (__instance.isEnemyDead || __instance.inKillAnimation || __instance.currentBehaviourStateIndex != 2)
                return;

            Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, 20f, -1, QueryTriggerInteraction.Collide);
            PikminAI closestPikmin = null;
            float closestDistance = float.MaxValue;

            foreach (Collider collider in colliders)
            {
                PikminAI pikmin = collider.GetComponentInParent<PikminAI>();
                if (pikmin != null && pikmin.currentBehaviourStateIndex == (int)PState.Working && pikmin.targetItem != null)
                {
                    float distance = Vector3.Distance(__instance.transform.position, pikmin.transform.position);
                    if (distance < closestDistance)
                    {
                        closestPikmin = pikmin;
                        closestDistance = distance;
                    }
                }
            }

            if (closestPikmin != null)
            {
                // Chase the closest Pikmin
                __instance.agent.SetDestination(closestPikmin.transform.position);
                __instance.noisePositionGuess = closestPikmin.transform.position;
                __instance.AITimer = 3f; // Reset the AI timer to maintain chase behavior

                // Check if close enough to lunge
                if (closestDistance <= 5f && !__instance.inLunge) // Adjust the distance as needed
                {
                    LethalMin.Logger.LogInfo("Lunging...");
                    __instance.inLunge = true;
                    __instance.EnterLunge();
                }
            }
        }
    }
}