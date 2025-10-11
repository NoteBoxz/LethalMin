using System.Collections.Generic;
using System.Numerics;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMin.Pikmin;
using Unity.Netcode;
using UnityEngine.AI;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(MineshaftElevatorController))]
    public class MineshaftElevatorControllerPatch
    {
        [HarmonyPatch(nameof(MineshaftElevatorController.OnEnable))]
        [HarmonyPrefix]
        private static void OnEnablePrefix(MineshaftElevatorController __instance)
        {
            try
            {
                if (__instance.GetComponent<ItemArrivalZone>() != null) return;
                
                ItemArrivalZone.CreateZoneOnObject(__instance.gameObject, ItemArrivalZone.ArrivalZoneType.MineElevator);
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Failed to patch OnEnable for {__instance.gameObject.name} due to: {e}");
            }
        }
    }
}
