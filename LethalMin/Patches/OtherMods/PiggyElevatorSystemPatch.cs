using System;
using System.Reflection;
using HarmonyLib;
using LCOffice.Patches;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Patches.OtherMods
{
    [HarmonyPatch(typeof(ElevatorSystem))]
    public static class PiggyElevatorSystemPatch
    {
        public static bool HasCreatedNavMeshOnElevate;
        public static NavMeshLink Link;
        public static GameObject DebugCubeA, DebugCubeB;
        public static PikminOnlyZone pikminOnlyZone;
        public static Vector3 LastPosition;
        private static int GetElevatorFloorValue()
        {
            try
            {
                Type elevatorSystemType = typeof(ElevatorSystem);
                FieldInfo elevatorFloorField = elevatorSystemType.GetField("elevatorFloor", BindingFlags.Public | BindingFlags.Static);

                if (elevatorFloorField != null)
                {
                    object elevatorFloor = elevatorFloorField.GetValue(null);
                    PropertyInfo valueProperty = elevatorFloor.GetType().GetProperty("Value");

                    if (valueProperty != null)
                    {
                        return (int)valueProperty.GetValue(elevatorFloor);
                    }
                }

                LethalMin.Logger.LogWarning("Cannot access elevator floor value!");
            }
            catch (Exception ex)
            {
                LethalMin.Logger.LogError($"Error accessing elevator floor value: {ex.Message}");
            }

            // Return a default value if the property couldn't be accessed
            return 0;
        }

        [HarmonyPatch("LateUpdate")]
        [HarmonyPostfix]
        public static void LateUpdatePostfix(ElevatorSystem __instance)
        {
            if (!LethalMin.GenNavMehsOnElevate) { return; }
            if (!__instance.IsServer) { return; }
            pikminOnlyZone.enabled = GetElevatorFloorValue() != 0 && LethalMin.RasistElevator;

            //Only Update when the elevator is moving
            if (HasCreatedNavMeshOnElevate && Vector3.Distance(LastPosition, ElevatorSystem.animator.transform.position) < 0.01f)
            {
                return;
            }
            if (HasCreatedNavMeshOnElevate && ElevatorSystem.isElevatorClosed)
            {
                //Entites Cannot Leave or Exit a closed elevator
                Link.enabled = false;
                return;
            }
            if (HasCreatedNavMeshOnElevate && Link != null)
            {
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

                LastPosition = ElevatorSystem.animator.transform.position;
            }
        }

        [HarmonyPatch("Setup")]
        [HarmonyPostfix]
        public static void SetupPostfix(ElevatorSystem __instance)
        {
            if (!LethalMin.GenNavMehsOnElevate) { return; }
            if (!HasCreatedNavMeshOnElevate)
            {
                LethalMin.Logger.LogInfo("Creating NavMesh Cube on elevator...");

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"AIelevatorFloor (4 Picles)";
                cube.transform.SetParent(ElevatorSystem.animator.transform);
                // cube.transform.position = new Vector3(1.84899998f,0.119999997f,2.15199995f);
                // cube.transform.localScale = new Vector3(4.64883041f,0.454273105f,4.64883041f);
                cube.transform.localPosition = new Vector3(1.84899998f, 0.119999997f, 2.15199995f);
                cube.transform.localScale = new Vector3(5.2982769f, 0.454273105f, 5.56672716f);
                cube.transform.localRotation = Quaternion.Euler(0, 0, 0);

                LethalMin.Logger.LogInfo("Creating NavMeshsurface on cube...");

                // Add NavMeshSurface component
                NavMeshSurface surface = cube.AddComponent<NavMeshSurface>();

                // Configure NavMeshSurface settings
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
                //surface.layerMask = LayerMask.GetMask("Default"); // Adjust this to match your layer setup
                surface.overrideVoxelSize = true;
                surface.voxelSize = 0.1f;
                surface.overrideTileSize = true;
                surface.tileSize = 16;

                LethalMin.Logger.LogInfo("Bakeing NavMeshsurface on cube...");

                // Bake the NavMeshSurface
                surface.BuildNavMesh();

                //Remove the colider to prevent player bull shiz
                GameObject.Destroy(cube.GetComponent<Collider>());

                LethalMin.Logger.LogInfo("Adding link...");

                GameObject link = new GameObject();
                link.name = $"AIelvatorlink (4 Picles)";

                GameObject cubeA = GameObject.CreatePrimitive(PrimitiveType.Cube);
                GameObject cubeB = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubeA.transform.SetParent(link.transform);
                cubeB.transform.SetParent(link.transform);

                link.transform.SetParent(ElevatorSystem.animator.transform);
                link.transform.localPosition = new Vector3(4.37f, 0.35f, 2.25f);
                link.transform.localRotation = Quaternion.Euler(0, 90, 0);

                NavMeshLink Sasueage = link.AddComponent<NavMeshLink>();

                Sasueage.width = 3f;
                Sasueage.startPoint = new Vector3(0, 0, -1);
                Sasueage.endPoint = new Vector3(0, 0, 2);

                Link = Sasueage;
                DebugCubeA = cubeA;
                DebugCubeB = cubeB;

                //Create 2 cubes parented to the link to visualize the end and start points
                GameObject.Destroy(cubeA.GetComponent<Collider>());
                GameObject.Destroy(cubeB.GetComponent<Collider>());

                //Debugging purposes: colorize the cubes and add a material
                //Renderer renderer = cube.GetComponent<Renderer>();
                //renderer.material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
                // cubeA.GetComponent<Renderer>().material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
                // cubeB.GetComponent<Renderer>().material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
                // cubeA.GetComponent<Renderer>().material.color = Color.red;
                // cubeB.GetComponent<Renderer>().material.color = Color.blue;
                // cubeA.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                // cubeB.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);


                GameObject Zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Zone.name = $"ELevator Zone (4 Picles)";
                Zone.transform.SetParent(ElevatorSystem.animator.transform);
                // Zone.transform.position = new Vector3(1.84899998f,0.119999997f,2.15199995f);
                // Zone.transform.localScale = new Vector3(4.64883041f,0.454273105f,4.64883041f);
                Zone.transform.localPosition = new Vector3(1.8964f, 1.82f, 2.1831f);
                Zone.transform.localScale = new Vector3(4.298542f, 2.8306f, 4.338413f);
                Zone.transform.localRotation = Quaternion.Euler(0, 0, 0);
                Zone.GetComponent<Collider>().isTrigger = true;
                if (__instance.IsServer)
                    pikminOnlyZone = Zone.AddComponent<PikminOnlyZone>();

                LethalMin.Logger.LogInfo("Pikmin can now use the LC_Office Elevator!!!");
                HasCreatedNavMeshOnElevate = true;
            }
        }
    }
}
