using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(LethalMon.Utils))]
    public class FilterEnemyTypesPatch
    {
        [HarmonyPatch(nameof(LethalMon.Utils.EnemyTypes), MethodType.Getter)]
        [HarmonyPostfix]
        public static void FilterOutPikminEnemyType(ref List<EnemyType> __result)
        {
            if (__result != null && LethalMin.pikminEnemyType != null)
            {
                __result = __result.Where(e => e != LethalMin.pikminEnemyType).ToList();
                LethalMin.Logger.LogInfo($"Filtered out Pikmin enemy type. Remaining enemy types: {__result.Count}");
            }
        }
    }
}