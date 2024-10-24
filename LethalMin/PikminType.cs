using UnityEngine;
using LethalMin;
using Unity.Netcode;
using System;
namespace LethalMin
{
    [CreateAssetMenu(menuName = "LethalMin/PikminType", order = 1)]
    public class PikminType : ScriptableObject
    {
        [Header("Distinguishing Information")]
        [Tooltip("The pikmin's color")]
        public Color PikminColor;

        [Tooltip("The pikmin's Secondary color")]
        public Color PikminColor2;

        [Tooltip("The pikmin's icon")]
        public Sprite? PikminIcon;

        [Tooltip("The pikmin's the Idle Glow Sprite")]
        public Sprite? PikminGlow;
        public bool ReplaceGlowFXwithDefult = true;
        [Tooltip("The pikmin's the sprout path, should be the object right before the plant objects (will be filled automatically if empty)")]
        public string PikminGlowPath;

        [Tooltip("The pikmin's custom AI scripts")]
        public NetworkBehaviour[]? PikminScripts;

        [Tooltip("In the pikmin's prefab, the path of the GameObject that contains the pikmin's mesh.")]
        public GameObject? MeshPrefab;

        [Tooltip("If null, the sprout will use the mesh prefab")]
        public GameObject? SproutMeshPrefab;

        [Tooltip("Set by mod, do not change.")]
        [HideInInspector]
        public int PikminTypeID;

        [Tooltip("The pikmin's sound pack.")]
        public PikminSoundPack? soundPack;


        [Tooltip("The path of the pikmin's plants in the pikmin's prefab.")]
        public string[] GrowthStagePaths = { "", "", "" };

        [Tooltip("The path to the pikmin's animations")]
        public string AnimPath = "";

        [Tooltip("The pikmin's name affix. (i.e. (RTypeName: Red) (NameAffix: Pikmin) = 'Red Pikmin') ")]
        public string NameAffix = " Pikmin";


        [Tooltip("The name of the pikmin's type.")]
        public string TypeName = "";


        [Header("Pikmin Stats")]
        [Tooltip("Whether the pikmin is resistant to fire")]
        public bool IsResistantToFire;

        [Tooltip("Whether the pikmin is resistant to electricity")]
        public bool IsResistantToElectricity;

        [Tooltip("Whether the pikmin is resistant to water")]
        public bool IsResistantToWater;

        [Tooltip("Whether the pikmin is resistant to crushing")]
        public bool IsResistantToCrushing;

        [Tooltip("Whether the pikmin is resistant to exsplosions")]
        public bool IsresistantToExsplosions;

        [Tooltip("Whether the pikmin can latch on to enemies")]
        public bool CanLatchOnToEnemies = true;

        [Tooltip("Whether the pikmin can carry items")]
        public bool CanCarryItems = true;

        [Tooltip("Whether the pikmin can attack without latching on")]
        public bool CanAttackWithoutLatchingOn = true;

        [Tooltip("The speeds of the pikmin at different growth stages")]
        public float[] Speeds = { 10, 20, 35 };

        [Tooltip("The force with which the pikmin is thrown")]
        public float ThrowForce;

        [Tooltip("Multiplier for the pikmin's growth speed")]
        public float GrowSpeedMultipler = 1;

        [Tooltip("Multiplier for the pikmin's throw force")]
        public float ThrowForceMultiplier = 1;

        [Tooltip("Multiplier for the pikmin's damage")]
        public float DamageMultiplier = 1;

        [Tooltip("Speed multipliers for different growth stages")]
        public float[] SpeedMultipliers = { 1, 1, 1 };

        [Tooltip("The amount of damage the pikmin does when it attacks.")]
        public float DamageAmmount;

        [Tooltip("Normalizes the damage the pikmin does when it attacks, (i.e. 5 -> 0.05)")]
        public bool NormalizeDamage;

        [Tooltip("Adds ten pikmin to the number of pikmin carrying the item")]
        public bool AddTen;

        [Tooltip("The ammount of distance the pikmin can detect items")]
        public float ItemDetectionRange = 5f;

        [Header("Spawning")]

        [Tooltip("Whether the pikmin can spawn in from Lethal Company's spawn system")]
        public bool SpawnsNaturally = true;

        [Tooltip("Whether the pikmin can spawn indoors")]
        public bool SpawnsIndoors;

        [Tooltip("Whether the pikmin can spawn outdoors")]
        public bool SpawnsOutdoors;

        [Tooltip("Whether the pikmin spawns as a sprout")]
        public bool SpawnsAsSprout;

        [Tooltip("Whether the pikmin uses a pikmin container")]
        public bool UsesPikminContainer;

        [Header("Other")]
        [Tooltip("Whether the pikmin instantly enters the onion")]
        public bool InstaEnterOnion;

        [Tooltip("Multiplier for the pikmin's spawn chance")]
        public float SpawnChanceMultiplier = 1;

        [Tooltip("To Prevent pikmin from instantly dropping the item right outside of the ship, this varible is used to delay the drop in the ship buffer.")]
        public float DropItemInShipBuffer = 1;

        [Tooltip("Minimum knockback resistance")]
        public int MinKnockBackResistance;

        [Tooltip("Maximum knockback resistance")]
        public int MaxKnockBackResistance;

        [Tooltip("Material for the pikmin")]
        public Material? PikminMaterial;

        [Tooltip("Material for the pikmin sprout")]
        public Material? SproutMaterial;

        [Tooltip("The type of onion this pikmin targets")]
        public OnionType TargetOnion;

        [Header("Beastiary")]
        [Tooltip("The Pikmin type's scientific name")]
        public string ScientificName = "Pikminus Pikminus";

        [Tooltip("The Pikmin type's beastiary segment")]
        [TextArea(2, 20)]
        public string beastiarySegment = "";

        public string GetName()
        {
            return TypeName + NameAffix;
        }
        public float GetDamage()
        {
            if (NormalizeDamage)
            {
                return DamageAmmount / 100 * DamageMultiplier * LethalMin.DamageMultiplier;
            }
            return DamageAmmount * DamageMultiplier * LethalMin.DamageMultiplier;
        }
        [HideInInspector]
        public PikminMeshData MeshData;
        [HideInInspector]
        public bool HasBeenRegistered;
        [HideInInspector]
        public string version = "0.1.4";
    }

    public struct PikminMeshData
    {
        public PikminType type;
        public Renderer[] Renders;
        public bool[] InitalRendererStates;
        public void Initalize()
        {
            if (type != null && type.MeshPrefab != null)
            {
                Renders = type.MeshPrefab.GetComponentsInChildren<Renderer>(true);
                InitalRendererStates = new bool[Renders.Length];
                for (int i = 0; i < Renders.Length; i++)
                {
                    InitalRendererStates[i] = Renders[i].enabled;
                }
            }
        }
        public void ToggleMeshVisibility(bool visible)
        {
            for (int i = 0; i < Renders.Length; i++)
            {
                Renders[i].enabled = visible && InitalRendererStates[i];
            }
        }
    }
}