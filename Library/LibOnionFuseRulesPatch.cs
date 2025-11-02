using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using LethalMinLibrary;
using System;
using System.Reflection;
using System.Linq;

namespace LethalMin.Library
{
    [HarmonyPatch(typeof(LibOnionFuseRules))]
    internal class LibOnionFuseRulesPatch
    {
        [HarmonyPatch(nameof(LibOnionFuseRules.OnEnable))]
        [HarmonyPrefix]
        private static void OnEnable4Real(LibOnionFuseRules __instance)
        {
            // for whatever goofy reason the LibOnionFuseRules that was imported from a .LethalMin file isn't apart of the loaded asset bundle
            // so we need to manually match it with the correct type in the ConvertedTypesList
            if (__instance.ModInfo != null && __instance.ModInfo.DontLoad)
                DepsManager.MatchPossibleTypeWithDep(__instance);
        }
    }
}