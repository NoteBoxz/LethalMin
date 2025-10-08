using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LethalMin.Routeing;

public class PikminRoute
{
    public PikminRouteRequest Request { get; private set; }
    public RouteContext Context { get; private set; }
    public List<RouteNode> Nodes { get; private set; }
    public int CurrentNodeIndex { get; private set; }
    public PikminAI Pikmin => Request.Pikmin;
    public bool DontIncrumentNodeIndex = false;
    public bool HandleEntrances = true; // Whether to handle entrance nodes automatically

    // Events
    public UnityEvent<RouteNode> OnNodeReached = new UnityEvent<RouteNode>();
    public UnityEvent OnRouteComplete = new UnityEvent();
    public UnityEvent<RouteValidation.InvalidationReason> OnRouteInvalidated = new UnityEvent<RouteValidation.InvalidationReason>();

    private RouteValidation validator;
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.1f; // Update 10 times per second
    private bool createdThisFrame = true;

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
        // Validate route is still good
        RouteValidation.InvalidationReason reason = validator.ValidateCurrentRoute(this);
        if (reason != RouteValidation.InvalidationReason.None)
        {
            LethalMin.Logger.LogWarning($"Route invalidated: {reason}");
            OnRouteInvalidated?.Invoke(reason);
            return; // Let the owner decide to regenerate
        }

        // Path to current node
        lastUpdateTime = Time.time;
        if (lastUpdateTime - UPDATE_INTERVAL >= Time.time && !createdThisFrame)
            UpdatePikminPath();

        // Check if reached current node
        if (Nodes[CurrentNodeIndex].IsPikminAtNode(Request.Pikmin))
        {
            if (!Nodes[CurrentNodeIndex].Buffer())
                return; // Still buffering

            OnNodeReached?.Invoke(Nodes[CurrentNodeIndex]);

            // Handle special node types
            Nodes[CurrentNodeIndex].NodeReached(this);

            // Move to next node or finish
            if (!DontIncrumentNodeIndex)
                CurrentNodeIndex++;
            if (CurrentNodeIndex >= Nodes.Count)
            {
                OnRouteComplete?.Invoke();
            }
        }

        createdThisFrame = false;
    }


    public void UpdatePikminPath()
    {
        if (Pikmin == null) return;

        RouteNode CurNode = Nodes[CurrentNodeIndex];

        Vector3 targetPosition = CurNode.GetPosition();
        Pikmin.PathToPosition(targetPosition);
    }

    public void DestoryRoute()
    {
        OnNodeReached.RemoveAllListeners();
        OnRouteComplete.RemoveAllListeners();
        OnRouteInvalidated.RemoveAllListeners();
        Nodes.Clear();
    }
}