using System;
using HarmonyLib;
using Imperium.Interface.SpawningUI;
using Imperium.Patches.Systems;
using LethalMin.Pikmin;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Compats
{
    [CompatClass("giosuel.Imperium")]
    [HarmonyPatch(typeof(SpawningUI))]
    public class SpawningUIPatch
    {
        [HarmonyPatch(nameof(SpawningUI.GenerateItems))]
        [HarmonyPostfix]
        private static void GenerateItemsPostfix(SpawningUI __instance)
        {
            if (!LethalMin.RemovePuffminFromSpawnSearch)
            {
                return;
            }
            
            int index = -1;
            foreach (SpawningObjectEntry soe in __instance.entries)
            {
                if (soe.displayName.Contains("Puffmin"))
                {
                    index = __instance.entries.IndexOf(soe);
                }
            }
            if (index != -1)
            {
                __instance.entries.RemoveAt(index);
            }
        }
    }
}
