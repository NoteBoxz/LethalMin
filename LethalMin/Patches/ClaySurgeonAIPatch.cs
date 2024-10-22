using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(ClaySurgeonAI))]
    internal class ClaySurgeonAIPatch
    {
        [HarmonyPatch("DanceBeat")]
        [HarmonyPostfix]
        private static void DanceBeatPostfix(ClaySurgeonAI __instance)
        {
            if (!LethalMin.LethalBarber) { return; }
            bool HasSnipped = false;
            if (__instance.stunNormalizedTimer <= 0f && __instance.IsServer)
            {
                List<PikminAI> pikmin = new List<PikminAI>();
                pikmin = LethalMin.FindNearestPikmin(__instance.transform.position, LethalMin.BarberRange, LethalMin.BarberEatLimmit);

                foreach (PikminAI pik in pikmin)
                {
                    if (pik == null || pik.isEnemyDead || pik.IsDying || pik.FinnaBeDed || pik.KncockedBack || pik.GrowStage <= 0) { continue; }
                    if (!HasSnipped)
                    {
                        __instance.creatureAnimator.SetTrigger("snip");
                        __instance.creatureSFX.PlayOneShot(__instance.snipScissors);
                        HasSnipped = true;
                    }
                    pik.GrowStage = 0;
                    pik.UpdateGrowStageClientRpc(0);

                    // Calculate direction from ClaySurgeon to Pikmin
                    Vector3 knockbackDirection = (pik.transform.position - __instance.transform.position).normalized;

                    // Add some randomness to the direction
                    float randomX = pik.enemyRandom.Next(-5, 5) * 0.1f;
                    float randomZ = pik.enemyRandom.Next(-5, 5) * 0.1f;
                    knockbackDirection += new Vector3(randomX, 0, randomZ);

                    // Scale the knockback force (adjust 15f as needed for desired knockback strength)
                    Vector3 knockbackForce = knockbackDirection * 15f;

                    // Ensure some upward force
                    knockbackForce.y = Mathf.Max(knockbackForce.y, 2f);

                    pik.ApplyKnockbackServerRpc(knockbackForce, false, false, 0);
                }
                HasSnipped = false;
            }
        }

    }
}