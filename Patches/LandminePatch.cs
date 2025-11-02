using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using LethalMin.Utils;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(Landmine))]
    public class LandminePatch
    {
        [HarmonyPatch(nameof(Landmine.OnTriggerEnter))]
        [HarmonyPostfix]
        private static void OnTriggerEnterPostfix(Landmine __instance, Collider other)
        {
            if (__instance.hasExploded || __instance.pressMineDebounceTimer > 0f)
            {
                return;
            }

            if (other.CompareTag("Enemy"))
            {
                if (LethalMin.TriggerLandmines && other.TryGetComponent(out PikminCollisionDetect detect) && detect.mainPikmin.IsOwner)
                {
                    __instance.pressMineDebounceTimer = 0.5f;
                    __instance.PressMineServerRpc();
                }
            }
        }

        [HarmonyPatch(nameof(Landmine.OnTriggerExit))]
        [HarmonyPostfix]
        private static void OnTriggerExitPostfix(Landmine __instance, Collider other)
        {
            if (__instance.hasExploded || !__instance.mineActivated)
            {
                return;
            }
            if (other.CompareTag("Enemy"))
            {
                if (LethalMin.TriggerLandmines && other.TryGetComponent(out PikminCollisionDetect detect) && detect.mainPikmin.IsOwner)
                {
                    __instance.TriggerMineOnLocalClientByExiting();
                }
            }
        }

        static List<PikminAI> pikminList = new List<PikminAI>();
        [HarmonyPatch(nameof(Landmine.SpawnExplosion))]
        [HarmonyPrefix]
        public static void SpawnExplosionPrePatch(Landmine __instance, Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0f, GameObject overridePrefab = null!, bool goThroughCar = false)
        {
            try
            {
                Collider[] array = Physics.OverlapSphere(explosionPosition, damageRange, 2621448, QueryTriggerInteraction.Collide);
                RaycastHit hitInfo;
                for (int i = 0; i < array.Length; i++)
                {
                    float num2 = Vector3.Distance(explosionPosition, array[i].transform.position);
                    if (Physics.Linecast(explosionPosition, array[i].transform.position + Vector3.up * 0.3f, out hitInfo, 1073742080, QueryTriggerInteraction.Ignore) && ((!goThroughCar && hitInfo.collider.gameObject.layer == 30) || num2 > 4f))
                    {
                        continue;
                    }
                    if (array[i].gameObject.layer == 19)
                    {
                        PikminCollisionDetect componentInChildren2 = array[i].gameObject.GetComponentInChildren<PikminCollisionDetect>();
                        if (componentInChildren2 != null && componentInChildren2.mainPikmin.IsOwner && (num2 <= damageRange || num2 <= killRange))
                        {
                            if (PikChecks.IsPikminResistantToHazard(componentInChildren2.mainPikmin, PikminHazard.Explosive, false))
                            {
                                pikminList.Add(componentInChildren2.mainPikmin);
                                componentInChildren2.mainPikmin.SetInvincibiltyServerRpc(true);
                                componentInChildren2.mainPikmin.Invincible = true;
                            }
                            componentInChildren2.mainPikmin.HitFromExplosionAndSync(explosionPosition, killRange, damageRange, nonLethalDamage, physicsForce);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"LethalMin: Error in LandmineExsplotionPatch: {e}");
            }
        }

        [HarmonyPatch(nameof(Landmine.SpawnExplosion))]
        [HarmonyPostfix]
        public static void SpawnExplosionPostPatch()
        {
            foreach (PikminAI pikmin in pikminList)
            {
                if (pikmin != null && pikmin.IsOwner)
                {
                    pikmin.SetInvincibiltyServerRpc(false);
                    pikmin.Invincible = false;
                }
            }
            pikminList.Clear();
        }
    }
}