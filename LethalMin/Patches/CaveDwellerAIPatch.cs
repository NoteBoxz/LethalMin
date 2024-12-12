using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(CaveDwellerAI))]
    internal class CaveDwellerAIPatch
    {
        private static PikminAttackable pikminAttackable = new PikminAttackable
        {
            PikminGrabPath = "MeshContainer/AnimContainer/RagdollPoint",
            AttackInAnyState = false,
            AttackRange = 4f,
            CheckAtGrabPos = false,
            AttackStates = new int[] { 3 }
        };
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        public static void UpdateAttacker(CrawlerAI __instance)
        {
            if (__instance.isEnemyDead || __instance.inSpecialAnimation)
            {
                return;
            }
            // Set the PikminAttackable component's values
            pikminAttackable.MaxPikminEatCount = LethalMin.ManeaterEatLimmit;
            pikminAttackable.AttackBuffer = LethalMin.ManeaterEatBuffer;
            pikminAttackable.HarmfulToPikmin = LethalMin.LethalManEater;

            PikminAttacker pikminAttacker = __instance.GetComponentInChildren<PikminAttacker>();

            // If the object does not have a PikminAttacker component, create one
            if (pikminAttacker == null)
            {
                GameObject NodeInstance = GameObject.Instantiate(LethalMin.PikminAttackerNode, __instance.transform);
                pikminAttacker = NodeInstance.GetComponent<PikminAttacker>();
                pikminAttackable = pikminAttacker.SABOBJ = pikminAttackable;
                if (__instance.IsServer)
                {
                    pikminAttacker.NetworkObject.Spawn();
                    NodeInstance.transform.SetParent(__instance.transform);
                }
                if (LethalMin.DebugMode)
                {
                    LethalMin.Logger.LogInfo("PikminAttacker component created on " + __instance.enemyType.enemyName);
                }
            }

            // Call the AttackNearby
            pikminAttacker.AttackNearbyPikmin(__instance);
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start(CaveDwellerAI __instance)
        {
            if (LethalMin.CalmableManeater)
            {
                GameObject eaterinstance = GameObject.Instantiate(LethalMin.EaterBehavior, __instance.transform.position, Quaternion.identity);
                if (__instance.IsServer)
                {
                    eaterinstance.GetComponent<NetworkObject>().Spawn();
                    eaterinstance.transform.SetParent(__instance.transform);
                }
                eaterinstance.GetComponent<EaterBehavior>().__instance = __instance;

                CaveDwellerPhysicsProp prop = __instance.propScript;
                GameObject PikminObjectPrefab = LethalMin.PikminObjectPrefab;
                GameObject PikminObjectPrefabInstance = UnityEngine.Object.Instantiate(PikminObjectPrefab, prop.transform.position, Quaternion.identity);
                NetworkObject networkObject = PikminObjectPrefabInstance.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Spawn();
                    PikminItem pikminItem = PikminObjectPrefabInstance.GetComponent<PikminItem>();
                    //pikminItem.Initialize(grabbableObject);
                    pikminItem.ManEater = __instance;
                    pikminItem.SetRootServerRpc(new NetworkObjectReference(prop.NetworkObject));
                    PikminObjectPrefabInstance.name = prop.name + "(PikminNode)";
                    PikminObjectPrefabInstance.transform.position = prop.transform.position;
                    PikminObjectPrefabInstance.transform.SetParent(prop.transform);
                }
                else
                {
                    LethalMin.Logger.LogError($"NetworkObject component not found on PikminItemNode for {prop.name}");
                }
            }
        }

        [HarmonyPatch("DoBabyAIInterval")]
        [HarmonyPrefix]
        public static void DoBabyAIInterval(CaveDwellerAI __instance)
        {

            if (__instance.holdingBaby || __instance.rolledOver || __instance.babyCrying || __instance.babyRunning)
            {
                return;
            }

            __instance.GetComponentInChildren<EaterBehavior>().IntervalledUpdate();

        }
        [HarmonyPatch("StartTransformationAnim")]
        [HarmonyPrefix]
        public static void StartTransformationAnim(CaveDwellerAI __instance)
        {
            __instance.GetComponentInChildren<PikminItem>().RemoveAllPikminAndUnparent(new Vector3(5, 5, 5));
            if (__instance.IsServer)
            {
                __instance.GetComponentInChildren<PikminItem>().NetworkObject.Despawn(true);
            }
        }
        [HarmonyPatch("ScareBabyClientRpc")]
        [HarmonyPrefix]
        public static void ScareBabyClientRpc(CaveDwellerAI __instance)
        {
            if (__instance.IsServer)
                __instance.GetComponentInChildren<PikminItem>().RemoveAllPikminAndUnparent(new Vector3(5, 5, 5));
        }
        [HarmonyPatch("ScareBaby")]
        [HarmonyPrefix]
        public static void ScareBaby(CaveDwellerAI __instance)
        {
        }
    }
}