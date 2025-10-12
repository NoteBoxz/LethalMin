using System.Collections.Generic;
using System.Linq;
using DunGen;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Routeing;

public class PikminRouteManager : MonoBehaviour
{
    public static PikminRouteManager Instance = null!;
    void Awake()
    {
        Instance = this;
    }

    public List<Dungeon> Dungeons = new List<Dungeon>();
    public List<FloorData> CurrentFloorData = new List<FloorData>();
    public Dictionary<EntranceTeleport, Transform> EntranceExitPoints = new Dictionary<EntranceTeleport, Transform>();
    public bool CurrentLevelHasMultipleDungeons;
    public bool RefreshCachePerRoute => CurrentLevelHasMultipleDungeons && !cLHMDtrueOnLoad; // to handle levels that change dungeon count mid-game
    public Dictionary<int, Transform> AddedTelepointsForExits = new Dictionary<int, Transform>();

    public RouteNode ShipNode = null!;
    public List<RouteNode> EntranceNodes = new List<RouteNode>();

    private List<RouteGenerationStrategy> strategies = new List<RouteGenerationStrategy>();
    private RouteValidation validator = new RouteValidation();
    private bool cLHMDtrueOnLoad = false;
    private bool insideLogFlag = false;

    public void Start()
    {
        // Register strategies in priority order
        strategies.Add(new MoonOverrideStrategy());      // 200 - high
        strategies.Add(new DirectOutdoorStrategy());     // 90
        strategies.Add(new IndoorToOutdoorStrategy());   // 90
        strategies.Add(new DirectIndoorStrategy());      // 90
        strategies.Add(new OutdoorToIndoorStrategy());   // 90 - low

        // Create Ship Node
        ShipNode = new RouteNode
        (
            name: "Ship",
            point: StartOfRound.Instance.insideShipPositions[5],
            check: StartOfRound.Instance.shipInnerRoomBounds
        );
        ShipNode.CheckDistance = 5f;
        ShipNode.CheckBuffer = 1f;
    }

    public void OnGameLoaded()
    {
        if (Dungeons.Count == 0)
        {
            LethalMin.Logger.LogDebug("No Dungeons found in scene on game load.");
        }
            
        FloorDataGenerator.DungeonFloorDataCache.Clear();
        FloorDataGenerator.EntranceDungeonCache.Clear();

        cLHMDtrueOnLoad = CurrentLevelHasMultipleDungeons;
        insideLogFlag = false;
        EntranceTeleport[] entrances = Object.FindObjectsOfType<EntranceTeleport>();
        RefreshEntrancePairs(entrances);
        EntranceNodes = GetAllEntranceNodes(entrances);
        if (Dungeons.Count > 0)
            CurrentFloorData = FloorDataGenerator.GenerateFloorDataInterior(Dungeons[0]);
    }

    public void RefreshEntrancePairs(EntranceTeleport[] entrances)
    {
        EntranceExitPoints.Clear();
        string log = $"Found {entrances.Length} entrances:";

        foreach (EntranceTeleport entrance in entrances)
        {
            if (!entrance.isEntranceToBuilding)
                continue;

            foreach (EntranceTeleport exit in entrances)
            {
                if (entrance.entranceId == exit.entranceId && entrance != exit
                && !EntranceExitPoints.ContainsKey(entrance) && !EntranceExitPoints.ContainsKey(exit))
                {
                    EntranceExitPoints.Add(entrance, exit.entrancePoint);
                    if (AddedTelepointsForExits.ContainsKey(entrance.entranceId))
                    {
                        EntranceExitPoints.Add(exit, AddedTelepointsForExits[entrance.entranceId]);
                        log += $"\n - ({entrance.name} -> {AddedTelepointsForExits[entrance.entranceId].name}) <=> {exit.name} [Manually Added]";
                    }
                    else
                    {
                        EntranceExitPoints.Add(exit, entrance.entrancePoint);
                        log += $"\n - {entrance.name} <=> {exit.name}";
                    }
                }
            }
        }

        LethalMin.Logger.LogDebug(log);
    }

    public void OnGameEnded()
    {
        CurrentFloorData.Clear();
        EntranceExitPoints.Clear();
        AddedTelepointsForExits.Clear();
        FloorDataGenerator.DungeonFloorDataCache.Clear();
        FloorDataGenerator.EntranceDungeonCache.Clear();
    }

    public PikminRoute CreateRoute(PikminRouteRequest request)
    {
        RouteContext context = BuildContext(request);

        // Log Context Varibles
        string contextStr = $"\n({request.Intent}) Route Context: IsInside={context.IsInside}, IsInShip={context.IsInShip},"
        + $" CurrentFloor={(context.CurrentFloor != null ? context.CurrentFloor.FloorTitle : "null")},"
        + $" DestinationIsInside={context.DestinationIsInside}, DestinationIsInShip={context.DestinationIsInShip}";

        // Find best strategy
        RouteGenerationStrategy strategy = strategies
            .Where(s => s.CanHandle(request, context))
            .OrderByDescending(s => s.Priority)
            .FirstOrDefault();

        // Log chosen strategy
        if (strategy != null)
        {
            LethalMin.Logger.LogDebug($"Chosen Route Strategy: {strategy.GetType().Name} (Priority {strategy.Priority}) {contextStr}");
        }
        else
        {
            LethalMin.Logger.LogError($"No strategy could handle route request: {request.Intent} {contextStr}");
            return null!;
        }

        if (RefreshCachePerRoute)
        {
            LethalMin.Logger.LogDebug("Refreshing Entrance Pairs Cache");
            EntranceTeleport[] entrances = FindObjectsOfType<EntranceTeleport>();
            RefreshEntrancePairs(entrances);
            EntranceNodes = GetAllEntranceNodes(entrances);
        }

        List<RouteNode> nodes = strategy.GenerateRoute(request, context);
        bool unpathableNodeFound = false;
        foreach (RouteNode node in nodes)
        {
            if (node.UnpathableOnCreation)
            {
                unpathableNodeFound = true;
                break;
            }
        }

        //Log each route node name
        string nodeLog = "Generated Route Nodes: " + string.Join(" -> ", nodes.Select(n => n.name));
        nodeLog += unpathableNodeFound ? "\n(Unpathable Node Found)" : "";
        LethalMin.Logger.LogDebug(nodeLog);

        // Create route with the generated nodes
        PikminRoute route = new PikminRoute(request, context, nodes);
        route.Pathable = !unpathableNodeFound;
        return route;
    }

    private RouteContext BuildContext(PikminRouteRequest request)
    {
        RouteContext context = new RouteContext();

        // Determine current location
        context.IsInside = request.Pikmin.isOutside == false;
        context.IsInShip = IsInShipBounds(request.Pikmin.transform.position);
        context.CurrentFloor = context.IsInside ? GetFloorPikminIsOn(request.Pikmin) : null;

        // Determine destination location
        switch (request.Intent)
        {
            case RouteIntent.ToShip:
                context.DestinationIsInside = IsInDungeon(LethalMin.SSRenviourment.transform.position)
                && Vector3.Distance(LethalMin.SSRenviourment.transform.position, LethalMin.enviormentStartPos) > 200f; // Outfall my beloved
                context.DestinationIsInShip = true;

                // lets hope this for some reason does not get set to true for any other level than outfall
                if (context.DestinationIsInside && !insideLogFlag)
                {
                    insideLogFlag = true;
                    LethalMin.Logger.LogMessage($"SHIP IS INSIDE?!?!?!??!??!?!?!?!?!?!!??????!?!?!?!?!??!?!?!?!?!?!?!?!?!?!?????" +
                    $" CurrentLevelScene: {StartOfRound.Instance.currentLevel.sceneName}");
                }
                break;
            case RouteIntent.ToOnion:
                context.DestinationIsInside = false; // Outfall and any moon like it should disable onion spawning in it's moon settings
                break;
            case RouteIntent.ToExit:
                context.DestinationIsInside = context.IsInside;
                break;
            case RouteIntent.ToPlayer:
                if (request.TargetPlayer == null)
                {
                    LethalMin.Logger.LogWarning("RouteIntent.ToPlayer but TargetPlayer is null");
                    break;
                }
                PlayerControllerB player = request.TargetPlayer.Controller;
                context.DestinationIsInside = player.isInsideFactory;
                context.DestinationIsInShip = player.isInHangarShipRoom;
                break;
            case RouteIntent.ToVehicle:
                context.DestinationIsInside = false; // If zeekees or another mod adds indoor vehicles we're screwed
                break;
            case RouteIntent.ToCounter:
                context.DestinationIsInside = false; // Imagine a company interior :D
                break;
            case RouteIntent.ToElevator:
                context.DestinationIsInside = true;
                break;
            case RouteIntent.ToSpecificPoint:
                Vector3 Pos = request.CustomTransform != null ? request.CustomTransform.position : request.CustomDestination;
                context.DestinationIsInside = IsInDungeon(Pos);
                context.DestinationIsInShip = IsInShipBounds(Pos);
                break;
        }

        return context;
    }

    public FloorData? GetFloorPikminIsOn(PikminAI pikmin)
    {
        List<FloorData> floors = CurrentFloorData;
        if (Dungeons.Count > 1)
        {
            floors = FloorDataGenerator.GenerateFloorDataInterior(GetClosestDungeon(pikmin.transform.position));
        }
        FloorData FloorOn;
        FloorOn = null!;
        if (floors.Count == 0)
        {
            return null;
        }

        FloorData currentFloor = floors.OrderBy(floor =>
                Mathf.Abs(pikmin.transform.position.y - floor.FloorRoot.y))
                .FirstOrDefault();

        if (currentFloor != null)
        {
            //LethalMin.Logger.LogInfo($"({pikmin.DebugID}) Current floor: {currentFloor.FloorTitle}");
        }
        else
        {
            LethalMin.Logger.LogWarning($"({pikmin.DebugID}) No valid floor found for Pikmin");
        }

        if (currentFloor != null)
        {
            FloorOn = currentFloor;
        }
        return currentFloor;
    }

    public List<RouteNode> GetAllPossibleOnionNodes()
    {
        List<RouteNode> onionNodes = new List<RouteNode>();
        foreach (Onion onion in PikminManager.instance.Onions)
        {
            RouteNode onionNode = new RouteNode
            (
                name: onion.onionType.TypeName,
                point: onion.ItemDropPos,
                check: 0.5f
            );
            onionNode.InstanceIdentifiyer = onion.ItemDropPos;
            onionNodes.Add(onionNode);
        }
        return onionNodes;
    }

    public List<RouteNode> GetAllPossibleVehicleNodes(PikminRouteRequest request)
    {
        List<RouteNode> vehicleNodes = new List<RouteNode>();
        foreach (PikminVehicleController vehicle in PikminManager.instance.Vehicles)
        {
            if (!vehicle.controller.backDoorOpen || vehicle.controller.carDestroyed)
            {
                continue; // Skip vehicles with closed doors
            }
            if (vehicle.IsNearByShip())
            {
                continue;
            }
            RouteNode vehicleNode = new RouteNode
            (
                name: vehicle.gameObject.name,
                point: vehicle.GetAvaiblePikminPoint(request.Pikmin),
                check: vehicle.PikminCheckRegion
            );
            vehicleNode.CheckBuffer = 1f;
            vehicleNode.CheckDistance = 1f;
            vehicleNodes.Add(vehicleNode);
        }
        return vehicleNodes;
    }

    public List<RouteNode> GetAllEntranceNodes(EntranceTeleport[] entrances)
    {
        List<RouteNode> entranceNodes = new List<RouteNode>();
        foreach (EntranceTeleport entrance in entrances)
        {
            RouteNode entranceNode = new RouteNode
            (
                name: entrance.name,
                point: entrance,
                check: 1f
            );
            entranceNodes.Add(entranceNode);
        }
        return entranceNodes;
    }

    public RouteNode? GetPossibleCounterNode(PikminRouteRequest request)
    {
        DepositItemsDesk counter = Object.FindObjectOfType<DepositItemsDesk>();
        if (counter != null)
        {
            Vector3 pikminPos = request.Pikmin.transform.position;
            Vector3 counterPos = counter.triggerCollider.transform.position;

            // Try to find a pathable position on the ground in front of the counter, since the company building doesn't have this issue we skip it there
            Vector3 pathablePos = RoundManager.Instance.currentLevel.sceneName == "CompanyBuilding" ? counterPos : FindPathableCounterPosition(counterPos, pikminPos);

            RouteNode counterNode = new RouteNode
            (
                name: "Counter",
                point: pathablePos,
                check: 2.5f
            );
            return counterNode;
        }
        return null;
    }

    private Vector3 FindPathableCounterPosition(Vector3 counterPos, Vector3 pikminPos)
    {
        // First, try to sample the NavMesh directly at the counter position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(counterPos, out hit, 2f, NavMesh.AllAreas))
        {
            // Verify this position is actually reachable
            NavMeshPath testPath = new NavMeshPath();
            if (NavMesh.CalculatePath(pikminPos, hit.position, NavMesh.AllAreas, testPath)
                && testPath.status == NavMeshPathStatus.PathComplete)
            {
                return hit.position;
            }
            else
            {
                // get last corner of path and return that
                if (testPath.corners.Length > 0)
                {
                    return testPath.corners.Last();
                }
            }
        }

        return counterPos;
    }

    public RouteNode GetSpesficPointNode(PikminRouteRequest request)
    {
        RouteNode specificPointNode;
        if (request.CustomTransform != null)
        {
            specificPointNode = new RouteNode
            (
                name: "CustomTransform",
                point: request.CustomTransform.position,
                check: request.CustomCheckCollider == null ? request.CustomCheckDistance : request.CustomCheckCollider
            );
        }
        else
        {
            specificPointNode = new RouteNode
            (
                name: "CustomDestination",
                point: request.CustomDestination,
                check: request.CustomCheckCollider == null ? request.CustomCheckDistance : request.CustomCheckCollider
            );
        }
        return specificPointNode;
    }

    public RouteNode GetPlayerNode(PikminRouteRequest request)
    {
        RouteNode playerNode = new RouteNode
        (
            name: "Player",
            point: request.TargetPlayer!.transform,
            check: request.CustomCheckDistance
        );
        playerNode.InstanceIdentifiyer = request.TargetPlayer;
        return playerNode;
    }

    public static EntranceTeleport GetClosestEntrance(Vector3 position, bool CompareOutside)
    {
        float closestDistance = Mathf.Infinity;
        EntranceTeleport closestEntrance = null!;
        foreach (EntranceTeleport entrance in Instance.EntranceExitPoints.Keys)
        {
            if (CompareOutside != entrance.isEntranceToBuilding)
            {
                continue;
            }

            float dist = Vector3.Distance(position, entrance.entrancePoint.position);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestEntrance = entrance;
            }
        }
        return closestEntrance;
    }

    public static Dungeon GetClosestDungeon(Vector3 position)
    {
        if (Instance.Dungeons.Count == 1)
        {
            return Instance.Dungeons[0];
        }

        float closestDistance = Mathf.Infinity;
        Dungeon closestDungeon = null!;
        foreach (Dungeon dungeon in Instance.Dungeons)
        {
            if (dungeon == null)
            {
                LethalMin.Logger.LogWarning("Null Dungeon found in Dungeon list");
                continue;
            }
            float dist = Vector3.Distance(position, dungeon.transform.position);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestDungeon = dungeon;
            }
        }
        return closestDungeon;
    }

    public static bool IsInShipBounds(Vector3 position)
    {
        return StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(position);
    }

    public static bool IsInDungeon(Vector3 position)
    {
        GameObject[] OutsideNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
        GameObject[] InsideNodes = GameObject.FindGameObjectsWithTag("AINode");

        //Get the closest outside node
        GameObject closestOutsideNode = OutsideNodes.OrderBy(node => Vector3.Distance(position, node.transform.position)).FirstOrDefault();
        GameObject closestInsideNode = InsideNodes.OrderBy(node => Vector3.Distance(position, node.transform.position)).FirstOrDefault();
        if (closestOutsideNode == null || closestInsideNode == null)
        {
            return false;
        }
        float outsideDistance = Vector3.Distance(position, closestOutsideNode.transform.position);
        float insideDistance = Vector3.Distance(position, closestInsideNode.transform.position);

        return insideDistance < outsideDistance;
    }
}