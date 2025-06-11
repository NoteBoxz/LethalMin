using HarmonyLib;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(SpikeRoofTrap))]
    public class SpikeRoofTrapPatch
    {
        [HarmonyPatch(nameof(SpikeRoofTrap.OnTriggerStay))]
        [HarmonyPrefix]
        public static bool OnTriggerStayPrefix(SpikeRoofTrap __instance, Collider other)
        {
            try
            {
                if (!__instance.trapActive || !__instance.slammingDown || Time.realtimeSinceStartup - __instance.timeSinceMovingUp < 0.75f)
                {
                    return true;
                }
                PikminCollisionDetect component3 = other.gameObject.GetComponent<PikminCollisionDetect>();
                if (component3 != null && component3.mainPikmin != null && component3.mainPikmin.IsOwner && !component3.mainPikmin.isEnemyDead)
                {
                    if (PikChecks.IsPikminResistantToHazard(component3.mainPikmin, PikminHazard.Crush, __instance))
                    {
                        return false;
                    }
                    component3.mainPikmin.DoSquishDeath();
                    component3.mainPikmin.DoSquishDeathServerRpc();
                    return false;
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Error in SpikeRoofTrapPatch.OnTriggerEnterPrefix: {e}");
                return true;
            }
            return true;
        }
    }
}
