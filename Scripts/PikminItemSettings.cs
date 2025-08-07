using System.Collections.Generic;
using LethalMin.Pikmin;
using UnityEngine;

namespace LethalMin
{
    public class PikminItemSettings : MonoBehaviour
    {
        [Tooltip("How many Pikmin are required to carry this item. -1 means to auto calculate based on weight")]
        public int CarryStrength = -1;
        [Tooltip("If true, this item will be taken to the onion to produce sprouts")]
        public bool CanProduceSprouts = false;
        [Tooltip("If true, Pikmin will beable to grab this item even if GrabableToEnemies is false on the grabable component")]
        public bool OverrideGrabbableToEnemeis = false;
        [Tooltip("How many sprouts to spawn when this item is taken to the onion. (will be auto set with CarryStrength if CarryStrength is -1)")]
        public int SproutsToSpawn = 0;
        [Tooltip("How many more sprouts will spawn if the item is taken to the onion of the preferred type, (SproutsToSpawn * PerferedTypeMultipler)")]
        public float PerferedTypeMultipler = 1;
        [Tooltip("If true, the item will not be reparented when dropped")]
        public bool DontParentWhenDropping = false;
        [Tooltip("If true, the server will be the authority on parenting when dropped")]
        public bool ServerAuthParenting = false;
        [Tooltip("If true, pikmin will take this item to the player")]
        public bool RouteToPlayer = false;
        [Tooltip("The NavMeshAgent Stopping distance when routing to the player")]
        public float RouteToPlayerStoppingDistance = 5f;
        [Tooltip("The distance from the player at which the item will be dropped when routing to the player")]
        public float RouteToPlayerDroppingDistance = 0f;
        [Tooltip("If true, the item will change ownership to the Primary Pikmin's leader when carried")]
        public bool ChangeOwnershipOnCarry = true;
        [Tooltip("If true, the item can be grabbed by Pikmin")]
        public bool GrabableToPikmin = true;
        [Tooltip("If true, the pikmin will not consider this item when looking for items to grab, basically the same as setting GrabableToPikmin to false, but pikmin won't drop it if they are already carrying it")]
        public bool ExcludeFromGetItemsCheck = false;
        [Tooltip("If true, the item will not be initialized on startup with a counter and grab positions.")]
        public bool DontInitalizeOnStartup = false;
        [Tooltip("The preferred type of Pikmin that will give the preferred type multiplier when brought to the onion")]
        public PikminType PerferedType = null!;
        [Tooltip("The colider that will be used for determining the grab positions")]
        public Collider OverrideGrabPostionColider = null!;
        [Tooltip("Used for the white glow effect when brought to the onion")]
        public List<Renderer> ExtraRenderers = new List<Renderer>();
    }
}