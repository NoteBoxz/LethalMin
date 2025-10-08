using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalMin.Routeing;

public class PikminRouteManager : MonoBehaviour
{
    public static PikminRouteManager Instance = null!;
    void Awake()
    {
        Instance = this;
    }

    public static List<FloorData> CurrentFloorData = new List<FloorData>();
    public bool CurrentLevelHasMultipleDungeons;
    private List<RouteGenerationStrategy> strategies = new List<RouteGenerationStrategy>();
    private RouteValidation validator = new RouteValidation();

    public void Start()
    {
        // Register strategies in priority order
        strategies.Add(new MoonOverrideStrategy());      // 200 - highest
        strategies.Add(new DirectOutdoorStrategy());     // 100
        strategies.Add(new ElevatorStrategy());          // 90
        strategies.Add(new ExitFirstStrategy());         // 80
        strategies.Add(new FallbackStrategy());          // 0 - lowest
    }

    public void OnGameLoaded()
    {
        
    }

    public void OnGameEnded()
    {
        
    }

    public PikminRoute CreateRoute(PikminRouteRequest request)
    {
        RouteContext context = BuildContext(request);

        // Find best strategy
        RouteGenerationStrategy strategy = strategies
            .Where(s => s.CanHandle(request, context))
            .OrderByDescending(s => s.Priority)
            .FirstOrDefault();

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
                context.DestinationIsInShip = false; // Ship is outside (usually)
                break;
            case RouteIntent.ToOnion:
                context.DestinationIsInside = false;
                break;
            case RouteIntent.ToExit:
                context.DestinationIsInside = false;
                break;
            case RouteIntent.ToElevator:
                context.DestinationIsInside = true;
                break;
                // etc...
        }

        return context;
    }

    public static bool IsInShipBounds(Vector3 position)
    {
        return StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(position);
    }
    public static FloorData? GetFloorPikminIsOn(PikminAI pikmin)
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
}