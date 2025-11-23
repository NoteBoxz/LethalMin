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

    protected bool IsDestinationReachable(Vector3 from, Vector3 to, NavMeshPath path = null!, bool OffsetOntoNavMesh = true)
    {
        if (path != null && path.status == NavMeshPathStatus.PathComplete) // Early exit if the provided path is already complete
            return true;

        path = new NavMeshPath();
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
        List<RouteNode> validNodes = new List<RouteNode>();

        foreach (RouteNode node in entranceNodes)
        {
            if (node.entrancePoint == null)
                continue;
            if (node.entrancePoint.isEntranceToBuilding && !Outside)
                continue;
            if (!node.entrancePoint.isEntranceToBuilding && Outside)
                continue;
            validNodes.Add(node);

            Vector3 nodePos = node.GetPosition();
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(from, nodePos, NavMesh.AllAreas, path);

            bool shouldCheckExitPath = exitPointCheckNode != null
                && manager.EntranceExitPoints.ContainsKey(node.entrancePoint);

            if (shouldCheckExitPath)
                outsideStr = Outside ? "outside -> inside" : "inside -> outside";

            // Check if the entrance node itself is reachable
            bool entranceIsReachable = IsDestinationReachable(from, nodePos, path);

            // Check if the exit path is valid (if needed)
            bool exitPathIsValid = !shouldCheckExitPath || IsExitPathReachable(node, exitPointCheckNode!);

            if (entranceIsReachable && exitPathIsValid)
            {
                float distance = CalculatePathLength(from, nodePos, path);
                LethalMin.Logger.LogDebug($"GMPEN: {outsideStr} Entrance Node {node.name} is reachable at distance {distance}");
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestNode = node;
                }
            }
            else
            {
                LethalMin.Logger.LogDebug($"GMPEN: {outsideStr} Entrance Node {node.name} is NOT reachable");
            }
        }

        if (bestNode == null)
        {
            LethalMin.Logger.LogDebug($"GMPEN: No {outsideStr} entrance nodes are directly reachable, defaulting to closest entrance node");
            if (entranceNodes.Count == 0)
            {
                LethalMin.Logger.LogDebug($"GMPEN: No {outsideStr} entrance nodes available!");
                return null!;
            }
            if (validNodes.Count == 0)
            {
                LethalMin.Logger.LogDebug($"GMPEN: No valid {outsideStr} entrance nodes available!");
                bestNode = entranceNodes.OrderBy(n => Vector3.Distance(from, n.GetPosition())).First();
            }
            else
            {
                bestNode = validNodes.OrderBy(n => Vector3.Distance(from, n.GetPosition())).First();
            }
            bestNode = new RouteNode(bestNode);
            bestNode.name += " (Unpathable)";
            bestNode.UnpathableOnCreation = true;
        }

        bestNode.CheckDistance = !LethalMin.UseExitsWhenCarryingItems ? 3.0f : 1.0f;

        return bestNode!;
    }

    private bool IsExitPathReachable(RouteNode node, RouteNode exitPointCheckNode)
    {
        // Check if the mapped exit point is reachable
        if (IsDestinationReachable(manager.EntranceExitPoints[node.entrancePoint].position, exitPointCheckNode.GetPosition()))
            return true;

        // Check if there's an alternate exit point that's reachable
        bool hasAlternateExitPoint = node.entrancePoint.exitPoint != null
            && manager.EntranceExitPoints[node.entrancePoint] != node.entrancePoint.exitPoint;

        if (hasAlternateExitPoint
            && IsDestinationReachable(node.entrancePoint.exitPoint!.position, exitPointCheckNode.GetPosition()))
            return true;

        return false;
    }

    protected RouteNode? GetMostPathableElevator(Vector3 from, FloorData data)
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
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(from, nodePos, NavMesh.AllAreas, path);

            if (IsDestinationReachable(from, nodePos, path))
            {
                float distance = CalculatePathLength(from, nodePos, path);
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

        if (bestNode == null && data.Elevators.Count > 0)
        {
            LethalMin.Logger.LogWarning("No elevator nodes are directly reachable, defaulting to closest elevator node");
            bestNode = data.Elevators.OrderBy(n => Vector3.Distance(from, n.GetPosition())).First();
            bestNode = new RouteNode(bestNode);
            bestNode.name += " (Unpathable)";
            bestNode.UnpathableOnCreation = true;
        }

        return bestNode;
    }

    protected RouteNode? GetMostPathableNode(Vector3 from, List<RouteNode> nodes)
    {
        RouteNode? bestNode = null;
        float bestDistance = float.MaxValue;

        foreach (RouteNode node in nodes)
        {
            Vector3 nodePos = node.GetPosition();
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(from, nodePos, NavMesh.AllAreas, path);

            if (IsDestinationReachable(from, nodePos, path))
            {
                float distance = CalculatePathLength(from, nodePos, path);
                LethalMin.Logger.LogDebug($"Node {node.name} is reachable at distance {distance}");
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestNode = node;
                }
            }
            else
            {
                LethalMin.Logger.LogDebug($"Node {node.name} is NOT reachable");
            }
        }

        if (bestNode == null && nodes.Count > 0)
        {
            LethalMin.Logger.LogWarning("No nodes are directly reachable, defaulting to closest node");
            bestNode = nodes.OrderBy(n => Vector3.Distance(from, n.GetPosition())).First();
            bestNode = new RouteNode(bestNode);
            bestNode.name += " (Unpathable)";
            bestNode.UnpathableOnCreation = true;
        }

        return bestNode;
    }

    protected float CalculatePathLength(Vector3 from, Vector3 to, NavMeshPath path = null!)
    {
        if (path == null)
        {
            path = new NavMeshPath();
            NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
        }

        float length = 0f;
        if (path.corners.Length < 2)
            return length;

        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return length;
    }

    protected Vector3 GetPathStartPos(PikminRouteRequest req)
    {
        return req.StartOverride != null ? req.StartOverride.Value : req.Pikmin.agent.transform.position;
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
                if (request.TargetOnion == null)
                {
                    LethalMin.Logger.LogWarning("No target onion specified in request!");
                    break;
                }
                foreach (RouteNode onionNode in PikminRouteManager.Instance.GetAllPossibleOnionNodes())
                {
                    if (onionNode.InstanceIdentifiyer != null && onionNode.InstanceIdentifiyer == request.TargetOnion!.ItemDropPos)
                    {
                        nodes.Add(onionNode);
                        break;
                    }
                }
                break;

            case RouteIntent.ToVehicle:
                List<RouteNode> vehicleNodes = manager.GetAllPossibleVehicleNodes(request);
                if (vehicleNodes.Count == 0)
                {
                    LethalMin.Logger.LogWarning("No vehicle nodes found!");
                    break;
                }
                RouteNode? bestVehicleNode = GetMostPathableNode(GetPathStartPos(request), vehicleNodes);
                if (bestVehicleNode != null)
                    nodes.Add(bestVehicleNode);
                break;

            case RouteIntent.ToCounter:
                RouteNode? counterNode = manager.GetPossibleCounterNode(request);
                if (counterNode != null)
                    nodes.Add(counterNode);
                break;

            case RouteIntent.ToSpecificPoint:
                nodes.Add(manager.GetSpesficPointNode(request));
                break;

            case RouteIntent.ToExit:
                RouteNode BestExitNode = GetMostPathableEntranceNode(true, GetPathStartPos(request), manager.EntranceNodes);
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
        List<RouteNode> TargetExits = context.CurrentFloor == null ? manager.EntranceNodes : context.CurrentFloor.Exits;

        RouteNode mostPathableExit = GetMostPathableEntranceNode(false, GetPathStartPos(request), TargetExits);

        if (mostPathableExit == null)
        {
            LethalMin.Logger.LogWarning("No pathable exit found, cannot create route.");
            return nodes;
        }

        PikminRouteRequest movedRequest = new PikminRouteRequest(request);
        movedRequest.StartOverride = PikminRouteManager.Instance.EntranceExitPoints[mostPathableExit.entrancePoint].position;
        LethalMin.Logger.LogDebug($"ITOS: Moved request start to {movedRequest.StartOverride}");

        List<RouteNode> outsideNodes = directOutdoor.GenerateRoute(movedRequest, context);

        if (outsideNodes.Count == 0)
        {
            LethalMin.Logger.LogWarning("No outside nodes generated, cannot create route.");
            return nodes;
        }

        RouteNode mostPathableEntrance = GetMostPathableEntranceNode(false, GetPathStartPos(request), TargetExits, outsideNodes[0]);

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
                RouteNode BestExitNode = GetMostPathableEntranceNode(false, GetPathStartPos(request), TargetExits);
                nodes.Add(BestExitNode);
                break;

            case RouteIntent.ToElevator:
                if (context.CurrentFloor == null)
                {
                    LethalMin.Logger.LogWarning("No current floor data available for elevator routing.");
                    break;
                }
                RouteNode? BestElevatorNode = GetMostPathableElevator(GetPathStartPos(request), context.CurrentFloor);
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

        RouteNode mostPathableExit = GetMostPathableEntranceNode(true, GetPathStartPos(request), manager.EntranceNodes);

        if (mostPathableExit == null)
        {
            LethalMin.Logger.LogWarning("No pathable entrance found, cannot create route.");
            return nodes;
        }

        PikminRouteRequest movedRequest = new PikminRouteRequest(request);
        movedRequest.StartOverride = PikminRouteManager.Instance.EntranceExitPoints[mostPathableExit.entrancePoint].position;
        LethalMin.Logger.LogDebug($"OTIS: Moved request start to {movedRequest.StartOverride}");

        List<RouteNode> insideNodes = directIndoor.GenerateRoute(movedRequest, context);

        if (insideNodes.Count == 0)
        {
            LethalMin.Logger.LogWarning("No inside nodes generated, cannot create route.");
            return nodes;
        }

        RouteNode mostPathableEntrance = GetMostPathableEntranceNode(true, GetPathStartPos(request), manager.EntranceNodes, insideNodes[0]);

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
        return base.CanHandle(request, context) && PikminManager.instance.CurrentMoonSettings != null 
        && PikminManager.instance.CurrentMoonSettings.CanSettingsHandlePathing(request, context);
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
        return null!;
    }
}
