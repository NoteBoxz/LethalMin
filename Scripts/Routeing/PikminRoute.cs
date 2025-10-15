using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LethalMin.Routeing;

public class PikminRoute
{
    public PikminRouteRequest Request { get; private set; }
    public RouteContext Context { get; private set; }
    public List<RouteNode> Nodes { get; private set; } = new List<RouteNode>();
    public int CurrentNodeIndex { get; private set; }
    public PikminAI Pikmin => Request.Pikmin;
    public RouteNode CurNode => Nodes[CurrentNodeIndex];
    public bool DontIncrumentNodeIndex = false;
    public bool HandleEntrances = true; // Whether to handle entrance nodes automatically
    public bool Pathable = true; // Whether the route is pathable by the Pikmin
    public bool IsFullPath => Pathable && Nodes.Count > 0;

    // Events
    public UnityEvent<RouteNode> OnNodeReached = new UnityEvent<RouteNode>();
    public UnityEvent OnRouteComplete = new UnityEvent();
    public UnityEvent<RouteValidation.InvalidationReason> OnRouteInvalidated = new UnityEvent<RouteValidation.InvalidationReason>();

    private RouteValidation validator;
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.1f; // Update 10 times per second
    private bool createdThisFrame = true;
    private bool FiredRouteComplete = false;

    public PikminRoute(PikminRouteRequest request, RouteContext context, List<RouteNode> nodes)
    {
        Request = request;
        Context = context;
        Nodes = nodes;
        HandleEntrances = request.HandleEntrances;
        validator = new RouteValidation();
    }

    public void Update()
    {
        if (Nodes.Count == 0)
        {
            LethalMin.Logger.LogWarning("Route has no nodes! Auto Ending...");

            if (!FiredRouteComplete)
                OnRouteComplete?.Invoke();

            FiredRouteComplete = true;

            return;
        }

        // Validate route is still good
        RouteValidation.InvalidationReason reason = validator.ValidateCurrentRoute(this);
        if (reason != RouteValidation.InvalidationReason.None)
        {
            //LethalMin.Logger.LogWarning($"Route invalidated on node {CurNode.name}: {reason}");
            OnRouteInvalidated?.Invoke(reason);
            Pathable = false;
            return; // Let the owner decide to regenerate
        }
        else
        {
            Pathable = true;
        }

        // Path to current node
        if (lastUpdateTime - UPDATE_INTERVAL <= Time.time || createdThisFrame)
        {
            UpdatePikminPath();
            lastUpdateTime = Time.time;
        }

        // Check if reached current node
        if (CurNode.IsPikminAtNode(Request.Pikmin))
        {
            if (!CurNode.Buffer())
                return; // Still buffering

            OnNodeReached?.Invoke(CurNode);

            CurNode.NodeReached(this);

            // Move to next node or finish
            if (!DontIncrumentNodeIndex)
                CurrentNodeIndex++;

            LethalMin.Logger.LogDebug($"Pikmin reached node {CurrentNodeIndex}/{Nodes.Count}");

            if (CurrentNodeIndex >= Nodes.Count && !FiredRouteComplete)
            {
                OnRouteComplete?.Invoke();
                FiredRouteComplete = true;

                LethalMin.Logger.LogInfo("Pikmin route complete.");
            }
        }

        createdThisFrame = false;
    }


    public void UpdatePikminPath()
    {
        if (Pikmin == null) return;

        Pikmin.PathToPosition(CurNode.GetPosition());
    }

    public void DestoryRoute()
    {
        OnNodeReached.RemoveAllListeners();
        OnRouteComplete.RemoveAllListeners();
        OnRouteInvalidated.RemoveAllListeners();
        Nodes.Clear();
    }
}