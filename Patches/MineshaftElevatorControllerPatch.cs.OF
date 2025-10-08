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
        public static RouteNode node = null!;
        [HarmonyPatch(nameof(MineshaftElevatorController.LateUpdate))]
        [HarmonyPrefix]
        private static void SetRoute(MineshaftElevatorController __instance)
        {
            try
            {
                if(node == null || node.cachedNode == null) { return; }

                node.cachedNode.DontDoInRangeCheck = !__instance.elevatorMovingDown && LethalMin.CanPathOutsideWhenInside.InternalValue;
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Failed to patch NavMeshAgent.setdest for {__instance.gameObject.name} due to: {e}");
            }
        }
    }
}
