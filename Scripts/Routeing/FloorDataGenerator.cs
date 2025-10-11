using System.Collections.Generic;
using DunGen;
using itolib.PlayZone;
using LCOffice.Components;
using LethalMin.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Routeing;

public static class FloorDataGenerator
{
    enum DungeonType
    {
        None,
        PlayDungen,
        PiggyDungen,
        VanillaDungen
    }

    public static Dictionary<Dungeon, List<FloorData>> DungeonFloorDataCache = new Dictionary<Dungeon, List<FloorData>>();

    // Called when level loads or when new dungeon generated
    public static List<FloorData> GenerateFloorDataInterior(Dungeon dungeon)
    {
        List<FloorData> floorDataList = new List<FloorData>();
        DungeonType dungeonType = DetermineDungeonType(dungeon, out Object? elevatorObject);
        if (dungeonType == DungeonType.None)
            return floorDataList;

        if (DungeonFloorDataCache.ContainsKey(dungeon))
        {
            return DungeonFloorDataCache[dungeon];
        }

        switch (dungeonType)
        {
            case DungeonType.VanillaDungen:
                if (elevatorObject is MineshaftElevatorController elevator)
                {
                    floorDataList = GetVanillaFloorData(elevator);
                    DungeonFloorDataCache.Add(dungeon, floorDataList);
                }
                else
                {
                    LethalMin.Logger.LogError("Elevator object is not of type MineshaftElevatorController.");
                }
                break;
            case DungeonType.PlayDungen:

                break;
            case DungeonType.PiggyDungen:

                break;
        }

        return floorDataList;
    }

    /// <summary>
    /// Gets the floor data from the vanilla mineshaft interior.
    /// </summary>
    public static List<FloorData> GetVanillaFloorData(MineshaftElevatorController elevator)
    {
        List<FloorData> data = new List<FloorData>();
        GameObject CustomBounds = GameObject.CreatePrimitive(PrimitiveType.Cube);
        CustomBounds.GetComponent<Collider>().isTrigger = true;
        CustomBounds.transform.SetParent(elevator.GetComponentInChildren<PlayerPhysicsRegion>().transform.parent);
        CustomBounds.transform.localPosition = new Vector3(0.0001f, 0.7978f, 0f);
        CustomBounds.transform.localScale = new Vector3(2f, 4f, 2f);
        CustomBounds.GetComponent<Renderer>().enabled = false;
        CustomBounds.AddComponent<DirectlyPathZone>();
        //CustomBounds.GetComponent<Renderer>().material = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Materials/MapDotA.mat");
        CustomBounds.name = "Pikmin Elevator Bounds";

        RouteNode MainNode = null!;
        List<RouteNode> FireNodes = new List<RouteNode>();
        foreach (RouteNode node in PikminRouteManager.Instance.EntranceNodes)
        {
            if (node.entrancePoint != null && !node.entrancePoint.isEntranceToBuilding)
            {
                if (node.entrancePoint.entranceId == 0)
                    MainNode = node;
                else
                    FireNodes.Add(node);
            }
        }


        RouteNode ElevatorNode = new RouteNode
        (
            name: elevator.name,
            point: elevator.transform,
            check: CustomBounds.GetComponent<Collider>()
        );

        ElevatorNode.CheckBuffer = 0.25f;
        ElevatorNode.CheckDistance = 1f;

        FloorData F1 = new FloorData();
        F1.Exits.Add(MainNode);
        F1.FloorRoot = RoundManager.FindMainEntrancePosition();
        F1.Elevators.Add(ElevatorNode);
        F1.FloorTitle = "(Floor1) Entrance";
        data.Add(F1);

        FloorData F2 = new FloorData();
        F2.Exits.AddRange(FireNodes);
        F2.FloorRoot = elevator.elevatorBottomPoint.position;
        F2.Elevators.Add(ElevatorNode);
        F2.FloorTitle = "(Floor2) Mineshaft";
        data.Add(F2);

        LethalMin.Logger.LogInfo("Registered Vanilla Minshaft Floors");

        return data;
    }

    // /// <summary>
    // /// Gets the floor data from the Piggy LC-Office mod.
    // /// </summary>
    // public static void GetPiggyFloorData()
    // {
    //     List<RouteNode> FireExits = FindFireExitRouteNodes();
    //     LCOffice.Components.ElevatorSystem ElevatorSystem = Object.FindObjectOfType<LCOffice.Components.ElevatorSystem>();
    //     PlayerPhysicsRegion ElevatorRegion = ElevatorSystem.animator.GetComponentInChildren<PlayerPhysicsRegion>();
    //     Scene currentScene = SceneManager.GetSceneByName(RoundManager.Instance.currentLevel.sceneName);

    //     GameObject CreateDebugCube(Vector3 LocalPos)
    //     {
    //         //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //         GameObject cube = new GameObject("Floor Ref Pos");
    //         SceneManager.MoveGameObjectToScene(cube, currentScene);
    //         cube.transform.SetParent(ElevatorSystem.animator.transform.parent);
    //         cube.transform.localPosition = LocalPos;
    //         //cube.GetComponent<Renderer>().material = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Materials/MapDotA.mat");
    //         return cube;
    //     }

    //     RouteNode MainNode = new RouteNode(
    //         "Main",
    //         RoundManager.FindMainEntranceScript(true),
    //         0.45f
    //     );

    //     RouteNode ElevatorNode = new RouteNode(
    //         "Elevator",
    //         ElevatorSystem.animator.transform,
    //         -1,
    //         ElevatorRegion.GetComponent<Collider>()
    //     );
    //     if (!ElevatorRegion.gameObject.TryGetComponent(out DirectlyPathZone zone))
    //     {
    //         zone = ElevatorRegion.gameObject.AddComponent<DirectlyPathZone>();
    //     }

    //     ElevatorNode.CheckBuffer = 0.25f;
    //     ElevatorNode.GetNavPos = true;
    //     PiggyElevatorSystemPatch.ElevateNode = ElevatorNode;

    //     FloorData F1 = new FloorData();
    //     GameObject cubeA = CreateDebugCube(new Vector3(-2.32f, -11.41f, 1.01f));
    //     F1.FloorRoot = cubeA.transform.position;
    //     F1.Elevators.Add(ElevatorNode);
    //     F1.FloorTitle = "(Floor1) Basement";
    //     CurrentFloorData.Add(F1);

    //     FloorData F2 = new FloorData();
    //     GameObject cubeB = CreateDebugCube(new Vector3(-2.32f, 22.09f, 1.01f));
    //     F2.FloorRoot = cubeB.transform.position;
    //     F2.Elevators.Add(ElevatorNode);
    //     F2.MainExits.Add(MainNode);
    //     F2.FloorTitle = "(Floor2) Lobby";
    //     CurrentFloorData.Add(F2);
    //     DefultFloorData = F2;

    //     FloorData F3 = new FloorData();
    //     GameObject cubeC = CreateDebugCube(new Vector3(-2.32f, 61.44f, 1.01f));
    //     F3.FloorRoot = cubeC.transform.position;
    //     F3.Elevators.Add(ElevatorNode);
    //     F3.FloorTitle = "(Floor3) Upstairs Basement";
    //     CurrentFloorData.Add(F3);

    //     GameObject PonlyZone = PikUtils.CreateDebugCube(cubeC.transform.position + new Vector3(0, 1.5f, 0));
    //     PonlyZone.transform.localScale = new Vector3(4.3f, 4.25f, 7f);
    //     PonlyZone.GetComponent<Collider>().isTrigger = true;
    //     PonlyZone.GetComponent<Collider>().enabled = true;
    //     PonlyZone.AddComponent<PikminOnlyZone>();
    //     PonlyZone.GetComponent<Renderer>().enabled = false;
    //     PonlyZone.name = "LC-Office Pikmin Only Zone";
    //     SceneManager.MoveGameObjectToScene(PonlyZone, currentScene);
    //     PiggyElevatorSystemPatch.POnlyZone = PonlyZone;

    //     if (LethalMin.AddNavLinkToThridFloorOffice)
    //     {
    //         GameObject link = new GameObject();
    //         link.name = $"LethalMinFloor3Link";
    //         SceneManager.MoveGameObjectToScene(link, currentScene);

    //         GameObject LcubeA = new GameObject(); //PikUtils.CreateDebugCube(new Vector3(0, 0, 0));
    //         GameObject LcubeB = new GameObject(); //PikUtils.CreateDebugCube(new Vector3(0, 0, 0));
    //         LcubeA.name = "LethalMinLinkCubeA";
    //         LcubeB.name = "LethalMinLinkCubeB";
    //         SceneManager.MoveGameObjectToScene(LcubeA, currentScene);
    //         SceneManager.MoveGameObjectToScene(LcubeB, currentScene);
    //         LcubeA.transform.SetParent(link.transform);
    //         LcubeB.transform.SetParent(link.transform);

    //         link.transform.SetParent(ElevatorSystem.animator.transform);
    //         link.transform.localPosition = new Vector3(2.19f, -3.62f, 0.015f);
    //         link.transform.localRotation = Quaternion.Euler(0, 90, 0);

    //         NavMeshLink Sasueage = link.AddComponent<NavMeshLink>();

    //         Sasueage.width = 3f;
    //         Sasueage.startPoint = new Vector3(0, 0, -1);
    //         Sasueage.endPoint = new Vector3(0, 0, 2);

    //         PiggyElevatorSystemPatch.Link = Sasueage;
    //         PiggyElevatorSystemPatch.DebugCubeA = LcubeA;
    //         PiggyElevatorSystemPatch.DebugCubeB = LcubeB;
    //     }

    //     FloorData DetermineFloor(GameObject obj)
    //     {
    //         Vector3 objPosition = obj.transform.position;
    //         return CurrentFloorData.OrderBy(floor =>
    //             Mathf.Abs(objPosition.y - floor.FloorRoot.y))
    //             .FirstOrDefault();
    //     }

    //     foreach (RouteNode exit in FireExits)
    //     {
    //         if (exit.Entrance == null)
    //         {
    //             LethalMin.Logger.LogWarning($"({exit.NodeName}) Entrance is null, skipping exit registration.");
    //             continue;
    //         }
    //         if (exit.Entrance.exitPoint == null)
    //         {
    //             exit.Entrance.FindExitPoint();
    //         }
    //         GameObject? Point = exit.Entrance.exitPoint?.gameObject;
    //         if (!exit.Entrance.gotExitPoint)
    //         {
    //             Point = FindEntranceExitPoint(exit.Entrance);
    //         }
    //         if (Point == null)
    //         {
    //             LethalMin.Logger.LogWarning($"({exit.NodeName}) Exit point is null, skipping exit registration.");
    //             continue;
    //         }

    //         FloorData floor = DetermineFloor(Point);
    //         LethalMin.Logger.LogDebug($"({exit.NodeName}) Floor: {floor.FloorTitle} Position: {Point.transform.position}");
    //         CurrentFloorData[CurrentFloorData.IndexOf(floor)].FireExits.Add(exit);
    //     }

    //     LethalMin.Logger.LogInfo("Registered LC-Office Floors");
    // }

    // /// <summary>
    // /// Gets the floor data from the PlayZone mod.
    // /// </summary>
    // public static void GetPlayFloorData()
    // {
    //     RouteNode MainNode = new RouteNode(
    //         "Main",
    //         RoundManager.FindMainEntranceScript(true),
    //         0.45f
    //     );

    //     itolib.PlayZone.PlayZoneElevator elevator = Object.FindObjectOfType<itolib.PlayZone.PlayZoneElevator>();

    //     if (elevator == null || elevator.elevatorAnimator == null)
    //     {
    //         LethalMin.Logger.LogError("PlayZone Elevator not found or elevatorAnimator is null.");
    //         return;
    //     }

    //     RouteNode ElevatorNode = new RouteNode(
    //         "Elevator",
    //         elevator.GetComponentInChildren<PlayerPhysicsRegion>().transform,
    //         -1,
    //         elevator.GetComponentInChildren<PlayerPhysicsRegion>().GetComponent<Collider>()
    //     );

    //     ElevatorNode.CheckBuffer = 0.25f;
    //     MineshaftElevatorControllerPatch.node = ElevatorNode;

    //     FloorData F2 = new FloorData();
    //     F2.MainExits.Add(MainNode);
    //     F2.FireExits.AddRange(FindFireExitRouteNodes());
    //     F2.FloorRoot = RoundManager.FindMainEntrancePosition();
    //     F2.Elevators.Add(ElevatorNode);
    //     F2.FloorTitle = "(Floor2) Ground";
    //     CurrentFloorData.Add(F2);

    //     DefultFloorData = F2;

    //     FloorData F1 = new FloorData();
    //     F1.FireExits.AddRange(FindFireExitRouteNodes());
    //     F1.FloorRoot = elevator.doorAnimatorLower?.transform.position ?? elevator.transform.position;
    //     F1.Elevators.Add(ElevatorNode);
    //     F1.FloorTitle = "(Floor1) Fun";
    //     CurrentFloorData.Add(F1);

    //     LethalMin.Logger.LogInfo("Registered PlayZone Floors");
    // }

    private static DungeonType DetermineDungeonType(Dungeon dungeon, out Object? elevatorObject)
    {
        Object? foundElevator = null;

        PlayZoneElevator? FindPlayElevator()
        {
            if (PikminRouteManager.Instance.Dungeons.Count == 1)
            {
                return Object.FindObjectOfType<PlayZoneElevator>();
            }

            // Multi Dungeon check
            PlayZoneElevator[] elevators = Object.FindObjectsOfType<PlayZoneElevator>();
            if (elevators == null || elevators.Length == 0)
                return null;
            
            foreach (var elevator in elevators)
            {
                Dungeon ClosestDungeon = PikminRouteManager.GetClosestDungeon(elevator.transform.position);
                LethalMin.Logger.LogDebug($"Closest Dungeon to {elevator.name} is {ClosestDungeon?.name}");
                if (ClosestDungeon != null && ClosestDungeon == dungeon)
                    return elevator;
            }
            return null;
        }

        ElevatorController? FindPiggyElevator()
        {
            if (PikminRouteManager.Instance.Dungeons.Count == 1)
            {
                return Object.FindObjectOfType<ElevatorController>();
            }

            // Multi Dungeon check
            ElevatorController[] elevators = Object.FindObjectsOfType<ElevatorController>();
            if (elevators == null || elevators.Length == 0)
                return null;
            
            foreach (var elevator in elevators)
            {
                Dungeon ClosestDungeon = PikminRouteManager.GetClosestDungeon(elevator.transform.position);
                LethalMin.Logger.LogDebug($"Closest Dungeon to {elevator.name} is {ClosestDungeon?.name}");
                if (ClosestDungeon != null && ClosestDungeon == dungeon)
                    return elevator;
            }
            return null;
        }

        MineshaftElevatorController? FindVanillaElevator()
        {
            if (PikminRouteManager.Instance.Dungeons.Count == 1)
            {
                return Object.FindObjectOfType<MineshaftElevatorController>();
            }

            // Multi Dungeon check
            MineshaftElevatorController[] elevators = Object.FindObjectsOfType<MineshaftElevatorController>();
            if (elevators == null || elevators.Length == 0)
                return null;
            
            foreach (var elevator in elevators)
            {
                Dungeon ClosestDungeon = PikminRouteManager.GetClosestDungeon(elevator.transform.position);
                LethalMin.Logger.LogDebug($"Closest Dungeon to {elevator.name} is {ClosestDungeon?.name}");
                if (ClosestDungeon != null && ClosestDungeon == dungeon)
                    return elevator;
            }
            return null;
        }

        MineshaftElevatorController? vanillaElevator = FindVanillaElevator();
        if (vanillaElevator != null)
        {
            LethalMin.Logger.LogInfo($"Vanilla Dungen detected, getting floor data.");
            foundElevator = vanillaElevator;
            elevatorObject = foundElevator;
            return DungeonType.VanillaDungen;
        }

        if (LethalMin.IsDependencyLoaded("Piggy.LCOffice"))
        {
            ElevatorController? piggyElevator = FindPiggyElevator();
            if (piggyElevator != null)
            {
                LethalMin.Logger.LogInfo($"Piggy.LCOffice detected, getting floor data.");
                foundElevator = piggyElevator;
                elevatorObject = foundElevator;
                return DungeonType.PiggyDungen;
            }
        }

        if (LethalMin.IsDependencyLoaded("LethalMatt.PlayZone"))
        {
            PlayZoneElevator? playElevator = FindPlayElevator();
            if (playElevator != null)
            {
                LethalMin.Logger.LogInfo($"LethalMatt.PlayZone detected, getting floor data.");
                foundElevator = playElevator;
                elevatorObject = foundElevator;
                return DungeonType.PlayDungen;
            }
        }

        LethalMin.Logger.LogDebug($"did not find any flor data");
        elevatorObject = null;
        return DungeonType.None;
    }
}