using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(Landmine))]
    internal class LandminePatch
    {
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPostfix]
        private static void OnTriggerEnterPostfix(Landmine __instance, Collider other, ref float ___pressMineDebounceTimer)
        {
            if (!LethalMin.LethalLandminesValue) { return; }
            if (__instance.hasExploded || ___pressMineDebounceTimer > 0f)
            {
                return;
            }
            if (other.name != "PikminColision") { return; }
            PikminAI pikmin = null;

            pikmin = other.GetComponentInParent<PikminAI>();
            if (pikmin != null)
            {
                LethalMin.Logger.LogInfo($"Pikmin {pikmin.name} stepped on a landmine!");
                __instance.PressMineServerRpc();
            }
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPostfix]
        private static void OnTriggerExitPostfix(Landmine __instance, Collider other, ref bool ___mineActivated)
        {
            if (!LethalMin.LethalLandminesValue) { return; }
            if (__instance.hasExploded || !___mineActivated)
            {
                return;
            }
            if (other.name != "PikminColision") { return; }
            PikminAI pikmin = null;
            pikmin = other.GetComponentInParent<PikminAI>();
            if (pikmin != null)
            {
                LethalMin.Logger.LogInfo($"Pikmin {pikmin.name} triggered a landmine explosion!");
                __instance.ExplodeMineServerRpc();
            }
        }

        [HarmonyPatch("SpawnExplosion")]
        [HarmonyPrefix]
        public static void SpawnExplosion(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0f, GameObject overridePrefab = null, bool goThroughCar = false)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                AffectNearbyPikmin(explosionPosition, damageRange);
            }
        }
        [HarmonyPatch("SpawnExplosion")]
        [HarmonyPostfix]
        public static void SpawnExplosion2(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0f, GameObject overridePrefab = null, bool goThroughCar = false)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                UnaffectNearbyPikmin(explosionPosition);
            }
        }
        static List<PikminAI> AffectedPikminAIList = new List<PikminAI>();
        private static void AffectNearbyPikmin(Vector3 strikePosition, float lightningRadius = 1f)
        {
            Collider[] colliders = Physics.OverlapSphere(strikePosition, lightningRadius, 2621448, QueryTriggerInteraction.Collide);
            foreach (Collider collider in colliders)
            {
                PikminAI pikmin = null;
                if (collider.name == "PikminColision")
                {
                    pikmin = collider.GetComponentInParent<PikminAI>();
                }
                if (pikmin != null)
                {
                    if (LethalMin.IsPikminResistantToHazard(pikmin.PminType, HazardType.Exsplosive))
                    {
                        LethalMin.Logger.LogInfo($"Orange Pikmin {pikmin.name} is immune to Booms!");
                        AffectedPikminAIList.Add(pikmin);
                        pikmin.Invincible.Value = true;
                        continue; // Skip to the next Pikmin
                    }
                }
            }
        }
        private static void UnaffectNearbyPikmin(Vector3 strikePosition)
        {
            foreach (var item in AffectedPikminAIList)
            {
                LethalMin.Logger.LogInfo($"{item.name} is no longer immune...");
                item.Invincible.Value = false;
            }
            AffectedPikminAIList.Clear();
        }
    }
}