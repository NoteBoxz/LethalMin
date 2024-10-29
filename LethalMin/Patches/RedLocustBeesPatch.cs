using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Linq;
using System.Reflection.Emit;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(RedLocustBees))]
    internal class RedLocustBeesPatch
    {
        public static void ShockPikmin(RedLocustBees __instance)
        {
            List<PikminAI> MinsInWay = LethalMin.FindNearestPikmin(__instance.transform.position, 3.4f, LethalMin.BeesShockCount);
            if (MinsInWay.Count > 0)
            {
                LethalMin.Logger.LogInfo($"Found {MinsInWay.Count} Pikmin in range.");
                __instance.BeesZap();
                foreach (var item in MinsInWay)
                {
                    if (!item.IsDying && !item.FinnaBeDed && !item.isEnemyDead && !item.isHeld && !item.PminType.IsResistantToElectricity)
                    {
                        // Calculate knockback direction (away from the turret)
                        Vector3 knockbackDirection = (item.transform.position - __instance.transform.position).normalized;

                        // Set knockback force
                        float knockbackForce = 10f; // Adjust this value as needed

                        // Calculate knockback vector
                        Vector3 knockbackVector = knockbackDirection * knockbackForce;

                        // Add a small upward force
                        knockbackVector += Vector3.up * 2f;

                        if (LethalMin.SuperLethalBees)
                            item.ReqeustHurtSFXClientRpc();

                        // Apply knockback
                        item.ApplyKnockbackServerRpc(knockbackVector, LethalMin.SuperLethalBees, false, 2.5f);

                        // Optionally, make the item face away from the turret
                        item.transform.rotation = Quaternion.LookRotation(knockbackDirection);
                    }
                }

            }
        }
    }
}