using System.Collections.Generic;
using System.Linq;
using LCOffice.Components;
using LethalMin.Compats;
using LethalMin.Patches;
using LethalMin.Utils;
using LethalMon;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace LethalMin.Pikmin
{
    public struct PikminRouteData
    {
        public (Leader, float)? TargetPlayer;
        public Onion? TargetOnion;
        public RouteNode? OverrideDestNode;
        public bool UseDoors;
    }
    public class PikminRoute
    {
        //The exitpoint on an entance teleport is the InsidePosition
        //The entrancePoint on an exit teleport is the OutsidePosition
        public RouteNode ShipNode = new RouteNode(
            "Ship",
            StartOfRound.Instance.insideShipPositions[5],
            -1,
            StartOfRound.Instance.shipInnerRoomBounds
        );
        /// <summary>
        /// Because lethal company is werid we need to get the outside door script in order to get the indoor position
        /// </summary>
        public RouteNode MainEntranceInsideNode = new RouteNode(
            "Main Entrance (Inside)",
            RoundManager.FindMainEntranceScript(true),
            4.5f
        );
        public RouteNode MainEntranceOutsideNode = new RouteNode(
            "Main Entrance (Outside)",
            RoundManager.FindMainEntrancePosition(true, true)
        );
        public RouteNode CompanyCounterNode = null!;

        List<RouteNode[]> CarPaths = new List<RouteNode[]>();
        List<RouteNode> OnionNodes = new List<RouteNode>();
        public static Collider OverrideShipBounds = null!;

        public PikminRoute(PikminItem Item)
        {
            if (Item.PrimaryPikminOnItem == null)
            {
                LethalMin.Logger.LogError($"PikminItem {Item.gameObject.name} has no primary pikmin");
                return;
            }
            if (Item.settings.RouteToPlayer && Item.PrimaryLeader != null)
            {
                RouteData.TargetPlayer = (Item.PrimaryLeader, Item.settings.RouteToPlayerStoppingDistance);
            }
            RouteData.TargetOnion = Item.TargetOnion;
            RouteData.UseDoors = false;
            Pikmin = Item.PrimaryPikminOnItem;
            IsPathOutside = Pikmin.isOutside;
            ShipNode.CheckBuffer = 1;
            CarPaths = FindCarRouteNodes(this);
            OnionNodes = FindOnionRouteNodes(this);
            GetNodes(true);
        }

        public PikminRoute(PikminAI Pikmin)
        {
            this.Pikmin = Pikmin;
            IsPathOutside = Pikmin.isOutside;
            RouteData.UseDoors = true;
            GetNodes(true);
        }

        public static List<FloorData> CurrentFloorData = new List<FloorData>();
        public static FloorData DefultFloorData = new FloorData();
        public RouteNode ExitUsedInside = null!, ExitUsedOutside = null!;
        List<NavMeshPathVisualizer> Visualizers = new List<NavMeshPathVisualizer>();
        public List<RouteNode> Nodes = new List<RouteNode>();
        public PikminRouteData RouteData = new PikminRouteData();
        public PikminAI Pikmin = null!;
        public int CurrentPathIndex = 0;
        public bool IsPathPossible;
        public bool IsPathOutside = false;
        public static bool IsGettingFloorData;
        public static List<RouteNode> FireNodes = new List<RouteNode>();
        public static List<CachedRouteNode> NodeCache = new List<CachedRouteNode>();
        public static List<MoonSettings> MoonSettingss = new List<MoonSettings>();
        private List<string> previousRouteNodeNames = new List<string>();
        private bool isFirstRoute = true;
        public UnityEvent OnPointReached = new UnityEvent();
        public UnityEvent OnRouteEnd = new UnityEvent();
        public UnityEvent<EntranceTeleport> OnReachDoor = new UnityEvent<EntranceTeleport>();


        #region Node Processing
        /// <summary>
        /// Gets the nodes for a route, depending on the item's primary pikmin and the current floor data.
        /// </summary>
        public void GetNodes(bool Debug = false)
        {
            Nodes.Clear();

            // Ship bounds override
            if (LethalMin.IsDependencyLoaded("mborsh.WiderShipMod") && !LethalMin.IsDependencyLoaded("MelanieMelicious.2StoryShip")
            && LethalMin.MakeCustomBoundsForWideShip)
            {
                if (OverrideShipBounds == null)
                {
                    GameObject BoundsObj = PikUtils.CreateDebugCube(StartOfRound.Instance.shipInnerRoomBounds.transform.position);
                    BoundsObj.transform.SetParent(StartOfRound.Instance.shipInnerRoomBounds.transform.parent);
                    GameObject.Destroy(BoundsObj.GetComponent<Renderer>());
                    OverrideShipBounds = BoundsObj.GetComponent<Collider>();
                    OverrideShipBounds.enabled = true;
                    OverrideShipBounds.isTrigger = true;
                    BoundsObj.transform.localScale = new Vector3(1.4367f, 2.281f, -6.64f);
                    BoundsObj.transform.localScale = new Vector3(17.2219f, 4.45f, 5.9745f);
                    ShipNode.CheckRegion = OverrideShipBounds;
                    LethalMin.Logger.LogInfo($"Created custom bounds for wider ship: {BoundsObj.name}");
                }
                else
                {
                    ShipNode.CheckRegion = OverrideShipBounds;
                }
            }

            if (RouteData.OverrideDestNode != null)
            {
                if (Debug)
                    LethalMin.Logger.LogInfo($"Creating route for override destination: {RouteData.OverrideDestNode.NodeName}");
                Nodes.Add(RouteData.OverrideDestNode);
                CastRoute(Debug);
                return;
            }

            //Player route
            if (RouteData.TargetPlayer != null)
            {
                RouteNode PlayerNode = new RouteNode();
                PlayerNode.NodeName = "Player";
                PlayerNode.Point = RouteData.TargetPlayer.Value.Item1.transform;
                PlayerNode.Type = RouteNode.RouteNodeType.Point;
                PlayerNode.CheckDistance = RouteData.TargetPlayer.Value.Item2;

                NodeCache.Remove(PlayerNode.cachedNode);
                PlayerNode.cachedNode = null!;
                Nodes.Add(PlayerNode);
                CastRoute(Debug);
                return;
            }

            //Company building route
            if (LethalMin.OnCompany && (!LethalMin.TakeItemsToOnionOnCompany.InternalValue || RouteData.TargetOnion == null))
            {
                if (Debug)
                    LethalMin.Logger.LogInfo($"Creating route for company building");
                if (CompanyCounterNode == null)
                {
                    NodeCache.Clear();
                    CompanyCounterNode = new RouteNode(
                        "Company Counter",
                        GameObject.FindObjectOfType<DepositItemsDesk>().triggerCollider.transform.position,
                        5f
                    );
                    NodeCache[0].CheckDistance = 10;
                }
                Nodes.Add(CompanyCounterNode);
                CastRoute(Debug);
                return;
            }

            //Moon Override Route
            if (MoonSettingss.Count > 0 && MoonSettingss.FirstOrDefault(mop => mop.Level == RoundManager.Instance.currentLevel) != null)
            {
                RouteNode? OverrideNode = CreateRouteFromMoon(IsPathOutside);
                if (OverrideNode != null)
                {
                    if (Debug)
                        LethalMin.Logger.LogInfo($"Creating route for moon override");
                    Nodes.Add(OverrideNode);
                    CastRoute(Debug);
                    return;
                }
                else if (Debug)
                {
                    LethalMin.Logger.LogWarning($"Failed to create route for moon override, using default route.");
                }
            }

            //Outside Route
            if (Pikmin != null && IsPathOutside)
            {
                if (Debug)
                    LethalMin.Logger.LogInfo($"Creating route for outside pikmin");

                Nodes = CreateRouteOutdoors();
                CastRoute(Debug);
                return;
            }

            //Inside Route No Floor Data
            if (Pikmin != null && !IsPathOutside && CurrentFloorData.Count <= 0)
            {
                if (Debug)
                    LethalMin.Logger.LogInfo($"Creating route for inside pikmin");
                Nodes = CreateRouteIndoors(false);
                CastRoute(Debug);
                return;
            }

            //Inside Route With Floor Data
            if (Pikmin != null && !IsPathOutside && CurrentFloorData.Count > 0)
            {
                if (Debug)
                    LethalMin.Logger.LogInfo($"Creating route for inside pikmin on different floor");
                Nodes = CreateRouteIndoors(true);
                CastRoute(Debug);
                return;
            }
        }

        /// <summary>
        /// Called after the nodes have been added to the route.
        /// </summary>
        public void CastRoute(bool debug = false)
        {
            // Get current route names
            List<string> currentRouteNodeNames = Nodes
                .Select(n => n == null || n.NodeName == null ? "null" : n.NodeName)
                .ToList();

            string currentRouteString = string.Join(" -> ", currentRouteNodeNames);

            // Check if route has changed
            bool routeChanged = false;
            if (isFirstRoute)
            {
                routeChanged = true;
                isFirstRoute = false;
                LethalMin.Logger.LogMessage($"Initial route created: {currentRouteString}");
            }
            else if (!AreRoutesEqual(previousRouteNodeNames, currentRouteNodeNames))
            {
                routeChanged = true;
                string previousRouteString = string.Join(" -> ", previousRouteNodeNames);
                LethalMin.Logger.LogMessage($"Route changed from: {previousRouteString}");
                LethalMin.Logger.LogMessage($"Route changed to: {currentRouteString}");
            }

            // Always log current route if debug is enabled
            if (debug)
            {
                LethalMin.Logger.LogMessage($"Current route: {currentRouteString}");
                if (!routeChanged && !isFirstRoute)
                {
                    LethalMin.Logger.LogMessage("Route unchanged");
                }
            }

            // Update previous route for next comparison
            previousRouteNodeNames = new List<string>(currentRouteNodeNames);

            //RecalculateVisualizer();
        }

        /// <summary>
        /// Updates the route item and checks if the pikmin has reached the current node
        /// </summary>
        public void UpdateRouteItem()
        {
            PikminAI ai = Pikmin;

            RouteNode CurNode = Nodes[CurrentPathIndex];

            UpdatePikminPath();

            if (CurNode.IsPikminAtNode(ai))
            {
                if (!CurNode.Buffer())
                {
                    //LethalMin.Logger.LogInfo($"Buffering at node {CurNode.NodeName} for {CurNode.CheckBuffer} seconds");
                    return;
                }

                if (!PikUtils.IsOutOfRange(Nodes, CurrentPathIndex + 1))
                {
                    CurNode.OnNodeReached(this);
                    CurrentPathIndex++;
                    OnPointReached.Invoke();
                }
                else
                {
                    LethalMin.Logger.LogInfo($"{ai.DebugID}: Reached last node and buffer time is up");
                    OnRouteEnd.Invoke();
                }
                LethalMin.Logger.LogInfo($"{ai.DebugID}: Reached node {CurNode.NodeName}");
                LethalMin.Logger.LogInfo($"{Nodes.Count} - {CurrentPathIndex}");
            }
        }

        /// <summary>
        /// Updates the route pikmin and checks if the pikmin has reached the current node
        /// </summary>
        public void UpdateRoutePikmin()
        {
            if (Pikmin == null) return;

            RouteNode CurNode = Nodes[CurrentPathIndex];

            Pikmin.agent.speed = Pikmin.pikminType.GetSpeed(Pikmin.CurrentGrowthStage, Pikmin.ShouldRun);
            UpdatePikminPath();

            if (CurNode.IsPikminAtNode(Pikmin))
            {
                if (!CurNode.Buffer())
                {
                    //LethalMin.Logger.LogInfo($"Buffering at node {CurNode.NodeName} for {CurNode.CheckBuffer} seconds");
                    return;
                }

                if (!PikUtils.IsOutOfRange(Nodes, CurrentPathIndex + 1))
                {
                    CurNode.OnNodeReached(this);
                    CurrentPathIndex++;
                }
                else
                {
                    LethalMin.Logger.LogInfo($"{Pikmin.DebugID}: Reached last node and buffer time is up");
                    Pikmin.SetToIdleServerRpc();
                }

                LethalMin.Logger.LogInfo($"{Pikmin.DebugID}: Reached node {CurNode.NodeName}");
                LethalMin.Logger.LogInfo($"{Nodes.Count} - {CurrentPathIndex}");
            }
        }

        /// <summary>
        /// Paths the Pikmin on the route
        /// </summary>
        public void UpdatePikminPath()
        {
            if (Pikmin == null) return;

            RouteNode CurNode = Nodes[CurrentPathIndex];

            Vector3? targetPosition = CurNode.GetNodePosition();
            if (targetPosition.HasValue)
            {
                Vector3 target = targetPosition.Value;
                Pikmin.PathToPosition(target);
            }
        }

        /// <summary>
        /// Should be called before setting a route as null
        /// </summary>
        public void DestoryRoute()
        {
            LethalMin.Logger.LogInfo($"{Pikmin?.DebugID}: Destroying route");

            foreach (var visualizer in Visualizers)
            {
                foreach (var item in visualizer.pathPoints)
                {
                    GameObject.Destroy(item);
                }
                GameObject.Destroy(visualizer.gameObject);
            }

            Visualizers.Clear();
            Nodes.Clear();
        }
        #endregion





        #region Node Fetching
        /// <summary>
        /// Creates a route for a Pikmin carrying an item outdoors, determining the optimal destination.
        /// This method determines the best destination for a Pikmin carrying an item by:
        /// 1. Checking if the item should go to a specific onion (if the item and target onion are defined)
        /// 2. Finding the shortest pathable route to either a vehicle or the ship
        /// 3. Prioritizing onion delivery when a target onion is specified
        /// </summary>
        /// <returns>
        /// A List of RouteNodes representing the path for the Pikmin to follow.
        /// </returns>
        public List<RouteNode> CreateRouteOutdoors()
        {
            List<RouteNode> Nodes = new List<RouteNode>();
            bool ShouldGoToOnion = LethalMin.TakeItemsToTheOnion && RouteData.TargetOnion != null;

            // Determine the best destination (ship or vehicle)
            RouteNode[]? selectedCarPath = null;

            RouteNode? OnionNode = null;

            // Check if a car path is better
            if (CarPaths.Count > 0 && !ShouldGoToOnion)
            {
                // Get Pikmin position for pathfinding
                Vector3 pikminPosition = Pikmin.transform.position;

                // Start with the ship as default destination
                float shortestDistance = float.MaxValue;
                Vector3? shipPos = ShipNode.GetNodePosition();

                if (shipPos.HasValue && Is2PointsPathable(pikminPosition, shipPos.Value))
                {
                    shortestDistance = CalculatePathLength(pikminPosition, shipPos.Value);
                }


                foreach (RouteNode[] carPath in CarPaths)
                {
                    if (carPath.Length == 0) continue;

                    Vector3? carPos = carPath[0].GetNodePosition();
                    if (!carPos.HasValue) continue;

                    if (Is2PointsPathable(pikminPosition, carPos.Value))
                    {
                        float distance = CalculatePathLength(pikminPosition, carPos.Value);
                        if (distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            selectedCarPath = carPath;
                        }
                    }
                }
            }

            // Check if an onion path is better
            if (ShouldGoToOnion && RouteData.TargetOnion != null)
            {
                Vector3 pos = RouteData.TargetOnion.ItemDropPos.position;
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    pos = hit.position;
                }
                OnionNode = new RouteNode($"Onion ({RouteData.TargetOnion.onionType.TypeName})", pos, 0.1f);
                OnionNode.InstanceIdentifiyer = RouteData.TargetOnion.ItemDropPos;
            }

            // Add the appropriate destination to the route
            if (OnionNode != null)
            {
                // if (Debug)
                //     LethalMin.Logger.LogInfo($"Creating route to onion: {OnionNode.NodeName}");
                Nodes.Add(OnionNode);
            }
            else if (selectedCarPath != null)
            {
                // if (Debug)
                //     LethalMin.Logger.LogInfo($"Creating route to vehicle: {selectedCarPath[0].NodeName}");
                Nodes.AddRange(selectedCarPath);
            }
            else
            {
                // if (Debug)
                //     LethalMin.Logger.LogInfo($"Creating route to ship");
                Nodes.Add(ShipNode);
            }

            return Nodes;
        }


        /// <summary>
        /// Creates a route for Pikmin to follow indoors, with or without floor data.
        /// This method determines the best path from the Pikmin's current position to the ship or elevator,
        /// considering available entrances, exits, and floor information if available.
        /// </summary>
        /// <param name="useFloorData">Whether to use floor data for route creation</param>
        /// <returns>
        /// A List of RouteNodes representing the path for the Pikmin to follow.
        /// </returns>
        public List<RouteNode> CreateRouteIndoors(bool useFloorData = false)
        {
            const int PRIORITIZE_EXIT = 0;
            const int PRIORITIZE_ELEVATOR = 1;
            bool ShouldGoOutside = LethalMin.CanPathOutsideWhenInside.InternalValue;
            bool ShouldGoToOnion = LethalMin.TakeItemsToTheOnion && RouteData.TargetOnion != null;

            //Local Nodes list
            List<RouteNode> Nodes = new List<RouteNode>();
            if (Pikmin == null)
            {
                return Nodes;
            }

            // Get route nodes based on whether we're using floor data
            List<RouteNode> routeNodes = new List<RouteNode>();
            FloorData? floorData = null;

            if (useFloorData)
            {
                floorData = GetFloorPikminIsOn(Pikmin);
                if (floorData == null)
                {
                    LethalMin.Logger.LogWarning($"PikItemRoute: Unable to get floor data for Pikmin {Pikmin.DebugID}!");
                    return Nodes;
                }

                routeNodes.AddRange(floorData.MainExits);
                routeNodes.AddRange(floorData.FireExits);
                routeNodes.AddRange(floorData.Elevators);
            }
            else
            {
                routeNodes.AddRange(FireNodes);
                routeNodes.Add(MainEntranceInsideNode);
            }

            // Sort route nodes
            int priority = ShouldGoOutside ? PRIORITIZE_EXIT : PRIORITIZE_ELEVATOR;

            routeNodes.Sort((a, b) =>
            {
                Vector3? aPos = a.GetNodePosition();
                Vector3? bPos = b.GetNodePosition();

                // When using floor data, consider node type based on priority
                if (useFloorData && priority == PRIORITIZE_EXIT)
                {
                    if (a.Type == RouteNode.RouteNodeType.Door && b.Type != RouteNode.RouteNodeType.Door)
                        return -1;
                    if (a.Type != RouteNode.RouteNodeType.Door && b.Type == RouteNode.RouteNodeType.Door)
                        return 1;
                }

                // For both cases, compare distances
                if (aPos.HasValue && bPos.HasValue)
                {
                    return CalculatePathLength(Pikmin.transform.position, aPos.Value)
                        .CompareTo(CalculatePathLength(Pikmin.transform.position, bPos.Value));
                }

                return 0;
            });

            //Get the ship or vehicle position, (whichever is closer)
            RouteNode TargetEndRouteNode = null!;
            List<RouteNode>? TargetEndRouteNodes = null;
            RouteNode[] SelectedCarPath = null!;
            bool AllMissing = false;

            // Set up target end routes - same for both methods
            if (CarPaths.Count > 0 && !ShouldGoToOnion)
            {
                TargetEndRouteNodes = new List<RouteNode>();
                TargetEndRouteNodes.Add(ShipNode);
                foreach (RouteNode[] routeNodeArray in CarPaths)
                {
                    TargetEndRouteNodes.Add(routeNodeArray[0]);
                }

                List<RouteNode> TMPTargetEndRouteNodes = new List<RouteNode>(TargetEndRouteNodes);
                foreach (RouteNode node in TMPTargetEndRouteNodes)
                {
                    Vector3? pos = node.GetNodePosition();
                    if (!pos.HasValue)
                    {
                        LethalMin.Logger.LogWarning($"Removing node {node.NodeName} from target nodes due to missing position");
                        TargetEndRouteNodes.Remove(node);
                    }
                }

                if (TargetEndRouteNodes.Count <= 0)
                {
                    AllMissing = true;
                }
            }
            else if (ShouldGoToOnion && RouteData.TargetOnion != null)
            {
                Vector3 pos = RouteData.TargetOnion.ItemDropPos.position;
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    pos = hit.position;
                }
                RouteNode node = new RouteNode($"Onion ({RouteData.TargetOnion.onionType.TypeName})", pos, 0.1f);
                node.InstanceIdentifiyer = RouteData.TargetOnion.ItemDropPos;
                TargetEndRouteNode = node;
                ShouldGoOutside = true;
            }
            else
            {
                TargetEndRouteNode = ShipNode;
                AllMissing = !ShipNode.GetNodePosition().HasValue;
            }

            //Target nodes
            RouteNode? TargetIndoorNode = null;
            RouteNode? TargetOutdoorNode = null;
            RouteNode? DefaultTargetIndoorNode = null;
            RouteNode? DefaultTargetOutdoorNode = null;
            RouteNode? TargetElevatorNode = null;
            RouteNode? DefaultTargetElevatorNode = null;

            //Set TargetNodes to the last used ones if not null
            if (ExitUsedInside != null)
            {
                TargetIndoorNode = ExitUsedInside;
            }
            if (ExitUsedOutside != null)
            {
                TargetOutdoorNode = ExitUsedOutside;
            }

            // Pre-calculate the most likely target end node (outside the foreach loop)
            if (TargetEndRouteNodes != null && !AllMissing && ShouldGoOutside)
            {
                // Sort target nodes by straight-line distance to Pikmin
                TargetEndRouteNodes.Sort((a, b) =>
                {
                    Vector3? aPos = a.GetNodePosition();
                    Vector3? bPos = b.GetNodePosition();
                    if (aPos.HasValue && bPos.HasValue)
                    {
                        return Vector3.Distance(Pikmin.transform.position, aPos.Value)
                            .CompareTo(Vector3.Distance(Pikmin.transform.position, bPos.Value));
                    }
                    return 0;
                });

                // Pre-select the best candidate end node
                if (TargetEndRouteNodes.Count > 0)
                {
                    TargetEndRouteNode = TargetEndRouteNodes[0];

                    // If it's a car node, find the matching car path
                    if (TargetEndRouteNode != ShipNode)
                    {
                        foreach (RouteNode[] carPath in CarPaths)
                        {
                            if (carPath[0] == TargetEndRouteNode)
                            {
                                SelectedCarPath = carPath;
                                break;
                            }
                        }
                    }
                }
            }

            // Process each route node to find targets
            foreach (RouteNode node in routeNodes)
            {
                // Handle door nodes
                if (node.Type == RouteNode.RouteNodeType.Door)
                {
                    Vector3? nodePosition = node.GetNodePosition();
                    Vector3? outdoorPosition = node.GetNodePosition(true);

                    if (AllMissing && ShouldGoOutside)
                    {
                        LethalMin.Logger.LogError($"Failed to get ship position, unable to do calculations");
                        break;
                    }

                    if (!nodePosition.HasValue || !outdoorPosition.HasValue && ShouldGoOutside)
                    {
                        continue;
                    }
                    if (TargetIndoorNode != null && TargetOutdoorNode != null)
                    {
                        break;
                    }

                    bool Check1 = false, Check2 = false;

                    // Indoor path check
                    Check1 = Is2PointsPathable(Pikmin.transform.position, nodePosition.Value, 5);

                    // Outdoor path check - simplified to only check the pre-selected target
                    if (ShouldGoOutside)
                    {
                        Check2 = Is2PointsPathable(outdoorPosition, TargetEndRouteNode.GetNodePosition(), 5);

                        // If this door can't path to our pre-selected target, try the ship as fallback
                        if (!Check2 && TargetEndRouteNode != ShipNode)
                        {
                            Check2 = Is2PointsPathable(outdoorPosition, ShipNode.GetNodePosition(), 5);
                            if (Check2)
                            {
                                // Use ship as fallback
                                TargetEndRouteNode = ShipNode;
                                SelectedCarPath = null!;
                                LethalMin.Logger.LogWarning($"Door node {node.NodeName} can't path to the selected target, using ship as fallback.");
                            }
                        }
                    }
                    else
                    {
                        // When not going outside, indoor pathability is sufficient
                        Check2 = true;
                    }

                    // Set default targets if needed
                    if (DefaultTargetIndoorNode == null && DefaultTargetOutdoorNode == null)
                    {
                        DefaultTargetIndoorNode = node;
                        DefaultTargetOutdoorNode = new RouteNode(
                            $"{node.NodeName} (Outoor)",
                            (node.Entrance?.entrancePoint != null ?
                            node.Entrance.entrancePoint.position : node.GetNodePosition(true)) ?? Vector3.zero
                        );
                        DefaultTargetOutdoorNode.AutoSkip = true;
                    }

                    // Set targets if checks pass
                    if (Check1 && Check2 || Check1 && !ShouldGoOutside)
                    {
                        TargetIndoorNode = node;
                        TargetOutdoorNode = new RouteNode(
                            $"{node.NodeName} (Outoor)",
                            (node.Entrance?.entrancePoint != null ?
                            node.Entrance.entrancePoint.position : node.GetNodePosition(true)) ?? Vector3.zero
                        );
                        TargetOutdoorNode.AutoSkip = true;
                        break;
                    }
                }
                // Handle elevator nodes (for floor data only)
                else if (useFloorData && node.Type != RouteNode.RouteNodeType.Door)
                {
                    Vector3? nodePosition = node.GetNodePosition();
                    if (!nodePosition.HasValue)
                    {
                        continue;
                    }

                    bool Check1 = Is2PointsPathable(Pikmin.transform.position, nodePosition.Value, 5);

                    if (DefaultTargetElevatorNode == null)
                    {
                        DefaultTargetElevatorNode = node;
                    }

                    if (Check1)
                    {
                        TargetElevatorNode = node;
                        break;
                    }
                }
            }

            // Build the final route nodes list
            if (ShouldGoOutside)
            {
                // Case 1: We have valid indoor and outdoor nodes
                if (TargetIndoorNode != null && TargetOutdoorNode != null)
                {
                    Nodes.Add(TargetIndoorNode);
                    Nodes.Add(TargetOutdoorNode);

                    // Add end target nodes
                    if (SelectedCarPath != null)
                    {
                        Nodes.AddRange(SelectedCarPath);
                    }
                    else
                    {
                        Nodes.Add(TargetEndRouteNode);
                    }
                    return Nodes;
                }
                // Case 2: Use elevator if available when using floor data
                else if (useFloorData && TargetElevatorNode != null)
                {
                    Nodes.Add(TargetElevatorNode);
                    return Nodes;
                }
                // Case 3: Fallback to defaults for indoor/outdoor path
                else
                {
                    LethalMin.Logger.LogDebug($"No suitable route node found for pikmin on item. Using default route.");

                    // Try indoor default
                    if (DefaultTargetIndoorNode == null)
                    {
                        // Try elevator default if floor data is being used
                        if (useFloorData && DefaultTargetElevatorNode != null)
                        {
                            Nodes.Add(DefaultTargetElevatorNode);
                            LethalMin.Logger.LogDebug($"Using default elevator node.");
                            return Nodes;
                        }
                        else
                        {
                            LethalMin.Logger.LogDebug($"No default route node found");
                            return Nodes;
                        }
                    }
                    else if (DefaultTargetOutdoorNode == null)
                    {
                        // If we have indoor but no outdoor, just use indoor
                        Nodes.Add(DefaultTargetIndoorNode);
                        LethalMin.Logger.LogDebug($"Using only default indoor node (no outdoor node).");
                        return Nodes;
                    }
                    else
                    {
                        // Both defaults available
                        Nodes.Add(DefaultTargetIndoorNode);
                        Nodes.Add(DefaultTargetOutdoorNode);

                        // Add end target nodes
                        if (SelectedCarPath != null)
                        {
                            Nodes.AddRange(SelectedCarPath);
                        }
                        else
                        {
                            Nodes.Add(TargetEndRouteNode);
                        }
                        return Nodes;
                    }
                }
            }
            else
            {
                // When we can't path outside from inside
                if (TargetIndoorNode != null)
                {
                    Nodes.Add(TargetIndoorNode);
                    return Nodes;
                }
                else if (useFloorData && TargetElevatorNode != null)
                {
                    Nodes.Add(TargetElevatorNode);
                    return Nodes;
                }
                else
                {
                    LethalMin.Logger.LogDebug($"No suitable route node found for pikmin on item. Using default route.");

                    if (DefaultTargetIndoorNode != null)
                    {
                        Nodes.Add(DefaultTargetIndoorNode);
                        return Nodes;
                    }
                    else if (useFloorData && DefaultTargetElevatorNode != null)
                    {
                        Nodes.Add(DefaultTargetElevatorNode);
                        return Nodes;
                    }
                }
            }

            if (Nodes.Count == 0)
            {
                LethalMin.Logger.LogWarning("Unexpected scenario: No valid route found.");
            }

            return Nodes;
        }


        /// <summary>
        /// Creates a route for Pikmin to follow based on the current moon override paths.
        /// </summary>
        /// <param name="outDoors"></param>
        /// <returns></returns>
        public RouteNode? CreateRouteFromMoon(bool outDoors)
        {
            // Check Override Paths
            MoonSettings? CurrentMOP = MoonSettingss.FirstOrDefault(mop => mop.Level == RoundManager.Instance.currentLevel);
            if (CurrentMOP == null)
            {
                LethalMin.Logger.LogWarning($"No moon override path found for level {RoundManager.Instance.currentLevel}");
                return null;
            }

            List<RouteNode> nodes = new List<RouteNode>();

            // Get the appropriate node collections based on whether we're outdoors or indoors
            List<RouteNode> baseNodes = outDoors ?
                new List<RouteNode>(CurrentMOP.OutdoorRouteNodes) :
                new List<RouteNode>(CurrentMOP.IndoorRouteNodes);

            List<Transform> pointTransforms = outDoors ?
                new List<Transform>(CurrentMOP.OutdoorPoints) :
                new List<Transform>(CurrentMOP.IndoorPoints);

            bool checkPathable = outDoors ?
                CurrentMOP.CheckPathableOutdoor :
                CurrentMOP.CheckPathableIndoor;

            string locationLabel = outDoors ? "Outdoor" : "Indoor";

            // Add transform-based nodes
            foreach (Transform pointTransform in pointTransforms)
            {
                RouteNode node = new RouteNode(
                    $"Override {locationLabel} Point ({pointTransform.gameObject.name})",
                    pointTransform,
                    0.1f
                );
                baseNodes.Add(node);
            }

            // Filter out non-pathable or null positions
            foreach (RouteNode node in baseNodes)
            {
                Vector3? pos = node.GetNodePosition();
                if (pos == null || (checkPathable && !Is2PointsPathable(Pikmin.transform.position, pos, 5)))
                {
                    continue;
                }
                if (node.cachedNode != null && !NodeCache.Contains(node.cachedNode))
                {
                    LethalMin.Logger.LogInfo($"Adding forgoten node {node.cachedNode.NodeName} to cache");
                    NodeCache.Add(node.cachedNode);
                }
                nodes.Add(node);
            }

            // Sort nodes by distance to pikmin
            nodes.Sort((a, b) =>
            {
                Vector3? aPos = a.GetNodePosition();
                Vector3? bPos = b.GetNodePosition();

                if (!aPos.HasValue && !bPos.HasValue) return 0;
                if (!aPos.HasValue) return 1;
                if (!bPos.HasValue) return -1;

                return CalculatePathLength(Pikmin.transform.position, aPos.Value)
                    .CompareTo(CalculatePathLength(Pikmin.transform.position, bPos.Value));
            });

            if (nodes.Count == 0)
            {
                LethalMin.Logger.LogWarning($"No valid nodes found for {locationLabel} override path.");
            }
            return nodes.Count > 0 ? nodes[0] : null;
        }

        /// <summary>
        /// Gets the nodes for a route, depending on the item's primary pikmin and the current floor data.
        /// </summary>
        public static void GetFloorData()
        {
            if (IsGettingFloorData)
            {
                LethalMin.Logger.LogWarning($"GetFloorData is already in progress.");
                return;
            }
            CurrentFloorData.Clear();
            DefultFloorData = null!;
            IsGettingFloorData = true;

            bool IsPiggyDungen()
            {
                return GameObject.FindAnyObjectByType<ElevatorSystem>() != null;
            }

            bool IsVanillaDungen()
            {
                return RoundManager.Instance.currentMineshaftElevator != null;
            }

            if (IsVanillaDungen())
            {
                GetVanillaFloorData();
                IsGettingFloorData = false;
                return;
            }

            if (LethalMin.IsDependencyLoaded("Piggy.LCOffice") && IsPiggyDungen())
            {
                LethalMin.Logger.LogInfo($"Piggy LC-Office detected, getting floor data.");
                GetPiggyFloorData();
                IsGettingFloorData = false;
                return;
            }

            LethalMin.Logger.LogDebug($"did not find any flor data");

            IsGettingFloorData = false;
            return;
        }

        /// <summary>
        /// Gets the floor data from the vanilla mineshaft interior.
        /// </summary>
        public static void GetVanillaFloorData()
        {
            RouteNode MainNode = new RouteNode(
                "Main",
                RoundManager.FindMainEntranceScript(true),
                0.45f
            );

            GameObject CustomBounds = GameObject.CreatePrimitive(PrimitiveType.Cube);
            CustomBounds.GetComponent<Collider>().isTrigger = true;
            CustomBounds.transform.SetParent(RoundManager.Instance.currentMineshaftElevator.GetComponentInChildren<PlayerPhysicsRegion>().transform.parent);
            CustomBounds.transform.localPosition = new Vector3(0.0001f, 0.7978f, 0f);
            CustomBounds.transform.localScale = new Vector3(2f, 4f, 2f);
            CustomBounds.GetComponent<Renderer>().enabled = false;
            CustomBounds.AddComponent<DirectlyPathZone>();
            //CustomBounds.GetComponent<Renderer>().material = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Materials/MapDotA.mat");
            CustomBounds.name = "Pikmin Elevator Bounds";


            RouteNode ElevatorNode = new RouteNode(
                "Elevator",
                RoundManager.Instance.currentMineshaftElevator.transform,
                -1,
                CustomBounds.GetComponent<Collider>()
            );

            ElevatorNode.CheckBuffer = 0.25f;
            MineshaftElevatorControllerPatch.node = ElevatorNode;

            FloorData F1 = new FloorData();
            F1.MainExits.Add(MainNode);
            F1.FloorRoot = RoundManager.FindMainEntrancePosition();
            F1.Elevators.Add(ElevatorNode);
            F1.FloorTitle = "(Floor1) Entrance";
            CurrentFloorData.Add(F1);

            DefultFloorData = F1;

            FloorData F2 = new FloorData();
            F2.FireExits.AddRange(FindFireExitRouteNodes());
            F2.FloorRoot = RoundManager.Instance.currentMineshaftElevator.elevatorBottomPoint.position;
            F2.Elevators.Add(ElevatorNode);
            F2.FloorTitle = "(Floor2) Mineshaft";
            CurrentFloorData.Add(F2);

            LethalMin.Logger.LogInfo("Registered Vanilla Minshaft Floors");
        }


        /// <summary>
        /// Gets the floor data from the Piggy LC-Office mod.
        /// </summary>
        public static void GetPiggyFloorData()
        {
            List<RouteNode> FireExits = FindFireExitRouteNodes();
            ElevatorSystem ElevatorSystem = GameObject.FindObjectOfType<ElevatorSystem>();
            PlayerPhysicsRegion ElevatorRegion = ElevatorSystem.animator.GetComponentInChildren<PlayerPhysicsRegion>();
            Scene currentScene = SceneManager.GetSceneByName(RoundManager.Instance.currentLevel.sceneName);

            GameObject CreateDebugCube(Vector3 LocalPos)
            {
                //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                GameObject cube = new GameObject("Floor Ref Pos");
                SceneManager.MoveGameObjectToScene(cube, currentScene);
                cube.transform.SetParent(ElevatorSystem.animator.transform.parent);
                cube.transform.localPosition = LocalPos;
                //cube.GetComponent<Renderer>().material = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Materials/MapDotA.mat");
                return cube;
            }

            RouteNode MainNode = new RouteNode(
                "Main",
                RoundManager.FindMainEntranceScript(true),
                0.45f
            );

            RouteNode ElevatorNode = new RouteNode(
                "Elevator",
                ElevatorSystem.animator.transform,
                -1,
                ElevatorRegion.GetComponent<Collider>()
            );
            if (!ElevatorRegion.gameObject.TryGetComponent(out DirectlyPathZone zone))
            {
                zone = ElevatorRegion.gameObject.AddComponent<DirectlyPathZone>();
            }

            ElevatorNode.CheckBuffer = 0.25f;
            ElevatorNode.GetNavPos = true;
            PiggyElevatorSystemPatch.ElevateNode = ElevatorNode;

            FloorData F1 = new FloorData();
            GameObject cubeA = CreateDebugCube(new Vector3(-2.32f, -11.41f, 1.01f));
            F1.FloorRoot = cubeA.transform.position;
            F1.Elevators.Add(ElevatorNode);
            F1.FloorTitle = "(Floor1) Basement";
            CurrentFloorData.Add(F1);

            FloorData F2 = new FloorData();
            GameObject cubeB = CreateDebugCube(new Vector3(-2.32f, 22.09f, 1.01f));
            F2.FloorRoot = cubeB.transform.position;
            F2.Elevators.Add(ElevatorNode);
            F2.MainExits.Add(MainNode);
            F2.FloorTitle = "(Floor2) Lobby";
            CurrentFloorData.Add(F2);
            DefultFloorData = F2;

            FloorData F3 = new FloorData();
            GameObject cubeC = CreateDebugCube(new Vector3(-2.32f, 61.44f, 1.01f));
            F3.FloorRoot = cubeC.transform.position;
            F3.Elevators.Add(ElevatorNode);
            F3.FloorTitle = "(Floor3) Upstairs Basement";
            CurrentFloorData.Add(F3);

            GameObject PonlyZone = PikUtils.CreateDebugCube(cubeC.transform.position + new Vector3(0, 1.5f, 0));
            PonlyZone.transform.localScale = new Vector3(4.3f, 4.25f, 7f);
            PonlyZone.GetComponent<Collider>().isTrigger = true;
            PonlyZone.GetComponent<Collider>().enabled = true;
            PonlyZone.AddComponent<PikminOnlyZone>();
            PonlyZone.GetComponent<Renderer>().enabled = false;
            PonlyZone.name = "LC-Office Pikmin Only Zone";
            SceneManager.MoveGameObjectToScene(PonlyZone, currentScene);
            PiggyElevatorSystemPatch.POnlyZone = PonlyZone;

            if (LethalMin.AddNavLinkToThridFloorOffice)
            {
                GameObject link = new GameObject();
                link.name = $"LethalMinFloor3Link";
                SceneManager.MoveGameObjectToScene(link, currentScene);

                GameObject LcubeA = new GameObject(); //PikUtils.CreateDebugCube(new Vector3(0, 0, 0));
                GameObject LcubeB = new GameObject(); //PikUtils.CreateDebugCube(new Vector3(0, 0, 0));
                LcubeA.name = "LethalMinLinkCubeA";
                LcubeB.name = "LethalMinLinkCubeB";
                SceneManager.MoveGameObjectToScene(LcubeA, currentScene);
                SceneManager.MoveGameObjectToScene(LcubeB, currentScene);
                LcubeA.transform.SetParent(link.transform);
                LcubeB.transform.SetParent(link.transform);

                link.transform.SetParent(ElevatorSystem.animator.transform);
                link.transform.localPosition = new Vector3(2.19f, -3.62f, 0.015f);
                link.transform.localRotation = Quaternion.Euler(0, 90, 0);

                NavMeshLink Sasueage = link.AddComponent<NavMeshLink>();

                Sasueage.width = 3f;
                Sasueage.startPoint = new Vector3(0, 0, -1);
                Sasueage.endPoint = new Vector3(0, 0, 2);

                PiggyElevatorSystemPatch.Link = Sasueage;
                PiggyElevatorSystemPatch.DebugCubeA = LcubeA;
                PiggyElevatorSystemPatch.DebugCubeB = LcubeB;
            }

            FloorData DetermineFloor(GameObject obj)
            {
                Vector3 objPosition = obj.transform.position;
                return CurrentFloorData.OrderBy(floor =>
                    Mathf.Abs(objPosition.y - floor.FloorRoot.y))
                    .FirstOrDefault();
            }

            foreach (RouteNode exit in FireExits)
            {
                if (exit.Entrance == null)
                {
                    LethalMin.Logger.LogWarning($"({exit.NodeName}) Entrance is null, skipping exit registration.");
                    continue;
                }
                if (exit.Entrance.exitPoint == null)
                {
                    exit.Entrance.FindExitPoint();
                }
                GameObject? Point = exit.Entrance.exitPoint?.gameObject;
                if (!exit.Entrance.gotExitPoint)
                {
                    Point = FindEntranceExitPoint(exit.Entrance);
                }
                if (Point == null)
                {
                    LethalMin.Logger.LogWarning($"({exit.NodeName}) Exit point is null, skipping exit registration.");
                    continue;
                }

                FloorData floor = DetermineFloor(Point);
                LethalMin.Logger.LogDebug($"({exit.NodeName}) Floor: {floor.FloorTitle} Position: {Point.transform.position}");
                CurrentFloorData[CurrentFloorData.IndexOf(floor)].FireExits.Add(exit);
            }

            LethalMin.Logger.LogInfo("Registered LC-Office Floors");
        }
        #endregion





        #region Utils
        public static FloorData? GetFloorPikminIsOn(PikminAI pikmin)
        {
            FloorData FloorOn;
            FloorOn = null!;
            if (CurrentFloorData.Count == 0)
            {
                return null;
            }

            FloorData currentFloor = CurrentFloorData.OrderBy(floor =>
                    Mathf.Abs(pikmin.transform.position.y - floor.FloorRoot.y))
                    .FirstOrDefault();

            if (currentFloor != null)
            {
                //LethalMin.Logger.LogInfo($"({pikmin.DebugID}) Current floor: {currentFloor.FloorTitle}");
            }
            else
            {
                LethalMin.Logger.LogWarning($"({pikmin.DebugID}) No valid floor found for Pikmin");
            }

            if (currentFloor != null)
            {
                FloorOn = currentFloor;
            }
            return currentFloor;
        }
        public static List<EntranceTeleport> FindFireExits(Transform transform = null!)
        {
            EntranceTeleport[] allEntrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            List<EntranceTeleport> allExits = new List<EntranceTeleport>();
            foreach (EntranceTeleport entrance in allEntrances)
            {
                if (!entrance.isEntranceToBuilding)
                {
                    continue;
                }
                if (entrance.entranceId != 0)
                {
                    allExits.Add(entrance);
                }
            }
            if (allExits.Count == 0)
            {
                return new List<EntranceTeleport>();
            }
            if (transform != null)
            {
                return allExits.OrderBy(exit => Vector3.Distance(transform.position, exit.transform.position)).ToList();
            }
            else
            {
                return allExits;
            }
        }
        public static List<RouteNode> FindFireExitRouteNodes(Transform transform = null!)
        {
            List<RouteNode> routeNodes = new List<RouteNode>();
            List<EntranceTeleport> allExits = FindFireExits(transform);
            foreach (EntranceTeleport exit in allExits)
            {
                RouteNode node = new RouteNode($"FireExit ({allExits.IndexOf(exit)})", exit, 4.5f);
                routeNodes.Add(node);
            }
            return routeNodes;
        }
        public static List<RouteNode[]> FindCarRouteNodes(PikminRoute route = null!)
        {
            List<RouteNode[]> routeNodes = new List<RouteNode[]>();
            PikminAI? ai = route.Pikmin;
            if (ai == null)
            {
                return routeNodes;
            }
            if (!LethalMin.TakeItemsToTheCar)
            {
                return routeNodes;
            }
            foreach (PikminVehicleController Vcontroller in PikminManager.instance.Vehicles)
            {
                if (Vcontroller == null)
                {
                    LethalMin.Logger.LogError($"Vehicle controller is null");
                    continue;
                }
                if (Vcontroller.controller.magnetedToShip)
                {
                    continue;
                }
                if (!Vcontroller.controller.backDoorOpen)
                {
                    continue;
                }

                if (!Vcontroller.PikminCheckRegion.bounds.Contains(ai.transform.position))
                {
                    RouteNode[] array = new RouteNode[2];
                    RouteNode nodeA = new RouteNode($"Car WarpPoints ({Vcontroller.name})", (Vcontroller.PikminWarpPoint, Vcontroller.PointsRegion.transform));
                    RouteNode nodeB = new RouteNode($"Car ({Vcontroller.name})", Vcontroller.PointsRegion.transform, -1, Vcontroller.PikminCheckRegion);
                    nodeB.CheckBuffer = 1;
                    nodeA.CheckDistance = 1;
                    array[0] = nodeA;
                    array[1] = nodeB;
                    routeNodes.Add(array);
                }
                else
                {
                    RouteNode[] array = new RouteNode[1];
                    RouteNode nodeB = new RouteNode($"Car ({Vcontroller.name})", Vcontroller.PointsRegion.transform, -1, Vcontroller.PikminCheckRegion);
                    nodeB.CheckBuffer = 1;
                    array[0] = nodeB;
                    routeNodes.Add(array);
                }
            }
            return routeNodes;
        }
        public static List<RouteNode> FindOnionRouteNodes(PikminRoute route = null!)
        {
            List<RouteNode> routeNodes = new List<RouteNode>();
            PikminAI? ai = route.Pikmin;
            foreach (Onion on in PikminManager.instance.Onions)
            {
                Vector3 pos = on.transform.position;
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                {
                    pos = hit.position;
                }
                RouteNode node = new RouteNode($"Onion ({on.onionType.TypeName})", pos, 0.1f);
                routeNodes.Add(node);
            }
            return routeNodes;
        }
        public static GameObject? FindEntranceExitPoint(EntranceTeleport tp)
        {
            EntranceTeleport[] array = Object.FindObjectsOfType<EntranceTeleport>();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].isEntranceToBuilding != tp.isEntranceToBuilding && array[i].entranceId == tp.entranceId)
                {
                    return array[i].entrancePoint.gameObject;
                }
            }
            return null;
        }

        public static float CalculatePathLength(Vector3 start, Vector3 end)
        {
            //Unused due to inaccuracy

            // NavMeshPath path = new NavMeshPath();
            // if (NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
            // {
            //     float pathLength = 0f;
            //     for (int i = 1; i < path.corners.Length; i++)
            //     {
            //         pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            //     }
            //     return pathLength;
            // }

            return Vector3.Distance(start, end);
        }
        public static bool Is2PointsPathable(Vector3? start, Vector3? end, float SampleRadius = 5)
        {
            if (!start.HasValue || !end.HasValue)
            {
                return false;
            }
            return Is2PointsPathable(start.Value, end.Value, SampleRadius);
        }
        public static bool Is2PointsPathable(Vector3 start, Vector3 end, float SampleRadius = 5)
        {
            NavMeshPath path = new NavMeshPath();

            NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);

            return path.status == NavMeshPathStatus.PathComplete;
        }
        private bool AreRoutesEqual(List<string> route1, List<string> route2)
        {
            if (route1.Count != route2.Count)
                return false;

            for (int i = 0; i < route1.Count; i++)
            {
                if (route1[i] != route2[i])
                    return false;
            }

            return true;
        }
        #endregion





        #region Visualizers
        private Vector3 lastStartPos;
        private List<Vector3> lastEndPositions = new List<Vector3>();
        private const float updateThreshold = 0f; // Minimum distance for update

        public void RecalculateVisualizer()
        {
            if (Nodes.Count == 0) return;

            // Get the starting position (PrimaryPikminOnItem's position) for the first node
            Vector3 rawStartPos = Pikmin.transform.position;

            // Check if the start position has changed significantly
            if (Vector3.Distance(rawStartPos, lastStartPos) < updateThreshold)
            {
                return; // Skip recalculation if the start position hasn't changed much
            }

            lastStartPos = rawStartPos;

            // Ensure the lastEndPositions list has the same size as Nodes
            while (lastEndPositions.Count < Nodes.Count)
            {
                lastEndPositions.Add(Vector3.zero);
            }

            bool pathsChanged = false;

            // Remove visualizers for nodes below CurrentPathIndex
            for (int i = 0; i < CurrentPathIndex && i < Visualizers.Count; i++)
            {
                RemoveVisualizer(0); // Remove from the beginning of the list
                pathsChanged = true;
            }

            // Update or create visualizers for remaining nodes
            for (int i = CurrentPathIndex; i < Nodes.Count; i++)
            {
                RouteNode currentNode = Nodes[i];
                Vector3? rawEndPos = currentNode.GetNodePosition();
                if (!rawEndPos.HasValue) continue;

                // For the first active node, use the item/pikmin position as start
                // For subsequent nodes, use the previous node's position as start
                Vector3 startPos = (i == CurrentPathIndex) ? rawStartPos : Nodes[i - 1].GetNodePosition(true) ?? rawStartPos;

                //LethalMin.Logger.LogInfo($"Calculating path for {currentNode.NodeName}: {startPos} -> {rawEndPos.Value}");

                // Sample the start position on the NavMesh
                if (!NavMesh.SamplePosition(startPos, out NavMeshHit startHit, 5f, NavMesh.AllAreas))
                {
                    startHit.position = startPos;
                }
                startPos = startHit.position;

                // Sample the end position on the NavMesh
                if (!NavMesh.SamplePosition(rawEndPos.Value, out NavMeshHit endHit, 5f, NavMesh.AllAreas))
                {
                    endHit.position = rawEndPos.Value;
                }
                Vector3 endPos = endHit.position;

                // Check if the end position has changed significantly
                if (Vector3.Distance(endPos, lastEndPositions[i]) >= updateThreshold)
                {
                    lastEndPositions[i] = endPos;
                    pathsChanged = true;

                    // Calculate the path
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, path))
                    {
                        UpdateOrCreateVisualizer(i - CurrentPathIndex, path);
                    }
                    else
                    {
                        RemoveVisualizer(i - CurrentPathIndex);
                    }
                }
            }

            // Remove any excess visualizers
            while (Visualizers.Count > Nodes.Count - CurrentPathIndex)
            {
                RemoveVisualizer(Visualizers.Count - 1);
            }

            if (pathsChanged)
            {
                //LethalMin.Logger.LogInfo("Paths recalculated due to significant changes.");
            }
        }

        private void UpdateOrCreateVisualizer(int index, NavMeshPath path)
        {
            NavMeshPathVisualizer visualizer;
            if (index < Visualizers.Count)
            {
                visualizer = Visualizers[index];
            }
            else
            {
                GameObject visPrefab = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/DebugObjects/NavVis.prefab");
                GameObject visInstance = GameObject.Instantiate(visPrefab);
                visualizer = visInstance.GetComponent<NavMeshPathVisualizer>();
                Visualizers.Add(visualizer);
            }

            if (path == null)
            {
                visualizer.gameObject.SetActive(false);
                return;
            }
            else
            {
                visualizer.gameObject.SetActive(true);
            }
            visualizer.SetPath(path);
        }

        private void RemoveVisualizer(int index)
        {
            if (index < Visualizers.Count)
            {
                if (Visualizers[index] != null)
                {
                    GameObject.Destroy(Visualizers[index].gameObject);
                }
                Visualizers.RemoveAt(index);
            }
        }
        #endregion
    }

}