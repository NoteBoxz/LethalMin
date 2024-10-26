using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Linq;

namespace LethalMin
{
    public class PikminAttacker : NetworkBehaviour
    {
        public PikminAttackable SABOBJ;
        public int EatBuffer;
        EnemyAI instance;
        public void AttackNearbyPikmin(EnemyAI __instance)
        {
            if (!IsServer)
            {
                //LethalMin.Logger.LogInfo("Not the server.");
                return;
            }
            if (!SABOBJ.HarmfulToPikmin)
            {
                //LethalMin.Logger.LogInfo("Not harmful to Pikmin.");
                return;
            }
            if (!SABOBJ.AttackInAnyState && !SABOBJ.AttackStates.Contains(__instance.currentBehaviourStateIndex))
            {
                //LethalMin.Logger.LogInfo($"Not in attack state. Current state: {__instance.currentBehaviourStateIndex}");
                return;
            }
            if (__instance.isEnemyDead)
            {
                //LethalMin.Logger.LogInfo("Enemy is dead.");
                return;
            }
            if (EatBuffer > 0)
            {
                //LethalMin.Logger.LogInfo("Eat buffer active.");
                EatBuffer--;
                return;
            }

            //LethalMin.Logger.LogInfo("Checking for Pikmin in range.");
            
            List<PikminAI> MinsInWay = LethalMin.FindNearestPikmin(
                SABOBJ.CheckAtGrabPos == true ? __instance.transform.Find(SABOBJ.PikminGrabPath).position : __instance.transform.position,
                SABOBJ.AttackRange, SABOBJ.MaxPikminEatCount);

            //LethalMin.Logger.LogInfo($"Found {MinsInWay.Count} Pikmin in range.");

            if (MinsInWay.Count > 0)
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"Found {MinsInWay.Count} Pikmin in range.");
                if (MinsInWay.Count > SABOBJ.MaxPikminEatCount)
                {
                    MinsInWay = MinsInWay.Take(SABOBJ.MaxPikminEatCount).ToList();
                }
                instance = __instance;
                DoattackClientRpc();
                foreach (var item in MinsInWay)
                {
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo($"Eating Pikmin {item.gameObject.name}.");
                    if (!item.IsDying && !item.FinnaBeDed && !item.isEnemyDead && !item.isHeld)
                        item.SnapPikminToPosition(__instance.transform.Find(SABOBJ.PikminGrabPath), false, true, 2, true);
                }
                EatBuffer = SABOBJ.AttackBuffer; // Set the eat buffer to 3 after eating
            }
        } 
        [ClientRpc]
        public void DoattackClientRpc()
        {
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo("Doing attack visuals.");
            if (instance == null)
            {
                LethalMin.Logger.LogWarning("Instance is null.");
                return;
            }
            LethalMin.Logger.LogInfo($"Playing sfx");
            if (SABOBJ.AttackSound != null)
            {
                instance?.creatureSFX.PlayOneShot(SABOBJ.AttackSound);
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"Played sfx");
            }
            if (!string.IsNullOrEmpty(SABOBJ.AttackAnimName))
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"Playing attack anim {SABOBJ.AttackAnimName}.");
                instance?.creatureAnimator.Play(SABOBJ.AttackAnimName);
            }
            if (!string.IsNullOrEmpty(SABOBJ.AttackAnimTrigger))
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo($"Playing attack anim trigger {SABOBJ.AttackAnimTrigger}.");
                instance?.creatureAnimator.SetTrigger(SABOBJ.AttackAnimTrigger);
            }
            instance = null;
        }
        void LateUpdate()
        {
            CheckAndDespawnIfParentDestroyed();
        }
        private void CheckAndDespawnIfParentDestroyed()
        {
            if (!IsServer)
            {
                return;
            }
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                if (transform.parent == null || transform.parent.gameObject == null)
                {
                    // Parent has been destroyed, despawn this NetworkObject
                    if (IsServer)
                        NetworkObject.Despawn(true);
                    LethalMin.Logger.LogInfo($"EaterBehavior despawned due to destroyed parent");
                }
            }
        }
    }
}