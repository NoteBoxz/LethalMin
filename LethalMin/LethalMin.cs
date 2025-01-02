using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using BepInEx.Configuration;
using LethalLib.Extras;
using LethalMin.Patches.OtherMods;
using System.Text;
using UnityEngine.AI;
using System.IO;
using Newtonsoft.Json;
using System.Collections;

namespace LethalMin
{
    public enum GameStyle
    {
        Pikmin1,
        Pikmin2,
        Pikmin3,
        Pikmin4
    }
    public enum HazardType
    {
        Lethal,
        Poison,
        Fire,
        Electric,
        Water,
        Exsplosive,
        Spore,
        Crush
    }
    public class FloorData
    {
        public Vector3 FloorRoot;
        public string FloorTitle;
        //There can only really be one elevator at a time so if there are multiple elevators we are screwed.
        public List<Transform> Elevators = new List<Transform>();
        public List<EntranceTeleport> MainExits = new List<EntranceTeleport>();
        public List<EntranceTeleport> FireExits = new List<EntranceTeleport>();
        public List<Transform> AlterntiveExits = new List<Transform>();
        public Collider ElevatorBounds;
    }
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("evaisa.lethallib")]
    [BepInDependency("LethalMon", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("twig.latecompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("dev.kittenji.NavMeshInCompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("MaxWasUnavailable.LethalModDataLib", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Entity378.sellbodies", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Piggy.LCOffice", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Piggy.PiggyVarietyMod", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("kite.ZelevatorCode", BepInDependency.DependencyFlags.SoftDependency)]
    public class LethalMin : BaseUnityPlugin
    {
        public static LethalMin Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony Harmony { get; set; }
        public static bool SmartMinMov = true, SmarterMinMov = false;
        public static string ThrowAction;
        public static string SwitchForwardAction;
        public static string SwitchBackwawrdsAction;
        public static string WhisleAction;
        public static string DismissAction;

        public static InputClass InputClassInstace = null!;

        public static string ManeaterName = "Maneater";
        public static EnemyType pikminEnemyType = null!;
        public static EnemyType puffminEnemyType = null!;
        private static TerminalNode pikminTerminalNode = null!;
        private TerminalKeyword pikminTerminalKeyword = null!;
        private static TerminalNode puffminTerminalNode = null!;
        private TerminalKeyword puffminTerminalKeyword = null!;
        public GameObject PikminPrefab = null!;
        public static Dictionary<int, PikminType> RegisteredPikminTypes = new Dictionary<int, PikminType>();
        public static Dictionary<int, PikminType> IndoorTypes = new Dictionary<int, PikminType>();
        public static Dictionary<int, PikminType> OutdoorTypes = new Dictionary<int, PikminType>();
        public static Dictionary<int, PikminType> SproutTypes = new Dictionary<int, PikminType>();
        public static Dictionary<int, PikminType> NaturalTypes = new Dictionary<int, PikminType>();
        public static Dictionary<int, OnionType> RegisteredOnionTypes = new Dictionary<int, OnionType>();
        public static Dictionary<int, OnionType> SpawnableOnionTypes = new Dictionary<int, OnionType>();
        public static Dictionary<int, OnionFuseRules> RegisteredFuseRules = new Dictionary<int, OnionFuseRules>();
        public LayerMask PikminColideable = 1107298561 | (1 << 28);
        public static AssetBundleLoader Loader = null!;
        public static string LMConfigFolder = Path.Combine(Paths.ConfigPath, "LethalMin Pikmin Types");

        public static bool IsUsingModLib()
        {
            return IsDependencyLoaded("MaxWasUnavailable.LethalModDataLib");
        }

        public static bool IsUsingInputUtils()
        {
            return IsDependencyLoaded("com.rune580.LethalCompanyInputUtils");
        }

        public static List<string> GetParsedAttackBlacklist()
        {
            if (string.IsNullOrEmpty(AttackBlacklist))
            {
                return new List<string>();
            }
            return AttackBlacklist.Split(',').ToList();
        }
        public static List<string> GetParsedPickupBlacklist()
        {
            if (string.IsNullOrEmpty(PickupBlacklist))
            {
                return new List<string>();
            }
            return PickupBlacklist.Split(',').ToList();
        }

        public static string ParseEnumToString<T>() where T : Enum
        {
            return string.Join(", ", Enum.GetNames(typeof(T)));
        }


        #region  Config Variables
        public static bool CustomOnionAllowedValue;
        public static bool LethalWhistleValue;
        public static float FallTimerValue;
        public static int MaxMinValue;
        public static bool InvinciMinValue;
        public static bool StrudyMinValue;
        public static bool UselessblueMinValue;
        public static float OnionSpawnChanceValue;
        public static float OutdoorSpawnChanceValue;
        public static float IndoorSpawnChanceValue;
        public static bool FasrPurpsValue;
        public static bool LethalLandminesValue;
        public static bool AllItemsToP;
        public static int WhistlePriceValue;
        public static int ContianerPriceValue;
        public static bool LethalHydroValue;
        public static bool LethaDogs1Value;
        public static bool LethaDogs2Value;
        public static float AttentionTimer;
        public static int ItemRequireSubracterValue;
        public static float ItemCounterYPositionOffsetValue;
        public static bool DebugMode;
        public static bool OnlyMain;
        public static bool OnlyExit;
        public static bool PrioitizeAttacking;
        public static bool FunniOnion;
        public static bool LethalBugs;
        public static bool LethalManEater;
        public static bool PassiveToManEater;
        public static bool CalmableManeater;
        public static bool NonRasistManEater;
        public static bool LethalSpider;
        public static bool LethalJester;
        public static bool LethalThumper;
        public static bool LethalGiant;
        public static bool LethalBarber;
        public static bool LethalMech;
        public static int MechBurnLimmit;
        public static int JesterEatLimmit;
        public static int ThumperEatLimmit;
        public static int GiantEatLimmit;
        public static int BarberEatLimmit;
        public static int ManeaterEatLimmit;
        public static int SpiderEatLimmit;
        public static int JesterEatBuffer;
        public static int ThumperEatBuffer;
        public static int ManeaterEatBuffer;
        public static int SpiderEatBuffer;
        public static bool FriendlyFire;
        public static float PikminScale, SproutScale;
        public static float ChaseRange;
        public static float BarberRange;
        public static bool HundradOnOne;
        public static bool DontFormidOak;
        public static bool Pikmin3Style;
        public static bool LethalTurrents;
        public static bool LethalBees;
        public static bool SuperLethalBees;
        public static int BeesShockCount;
        public static bool MeanBees;
        public static float PikminSelectedPosX, PikminSelectedPosY, PikminSelectedPosZ;
        public static float PikminSelectedRotX, PikminSelectedRotY, PikminSelectedRotZ;
        public static float PikminSelectedScale;

        public static float PikminCountPosX, PikminCountPosY, PikminCountPosZ;
        public static float PikminCountRotX, PikminCountRotY, PikminCountRotZ;
        public static float PikminCountScale;
        public static bool CapCarrySpeed = false;
        public static bool FriendlyFireOmon;
        public static bool FriendlyFireMon;
        public static bool TargetCar, TeleportEle, TeleportCarie;
        public static bool GoToCar;
        public static bool AllowSpawnMultiplier;
        public static float WhistleVolume;
        public static float ManagerRefreshRate;
        public static bool DontNatualSpawn;
        public static ObstacleAvoidanceType PikminDefultAvoidanceType, PikminCarryingAvoidanceType;
        public static bool MeshWrapping;
        public static float WhisRange, WhisMin, WhisMax;
        public static float PlayerNoticeRange;
        public static float SpeedMultiplier;
        public static float DamageMultiplier;
        public static bool ScanMin;
        public static bool PurgeAfterFire;
        public static string AttackBlacklist, PickupBlacklist;
        public static int HoarderBugEatBuffer, HoarderBugEatLimmit;
        public static bool ConvertPuffminOnDeath;
        public static bool PuffMask;
        public static bool ShowSafety;
        public static HudPresets CurrentHudPreset;
        public static bool AllowConvertion;
        public static bool AllowProduction;
        public static float MaskedWhistleVolume, MaskedWhistleRange;
        public static bool HidePuffminPrompt;
        public static float ShipPhaseOnionX, ShipPhaseOnionY, ShipPhaseOnionZ;
        //Generated Useable Varibles GoES HERE

        public static float GrabRange;
        public static float SelectedDefultAlpha;
        public static float CounterDefultAlpha;
        public static ElementBehavior SquadHudBehavior;
        public static ElementBehavior CounterBehavior;
        public static ElementBehavior PromptBehavior;
        public static bool AllowWildPToDie;
        public static bool AllowCarryNoLeader;
        public static bool AllowCarryAfterWork;
        public static bool AllowAttackNoLeader;
        public static bool AllowAttackAfterWork;
        public static bool GeneratePConfig;
        public static bool UsePConfigs;
        public static bool RasistElevator;
        public static bool GenNavMehsOnElevate;
        public static bool AllowLethalEscape;
        //public LayerMask PikminColideable_DECREPAED = 1107298561 | (1 << 19) | (1 << 28);

        public static ConfigEntry<bool> SkipPluckAnimation, FF, Smartmin, Smartermin, OnlyMainV, OnlyExitV, Pattack,
        CarrySpeedConfig, LethalSpiderConfig, LethalJesterConfig, LethalThumperConfig, LethalGiantConfig, LethaDogs,
        LethaDogs2, LethalBugsConfig, LethalBarberConfig, LethalMechConfig, LethalBB, LethalHydro, LethalB, BeeChase,
        CustomOnionAllowed, LethalWhistle, LethalLandmines, AllToPItems, LimmitItemGrab, AllowOnionFuseConfig,
        LethalManEaterConfig, CalmableManeaterConfig, Rasisium, NotFormidableOak, LethalTurrentsC, InvinciMin,
        StrudyMin, UselessblueMin, DebugM, FunniMode, PassiveToManEaterConfig, FFOM, FFM, TeleEle, TeleCarie,
        TargetCarConfig, GetToDaCar, AllowSpawnMultiplierCF, NoPowerSpawn, MWon, ScanablePikmin, CanShipEjectFromShip,
        TurnToNormalOnDeath, PuffMaskConfig, ShowSafetyConfig, AllowConvertionConfig, AllowProductionConfig, HidePuffminPromptConfig;

        public static ConfigEntry<float> Pscale, Sscale, ChaseR, PCPX, PCPY, PCPZ, PCRX, PCRY, PCRZ, PCScale,
         PCPCountX, PCPCountY, PCPCountZ, PCRCCountX, PCRCCountY, PCRCCountZ, PCScaleCount, FallTimer, CounterOffset,
         NoticeTimer, BarberR, OnionSpawnChance, SproutSpawnChance, IndoorSpawnChance, WhistleVolumeConfig,
         ManagerRefreshRateC, WhistleRange, WhistleMinRaidus, WhistleMaxRadius, PlayerNR, SpeedMultiplierConfig,
         DamageMultiplierConfig, MaskedWhistleVolumeConfig, MaskedWhistleRangeConfig, ThrowX, ThrowY, ThrowZ,
         SPOx, SPOy, SPOz;

        public static ConfigEntry<int> MechBurnLimmitConfig, JesterDiet, ThumperDiet, GiantDiet, BarberDiet, ManeaterDiet, SpideDiet,
        JesterBuffer, ThumperBuffer, SpiderBuffer, BeesShockCountConfig, ManeaterBuffer, MaxMin
        , WhistlePrice, ContianerPrice, ItemRequireSubracter, HBDiet, HBBuffer;

        public static ConfigEntry<string> throwActionConfig, switchForwardConfig,
        switchBackwardsConfig, whistleActionConfig, dismissActionConfig,
        AttackBlackListConfig, PickupBlacklistConfig;
        public static ConfigEntry<ObstacleAvoidanceType> PikminDefaultAvoidanceTypeConfig;
        public static ConfigEntry<ObstacleAvoidanceType> PikminCarryingAvoidanceTypeConfig;
        public static ConfigEntry<HudPresets> HudPresetsConfig;


        //Generated Config Varibles GoES HERE

        public static ConfigEntry<float> GrabRangeConfig;
        public static ConfigEntry<float> SelectedDefultAlphaConfig;
        public static ConfigEntry<float> CounterDefultAlphaConfig;
        public static ConfigEntry<ElementBehavior> SquadHudBehaviorConfig;
        public static ConfigEntry<ElementBehavior> CounterBehaviorConfig;
        public static ConfigEntry<ElementBehavior> PromptBehaviorConfig;
        public static ConfigEntry<bool> AllowWildPToDieConfig;
        public static ConfigEntry<bool> AllowCarryNoLeaderConfig;
        public static ConfigEntry<bool> AllowCarryAfterWorkConfig;
        public static ConfigEntry<bool> AllowAttackNoLeaderConfig;
        public static ConfigEntry<bool> AllowAttackAfterWorkConfig;
        public static ConfigEntry<bool> GeneratePConfigConfig;
        public static ConfigEntry<bool> UsePConfigsConfig;
        public static ConfigEntry<bool> RasistElevatorConfig;
        public static ConfigEntry<bool> GenNavMehsOnElevateConfig;
        public static ConfigEntry<bool> AllowLethalEscapeConfig;
        #endregion

        string AciisArt = @" 
         ___  ___  _  __ __  __  ___  _  _ 
        | _ \|_ _|| |/ /|  \/  ||_ _|| \| |
        |  _/ | | |   < | |\/| | | | | .  |
        |_|  |___||_|\_\|_|  |_||___||_|\_|
";

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;
            //Patch code first
            NetcodePatcher();
            Patch();

            // Bind config second
            BindConfig();
            if (IsDependencyLoaded("ainavt.lc.lethalconfig"))
                BindLCconfigs();

            if (IsUsingInputUtils())
            {
                Logger.LogMessage("InputUtils detected, binding input class");
                BindInputClass();
            }

            if (IsUsingModLib())
            {
                Logger.LogMessage("LethalModDataLib detected, using that for save data");
            }

            // load everything third
            LoadPikminAssets();

            LoadWhisleAssets();

            // Register everything fourth
            RegisterPikminEnemy();
            Logger.LogInfo($"{AciisArt}");
            Logger.LogInfo($"v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }


        public void BindInputClass() { InputClassInstace = new InputClass(); }

        public void BindConfig()
        {
            #region Setting Config
            SkipPluckAnimation = Config.Bind("Pikmin", "Skip Pluck Animation", false, "Skips the player's pluck animation");
            FF = Config.Bind("Pikmin", "Friendly Fire", false, "Allows a leaders to attack their pikmin");
            Smartmin = Config.Bind("Pikmin", "Make pikmin follow behind leader", true, "(HOST ONLY) Makes pikmin move behind their leader when following.");
            Smartermin = Config.Bind("Pikmin", "Dynamic Positioning", false, "(HOST ONLY) Makes pikmin move in a more dynamic way simular to the Pikmin games, (Causes Lag)");
            OnlyMainV = Config.Bind("Pikmin", "Make Pikmin Path Only to Main", false, "(HOST ONLY) Only allows pikmin to carry items to the main entrance");
            OnlyExitV = Config.Bind("Pikmin", "Make Pikmin Path Only to Fire Exit", false, "(HOST ONLY) Only allows pikmin to carry items to the fire exits (if their are any)");
            Pattack = Config.Bind("Pikmin", "Prioitize Pikmin Attacking", false, "Makes pikmin attack enemies before carrying items");
            Pscale = Config.Bind("Pikmin", "Pikmin Scale", 1f, "Changes the scale of the pikmin");
            Sscale = Config.Bind("Pikmin", "Sprout Scale", 1f, "Changes the scale of the sprout");
            ChaseR = Config.Bind("Pikmin", "Chase Range", 20f, "Changes the range at which pikmin will chase after enemies");
            CarrySpeedConfig = Config.Bind("Pikmin", "Cap Carry Speed", false, "Caps the speed at which pikmin can carry items to 200%");
            //TeleEle = Config.Bind("Pikmin", "Teleport Pikmin to Elevator", false, "Teleports pikmin to the Elevator when their leader is in the elevator");
            //TeleCarie = Config.Bind("Pikmin", "Teleport Pikmin to Car", false, "Teleports pikmin to the Car when the");
            TargetCarConfig = Config.Bind("Pikmin", "Make Pikmin Target Car", true, "Makes Pikmin target the car when their leader is in the car");
            GetToDaCar = Config.Bind("Pikmin", "Make Pikmin Carry Items To Car", true, "Makes Pikmin carry items to the car when the car if the car is closer than the ship");
            PikminDefaultAvoidanceTypeConfig = Config.Bind("Pikmin", "Default Avoidance Type", ObstacleAvoidanceType.LowQualityObstacleAvoidance, "The default obstacle avoidance type for Pikmin");
            PikminCarryingAvoidanceTypeConfig = Config.Bind("Pikmin", "Carrying Avoidance Type", ObstacleAvoidanceType.NoObstacleAvoidance, "The obstacle avoidance type for Pikmin when carrying items");
            NoPowerSpawn = Config.Bind("Pikmin", "Disable Natual Spawning", false, "Makes it so Pikmin won't spawn in from Lethal Company's spawn system");
            ScanablePikmin = Config.Bind("Pikmin", "Make Pikmin Scanable", true, "Makes it so Pikmin can be scanned");
            AttackBlackListConfig = Config.Bind("Pikmin", "Attack Blacklist", "Docile Locust Bees,Manticoil", "The list of enemy names that pikmin can't attack (separated by commas, no spaces in between) (item1,item2,item3...)");
            PickupBlacklistConfig = Config.Bind("Pikmin", "Pickup Blacklist", "", "The list of item names that pikmin can't pickup (separated by commas, no spaces in between) (item1,item2,item3...)");
            AllowConvertionConfig = Config.Bind("Pikmin", "Allow bodies to be converted into Items", true, "Allows bodies to be converted into items. You'd only really need to disable this if you have another mod to do this");

            TurnToNormalOnDeath = Config.Bind("Puffmin", "Turn Puffmin into Pikmin on Death", false, "Turns a puffmin back to normal when they die");
            HidePuffminPromptConfig = Config.Bind("Puffmin", "Hide Puffmin Prompt", false, "Hides the wiggle prompt that shows when a puffmin is latched on to you.");

            LethalSpiderConfig = Config.Bind("Enemy AI", "Make Spider eat Pikmin", true, "Makes Spider eat Pikmin that are too close to the spider");
            LethalJesterConfig = Config.Bind("Enemy AI", "Make Jester eat Pikmin", true, "Makes Jester eat Pikmin when opened");
            LethalThumperConfig = Config.Bind("Enemy AI", "Make Thumper Attack Pikmin", true, "Makes Thumper eat Pikmin when aggroed");
            LethalGiantConfig = Config.Bind("Enemy AI", "Make Giant eat Pikmin", true, "Makes Giant grab and eat near by pikmin when grabbing a player");
            LethalBarberConfig = Config.Bind("Enemy AI", "Make Barber Deflower Pikmin", true, "Makes Barber deflower Pikmin when they move (Deflowering means to revert a pikmin's grow stage back by one or more)");
            LethalMechConfig = Config.Bind("Enemy AI", "Make Pikmin grabable by OldBird", true, "Makes Pikmin grab and torachable by old birds");
            MechBurnLimmitConfig = Config.Bind("Enemy AI", "OldBird grab Limmit", 7, "The max ammount of pikmin the Old Birds can grab/burn at a time");
            JesterDiet = Config.Bind("Enemy AI", "Jester Eat Limmit", 5, "The max ammount of pikmin the Jester can eat at a time");
            ThumperDiet = Config.Bind("Enemy AI", "Thumper Eat Limmit", 4, "The max ammount of pikmin the Thumper can eat at a time");
            GiantDiet = Config.Bind("Enemy AI", "Giant Eat Limmit", 5, "The max ammount of pikmin the Giant can eat at a time");
            BarberDiet = Config.Bind("Enemy AI", "Barber snip Limmit", 3, "The max ammount of pikmin the Barber can deflower at a time");
            BarberR = Config.Bind("Enemy AI", "Barber Range", 5f, "The distance at which the Barber can deflower pikmin");
            SpideDiet = Config.Bind("Enemy AI", "Spider Eat Limmit", 2, "The max ammount of pikmin the Spider can eat at a time");
            JesterBuffer = Config.Bind("Enemy AI", "Jester Eat Buffer", 10, "The max ammount time after eating that the Jester can eat again");
            ThumperBuffer = Config.Bind("Enemy AI", "Thumper Eat Buffer", 5, "The max ammount time after eating that the Thumper can eat again");
            SpiderBuffer = Config.Bind("Enemy AI", "Spider Eat Buffer", 8, "The max ammount time after eating that the Spider can eat again");
            LethalB = Config.Bind("Enemy AI", "Make bees attack Pikmin", true, "Makes Bees electrocute pikmin");
            LethalBB = Config.Bind("Enemy AI", "Make bees kill Pikmin", false, "Makes it so when non-electric resistant Pikmin are shocked bees, they die.");
            LethalHydro = Config.Bind("Enemy AI", "Make Hydrogere kill pikmin", false, "Makes the Hydrogere slime thing insta-kill pikmin");
            LethaDogs = Config.Bind("Enemy AI", "Make Enemies deaf to Pikmin", false, "Makes Enemie unable to hear pikmin when throwning or carrying");
            LethaDogs2 = Config.Bind("Enemy AI", "Make Enemies deaf to Whistle", false, "Makes Enemies unable to hear whistle when whistleing Pikmin");
            LethalBugsConfig = Config.Bind("Enemy AI", "Make Hoarding Bugs eat Pikmin", true, "Makes Hoarding Bugs attack Pikmin if a pikmin attempts to grab it's scrap");
            BeesShockCountConfig = Config.Bind("Enemy AI", "Bee Shock Count", 3, "The max ammount of bees that can shock a pikmin at a time");
            BeeChase = Config.Bind("Enemy AI", "Make bees chase Pikmin", false, "Makes Bees chase Pikmin when their hive goes missing");
            HBDiet = Config.Bind("Enemy AI", "Hoarding Bug Eat Limmit", 1, "The max ammount of Hoarding Bugs can eat at a time");
            HBBuffer = Config.Bind("Enemy AI", "Hoarding Bug Eat Buffer", 10, "The max ammount time after eating that the Hoarding Bug can eat again");
            PuffMaskConfig = Config.Bind("Enemy AI", "Allow Masked to Make Puffmin", true, "Allows masked pikmin to convert Pikmin into Puffmin");
            MaskedWhistleVolumeConfig = Config.Bind("Enemy AI", "Masked Whistle Volume", 1f, "The volume of the whistle sound for the masked");
            MaskedWhistleRangeConfig = Config.Bind("Enemy AI", "Masked Whistle Range", 10f, "The range at which the whistle can reach for the masked");


            HudPresetsConfig = Config.Bind("HUD", "HUD Preset", HudPresets.New, "The preset for the HUD");
            PCPX = Config.Bind("HUD", "PikminSelected(XPos)", 8.4f, "The X position of the selected pikmin UI element");
            PCPY = Config.Bind("HUD", "PikminSelected(YPos)", -106.6f, "The Y position of the selected pikmin UI element");
            PCPZ = Config.Bind("HUD", "PikminSelected(ZPos)", -15.9f, "The Z position of the selected pikmin UI element");
            PCRX = Config.Bind("HUD", "PikminSelected(RotX)", 0f, "The X rotation of the selected pikmin UI element");
            PCRY = Config.Bind("HUD", "PikminSelected(RotY)", 0f, "The Y rotation of the selected pikmin UI element");
            PCRZ = Config.Bind("HUD", "PikminSelected(RotZ)", 0f, "The Z rotation of the selected pikmin UI element");
            PCScale = Config.Bind("HUD", "PikminSelected(Scale)", 0.6f, "The scale of the selected pikmin UI element");
            PCPCountX = Config.Bind("HUD", "PikminCount(XPos)", 23.5f, "The X position of the pikmin count UI element");
            PCPCountY = Config.Bind("HUD", "PikminCount(YPos)", -204.9f, "The Y position of the pikmin count UI element");
            PCPCountZ = Config.Bind("HUD", "PikminCount(ZPos)", -47.4f, "The Z position of the pikmin count UI element");
            PCRCCountX = Config.Bind("HUD", "PikminCount(RotX)", 0f, "The X rotation of the pikmin count UI element");
            PCRCCountY = Config.Bind("HUD", "PikminCount(RotY)", 0f, "The Y rotation of the pikmin count UI element");
            PCRCCountZ = Config.Bind("HUD", "PikminCount(RotZ)", 0f, "The Z rotation of the pikmin count UI element");
            PCScaleCount = Config.Bind("HUD", "PikminCount(Scale)", 0.6f, "The scale of the pikmin count UI element");

            throwActionConfig = Config.Bind("Controls", "Throw Action", "<Keyboard>/4", "Key binding for throwing Pikmin");
            switchForwardConfig = Config.Bind("Controls", "Switch Forward", "<Keyboard>/3", "Key binding for switching Pikmin type forward");
            switchBackwardsConfig = Config.Bind("Controls", "Switch Backwards", "<Keyboard>/2", "Key binding for switching Pikmin type backwards");
            whistleActionConfig = Config.Bind("Controls", "Whistle Action", "<Mouse>/leftButton", "Key binding for whistling");
            dismissActionConfig = Config.Bind("Controls", "Dismiss Action", "<Mouse>/middleButton", "Key binding for dismissing Pikmin");

            SPOx = Config.Bind("Onions", "Space Onions Position X", -6.1364f, "The X Position of the onions when in space");
            SPOy = Config.Bind("Onions", "Space Onions Position Y", 0f, "The Y Position of the onions when in space");
            SPOz = Config.Bind("Onions", "Space Onions Position Z", 60.136f, "The Z Position of the onions when in space");
            AllowProductionConfig = Config.Bind("Onions", "Allow Onions to produce Sprouts", true, "Allows Onions to produce sprouts.");
            AllowOnionFuseConfig = Config.Bind("Onions", "Allow Onion Fuse", true, "Allows onions to fuse after the ship leaves.");
            CustomOnionAllowed = Config.Bind("Onions", "Allow Custom Onion spawn Position", true, "Allows onions to land on pre defined spawn points on modded moons (if there are any).");

            AllowSpawnMultiplierCF = Config.Bind("Extra", "Allow Spawn Multiplier", true, "Allows the custom Pikmin Types to use Spawn Multipliers.");
            LethalWhistle = Config.Bind("Extra", "Make whistle conductive", false, "Makes whistles conductive to stormy weather.");
            LethalLandmines = Config.Bind("Extra", "Make Pikmin Trigger Landmines", true, "Allows pikmin to trigger landmines");
            LethalTurrentsC = Config.Bind("Extra", "Make Turrents Kill Pikmin", true, "Allows pikmin to get shot by turrents");
            AllToPItems = Config.Bind("Extra", "Make every item carrieable by pikmin", false, "Allows pikmin to carry any item, includeing items that are not ment to be held by entites such as the shotgun.");
            FallTimer = Config.Bind("Extra", "Max fall duration", 10f, "The max ammount of time pikmin can be in the air before they automaticly land, in case they fell out of bounds or are stuck.");
            CounterOffset = Config.Bind("Extra", "Item Fraction Y Position Offset", 2.5f, "Offsets the Y position of the item fraction");
            NoticeTimer = Config.Bind("Extra", "Working state change timer", 1f, "The time it takes for a player to stand next to a pikmin before they notice them if they are carrying an item.");
            LimmitItemGrab = Config.Bind("Extra", "Limmit Pikmin on item", true, "Limmits the max ammount of pikmin that can grab an item.");
            WhistleVolumeConfig = Config.Bind("Extra", "Whistle Volume", 1f, "The volume of the whistle sound (I'm only implumenting this because the whistle sound is bugged and I can't fix it)");
            ManagerRefreshRateC = Config.Bind("Extra", "PikminManager Refersh Rate", 0.75f, "The rate at which the PikminManager refreshes it's object refernces. Warning! Having this value too low could cause lag.");
            MWon = Config.Bind("Extra", "Mesh Wrapping", false, "Enables mesh wrapping for the target object");
            ShowSafetyConfig = Config.Bind("Extra", "Show Safety areas", false, "Shows the range in which Pikmin are safe from being left behind");
            ThrowX = Config.Bind("Extra", "ThrowOrigen X", 0.1f, "The X Position of the throw origen");
            ThrowY = Config.Bind("Extra", "ThrowOrigen Y", 0f, "The Y Position of the throw origen");
            ThrowZ = Config.Bind("Extra", "ThrowOrigen Z", 0f, "The Z Position of the throw origen");

            LethalManEaterConfig = Config.Bind("Maneater", "Make Adult Maneater Eat Pikmin", true, "Makes The Maneater kill pikmin in it's way when agroed");
            CalmableManeaterConfig = Config.Bind("Maneater", "Make Maneater Calmable by Pikmin", true, "Makes the maneater in it's baby state calmable by pikmin. (Note: The unless Favor any Pikmin Type is enabled, the maneater will only be calmable by the pikmin type that was selected by the first player it sees.)");
            Rasisium = Config.Bind("Maneater", "Make Maneater Favor any Pikmin Type", false, "Makes the man eater like all pikmin types instead of the first one it comes into conatact with.");
            ManeaterDiet = Config.Bind("Maneater", "Maneater Eat Limmit", 2, "The max ammount of pikmin the Maneater can eat at a time");
            ManeaterBuffer = Config.Bind("Maneater", "Maneater Eat Buffer", 8, "The max ammount time after eating that the Maneater can eat again");
            PassiveToManEaterConfig = Config.Bind("Maneater", "Make Pikmin passive to Baby Maneater", true, "Makes Pikmin not attack the Maneater when in it's baby state");
            NotFormidableOak = Config.Bind("Maneater", "Make Pikmin drop the man eater when near player", false, "Makes Pikmin drop the man eater when it is near their leader");

            MaxMin = Config.Bind("`Cheats`", "Max pikmin count", 100, "The max ammount of pikmin that is allowed to be out at a time.");
            InvinciMin = Config.Bind("`Cheats`", "Make Pikmin unkillable", false, "Makes pikmin unable to die, but they can still get knocked back.");
            StrudyMin = Config.Bind("`Cheats`", "Sturdy Pikmin", false, "Makes pikmin unable to be knocked back.");
            UselessblueMin = Config.Bind("`Cheats`", "Make Pikmin undrownable", false, "Makes blue pikmin useless- I mean. Makes all pikmin be able to enter water.");
            OnionSpawnChance = Config.Bind("`Cheats`", "Onion Spawn Chance", 0.45f, "Changes the odds of Onions spawning");
            SproutSpawnChance = Config.Bind("`Cheats`", "Outdoor pikmin Spawn Chance", 0.3f, "Changes the odds of Outdoor pikmin spawning");
            IndoorSpawnChance = Config.Bind("`Cheats`", "Indoor pikmin Spawn Chance", 0.05f, "Changes the odds of Indoor Pikmin spawning");
            WhistlePrice = Config.Bind("`Cheats`", "Whistle Price", 15, "Changes price of the Whistle");
            ContianerPrice = Config.Bind("`Cheats`", "Pikmin Container Price", 251, "Changes the price of the pikmin container");
            ItemRequireSubracter = Config.Bind("`Cheats`", "Pikmin Needed Per-item subtraction", 0, "Subtracts the ammount of pikmin needed per item");
            DebugM = Config.Bind("`Cheats`", "Debug Mode", false, "q");
            FunniMode = Config.Bind("`Cheats`", "Funni Onion", false, "Funni Onion");
            WhistleRange = Config.Bind("`Cheats`", "Whistle Range", 20f, "The range at which the whistle can reach");
            WhistleMinRaidus = Config.Bind("`Cheats`", "Whistle Min Radius", 1f, "The min radius at which the whistle can be heard");
            WhistleMaxRadius = Config.Bind("`Cheats`", "Whistle Max Radius", 15f, "The max radius at which the whistle can be heard");
            PlayerNR = Config.Bind("`Cheats`", "Player Notice Range", 1.5f, "The distance between a player and a pikmin at which the pikmin will notice the player");
            SpeedMultiplierConfig = Config.Bind("`Cheats`", "Speed Multiplier", 1f, "The multiplies the pikmin's speed by this value");
            DamageMultiplierConfig = Config.Bind("`Cheats`", "Damage Multiplier", 1f, "The multiplies the pikmin's damage by this value");
            CanShipEjectFromShip = Config.Bind("`Cheats`", "Purge save after fire", true, "Deleats the save file after the players have been fired.");

            FFOM = Config.Bind("LethalMon", "Make Pikmin Attack Leaders Tammed Enemy", false, "Makes Pikmin attack the leaders Pokémon");
            FFM = Config.Bind("LethalMon", "Make Pikmin Attack Tammed Enemies", false, "Makes Pikmin attack any Tamed Enemies");

            //Generated ConfigBindings goes here

            GrabRangeConfig = Config.Bind("Pikmin", "Pikmin Grab Range", 2.5f, "The range a Pikmin can Grab an Item");
            SelectedDefultAlphaConfig = Config.Bind("HUD", "Pikmin In Group Alpha", 0f, "The Alpha or transparency of the Pikmin In Group Hud when idle (Normalized: 0 = transparent, 1 = opaque) (Will only work if the OnlyShowWhenChanged option is selected)");
            CounterDefultAlphaConfig = Config.Bind("HUD", "Pikmin Counter Alpha", 0.25f, "The Alpha or transparency of the Pikmin Counter when idle (Normalized: 0 = transparent, 1 = opaque) (Will only work if the OnlyShowWhenChanged option is selected)");
            SquadHudBehaviorConfig = Config.Bind("HUD", "Pikmin Selected Visibilty", ElementBehavior.OnlyShowWhenChanged, "The visibilty settings for the Pikmin Selected Hud");
            CounterBehaviorConfig = Config.Bind("HUD", "Pikmin Counter Visibilty", ElementBehavior.OnlyShowWhenChanged, "The visibilty settings for the Pikmin Counter");
            PromptBehaviorConfig = Config.Bind("HUD", "Input Prompt Visibilty", ElementBehavior.OnlyShowWhenChanged, "The visibilty settings for the InputPrompt above the Pikmin in group counter");
            AllowWildPToDieConfig = Config.Bind("Pikmin", "Allow Wild Pikmin To Die", false, "Allows Wild Pikmin (Pikmin that has not been in a player's squad at least once since spawning in.) to die.");
            AllowCarryNoLeaderConfig = Config.Bind("Pikmin", "Allow Wild Pikmin To Carry items", false, "Allow Wild Pikmin (Pikmin that has not been in a player's squad at least once since spawning in.) To carry items.");
            AllowCarryAfterWorkConfig = Config.Bind("Pikmin", "Carry Items After Work-State", false, "Allows Pikmin to carry items again after already carrying items.");
            AllowAttackNoLeaderConfig = Config.Bind("Pikmin", "Allow Wild Pikmin To Attack", true, "Allows Wild Pikmin (Pikmin that has not been in a player's squad at least once since spawning in.) to chase and attack enemies.");
            AllowAttackAfterWorkConfig = Config.Bind("Pikmin", "Attack After Work-State", true, "Allows Pikmin to chase and attack enemies after carrying items.");
            GeneratePConfigConfig = Config.Bind("Pikmin", "Generate PikminType Configs", false, "Generates a config file for each Pikmin type. (Note: The game will need to be restarted in order for the type configureation changes to take effect.)");
            UsePConfigsConfig = Config.Bind("Pikmin", "Allow Config File Override", false, "Allows a Pikmin Type's config file's values to override the values in game.");
            RasistElevatorConfig = Config.Bind("LC-Office", "Make Only Pikmin Use Elevator", true, "Makes it so that only Pikmin can enter the elevator (On floors 2 and 3 only). Any other entity just gets instantly teleported out if they get in.");
            GenNavMehsOnElevateConfig = Config.Bind("LC-Office", "Genorate Navmesh On Elevator", true, "Genorates a NavMesh on the Elevator in the LC-Office Interor. This makes it so Pikmin can path to, and walk on the elevator.");
            AllowLethalEscapeConfig = Config.Bind("Pikmin", "Make Pikmin Only Target Outdoors", false, "Makes Pikmin only target destinatons that are outside when carrying items. Even if the Pikmin is indoors");

            #endregion






            #region Setting Config values
            ShipPhaseOnionX = SPOx.Value;
            ShipPhaseOnionY = SPOy.Value;
            ShipPhaseOnionZ = SPOz.Value;
            HidePuffminPrompt = HidePuffminPromptConfig.Value;
            AllowConvertion = AllowConvertionConfig.Value;
            AllowProduction = AllowProductionConfig.Value;
            PuffMask = PuffMaskConfig.Value;
            MaskedWhistleRange = MaskedWhistleRangeConfig.Value;
            MaskedWhistleVolume = MaskedWhistleVolumeConfig.Value;
            CurrentHudPreset = HudPresetsConfig.Value;
            ShowSafety = ShowSafetyConfig.Value;
            ConvertPuffminOnDeath = TurnToNormalOnDeath.Value;
            HoarderBugEatBuffer = HBBuffer.Value;
            HoarderBugEatLimmit = HBDiet.Value;
            AttackBlacklist = AttackBlackListConfig.Value;
            PickupBlacklist = PickupBlacklistConfig.Value;
            PurgeAfterFire = CanShipEjectFromShip.Value;
            DamageMultiplier = DamageMultiplierConfig.Value;
            ScanMin = ScanablePikmin.Value;
            SpeedMultiplier = SpeedMultiplierConfig.Value;
            PlayerNoticeRange = PlayerNR.Value;
            WhisRange = WhistleRange.Value;
            WhisMin = WhistleMinRaidus.Value;
            WhisMax = WhistleMaxRadius.Value;
            MeshWrapping = MWon.Value;
            DontNatualSpawn = NoPowerSpawn.Value;
            AllowSpawnMultiplier = AllowSpawnMultiplierCF.Value;
            PikminDefultAvoidanceType = PikminDefaultAvoidanceTypeConfig.Value;
            PikminCarryingAvoidanceType = PikminCarryingAvoidanceTypeConfig.Value;
            ManagerRefreshRate = ManagerRefreshRateC.Value;
            WhistleVolume = WhistleVolumeConfig.Value;
            GoToCar = GetToDaCar.Value;
            TargetCar = TargetCarConfig.Value;
            FriendlyFireMon = FFOM.Value;
            FriendlyFireMon = FFM.Value;
            CapCarrySpeed = CarrySpeedConfig.Value;
            PikminSelectedPosX = PCPX.Value;
            PikminSelectedPosY = PCPY.Value;
            PikminSelectedPosZ = PCPZ.Value;
            PikminSelectedRotX = PCRX.Value;
            PikminSelectedRotY = PCRY.Value;
            PikminSelectedRotZ = PCRZ.Value;
            PikminSelectedScale = PCScale.Value;
            PikminCountPosX = PCPCountX.Value;
            PikminCountPosY = PCPCountY.Value;
            PikminCountPosZ = PCPCountZ.Value;
            PikminCountRotX = PCRCCountX.Value;
            PikminCountRotY = PCRCCountY.Value;
            PikminCountRotZ = PCRCCountZ.Value;
            PikminCountScale = PCScaleCount.Value;
            SmartMinMov = Smartmin.Value;
            SmarterMinMov = Smartermin.Value;
            ThrowAction = throwActionConfig.Value;
            SwitchForwardAction = switchForwardConfig.Value;
            SwitchBackwawrdsAction = switchBackwardsConfig.Value;
            WhisleAction = whistleActionConfig.Value;
            DismissAction = dismissActionConfig.Value; SmartMinMov = Smartmin.Value;
            SmarterMinMov = Smartermin.Value;
            ThrowAction = throwActionConfig.Value;
            SwitchForwardAction = switchForwardConfig.Value;
            SwitchBackwawrdsAction = switchBackwardsConfig.Value;
            WhisleAction = whistleActionConfig.Value;
            DismissAction = dismissActionConfig.Value;
            CustomOnionAllowedValue = CustomOnionAllowed.Value;
            LethalWhistleValue = LethalWhistle.Value;
            FallTimerValue = FallTimer.Value;
            MaxMinValue = MaxMin.Value;
            InvinciMinValue = InvinciMin.Value;
            StrudyMinValue = StrudyMin.Value;
            UselessblueMinValue = UselessblueMin.Value;
            OnionSpawnChanceValue = OnionSpawnChance.Value;
            OutdoorSpawnChanceValue = SproutSpawnChance.Value;
            IndoorSpawnChanceValue = IndoorSpawnChance.Value;
            LethalLandminesValue = LethalLandmines.Value;
            WhistlePriceValue = WhistlePrice.Value;
            ContianerPriceValue = ContianerPrice.Value;
            LethalHydroValue = LethalHydro.Value;
            LethaDogs1Value = LethaDogs.Value;
            LethaDogs2Value = LethaDogs2.Value;
            AttentionTimer = NoticeTimer.Value;
            AllItemsToP = AllToPItems.Value;
            ItemCounterYPositionOffsetValue = CounterOffset.Value;
            DebugMode = DebugM.Value;
            OnlyMain = OnlyMainV.Value;
            OnlyExit = OnlyExitV.Value;
            PrioitizeAttacking = Pattack.Value;
            LethalBugs = LethalBugsConfig.Value;
            LethalManEater = LethalManEaterConfig.Value;
            PassiveToManEater = PassiveToManEaterConfig.Value;
            LethalSpider = LethalSpiderConfig.Value;
            LethalJester = LethalJesterConfig.Value;
            LethalThumper = LethalThumperConfig.Value;
            LethalGiant = LethalGiantConfig.Value;
            LethalBarber = LethalBarberConfig.Value;
            CalmableManeater = CalmableManeaterConfig.Value;
            NonRasistManEater = Rasisium.Value;
            JesterEatLimmit = JesterDiet.Value;
            ThumperEatLimmit = ThumperDiet.Value;
            GiantEatLimmit = GiantDiet.Value;
            BarberEatLimmit = BarberDiet.Value;
            ManeaterEatLimmit = ManeaterDiet.Value;
            JesterEatBuffer = JesterBuffer.Value;
            ThumperEatBuffer = ThumperBuffer.Value;
            ManeaterEatBuffer = ManeaterBuffer.Value;
            SpiderEatLimmit = SpideDiet.Value;
            SpiderEatBuffer = SpiderBuffer.Value;
            PikminScale = Pscale.Value;
            SproutScale = Sscale.Value;
            FriendlyFire = FF.Value;
            LethalMech = LethalMechConfig.Value;
            MechBurnLimmit = MechBurnLimmitConfig.Value;
            ChaseRange = ChaseR.Value;
            BarberRange = BarberR.Value;
            HundradOnOne = LimmitItemGrab.Value;
            DontFormidOak = NotFormidableOak.Value;
            ItemRequireSubracterValue = ItemRequireSubracter.Value;
            Pikmin3Style = AllowOnionFuseConfig.Value;
            LethalLandminesValue = LethalLandmines.Value;
            LethalTurrents = LethalTurrentsC.Value;
            LethalBees = LethalB.Value;
            SuperLethalBees = LethalBB.Value;
            BeesShockCount = BeesShockCountConfig.Value;
            MeanBees = BeeChase.Value;
            FunniOnion = FunniMode.Value;

            //Generated Settings Valuse Goes Here

            GrabRange = GrabRangeConfig.Value;
            SelectedDefultAlpha = SelectedDefultAlphaConfig.Value;
            CounterDefultAlpha = CounterDefultAlphaConfig.Value;
            SquadHudBehavior = SquadHudBehaviorConfig.Value;
            CounterBehavior = CounterBehaviorConfig.Value;
            PromptBehavior = PromptBehaviorConfig.Value;
            AllowWildPToDie = AllowWildPToDieConfig.Value;
            AllowCarryNoLeader = AllowCarryNoLeaderConfig.Value;
            AllowCarryAfterWork = AllowCarryAfterWorkConfig.Value;
            AllowAttackNoLeader = AllowAttackNoLeaderConfig.Value;
            AllowAttackAfterWork = AllowAttackAfterWorkConfig.Value;
            GeneratePConfig = GeneratePConfigConfig.Value;
            UsePConfigs = UsePConfigsConfig.Value;
            RasistElevator = RasistElevatorConfig.Value;
            GenNavMehsOnElevate = GenNavMehsOnElevateConfig.Value;
            AllowLethalEscape = AllowLethalEscapeConfig.Value;
            #endregion






            #region Setting Config Events
            // Add SettingChanged events for all configs
            SPOx.SettingChanged += (_, _) => ShipPhaseOnionX = SPOx.Value;
            SPOy.SettingChanged += (_, _) => ShipPhaseOnionY = SPOy.Value;
            SPOz.SettingChanged += (_, _) => ShipPhaseOnionZ = SPOz.Value;
            HidePuffminPromptConfig.SettingChanged += (_, _) => HidePuffminPrompt = HidePuffminPromptConfig.Value;
            AllowConvertionConfig.SettingChanged += (_, _) => AllowConvertion = AllowConvertionConfig.Value;
            AllowProductionConfig.SettingChanged += (_, _) => AllowProduction = AllowProductionConfig.Value;
            PuffMaskConfig.SettingChanged += (_, _) => PuffMask = PuffMaskConfig.Value;
            MaskedWhistleVolumeConfig.SettingChanged += (_, _) => MaskedWhistleVolume = MaskedWhistleVolumeConfig.Value;
            MaskedWhistleRangeConfig.SettingChanged += (_, _) => MaskedWhistleRange = MaskedWhistleRangeConfig.Value;
            HudPresetsConfig.SettingChanged += (_, _) => CurrentHudPreset = HudPresetsConfig.Value;
            HudPresetsConfig.SettingChanged += (_, _) =>
            {
                CurrentHudPreset = HudPresetsConfig.Value;
                if (PikminHUD.pikminHUDInstance != null)
                {
                    PikminHUD.pikminHUDInstance.SetHudPresets(CurrentHudPreset);
                }
            };
            ShowSafetyConfig.SettingChanged += (_, _) => ShowSafety = ShowSafetyConfig.Value;
            TurnToNormalOnDeath.SettingChanged += (_, _) => ConvertPuffminOnDeath = TurnToNormalOnDeath.Value;
            HBBuffer.SettingChanged += (_, _) => HoarderBugEatBuffer = HBBuffer.Value;
            HBDiet.SettingChanged += (_, _) => HoarderBugEatLimmit = HBDiet.Value;
            AttackBlackListConfig.SettingChanged += (_, _) => AttackBlacklist = AttackBlackListConfig.Value;
            PickupBlacklistConfig.SettingChanged += (_, _) => PickupBlacklist = PickupBlacklistConfig.Value;
            CanShipEjectFromShip.SettingChanged += (_, _) => PurgeAfterFire = CanShipEjectFromShip.Value;
            DamageMultiplierConfig.SettingChanged += (_, _) => DamageMultiplier = DamageMultiplierConfig.Value;
            ScanablePikmin.SettingChanged += (_, _) => ScanMin = ScanablePikmin.Value;
            SpeedMultiplierConfig.SettingChanged += (_, _) => SpeedMultiplier = SpeedMultiplierConfig.Value;
            PlayerNR.SettingChanged += (_, _) => PlayerNoticeRange = PlayerNR.Value;
            WhistleRange.SettingChanged += (_, _) => WhisRange = WhistleRange.Value;
            WhistleMinRaidus.SettingChanged += (_, _) => WhisMin = WhistleMinRaidus.Value;
            WhistleMaxRadius.SettingChanged += (_, _) => WhisMax = WhistleMaxRadius.Value;
            MWon.SettingChanged += (_, _) => MeshWrapping = MWon.Value;
            NoPowerSpawn.SettingChanged += (_, _) => DontNatualSpawn = NoPowerSpawn.Value;
            AllowSpawnMultiplierCF.SettingChanged += (_, _) => AllowSpawnMultiplier = AllowSpawnMultiplierCF.Value;
            PikminDefaultAvoidanceTypeConfig.SettingChanged += (_, _) => PikminDefultAvoidanceType = PikminDefaultAvoidanceTypeConfig.Value;
            PikminCarryingAvoidanceTypeConfig.SettingChanged += (_, _) => PikminCarryingAvoidanceType = PikminCarryingAvoidanceTypeConfig.Value;
            ManagerRefreshRateC.SettingChanged += (_, _) => ManagerRefreshRate = ManagerRefreshRateC.Value;
            WhistleVolumeConfig.SettingChanged += (_, _) => WhistleVolume = WhistleVolumeConfig.Value;
            GetToDaCar.SettingChanged += (_, _) => GoToCar = GetToDaCar.Value;
            TargetCarConfig.SettingChanged += (_, _) => TargetCar = TargetCarConfig.Value;
            FFOM.SettingChanged += (_, _) => FriendlyFireOmon = FFOM.Value;
            FFM.SettingChanged += (_, _) => FriendlyFireMon = FFM.Value;
            CarrySpeedConfig.SettingChanged += (_, _) => CapCarrySpeed = CarrySpeedConfig.Value;
            PCPX.SettingChanged += (_, _) => PikminSelectedPosX = PCPX.Value;
            PCPY.SettingChanged += (_, _) => PikminSelectedPosY = PCPY.Value;
            PCPZ.SettingChanged += (_, _) => PikminSelectedPosZ = PCPZ.Value;
            PCRX.SettingChanged += (_, _) => PikminSelectedRotX = PCRX.Value;
            PCRY.SettingChanged += (_, _) => PikminSelectedRotY = PCRY.Value;
            PCRZ.SettingChanged += (_, _) => PikminSelectedRotZ = PCRZ.Value;
            PCScale.SettingChanged += (_, _) => PikminSelectedScale = PCScale.Value;
            PCPCountX.SettingChanged += (_, _) => PikminCountPosX = PCPCountX.Value;
            PCPCountY.SettingChanged += (_, _) => PikminCountPosY = PCPCountY.Value;
            PCPCountZ.SettingChanged += (_, _) => PikminCountPosZ = PCPCountZ.Value;
            PCRCCountX.SettingChanged += (_, _) => PikminCountRotX = PCRCCountX.Value;
            PCRCCountY.SettingChanged += (_, _) => PikminCountRotY = PCRCCountY.Value;
            PCRCCountZ.SettingChanged += (_, _) => PikminCountRotZ = PCScaleCount.Value;
            PCScaleCount.SettingChanged += (_, _) => PikminCountScale = PCScaleCount.Value;
            Smartmin.SettingChanged += (obj, args) => { SmartMinMov = Smartmin.Value; };
            Smartermin.SettingChanged += (obj, args) => { SmarterMinMov = Smartermin.Value; };
            throwActionConfig.SettingChanged += (_, _) => ThrowAction = throwActionConfig.Value;
            switchForwardConfig.SettingChanged += (_, _) => SwitchForwardAction = switchForwardConfig.Value;
            switchBackwardsConfig.SettingChanged += (_, _) => SwitchBackwawrdsAction = switchBackwardsConfig.Value;
            whistleActionConfig.SettingChanged += (_, _) => WhisleAction = whistleActionConfig.Value;
            dismissActionConfig.SettingChanged += (_, _) => DismissAction = dismissActionConfig.Value;
            Smartmin.SettingChanged += (_, _) => SmartMinMov = Smartmin.Value;
            Smartermin.SettingChanged += (_, _) => SmarterMinMov = Smartermin.Value;
            throwActionConfig.SettingChanged += (_, _) => ThrowAction = throwActionConfig.Value;
            switchForwardConfig.SettingChanged += (_, _) => SwitchForwardAction = switchForwardConfig.Value;
            switchBackwardsConfig.SettingChanged += (_, _) => SwitchBackwawrdsAction = switchBackwardsConfig.Value;
            whistleActionConfig.SettingChanged += (_, _) => WhisleAction = whistleActionConfig.Value;
            dismissActionConfig.SettingChanged += (_, _) => DismissAction = dismissActionConfig.Value;
            CustomOnionAllowed.SettingChanged += (_, _) => CustomOnionAllowedValue = CustomOnionAllowed.Value;
            LethalWhistle.SettingChanged += (_, _) => LethalWhistleValue = LethalWhistle.Value;
            FallTimer.SettingChanged += (_, _) => FallTimerValue = FallTimer.Value;
            MaxMin.SettingChanged += (_, _) => MaxMinValue = MaxMin.Value;
            InvinciMin.SettingChanged += (_, _) => InvinciMinValue = InvinciMin.Value;
            StrudyMin.SettingChanged += (_, _) => StrudyMinValue = StrudyMin.Value;
            UselessblueMin.SettingChanged += (_, _) => UselessblueMinValue = UselessblueMin.Value;
            OnionSpawnChance.SettingChanged += (_, _) => OnionSpawnChanceValue = OnionSpawnChance.Value;
            SproutSpawnChance.SettingChanged += (_, _) => OutdoorSpawnChanceValue = SproutSpawnChance.Value;
            IndoorSpawnChance.SettingChanged += (_, _) => IndoorSpawnChanceValue = IndoorSpawnChance.Value;
            LethalLandmines.SettingChanged += (_, _) => LethalLandminesValue = LethalLandmines.Value;
            WhistlePrice.SettingChanged += (_, _) => WhistlePriceValue = WhistlePrice.Value;
            ContianerPrice.SettingChanged += (_, _) => ContianerPriceValue = ContianerPrice.Value;
            LethalHydro.SettingChanged += (_, _) => LethalHydroValue = LethalHydro.Value;
            LethaDogs.SettingChanged += (_, _) => LethaDogs1Value = LethaDogs.Value;
            LethaDogs2.SettingChanged += (_, _) => LethaDogs2Value = LethaDogs2.Value;
            NoticeTimer.SettingChanged += (_, _) => AttentionTimer = NoticeTimer.Value;
            AllToPItems.SettingChanged += (_, _) => AllItemsToP = AllToPItems.Value;
            CounterOffset.SettingChanged += (_, _) => ItemCounterYPositionOffsetValue = CounterOffset.Value;
            DebugM.SettingChanged += (_, _) => DebugMode = DebugM.Value;
            OnlyMainV.SettingChanged += (_, _) => OnlyMain = OnlyMainV.Value;
            OnlyExitV.SettingChanged += (_, _) => OnlyExit = OnlyExitV.Value;
            Pattack.SettingChanged += (_, _) => PrioitizeAttacking = Pattack.Value;
            LethalBugsConfig.SettingChanged += (_, _) => LethalBugs = LethalBugsConfig.Value;
            LethalManEaterConfig.SettingChanged += (_, _) => LethalManEater = LethalManEaterConfig.Value;
            PassiveToManEaterConfig.SettingChanged += (_, _) => PassiveToManEater = PassiveToManEaterConfig.Value;
            LethalSpiderConfig.SettingChanged += (_, _) => LethalSpider = LethalSpiderConfig.Value;
            LethalJesterConfig.SettingChanged += (_, _) => LethalJester = LethalJesterConfig.Value;
            LethalThumperConfig.SettingChanged += (_, _) => LethalThumper = LethalThumperConfig.Value;
            LethalGiantConfig.SettingChanged += (_, _) => LethalGiant = LethalGiantConfig.Value;
            LethalBarberConfig.SettingChanged += (_, _) => LethalBarber = LethalBarberConfig.Value;
            CalmableManeaterConfig.SettingChanged += (_, _) => CalmableManeater = CalmableManeaterConfig.Value;
            Rasisium.SettingChanged += (_, _) => NonRasistManEater = Rasisium.Value;
            JesterDiet.SettingChanged += (_, _) => JesterEatLimmit = JesterDiet.Value;
            ThumperDiet.SettingChanged += (_, _) => ThumperEatLimmit = ThumperDiet.Value;
            GiantDiet.SettingChanged += (_, _) => GiantEatLimmit = GiantDiet.Value;
            BarberDiet.SettingChanged += (_, _) => BarberEatLimmit = BarberDiet.Value;
            ManeaterDiet.SettingChanged += (_, _) => ManeaterEatLimmit = ManeaterDiet.Value;
            JesterBuffer.SettingChanged += (_, _) => JesterEatBuffer = JesterBuffer.Value;
            ThumperBuffer.SettingChanged += (_, _) => ThumperEatBuffer = ThumperBuffer.Value;
            ManeaterBuffer.SettingChanged += (_, _) => ManeaterEatBuffer = ManeaterBuffer.Value;
            SpideDiet.SettingChanged += (_, _) => SpiderEatLimmit = SpideDiet.Value;
            SpiderBuffer.SettingChanged += (_, _) => SpiderEatBuffer = SpiderBuffer.Value;
            Pscale.SettingChanged += (_, _) => PikminScale = Pscale.Value;
            Sscale.SettingChanged += (_, _) => SproutScale = Sscale.Value;
            FF.SettingChanged += (_, _) => FriendlyFire = FF.Value;
            LethalMechConfig.SettingChanged += (_, _) => LethalMech = LethalMechConfig.Value;
            MechBurnLimmitConfig.SettingChanged += (_, _) => MechBurnLimmit = MechBurnLimmitConfig.Value;
            ChaseR.SettingChanged += (_, _) => ChaseRange = ChaseR.Value;
            BarberR.SettingChanged += (_, _) => BarberRange = BarberR.Value;
            LimmitItemGrab.SettingChanged += (_, _) => HundradOnOne = LimmitItemGrab.Value;
            NotFormidableOak.SettingChanged += (_, _) => DontFormidOak = NotFormidableOak.Value;
            ItemRequireSubracter.SettingChanged += (_, _) => ItemRequireSubracterValue = ItemRequireSubracter.Value;
            AllowOnionFuseConfig.SettingChanged += (_, _) => Pikmin3Style = AllowOnionFuseConfig.Value;
            LethalLandmines.SettingChanged += (_, _) => LethalLandminesValue = LethalLandmines.Value;
            LethalTurrentsC.SettingChanged += (_, _) => LethalTurrents = LethalTurrentsC.Value;
            LethalB.SettingChanged += (_, _) => LethalBees = LethalB.Value;
            LethalBB.SettingChanged += (_, _) => SuperLethalBees = LethalBB.Value;
            BeesShockCountConfig.SettingChanged += (_, _) => BeesShockCount = BeesShockCountConfig.Value;
            BeeChase.SettingChanged += (_, _) => MeanBees = BeeChase.Value;
            FunniMode.SettingChanged += (_, _) => FunniOnion = FunniMode.Value;

            //Generated Settings Events Goes here

            GrabRangeConfig.SettingChanged += (_, _) => GrabRange = GrabRangeConfig.Value;
            SelectedDefultAlphaConfig.SettingChanged += (_, _) => SelectedDefultAlpha = SelectedDefultAlphaConfig.Value;
            CounterDefultAlphaConfig.SettingChanged += (_, _) => CounterDefultAlpha = CounterDefultAlphaConfig.Value;
            SquadHudBehaviorConfig.SettingChanged += (_, _) =>
            {
                SquadHudBehavior = SquadHudBehaviorConfig.Value;
                if (PikminHUD.pikminHUDInstance != null)
                {
                    PikminHUD.pikminHUDInstance.UpdatePelements();
                }
            };
            CounterBehaviorConfig.SettingChanged += (_, _) =>
            {
                CounterBehavior = CounterBehaviorConfig.Value;
                if (PikminHUD.pikminHUDInstance != null)
                {
                    PikminHUD.pikminHUDInstance.UpdatePelements();
                }
            };
            PromptBehaviorConfig.SettingChanged += (_, _) =>
            {
                PromptBehavior = PromptBehaviorConfig.Value;
                if (PikminHUD.pikminHUDInstance != null)
                {
                    PikminHUD.pikminHUDInstance.UpdatePelements();
                }
            };
            AllowWildPToDieConfig.SettingChanged += (_, _) => AllowWildPToDie = AllowWildPToDieConfig.Value;
            AllowCarryNoLeaderConfig.SettingChanged += (_, _) => AllowCarryNoLeader = AllowCarryNoLeaderConfig.Value;
            AllowCarryAfterWorkConfig.SettingChanged += (_, _) => AllowCarryAfterWork = AllowCarryAfterWorkConfig.Value;
            AllowAttackNoLeaderConfig.SettingChanged += (_, _) => AllowAttackNoLeader = AllowAttackNoLeaderConfig.Value;
            AllowAttackAfterWorkConfig.SettingChanged += (_, _) => AllowAttackAfterWork = AllowAttackAfterWorkConfig.Value;
            GeneratePConfigConfig.SettingChanged += (_, _) => GeneratePConfig = GeneratePConfigConfig.Value;
            UsePConfigsConfig.SettingChanged += (_, _) => UsePConfigs = UsePConfigsConfig.Value;
            RasistElevatorConfig.SettingChanged += (_, _) => RasistElevator = RasistElevatorConfig.Value;
            GenNavMehsOnElevateConfig.SettingChanged += (_, _) => GenNavMehsOnElevate = GenNavMehsOnElevateConfig.Value;
            AllowLethalEscapeConfig.SettingChanged += (_, _) => AllowLethalEscape = AllowLethalEscapeConfig.Value;
            #endregion
        }
        public void BindLCconfigs()
        {
            LethalConfigManager.SetModDescription("Adds Pikmin to Lethal Company!");

            // Pikmin
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(SkipPluckAnimation, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(FF, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(Smartmin, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(Smartermin, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(OnlyMainV, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(OnlyExitV, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowLethalEscapeConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(Pattack, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(Pscale, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(Sscale, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(ChaseR, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(CarrySpeedConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(TargetCarConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(GetToDaCar, false));
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<ObstacleAvoidanceType>(PikminDefaultAvoidanceTypeConfig, false));
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<ObstacleAvoidanceType>(PikminCarryingAvoidanceTypeConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(NoPowerSpawn, true));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(ScanablePikmin, false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(AttackBlackListConfig, false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(PickupBlacklistConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowConvertionConfig, false));

            // Puffmin
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(TurnToNormalOnDeath, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(HidePuffminPromptConfig, false));

            // Onion
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(SPOx, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(SPOy, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(SPOz, false));

            // Controls
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(throwActionConfig, false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(switchForwardConfig, false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(switchBackwardsConfig, false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(whistleActionConfig, false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(dismissActionConfig, false));

            // Enemy AI
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalSpiderConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalJesterConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalThumperConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalGiantConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalBarberConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalMechConfig, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(MechBurnLimmitConfig, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(JesterDiet, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(ThumperDiet, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(GiantDiet, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(BarberDiet, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(BarberR, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(SpideDiet, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(JesterBuffer, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(ThumperBuffer, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(SpiderBuffer, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalB, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalBB, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalHydro, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethaDogs, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethaDogs2, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalBugsConfig, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(BeesShockCountConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(BeeChase, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(HBDiet, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(HBBuffer, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(PuffMaskConfig, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(MaskedWhistleVolumeConfig, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(MaskedWhistleRangeConfig, false));

            // Maneater
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalManEaterConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(CalmableManeaterConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(Rasisium, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(ManeaterDiet, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(ManeaterBuffer, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(PassiveToManEaterConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(NotFormidableOak, false));

            // Extra
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(CustomOnionAllowed, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowSpawnMultiplierCF, true));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalWhistle, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalLandmines, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LethalTurrentsC, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllToPItems, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(FallTimer, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(CounterOffset, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(NoticeTimer, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(LimmitItemGrab, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowOnionFuseConfig, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(WhistleVolumeConfig, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(ManagerRefreshRateC, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(MWon, true));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(ShowSafetyConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowProductionConfig, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(ThrowX, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(ThrowY, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(ThrowZ, false));


            // HUD
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<HudPresets>(HudPresetsConfig, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCPX, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCPY, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCPZ, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCRX, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCRY, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCRZ, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCScale, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCPCountX, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCPCountY, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCPCountZ, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCRCCountX, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCRCCountY, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCRCCountZ, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PCScaleCount, false));

            // Cheats
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(MaxMin, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(InvinciMin, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(StrudyMin, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(UselessblueMin, false));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(OnionSpawnChance, new FloatSliderOptions { Min = 0f, Max = 1f }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(SproutSpawnChance, new FloatSliderOptions { Min = 0f, Max = 1f }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(IndoorSpawnChance, new FloatSliderOptions { Min = 0f, Max = 1f }));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(WhistlePrice, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(ContianerPrice, false));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(ItemRequireSubracter, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(FunniMode, true));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(WhistleRange, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(WhistleMinRaidus, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(WhistleMaxRadius, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PlayerNR, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(SpeedMultiplierConfig, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(DamageMultiplierConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(CanShipEjectFromShip, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(DebugM, false));


            // Lethal Mon            
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(FFOM, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(FFM, false));

            //Generated LC bindings goes here

            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(GrabRangeConfig, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(SelectedDefultAlphaConfig, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(CounterDefultAlphaConfig, false));
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<ElementBehavior>(SquadHudBehaviorConfig, false));
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<ElementBehavior>(CounterBehaviorConfig, false));
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<ElementBehavior>(PromptBehaviorConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowWildPToDieConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowCarryNoLeaderConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowCarryAfterWorkConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowAttackNoLeaderConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowAttackAfterWorkConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(GeneratePConfigConfig, true));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(UsePConfigsConfig, true));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(RasistElevatorConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(GenNavMehsOnElevateConfig, true));
        }
        public static void BindPIKconfig(PikminType type, bool shouldLoad)
        {
            if (!Directory.Exists(LMConfigFolder))
                Directory.CreateDirectory(LMConfigFolder);

            GenerateOrLoadPikminTypeConfig(type, shouldLoad);
        }
        private static T BindAndLoadPikConfig<T>(ConfigFile configFile, string section, string key, T defaultValue, string description)
        {
            if (typeof(T).IsArray || (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>)))
            {
                Type elementType = typeof(T).IsArray ? typeof(T).GetElementType() : typeof(T).GetGenericArguments()[0];
                string arrayType = elementType.Name;

                if (elementType.IsEnum)
                {
                    string enumString = ParseEnumToString<HazardType>();
                    description += $" (Possible values: {enumString})";
                    string stringValue = string.Join(",", ((IEnumerable)defaultValue).Cast<Enum>().Select(e => e.ToString()));
                    description += $" (This is a list, separated by commas, no spaces in between) (item1,item2,item3...) (type: Enum[{arrayType}])";
                    var entry = configFile.Bind<string>(section, key, stringValue, description);

                    string[] values = entry.Value.Split(',');
                    if (typeof(T).IsArray)
                    {
                        Array array = Array.CreateInstance(elementType, values.Length);
                        for (int i = 0; i < values.Length; i++)
                        {
                            try
                            {
                                array.SetValue(Enum.Parse(elementType, values[i], true), i);
                            }
                            catch (ArgumentException)
                            {
                                if (DebugMode)
                                    Logger.LogWarning($"Invalid enum value '{values[i]}' for {key}. Using default value.");
                                array.SetValue(null, i);
                            }
                        }
                        return (T)(object)array;
                    }
                    else // List<T>
                    {
                        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                        foreach (var value in values)
                        {
                            try
                            {
                                list.Add(Enum.Parse(elementType, value, true));
                            }
                            catch (ArgumentException)
                            {
                                if (DebugMode)
                                    Logger.LogWarning($"Invalid enum value '{value}' for {key}. Using default value.");
                                list.Add(null);
                            }
                        }
                        return (T)list;
                    }
                }
                else
                {
                    string stringValue = string.Join(",", ((IEnumerable)defaultValue).Cast<object>());
                    description += $" (This is a list, separated by commas, no spaces in between) (item1,item2,item3...) (type:{arrayType})";
                    var entry = configFile.Bind<string>(section, key, stringValue, description);

                    string[] values = entry.Value.Split(',');
                    if (typeof(T).IsArray)
                    {
                        Array array = Array.CreateInstance(elementType, values.Length);
                        for (int i = 0; i < values.Length; i++)
                        {
                            array.SetValue(Convert.ChangeType(values[i], elementType), i);
                        }
                        return (T)(object)array;
                    }
                    else // List<T>
                    {
                        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                        foreach (var value in values)
                        {
                            list.Add(Convert.ChangeType(value, elementType));
                        }
                        return (T)list;
                    }
                }
            }
            else
            {
                try
                {
                    return configFile.Bind(section, key, defaultValue, description).Value;
                }
                catch (Exception)
                {
                    if (DebugMode)
                        Logger.LogWarning($"Failed to bind config for {key}, likely due to a missmatch");// {e}");
                    return defaultValue;
                }
            }
        }
        public static void GenerateOrLoadPikminTypeConfig(PikminType type, bool loadValues)
        {
            string configPath = Path.Combine(LMConfigFolder, $"{type.name}.cfg");
            ConfigFile configFile = new ConfigFile(configPath, true);

            Type pikminTypeClass = typeof(PikminType);
            FieldInfo[] fields = pikminTypeClass.GetFields(BindingFlags.Public | BindingFlags.Instance);

            string currentSection = "";
            string[] ExcludedFields = new string[] { "PikminTypeID", "HasBeenRegistered", "version", "GenerateConfigFile", "MeshData", "PikminScripts", "MeshRefernces" };

            foreach (FieldInfo field in fields)
            {
                if (field.IsInitOnly || ExcludedFields.Contains(field.Name))
                    continue;

                HeaderAttribute[] headerAttrs = (HeaderAttribute[])field.GetCustomAttributes(typeof(HeaderAttribute), false);
                if (headerAttrs.Length > 0)
                {
                    currentSection = headerAttrs[headerAttrs.Length - 1].header;
                }

                string description = "";
                TooltipAttribute[] tooltipAttrs = (TooltipAttribute[])field.GetCustomAttributes(typeof(TooltipAttribute), false);
                if (tooltipAttrs.Length > 0)
                {
                    description = tooltipAttrs[tooltipAttrs.Length - 1].tooltip;
                }

                object value = field.GetValue(type);

                if (value != null)
                {
                    Type fieldType = field.FieldType;

                    if (loadValues)
                    {
                        object loadedValue = typeof(LethalMin).GetMethod("BindAndLoadPikConfig", BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(fieldType)
                            .Invoke(null, new object[] { configFile, currentSection, field.Name, value, description });

                        if (loadedValue != null)
                            field.SetValue(type, loadedValue);
                    }
                    else
                    {
                        typeof(LethalMin).GetMethod("BindAndLoadPikConfig", BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(fieldType)
                            .Invoke(null, new object[] { configFile, currentSection, field.Name, value, description });
                    }

                    Logger.LogDebug($"{(loadValues ? "Loaded" : "Generated")} config for ({field.Name}, {value}, {fieldType})");
                }
            }

            configFile.Save();
            Logger.LogInfo($"{(loadValues ? "Loaded" : "Generated")} config for {type.name}");
        }
        public static bool PtypeHasConfig(PikminType type)
        {
            return File.Exists($"{LMConfigFolder}/{type.name}.cfg");
        }



        public static bool CantConvertEnemy(EnemyType enemy)
        {
            return false;
        }

        public static bool IsPikminResistantToHazard(PikminType type, HazardType hazard)
        {
            return type.HazardsResistantTo.Contains(hazard);
        }

        public static Color GetColorFromPType(PikminType type)
        {
            return type.PikminColor;
        }
        public static Color GetColorFromPType(PikminType type, float offset)
        {
            return new Color(type.PikminColor.r - offset, type.PikminColor.g - offset, type.PikminColor.b - offset, type.PikminColor.a - offset);
        }
        public static Color GetColorFromPType(OnionType type)
        {
            return type.OnionColor;
        }

        public static List<PikminAI> FindNearestPikmin(Vector3 position, float maxDistance, int maxCount)
        {
            var pikminEnemies = PikminManager.GetPikminEnemies();
            if (pikminEnemies == null || pikminEnemies.Count == 0)
            {
                //Logger.LogWarning("No Pikmin enemies found.");
                return new List<PikminAI>();
            }

            return pikminEnemies
                .Where(gameObject => gameObject != null)
                .Where(pikmin => pikmin != null && Vector3.Distance(position, pikmin.transform.position) <= maxDistance)
                .OrderBy(pikmin => Vector3.Distance(position, pikmin.transform.position))
                .Take(maxCount)
                .ToList();

        }

        public static PikminType GetPikminTypeById(int id)
        {
            if (RegisteredPikminTypes.ContainsKey(id))
            {
                return RegisteredPikminTypes[id];
            }
            else
            {
                Logger.LogError("Pikmin type with ID " + id + " not found!");
                return RegisteredPikminTypes[0];
            }
        }
        public static OnionType GetOnionTypeById(int id)
        {
            if (RegisteredOnionTypes.ContainsKey(id))
            {
                return RegisteredOnionTypes[id];
            }
            else
            {
                Logger.LogError("Onion type with ID " + id + " not found!");
                return RegisteredOnionTypes[0];
            }
        }


        public static void RegisterPikminType(PikminType type, bool SkipOnionCheck = false)
        {
            if (!type.HasBeenRegistered)
            {
                //Do Fatal Checks
                if (type.MeshRefernces == null)
                {
                    if (type.MeshPrefab == null)
                    {
                        Logger.LogError("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has no mesh prefab, skipping registration!");
                        return;
                    }
                    if (string.IsNullOrEmpty(type.AnimPath) && type.MeshPrefab.GetComponent<Animator>() == null)
                    {
                        Logger.LogError("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has no anim path, skipping registration!");
                        return;
                    }
                    if (type.MeshPrefab.transform.Find(type.AnimPath).GetComponent<Animator>() == null)
                    {
                        Logger.LogError("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " anim path does not contain an animator, skipping registration!");
                        return;
                    }
                }
                else
                {
                    if (type.MeshRefernces.PikminAnimator == null)
                    {
                        Logger.LogError("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has no animator, skipping registration!");
                        return;
                    }
                }
                //Register the type
                if (type.PikminScripts != null && type.PikminScripts.Length > 0)
                {
                    GameObject container = LethalMin.pikminPrefab.transform.Find("PikminScriptContainer").gameObject;
                    LethalMin.Logger.LogInfo(type.PikminName + " has " + type.PikminScripts.Length + " scripts");
                    foreach (var script in type.PikminScripts)
                    {
                        container.AddComponent(script.GetType());
                        LethalMin.Logger.LogInfo("Added " + script.GetType().Name);
                    }
                }

                //Do Invalid Checks
                type.PikminTypeID = RegisteredPikminTypes.Count;
                RegisteredPikminTypes.Add(type.PikminTypeID, type);
                if (GetMajorMinorVersion(type.version) != GetMajorMinorVersion(MyPluginInfo.PLUGIN_VERSION))
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName +
                    $" {type.version} has a different version than the mod " + $"({MyPluginInfo.PLUGIN_VERSION})" + ", this may cause issues!");
                }
                if (type.PikminIcon == null)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has no icon!");
                    type.PikminIcon = UndefinedPikmin;
                }
                if (type.soundPack == null)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has no soundPack!");
                }
                if (type.ThrowForce == 0)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has no throw force!");
                }
                if (type.MinKnockBackResistance == 0 && type.MaxKnockBackResistance == 0)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has no knockback resistance!");
                }
                if (type.MaxKnockBackResistance < type.MinKnockBackResistance)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has invalid knockback resistance!");
                }
                int invalidspeeds = 0;
                foreach (float item in type.Speeds)
                {
                    if (item == 0)
                        invalidspeeds++;
                }
                if (invalidspeeds == type.Speeds.Length || type.Speeds.Length == 0)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has no speed!");
                }
                if (type.MeshRefernces == null)
                {
                    if (type.GrowthStagePaths.Length > type.Speeds.Length)
                    {
                        Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has more grow paths than speeds!");
                    }
                    if (type.GrowthStagePaths.Length < type.Speeds.Length)
                    {
                        Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has less grow paths than speeds!");
                    }
                }
                else
                {
                    if (type.MeshRefernces.PikminGrowthStagePlants.Length > type.Speeds.Length)
                    {
                        Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has more grow plants than speeds!");
                    }
                    if (type.MeshRefernces.PikminGrowthStagePlants.Length < type.Speeds.Length)
                    {
                        Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has less grow plants than speeds!");
                    }
                }
                if (AllowSpawnMultiplier && type.SpawnChanceMultiplier == 0)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName + " has a zero spawn chance multiplier!");
                }

                //Do spawn checks
                if (type.SpawnsIndoors)
                {
                    if (DebugMode)
                        Logger.LogInfo(" " + type.PikminName + " is an indoor pikmin type");
                    IndoorTypes.Add(type.PikminTypeID, type);
                }
                if (type.SpawnsOutdoors)
                {
                    if (DebugMode)
                        Logger.LogInfo(" " + type.PikminName + " is an outdoor pikmin type");
                    OutdoorTypes.Add(type.PikminTypeID, type);
                }
                if (type.SpawnsAsSprout)
                {
                    if (DebugMode)
                        Logger.LogInfo(" " + type.PikminName + " spawns as sprout");
                    SproutTypes.Add(type.PikminTypeID, type);
                }
                if (type.SpawnsNaturally)
                {
                    if (DebugMode)
                        Logger.LogInfo(" " + type.PikminName + " spawns naturally");
                    NaturalTypes.Add(type.PikminTypeID, type);
                }

                //Do Ship Onion Checks
                if (type.UsesPikminContainer && !SkipOnionCheck)
                {
                    if (DebugMode)
                        Logger.LogInfo(" " + type.PikminName + " uses a pikmin container");

                    OnionType ShipOnion = AssetLoader.LoadAsset<OnionType>("Assets/LethalminAssets/Pikmin/Types 2/ShipOnion.asset");
                    List<PikminType> oldTC = new List<PikminType>(ShipOnion.TypesCanHold);
                    oldTC.Add(type);
                    ShipOnion.TypesCanHold = oldTC.ToArray();
                    type.TargetOnion = ShipOnion;
                }

                //Do Animator Checks
                if (type.AttackAnimation != null)
                {
                    float timeAtFrame = (float)type.AttackAnimationHitFrame / type.AttackAnimation.frameRate;
                    if (timeAtFrame > type.AttackAnimation.stopTime)
                    {
                        Logger.LogWarning($"Pikmin type with ID {type.PikminTypeID} {type.PikminName} has an attack animation with a stop time" +
                        $" ({type.AttackAnimation.stopTime}) greater than its hit frame time {timeAtFrame}!");

                    }
                    else
                    {
                        AddEventToFrame(1, "InitHit", type.AttackAnimation);
                        AddEventToFrame(type.AttackAnimationHitFrame, "Hit", type.AttackAnimation);
                    }
                }
                if (type.NonLatchAttackAnimation != null)
                {
                    float timeAtFrame = (float)type.NonLatchAttackAnimationHitFrame / type.NonLatchAttackAnimation.frameRate;
                    if (timeAtFrame > type.NonLatchAttackAnimation.stopTime)
                    {
                        Logger.LogWarning($"Pikmin type with ID {type.PikminTypeID} {type.PikminName} has an non attack animation with a stop time" +
                        $" ({type.NonLatchAttackAnimation.stopTime}) greater than its hit frame time {timeAtFrame}!");

                    }
                    else
                    {
                        AddEventToFrame(1, "InitHit", type.NonLatchAttackAnimation);
                        AddEventToFrame(type.NonLatchAttackAnimationHitFrame, "HitCastable", type.NonLatchAttackAnimation);
                    }
                }

                //Do Finializeations
                type.MeshData.type = type;
                type.MeshData.Initalize();
                UpdateBeastairy();
                type.HasBeenRegistered = true;
                if (type.GenerateConfigFile && GeneratePConfig)
                {
                    BindPIKconfig(type, PtypeHasConfig(type) && UsePConfigs);
                }
                Logger.LogMessage("Registered Pikmin type with ID " + type.PikminTypeID + " " + type.PikminName);
            }
            else
            {
                Logger.LogWarning("Pikmin type: " + type.PikminName + " with ID " + type.PikminTypeID + " already registered!");
            }
        }
        public static void RegisterOnionType(OnionType type)
        {
            if (!type.HasBeenRegistered)
            {
                type.OnionTypeID = RegisteredOnionTypes.Count;
                RegisteredOnionTypes.Add(type.OnionTypeID, type);
                if (GetMajorMinorVersion(type.version) != GetMajorMinorVersion(MyPluginInfo.PLUGIN_VERSION))
                {
                    Logger.LogWarning("Onion type with ID " + type.OnionTypeID + " " + type.TypeName + $" has a different version {type.version} than the mod " + $"({MyPluginInfo.PLUGIN_VERSION})" + ", this may cause issues!");
                }
                if (type.TypesCanHold.Length == 0)
                {
                    Logger.LogWarning("Onion type with ID " + type.OnionTypeID + " " + type.TypeName + " has no types that can hold it!");
                }
                if (type.SpawnInAsItem)
                {
                    SpawnableOnionTypes.Add(type.OnionTypeID, type);
                }
                Logger.LogMessage("Registered Onion type with ID " + type.OnionTypeID + " " + type.TypeName);
                type.HasBeenRegistered = true;
            }
            else
            {
                Logger.LogWarning("Onion type: " + type.TypeName + " with ID " + type.OnionTypeID + " already registered!");
            }
        }
        public static void RegisterFuseRule(OnionFuseRules fuseRule)
        {
            if (!fuseRule.HasBeenRegistered)
            {
                fuseRule.FuseID = RegisteredFuseRules.Count;
                RegisteredFuseRules.Add(fuseRule.FuseID, fuseRule);
                if (GetMajorMinorVersion(fuseRule.version) != GetMajorMinorVersion(MyPluginInfo.PLUGIN_VERSION))
                {
                    Logger.LogWarning("Onion Fuse Rule with ID " + fuseRule.FuseID + " " + fuseRule.name + $" has a different version {fuseRule.version} than the mod " + $"({MyPluginInfo.PLUGIN_VERSION})" + ", this may cause issues!");
                }
                Logger.LogMessage("Registered Onion Fuse Rule with ID " + fuseRule.FuseID + " " + fuseRule.name);
                fuseRule.HasBeenRegistered = true;
            }
            else
            {
                Logger.LogWarning("Onion Fuse Rule type: " + fuseRule.name + " with ID " + fuseRule.FuseID + " already registered!");
            }
        }

        private static string GetMajorMinorVersion(string version)
        {
            int lastDotIndex = version.LastIndexOf('.');
            if (lastDotIndex != -1)
            {
                return version.Substring(0, lastDotIndex);
            }
            return version;
        }
        private void LoadWhisleAssets()
        {
            Item Whistle = AssetLoader.LoadAsset<Item>("Assets/LethalminAssets/Whisle/WhisleItem.asset");
            Item Onion = AssetLoader.LoadAsset<Item>("Assets/LethalminAssets/Onion/OnionItem.asset");
            Utilities.FixMixerGroups(Onion.spawnPrefab);
            TerminalNode WTN = AssetLoader.LoadAsset<TerminalNode>("Assets/LethalminAssets/Whisle/Shop/Whistle TN.asset");
            Whistle.isConductiveMetal = LethalWhistleValue;
            //TerminalKeyword WTK = AssetLoader.LoadAsset<TerminalKeyword>("Assets/LethalminAssets/Whisle/Shop/Whistle TK.asset");
            Items.RegisterShopItem(Whistle, null!, null!, WTN, WhistlePriceValue);
            Items.RegisterItem(Onion);
        }



        public static GameObject pikminPrefab, sproutPrefab;
        public static GameObject CallminUI, StatsUI, KilledUIelement, RasiedUIelement, LeftUIelement, InDangerUIelement, LeftElement, CounterPrefab;
        public static Sprite NoPikmin, UndefinedPikmin;
        public static Sprite DangerRanger, SaferWafer;
        public static GameObject Ghost, PikminAttackerNode;
        public static AudioClip[] AttackSFX, BornSFX, ExitOnionSFX, EnterOnionSFX, ItemNoticeSFX, GhostSFX, CarrySFX, LostSFX, YaySFX, CoughSFXs;
        public static AudioClip LiftSFX, DeadSFX, NoticeSFX, ThrowSFX, HoldSFX;
        public static Dictionary<OnionType, int> PreviousRoundPikminCounts = new Dictionary<OnionType, int>();
        public static Material lineMaterial;
        public static AudioClip WhistleSFX, DissSFX, PlayerPluckSound, PlayerPluckSound2, PurpSlam, PuffHit;
        public static AudioClip OnionMeunOpen, OnionMeunClose, PikAdd, PikSub, OnionVac, OnionSuc, OnionSpi;
        public static AudioClip[] PlayerThrowSound, RealHitSFX;
        public static GameObject PikminObjectPrefab, OnionPrefab, OnionItemPrefab, leaderManagerPrefab,
         WhistlePrefab, PmanPrefab, ManeaterScriptContainer, IdelGlowPrefab, EaterBehavior, NoticeZone;
        public static Mesh TwoSideOnion, ThreeSideOnion, FourSideOnion, FiveSideOnion, SixSideOnion, SevenSideOnion, EightSideOnion;
        public static Item OnionItem;
        public static AnimationClip PluckAnim;
        public static GameObject PuffminPrefab, POMprefab, AnimSproutPrefab, PosionPrefab;

        private void LoadPikminAssets()
        {
            PosionPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/Particles/fixed FireGasPrefabs/PikminGas.prefab");
            AnimSproutPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/AnimSprout.prefab");
            OnionMeunOpen = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Onion/Audio/UI_PikOnyonMenu_Open.wav");
            OnionMeunClose = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Onion/Audio/UI_PikOnyonMenu_Exit.wav");
            //AssetLoader.LoadAsset<AudioClip>("");
            //AssetLoader.LoadAsset<AudioClip>("");
            OnionVac = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Onion/Audio/Onyon_PelletVacuum.wav");
            OnionSuc = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Onion/Audio/Onyon_PelletVacuumFinish.wav");
            OnionSpi = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_BornSeed.wav");

            // Helper method to load audio clips
            AudioClip[] LoadAudioSet(string basePath, int count)
            {
                return LoadIncrementalSFX($"{basePath}", count);
            }

            // Helper method to load single audio clips
            AudioClip LoadSingleAudioSet(string basePath)
            {
                return AssetLoader.LoadAsset<AudioClip>($"{basePath}.wav");
            }

            // Common Pikmin types

            // Load multi-clip audio sets
            AttackSFX = LoadAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_AttackBattle", 9);

            BornSFX = LoadAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_BornGreet", 6);

            ExitOnionSFX = LoadAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_Onyon_Exit", 4);

            ItemNoticeSFX = LoadAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_Nidomi_Start", 3);

            EnterOnionSFX = LoadAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_Onyon_EnterStart", 12);

            GhostSFX = LoadAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_Ghost", 2);

            CarrySFX = LoadAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_Transport_Hold", 6);

            LostSFX = LoadAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_GiveUp", 2);


            CoughSFXs = new AudioClip[3]
            {
                AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Pikmin/Audio/p2aud/poison1.wav"),
                AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Pikmin/Audio/p2aud/poison2.wav"),
                AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Pikmin/Audio/p2aud/poison3.wav")
            };

            // Load single-clip audio sets
            LiftSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_Push_Start_04");

            DeadSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_DeadBited");

            NoticeSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_VoiceDEMO_ED2_JoinParty");

            ThrowSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_ThrownNormal");

            HoldSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/p2aud/prepare2");

            HoldSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/p2aud/prepare2");

            PlayerThrowSound = LoadAudioSet("Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_Thrown", 3);

            PuffHit = LoadSingleAudioSet("Assets/LethalminAssets/Puffmin/Audio/PuffminHit");

            // Load other single audio clips
            WhistleSFX = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Whisle/Audio/P2_whistle_Olimar.ogg.mp3");
            DissSFX = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Whisle/Audio/P2_dismiss.ogg");
            PluckAnim = AssetLoader.LoadAsset<AnimationClip>("Assets/LethalminAssets/Player/PlayerAnims/Recorded.anim");
            PurpSlam = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Pikmin/Audio/p2aud/Slam.wav");
            PlayerPluckSound = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_Pulled.wav");
            PlayerPluckSound2 = AssetLoader.LoadAsset<AudioClip>("Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_PulledOff_02.wav");

            // Load RealHitSFX
            RealHitSFX = new[]
            {
                "Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_Hit_S_51.wav",
                "Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_Hit_S_50.wav",
                "Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_Hit_S_49.wav",
                "Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_Hit_S_06.wav",
                "Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_Hit_S_62.wav",
                "Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_Hit_S_61.wav",
                "Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_Hit_S_60.wav"
            }.Select(path => AssetLoader.LoadAsset<AudioClip>(path)).ToArray();

            YaySFX = new[]
            {
                "Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_VoiceDEMO_PortalEnter.wav"
            }.Select(path => AssetLoader.LoadAsset<AudioClip>(path)).ToArray();

            // Load other assets
            pikminEnemyType = AssetLoader.LoadAsset<EnemyType>("Assets/LethalminAssets/Pikmin/Pikmin");
            pikminTerminalNode = AssetLoader.LoadAsset<TerminalNode>("Assets/LethalminAssets/Pikmin/Bestiary/Pikmin TN");
            pikminTerminalKeyword = AssetLoader.LoadAsset<TerminalKeyword>("Assets/LethalminAssets/Pikmin/Bestiary/Pikmin TK");
            PikminPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/Pikmin.prefab");
            sproutPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/Sprout.prefab");
            OnionPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/Onion.prefab");
            OnionItemPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/OnionGrabable.prefab");
            WhistlePrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Whisle/Whisle.prefab");
            leaderManagerPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/LeaderManager.prefab");
            pikminPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/Pikmin.prefab");
            Ghost = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/PikminGhost.prefab");
            PmanPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/Pikmin Manager.prefab");
            OnionItem = AssetLoader.LoadAsset<Item>("Assets/LethalminAssets/Onion/OnionItem.asset");
            PikminObjectPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/PikminItemNode.prefab");
            EaterBehavior = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/ManeaterBehavior.prefab");
            puffminEnemyType = AssetLoader.LoadAsset<EnemyType>("Assets/LethalminAssets/Puffmin/Puffmin");
            puffminTerminalNode = AssetLoader.LoadAsset<TerminalNode>("Assets/LethalminAssets/Puffmin/Bestiary/Puffmin TN");
            puffminTerminalKeyword = AssetLoader.LoadAsset<TerminalKeyword>("Assets/LethalminAssets/Puffmin/Bestiary/Puffmin TK");
            POMprefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Puffmin/PuffminOwnerManager.prefab");

            // Load UI elements
            KilledUIelement = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/HUD/Pikmin4/KillNleft.prefab");
            RasiedUIelement = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/HUD/Pikmin4/RaisedBox.prefab");
            LeftElement = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/HUD/Pikmin4/Left.prefab");
            InDangerUIelement = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/HUD/Pikmin4/PikminInDanger.prefab");
            pikminPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/Pikmin.prefab");
            CallminUI = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/Call_Return Pikmin UI.prefab");
            StatsUI = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/HUD/PikminCorner.prefab");
            Ghost = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/PikminGhost.prefab");
            PmanPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/Pikmin Manager.prefab");
            CounterPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/Counter.prefab");
            NoticeZone = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/PikminNoticeZone.prefab");

            // Load sprites
            SaferWafer = AssetLoader.LoadAsset<Sprite>("Assets/LethalminAssets/HUD/Pikmin4/Psafe.png");
            DangerRanger = AssetLoader.LoadAsset<Sprite>("Assets/LethalminAssets/HUD/Pikmin4/Pwarn.png");
            OnionItem = AssetLoader.LoadAsset<Item>("Assets/LethalminAssets/Onion/OnionItem.asset");

            // Load Picons
            NoPikmin = AssetLoader.LoadAsset<Sprite>("Assets/LethalminAssets/HUD/Pikmin4/IconsVer1_9.asset");
            UndefinedPikmin = AssetLoader.LoadAsset<Sprite>("Assets/LethalminAssets/HUD/Pikmin4/Undfin.png");

            lineMaterial = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Whisle/Materials/WhisleZone.mat");
            PikminAttackerNode = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/PikminAttackerNode.prefab");

            IdelGlowPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/IdleGlow.prefab");
            TwoSideOnion = AssetLoader.LoadAsset<Mesh>("Assets/LethalminAssets/Onion/Models/SK_stg_Onyon.022.mesh");
            ThreeSideOnion = AssetLoader.LoadAsset<Mesh>("Assets/LethalminAssets/Onion/Models/SK_stg_Onyon.023.mesh");
            FourSideOnion = AssetLoader.LoadAsset<Mesh>("Assets/LethalminAssets/Onion/Models/SK_stg_Onyon.024.mesh");
            FiveSideOnion = AssetLoader.LoadAsset<Mesh>("Assets/LethalminAssets/Onion/Models/SK_stg_Onyon.025.mesh");
            SixSideOnion = AssetLoader.LoadAsset<Mesh>("Assets/LethalminAssets/Onion/Models/SK_stg_Onyon.026.mesh");
            SevenSideOnion = AssetLoader.LoadAsset<Mesh>("Assets/LethalminAssets/Onion/Models/SK_stg_Onyon.027.mesh");
            EightSideOnion = AssetLoader.LoadAsset<Mesh>("Assets/LethalminAssets/Onion/Models/SK_stg_Onyon.028.mesh");

            PuffminPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Puffmin/Puffmin.prefab");
        }

        static AudioClip[] LoadIncrementalSFX(string baseName, int count)
        {
            AudioClip[] clips = new AudioClip[count];
            for (int i = 0; i < count; i++)
            {
                string assetName;
                if (i == 0)
                {
                    assetName = $"{baseName}.wav";
                }
                else
                {
                    assetName = $"{baseName}_{i + 1:D2}.wav";
                }
                AudioClip clip = AssetLoader.LoadAsset<AudioClip>(assetName);
                if (clip == null)
                {
                    Logger.LogWarning($"Failed to load audio clip: {assetName}");
                }
                else
                {
                    //Logger.LogMessage($"Successfully loaded audio clip: {assetName}");
                }
                clips[i] = clip;
            }
            return clips;
        }
        public static bool CanWalkAtCompany()
        {
            return IsDependencyLoaded("dev.kittenji.NavMeshInCompany");
        }

        private void RegisterPikminEnemy()
        {
            if (pikminEnemyType == null || pikminTerminalNode == null || pikminTerminalKeyword == null || PikminPrefab == null)
            {
                Logger.LogError("One or more Pikmin assets are null. Cannot register enemy.");
                return;
            }

            // Ensure the PikminPrefab has a NetworkObject component
            if (PikminPrefab.GetComponent<NetworkObject>() == null)
            {
                PikminPrefab.AddComponent<NetworkObject>();
                Logger.LogInfo("Added NetworkObject component to PikminPrefab");
            }

            // Register the Pikmin enemy with LethalLib, including TerminalNode and TerminalKeyword
            PikminAI ai = PikminPrefab.AddComponent<PikminAI>();
            //PikminPrefab.transform.Find("WaterDetector").gameObject.AddComponent<PikminWaterDetector>().parentScript = PikminPrefab.GetComponent<PikminAI>();
            ai.enemyType = pikminEnemyType;
            //ai.creatureVoice = PikminPrefab.transform.Find("CreatureVoice").GetComponent<AudioSource>();
            //ai.creatureSFX = PikminPrefab.transform.Find("CreatureSFX").GetComponent<AudioSource>();
            ai.eye = PikminPrefab.transform.Find("Eye");
            pikminPrefab.GetComponentInChildren<EnemyAICollisionDetect>().mainScript = ai;
            ai.openDoorSpeedMultiplier = pikminEnemyType.doorSpeedMultiplier;

            ai.LocalSFX = pikminPrefab.transform.Find("CreatureSFX").GetComponent<AudioSource>();
            ai.LocalVoice = pikminPrefab.transform.Find("CreatureVoice").GetComponent<AudioSource>();
            ai.DrowingAud = pikminPrefab.transform.Find("Drown").GetComponent<AudioSource>();
            ai.HoldPos = pikminPrefab.transform.Find("HoldPos");
            IdelGlowPrefab.GetComponentInChildren<SpriteRenderer>().gameObject.AddComponent<LookAtMainCamera>();

            //PikminPrefab.GetComponent<PikminAI>().enemyBehaviourStates = new EnemyBehaviourState[Enum.GetValues(typeof(PState)).Length];
            AddEventToFrame(1, "InitHit", AssetLoader.LoadAsset<AnimationClip>("Assets/LethalminAssets/Pikmin/Animations/AttackPlaceHolder2.anim"));
            AddEventToFrame(1, "InitHit", AssetLoader.LoadAsset<AnimationClip>("Assets/LethalminAssets/Pikmin/Animations/AttackStandingPlaceholder.anim"));
            AddEventToFrame(20, "Hit", AssetLoader.LoadAsset<AnimationClip>("Assets/LethalminAssets/Pikmin/Animations/AttackPlaceHolder2.anim"));
            AddEventToFrame(20, "HitCastable", AssetLoader.LoadAsset<AnimationClip>("Assets/LethalminAssets/Pikmin/Animations/AttackStandingPlaceholder.anim"));
            //AddEventToFrame(65, "Pluck", PluckAnim);
            pikminTerminalNode.displayText = "__PLACEHOLDER__";
            pikminEnemyType.spawningDisabled = DontNatualSpawn;
            Enemies.RegisterEnemy(pikminEnemyType, 50, Levels.LevelTypes.All, pikminTerminalNode, pikminTerminalKeyword);

            //RegisterPikminType(AssetLoader.LoadAsset<)

            Logger.LogInfo("Pikmin enemy registered successfully!");

            UnlockableItemDef OnionShipItem = AssetLoader.LoadAsset<UnlockableItemDef>("Assets/LethalminAssets/Onion/ShipOnionItem.asset");
            Unlockables.RegisterUnlockable(OnionShipItem, ContianerPriceValue, StoreType.ShipUpgrade);

            Logger.LogInfo("Faucet registered successfully!");

            // Register the Puffmin enemy with LethalLib, including TerminalNode and TerminalKeyword
            PuffminAI ai2 = PuffminPrefab.AddComponent<PuffminAI>();
            //PuffminPrefab.transform.Find("WaterDetector").gameObject.AddComponent<PuffminWaterDetector>().parentScript = PuffminPrefab.GetComponent<PuffminAI>();
            ai2.enemyType = puffminEnemyType;
            //ai2.creatureVoice = PuffminPrefab.transform.Find("CreatureVoice").GetComponent<AudioSource>();
            //ai2.creatureSFX = PuffminPrefab.transform.Find("CreatureSFX").GetComponent<AudioSource>();
            ai2.eye = PuffminPrefab.transform.Find("Eye");
            PuffminPrefab.GetComponentInChildren<EnemyAICollisionDetect>().mainScript = ai2;
            ai2.openDoorSpeedMultiplier = puffminEnemyType.doorSpeedMultiplier;

            ai2.LocalSFX = PuffminPrefab.transform.Find("CreatureSFX").GetComponent<AudioSource>();
            ai2.LocalVoice = PuffminPrefab.transform.Find("CreatureVoice").GetComponent<AudioSource>();
            Enemies.RegisterEnemy(puffminEnemyType, 0, Levels.LevelTypes.All, puffminTerminalNode, puffminTerminalKeyword);

            Logger.LogInfo("Puffmin enemy registered successfully!");

            AssetLoader.LoadAsset<PikminType>("Assets/LethalminAssets/Pikmin/Types 2/PurplePikmin.asset").PikminScripts = new[] { new PurplePikmin() };
            PikminType[] RPtypes = new[] {
            AssetLoader.LoadAsset<PikminType>("Assets/LethalminAssets/Pikmin/Types/RedPikmin.asset"),
            AssetLoader.LoadAsset<PikminType>("Assets/LethalminAssets/Pikmin/Types/YellowPikmin.asset"),
            AssetLoader.LoadAsset<PikminType>("Assets/LethalminAssets/Pikmin/Types/BluePikmin.asset"),
            AssetLoader.LoadAsset<PikminType>("Assets/LethalminAssets/Pikmin/Types 2/WhitePikmin.asset"),
            AssetLoader.LoadAsset<PikminType>("Assets/LethalminAssets/Pikmin/Types 2/PurplePikmin.asset"),
            AssetLoader.LoadAsset<PikminType>("Assets/LethalminAssets/Pikmin/Types 2/Bulbmin.asset")
             };
            foreach (var item in RPtypes)
            {
                RegisterPikminType(item, true);
            }
            OnionType[] ROnionTypes = new[] {
            AssetLoader.LoadAsset<OnionType>("Assets/LethalminAssets/Pikmin/Types/RedOnion.asset"),
            AssetLoader.LoadAsset<OnionType>("Assets/LethalminAssets/Pikmin/Types/YellowOnion.asset"),
            AssetLoader.LoadAsset<OnionType>("Assets/LethalminAssets/Pikmin/Types/BlueOnion.asset"),
            AssetLoader.LoadAsset<OnionType>("Assets/LethalminAssets/Pikmin/Types 2/ShipOnion.asset") };
            foreach (var item in ROnionTypes)
            {
                RegisterOnionType(item);
            }
            OnionFuseRules[] ROnionFuseRules = new[] {
            AssetLoader.LoadAsset<OnionFuseRules>("Assets/LethalminAssets/Pikmin/Types/RBYFuse.asset"),};
            foreach (var item in ROnionFuseRules)
            {
                RegisterFuseRule(item);
            }

            Loader = new GameObject().AddComponent<AssetBundleLoader>();
            Loader.name = "LethalMin's AssetBundleLoader";
        }
        public static void AddEventToFrame(int frame, string functionName2, AnimationClip animationClip)
        {
            if (animationClip == null)
            {
                LethalMin.Logger.LogError("Animation clip is not assigned!");
                return;
            }

            // Create a new AnimationEvent
            AnimationEvent animEvent = new AnimationEvent();

            // Set the time of the event (convert frame to time)
            float timeAtFrame = (float)frame / animationClip.frameRate;
            animEvent.time = timeAtFrame;

            // Set the function to call
            animEvent.functionName = functionName2;

            // Optionally, add a parameter
            // animEvent.stringParameter = "YourStringParameter";
            // animEvent.floatParameter = 1.5f;
            // animEvent.intParameter = 1;
            // animEvent.objectReferenceParameter = someObject;

            // Add the event to the animation clip
            animationClip.AddEvent(animEvent);

            Logger.LogInfo($"Added event at frame {frame} (time: {timeAtFrame}) for {animationClip.name}");
        }

        public static void UpdateBeastairy()
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            // Append scientific names
            sb.AppendLine("Scientific names:");
            foreach (var pikminType in RegisteredPikminTypes.Values)
            {
                sb.AppendLine($"{pikminType.ScientificName}");
            }
            sb.AppendLine();

            // Append types and bestiary entries
            sb2.AppendLine("TYPES:");
            sb2.AppendLine();
            foreach (var pikminType in RegisteredPikminTypes.Values)
            {
                sb.AppendLine(pikminType.beastiarySegment);
                sb.AppendLine();
            }
            // Replace the existing line with this:

            pikminTerminalNode.displayText = $@"Pikmin
Sigurd's danger level: -100% (They're our friends!)

{sb}

These small, plant-like creatures are a remarkable example of convergent evolution between flora and fauna.
Pikmin are characterized by their flower growing from their stem and their distinctive stem-like bodies.
They exhibit complex social behaviors and an innate desire to follow leadership, making them invaluable companions for scrap collectors.
While harmless to humans, Pikmin demonstrate remarkable strength in numbers, capable of carrying items many times their size when working together. 
They are also capable of attacking and carrying away small creatures, making them a valuable asset in the field.

{sb2}

FIELD NOTES:
Use the Company Store's whistle ({WhistlePriceValue} credits) to command nearby Pikmin
Pikmin automatically follow the nearest player if idle
Can be thrown at items to initiate carrying or at enemies to attack
Finding Onions allows Pikmin to be preserved between moons
Purple and White Pikmin require the Pikmin Container ({ContianerPriceValue} credits) to be kept between moons
EXTREMELY VULNERABLE to all hazards - keep them away from anything dangerous!

NOTE FROM SIGURD: These little guys are amazing! Just don't get attached...
I lost 47 of them to a single Jester yesterday. Still hurts to think about it...";
        }

        // For IndoorTypes
        public static PikminType GetRandomIndoorPikminType()
        {
            var indoorTypes = LethalMin.IndoorTypes.Values.ToList();
            return indoorTypes[UnityEngine.Random.Range(0, indoorTypes.Count)];
        }

        // For OutdoorTypes
        public static PikminType GetRandomOutdoorPikminType()
        {
            var outdoorTypes = LethalMin.OutdoorTypes.Values.ToList();
            return outdoorTypes[UnityEngine.Random.Range(0, outdoorTypes.Count)];
        }

        public static bool IsDependencyLoaded(string pluginGUID)
        {
            return Chainloader.PluginInfos.ContainsKey(pluginGUID);
        }

        public static string[] GetLayerNamesFromMask(LayerMask layerMask)
        {
            List<string> layerNames = new List<string>();
            for (int i = 0; i < 32; i++) // Unity supports up to 32 layers
            {
                int layerBit = 1 << i;
                if ((layerMask.value & layerBit) != 0)
                {
                    string layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        layerNames.Add(layerName);
                    }
                }
            }
            return layerNames.ToArray();
        }

        private void NetcodePatcher()
        {
            Type[] types = GetTypesWithErrorHandling();
            foreach (var type in types)
            {
                try
                {
                    var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        try
                        {
                            var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                            if (attributes.Length > 0)
                            {
                                method.Invoke(null, null);
                            }
                        }
                        catch (Exception methodException)
                        {
                            Logger.LogWarning($"Error invoking method {method.Name} in type {type.FullName}: {methodException.Message}");
                            if (methodException.InnerException != null)
                            {
                                Logger.LogWarning($"Inner exception: {methodException.InnerException.Message}");
                            }
                        }
                    }
                }
                catch (Exception typeException)
                {
                    Logger.LogWarning($"Error processing type {type.FullName}: {typeException.Message}");
                }
            }

            Logger.LogInfo("NetcodePatcher completed.");
        }
        public static List<Type> LibraryTypes = new List<Type>();
        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            try
            {
                // Get all types from the executing assembly
                Type[] types = GetTypesWithErrorHandling();

                // Patch everything except FilterEnemyTypesPatch
                foreach (var type in types)
                {
                    if (type.Namespace == "LethalMin.Library")
                    {
                        LibraryTypes.Add(type);
                        continue;
                    }
                    if (!IsDependencyLoaded("LethalMon") && type == typeof(FilterEnemyTypesPatch))
                    {
                        Logger.LogDebug($"Skipping FilterEnemyTypesPatch script. Because LethalMon is not installed");
                        continue;
                    }
                    if (!IsDependencyLoaded("Piggy.PiggyVarietyMod") &&
                    (type == typeof(PiggyTeslaGatePatch) || type == typeof(PiggyTouchTriggerPatch)))
                    {
                        Logger.LogDebug("Skipping VarietyMod scripts. Because Piggy.PiggyVarietyMod is not installed");
                        continue;
                    }
                    if (!IsDependencyLoaded("Piggy.LCOffice") && type == typeof(PiggyElevatorSystemPatch))
                    {
                        Logger.LogDebug("Skipping LC office scripts. Because Piggy.LCOffice is not installed");
                        continue;
                    }
                    if (!IsDependencyLoaded("kite.ZelevatorCode") && type == typeof(EndlessElevatorPatch))
                    {
                        Logger.LogDebug("Skipping Zelevator scripts. Because kite.ZelevatorCode is not installed");
                        continue;
                    }
                    try
                    {
                        Harmony.PatchAll(type);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Error patching type {type.FullName}: {e.Message}");
                        if (e.InnerException != null)
                        {
                            Logger.LogError($"Inner exception: {e.InnerException.Message}");
                        }
                    }
                }

                if (IsDependencyLoaded("LethalMon"))
                {
                    Logger.LogMessage("LethalMon detected. Patching FilterEnemyTypesPatch.");
                }
                if (IsDependencyLoaded("Piggy.PiggyVarietyMod"))
                {
                    Logger.LogMessage("Variety Mod detected. Patching TeslaGates and TouchTriggers.");
                }
                if (IsDependencyLoaded("Piggy.LCOffice"))
                {
                    Logger.LogMessage("LC Office Detected. Patching Elevator.");
                }
                if (IsDependencyLoaded("kite.ZelevatorCode"))
                {
                    Logger.LogMessage("Zeranos Moon Detected. Patching Elevator.");
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error during patching process: {e.Message}");
                if (e.InnerException != null)
                {
                    Logger.LogError($"Inner exception: {e.InnerException.Message}");
                }
            }

            Logger.LogDebug("Finished patching!");
        }

        public static void PatchLibraryTypes()
        {
            Logger.LogMessage("LethalMin Recived Library Call! Patching Library Types...");
            foreach (var type in LibraryTypes)
            {
                try
                {
                    Harmony.PatchAll(type);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error patching type {type.FullName}: {e.Message}");
                    if (e.InnerException != null)
                    {
                        Logger.LogError($"Inner exception: {e.InnerException.Message}");
                    }
                }
            }
        }
        private static Type[] GetTypesWithErrorHandling()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                Logger.LogWarning("ReflectionTypeLoadException caught while getting types. Some types will be skipped.");
                foreach (var loaderException in e.LoaderExceptions)
                {
                    Logger.LogWarning($"Loader Exception: {loaderException.Message}");
                    if (loaderException is FileNotFoundException fileNotFound)
                    {
                        Logger.LogWarning($"Could not load file: {fileNotFound.FileName}");
                    }
                }
                return e.Types.Where(t => t != null).ToArray();
            }
            catch (Exception e)
            {
                Logger.LogError($"Unexpected error while getting types: {e.Message}");
                return new Type[0];
            }
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }
    }
}
