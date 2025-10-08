using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using LethalMin.Utils;
using LethalMin.Achivements;
using UnityEngine.SceneManagement;
using Dusk;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(Landmine))]
    public class LandminePatch
    {
        private static Dictionary<Landmine, List<EnemyAI>> landmineEnemySnapshot = new Dictionary<Landmine, List<EnemyAI>>();

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
                    if (LethalMin.UsingAchivements && AchivementController.WhatHappenedDoable())
                        AchivementController.LandminesTriggeredByPikmin.Add(__instance);

                    __instance.TriggerMineOnLocalClientByExiting();
                }
            }
        }

        [HarmonyPatch(nameof(Landmine.Detonate))]
        [HarmonyPrefix]
        public static void DetonatePrePatch(Landmine __instance)
        {
            if (LethalMin.UsingAchivements && AchivementController.WhatHappenedDoable())
            {
                if (!AchivementController.LandminesTriggeredByPikmin.Contains(__instance))
                    return;
                AchivementController controller = (AchivementController)LethalMin.AchivementController;
                if (controller != null)
                {
                    // Capture enemies that are alive before explosion
                    List<EnemyAI> aliveEnemies = PikUtils.GetAliveEnemiesNearPosition(__instance.transform.position, 6f);
                    landmineEnemySnapshot[__instance] = aliveEnemies;
                }
            }
        }

        [HarmonyPatch(nameof(Landmine.Detonate))]
        [HarmonyPostfix]
        public static void DetonatePostPatch(Landmine __instance)
        {
            if (LethalMin.UsingAchivements
            && AchivementController.WhatHappenedDoable()
            && AchivementController.LandminesTriggeredByPikmin.Contains(__instance)
            && landmineEnemySnapshot.TryGetValue(__instance, out List<EnemyAI> enemiesAliveBeforeExplosion))
            {
                AchivementController controller = (AchivementController)LethalMin.AchivementController;
                if (controller != null)
                {
                    __instance.StartCoroutine(controller.CheckForWhatHappenedAchievement(__instance, enemiesAliveBeforeExplosion));
                }

                // Clean up the snapshot
                landmineEnemySnapshot.Remove(__instance);
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