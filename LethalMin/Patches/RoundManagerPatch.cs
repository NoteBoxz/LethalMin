using HarmonyLib;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        [HarmonyPatch("SpawnScrapInLevel")]
        [HarmonyPostfix]
        private static void SpawnOnions()
        {
            PikminManager.Instance.SpawnOnionItems();
        }
    }
}