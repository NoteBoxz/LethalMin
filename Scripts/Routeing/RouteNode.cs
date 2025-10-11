using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Routeing;

public class RouteNode
{
    public enum NodeType
    {
        VectorPoint,
        TransformPoint,
        OutdoorEntrancePoint,
        IndoorEntrancePoint
    }

    [Header("Point Definition")]
    public string name = "RouteNode";
    public NodeType Type;
    public bool AlwaysUseNavMeshOffset = true;
    public Vector3 vectorPoint;
    public Transform transformPoint = null!;
    /// <summary>
    /// EntracePoint = Exit's own telepoint
    /// ExitPoint = Exit's destination telepoint, should be its outside/inside equivalent
    /// </summary>
    public EntranceTeleport entrancePoint = null!;
    public float NavMeshSampleRadius = 5f;
    public bool UnpathableOnCreation = false; // Whether to mark the route as unpathable when this node is created

    [Header("Check Settings")]
    public bool Skip = false;
    public float CheckBuffer;
    public float CheckDistance;
    public Collider? CheckRegion;
    public Object? InstanceIdentifiyer; // Optional, for debugging or special cases
    private float checkBufferTimer;
    public class FloatOrCollider
    {
        public float FloatValue;
        public Collider? ColliderValue;

        public FloatOrCollider(float value)
        {
            FloatValue = value;
            ColliderValue = null;
        }

        public FloatOrCollider(Collider collider)
        {
            FloatValue = 0.1f;
            ColliderValue = collider;
        }

        public static implicit operator FloatOrCollider(float value) => new FloatOrCollider(value);
        public static implicit operator FloatOrCollider(Collider collider) => new FloatOrCollider(collider);
        public static implicit operator float(FloatOrCollider foc) => foc.FloatValue;
        public static implicit operator Collider?(FloatOrCollider foc) => foc.ColliderValue;
    }


    /// <summary>
    /// Creates a RouteNode at a specific point in space.
    /// </summary>
    public RouteNode(string name, Vector3 point, FloatOrCollider check)
    {
        this.name = name;
        Type = NodeType.VectorPoint;
        vectorPoint = point;
        CheckRegion = check;
        CheckDistance = check;
    }

    /// <summary>
    /// Creates a RouteNode at a specific Transform's position.
    /// </summary>
    public RouteNode(string name, Transform point, FloatOrCollider check)
    {
        this.name = name;
        Type = NodeType.TransformPoint;
        transformPoint = point;
        CheckRegion = check;
        CheckDistance = check;
    }

    /// <summary>
    /// Creates a RouteNode at an EntranceTeleport's point.
    /// </summary>
    public RouteNode(string name, EntranceTeleport point, FloatOrCollider check)
    {
        this.name = name;
        Type = point.isEntranceToBuilding ? NodeType.OutdoorEntrancePoint : NodeType.IndoorEntrancePoint;
        entrancePoint = point;
        CheckRegion = check;
        CheckDistance = check;
    }

    public RouteNode(RouteNode other)
    {
        name = other.name;
        Type = other.Type;
        vectorPoint = other.vectorPoint;
        transformPoint = other.transformPoint;
        entrancePoint = other.entrancePoint;
        NavMeshSampleRadius = other.NavMeshSampleRadius;
        UnpathableOnCreation = other.UnpathableOnCreation;
        AlwaysUseNavMeshOffset = other.AlwaysUseNavMeshOffset;
        CheckBuffer = other.CheckBuffer;
        CheckDistance = other.CheckDistance;
        CheckRegion = other.CheckRegion;
        InstanceIdentifiyer = other.InstanceIdentifiyer;
    }

    public bool IsPikminAtNode(PikminAI ai)
    {
        if (Skip)
        {
            return true;
        }
        Vector3 Pos = GetPosition();
        if (CheckRegion != null)
        {
            bool AlsoCheckDistance = CheckDistance > 0;
            return CheckRegion.bounds.Contains(ai.agent.transform.position)
             && (!AlsoCheckDistance || Vector3.Distance(ai.agent.transform.position, Pos) <= CheckDistance);
        }
        else
        {
            return Vector3.Distance(ai.agent.transform.position, Pos) <= CheckDistance;
        }
    }

    public bool IsPikminNearNode(PikminAI ai, float distance)
    {
        Vector3 Pos = GetPosition();
        return Vector3.Distance(ai.agent.transform.position, Pos) <= distance;
    }

    public void NodeReached(PikminRoute route)
    {
        PikminAI ai = route.Request.Pikmin;
        if (route.HandleEntrances && (Type == NodeType.OutdoorEntrancePoint || Type == NodeType.IndoorEntrancePoint))
        {
            // Use the entrance teleport
            ai.UseEntranceServerRpc(entrancePoint.NetworkObject, Type == NodeType.OutdoorEntrancePoint);
        }
    }

    public Vector3 GetPosition(bool DontUseNavmesh = false)
    {
        bool shouldUseNavmesh = !DontUseNavmesh && AlwaysUseNavMeshOffset;
        Vector3 basePos = Type switch
        {
            NodeType.VectorPoint => vectorPoint,
            NodeType.TransformPoint => transformPoint.position,
            NodeType.OutdoorEntrancePoint => entrancePoint.entrancePoint.transform.position,
            NodeType.IndoorEntrancePoint => entrancePoint.entrancePoint.transform.position,
            _ => Vector3.zero
        };
        if (basePos == Vector3.zero)
        {
            LethalMin.Logger.LogWarning($"{name}: has a baseposition of vector zero!");
        }
        if (shouldUseNavmesh)
        {
            if (NavMesh.SamplePosition(basePos, out NavMeshHit hit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return basePos;

    }

    /// <summary>
    /// Buffers the node's check buffer.
    /// </summary>
    /// <returns>True if the buffer is up, false if the buffer is ongoing</returns>
    public bool Buffer()
    {
        if (CheckBuffer <= 0)
        {
            return true; // No buffer needed
        }
        if (checkBufferTimer > 0)
        {
            CheckBuffer = checkBufferTimer;

            checkBufferTimer -= Time.deltaTime;
            return false;
        }
        else
        {
            checkBufferTimer = CheckBuffer;
            return true;
        }
    }
}