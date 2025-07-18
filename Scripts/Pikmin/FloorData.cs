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
    public class FloorData
    {
        public Vector3 FloorRoot;
        public string FloorTitle = "Floor";
        public List<RouteNode> Elevators = new List<RouteNode>();
        public List<RouteNode> MainExits = new List<RouteNode>();
        public List<RouteNode> FireExits = new List<RouteNode>();
        public List<RouteNode> AlterntiveExits = new List<RouteNode>();
    }
}