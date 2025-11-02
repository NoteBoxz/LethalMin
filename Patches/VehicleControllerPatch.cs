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
            try
            {
                if (__instance.gameObject.GetComponent<PikminVehicleController>() == null)
                    __instance.gameObject.AddComponent<PikminVehicleController>();

                ItemArrivalZone.CreateZoneOnObject(__instance.gameObject, ItemArrivalZone.ArrivalZoneType.Crusier);
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Error in VehicleControllerPatch.Awake: {e}");
            }
        }
    }
}
