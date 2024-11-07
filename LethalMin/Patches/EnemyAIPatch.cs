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
        private static ulong currentEnemy = 9999999;
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
            if (HPDict.ContainsKey(__instance))
            {
                HPDict[__instance] = __instance.enemyHP;

            }
        }

        public static void CreateItemNodeOnBody(EnemyAI __instance)
        {
            if (LethalMin.IsDependencyLoaded("Entity378.sellbodies")) return;
            if (currentEnemy == __instance.NetworkObject.NetworkObjectId) return;
            if (__instance.GetComponentInChildren<PlayerControllerB>()) return;

            currentEnemy = __instance.NetworkObject.NetworkObjectId;

            LethalMin.Logger.LogInfo("Creating item node on enemy body " + __instance.gameObject.name);

            Item Iprops = new Item();
            Iprops.restingRotation = __instance.transform.rotation.eulerAngles;

            // Calculate the weight based on the largest mesh
            // float largestVolume = 0f;
            // Renderer[] renderers = __instance.GetComponentsInChildren<Renderer>();
            // foreach (Renderer renderer in renderers)
            // {
            //     Mesh mesh = null;
            //     if (renderer is MeshRenderer)
            //     {
            //         MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            //         if (meshFilter != null)
            //         {
            //             mesh = meshFilter.sharedMesh;
            //         }
            //     }
            //     else if (renderer is SkinnedMeshRenderer)
            //     {
            //         SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)renderer;
            //         mesh = skinnedMeshRenderer.sharedMesh;
            //     }

            //     if (mesh != null)
            //     {
            //         float volume = CalculateMeshVolume(mesh);
            //         if (volume > largestVolume)
            //         {
            //             largestVolume = volume;
            //         }
            //     }
            // }

            // Normalize the volume to a weight between 1 and 5
            // LethalMin.Logger.LogInfo($"Largest volume for {__instance.gameObject.name}: {largestVolume}");
            // float normalizedWeight = Mathf.Clamp(largestVolume / 330f, 1f, 5f);
            // Iprops.weight = normalizedWeight;

            // LethalMin.Logger.LogInfo($"Calculated weight for {__instance.gameObject.name}: {normalizedWeight}");

            if (__instance.enemyType.canDie && HPDict.ContainsKey(__instance) && HPDict[__instance] > 0f)
            {
                Iprops.weight = HPDict[__instance] * 0.4f;
            }
            else
            {
                Iprops.weight = Mathf.Clamp((float)(__instance.enemyType.PowerLevel * 0.6), 1f, 5f);
            }

            PhysicsProp prop = __instance.gameObject.AddComponent<PhysicsProp>();
            prop.itemProperties = Iprops;
            prop.grabbable = false;

            if (__instance.IsServer)
                CreatePikminItemForBody(prop);
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