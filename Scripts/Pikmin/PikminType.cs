using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;
using LethalMin.Utils;
using UnityEngine.Video;

namespace LethalMin.Pikmin
{
    [CreateAssetMenu(fileName = "PikminType", menuName = "Pikmin/Type", order = 0)]
    public class PikminType : ScriptableObject
    {
        [Header("NOTE FROM DEV: If you are seeing this,"
        + "\n that probobly means you are createing a PikminType using the base LethalMin mod."
        + "\n This is not recomended, as it requires alot more unnessasy set up."
        + "\n So I recommend using the LethalMinLibrary mod to create custom Pikmin Types instead!")]

        [Header("Pikmin Info")]
        [Tooltip("The name of the Pikmin Type")]
        public string PikminName = "Untitled Pikmin";

        [Tooltip("The main color of the Pikmin (Used for HUD)")]
        public Color PikminPrimaryColor = Color.white;

        [Tooltip("The secondary color of the Pikmin (Used for HUD)")]
        public Color PikminSecondaryColor = Color.gray;

        [Tooltip("The Pikmin's Icon that will show in the HUD")]
        public Sprite? PikminIcon;

        [Tooltip("The model prefab for the Pikmin")]
        public GameObject ModelPrefab = null!;

        [Tooltip("The Main Model Refernces for the Pikmin This script should be attached to the model prefab")]
        public PikminModelRefernces modelRefernces = null!;

        [Tooltip("The scriptable object that contains every sound for the pikmin type")]
        public PikminSoundPack SoundPack = null!;

        [Tooltip("The different sound packs for other generations, if you don't have multiple generations, set this to empty")]
        public PikminSoundPack[] SoundPackGenerations = new PikminSoundPack[] { };

        [Tooltip("The texture that overrides the pikmin ghost's model")]
        public Texture2D? PikminGhostOverrideTexture;

        [Tooltip("The frames for the animated pikmin ghost, if null, will use the default ghost texture")]
        public Texture2D[]? AnimatedPikminGhostTexture;
        [Tooltip("The number of frames the animated pikmin ghost will hold on each frame")]
        public int AnimatedPikminGhostFrameHold = 2;
        [Tooltip("The tiling for the pikmin ghost's texture")]
        public Vector2 OverrideGhostTextureTileing = new Vector2(1, 1);
        [Tooltip("The offset for the pikmin ghost's texture")]
        public Vector2 OverrideGhostTextureOffset = new Vector2(0, 0);
        [Tooltip("Whether to set the pikmin ghost's color to the pikmin's primary color")]
        public bool SetGhostColor = true;
        [Tooltip("The gameobject that overrides the pikmin ghost's model")]
        public GameObject? PikminGhostOverrideModel;

        [Tooltip("Prevents the pikmin from being registered, only set this to true if the type is unused or not ment to be registered")]
        public bool DisableRegistration = false;

        [Header("Sprout")]
        [Tooltip("The color the sprout plant uses")]
        public Sprout.SproutPlantColor SproutPlantColor = Sprout.SproutPlantColor.Default;

        [Tooltip("The material that overrides the pikmin ghost's sprout" +
        " (Listed for each generation Pikmin1And2 = 0 Pikmin3 = 1 HeyPikmin = 2 Pikmin4 = 3, leave at 0 to for use any generation)")]
        public Material[]? PikminSproutOverrideMaterial;

        [Tooltip("If null, the sprout will use the mesh prefab")]
        public GameObject? SproutOverrideModel;

        [Tooltip("If the override material is null, the pikmin's sprout will just color it with the PikminPrimaryColor")]
        public bool SetColorOnSprout = true;

        [Tooltip("If you want the Pikmin's idle glow to be a differnt color than the PikminPrimaryColor")]
        public bool UseOverrideSproutGlowColor = false;

        [Tooltip("If you want the Pikmin's idle glow to be a differnt color than the PikminPrimaryColor (Make sure UseOverrideSproutGlowColor is true when using this)")]
        public Color OverrideSproutGlowColor = Color.white;

        [Header("Pikmin Stats")]

        [Tooltip("The Pikmin's HP")]
        public int HP = 1;

        [Tooltip("The speed the pikmin is at the growth stage")]
        public List<float> growSpeeds = new List<float>();

        [Tooltip("(optinal) The attack power the pikmin is at the growth stage")]
        public List<float> growAttacks = new List<float>();

        [Tooltip("(optinal) The carry strength the pikmin is at the growth stage")]
        public List<int> growCarryStrengths = new List<int>();

        [Tooltip("The multiplier for the pikmin's growth speed (1 = no change, 2 = double speed)")]
        public float GrowSpeedMultiplier = 1f;

        [Tooltip("Only used if your type does not have any GrowStages")]
        public float DefaultSpeed = 1;

        [Tooltip("How much faster the pikmin goes when running")]
        public float RunningSpeedMultiplier = 1.5f;

        [Tooltip("The pikmin's carry strength")]
        public int CarryStrength = 1;

        [Tooltip("You generally want all of these to be the same value")]
        public Vector3 ThrowForce = new Vector3(15, 15, 15);

        [Tooltip("The hazards this Pikmin is resistant to")]
        public List<PikminHazard> HazardsResistantTo = new List<PikminHazard>();

        [Tooltip("The maximum distance the pikmin can detect items")]
        public float ItemDetectionRange = 5;

        [Tooltip("Allows the pikmin type to latch onto objects")]
        public bool CanLatchOnToObjects = true;

        [Tooltip("Allows the pikmin type to jump and latch onto objects")]
        public bool CanJumpOntoObjects = true;

        [Tooltip("Allows the pikmin type to carry objects")]
        public bool CanCarryObjects = true;

        [Header("Attacking")]

        [Tooltip("The maximum distance the pikmin can detect and change enemies")]
        public float EnemyDetectionRange = 5;

        [Tooltip("The pikmin's attack strength, will be normalizd (1 = 0.01)")]
        public float AttackStrength = 1;

        [Tooltip("The speed at which the Pikmin type attacks at.")]
        public float AttackRate = 0.5f;

        [Tooltip("The range at which pikmin tpe can hit an enemy when not latched on")]
        public float AttackDistance = 0.5f;

        [Tooltip("The range at which the pikmin type will attempt to jump onto the enemy")]
        public float JumpLatchOnRange = 5;

        [Tooltip("The damage the will be delt to enemies around the Pikmin Type when it dies, will be normalizd (1 = 0.01)")]
        public float DamageDeltUponDeath = 0;

        [Tooltip("Enemies within this range will be delt the DamageDeltUponDeath ammount of damage when the pikmin type dies")]
        public float DeathDamageRange = 0;

        [Tooltip("The ammount of damage delt on enemies the pikmin lands on, will be normalizd (1 = 0.01)")]
        public float DamageDeltUpLanding = 0;
        [Tooltip("The odds of the pikmin being shooken off an enemy (1 = Wont be shooken off, 0 = Will be shooken off)")]
        public float ShakeEndurance = 0.75f;


        [Header("Spawning")]

        [Tooltip("Whether the pikmin can spawn in from Lethal Company's spawn system")]
        public bool SpawnsNaturally = true;

        [Tooltip("Whether the pikmin can spawn indoors")]
        public bool SpawnsIndoors;

        [Tooltip("Whether the pikmin can spawn outdoors")]
        public bool SpawnsOutdoors;

        [Tooltip("Whether the pikmin spawns as a sprout")]
        public bool SpawnsAsSprout;

        [Tooltip("Whether the pikmin uses the pikmin container")]
        public bool UsesPikminContainer;

        [Tooltip("The onion type that the pikmin will try to go to first when automatically leaving")]
        public OnionType? TargetOnion = null;

        [Tooltip("The day after which this pikmin type can start spawning in. Set to -1 to spawn on day 1.")]
        public int SpawnAfterDay = -1;

        [Tooltip("The pikmin's favord moons, the pikmin will have a higher chance of spawning on these moons.")]
        public List<string> FavoredMoons = new List<string>();

        [Tooltip("The Pikmin's favored moon tags, the pikmin will have a higher chance of spawning on moons with these Lethal level Loader tags.")]
        public List<string> FavoredMoonTags = new List<string>();

        [Tooltip("The Pikmin's favored weather, the pikmin will have a higher chance of spawning in these weathers." +
            @"
        None,
        DustClouds,
        Rainy,
        Stormy,
        Foggy,
        Flooded,
        Eclipsed")]
        public List<string> FavoredWeathers = new List<string>();

        [Tooltip("Multiplier for the pikmin's spawn chance when spawning on a favored moon")]
        public float SpawnChanceMultiplier = 2f;

        [Tooltip("Moon Names that Pikmin will haev a lower chance spawning on.")]
        public List<string> AvoidMoons = new List<string>();

        [Tooltip("The Pikmin's avoid moon tags, the pikmin will have a lower chance of spawning on moons with these Lethal level Loader tags.")]
        public List<string> AvoidMoonTags = new List<string>();

        [Tooltip("Multiplier for the pikmin's spawn chance when spawning on an AvoidedMoon")]
        public float SpawnChanceDemultiplier = 0.5f;

        [Header("Other")]
        [Tooltip("Generates a configuration file for the pikmin type")]
        public bool GenerateConfigFile = true;

        [Tooltip("Whether the pikmin instantly enters the onion")]
        public bool InstaEnterOnion;

        [Header("Piklopedia")]
        [Tooltip("Whether the pikmin type is in the Piklopedia")]
        public bool UsePiklopedia = true;
        [Tooltip("Puts the Pikmin Type's Piklopedia page in a more standardised format. If turned off: ScientificName and HelpfulLevel will not be used")]
        public bool UsePresetFormatting = true;
        public string HelpfulLevel = "50%";
        [Tooltip("The Pikmin type's scientific name")]
        public string ScientificName = "Pkminidae pikminus";
        [Tooltip("The Pikmin type's Piklopedia name (use - for spaces), leave empty to use the PikminName")]
        public string piklopediaName = "";
        [Tooltip("The Pikmin type's Piklopedia description")]
        [TextArea(4, 20)]
        public string piklopediaDescription = "";
        [Tooltip("The Pikmin type's Piklopedia page video that is played in the terminal")]
        public VideoClip? piklopediaVideo;
        [Tooltip("The override terminal node for the piklopedia")]
        public TerminalNode? OverridePiklopediaNode = null!;
        [Tooltip("The override terminal keyword for the piklopedia")]
        public TerminalKeyword? OverridePiklopediaKeyword = null!;
        [HideInInspector]
        public PiklopediaEntry? PiklopediaEntry = null!;

        [Header("Advanced Settings")]

        [Tooltip("Whether to cache plant object references or not. This will improve performance," +
        " but may cause issues if the plant changes between instances.")]
        public bool CachePlantObjectRefernces = true;

        [Tooltip("The pikmin's custom AI script that will override the defult one")]
        public PikminAI CustomTypeScript = null!;


        [HideInInspector]
        public int PikminTypeID = -1;

        public PikminSoundPack GetSoundPackByGeneration(PikminGeneration gen)
        {
            if (SoundPackGenerations.Length == 0)
            {
                return SoundPack;
            }
            else
            {
                foreach (PikminSoundPack pack in SoundPackGenerations)
                {
                    if ((int)pack.TargetGeneration == (int)gen)
                    {
                        return pack;
                    }
                }
                LethalMin.Logger.LogDebug($"{PikminName}: No sound pack found for generation {gen}, using default sound pack.");
                return SoundPack;
            }
        }

        public float GetSpeed(int GrowStage, bool Running)
        {
            float val = 0;

            if (growSpeeds.Count == 0 || PikUtils.IsOutOfRange(growSpeeds, GrowStage)
            || growSpeeds[GrowStage] == -1)
            {
                val = DefaultSpeed;
            }
            else
            {
                val = growSpeeds[GrowStage];
            }

            if (Running)
            {
                val = val * RunningSpeedMultiplier;
            }

            if (LethalMin.PikminSpeedMultiplerCheat != -1)
            {
                val *= LethalMin.PikminSpeedMultiplerCheat;
            }
            //LethalMin.Logger.LogInfo($"GotSpeed: {val}");
            return val;
        }
        public int GetCarryStrength(int GrowStage)
        {
            int val = 0;

            if (growCarryStrengths.Count == 0 || PikUtils.IsOutOfRange(growCarryStrengths, GrowStage)
            || growCarryStrengths[GrowStage] == -1)
            {
                val = CarryStrength;
            }
            else
            {
                val = growCarryStrengths[GrowStage];
            }

            //LethalMin.Logger.LogInfo($"GotCarryStrength: {val}");
            return val;
        }
        public float GetAttackStrength(int GrowStage)
        {
            float val = 0;

            if (growAttacks.Count == 0 || PikUtils.IsOutOfRange(growAttacks, GrowStage)
            || growAttacks[GrowStage] == -1)
            {
                val = AttackStrength;
            }
            else
            {
                val = growAttacks[GrowStage];
            }

            val = val / 100;

            if (LethalMin.PikminDamageMultiplerCheat != -1)
            {
                val *= LethalMin.PikminDamageMultiplerCheat;
            }

            //LethalMin.Logger.LogInfo($"GotAttackStrength: {val}");
            return val;
        }
    }
}