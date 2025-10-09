using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMin.Routeing;

public class PikminRouteManager : MonoBehaviour
{
    public static PikminRouteManager Instance = null!;
    void Awake()
    {
        Instance = this;
    }

    public List<FloorData> CurrentFloorData = new List<FloorData>();
    public Dictionary<EntranceTeleport, Transform> EntranceExitPoints = new Dictionary<EntranceTeleport, Transform>();
    public bool CurrentLevelHasMultipleDungeons;
    public bool RefreshCachePerRoute => CurrentLevelHasMultipleDungeons && !CLHMDtrueOnLoad; // to handle levels that change dungeon count mid-game
    public List<EntranceTeleport> EntrancePathableCheckBlacklist = new List<EntranceTeleport>(); // to handle entrances with teleport triggers
    public RouteNode ShipNode = null!;
    public List<RouteNode> EntranceNodes = new List<RouteNode>();

    private List<RouteGenerationStrategy> strategies = new List<RouteGenerationStrategy>();
    private RouteValidation validator = new RouteValidation();
    private bool CLHMDtrueOnLoad = false;

    public void Start()
    {
        // Register strategies in priority order
        strategies.Add(new DirectPlayerStrategy());      // 250 - highest
        strategies.Add(new MoonOverrideStrategy());      // 200 
        strategies.Add(new DirectOutdoorStrategy());     // 90
        strategies.Add(new IndoorToOutdoorStrategy());   // 90
        strategies.Add(new DirectIndoorStrategy());      // 90
        strategies.Add(new OutdoorToIndoorStrategy());   // 90
        strategies.Add(new FallbackStrategy());          // 0 - lowest

        // Create Ship Node
        ShipNode = new RouteNode
        (
            name: "Ship",
            point: StartOfRound.Instance.insideShipPositions[5],
            check: StartOfRound.Instance.shipInnerRoomBounds
        );
        ShipNode.CheckBuffer = 2.5f;
    }

    public void OnGameLoaded()
    {
        CLHMDtrueOnLoad = CurrentLevelHasMultipleDungeons;
        EntranceTeleport[] entrances = Object.FindObjectsOfType<EntranceTeleport>();
        RefreshEntrancePairs(entrances);
        EntranceNodes = GetAllEntranceNodes(entrances);
    }

    public void RefreshEntrancePairs(EntranceTeleport[] entrances)
    {
        EntranceExitPoints.Clear();
        string log = $"Found {entrances.Length} entrances:";

        foreach (EntranceTeleport entrance in entrances)
        {
            if (!entrance.isEntranceToBuilding)
                continue;

            foreach (EntranceTeleport entranceB in entrances)
            {
                if (entrance.entranceId == entranceB.entranceId && entrance != entranceB
                && !EntranceExitPoints.ContainsKey(entrance) && !EntranceExitPoints.ContainsKey(entranceB))
                {
                    EntranceExitPoints.Add(entrance, entranceB.entrancePoint);
                    EntranceExitPoints.Add(entranceB, entrance.entrancePoint);
                    log += $"\n - {entrance.name} <=> {entranceB.name}";
                }
            }
        }

        LethalMin.Logger.LogDebug(log);
    }

    public void OnGameEnded()
    {
        CurrentFloorData.Clear();
        EntranceExitPoints.Clear();
    }

    public PikminRoute CreateRoute(PikminRouteRequest request)
    {
        RouteContext context = BuildContext(request);

        // Log Context Varibles
        LethalMin.Logger.LogDebug($"({request.Intent}) Route Context: IsInside={context.IsInside}, IsInShip={context.IsInShip},"
        + $" CurrentFloor={(context.CurrentFloor != null ? context.CurrentFloor.FloorTitle : "null")},"
        + $" DestinationIsInside={context.DestinationIsInside}, DestinationIsInShip={context.DestinationIsInShip}");

        // Find best strategy
        RouteGenerationStrategy strategy = strategies
            .Where(s => s.CanHandle(request, context))
            .OrderByDescending(s => s.Priority)
            .FirstOrDefault();

        // Log chosen strategy
        if (strategy != null)
        {
            LethalMin.Logger.LogDebug($"Chosen Route Strategy: {strategy.GetType().Name} (Priority {strategy.Priority})");
        }
        else
        {
            LethalMin.Logger.LogError($"No strategy could handle route request: {request.Intent}");
            return null!;
        }

        if (RefreshCachePerRoute)
        {
            LethalMin.Logger.LogDebug("Refreshing Entrance Pairs Cache");
            EntranceTeleport[] entrances = Object.FindObjectsOfType<EntranceTeleport>();
            RefreshEntrancePairs(entrances);
            EntranceNodes = GetAllEntranceNodes(entrances);
        }

        List<RouteNode> nodes = strategy.GenerateRoute(request, context);

        //Log each route node name
        string nodeLog = "Generated Route Nodes: " + string.Join(" -> ", nodes.Select(n => n.name));
        LethalMin.Logger.LogDebug(nodeLog);

        // Create route with the generated nodes
        PikminRoute route = new PikminRoute(request, context, nodes);
        return route;
    }

    private RouteContext BuildContext(PikminRouteRequest request)
    {
        RouteContext context = new RouteContext();

        // Determine current location
        context.IsInside = request.Pikmin.isOutside == false;
        context.IsInShip = IsInShipBounds(request.Pikmin.transform.position);
        context.CurrentFloor = GetFloorPikminIsOn(request.Pikmin);

        // Determine destination location
        switch (request.Intent)
        {
            case RouteIntent.ToShip:
                context.DestinationIsInside = false;
                context.DestinationIsInShip = true; // Ship is outside (usually)
                break;
            case RouteIntent.ToOnion:
                context.DestinationIsInside = false;
                break;
            case RouteIntent.ToExit:
                context.DestinationIsInside = true;
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
                context.DestinationIsInShip = false; // Vehicles can't be in ship
                break;
            case RouteIntent.ToCounter:
                context.DestinationIsInside = false;
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
        FloorData FloorOn;
        FloorOn = null!;
        if (CurrentFloorData.Count == 0)
        {
            return null;
        }

        FloorData currentFloor = CurrentFloorData.OrderBy(floor =>
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

    public List<RouteNode> GetAllPossibleVehicleNodes()
    {
        List<RouteNode> vehicleNodes = new List<RouteNode>();
        foreach (PikminVehicleController vehicle in PikminManager.instance.Vehicles)
        {
            RouteNode vehicleNode = new RouteNode
            (
                name: vehicle.gameObject.name,
                point: vehicle.PikminCheckRegion.bounds.center,
                check: vehicle.PikminCheckRegion
            );
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

    public RouteNode? GetPossibleCounterNode()
    {
        DepositItemsDesk counter = Object.FindObjectOfType<DepositItemsDesk>();
        if (counter != null)
        {
            RouteNode counterNode = new RouteNode
            (
                name: "Counter",
                point: counter.transform.position,
                check: 2.5f
            );
            return counterNode;
        }
        return null;
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