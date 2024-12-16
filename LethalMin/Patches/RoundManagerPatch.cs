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
        
        [HarmonyPatch("FinishGeneratingNewLevelClientRpc")]
        [HarmonyPostfix]
        public static void FinishGeneratingNewLevelClientRpcPostfix(StartOfRound __instance)
        {
            PikminManager.Instance.StartCoroutine(PikminManager.Instance.GetFloorData());
        }

    }
}