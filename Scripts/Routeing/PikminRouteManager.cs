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
    public RouteNode ShipNode = null!;
    public Dictionary<EntranceTeleport, GameObject> EntranceExitPoints = new Dictionary<EntranceTeleport, GameObject>();
    public bool CurrentLevelHasMultipleDungeons;
    public bool RefreshCachePerRoute => CurrentLevelHasMultipleDungeons && !CLHMDtrueOnLoad; // to handle levels that change dungeon count mid-game

    private List<RouteGenerationStrategy> strategies = new List<RouteGenerationStrategy>();
    private RouteValidation validator = new RouteValidation();
    private bool CLHMDtrueOnLoad = false;

    public void Start()
    {
        // Register strategies in priority order
        strategies.Add(new DirectPlayerStrategy());      // 250 - highest
        strategies.Add(new MoonOverrideStrategy());      // 200 
        strategies.Add(new DirectOutdoorStrategy());     // 100
        strategies.Add(new GoOutsideStrategy());         // 90
        strategies.Add(new ElevatorStrategy());          // 80
        strategies.Add(new FallbackStrategy());          // 0 - lowest

        // Create Ship Node
        ShipNode = new RouteNode
        (
            name: "Ship",
            point: StartOfRound.Instance.shipInnerRoomBounds.bounds.center,
            check: StartOfRound.Instance.shipInnerRoomBounds
        );
        ShipNode.CheckBuffer = 2.5f;
    }

    public void OnGameLoaded()
    {
        CLHMDtrueOnLoad = CurrentLevelHasMultipleDungeons;
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
        LethalMin.Logger.LogInfo($"Route Context: IsInside={context.IsInside}, IsInShip={context.IsInShip}, CurrentFloor={(context.CurrentFloor != null ? context.CurrentFloor.FloorTitle : "null")}, DestinationIsInside={context.DestinationIsInside}, DestinationIsInShip={context.DestinationIsInShip}");

        // Find best strategy
        RouteGenerationStrategy strategy = strategies
            .Where(s => s.CanHandle(request, context))
            .OrderByDescending(s => s.Priority)
            .FirstOrDefault();

        // Log chosen strategy
        if (strategy != null)
        {
            LethalMin.Logger.LogInfo($"Chosen Route Strategy: {strategy.GetType().Name} (Priority {strategy.Priority})");
        }
        else
        {
            LethalMin.Logger.LogWarning("No Route Strategy could handle the request");
        }

        if (strategy == null)
        {
            LethalMin.Logger.LogError($"No strategy could handle route request: {request.Intent}");
            return null!;
        }

        List<RouteNode> nodes = strategy.GenerateRoute(request, context);

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