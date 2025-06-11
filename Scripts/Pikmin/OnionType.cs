using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System;

namespace LethalMin.Pikmin
{
    [CreateAssetMenu(fileName = "OnionType", menuName = "Pikmin/Onion", order = 0)]
    public class OnionType : ScriptableObject
    {
        [Header("Distinguishing Information")]
        [Tooltip("The onion's color")]
        public Color OnionColor = Color.gray;

        [Tooltip("The name of the onion's type.")]
        public string TypeName = "Untitled Onion";

        [Tooltip("The model that overrides the onion's defult model (will be set to defult if empty)")]
        public GameObject OnionOverrideModelPrefab = null!;

        [Tooltip("The model that overrides the onion item's defult model. (will be set to defult if empty)")]
        public GameObject OnionItemOverrideModelPrefab = null!;

        [Header("Onion Stats")]
        [Tooltip("The types of pikmin that the onion can hold.")]
        public PikminType[] TypesCanHold = null!;

        [Tooltip("If set to false, Pikmin will not beable to bring back bodies to this onion.")]
        public bool CanCreateSprouts = true;


        [Header("Spawning")]
        
        [Tooltip("Spawns the onion indoors")]
        public bool SpawnsIndoors = true;

        [Tooltip("Spawns the onion outdoors")]
        public bool SpawnsOutdoors;

        [Tooltip("The minimum day this onion can spawn on. Set to -1 to spawn on day 1.")]
        public int SpawnAfterDay = -1;

        [Tooltip("Spawns on moons with a power level grater or equal to this")]
        public float PreferedPowerLevel = 0f;

        [Tooltip("The chance that this onion will spawn on a moon. (0-1)")]
        public float SpawnChanceMultiplier = 1f;
        
        [Tooltip("Activates the onion item when it is taken outside")]
        public bool ActivatesWhenBroughtOutside = true;

        [Tooltip("Activates when a player gets close to the onion")]
        public bool ActivatesWhenPlayerIsNear;

        [Tooltip("(Only works when ActivatesWhenPlayerIsNear is true) The distance from the player that the onion will activate at.")]
        [Range(0, 100)]
        public float ActivationDistance = 10f;

        [Tooltip("The amount of time it takes for the onion to actvate once an activation condistion has is true. (in seconds)")]
        public float ActivationTime = 5f;

        [Tooltip("Allows Pikmin Types that target this onion, that are not in the TypesCanHold array, to be added to the onion.")]
        public bool AllowPikminToBeAddedToOnion = true;
        
        [Tooltip("If set to false this onion will not beable to fuse with any other onion, even if it's in a fuse rule.")]
        public bool CanBeFusedWith = true;

        [Tooltip("Set by mod, do not change.")]
        [HideInInspector]
        public OnionFuseRules CurFueseRules = null!;

        [Tooltip("Set by mod, do not change.")]
        [HideInInspector]
        public int OnionTypeID;
    }
}