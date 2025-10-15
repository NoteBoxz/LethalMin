using ElevatorMod.Patches;
using HarmonyLib;
using LethalMin.HUD;
using LethalMin.Pikmin;
using LethalMin.Utils;
using PiggyVarietyMod.Patches;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalMin.Compats
{
    [CompatClass("kite.ZelevatorCode")]
    [HarmonyPatch(typeof(EndlessElevator))]
    public static class EndlessElevatorPatch
    {
        public static MoonSettings ZelevatorPath = null!;
        public static RouteNode ZelevatorNode = null!;
        public static List<ElevatorPikminData> PikminSaved = new List<ElevatorPikminData>();
        public static GameObject POnlyZone = null!;

        public struct ElevatorPikminData
        {
            public PikminData baseData;
            public long PlayerID;
        }

        [HarmonyPatch(nameof(EndlessElevator.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(EndlessElevator __instance)
        {
            try
            {
                //Add event listensers
                if (__instance.IsServer)
                {
                    __instance.OnDoneGenerate.AddListener(RespawnPikmin);
                }
                __instance.OnStopMove.AddListener(WarpPikminOnElevatorStop);

                //Add Create ZelevatorPath to MoonSettingss
                LethalMin.Logger.LogDebug("Adding Zelevator to MoonSettingss");
                if (ZelevatorPath == null)
                {
                    ZelevatorPath = ScriptableObject.CreateInstance<MoonSettings>();
                    ZelevatorPath.name = "Zelevator Path";
                    ZelevatorPath.Level = StartOfRound.Instance.currentLevel;
                }
                //Update ZelevatorPath
                ZelevatorPath.CheckPathableIndoor = false;
                ZelevatorPath.CheckPathableOutdoor = false;
                ZelevatorPath.IndoorRouteNodes.Clear();
                RouteNode ElevateNode = new RouteNode(
                    "Zelevator",
                    __instance.playerPhysicsRegion_elevator.transform,
                    -1,
                    __instance.playerPhysicsRegion_elevator.GetComponent<Collider>()
                );
                ZelevatorNode = ElevateNode;
                ZelevatorPath.IndoorRouteNodes.Add(ElevateNode);
                ElevateNode.CheckBuffer = 0.25f;
                if (!__instance.playerPhysicsRegion_elevator.TryGetComponent(out DirectlyPathZone zone))
                {
                    zone = __instance.playerPhysicsRegion_elevator.gameObject.AddComponent<DirectlyPathZone>();
                }
                if (!PikminRoute.MoonSettingss.Contains(ZelevatorPath))
                {
                    PikminRoute.MoonSettingss.Add(ZelevatorPath);
                }

                Scene currentScene = SceneManager.GetSceneByName(RoundManager.Instance.currentLevel.sceneName);
                GameObject PonlyZone = PikUtils.CreateDebugCube(new Vector3(-7.2f, 2.5f, -13.8f));
                PonlyZone.transform.localScale = new Vector3(2.5f, 5f, 8.3f);
                PonlyZone.GetComponent<Collider>().isTrigger = true;
                PonlyZone.GetComponent<Collider>().enabled = true;
                PonlyZone.AddComponent<PikminOnlyZone>().warpDistance = 8f;
                PonlyZone.GetComponent<Renderer>().enabled = false;
                PonlyZone.name = "Zelevator Pikmin Only Zone";
                SceneManager.MoveGameObjectToScene(PonlyZone, currentScene);
                POnlyZone = PonlyZone;

                if (LethalMin.AddNavLinkToZeranosShip.InternalValue)
                {
                    GameObject link = new GameObject();
                    link.name = $"LethalMinShipLink";
                    SceneManager.MoveGameObjectToScene(link, currentScene);

                    GameObject LcubeA = new GameObject();//PikUtils.CreateDebugCube(new Vector3(0, 0, 0));
                    GameObject LcubeB = new GameObject();//PikUtils.CreateDebugCube(new Vector3(0, 0, 0));
                    LcubeA.name = "LethalMinShipLinkA";
                    LcubeB.name = "LethalMinShipLinkB";
                    SceneManager.MoveGameObjectToScene(LcubeA, currentScene);
                    SceneManager.MoveGameObjectToScene(LcubeB, currentScene);
                    LcubeA.transform.SetParent(link.transform);
                    LcubeB.transform.SetParent(link.transform);

                    link.transform.localPosition = new Vector3(-8.4092f, 0.259f, -11.0494f);
                    link.transform.localRotation = Quaternion.Euler(0, 90, 0);

                    NavMeshLink Sasueage = link.AddComponent<NavMeshLink>();

                    Sasueage.width = 3f;
                    Sasueage.autoUpdate = true;
                    Sasueage.startPoint = new Vector3(0f, 0.0272f, 0.7f);
                    Sasueage.endPoint = new Vector3(0f, -1.4666f, -3.0053f);
                }

                if (LethalMin.AddNavLinkToZeranosElevator.InternalValue)
                {
                    GameObject link = new GameObject();
                    link.name = $"LethalMinElevatorLink";
                    SceneManager.MoveGameObjectToScene(link, currentScene);

                    GameObject LcubeA = new GameObject();//PikUtils.CreateDebugCube(new Vector3(0, 0, 0));
                    GameObject LcubeB = new GameObject();//PikUtils.CreateDebugCube(new Vector3(0, 0, 0));
                    LcubeA.name = "LethalMinElevatorLinkA";
                    LcubeB.name = "LethalMinElevatorLinkB";
                    SceneManager.MoveGameObjectToScene(LcubeA, currentScene);
                    SceneManager.MoveGameObjectToScene(LcubeB, currentScene);
                    LcubeA.transform.SetParent(link.transform);
                    LcubeB.transform.SetParent(link.transform);

                    link.transform.localPosition = new Vector3(-281.7f, -0.1f, 105.5f);
                    link.transform.localRotation = Quaternion.Euler(0, 90, 0);

                    NavMeshLink Sasueage = link.AddComponent<NavMeshLink>();

                    Sasueage.width = 3f;
                    Sasueage.autoUpdate = true;
                    Sasueage.startPoint = new Vector3(0.0f, 0.0f, 1.5f);
                    Sasueage.endPoint = new Vector3(0.0f, 0.0f, -1.5f);
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError("Failed to add Zelevator to MoonSettingss: " + e);
            }
        }

        [HarmonyPatch(nameof(EndlessElevator.Update))]
        [HarmonyPostfix]
        public static void UpdatePostfix(EndlessElevator __instance)
        {
            if (ZelevatorNode != null && ZelevatorNode.cachedNode != null)
            {
                ZelevatorNode.cachedNode.DontDoInRangeCheck = !StartOfRound.Instance.localPlayerController.isInsideFactory;
            }
            if (POnlyZone != null)
            {
                POnlyZone.SetActive(LethalMin.BlockEnemiesFromEnteringZeranosShip.InternalValue);
            }
        }

        [HarmonyPatch(nameof(EndlessElevator.GenerateNewFloor))]
        [HarmonyPrefix]
        public static void GenerateNewFloorPatch(EndlessElevator __instance)
        {
            if (__instance.firstTimeElevator)
            {
                LethalMin.Logger.LogDebug("First time elevator, not saving Pikmin.");
            }

            if (!__instance.IsServer || __instance.firstTimeElevator) { return; }

            PikminSaved.Clear();

            LethalMin.Logger.LogInfo("Saving Pikmin in elevator.");

            foreach (PikminAI pikmin in PikminManager.instance.PikminAIs)
            {
                if (pikmin == null) { continue; }
                if (pikmin.IsWildPikmin) { continue; }
                LethalMin.Logger.LogInfo("Saving Pikmin: " + pikmin.name);
                ElevatorPikminData data = new ElevatorPikminData
                {
                    baseData = pikmin.GetPikminData(),
                    PlayerID = pikmin.leader == null ? -1 : (long)pikmin.leader.OwnerClientId
                };
                PikminSaved.Add(data);
            }
        }

        public static void RespawnPikmin()
        {
            EndlessElevator __instance = GameObject.FindObjectOfType<EndlessElevator>();
            LethalMin.Logger.LogInfo($"Respawning: {PikminSaved.Count} Pikmin from elevator.");
            foreach (ElevatorPikminData data in PikminSaved)
            {
                LethalMin.Logger.LogInfo("Respawning Pikmin: " + data.baseData.DebugName);
                PikminSpawnProps props = new PikminSpawnProps();
                props.GrowthStage = data.baseData.GrowthStage;
                props.IsOutside = !StartOfRound.Instance.localPlayerController.isInsideFactory;
                if (data.PlayerID != -1)
                    props.PlayerID = (ulong)data.PlayerID;
                props.OverrideDebugID = data.baseData.DebugName;
                props.OverrideBirthDate = data.baseData.BirthDate;
                PikminManager.instance.SpawnPikminOnServer(LethalMin.GetPikminTypeByID(data.baseData.TypeID), __instance.playerPhysicsRegion_elevator.transform.position,
                __instance.playerPhysicsRegion_elevator.transform.rotation, props);
            }
            PikminSaved.Clear();

            PikminManager.instance.SpawnMapPikmin();

            PikminCounter counter = GameObject.FindObjectOfType<PikminCounter>();
            if (counter != null)
            {
                counter.previousExistanceCount = -1;
                counter.previousPikminCount = -1;
                counter.previousSquadCount = -1;
            }
        }

        public static void WarpPikminOnElevatorStop()
        {
            LethalMin.Logger.LogMessage("Warping Pikmin on elevator stop.");
            EndlessElevator __instance = GameObject.FindObjectOfType<EndlessElevator>();

            foreach (PikminAI ai in PikminManager.instance.PikminAIs)
            {
                if (ai == null) { continue; }
                if (ai.IsWildPikmin) { continue; }
                if (ai.leader == null)
                {
                    ai.SetToIdle();
                }
                if (ai.IsOwner)
                {
                    ai.agent.Warp(__instance.playerPhysicsRegion_elevator.transform.position);
                }
                Leader? leader = LethalMin.GetLeaderViaID(ai.OwnerClientId);
                if (leader == null)
                {
                    ai.isOutside = !StartOfRound.Instance.allPlayerScripts[0].isInsideFactory;
                }
                else
                {
                    ai.isOutside = !leader.Controller.isInsideFactory;
                }
                ai.transform2.TeleportOnLocalClient(__instance.playerPhysicsRegion_elevator.transform.position);
            }
        }
    }
}