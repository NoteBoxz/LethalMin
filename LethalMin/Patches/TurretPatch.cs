using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static void UpdatePrefix(Turret __instance)
        {
            if (!LethalMin.LethalTurrents) { return; }
            if (__instance.turretMode == TurretMode.Firing)
            {
                if (__instance.turretInterval >= 0.21f)
                {
                    PikminAI pikmin = CheckForPikminInLineOfSight(__instance, 3f);
                    if (pikmin == null) { return; }
                    if (LethalMin.IsPikminResistantToHazard(pikmin.PminType, HazardType.Lethal)) { return; }
                    // Calculate knockback direction (away from the turret)
                    Vector3 knockbackDirection = (pikmin.transform.position - __instance.centerPoint.position).normalized;

                    // Set knockback force
                    float knockbackForce = 10f; // Adjust this value as needed

                    // Calculate knockback vector
                    Vector3 knockbackVector = knockbackDirection * knockbackForce;

                    // Add a small upward force
                    knockbackVector += Vector3.up * 2f;

                    pikmin.ReqeustHurtSFXClientRpc();

                    // Apply knockback
                    pikmin.ApplyKnockbackServerRpc(knockbackVector, true, false, 1f);

                    // Optionally, make the Pikmin face away from the turret
                    pikmin.transform.rotation = Quaternion.LookRotation(knockbackDirection);

                    LethalMin.Logger.LogInfo($"Pikmin {pikmin.name} knocked back by turret. Force: {knockbackVector}");

                }
            }
        }

        public static PikminAI CheckForPikminInLineOfSight(Turret __instance, float radius = 2f, bool angleRangeCheck = false)
        {
            PikminAI hitPikmin;
            Vector3 forward = __instance.aimPoint.forward;
            forward = Quaternion.Euler(0f, (float)(int)(0f - __instance.rotationRange) / radius, 0f) * forward;
            float num = __instance.rotationRange / radius * 2f;

            for (int i = 0; i <= 6; i++)
            {
                Ray shootRay = new Ray(__instance.centerPoint.position, forward);
                RaycastHit[] hits = Physics.RaycastAll(shootRay, 30f, LayerMask.GetMask("Enemies"));

                foreach (RaycastHit hit in hits)
                {
                    PikminAI pikmin = hit.transform.GetComponent<PikminAI>();
                    if (pikmin == null)
                    {
                        pikmin = hit.transform.GetComponentInParent<PikminAI>();
                    }
                    if (pikmin != null)
                    {
                        if (pikmin.IsDying && pikmin.FinnaBeDed && pikmin.isEnemyDead && pikmin.isHeld) { return null; }
                        if (angleRangeCheck)
                        {
                            if (Vector3.Angle(pikmin.transform.position - __instance.centerPoint.position, __instance.forwardFacingPos.forward) <= __instance.rotationRange)
                            {
                                return pikmin;
                            }
                        }
                        else
                        {
                            return pikmin;
                        }
                    }
                }

                forward = Quaternion.Euler(0f, num / 6f, 0f) * forward;
            }
            return null;
        }
    }
}