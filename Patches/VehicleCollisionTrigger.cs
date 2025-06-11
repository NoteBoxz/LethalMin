using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(VehicleCollisionTrigger))]
    public class VehicleCollisionTriggerPatch
    {
        [HarmonyPatch(nameof(VehicleCollisionTrigger.OnTriggerEnter))]
        [HarmonyPrefix]
        public static bool OnTriggerEnterPrefix(VehicleCollisionTrigger __instance,Collider other)
        {
            try
            {
                //Don't let pikmin get hit by the vehicle
                if (other.gameObject.CompareTag("Enemy") && other.gameObject.TryGetComponent(out PikminCollisionDetect detect))
                {
                    return false;
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Error in VehicleCollisionTriggerPatch.OnTriggerEnterPrefix: {e}");
                return true;
            }
            return true;
        }
    }
}
