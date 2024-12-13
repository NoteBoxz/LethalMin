using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(BaboonBirdAI))]
    internal class BaboonBirdAIPatch
    {
        public static PikminDamager damager;
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void GetDamager(MouthDogAI __instance)
        {
            damager = __instance.GetComponent<PikminDamager>();
        }

        [HarmonyPatch("OnCollideWithEnemy")]
        [HarmonyPrefix]
        private static bool OnCollideWithPikmin(BaboonBirdAI __instance, Collider other, EnemyAI enemyScript)
        {
            if (damager != null)
            {
                damager.ShakePikmin(new Vector3(5, 5, 5), false, false, 0);
            }
            else
            {
                damager = __instance.gameObject.AddComponent<PikminDamager>();
                damager.RootScript = __instance;
                damager.ShakePikmin(new Vector3(5, 5, 5), false, false, 0);
            }
            PikminAI pikmin = enemyScript.GetComponent<PikminAI>();
            if (other.name == "PikminColision")
            {
                if (pikmin == null)
                {
                    return true;
                }
                if (!(__instance.timeSinceHitting < 0.75f) && !(__instance.stunNormalizedTimer > 0f) && !__instance.isEnemyDead && !pikmin.CannotEscape && pikmin.currentBehaviourStateIndex != (int)PState.Airborn)
                {
                    pikmin.KnockbackOnEnemy(new Vector3(5, 5, 5), __instance.deadBodyPoint, true, 1.5f, false);
                    __instance.creatureAnimator.ResetTrigger("Hit");
                    __instance.creatureAnimator.SetTrigger("Hit");
                    __instance.creatureSFX.PlayOneShot(__instance.enemyType.audioClips[5]);
                    WalkieTalkie.TransmitOneShotAudio(__instance.creatureSFX, __instance.enemyType.audioClips[5]);
                    RoundManager.Instance.PlayAudibleNoise(__instance.creatureSFX.transform.position, 8f, 0.7f);
                    __instance.timeSinceHitting = 0f;
                }
                return false; // Skip the original method
            }
            else
            {
                return true; // Continue with the original method if no Pikmin found
            }
        }

    }
}