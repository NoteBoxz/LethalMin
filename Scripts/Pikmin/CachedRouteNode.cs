using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DunGen.Graph;
using LethalMin.Utils;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder.Csg;

namespace LethalMin.Pikmin
{
    public class CachedRouteNode
    {
        public string NodeName;
        public Vector3? Pos;
        public EntranceTeleport? Entrance;
        public Collider? CheckRegion;
        public float CheckDistance;
        public bool SkipWhenCanPathOutsideWhenInside;
        public bool DontDoInRangeCheck;
        public CachedRouteNode(RouteNode routeNode)
        {
            NodeName = "(Cached)" + routeNode.NodeName;
            if (routeNode.Type == RouteNode.RouteNodeType.Door)
            {
                Entrance = routeNode.Entrance;
            }
            else
            {
                Pos = routeNode.GetNodePosition();
            }
            CheckRegion = routeNode.CheckRegion;
            CheckDistance = routeNode.CheckDistance;
        }
        public CachedRouteNode(string nodeName, Vector3? position, Collider? bounds, float distance)
        {
            NodeName = "(Cached)" + nodeName;
            Pos = position;
            CheckRegion = bounds;
            CheckDistance = distance;

        }
        public bool IsInRange(PikminItem itm, bool OverrideSkipInsideCheck = false)
        {
            if (itm.settings.CanProduceSprouts && PikminManager.instance.Onions.Count > 0)
            {
                if (NodeName == "(Cached)Ship")
                {
                    return false;
                }
            }
            return IsInRange(itm.transform.position, OverrideSkipInsideCheck);
        }
        public bool IsInRange(Vector3 position, bool OverrideSkipInsideCheck = false)
        {
            if (DontDoInRangeCheck)
            {
                return false;
            }
            if (Entrance != null && Entrance.exitPoint != null)
            {
                if (Entrance.exitPoint == null)
                {
                    Entrance.FindExitPoint();
                }
                Pos = Entrance.exitPoint?.position;
            }
            if (!OverrideSkipInsideCheck && SkipWhenCanPathOutsideWhenInside && LethalMin.CanPathOutsideWhenInside)
            {
                return false;
            }
            if (CheckDistance > 0 && Pos != null)
            {
                //LethalMin.Logger.LogInfo($"Checking distance for {NodeName} {Vector3.Distance(position, Pos.Value)} ({SkipWhenCanPathOutsideWhenInside})");
                return Vector3.Distance(position, Pos.Value) <= CheckDistance;
            }
            else if (CheckRegion != null)
            {
                //LethalMin.Logger.LogInfo($"Checking region for {NodeName} {CheckRegion.bounds.Contains(position)} ({SkipWhenCanPathOutsideWhenInside})");
                return CheckRegion.bounds.Contains(position);
            }
            else
            {
                LethalMin.Logger.LogError($"{NodeName}: Invalid route node cache, no check distance or region provided!");
                PikminItemRoute.NodeCache.Remove(this);
                return false;
            }
        }
        public float GetDist(Vector3 position)
        {
            if (Entrance != null && Entrance.exitPoint != null)
            {
                if (Entrance.exitPoint == null)
                {
                    Entrance.FindExitPoint();
                }
                Pos = Entrance.exitPoint?.position;
            }
            if (CheckDistance > 0 && Pos != null)
            {
                return Vector3.Distance(position, Pos.Value);
            }
            else
            {
                return -1;
            }
        }
    }
}