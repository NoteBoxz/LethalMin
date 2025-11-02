using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine.AI;
using System.Linq;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(MenuManager))]
    public class MenuManagerPatch
    {
        public static bool HasInitItems = false;
        [HarmonyPatch(nameof(MenuManager.Start))]
        [HarmonyPostfix]
        private static void Start(MenuManager __instance)
        {
            if (HasInitItems)
            {
                return; // already initialized
            }
            try
            {
                // because items that load into the ship will not be registered before they spawn in.
                LethalMin.Logger.LogInfo($"Initalizing Pikmin Items...");
                foreach (Item type in LethalMin.ItemTypes)
                {
                    try
                    {
                        GrabbableObjectPatch.CreatePikminItemOnGrabbableObject(type);
                    }
                    catch (System.Exception e)
                    {
                        LethalMin.Logger.LogError($"Error creating Pikmin Item for {type.name}: {e}");
                    }
                }

                foreach (InteractTrigger trigger in Resources.FindObjectsOfTypeAll<InteractTrigger>())
                {
                    if (trigger.transform.parent != null && trigger.transform.parent.gameObject.name == "SofaChairContainer")
                    {
                        trigger.gameObject.AddComponent<SofaChairPikminInteraction>();
                        LethalMin.Logger.LogInfo($"Added SofaChairPikminInteraction to {trigger.gameObject.name}");
                        break;
                    }
                }

                HasInitItems = true;
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Failed to patch MenuManager.Start for {__instance.gameObject.name} due to: {e}");
            }
        }
    }
}
