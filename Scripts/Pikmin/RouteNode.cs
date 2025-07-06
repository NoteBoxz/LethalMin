using LethalMin.Utils;
using UnityEngine;

namespace LethalMin.Pikmin
{
    public class RouteNode
    {
        /// <summary>
        /// Point: A position a pikmin will go to an will move to the next node once reaching it.
        /// Door: An EntranceTeleport that a Pikmin will use to go outside before moving to the next node.
        /// </summary>
        public enum RouteNodeType
        {
            Point,
            Door,
            TwoWayWarp
        }
        public RouteNodeType Type;
        public string NodeName = "Node";
        public Vector3? VectorPoint;
        public Transform? Point;
        public EntranceTeleport? Entrance;
        public (Vector3?, Vector3?) VectorTwoWayWarp;
        public (Transform, Transform) TwoWayWarp;
        public float CheckDistance;
        public Collider? CheckRegion;
        public float CheckBuffer;
        public bool ShouldBeEndingPoint = false;
        public bool AutoSkip = false;
        public bool GetNavPos = false;
        public bool SkipCacheWhenCanPathOutsideWhenInside = false;
        private float? initalCheckBuffer;
        public CachedRouteNode cachedNode = null!;
        public Object? InstanceIdentifiyer = null;


        /// <summary>
        /// Empty: Creates a new RouteNode with no position or entrance.
        /// </summary>
        public RouteNode()
        {

        }

        /// <summary>
        /// Vector Point: Creates a new RouteNode with a position point.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="vPoint"></param>
        /// <param name="checkDistance"></param>
        /// <param name="checkRegion"></param>
        public RouteNode(string Name, Vector3 vPoint, float checkDistance = 0.1f, Collider checkRegion = null!)
        {
            NodeName = Name;
            Type = RouteNodeType.Point;
            VectorPoint = vPoint;
            CheckDistance = checkDistance;
            CheckRegion = checkRegion;
            OnCreate();
        }

        /// <summary>
        /// Transform Point: Creates a new RouteNode with a transform point.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="point"></param>
        /// <param name="checkDistance"></param>
        /// <param name="checkRegion"></param>
        public RouteNode(string Name, Transform point, float checkDistance = 0.1f, Collider checkRegion = null!)
        {
            NodeName = Name;
            Type = RouteNodeType.Point;
            Point = point;
            CheckDistance = checkDistance;
            CheckRegion = checkRegion;
            OnCreate();
        }

        /// <summary>
        /// Entrance: Creates a new RouteNode with an entrance teleport.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="point"></param>
        /// <param name="checkDistance"></param>
        /// <param name="checkRegion"></param>
        public RouteNode(string Name, EntranceTeleport point, float checkDistance = 0.1f, Collider checkRegion = null!)
        {
            NodeName = Name;
            Type = RouteNodeType.Door;
            Entrance = point;
            CheckDistance = checkDistance;
            CheckRegion = checkRegion;
            SkipCacheWhenCanPathOutsideWhenInside = true;
            OnCreate();
        }

        /// <summary>
        /// Two Way Vector Warp: Creates a new RouteNode with two Vector3 points for a two-way warp.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="vPoints"></param>
        /// <param name="checkDistance"></param>
        /// <param name="checkRegion"></param>
        public RouteNode(string Name, (Vector3?, Vector3?) vPoints, float checkDistance = 0.1f, Collider checkRegion = null!)
        {
            NodeName = Name;
            Type = RouteNodeType.TwoWayWarp;
            VectorTwoWayWarp = vPoints;
            CheckDistance = checkDistance;
            CheckRegion = checkRegion;
            OnCreate();
        }

        /// <summary>
        /// Two Way Transform Warp: Creates a new RouteNode with two Transform points for a two-way warp.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="points"></param>
        /// <param name="checkDistance"></param>
        /// <param name="checkRegion"></param>
        public RouteNode(string Name, (Transform, Transform) points, float checkDistance = 0.1f, Collider checkRegion = null!)
        {
            NodeName = Name;
            Type = RouteNodeType.TwoWayWarp;
            TwoWayWarp = points;
            CheckDistance = checkDistance;
            CheckRegion = checkRegion;
            OnCreate();
        }

        public void OnCreate()
        {
            foreach (CachedRouteNode cachedRouteNode in PikminRoute.NodeCache)
            {
                if (cachedRouteNode.NodeName == "(Cached)" + NodeName)
                {
                    return;
                }
            }
            cachedNode = new CachedRouteNode(this);
            cachedNode.SkipWhenCanPathOutsideWhenInside = SkipCacheWhenCanPathOutsideWhenInside;
            PikminRoute.NodeCache.Add(cachedNode);
        }

        public bool IsPikminNearNode(PikminAI ai, float Range)
        {
            if (AutoSkip)
            {
                return true;
            }
            Vector3? Pos = GetNodePosition();
            if (Pos == null)
            {
                return false;
            }
            if (CheckDistance > 0)
            {
                return Vector3.Distance(ai.transform.position, Pos.Value) <= Range + CheckDistance;
            }
            else if (CheckRegion != null)
            {
                // For regions, we can expand the bounds by the range
                Bounds expandedBounds = CheckRegion.bounds;
                expandedBounds.Expand(Range * 2); // Expand by range in all directions
                return expandedBounds.Contains(ai.transform.position);
            }
            else
            {
                return false;
            }
        }


        public bool IsPikminAtNode(PikminAI ai)
        {
            if (AutoSkip)
            {
                return true;
            }
            Vector3? Pos = GetNodePosition();
            if (Pos == null)
            {
                return false;
            }
            if (CheckDistance > 0)
            {
                //LethalMin.Logger.LogInfo($"Checking distance for {NodeName} {Vector3.Distance(ai.transform.position, Pos.Value)}");
                return Vector3.Distance(ai.transform.position, Pos.Value) <= CheckDistance;
            }
            else if (CheckRegion != null)
            {
                //LethalMin.Logger.LogInfo($"Checking region for {NodeName} {CheckRegion.bounds.Contains(ai.transform.position)}");
                return CheckRegion.bounds.Contains(ai.transform.position);
            }
            else
            {
                //LethalMin.Logger.LogError($"{NodeName}: Invalid route node, no check distance or region provided!");
                return false;
            }
        }

        public void OnNodeReached(PikminRoute route)
        {
            if (Type == RouteNodeType.Door && Entrance != null)
            {
                route.ExitUsedInside = this;
                if (!PikUtils.IsOutOfRange(route.Nodes, route.CurrentPathIndex + 1) && route.Nodes[route.CurrentPathIndex + 1].AutoSkip)
                {
                    route.ExitUsedOutside = route.Nodes[route.CurrentPathIndex + 1];
                }
                if (route.RouteData.UseDoors && route.Pikmin != null)
                {
                    route.Pikmin.UseEntranceServerRpc(Entrance.NetworkObject, false);
                }
                route.OnReachDoor.Invoke(Entrance);
            }

            if (Type == RouteNodeType.TwoWayWarp)
            {
                Vector3? pos = GetNodePosition(true);
                if (pos.HasValue)
                    route.Pikmin?.agent.Warp(RoundManager.Instance.GetNavMeshPosition(pos.Value));
            }
        }

        public Vector3? GetNodePosition(bool UseWarpPosition = false)
        {
            Vector3? Pos = null!;
            if (UseWarpPosition && (Type == RouteNodeType.TwoWayWarp || Type == RouteNodeType.Door))
            {
                return GetWarpPosition();
            }
            if (VectorTwoWayWarp.Item1 != null)
            {
                Pos = VectorTwoWayWarp.Item1.Value;
            }
            else if (TwoWayWarp.Item1 != null)
            {
                Pos = TwoWayWarp.Item1.position;
            }
            else if (VectorPoint != null)
            {
                Pos = VectorPoint.Value;
            }
            else if (Point != null)
            {
                Pos = Point.position;
            }
            else if (Entrance != null)
            {
                if (Entrance.exitPoint == null)
                {
                    Entrance.FindExitPoint();
                }
                Pos = Entrance.exitPoint?.position;
            }
            if (Pos == null)
            {
                //LethalMin.Logger.LogError($"{NodeName}: Invalid route node, no point or entrance provided!");
                return null;
            }
            else
            {
                if (GetNavPos)
                {
                    Pos = RoundManager.Instance.GetNavMeshPosition(Pos.Value);
                }
                return Pos;
            }
        }

        public Vector3? GetWarpPosition()
        {
            Vector3? Pos = null;
            if (VectorTwoWayWarp.Item2 != null)
            {
                Pos = VectorTwoWayWarp.Item2.Value;
            }
            else if (TwoWayWarp.Item2 != null)
            {
                Pos = TwoWayWarp.Item2.position;
            }
            else if (Entrance != null && Entrance.entrancePoint != null)
            {
                Pos = Entrance.entrancePoint.position;
            }

            if (GetNavPos && Pos != null)
            {
                Pos = RoundManager.Instance.GetNavMeshPosition(Pos.Value);
            }
            return Pos;
        }

        /// <summary>
        /// Buffers the node's check buffer.
        /// </summary>
        /// <returns>True if the buffer is up, false if the buffer is ongoing</returns>
        public bool Buffer()
        {
            if (CheckBuffer > 0)
            {
                if (initalCheckBuffer == null)
                    initalCheckBuffer = CheckBuffer;

                CheckBuffer -= Time.deltaTime;
                return false;
            }
            else
            {
                if (initalCheckBuffer != null)
                    CheckBuffer = initalCheckBuffer.Value;
                return true;
            }
        }
    }
}