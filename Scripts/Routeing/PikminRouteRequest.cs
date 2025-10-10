using UnityEngine;

namespace LethalMin.Routeing;

public enum RouteIntent
{
    ToShip,           // Get to the ship by any means
    ToOnion,          // Get to a specific onion
    ToPlayer,         // Follow/reach a player
    ToCounter,        // Company building counter
    ToVehicle,        // Any open vehicle
    ToExit,           // Just get outside (for ReturnToShip when inside)
    ToElevator,       // Get inside elevator (internal navigation)
    ToSpecificPoint   // Custom destination
}

public class PikminRouteRequest
{
    public RouteIntent Intent;
    public PikminAI Pikmin = null!;
    public PikminItem? CarriedItem;

    // Intent-specific data
    public Onion? TargetOnion;           // For ToOnion
    public Leader? TargetPlayer;         // For ToPlayer
    public Vector3 CustomDestination;   // For ToSpecificPoint
    public Transform? CustomTransform;   // For ToSpecificPoint
    public Vector3? StartOverride; // Optional start position override

    // Constraints
    public bool HandleEntrances = true; // Whether to handle entrance nodes automatically
    public float CustomCheckDistance = 1;  // Only used for ToPlayer and ToSpecificPoint
    public Collider CustomCheckCollider = null!; // Optional collider to use for ToSpecificPoint

    public PikminRouteRequest(PikminRouteRequest other)
    {
        Intent = other.Intent;
        Pikmin = other.Pikmin;
        CarriedItem = other.CarriedItem;
        TargetOnion = other.TargetOnion;
        TargetPlayer = other.TargetPlayer;
        CustomDestination = other.CustomDestination;
        CustomTransform = other.CustomTransform;
        StartOverride = other.StartOverride;
        HandleEntrances = other.HandleEntrances;
        CustomCheckDistance = other.CustomCheckDistance;
        CustomCheckCollider = other.CustomCheckCollider;
    }

    public PikminRouteRequest()
    {
        
    }
}