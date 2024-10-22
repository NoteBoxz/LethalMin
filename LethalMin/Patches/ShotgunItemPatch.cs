using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
[HarmonyPatch(typeof(ShotgunItem))]
internal class ShotgunItemPatch
{
    [HarmonyPatch("ShootGun")]
    [HarmonyPostfix]
    private static void ShootGunPostfix(ShotgunItem __instance, Vector3 shotgunPosition, Vector3 shotgunForward)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            PlayerControllerB shootingPlayer = __instance.playerHeldBy;
            KnockbackNearbyPikmin(shotgunPosition, shotgunForward, shootingPlayer);
        }
    }

    private static void KnockbackNearbyPikmin(Vector3 shotgunPosition, Vector3 shotgunForward, PlayerControllerB shootingPlayer)
    {
        float knockbackRadius = 10f; // Adjust this value as needed
        float maxKnockbackForce = 30f; // Adjust this value as needed
        float minKnockbackForce = 10f; // Adjust this value as needed

        Collider[] colliders = Physics.OverlapSphere(shotgunPosition, knockbackRadius, -1, QueryTriggerInteraction.Collide);
        foreach (Collider collider in colliders)
        {
            PikminAI pikmin = null;
            if (collider.name == "PikminColision")
            {
                pikmin = collider.GetComponentInParent<PikminAI>();
            }
            if (pikmin != null)
            {
                if (pikmin.currentLeader != null && pikmin.currentLeader.Controller == shootingPlayer) { return; }
                Vector3 toPikmin = pikmin.transform.position - shotgunPosition;
                float angle = Vector3.Angle(shotgunForward, toPikmin);

                // Only affect Pikmin within a 60-degree cone in front of the shotgun
                if (angle <= 30f)
                {
                    float distance = toPikmin.magnitude;
                    Vector3 knockbackDirection = toPikmin.normalized;
                    float distanceFactor = 1f - (distance / knockbackRadius); // 0 to 1, closer = higher
                    float knockbackForce = Mathf.Lerp(minKnockbackForce, maxKnockbackForce, distanceFactor);

                    Vector3 knockbackVector = knockbackDirection * knockbackForce;
                    knockbackVector += Vector3.up * knockbackForce * 0.3f;

                    if (distanceFactor > 0.15f)
                    {
                        pikmin.ApplyKnockbackServerRpc(knockbackVector, true, true, 2f);
                    }
                    else
                    {
                        pikmin.ApplyKnockbackServerRpc(knockbackVector, false, false, 2f);
                    }
                    pikmin.transform.rotation = Quaternion.LookRotation(knockbackDirection);

                    LethalMin.Logger.LogInfo($"Pikmin {pikmin.name} affected by shotgun. Force: {knockbackVector} Distance: {distance}");
                }
            }
        }
    }
}
}