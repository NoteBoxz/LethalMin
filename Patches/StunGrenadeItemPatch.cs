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
        [HarmonyPatch(nameof(StunGrenadeItem.StunExplosion))]
        [HarmonyPrefix]
        private static void StunExplosion(StunGrenadeItem __instance, Vector3 explosionPosition,
        bool affectAudio, float flashSeverityMultiplier, float enemyStunTime, float flashSeverityDistanceRolloff = 1f,
        bool isHeldItem = false, PlayerControllerB playerHeldBy = null!, PlayerControllerB playerThrownBy = null!, float addToFlashSeverity = 0f)
        {
            try
            {
                //the radius is hard coded in the game for some reason
                float knockbackRadius = 12f;

                Collider[] colliders = Physics.OverlapSphere(explosionPosition, knockbackRadius, -1, QueryTriggerInteraction.Collide);
                foreach (Collider collider in colliders)
                {
                    PikminCollisionDetect detect = collider.GetComponent<PikminCollisionDetect>();
                    if (detect == null)
                    {
                        continue;
                    }

                    PikminAI pikmin = detect.mainPikmin;
                    if (Physics.Linecast(explosionPosition + Vector3.up * 0.5f, pikmin.transform.position + Vector3.up * 0.5f, 256))
                    {
                        continue;
                    }

                    if (!pikmin.IsOwner)
                    {
                        continue;
                    }

                    // Calculate distance from explosion to determine damage parameters
                    float distance = Vector3.Distance(pikmin.transform.position, explosionPosition);

                    float stunKillRange = 1.0f;  // Very small kill range
                    float stunDamageRange = 12.0f; // Moderate damage range
                    int stunDamage = 0; // Pikmin should either be killed or just knockback with no HP loss

                    LethalMin.Logger.LogDebug($"{pikmin.DebugID} was hit from stun explotion at distance ({distance})");

                    // Apply explosion effect with appropriate parameters
                    pikmin.HitFromExplosionAndSync(
                        explosionPosition,
                        stunKillRange,
                        stunDamageRange,
                        stunDamage
                    );


                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"LethalMin: Error in StunGrenadeItemPatch: {e}");
            }
        }
    }
}