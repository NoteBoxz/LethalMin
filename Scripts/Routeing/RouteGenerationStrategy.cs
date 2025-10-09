using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Routeing;

public abstract class RouteGenerationStrategy
{
    public virtual bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Default implementation: can handle all requests
        return !UnsupportedIntents.Contains(request.Intent);
    }
    public abstract int Priority { get; } // Higher = preferred
    public abstract List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context);
    public abstract List<RouteIntent> UnsupportedIntents { get; }
    protected PikminRouteManager manager = PikminRouteManager.Instance;
    protected List<RouteNode> onionNodes = PikminRouteManager.Instance.GetAllPossibleOnionNodes();

    protected bool IsDestinationReachable(Vector3 from, Vector3 to, bool OffsetOntoNavMesh = true)
    {
        NavMeshPath path = new NavMeshPath();
        Vector3 startPos = from;
        Vector3 endPos = to;

        if (OffsetOntoNavMesh)
        {
            if (NavMesh.SamplePosition(from, out NavMeshHit hitStart, 2f, NavMesh.AllAreas))
                startPos = hitStart.position;
            if (NavMesh.SamplePosition(to, out NavMeshHit hitEnd, 2f, NavMesh.AllAreas))
                endPos = hitEnd.position;
        }

        bool foundPath = NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, path);
        return foundPath && path.status == NavMeshPathStatus.PathComplete;
    }
}

public class DirectPlayerStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        return request.Intent == RouteIntent.ToPlayer;
    }

    public override int Priority => 250; // Highest - simplest case

    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();

        RouteNode playerNode = new RouteNode
        (
            name: "Player",
            point: request.TargetPlayer!.transform,
            check: request.CustomCheckDistance
        );

        nodes.Add(playerNode);
        return nodes;
    }
}

public class DirectOutdoorStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
        RouteIntent.ToExit,
        RouteIntent.ToElevator,
        RouteIntent.ToPlayer,
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Can only handle if we're outside and destination is outside
        return base.CanHandle(request, context) && !context.IsInside && !context.DestinationIsInside;
    }

    public override int Priority => 100; // Highest - simplest case

    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();

        switch (request.Intent)
        {
            case RouteIntent.ToShip:
                nodes.Add(manager.ShipNode);
                break;

            case RouteIntent.ToOnion:
                foreach (RouteNode onionNode in onionNodes)
                {
                    if (onionNode.InstanceIdentifiyer != null && onionNode.InstanceIdentifiyer == request.TargetOnion?.ItemDropPos)
                    {
                        nodes.Add(onionNode);
                        break;
                    }
                }
                break;

            case RouteIntent.ToVehicle:
                foreach (RouteNode vehicleNode in manager.GetAllPossibleVehicleNodes())
                {
                    if (vehicleNode.InstanceIdentifiyer != null)
                    {
                        nodes.Add(vehicleNode);
                        break;
                    }
                }
                break;

            case RouteIntent.ToCounter:
                RouteNode? counterNode = manager.GetPossibleCounterNode();
                if (counterNode != null)
                    nodes.Add(counterNode);
                break;

            case RouteIntent.ToSpecificPoint:
                nodes.Add(manager.GetSpesficPointNode(request));
                break;

            case RouteIntent.ToExit: // Unsupported
                break;

            case RouteIntent.ToElevator: // Unsupported
                break;
                
            case RouteIntent.ToPlayer: // Unsupported
                break;
        }

        return nodes;
    }
}

public class GoOutsideStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Handle when inside and need to go outside
        return base.CanHandle(request, context) && context.IsInside && context.NeedToExitBuilding;
    }

    public override int Priority => 90;

    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();

        switch (request.Intent)
        {
            case RouteIntent.ToShip:
                break;
            case RouteIntent.ToOnion:
                break;
            case RouteIntent.ToVehicle:
                break;
            case RouteIntent.ToCounter:
                break;
            case RouteIntent.ToExit:
                break;
            case RouteIntent.ToElevator:
                break;
            case RouteIntent.ToPlayer:
                break;
            case RouteIntent.ToSpecificPoint:
                break;
        }

        return nodes;
    }
}

public class ElevatorStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Only handle if we are unable to reach an exit on the current floor
        return base.CanHandle(request, context) && context.CurrentFloor != null;
    }

    public override int Priority => 80;

    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();

        switch (request.Intent)
        {
            case RouteIntent.ToShip:
                break;
            case RouteIntent.ToOnion:
                break;
            case RouteIntent.ToVehicle:
                break;
            case RouteIntent.ToCounter:
                break;
            case RouteIntent.ToExit:
                break;
            case RouteIntent.ToElevator:
                break;
            case RouteIntent.ToPlayer:
                break;
            case RouteIntent.ToSpecificPoint:
                break;
        }

        return nodes;
    }
}

public class MoonOverrideStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        return base.CanHandle(request, context) && PikminManager.instance.CurrentMoonSettings != null && PikminManager.instance.CurrentMoonSettings.OverridePathing;
    }

    public override int Priority => 200; // Highest - moon-specific always wins

    /// <summary>
    /// Expected to be patched by the moon mod's code.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();


        return nodes;
    }
}

public class FallbackStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        return base.CanHandle(request, context) && true; // Always can handle
    }

    public override int Priority => 0; // Lowest priority

    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();


        return nodes;
    }
}

