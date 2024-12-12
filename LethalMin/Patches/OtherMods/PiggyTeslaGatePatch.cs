using HarmonyLib;
using PiggyVarietyMod.Patches;
using System.Collections.Generic;
using System.Linq;

namespace LethalMin.Patches.OtherMods
{
    [HarmonyPatch(typeof(TeslaGate))]
    public static class PiggyTeslaGatePatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void StartPrefix(TeslaGate __instance)
        {
            __instance.gameObject.AddComponent<PiggyTeslaGate>();
        }
    }
}