using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Routeing;

public class RouteValidation
{
    private float lastValidationTime = Time.time;
    private const float VALIDATION_INTERVAL = 1f; // Check every second, not every frame

    public enum InvalidationReason
    {
        None,
        NodeBecameUnreachable,
        PikminTeleported,
        DestinationChanged,
        DoorClosed,
        ElevatorMoved
    }

    public InvalidationReason ValidateCurrentRoute(PikminRoute route)
    {
        if (Time.time - lastValidationTime < VALIDATION_INTERVAL)
            return InvalidationReason.None;

        lastValidationTime = Time.time;

        // Check if current target node is still reachable
        RouteNode currentNode = route.Nodes[route.CurrentNodeIndex];
        if (!IsDestinationReachable(route.Pikmin.agent.transform.position, currentNode.GetPosition()))
            return InvalidationReason.NodeBecameUnreachable;

        return InvalidationReason.None;
    }

    public bool IsDestinationReachable(Vector3 from, Vector3 to)
    {
        NavMeshPath path = new NavMeshPath();
        Vector3 startPos = from;
        Vector3 endPos = to;
        

        bool foundPath = NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, path);
        return foundPath && path.status == NavMeshPathStatus.PathComplete;
    }
}