using UnityEngine;
using LethalMin.Library;
using Unity.Netcode;
using System;
namespace LethalMin
{
    [CreateAssetMenu(menuName = "LethalMin/PikminType", order = 1)]
    public class PikminType : ScriptableObject
    {
        [Header("NOTE FROM DEV: If you are seeing this,"
        + "\n that probobly means you are createing a PikminType using the base LethalMin mod."
        + "\n This is not recomended, as it requires alot more unnessasy set up."
        +"\n So I recommend using the LethalMinLibrary mod to create custom Pikmin Types instead!")]
        [Header("Distinguishing Information")]

        [Tooltip("The pikmin's name")]
        public string PikminName = "";

        [Tooltip("The pikmin's color")]
        public Color PikminColor;

        [Tooltip("The pikmin's Secondary color")]
        public Color PikminColor2;

        [Tooltip("The pikmin's icon")]
        public Sprite? PikminIcon;

        [Tooltip("The pikmin's the Idle Glow Sprite")]
        public Sprite? PikminGlow;
        public bool ReplaceGlowFXwithDefult = true;

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

        [Tooltip("The pikmin's the sprout path, should be the object right before the plant objects (will be filled automatically if empty)")]
        public string PikminGlowPath;

        [HideInInspector]
        public PikminMeshRefernces MeshRefernces = null!;

        [Header("Pikmin Stats")]
        
        [Tooltip("The hazards the pikmin is resistant to")]
        public HazardType[] HazardsResistantTo = new HazardType[0];

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

        [Tooltip("The amount of damage the pikmin does when it dies.")]
        public float DamageDeltUponDeath;

        [Tooltip("The ammount of distance between the pikmin and enemies where the enemy will be delt damage from the pikmin upon death.")]
        public float DeathDamageRange;

        [Tooltip("Normalizes the damage the pikmin does when it attacks, (i.e. 5 -> 0.05)")]
        public bool NormalizeDamage;

        [Tooltip("The pikmin's carry strength (i.e. one pikmin with the CarryStrength of 5 will count as 5 pikmin on the item)")]
        public int CarryStrength = 1;

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

        [HideInInspector]
        public LethalMinLibrary.PikminType typeConvertedFrom = null!;

        public float GetDamage()
        {
            if (NormalizeDamage)
            {
                return DamageAmmount / 100 * DamageMultiplier * LethalMin.DamageMultiplier;
            }
            return DamageAmmount * DamageMultiplier * LethalMin.DamageMultiplier;
        }        
        public float GetDeathDamage()
        {
            if (NormalizeDamage)
            {
                return DamageDeltUponDeath / 100 * DamageMultiplier * LethalMin.DamageMultiplier;
            }
            return DamageDeltUponDeath * DamageMultiplier * LethalMin.DamageMultiplier;
        }
        [HideInInspector]
        public PikminMeshData MeshData;
        [HideInInspector]
        public bool HasBeenRegistered;
        [HideInInspector]
        public string version = "0.2.0";
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