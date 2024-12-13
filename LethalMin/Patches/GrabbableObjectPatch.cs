using HarmonyLib;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using System;
using LethalMin.Library;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    internal class GrabbableObjectPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void StartPostfix(GrabbableObject __instance)
        {
            if (__instance.IsSpawned)
            {
                CreatePikminItem(__instance);
            }
            else
            {
                // Add a component to wait for the object to be spawned
                __instance.gameObject.AddComponent<WaitForSpawn>().Initialize(__instance);
            }
            if (LethalMin.IsDependencyLoaded("Entity378.sellbodies"))
            {
                DoSellBodiesCheck(__instance);
            }
        }

        public static void DoSellBodiesCheck(GrabbableObject __instance)
        {
            if (CleaningCompany.Plugin.instance.BodySpawns.Values.ToList().Contains(__instance.itemProperties))
            {
                if (__instance.IsSpawned)
                {
                    LethalMin.Logger.LogInfo("SellBodiesFixed body item detected, adding PikminItem");
                    CreatePikminItem(__instance, true).SetIsBodyClientRpc();
                }
                else
                {
                    LethalMin.Logger.LogInfo("SellBodiesFixed body item detected, adding WaitForSpawn component");
                    // Add a component to wait for the object to be spawned
                    __instance.gameObject.AddComponent<WaitForSpawn>().Initialize(__instance, true, true);
                }
            }
        }


        public static PikminItem CreatePikminItem(GrabbableObject grabbableObject, bool overrideGrabbableCheck = false)
        {
            if (grabbableObject == null)
            {
                LethalMin.Logger.LogWarning("GrabbableObject is  When creating PikminItem");
                return ;
            }
            if (!LethalMin.AllItemsToP && grabbableObject.IsServer && grabbableObject.grabbableToEnemies
            || LethalMin.AllItemsToP && grabbableObject.IsServer && grabbableObject.grabbable
            || overrideGrabbableCheck)
            {
                if (grabbableObject.GetComponent<CaveDwellerPhysicsProp>() != null)
                    return ;
                PikminItem[] Pims = GameObject.FindObjectsOfType<PikminItem>();
                foreach (var item in Pims)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    if (item.Root == grabbableObject)
                    {
                        LethalMin.Logger.LogWarning($"{grabbableObject.itemProperties.name} already has a pikmin node!");
                        return ;
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
                    if (grabbableObject.NetworkObject == null)
                    {
                        LethalMin.Logger.LogWarning($"NetworkObject component not found on GrabbableObject for {grabbableObject.name}");
                        return ;
                    }
                    pikminItem.SetRootServerRpc(new NetworkObjectReference(grabbableObject.NetworkObject));
                    PikminObjectPrefabInstance.name = grabbableObject.name + "(PikminNode)";
                    PikminObjectPrefabInstance.transform.position = grabbableObject.transform.position;
                    PikminObjectPrefabInstance.transform.SetParent(grabbableObject.transform);
                    return pikminItem;
                }
                else
                {
                    LethalMin.Logger.LogError($"NetworkObject component not found on PikminItemNode for {grabbableObject.name}");
                }
            }
            return ;
        }

        public static NetworkObject GetPhysicsRegionOfDroppedObject(GrabbableObject grabbableObject, out Vector3 hitPoint)
        {
            Transform transform = null;
            RaycastHit hitInfo;

            // Use the grabbable object's position instead of the player's
            Ray ray = new Ray(grabbableObject.transform.position, -Vector3.up);
            if (Physics.Raycast(ray, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawRay(grabbableObject.transform.position, -Vector3.up * 80f, Color.blue, 2f);
                transform = hitInfo.collider.gameObject.transform;
            }

            if (transform != null)
            {
                PlayerPhysicsRegion componentInChildren = transform.GetComponentInChildren<PlayerPhysicsRegion>();
                if (componentInChildren != null && componentInChildren.allowDroppingItems && componentInChildren.itemDropCollider.ClosestPoint(hitInfo.point) == hitInfo.point)
                {
                    NetworkObject parentNetworkObject = componentInChildren.parentNetworkObject;
                    if (parentNetworkObject != null)
                    {
                        Vector3 addPositionOffsetToItems = componentInChildren.addPositionOffsetToItems;
                        hitPoint = componentInChildren.physicsTransform.InverseTransformPoint(hitInfo.point + Vector3.up * 0.04f + grabbableObject.itemProperties.verticalOffset * Vector3.up + addPositionOffsetToItems);
                        return parentNetworkObject;
                    }
                    LethalMin.Logger.LogError("Error: physics region transform does not have network object: " + transform.gameObject.name);
                }
            }

            hitPoint = Vector3.zero;
            return null;
        }
    }

    // New class to wait for the object to be spawned
    public class WaitForSpawn : MonoBehaviour
    {
        private GrabbableObject grabbableObject;
        private bool or;
        public bool SetDeadBody;

        public void Initialize(GrabbableObject obj, bool overrideGC = false, bool SDB = false)
        {
            grabbableObject = obj;
            or = overrideGC;
            SetDeadBody = SDB;
        }

        private void Update()
        {
            if (grabbableObject != null && grabbableObject.IsSpawned)
            {
                if (SetDeadBody)
                    GrabbableObjectPatch.CreatePikminItem(grabbableObject, true).SetIsBodyClientRpc();
                else
                    GrabbableObjectPatch.CreatePikminItem(grabbableObject, or);
                Destroy(this); // Remove this component once we've created the PikminItem
            }
        }
    }
}