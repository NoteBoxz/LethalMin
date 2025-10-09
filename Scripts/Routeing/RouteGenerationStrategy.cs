using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Routeing;

/// <summary>
/// Base class for route generation strategies.
/// </summary>
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

    protected RouteNode GetMostPathableEntranceNode(bool Outside, Vector3 from, List<RouteNode> entranceNodes, RouteNode exitPointCheckNode = null!)
    {
        RouteNode? bestNode = null;
        float bestDistance = float.MaxValue;
        string outsideStr = Outside ? "outside" : "inside";

        foreach (RouteNode node in entranceNodes)
        {
            if (node.entrancePoint == null)
                continue;
            if (node.entrancePoint.isEntranceToBuilding && !Outside)
                continue;
            if (!node.entrancePoint.isEntranceToBuilding && Outside)
                continue;

            Vector3 nodePos = node.GetPosition();
            if (IsDestinationReachable(from, nodePos)
            && (exitPointCheckNode == null
            || !manager.EntranceExitPoints.ContainsKey(node.entrancePoint)
            || manager.EntrancePathableCheckBlacklist.Contains(node.entrancePoint)
            || IsDestinationReachable(manager.EntranceExitPoints[node.entrancePoint].position, exitPointCheckNode.GetPosition())))
            {
                float distance = CalculatePathLength(from, nodePos);
                LethalMin.Logger.LogDebug($"{outsideStr} Entrance Node {node.name} is reachable at distance {distance}");
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestNode = node;
                }
            }
            else
            {
                LethalMin.Logger.LogDebug($"{outsideStr} Entrance Node {node.name} is NOT reachable");
            }
        }

        if (bestNode == null)
            LethalMin.Logger.LogWarning($"No {outsideStr} entrance nodes are directly reachable, defaulting to closest entrance node");

        return bestNode ?? entranceNodes.OrderBy(n => Vector3.Distance(from, n.GetPosition())).First();
    }

    protected RouteNode GetMostPathableElevator(Vector3 from, FloorData data)
    {
        if (data.Elevators.Count == 0)
        {
            LethalMin.Logger.LogWarning("No elevator nodes found in current floor data.");
            return null!;
        }
        if (data.Elevators.Count == 1)
        {
            return data.Elevators[0];
        }

        RouteNode? bestNode = null;
        float bestDistance = float.MaxValue;

        foreach (RouteNode node in data.Elevators)
        {
            Vector3 nodePos = node.GetPosition();
            if (IsDestinationReachable(from, nodePos))
            {
                float distance = CalculatePathLength(from, nodePos);
                LethalMin.Logger.LogDebug($"Elevator Node {node.name} is reachable at distance {distance}");
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestNode = node;
                }
            }
            else
            {
                LethalMin.Logger.LogDebug($"Elevator Node {node.name} is NOT reachable");
            }
        }

        return bestNode!;
    }

    protected float CalculatePathLength(Vector3 from, Vector3 to)
    {
        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);

        float length = 0f;
        if (path.corners.Length < 2)
            return length;

        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return length;
    }
}

/// <summary>
/// Direct route to player strategy.
/// Primarlly used for the ManEater
/// </summary>
public class DirectPlayerStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        return request.Intent == RouteIntent.ToPlayer;
    }

    public override int Priority => 250; // Highest for obvious reasons

    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();

        nodes.Add(manager.GetPlayerNode(request));

        return nodes;
    }
}

/// <summary>
/// Direct route to outdoor destination strategy.
/// Used when both start and end are outside.
/// </summary>
public class DirectOutdoorStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
        RouteIntent.ToElevator,
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Can only handle if we're outside and destination is outside
        return base.CanHandle(request, context) && !context.IsInside && !context.DestinationIsInside;
    }

    public override int Priority => 90; // Basic Priority

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

            case RouteIntent.ToExit:
                List<RouteNode> TargetExits = context.CurrentFloor == null ? manager.EntranceNodes : context.CurrentFloor.Exits;
                RouteNode BestExitNode = GetMostPathableEntranceNode(true, request.Pikmin.agent.transform.position, TargetExits);
                nodes.Add(BestExitNode);
                break;

            case RouteIntent.ToPlayer:
                nodes.Add(manager.GetPlayerNode(request));
                break;

            case RouteIntent.ToElevator: // Unsupported
                break;
        }

        return nodes;
    }
}

/// <summary>
/// Route strategy for going from indoors to outdoors.
/// Used when starting inside and needing to go outside.
/// </summary>
public class IndoorToOutdoorStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
        RouteIntent.ToElevator, // Elevators can only be indoors
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Handle when inside and need to go outside
        return base.CanHandle(request, context) && context.NeedToExitBuilding;
    }

    public override int Priority => 90; // Basic Priority

    public DirectOutdoorStrategy directOutdoor = new DirectOutdoorStrategy();

    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();
        List<RouteNode> outsideNodes = directOutdoor.GenerateRoute(request, context);
        if (outsideNodes.Count == 0)
        {
            LethalMin.Logger.LogWarning("No outside nodes generated for indoor to outdoor route, aborting route generation.");
            return nodes;
        }
        RouteNode mostPathableEntrance = GetMostPathableEntranceNode(false, request.Pikmin.agent.transform.position, manager.EntranceNodes, outsideNodes[0]);

        nodes.Add(mostPathableEntrance);
        nodes.AddRange(outsideNodes);

        return nodes;
    }
}

/// <summary>
/// Direct route to indoor destination strategy.
/// Used when both start and end are inside.
/// </summary>
public class DirectIndoorStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
        RouteIntent.ToOnion,
        RouteIntent.ToVehicle,
        RouteIntent.ToCounter,
    }; // prolly gonna have to change this in the far future

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Can only handle if we're inside and destination is inside
        return base.CanHandle(request, context) && context.IsInside && context.DestinationIsInside;
    }

    public override int Priority => 90; // Basic Priority


    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();

        switch (request.Intent)
        {
            case RouteIntent.ToShip:
                nodes.Add(manager.ShipNode);
                break;

            case RouteIntent.ToExit:
                List<RouteNode> TargetExits = context.CurrentFloor == null ? manager.EntranceNodes : context.CurrentFloor.Exits;
                RouteNode BestExitNode = GetMostPathableEntranceNode(true, request.Pikmin.agent.transform.position, TargetExits);
                nodes.Add(BestExitNode);
                break;

            case RouteIntent.ToElevator:
                if (context.CurrentFloor == null)
                {
                    LethalMin.Logger.LogWarning("No current floor data available for elevator routing.");
                    break;
                }
                RouteNode BestElevatorNode = GetMostPathableElevator(request.Pikmin.agent.transform.position, context.CurrentFloor);
                if (BestElevatorNode != null)
                    nodes.Add(BestElevatorNode);
                break;

            case RouteIntent.ToPlayer:
                nodes.Add(manager.GetPlayerNode(request));
                break;

            case RouteIntent.ToSpecificPoint:
                nodes.Add(manager.GetSpesficPointNode(request));
                break;
        }

        return nodes;
    }
}

/// <summary>
/// Route strategy for going from outdoors to indoors.
/// Used when starting outside and needing to go inside.
/// </summary>
public class OutdoorToIndoorStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Handle when outside and need to go inside
        return base.CanHandle(request, context) && context.NeedToEnterBuilding;
    }

    public override int Priority => 90; // Basic Priority

    public DirectIndoorStrategy directIndoor = new DirectIndoorStrategy();

    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();
        List<RouteNode> insideNodes = directIndoor.GenerateRoute(request, context);
        if (insideNodes.Count == 0)
        {
            LethalMin.Logger.LogWarning("No outside nodes generated for outdoor to indoor route, aborting route generation.");
            return nodes;
        }
        RouteNode mostPathableEntrance = GetMostPathableEntranceNode(true, request.Pikmin.agent.transform.position, manager.EntranceNodes, insideNodes[0]);

        nodes.Add(mostPathableEntrance);
        nodes.AddRange(insideNodes);

        return nodes;
    }
}

/// <summary>
/// Route strategy that defers to the moon mod's pathing override if enabled.
/// </summary>
public class MoonOverrideStrategy : RouteGenerationStrategy
{
    public override List<RouteIntent> UnsupportedIntents => new List<RouteIntent>
    {
    };

    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        return base.CanHandle(request, context) && PikminManager.instance.CurrentMoonSettings != null && PikminManager.instance.CurrentMoonSettings.OverridePathing;
    }

    public override int Priority => 200; // Second Highest

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

/// <summary>
/// Fallback strategy if no other strategies can handle the request.
/// </summary>
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
        List<RouteNode> nodes = new List<RouteNode>(); ;

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
