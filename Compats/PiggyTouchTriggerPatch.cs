using HarmonyLib;
using LethalMin.Utils;
using PiggyVarietyMod.Patches;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalMin.Compats
{
    [CompatClass("Piggy.PiggyVarietyMod")]
    [HarmonyPatch(typeof(CustomTouchInteractTrigger))]
    public static class CustomTouchInteractTriggerPatch
    {
        [HarmonyPatch(nameof(CustomTouchInteractTrigger.OnTriggerEnter))]
        [HarmonyPrefix]
        public static bool OnTriggerEnterPatch(CustomTouchInteractTrigger __instance, Collider collider)
        {
            try
            {
                if (__instance.isKillTrigger)
                {
                    PikminCollisionDetect pikminDetection = collider.GetComponent<PikminCollisionDetect>();
                    if (pikminDetection != null)
                    {
                        if (pikminDetection.mainPikmin.IsDeadOrDying)
                        {
                            return false;
                        }
                        if (PikChecks.IsPikminResistantToHazard(pikminDetection.mainPikmin.pikminType, PikminHazard.Electricity))
                        {
                            return false;
                        }
                        else
                        {
                            if (pikminDetection.mainPikmin.enemyHP <= 5 && pikminDetection.mainPikmin.IsOwner)
                            {
                                pikminDetection.mainPikmin.DoZapDeath();
                                if (pikminDetection.mainPikmin.IsSpawned)
                                    pikminDetection.mainPikmin.DoZapDeathServerRpc();
                            }
                            else if (pikminDetection.mainPikmin.IsOwner)
                            {
                                pikminDetection.mainPikmin.HitEnemyOnLocalClient(5);
                            }
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"LethalMin: CustomTouchInteractTrigger.OnTriggerEnterPatch: {e}");
                return true;
            }
        }


        [HarmonyPatch(nameof(CustomTouchInteractTrigger.OnTriggerStay))]
        [HarmonyPrefix]
        private static bool OnTriggerStayPatch(CustomTouchInteractTrigger __instance, Collider collider)
        {
            try
            {
                if (__instance.isKillTrigger)
                {
                    PikminCollisionDetect pikminDetection = collider.GetComponent<PikminCollisionDetect>();
                    if (pikminDetection != null)
                    {
                        if (pikminDetection.mainPikmin.IsDeadOrDying)
                        {
                            return false;
                        }
                        if (PikChecks.IsPikminResistantToHazard(pikminDetection.mainPikmin.pikminType, PikminHazard.Electricity))
                        {
                            return false;
                        }
                        else
                        {
                            if (pikminDetection.mainPikmin.enemyHP <= 5 && pikminDetection.mainPikmin.IsOwner)
                            {
                                pikminDetection.mainPikmin.DoZapDeath();
                                if (pikminDetection.mainPikmin.IsSpawned)
                                    pikminDetection.mainPikmin.DoZapDeathServerRpc();
                            }
                            else if (pikminDetection.mainPikmin.IsOwner)
                            {
                                pikminDetection.mainPikmin.HitEnemyOnLocalClient(5);
                            }
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"LethalMin: CustomTouchInteractTrigger.OnTriggerEnterPatch: {e}");
                return true;
            }
        }

    }
}