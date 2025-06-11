using HarmonyLib;
using LethalMin.Pikmin;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(LungProp))]
    public class LungPropPatch
    {
        [HarmonyPatch(nameof(LungProp.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(LungProp __instance)
        {
            try
            {
                __instance.gameObject.AddComponent<LungPropPikmin>();
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"LungPropPatch.StartPostfix: {e}");
            }
        }
    }
}
