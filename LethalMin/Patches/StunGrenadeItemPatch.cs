using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using LethalMin;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(StunGrenadeItem))]
    internal class StunGrenadeItemPatch
    {
        [HarmonyPatch("ExplodeStunGrenade")]
        [HarmonyPostfix]
        private static void ExplodeStunGrenadePostfix(StunGrenadeItem __instance, ref bool ___explodeOnThrow)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if ((__instance.chanceToExplode < 100f && !___explodeOnThrow) || (__instance.explodeOnCollision && !StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap && __instance.parentObject == UnityEngine.Object.FindObjectOfType<DepositItemsDesk>().deskObjectsContainer))
                {
                    __instance.StartCoroutine(DelayedKnockback(__instance));
                }
                else
                {
                    KnockbackNearbyPikmin(__instance.transform.position);
                }
            }
        }

        private static IEnumerator DelayedKnockback(StunGrenadeItem stunGrenade)
        {
            // Wait a short time to allow for explosion chance calculation
            yield return new WaitForSeconds(0.1f);

            // Check if the grenade has actually exploded
            if (stunGrenade.hasExploded)
            {
                KnockbackNearbyPikmin(stunGrenade.transform.position);
            }
        }

        private static void KnockbackNearbyPikmin(Vector3 explosionPosition)
        {
            float knockbackRadius = 12f; // Adjust this value based on the stun grenade's effect radius
            float lethalRadius = 2f; // Radius within which Pikmin will be killed
            float maxKnockbackForce = 50f;
            float minKnockbackForce = 15f;

            Collider[] colliders = Physics.OverlapSphere(explosionPosition, knockbackRadius, -1, QueryTriggerInteraction.Collide);
            foreach (Collider collider in colliders)
            {
                PikminAI pikmin = null;
                if (collider.name == "PikminColision")
                {
                    pikmin = collider.GetComponentInParent<PikminAI>();
                }
                if (pikmin != null)
                {
                    float distance = Vector3.Distance(pikmin.transform.position, explosionPosition);
                    bool isLethal = distance <= lethalRadius;

                    Vector3 knockbackDirection = (pikmin.transform.position - explosionPosition).normalized;
                    float distanceFactor = 1f - (distance / knockbackRadius); // 0 to 1, closer = higher
                    float knockbackForce = Mathf.Lerp(minKnockbackForce, maxKnockbackForce, distanceFactor);

                    Vector3 knockbackVector = knockbackDirection * knockbackForce;
                    knockbackVector += Vector3.up * knockbackForce * 0.5f;

                    pikmin.ApplyKnockbackServerRpc(knockbackVector, isLethal, true, 3f);
                    pikmin.transform.rotation = Quaternion.LookRotation(knockbackDirection);

                    LethalMin.Logger.LogInfo($"Pikmin {pikmin.name} affected by stun grenade. Force: {knockbackVector} Distance: {distance}, Lethal: {isLethal}");
                }
            }
        }
    }
}