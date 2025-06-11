using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using LethalMin;
using Unity.Netcode;
using LethalMin.Utils;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(StormyWeather))]
    internal class StormyWeatherPatch
    {
        static List<PikminAI> AffectedPikminAIList = new List<PikminAI>();

        [HarmonyPatch(nameof(StormyWeather.LightningStrike))]
        [HarmonyPrefix]
        private static void LightningStrikePrefix(Vector3 strikePosition, bool useTargetedObject)
        {
            AffectNearbyPikmin(strikePosition);
        }
        [HarmonyPatch(nameof(StormyWeather.LightningStrike))]
        [HarmonyPostfix]
        private static void LightningStrikePostfix(Vector3 strikePosition, bool useTargetedObject)
        {
            UnaffectNearbyPikmin(strikePosition);
        }

        private static void AffectNearbyPikmin(Vector3 strikePosition)
        {
            float lightningRadius = 6f;

            Collider[] colliders = Physics.OverlapSphere(strikePosition, lightningRadius, 2621448, QueryTriggerInteraction.Collide);
            foreach (Collider collider in colliders)
            {
                PikminCollisionDetect detect = collider.GetComponent<PikminCollisionDetect>();
                if (detect == null)
                {
                    continue;
                }

                PikminAI pikmin = detect.mainPikmin;
                if (!pikmin.IsOwner)
                {
                    continue;
                }

                // Check if the Pikmin is Yellow (immune to electricity)
                if (PikChecks.IsPikminResistantToHazard(pikmin.pikminType, PikminHazard.Electricity))
                {
                    LethalMin.Logger.LogDebug($"Yellow Pikmin {pikmin.gameObject.name} is immune to lightning!");
                    AffectedPikminAIList.Add(pikmin);
                    pikmin.SetInvincibiltyServerRpc(true);
                    continue; // Skip to the next Pikmin
                }
            }
        }

        private static void UnaffectNearbyPikmin(Vector3 strikePosition)
        {
            foreach (var item in AffectedPikminAIList)
            {
                if (!item.IsOwner)
                {
                    continue;
                }
                LethalMin.Logger.LogDebug($"{item.gameObject.name} is no longer immune...");
                item.SetInvincibiltyServerRpc(false);
            }
            AffectedPikminAIList.Clear();
        }
    }
}