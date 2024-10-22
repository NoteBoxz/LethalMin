using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using LethalMin;
using Unity.Netcode;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(StormyWeather))]
    internal class StormyWeatherPatch
    {
        [HarmonyPatch("LightningStrike")]
        [HarmonyPrefix]
        private static void LightningStrikePrefix(Vector3 strikePosition, bool useTargetedObject)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                AffectNearbyPikmin(strikePosition);
            }
        }
        [HarmonyPatch("LightningStrike")]
        [HarmonyPostfix]
        private static void LightningStrikePostfix(Vector3 strikePosition, bool useTargetedObject)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                UnaffectNearbyPikmin(strikePosition);
            }
        }
        static List<PikminAI> AffectedPikminAIList = new List<PikminAI>();
        private static void AffectNearbyPikmin(Vector3 strikePosition)
        {
            float lightningRadius = 6f;

            Collider[] colliders = Physics.OverlapSphere(strikePosition, lightningRadius, -1, QueryTriggerInteraction.Collide);
            foreach (Collider collider in colliders)
            {
                PikminAI pikmin = null;
                if (collider.name == "PikminColision")
                {
                    pikmin = collider.GetComponentInParent<PikminAI>();
                }
                if (pikmin != null)
                {
                    // Check if the Pikmin is Yellow (immune to electricity)
                    if (pikmin.PminType.IsResistantToElectricity)
                    {
                        LethalMin.Logger.LogInfo($"Yellow Pikmin {pikmin.name} is immune to lightning!");
                        AffectedPikminAIList.Add(pikmin);
                        pikmin.Invincible = true;
                        continue; // Skip to the next Pikmin
                    }


                    // LethalMin.Logger.LogInfo($"Pikmin {pikmin.name} struck by lightning and killed.");

                    // // Apply knockback effect
                    // Vector3 knockbackDirection = (pikmin.transform.position - strikePosition).normalized;
                    // float knockbackForce = 20f; // Adjust as needed
                    // Vector3 knockbackVector = knockbackDirection * knockbackForce + Vector3.up * knockbackForce * 0.5f;
                    // pikmin.ApplyKnockbackServerRpc(knockbackVector, true, false, 3f);
                }
            }
        }
        private static void UnaffectNearbyPikmin(Vector3 strikePosition)
        {
            foreach (var item in AffectedPikminAIList)
            {
                LethalMin.Logger.LogInfo($"{item.name} is no longer immune...");
                item.Invincible = false;
            }
            AffectedPikminAIList.Clear();
        }
    }
}