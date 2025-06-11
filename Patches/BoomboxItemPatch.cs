using HarmonyLib;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(BoomboxItem))]
    public class BoomboxItemPatch
    {
        [HarmonyPatch(nameof(BoomboxItem.Start))]
        [HarmonyPrefix]
        public static void Start(BoomboxItem __instance)
        {
            if (!__instance.TryGetComponent(out BoomBoxPikminInteraction interact))
                __instance.gameObject.AddComponent<BoomBoxPikminInteraction>().boomBoxInstance = __instance;
        }
    }
}
