using System.Collections.Generic;
using System.Linq;
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
    public static Dictionary<RouteNode, Dungeon> EntranceDungeonCache = new Dictionary<RouteNode, Dungeon>();

    // Called when level loads or when new dungeon generated
    public static List<FloorData> GenerateFloorDataInterior(Dungeon dungeon)
    {
        if (DungeonFloorDataCache.ContainsKey(dungeon))
        {
            return DungeonFloorDataCache[dungeon];
        }

        List<FloorData> floorDataList = new List<FloorData>();
        DungeonType dungeonType = DetermineDungeonType(dungeon, out Object? elevatorObject);

        if (dungeonType == DungeonType.None)
        {
            DungeonFloorDataCache.Add(dungeon, floorDataList);
            return floorDataList;
        }

        UpdateEntranceDungeonCache(dungeon);

        switch (dungeonType)
        {
            case DungeonType.VanillaDungen:
                if (elevatorObject is MineshaftElevatorController elevator)
                {
                    floorDataList = GetVanillaFloorData(dungeon, elevator);
                }
                else
                {
                    LethalMin.Logger.LogError("Elevator object is not of type MineshaftElevatorController.");
                }
                break;
            case DungeonType.PlayDungen:
                HandlePlayDunGen(elevatorObject!, dungeon, ref floorDataList);
                break;
            case DungeonType.PiggyDungen:

                break;
        }

        DungeonFloorDataCache.Add(dungeon, floorDataList);

        return floorDataList;
    }

    public static void HandlePlayDunGen(Object elevatorObject, Dungeon dungeon, ref List<FloorData> floorDataList)
    {
        if (elevatorObject is PlayZoneElevator playElevator)
        {
            floorDataList = GetPlayFloorData(dungeon, playElevator);
        }
        else
        {
            LethalMin.Logger.LogError("Elevator object is not of type PlayZoneElevator.");
        }
    }

    private static void UpdateEntranceDungeonCache(Dungeon dungeon)
    {
        foreach (var entrance in PikminRouteManager.Instance.EntranceNodes)
        {
            if (entrance == null)
                continue;

            if (EntranceDungeonCache.ContainsKey(entrance))
                continue;

            if (PikminRouteManager.Instance.Dungeons.Count == 1)
            {
                EntranceDungeonCache.Add(entrance, dungeon);
                continue; // early exit for single dungeon levels
            }

            EntranceTeleport? entranceScript = entrance.entrancePoint;
            if (entranceScript == null || entranceScript.isEntranceToBuilding)
                continue;

            Dungeon closestDungeon = PikminRouteManager.GetClosestDungeon(entranceScript.transform.position);
            if (closestDungeon == null)
                continue;

            if (!EntranceDungeonCache.ContainsKey(entrance))
            {
                EntranceDungeonCache.Add(entrance, closestDungeon);
                LethalMin.Logger.LogDebug($"Mapped Entrance {entranceScript.name} to Dungeon {closestDungeon.name} ({PikminRouteManager.Instance.Dungeons.IndexOf(closestDungeon)})");
            }
        }
    }

    /// <summary>
    /// Gets the floor data from the vanilla mineshaft interior.
    /// </summary>
    public static List<FloorData> GetVanillaFloorData(Dungeon dungeon, MineshaftElevatorController elevator)
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

        RouteNode ElevatorNode = new RouteNode
        (
            name: elevator.name,
            point: elevator.transform,
            check: CustomBounds.GetComponent<Collider>()
        );

        ElevatorNode.CheckBuffer = 0.25f;
        ElevatorNode.CheckDistance = 1f;

        FloorData F1 = new FloorData();
        F1.FloorRoot = elevator.elevatorTopPoint.position - new Vector3(0, 0.5f, 0);
        F1.Elevators.Add(ElevatorNode);
        F1.FloorTitle = "(Floor1) Entrance";
        data.Add(F1);

        FloorData F2 = new FloorData();
        F2.FloorRoot = elevator.elevatorBottomPoint.position;
        F2.Elevators.Add(ElevatorNode);
        F2.FloorTitle = "(Floor2) Mineshaft";
        data.Add(F2);

        foreach (var kpv in EntranceDungeonCache)
        {
            if (kpv.Value != dungeon)
                continue;


            FloorData currentFloor = data.OrderBy(floor =>
                    Mathf.Abs(kpv.Key.GetPosition().y - floor.FloorRoot.y))
                    .FirstOrDefault();

            if (currentFloor == F1)
                F1.Exits.Add(kpv.Key);

            if (currentFloor == F2)
                F2.Exits.Add(kpv.Key);

            LethalMin.Logger.LogDebug($"({kpv.Key.name}) Floor: {currentFloor.FloorTitle} Position: {kpv.Key.GetPosition()}");
        }

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

    /// <summary>
    /// Gets the floor data from the PlayZone mod.
    /// </summary>
    public static List<FloorData> GetPlayFloorData(Dungeon dungeon, Object elevate)
    {
        List<FloorData> data = new List<FloorData>();
        RouteNode MainNode = new RouteNode(
            "Main",
            RoundManager.FindMainEntranceScript(true),
            0.45f
        );
        PlayZoneElevator? elevator = elevate as PlayZoneElevator;

        if (elevator == null || elevator.elevatorAnimator == null)
        {
            LethalMin.Logger.LogError("PlayZone Elevator not found or elevatorAnimator is null.");
            return data;
        }

        RouteNode ElevatorNode = new RouteNode
        (
            name: "Elevator",
            point: elevator.GetComponentInChildren<PlayerPhysicsRegion>().transform,
            check: elevator.GetComponentInChildren<PlayerPhysicsRegion>().GetComponent<Collider>()
        );

        ElevatorNode.CheckBuffer = 0.25f;

        FloorData F2 = new FloorData();
        F2.FloorRoot = elevator.doorAnimatorUpper!.transform.position;
        F2.Elevators.Add(ElevatorNode);
        F2.FloorTitle = "(Floor2) Ground";
        data.Add(F2);


        FloorData F1 = new FloorData();
        F1.FloorRoot = elevator.doorAnimatorLower!.transform.position;
        F1.Elevators.Add(ElevatorNode);
        F1.FloorTitle = "(Floor1) Fun";
        data.Add(F1);

        foreach (var kpv in EntranceDungeonCache)
        {
            if (kpv.Value != dungeon)
                continue;


            FloorData currentFloor = data.OrderBy(floor =>
                    Mathf.Abs(kpv.Key.GetPosition().y - floor.FloorRoot.y))
                    .FirstOrDefault();

            if (currentFloor == F1)
                F1.Exits.Add(kpv.Key);

            if (currentFloor == F2)
                F2.Exits.Add(kpv.Key);

            LethalMin.Logger.LogDebug($"({kpv.Key.name}) Floor: {currentFloor.FloorTitle} Position: {kpv.Key.GetPosition()}");
        }

        LethalMin.Logger.LogInfo("Registered PlayZone Floors");

        return data;
    }

    private static DungeonType DetermineDungeonType(Dungeon dungeon, out Object? elevatorObject)
    {
        MineshaftElevatorController? vanillaElevator = FindVanillaElevator(dungeon);
        if (vanillaElevator != null)
        {
            LethalMin.Logger.LogInfo($"Vanilla Dungen detected, getting floor data.");
            elevatorObject = vanillaElevator;
            return DungeonType.VanillaDungen;
        }

        if (LethalMin.IsDependencyLoaded("Piggy.LCOffice"))
        {
            return TryFindPiggyElevator(dungeon, out elevatorObject);
        }

        if (LethalMin.IsDependencyLoaded("LethalMatt.PlayZone"))
        {
            return TryFindPlayElevator(dungeon, out elevatorObject);
        }

        LethalMin.Logger.LogDebug($"did not find any flor data");
        elevatorObject = null;
        return DungeonType.None;
    }

    private static DungeonType TryFindPiggyElevator(Dungeon dungeon, out Object? elevatorObject)
    {
        ElevatorController? piggyElevator = FindPiggyElevator(dungeon) as ElevatorController;
        if (piggyElevator != null)
        {
            LethalMin.Logger.LogInfo($"Piggy.LCOffice detected, getting floor data.");
            elevatorObject = piggyElevator;
            return DungeonType.PiggyDungen;
        }
        elevatorObject = null;
        return DungeonType.None;
    }

    private static DungeonType TryFindPlayElevator(Dungeon dungeon, out Object? elevatorObject)
    {
        PlayZoneElevator? playElevator = FindPlayElevator(dungeon) as PlayZoneElevator;
        if (playElevator != null)
        {
            LethalMin.Logger.LogInfo($"LethalMatt.PlayZone detected, getting floor data.");
            elevatorObject = playElevator;
            return DungeonType.PlayDungen;
        }
        elevatorObject = null;
        return DungeonType.None;
    }

    private static MineshaftElevatorController? FindVanillaElevator(Dungeon dungeon)
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

    private static Object? FindPlayElevator(Dungeon dungeon)
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

    private static Object? FindPiggyElevator(Dungeon dungeon)
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
}