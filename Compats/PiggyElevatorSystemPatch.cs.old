using System;
using System.Reflection;
using HarmonyLib;
using LCOffice.Components;
using LethalMin.Pikmin;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Compats
{
    [CompatClass("Piggy.LCOffice")]
    [HarmonyPatch(typeof(ElevatorSystem))]
    public static class PiggyElevatorSystemPatch
    {
        public static NavMeshLink? Link;
        public static GameObject? DebugCubeA, DebugCubeB;
        public static GameObject POnlyZone = null!;
        public static RouteNode ElevateNode = null!;


        [HarmonyPatch("LateUpdate")]
        [HarmonyPostfix]
        public static void LateUpdatePostfix(ElevatorSystem __instance)
        {
            try
            {
                if (POnlyZone != null)
                    POnlyZone.SetActive(LethalMin.BlockEnemiesFromEnteringThirdFloorOffice && ElevatorSystem.elevatorFloor == 2);
                if (ElevateNode != null && ElevateNode.cachedNode != null)
                    ElevateNode.cachedNode.DontDoInRangeCheck = ElevatorSystem.elevatorFloor == 1 && LethalMin.CanPathOutsideWhenInside.InternalValue;

                if (Link == null) { return; }

                Link.gameObject.SetActive(ElevatorSystem.elevatorFloor == 2);
                if (!Link.gameObject.activeSelf) { return; }

                if (StartOfRound.Instance.shipIsLeaving)
                {
                    return;
                }
                // Define offset positions for start and end points
                Vector3 startOffset = new Vector3(0, 0, -1);  // Adjust these values as needed
                Vector3 endOffset = new Vector3(0, 0, 2);     // Adjust these values as needed

                // Calculate world positions for start and end points using offsets
                Vector3 worldStartPoint = Link.transform.TransformPoint(startOffset);
                Vector3 worldEndPoint = Link.transform.TransformPoint(endOffset);

                // Sample the nearest points on the NavMesh
                NavMeshHit hitStart, hitEnd;
                if (NavMesh.SamplePosition(worldStartPoint, out hitStart, 2f, NavMesh.AllAreas) &&
                    NavMesh.SamplePosition(worldEndPoint, out hitEnd, 2f, NavMesh.AllAreas))
                {
                    // Update the Link's start and end points
                    Link.startPoint = Link.transform.InverseTransformPoint(hitStart.position);
                    Link.endPoint = Link.transform.InverseTransformPoint(hitEnd.position);

                    // Update debug cubes
                    if (DebugCubeA != null && DebugCubeB != null)
                    {
                        DebugCubeA.transform.position = hitEnd.position;
                        DebugCubeB.transform.position = hitStart.position;
                    }

                    // Ensure the link is active
                    Link.enabled = true;
                }
                else
                {
                    // If we couldn't find valid NavMesh positions, disable the link
                    Link.enabled = false;
                }
            }
            catch (Exception e)
            {
                LethalMin.Logger.LogError($"Failed to patch ElevatorSystem.LateUpdate due to: {e}");
            }
        }
    }
}
