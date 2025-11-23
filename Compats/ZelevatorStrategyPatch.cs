using ElevatorMod.Patches;
using HarmonyLib;
using LethalMin.HUD;
using LethalMin.Pikmin;
using LethalMin.Routeing;
using LethalMin.Utils;
using PiggyVarietyMod.Patches;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalMin.Compats
{
    [CompatClass("kite.ZelevatorCode")]
    [HarmonyPatch(typeof(MoonOverrideStrategy))]
    public static class ZelevatorStrategyPatch
    {
        [HarmonyPatch(nameof(MoonOverrideStrategy.GenerateRoute))]
        [HarmonyPostfix]
        static void GenerateRoutePostfix(PikminRouteRequest request, RouteContext context, ref List<RouteNode> __result)
        {
            // custom logic here
            if (PikminManager.instance.CurrentMoonSettings == EndlessElevatorPatch.ZelevatorPath && __result == null)
            {
                __result = new List<RouteNode>();
                __result.Add(EndlessElevatorPatch.ElevateNode);
                if(!context.IsInside)
                {
                    LethalMin.Logger.LogWarning("Zelevator route generated for outside request.");
                }
            }
        }
    }
}