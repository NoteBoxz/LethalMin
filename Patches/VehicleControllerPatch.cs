using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(VehicleController))]
    public class VehicleControllerPatch
    {
        [HarmonyPatch(nameof(VehicleController.Awake))]
        [HarmonyPostfix]
        public static void AwakePostfix(VehicleController __instance)
        {
            if (__instance.gameObject.GetComponent<PikminVehicleController>() == null)
                __instance.gameObject.AddComponent<PikminVehicleController>();
        }
    }
}
