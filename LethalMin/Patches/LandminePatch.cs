using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(Landmine))]
    internal class LandminePatch
    {
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPostfix]
        private static void OnTriggerEnterPostfix(Landmine __instance, Collider other, ref float ___pressMineDebounceTimer)
        {
            if(!LethalMin.LethalLandminesValue){return;}
            if (__instance.hasExploded || ___pressMineDebounceTimer > 0f)
            {
                return;
            }
            if (other.name != "PikminColision") { return; }
            PikminAI pikmin = null;

            pikmin = other.GetComponentInParent<PikminAI>();
            if (pikmin != null)
            {
                LethalMin.Logger.LogInfo($"Pikmin {pikmin.name} stepped on a landmine!");
                __instance.PressMineServerRpc();
            }
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPostfix]
        private static void OnTriggerExitPostfix(Landmine __instance, Collider other, ref bool ___mineActivated)
        {
            if(!LethalMin.LethalLandminesValue){return;}
            if (__instance.hasExploded || !___mineActivated)
            {
                return;
            }
            if (other.name != "PikminColision") { return; }
            PikminAI pikmin = null;
            pikmin = other.GetComponentInParent<PikminAI>();
            if (pikmin != null)
            {
                LethalMin.Logger.LogInfo($"Pikmin {pikmin.name} triggered a landmine explosion!");
                __instance.ExplodeMineServerRpc();
            }
        }
    }
}