using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine.AI;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(EntranceTeleport))]
    public class EntranceTeleportPatch
    {
        [HarmonyPatch(nameof(EntranceTeleport.Awake))]
        [HarmonyPostfix]
        private static void Awake(EntranceTeleport __instance)
        {
            try
            {
                if (!__instance.isEntranceToBuilding)
                {
                    ItemArrivalZone.CreateZoneOnObject(__instance.gameObject, ItemArrivalZone.ArrivalZoneType.Exit);
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Error in EntranceTeleportPatch.Awake: {e}");
            }
        }
    }
}
