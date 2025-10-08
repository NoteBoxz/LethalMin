using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Routeing;

public abstract class RouteGenerationStrategy
{
    public abstract bool CanHandle(PikminRouteRequest request, RouteContext context);
    public abstract int Priority { get; } // Higher = preferred
    public abstract List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context);

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

// Example: Direct outdoor routing
public class DirectOutdoorStrategy : RouteGenerationStrategy
{
    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Can only handle if we're outside and destination is outside
        return !context.IsInside && !context.DestinationIsInside;
    }
    
    public override int Priority => 100; // Highest - simplest case
    
    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();
        
        switch (request.Intent)
        {
            case RouteIntent.ToShip:
            
                break;
            case RouteIntent.ToOnion:

                break;
        }
        
        return nodes;
    }
}

// Example: Exit-first strategy
public class ExitFirstStrategy : RouteGenerationStrategy
{
    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Handle when inside and need to go outside
        return context.IsInside && context.NeedToExitBuilding;
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
        }
        
        return nodes;
    }
}

// Example: Elevator strategy
public class ElevatorStrategy : RouteGenerationStrategy
{
    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        // Only handle if we are unable to reach an exit on the current floor
        return context.CurrentFloor != null;
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
        }
        
        return nodes;
    }
}

// Special: Moon override strategy
public class MoonOverrideStrategy : RouteGenerationStrategy
{
    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        return PikminManager.instance.CurrentMoonSettings != null && PikminManager.instance.CurrentMoonSettings.OverridePathing;
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

// Fallback strategy if nothing else works
public class FallbackStrategy : RouteGenerationStrategy
{
    public override bool CanHandle(PikminRouteRequest request, RouteContext context)
    {
        return true; // Always can handle
    }
    
    public override int Priority => 0; // Lowest priority
    
    public override List<RouteNode> GenerateRoute(PikminRouteRequest request, RouteContext context)
    {
        List<RouteNode> nodes = new List<RouteNode>();
        
        
        return nodes;
    }
}

