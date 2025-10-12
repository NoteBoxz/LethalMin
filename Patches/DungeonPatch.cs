using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine.AI;
using DunGen;
using LethalMin.Routeing;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(Dungeon))]
    public class DungeonPatch
    {
        [HarmonyPatch(nameof(Dungeon.PostGenerateDungeon))]
        [HarmonyPrefix]
        private static void PostGenerateDungeonPrefix(Dungeon __instance)
        {
            try
            {
                int numberOfDungeons = Object.FindObjectsOfType<Dungeon>().Length;
                PikminRouteManager.Instance.CurrentLevelHasMultipleDungeons = numberOfDungeons > 1;
                PikminRouteManager.Instance.Dungeons.Add(__instance);
                LethalMin.Logger.LogDebug($"Number of Dungeons in Scene: {numberOfDungeons} (Multiple Dungeons: {PikminRouteManager.Instance.CurrentLevelHasMultipleDungeons})");
            }
            catch (System.Exception ex)
            {
                LethalMin.Logger.LogError($"Error in Dungeon.PostGenerateDungeon Prefix: {ex}");
            }
        }
    }
}
