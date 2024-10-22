using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using BepInEx.Configuration;
using LethalLib.Extras;
using LethalMin.Patches;
using System.Text;
using UnityEngine.AI;

namespace LethalMin
{
    public enum pSFX
    {
        Pikmin2,
        Pikmin3,
        Pikmin4
    }
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("evaisa.lethallib")]
    [BepInDependency("twig.latecompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("dev.kittenji.NavMeshInCompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("MaxWasUnavailable.LethalModDataLib", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("LethalMon", BepInDependency.DependencyFlags.SoftDependency)]
    public class LethalMin : BaseUnityPlugin
    {
        public static LethalMin Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }
        public static bool SmartMinMov = true, SmarterMinMov = false;
        public static string ThrowAction;
        public static string SwitchForwardAction;
        public static string SwitchBackwawrdsAction;
        public static string WhisleAction;
        public static string DismissAction;
        public static string ManeaterName = "Maneater";
        public static EnemyType pikminEnemyType = null!;
        private static TerminalNode pikminTerminalNode = null!;
        private TerminalKeyword pikminTerminalKeyword = null!;
        public GameObject PikminPrefab = null!;
        public static Dictionary<int, PikminType> RegisteredPikminTypes = new Dictionary<int, PikminType>();
        public static Dictionary<int, PikminType> IndoorTypes = new Dictionary<int, PikminType>();
        public static Dictionary<int, PikminType> OutdoorTypes = new Dictionary<int, PikminType>();
        public static Dictionary<int, PikminType> SproutTypes = new Dictionary<int, PikminType>();
        public static Dictionary<int, OnionType> RegisteredOnionTypes = new Dictionary<int, OnionType>();
        public static Dictionary<int, OnionType> SpawnableOnionTypes = new Dictionary<int, OnionType>();
        public static Dictionary<int, OnionFuseRules> RegisteredFuseRules = new Dictionary<int, OnionFuseRules>();
        public LayerMask PikminColideable = 1107298561 | (1 << 28);
        public static AssetBundleLoader Loader = null!;

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
        public static ObstacleAvoidanceType PikminDefultAvoidanceType, PikminCarryingAvoidanceType;
        //public LayerMask PikminColideable_DECREPAED = 1107298561 | (1 << 19) | (1 << 28);

        public static ConfigEntry<bool> SkipPluckAnimation, FF, Smartmin, Smartermin, OnlyMainV, OnlyExitV, Pattack,
        CarrySpeedConfig, LethalSpiderConfig, LethalJesterConfig, LethalThumperConfig, LethalGiantConfig, LethaDogs,
        LethaDogs2, LethalBugsConfig, LethalBarberConfig, LethalMechConfig, LethalBB, LethalHydro, LethalB, BeeChase,
        CustomOnionAllowed, LethalWhistle, LethalLandmines, AllToPItems, LimmitItemGrab, AllowOnionFuseConfig,
        LethalManEaterConfig, CalmableManeaterConfig, Rasisium, NotFormidableOak, LethalTurrentsC, InvinciMin,
        StrudyMin, UselessblueMin, DebugM, FunniMode, PassiveToManEaterConfig, FFOM, FFM, TeleEle, TeleCarie,
        TargetCarConfig, GetToDaCar, AllowSpawnMultiplierCF;

        public static ConfigEntry<float> Pscale, Sscale, ChaseR, PCPX, PCPY, PCPZ, PCRX, PCRY, PCRZ, PCScale,
         PCPCountX, PCPCountY, PCPCountZ, PCRCCountX, PCRCCountY, PCRCCountZ, PCScaleCount, FallTimer, CounterOffset,
         NoticeTimer, BarberR, OnionSpawnChance, SproutSpawnChance, IndoorSpawnChance, WhistleVolumeConfig,
         ManagerRefreshRateC;

        public static ConfigEntry<int> MechBurnLimmitConfig, JesterDiet, ThumperDiet, GiantDiet, BarberDiet, ManeaterDiet, SpideDiet,
        JesterBuffer, ThumperBuffer, SpiderBuffer, BeesShockCountConfig, ManeaterBuffer, MaxMin
        , WhistlePrice, ContianerPrice, ItemRequireSubracter;

        public static ConfigEntry<string> throwActionConfig, switchForwardConfig,
        switchBackwardsConfig, whistleActionConfig, dismissActionConfig;
        public static ConfigEntry<ObstacleAvoidanceType> PikminDefaultAvoidanceTypeConfig;
        public static ConfigEntry<ObstacleAvoidanceType> PikminCarryingAvoidanceTypeConfig;
        #endregion

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

            // load everything third
            LoadPikminAssets();

            LoadWhisleAssets();

            // Register everything fourth
            RegisterPikminEnemy();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

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
            LethalBugsConfig = Config.Bind("Enemy AI", "Make Hoarding Bugs eat Pikmin", false, "Makes Hoarding Bugs attack Pikmin if a pikmin attempts to grab it's");
            BeesShockCountConfig = Config.Bind("Enemy AI", "Bee Shock Count", 3, "The max ammount of bees that can shock a pikmin at a time");
            BeeChase = Config.Bind("Enemy AI", "Make bees chase Pikmin", false, "Makes Bees chase Pikmin when their hive goes missing");

            PCPX = Config.Bind("HUD", "PikminSelected(XPos)", 262.78f, "The X position of the selected pikmin UI element");
            PCPY = Config.Bind("HUD", "PikminSelected(YPos)", -106f, "The Y position of the selected pikmin UI element");
            PCPZ = Config.Bind("HUD", "PikminSelected(ZPos)", -59.767f, "The Z position of the selected pikmin UI element");
            PCRX = Config.Bind("HUD", "PikminSelected(RotX)", 0f, "The X rotation of the selected pikmin UI element");
            PCRY = Config.Bind("HUD", "PikminSelected(RotY)", 12f, "The Y rotation of the selected pikmin UI element");
            PCRZ = Config.Bind("HUD", "PikminSelected(RotZ)", 0f, "The Z rotation of the selected pikmin UI element");
            PCScale = Config.Bind("HUD", "PikminSelected(Scale)", 0.677937f, "The scale of the selected pikmin UI element");
            PCPCountX = Config.Bind("HUD", "PikminCount(XPos)", 268.4f, "The X position of the pikmin count UI element");
            PCPCountY = Config.Bind("HUD", "PikminCount(YPos)", -165.4f, "The Y position of the pikmin count UI element");
            PCPCountZ = Config.Bind("HUD", "PikminCount(ZPos)", -45.4f, "The Z position of the pikmin count UI element");
            PCRCCountX = Config.Bind("HUD", "PikminCount(RotX)", 0f, "The X rotation of the pikmin count UI element");
            PCRCCountY = Config.Bind("HUD", "PikminCount(RotY)", 12f, "The Y rotation of the pikmin count UI element");
            PCRCCountZ = Config.Bind("HUD", "PikminCount(RotZ)", 0f, "The Z rotation of the pikmin count UI element");
            PCScaleCount = Config.Bind("HUD", "PikminCount(Scale)", 0.6698875f, "The scale of the pikmin count UI element");

            throwActionConfig = Config.Bind("Controls", "Throw Action", "<Keyboard>/4", "Key binding for throwing Pikmin");
            switchForwardConfig = Config.Bind("Controls", "Switch Forward", "<Keyboard>/3", "Key binding for switching Pikmin type forward");
            switchBackwardsConfig = Config.Bind("Controls", "Switch Backwards", "<Keyboard>/2", "Key binding for switching Pikmin type backwards");
            whistleActionConfig = Config.Bind("Controls", "Whistle Action", "<Mouse>/leftButton", "Key binding for whistling");
            dismissActionConfig = Config.Bind("Controls", "Dismiss Action", "<Mouse>/middleButton", "Key binding for dismissing Pikmin");

            CustomOnionAllowed = Config.Bind("Extra", "Allow Custom Onion spawn Position", true, "Allows onions to land on pre defined spawn points on modded moons (if there are any).");
            AllowSpawnMultiplierCF = Config.Bind("Extra", "Allow Spawn Multiplier", true, "Allows the custom Pikmin Types to use Spawn Multipliers.");
            LethalWhistle = Config.Bind("Extra", "Make whistle conductive", false, "Makes whistles conductive to stormy weather.");
            LethalLandmines = Config.Bind("Extra", "Make Pikmin Trigger Landmines", true, "Allows pikmin to trigger landmines");
            LethalTurrentsC = Config.Bind("Extra", "Make Turrents Kill Pikmin", true, "Allows pikmin to get shot by turrents");
            AllToPItems = Config.Bind("Extra", "Make every item carrieable by pikmin", false, "Allows pikmin to carry any item, includeing items that are not ment to be held by entites such as the shotgun.");
            FallTimer = Config.Bind("Extra", "Max fall duration", 10f, "The max ammount of time pikmin can be in the air before they automaticly land, in case they fell out of bounds or are stuck.");
            CounterOffset = Config.Bind("Extra", "Item Fraction Y Position Offset", 2.5f, "Offsets the Y position of the item fraction");
            NoticeTimer = Config.Bind("Extra", "Working state change timer", 1f, "The time it takes for a player to stand next to a pikmin before they notice them if they are carrying an item.");
            LimmitItemGrab = Config.Bind("Extra", "Limmit Pikmin on item", true, "Limmits the max ammount of pikmin that can grab an item.");
            AllowOnionFuseConfig = Config.Bind("Extra", "Allow Onion Fuse", true, "Allows onions to fuse after the ship leaves.");
            WhistleVolumeConfig = Config.Bind("Extra", "Whistle Volume", 1f, "The volume of the whistle sound (I'm only implumenting this because the whistle sound is bugged and I can't fix it)");
            ManagerRefreshRateC = Config.Bind("Extra", "PikminManager Refersh Rate", 0.75f, "The rate at which the PikminManager refreshes it's object refernces. Warning! Having this value too low could cause lag.");

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

            FFOM = Config.Bind("LethalMon", "Make Pikmin Attack Leaders Tammed Enemy", false, "Makes Pikmin attack the leaders Pokémon");
            FFM = Config.Bind("LethalMon", "Make Pikmin Attack Tammed Enemies", false, "Makes Pikmin attack any Tamed Enemies");
            #endregion






            #region Setting Config values
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
            #endregion






            #region Setting Config Events
            // Add SettingChanged events for all configs
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
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(Pattack, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(Pscale, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(Sscale, false));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(ChaseR, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(CarrySpeedConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(TargetCarConfig, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(GetToDaCar, false));
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<ObstacleAvoidanceType>(PikminDefaultAvoidanceTypeConfig, false));
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<ObstacleAvoidanceType>(PikminCarryingAvoidanceTypeConfig, false));

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

            // HUD
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
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(DebugM, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(FunniMode, true));

            // Lethal Mon            
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(FFOM, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(FFM, false));
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
                .Select(gameObject => gameObject.GetComponent<PikminAI>())
                .Where(pikmin => pikmin != null && Vector3.Distance(position, pikmin.transform.position) <= maxDistance)
                .OrderBy(pikmin => Vector3.Distance(position, pikmin.transform.position))
                .Take(maxCount)
                .ToList();

        }
        public static List<PikminAI> FindPikminInBox(Vector3 position, Vector3 size, int maxCount)
        {
            List<PikminAI> ais = new List<PikminAI>();
            foreach (var item in Physics.OverlapBox(position, size))
            {
                if (item.gameObject.GetComponent<PikminAI>() != null && ais.Contains(item.gameObject.GetComponent<PikminAI>()) == false)
                    ais.Add(item.gameObject.GetComponent<PikminAI>());
                if (item.gameObject.GetComponentInParent<PikminAI>() != null && ais.Contains(item.gameObject.GetComponentInParent<PikminAI>()) == false)
                    ais.Add(item.gameObject.GetComponentInParent<PikminAI>());
            }
            ais.Take(maxCount);
            return ais;
        }
        public static List<PikminAI> FindPikminInSphere(Vector3 position, float size, int maxCount)
        {
            List<PikminAI> ais = new List<PikminAI>();
            foreach (var item in Physics.OverlapSphere(position, size))
            {
                if (item.gameObject.GetComponent<PikminAI>() != null && ais.Contains(item.gameObject.GetComponent<PikminAI>()) == false)
                    ais.Add(item.gameObject.GetComponent<PikminAI>());
                if (item.gameObject.GetComponentInParent<PikminAI>() != null && ais.Contains(item.gameObject.GetComponentInParent<PikminAI>()) == false)
                    ais.Add(item.gameObject.GetComponentInParent<PikminAI>());
            }
            ais.Take(maxCount);
            return ais;
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


        public static void RegisterPikminType(PikminType type)
        {
            if (!type.HasBeenRegistered)
            {
                if (type.MeshPrefab == null)
                {
                    Logger.LogError("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has no mesh prefab, skipping registration!");
                    return;
                }
                if (type.MeshPrefab.GetComponent<NetworkObject>() == null)
                {
                    //Logger.LogError("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " mesh prefab has no network object, skipping registration!");;
                    //return;
                }
                if (type.PikminScripts != null && type.PikminScripts.Length > 0)
                {
                    GameObject container = LethalMin.pikminPrefab.transform.Find("PikminScriptContainer").gameObject;
                    LethalMin.Logger.LogInfo(type.GetName() + " has " + type.PikminScripts.Length + " scripts");
                    foreach (var script in type.PikminScripts)
                    {
                        container.AddComponent(script.GetType());
                        LethalMin.Logger.LogInfo("Added " + script.GetType().Name);
                    }
                }
                type.PikminTypeID = RegisteredPikminTypes.Count;
                RegisteredPikminTypes.Add(type.PikminTypeID, type);
                if (GetMajorMinorVersion(type.version) != GetMajorMinorVersion(MyPluginInfo.PLUGIN_VERSION))
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has a different version than the mod " + $"({GetMajorMinorVersion(MyPluginInfo.PLUGIN_VERSION)})" + ", this may cause issues!");
                }

                //Do Invalid Checks
                if (type.PikminIcon == null)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has no icon!");
                    type.PikminIcon = UndefinedPikmin;
                }
                if (type.soundPack == null)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has no soundPack!");
                }
                if (type.ThrowForce == 0)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has no throw force!");
                }
                if (type.MinKnockBackResistance == 0 && type.MaxKnockBackResistance == 0)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has no knockback resistance!");
                }
                if (type.MaxKnockBackResistance < type.MinKnockBackResistance)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has invalid knockback resistance!");
                }

                int invalidspeeds = 0;

                foreach (float item in type.Speeds)
                {
                    if (item == 0)
                        invalidspeeds++;
                }

                if (invalidspeeds == type.Speeds.Length || type.Speeds.Length == 0)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has no speed!");
                }

                if (type.GrowthStagePaths.Length > type.Speeds.Length)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has more grow paths than speeds!");
                }

                if (type.GrowthStagePaths.Length < type.Speeds.Length)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has less grow paths than speeds!");
                }
                if (AllowSpawnMultiplier && type.SpawnChanceMultiplier == 0)
                {
                    Logger.LogWarning("Pikmin type with ID " + type.PikminTypeID + " " + type.GetName() + " has a zero spawn chance multiplier!");
                }

                if (type.SpawnsIndoors)
                {
                    if (DebugMode)
                        Logger.LogInfo(" " + type.GetName() + " is an indoor pikmin type");
                    IndoorTypes.Add(type.PikminTypeID, type);
                }
                if (type.SpawnsOutdoors)
                {
                    if (DebugMode)
                        Logger.LogInfo(" " + type.GetName() + " is an outdoor pikmin type");
                    OutdoorTypes.Add(type.PikminTypeID, type);
                }
                if (type.SpawnsAsSprout)
                {
                    if (DebugMode)
                        Logger.LogInfo(" " + type.GetName() + " spawns as sprout");
                    SproutTypes.Add(type.PikminTypeID, type);
                }
                type.MeshData.type = type;
                type.MeshData.Initalize();
                UpdateBeastairy();
                type.HasBeenRegistered = true;
                Logger.LogMessage("Registered Pikmin type with ID " + type.PikminTypeID + " " + type.GetName());
            }
            else
            {
                Logger.LogWarning("Pikmin type: " + type.GetName() + " with ID " + type.PikminTypeID + " already registered!");
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
                    Logger.LogWarning("Pikmin type with ID " + type.OnionTypeID + " " + type.TypeName + " has a different version than the mod " + $"({MyPluginInfo.PLUGIN_VERSION})" + ", this may cause issues!");
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
                    Logger.LogWarning("Pikmin type with ID " + fuseRule.FuseID + " " + fuseRule.name + " has a different version than the mod " + $"({MyPluginInfo.PLUGIN_VERSION})" + ", this may cause issues!");
                }
                Logger.LogMessage("Registered Onion type with ID " + fuseRule.FuseID + " " + fuseRule.name);
                fuseRule.HasBeenRegistered = true;
            }
            else
            {
                Logger.LogWarning("Onion type: " + fuseRule.name + " with ID " + fuseRule.FuseID + " already registered!");
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
            Items.RegisterShopItem(Whistle, null, null, WTN, WhistlePriceValue);
            Items.RegisterItem(Onion);
        }



        public static GameObject pikminPrefab, sproutPrefab;
        public static GameObject CallminUI, StatsUI, KilledUIelement, RasiedUIelement, LeftUIelement, InDangerUIelement, LeftElement, CounterPrefab;
        public static Sprite NoPikmin, UndefinedPikmin;
        public static Sprite DangerRanger, SaferWafer;
        public static GameObject Ghost, PikminAttackerNode;
        public static AudioClip[] AttackSFX, BornSFX, ExitOnionSFX, EnterOnionSFX, ItemNoticeSFX, GhostSFX, CarrySFX, LostSFX, YaySFX;
        public static AudioClip LiftSFX, DeadSFX, NoticeSFX, ThrowSFX, HoldSFX;
        public static Dictionary<OnionType, int> PreviousRoundPikminCounts = new Dictionary<OnionType, int>();
        public static Material lineMaterial;
        public static AudioClip WhistleSFX, DissSFX, PlayerPluckSound, PlayerPluckSound2,
        OnionMeunOpen, OnionMeunClose, PikAdd, PikSub, PurpSlam;
        public static AudioClip[] PlayerThrowSound, RealHitSFX;
        public static GameObject PikminObjectPrefab, OnionPrefab, OnionItemPrefab, leaderManagerPrefab,
         WhistlePrefab, PmanPrefab, ManeaterScriptContainer, IdelGlowPrefab, EaterBehavior;
        public static Mesh TwoSideOnion, ThreeSideOnion, FourSideOnion, FiveSideOnion, SixSideOnion, SevenSideOnion, EightSideOnion;
        public static Item OnionItem;
        public static AnimationClip PluckAnim;

        private void LoadPikminAssets()
        {

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

            // Load single-clip audio sets
            LiftSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_Push_Start_04");

            DeadSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_DeadBited");

            NoticeSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_VoiceDEMO_ED2_JoinParty");

            ThrowSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/RBY/PikminNormal_Voice_ThrownNormal");

            HoldSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/p2aud/prepare2");

            HoldSFX = LoadSingleAudioSet("Assets/LethalminAssets/Pikmin/Audio/p2aud/prepare2");

            PlayerThrowSound = LoadAudioSet("Assets/LethalminAssets/Pikmin/Audio/Com/Pikmin_Thrown", 3);

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
            pikminPrefab.transform.Find("PikminColision").GetComponent<EnemyAICollisionDetect>().mainScript = ai;
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
            pikminTerminalNode.displayText = "__UNDER REWRITEING__";
            Enemies.RegisterEnemy(pikminEnemyType, 50, Levels.LevelTypes.All, pikminTerminalNode, pikminTerminalKeyword);

            //RegisterPikminType(AssetLoader.LoadAsset<)

            Logger.LogInfo("Pikmin enemy registered successfully!");

            UnlockableItemDef OnionShipItem = AssetLoader.LoadAsset<UnlockableItemDef>("Assets/LethalminAssets/Onion/ShipOnionItem.asset");
            Unlockables.RegisterUnlockable(OnionShipItem, ContianerPriceValue, StoreType.ShipUpgrade);

            Logger.LogInfo("Faucet registered successfully!");

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
                RegisterPikminType(item);
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
        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            // Patch everything except FilterEnemyTypesPatch
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type != typeof(FilterEnemyTypesPatch));

            foreach (var type in types)
            {
                Harmony.PatchAll(type);
            }

            // Only patch FilterEnemyTypesPatch if LethalMon is loaded
            if (IsDependencyLoaded("LethalMon"))  // Replace with actual LethalMon GUID
            {
                Logger.LogInfo("LethalMon detected. Patching FilterEnemyTypesPatch.");
                Harmony.PatchAll(typeof(FilterEnemyTypesPatch));
            }
            else
            {
                //Logger.LogInfo("LethalMon not detected. Skipping FilterEnemyTypesPatch.");
            }

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }

        private void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}
