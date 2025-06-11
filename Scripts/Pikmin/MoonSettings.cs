using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;
using LethalMin.Utils;

namespace LethalMin.Pikmin
{
    [CreateAssetMenu(fileName = "PikminMoonSettings", menuName = "Pikmin/MoonSettings", order = 0)]
    public class MoonSettings : ScriptableObject
    {
        public SelectableLevel Level = null!;
        public List<Transform> IndoorPoints = new List<Transform>();
        public List<Transform> OutdoorPoints = new List<Transform>();
        public List<RouteNode> IndoorRouteNodes = new List<RouteNode>();
        public List<RouteNode> OutdoorRouteNodes = new List<RouteNode>();
        public bool UseIndoorRouteNodes = true;
        public bool UseOutdoorRouteNodes = true;
        public bool CheckPathableIndoor = true;
        public bool CheckPathableOutdoor = true;
    }
}