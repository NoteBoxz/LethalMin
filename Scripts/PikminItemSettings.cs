using System.Collections.Generic;
using LethalMin.Pikmin;
using UnityEngine;

namespace LethalMin
{
    public class PikminItemSettings : MonoBehaviour
    {
        public int CarryStrength = -1;
        public bool CanProduceSprouts = false;
        public bool OverrideGrabbableToEnemeis = false;
        public int SproutsToSpawn = 0;
        public float PerferedTypeMultipler = 1;
        public bool DontParentWhenDropping = false;
        public bool ServerAuthParenting = false;
        public bool RouteToPlayer = false;
        public float RouteToPlayerStoppingDistance = 5f;
        public float RouteToPlayerDroppingDistance = 0f;
        public bool ChangeOwnershipOnCarry = true;
        public bool GrabableToPikmin = true;
        public bool DontInitalizeOnStartup = false;
        public PikminType PerferedType = null!;
        public Collider OverrideGrabPostionColider = null!;
        public List<Renderer> ExtraRenderers = new List<Renderer>();
    }
}