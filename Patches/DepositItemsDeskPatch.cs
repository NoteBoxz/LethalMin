using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine.AI;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(DepositItemsDesk))]
    public class DepositItemsDeskPatch
    {
        [HarmonyPatch(nameof(DepositItemsDesk.Start))]
        [HarmonyPostfix]
        private static void Start(DepositItemsDesk __instance)
        {
            try
            {
                ItemArrivalZone.CreateZoneOnObject(__instance.gameObject, ItemArrivalZone.ArrivalZoneType.Counter);
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Error in DepositItemsDeskPatch.Start: {e}");
            }
        }
    }
}
