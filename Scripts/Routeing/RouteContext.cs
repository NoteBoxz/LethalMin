using UnityEngine;

namespace LethalMin.Routeing;


public class RouteContext
{
    // Location info
    public bool IsInside;
    public bool IsInShip;
    public FloorData? CurrentFloor;
    
    // Destination info  
    public bool DestinationIsInside;
    public bool DestinationIsInShip;
    
    // Calculated flags
    public bool NeedToExitBuilding => IsInside && !DestinationIsInside;
    public bool NeedToEnterBuilding => !IsInside && DestinationIsInside;
}