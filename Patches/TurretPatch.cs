using HarmonyLib;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(Turret))]
    public class TurretPatch
    {
        [HarmonyPatch(nameof(Turret.Update))]
        [HarmonyPrefix]
        public static void UpdatePostfix(Turret __instance)
        {
            try
            {
                switch (__instance.turretMode)
                {
                    case TurretMode.Firing:
                        if (__instance.turretInterval >= 0.21f)
                        {
                            PikminAI? piki = CheckForPikminInLineOfSight(__instance, 3f);
                            if (piki != null && !PikChecks.IsPikminResistantToHazard(piki, PikminHazard.Bullet) && piki.IsOwner)
                            {
                                Vector3 direction = piki.transform.position - __instance.aimPoint.position;
                                piki.HitEnemyOnLocalClient(2, direction);
                            }
                        }
                        break;
                    case TurretMode.Berserk:
                        if (__instance.enteringBerserkMode)
                        {
                            break;
                        }
                        if (__instance.turretInterval >= 0.21f)
                        {
                            PikminAI? piki = CheckForPikminInLineOfSight(__instance, 3f);
                            if (piki != null && !PikChecks.IsPikminResistantToHazard(piki, PikminHazard.Bullet) && piki.IsOwner)
                            {
                                Vector3 direction = piki.transform.position - __instance.aimPoint.position;
                                piki.HitEnemyOnLocalClient(2, direction);
                            }
                        }
                        break;
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Error in TurretPatch.UpdatePostfix: {e}");
            }
        }

        public static PikminAI? CheckForPikminInLineOfSight(Turret __instance, float radius = 2f, bool angleRangeCheck = false)
        {
            Vector3 forward = __instance.aimPoint.forward;
            Vector3 up = __instance.aimPoint.up;
            forward = Quaternion.Euler(0f, (float)(int)(0f - __instance.rotationRange) / radius, 0f) * forward;
            float num = __instance.rotationRange / radius * 2f;
            for (int i = 0; i <= 6; i++)
            {
                Ray shootRay = new Ray(__instance.centerPoint.position - up, forward);
                if (Physics.Raycast(shootRay, out RaycastHit hit, 30f, LayerMask.GetMask("Enemies")))
                {
                    if (hit.transform.CompareTag("Enemy"))
                    {
                        PikminCollisionDetect component = hit.transform.GetComponent<PikminCollisionDetect>();
                        if (component != null && component.mainPikmin != null && !component.mainPikmin.IsDeadOrDying && component.mainPikmin.IsSpawned)
                        {
                            if (angleRangeCheck && Vector3.Angle(component.transform.position + Vector3.up * 1.75f - __instance.centerPoint.position,
                            __instance.forwardFacingPos.forward) > __instance.rotationRange)
                            {
                                return null;
                            }
                            return component.mainPikmin;
                        }
                        continue;
                    }
                }
                forward = Quaternion.Euler(0f, num / 6f, 0f) * forward;
            }
            return null;
        }
    }
}
