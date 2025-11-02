using HarmonyLib;
using UnityEngine;

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
                GameObject CustomBounds = GameObject.CreatePrimitive(PrimitiveType.Cube);
                CustomBounds.GetComponent<Collider>().isTrigger = true;
                CustomBounds.transform.SetParent(__instance.GetComponentInChildren<PlayerPhysicsRegion>().transform.parent);
                CustomBounds.transform.localPosition = new Vector3(0.0001f, 0.7978f, 0f);
                CustomBounds.transform.localScale = new Vector3(2f, 4f, 2f);
                CustomBounds.GetComponent<Renderer>().enabled = false;
                CustomBounds.AddComponent<DirectlyPathZone>();
                //CustomBounds.GetComponent<Renderer>().material = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Materials/MapDotA.mat");
                CustomBounds.name = "Pikmin Elevator Bounds";
                ItemArrivalZone.CreateZoneOnObject(__instance.gameObject, ItemArrivalZone.ArrivalZoneType.MineElevator);
                LethalMin.Logger.LogInfo($"Patched OnEnable for {__instance.gameObject.name} to add ItemArrivalZone and DirectlyPathZone.");
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Failed to patch OnEnable for {__instance.gameObject.name} due to: {e}");
            }
        }
    }
}
