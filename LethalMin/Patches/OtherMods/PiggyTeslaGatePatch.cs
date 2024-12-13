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
        [HarmonyPostfix]
        public static void StartPostfix(TeslaGate __instance)
        {
            PikminProtector Protection = __instance.gameObject.AddComponent<PikminProtector>();
            Protection.HazardTypez = new HazardType[1];
            Protection.HazardTypez[0] = HazardType.Electric;
            //Protection.ProtectTime = 1f;
        }

    }
}