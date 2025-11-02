using System;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using LCOffice.Components;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Compats
{
    [CompatClass("Piggy.LCOffice")]
    [HarmonyPatch(typeof(MetalDetector))]
    public static class PiggyMetalDetectorPatch
    {
        [HarmonyPatch(nameof(MetalDetector.OnTriggerStay))]
        [HarmonyPostfix]
        public static void OnTriggerStayPostfix(MetalDetector __instance, Collider other)
        {
            if (!LethalMin.AllowMetalDetectorToZap) return;
            
            try
            {
                if (!__instance.powered) return;

                if (other.CompareTag("Enemy") && other.gameObject.TryGetComponent(out PikminCollisionDetect detect) && !detect.mainPikmin.IsDeadOrDying)
                {
                    if (__instance.shocking)
                    {
                        if (detect.mainPikmin.IsOwner && !PikChecks.IsPikminResistantToHazard(detect.mainPikmin, PikminHazard.Electricity))
                        {
                            detect.mainPikmin.DoZapDeath();
                            if (detect.mainPikmin.IsSpawned)
                                detect.mainPikmin.DoZapDeathServerRpc();
                        }
                    }
                    else
                    {
                        PikminItem? item = detect.mainPikmin.TargetItem;
                        if (item == null) return;
                        if (item.ItemScript == null) return;
                        if (!item.PikminOnItem.Contains(detect.mainPikmin)) return;
                        if (item.ItemScript.itemProperties == null) return;
                        if (item.ItemScript.itemProperties.isConductiveMetal)
                        {
                            __instance.shockSequence = __instance.StartCoroutine(__instance.ShockSequence());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PiggyMetalDetectorPatch] Error in OnTriggerStayPostfix: {ex.Message}");
            }
        }
    }
}
