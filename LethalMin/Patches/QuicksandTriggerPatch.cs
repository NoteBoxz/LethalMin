using HarmonyLib;
using LethalMin;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(QuicksandTrigger))]
    public class QuicksandTriggerPatch
    {
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPostfix]
        public static void AwakePostfix(Collider other, QuicksandTrigger __instance)
        {
            PikminAI targetPikmin = null;
            if (!__instance.isWater) { return; }
            if (other.name == "PikminColision")
                targetPikmin = other.GetComponentInParent<PikminAI>();
            if (targetPikmin != null && targetPikmin.HasInitalized && !LethalMin.IsPikminResistantToHazard(targetPikmin.PminType, HazardType.Water) && !targetPikmin.IsDrowing)
            {
                targetPikmin.SetDrowingClientRpc();
                //LethalMin.Logger.LogInfo($"{targetPikmin.uniqueDebugId} Has entered water!");
            }
        }
        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPostfix]
        public static void StartPostfix(Collider other, QuicksandTrigger __instance)
        {
            PikminAI targetPikmin = null;
            if (!__instance.isWater) { return; }
            if (other.name == "PikminColision")
                targetPikmin = other.GetComponentInParent<PikminAI>();
            if (targetPikmin != null && targetPikmin.HasInitalized && !LethalMin.IsPikminResistantToHazard(targetPikmin.PminType, HazardType.Water) && targetPikmin.IsDrowing)
            {
                targetPikmin.StopDrowingClientRpc();
                //LethalMin.Logger.LogInfo($"{targetPikmin.uniqueDebugId} Has entered water!");
            }
        }
    }
}