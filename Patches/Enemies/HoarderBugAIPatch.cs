using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(HoarderBugAI))]
    public class HoarderBugAIPatch
    {
        [HarmonyPatch(nameof(HoarderBugAI.GrabItem))]
        [HarmonyPrefix]
        public static void GrabItemPatch(HoarderBugAI __instance, NetworkObject item)
        {
            try
            {
                if (__instance.sendingGrabOrDropRPC)
                {
                    return;
                }
                PikminItem Pitm;
                if (item.TryGetComponent(out GrabbableObject itm))
                {
                    Pitm = itm.GetComponentInChildren<PikminItem>();
                    if (Pitm != null && Pitm.IsOwner && Pitm.PikminOnItem.Count > 0)
                    {
                        LethalMin.Logger.LogInfo($"{Pitm.gameObject.name}: Stopping Carry because hording bug held item");
                        Pitm.RemoveAllPikminFromItemServerRpc();
                    }
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"LethalMin: HoarderBugAI.GrabItemPatch: {e}");
            }
        }

    }
}
