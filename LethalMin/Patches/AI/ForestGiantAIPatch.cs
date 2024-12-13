using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(ForestGiantAI))]
    internal class ForestGiantAIPatch
    {
        [HarmonyPatch("GrabPlayerServerRpc")]
        [HarmonyPrefix]
        public static void GrabPikmin(ForestGiantAI __instance)
        {
            if (!LethalMin.LethalGiant) { return; }
            LethalMin.Logger.LogInfo("Grabbing Pikmin");
            List<PikminAI> Pikmins = LethalMin.FindNearestPikmin(__instance.centerPosition.position, 10, LethalMin.GiantEatLimmit);

            foreach (var item in Pikmins)
            {
                if (LethalMin.IsPikminResistantToHazard(item.PminType, HazardType.Crush))
                {
                    continue;
                }
                LethalMin.Logger.LogInfo("Grabbing " + item.name);
                if (!item.IsInShip && !item.IsDying && !item.FinnaBeDed && !item.isEnemyDead)
                {
                    item.SnapPikminToPosition(__instance.holdPlayerPoint, false, true, 4.6f, true);
                }
            }
        }
    }
}