using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Diagnostics;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        public static Dictionary<EnemyAI, int> HPDict = new Dictionary<EnemyAI, int>();

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StarDamage(EnemyAI __instance)
        {
            if (__instance.enemyType != null && __instance.enemyType != LethalMin.puffminEnemyType &&
                __instance.enemyType != LethalMin.pikminEnemyType && __instance.enemyType.canDie
            )
            {
                __instance.gameObject.AddComponent<PikminDamager>().RootScript = __instance;
            }
            if (!HPDict.ContainsKey(__instance))
            {
                HPDict[__instance] = __instance.enemyHP;
            }
        }

        private static float CalculateMeshVolume(Mesh mesh)
        {
            Vector3 size = mesh.bounds.size;
            return size.x * size.y * size.z;
        }


        public static PikminItem CreatePikminItemForBody(GrabbableObject grabbableObject)
        {
            if (grabbableObject.GetComponent<CaveDwellerPhysicsProp>() != null)
                return null!;
            PikminItem[] Pims = GameObject.FindObjectsOfType<PikminItem>();
            foreach (var item in Pims)
            {
                if (item.Root == grabbableObject)
                {
                    LethalMin.Logger.LogWarning($"{grabbableObject.itemProperties.name} already has a pikmin node!");
                    return null!;
                }
            }
            GameObject PikminObjectPrefab = LethalMin.PikminObjectPrefab;
            GameObject PikminObjectPrefabInstance = UnityEngine.Object.Instantiate(PikminObjectPrefab, grabbableObject.transform.position, Quaternion.identity);
            NetworkObject networkObject = PikminObjectPrefabInstance.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                PikminItem pikminItem = PikminObjectPrefabInstance.GetComponent<PikminItem>();
                //pikminItem.Initialize(grabbableObject);
                pikminItem.SetRootServerRpc(new NetworkObjectReference(grabbableObject.NetworkObject));
                PikminObjectPrefabInstance.name = grabbableObject.name + "(PikminNode)";
                PikminObjectPrefabInstance.transform.position = grabbableObject.transform.position;
                PikminObjectPrefabInstance.transform.SetParent(grabbableObject.transform);
                pikminItem.CanBeConvertedIntoSprouts = true;
                pikminItem.UsePikminAsParent = true;
                pikminItem.DontParentToObjects = true;
                LethalMin.Logger.LogInfo($"Created PikminItemNode for {grabbableObject.name}");
                return pikminItem;
            }
            else
            {
                LethalMin.Logger.LogError($"NetworkObject component not found on PikminItemNode for {grabbableObject.name}");
            }
            return null!;
        }
    }
}