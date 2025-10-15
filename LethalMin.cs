using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using LethalMin.Pikmin;
using LethalMin.Utils;
using BepInEx.Bootstrap;
using LethalConfig;
using LethalLib.Extras;
using LethalLib.Modules;
using LobbyCompatibility.Features;
using LobbyCompatibility.Enums;
using LethalMin.HUD;
using BepInEx.Configuration;
using Dusk;
using LethalMin.Achivements;

namespace LethalMin
{
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
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("giosuel.Imperium", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("pacoito.itolib", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.github.teamxiaolan.dawnlib.dusk", BepInDependency.DependencyFlags.SoftDependency)]
    public class LethalMin : BaseUnityPlugin
    {
        public static LethalMin Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }
        internal static AssetBundle assetBundle { get; private set; } = null!;
        public static Dictionary<int, PikminType> RegisteredPikminTypes { get; private set; } = new Dictionary<int, PikminType>();
        public static Dictionary<int, OnionType> RegisteredOnionTypes { get; private set; } = new Dictionary<int, OnionType>();
        public static Dictionary<int, OnionFuseRules> RegisteredFuseRules = new Dictionary<int, OnionFuseRules>();
        public static List<PikminType> NaturalSpawningTypes { get; private set; } = new List<PikminType>();
        public static Dictionary<int, PiklopediaEntry> RegisteredEntries { get; private set; } = new Dictionary<int, PiklopediaEntry>();
        public static List<int> EnemyIDsOverridenByPiklopedia = new List<int>();
        public static InputClass InputClassInstace { get; private set; } = null!;
        public static LayerMask PikminColideable = 1107298561 | (1 << 28);
        public static EnemyType PikminEnemyType = null!;
        public static EnemyType PuffminEnemyType = null!;
        public static PikminSoundPack DefaultSoundPack = null!;
        public static PikminSoundPack Pikmin2DefaultSoundPack = null!;
        public static PikminSoundPack Pikmin3DefaultSoundPack = null!;

        public static List<EnemyType> EnemyTypes => Resources.FindObjectsOfTypeAll<EnemyType>()
            .Where(e => e.enemyPrefab != null)
            .GroupBy(e => e.enemyPrefab)
            .Select(g => g.First())
            .ToList();

        public static List<Item> ItemTypes => Resources.FindObjectsOfTypeAll<Item>()
            .Where(i => i.spawnPrefab != null)
            .GroupBy(i => i.spawnPrefab)
            .Select(i => i.First())
            .ToList();

        public static List<OnionType> OnionTypes => RegisteredOnionTypes.Values
            .Where(ot => ot.SpawnsIndoors || ot.SpawnsOutdoors)
            .ToList();

        public static GameObject PikminGhostPrefab = null!;
        public static GameObject SproutPrefab = null!;
        public static GameObject OnionPrefab = null!;
        public static GameObject DefultPikminSproutMesh = null!;
        public static GameObject DefultOnionMesh = null!;
        public static AnimationClip PlayerPluckAnim = null!;
        public static GameObject OnionHUDSlotPrefab = null!;
        public static GameObject SingleOnionHUDSlotPrefab = null!;
        public static GameObject OnionItemPrefab = null!;
        public static GameObject DefultOnionItemModel = null!;
        public static GameObject GlowPrefab = null!;
        public static GameObject PikminZapPrefab = null!;
        public static GameObject PikminFirePrefab = null!;
        public static GameObject PikminGasPrefab = null!;
        public static GameObject PlayerSproutPrefab = null!;
        public static GameObject NoticeZonePrefab = null!;
        public static GameObject EnemyItemNode = null!;
        public static GameObject GlowSeedPrefab = null!;
        public static GameObject ItemCounterPrefab = null!;
        public static GameObject GrabPosPrefab = null!;
        public static OnionSoundPack DefaultOnionSoundPack = null!;
        public static OnionType ShipOnionType = null!;
        public static GameObject SSRenviourment = null!;
        public static Vector3 enviormentStartPos = Vector3.zero;
        public const string FullEnglishAlphabetUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string FullEnglishAlphabetLower = "abcdefghijklmnopqrstuvwxyz";
        public const string FullEnglishAlhabet = FullEnglishAlphabetUpper + FullEnglishAlphabetLower;
        public const string FullNumbers = "0123456789";
        public static object AchivementController = null!;
        public static bool UsingAchivements => IsDependencyLoaded("com.github.teamxiaolan.dawnlib.dusk") && AchivementController != null;


        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;
            InputClassInstace = new InputClass();

            Logger.LogInfo("LethalMin: Initializing LethalMin");
            Logger.LogInfo("LethalMin: Netcode Patching");
            NetcodePatcher();
            Logger.LogInfo("LethalMin: Patching");
            Patch();
            Logger.LogInfo("LethalMin: Binding Configs");
            BindConfigs();
            Logger.LogInfo("LethalMin: Loading AssetBundle");
            LoadAssetBundle();
            Logger.LogInfo($"LethalMin: Loading Miscellaneous Assets");
            LoadMiscAssets();
            Logger.LogInfo("LethalMin: Registering Pikmin As Enemies");
            RegisterPikmin();
            Logger.LogInfo("LethalMin: Registering Items");
            RegisterItems();
            Logger.LogInfo("LethalMin: Registering Pikmin Types");
            RegisterPikminTypes();
            Logger.LogInfo("LethalMin: Registering Onion Types");
            RegisterOnionTypes();
            Logger.LogInfo("LethalMin: Registering Onion Fuse Rules");
            RegisterFuseRules();
            Logger.LogInfo($"LethalMin: Registering Custom Piklopedia Entries");
            RegisterPiklopediaEntries();
            if (IsDependencyLoaded("ainavt.lc.lethalconfig"))
            {
                BindLCconfigs();
            }
            if (IsDependencyLoaded("BMX.LobbyCompatibility"))
            {
                RegisterLobbyCompat();
            }
            if (IsDependencyLoaded("com.github.teamxiaolan.dawnlib.dusk"))
            {
                RegisterDuskModCompat();
            }

            GameObject go = new GameObject("PikminGenerationManager");
            go.AddComponent<GenerationManager>();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        public static void RegisterItems()
        {
            TerminalNode CreateTerminalNodeA(string displayText, Item itm)
            {
                string name = $"{itm.itemName.Replace(" ", "-")}InfoNode";
                return CreateTerminalNode(displayText, name);
            }
            TerminalNode CreateTerminalNodeB(string displayText, UnlockableItemDef def)
            {
                string name = $"{def.unlockable.unlockableName.Replace(" ", "-")}InfoNode";
                return CreateTerminalNode(displayText, name);
            }
            TerminalNode CreateTerminalNode(string displayText, string name)
            {
                var terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
                terminalNode.name = name;
                terminalNode.clearPreviousText = true;
                terminalNode.maxCharactersToType = 25;
                terminalNode.displayText = displayText;
                return terminalNode;
            }

            // Register the whistle item
            Item whistle = assetBundle.LoadAsset<Item>("Assets/LethalMin/Whistle/WhisleItem.asset");
            whistle.isConductiveMetal = IsWhistleConductive;
            var whistleInfo = CreateTerminalNodeA($"A whistle that can be used to call to your squad Pikmin easier.\n\n", whistle);
            Items.RegisterShopItem(whistle, null!, null!, whistleInfo, WhistlePriceConfig.InternalValue);

            // Register the ship onion upgrade
            UnlockableItemDef shipOnion = assetBundle.LoadAsset<UnlockableItemDef>("Assets/LethalMin/OnionAssets/ShipOnionDef.asset");
            var shipOnionInfo = CreateTerminalNodeB($"Used to keep Purple and White Pikmin between moons.\n\n", shipOnion);
            Unlockables.RegisterUnlockable(shipOnion, StoreType.ShipUpgrade, null!, null!, shipOnionInfo, ShipOnionPriceConfig.InternalValue);

            // Register the callback control upgrade
            UnlockableItemDef callbackControl = assetBundle.LoadAsset<UnlockableItemDef>("Assets/LethalMin/Whistle/CallBackControlDef.asset");
            float cooldown = 300f;
            string time = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(cooldown / 60), Mathf.FloorToInt(cooldown % 60));
            var callbackInfo = CreateTerminalNodeB($"A device that emits a large signal that calls any " +
            $"(non wild) pikmin to the ship. Goes on cooldown for ({time}) \n\n", callbackControl);
            Unlockables.RegisterUnlockable(callbackControl, StoreType.ShipUpgrade, null!, null!, callbackInfo, ShipWhistlePriceConfig.InternalValue);

            // Register the PluckAPhone item
            Item pluckaPhone = assetBundle.LoadAsset<Item>("Assets/LethalMin/Whistle/PluckAPhoneItem.asset");
            var pluckaPhoneInfo = CreateTerminalNodeA($"A more powerful whistle that can call pikmin with a bigger radius and pluck Pikmin from the ground. \n\n", pluckaPhone);
            Items.RegisterShopItem(pluckaPhone, null!, null!, pluckaPhoneInfo, PluckaPhonePriceConfig.InternalValue);

            // Register the glow seed item (doesn't need terminal node)
            Item glowSeed = assetBundle.LoadAsset<Item>("Assets/LethalMin/Types/Glow Pikmin/GlowSeedItm.asset");
            Items.RegisterItem(glowSeed);
        }

        public static void InitCustomDefaultSoundPacks(List<PikminSoundPack> soundPacks)
        {
            int count = 0;
            foreach (var soundPack in soundPacks)
            {
                if (!soundPack.IsDefaultSoundPack)
                {
                    continue;
                }
                soundPack.InitalizeDict();
                count++;
            }
            if (count > 0)
            {
                Logger.LogInfo($"Initialized {count} custom default sound packs.");
            }
        }
        public static void InitDefaultSoundPacks()
        {
            List<PikminSoundPack> soundPacks = assetBundle.LoadAllAssets<PikminSoundPack>().ToList();
            foreach (var soundPack in soundPacks)
            {
                if (!soundPack.IsDefaultSoundPack)
                {
                    continue;
                }
                soundPack.InitalizeDict();
            }
            DefaultSoundPack = assetBundle.LoadAsset<PikminSoundPack>("Assets/LethalMin/Types/DeafultSoundPack.asset");
            Pikmin2DefaultSoundPack = assetBundle.LoadAsset<PikminSoundPack>("Assets/LethalMin/Types/Pik2DeafultSoundPack.asset");
        }

        private static void RegisterTypes<T>(List<T> types, Dictionary<int, T> registry, string logName,
            Func<T, string> nameSelector, Action<T, int> registerAction)
        {
            if (types.Count == 0)
            {
                return;
            }

            Logger.LogInfo($"Registering {types.Count} {logName}");

            // Sort the types based on name
            types.Sort((a, b) => string.Compare(nameSelector(a), nameSelector(b), StringComparison.Ordinal));

            // Register the types in the sorted order
            int i = registry.Count;
            foreach (T item in types)
            {
                registerAction(item, i);
                i++;
            }
        }

        private static void RegisterTypesFromAssetBundle<T>(Dictionary<int, T> registry, string logName,
            Func<T, string> nameSelector, Action<T, int> registerAction) where T : ScriptableObject
        {
            // Load all T assets from your asset bundle
            List<T> loadedTypes = assetBundle.LoadAllAssets<T>().ToList();
            RegisterTypes(loadedTypes, registry, logName, nameSelector, registerAction);
        }

        // Replace your existing methods with these simplified versions:
        public static void RegisterCustomPikminTypes(List<PikminType> CustomTypes)
        {
            RegisterTypes(CustomTypes, RegisteredPikminTypes, "Custom Types",
                type => type.PikminName, RegisterPikminType);
        }

        public static void RegisterPikminTypes()
        {
            InitDefaultSoundPacks();
            RegisterTypesFromAssetBundle(RegisteredPikminTypes, "Pikmin Types",
                type => type.PikminName, RegisterPikminType);
        }

        public static void RegisterPiklopediaEntries()
        {
            RegisterTypesFromAssetBundle(RegisteredEntries, "Piklopedia Entries",
                entry => entry.EntryName, RegisterPiklopediaEntry);
        }

        public static void RegisterCustomPiklopediaEntries(List<PiklopediaEntry> loadedEntries)
        {
            RegisterTypes(loadedEntries, RegisteredEntries, "Custom Piklopedia Entries",
                entry => entry.EntryName, RegisterPiklopediaEntry);
        }

        public static void RegisterOnionTypes()
        {
            RegisterTypesFromAssetBundle(RegisteredOnionTypes, "Onion Types",
                type => type.TypeName, RegisterOnionType);
        }

        public static void RegisterCustomOnionTypes(List<OnionType> CustomTypes)
        {
            RegisterTypes(CustomTypes, RegisteredOnionTypes, "Custom Onion Types",
                type => type.TypeName, RegisterOnionType);
        }

        public static void RegisterFuseRules()
        {
            RegisterTypesFromAssetBundle(RegisteredFuseRules, "Fuse Rules",
                rules => rules.name, RegisterFuseRule);
        }

        public static void RegisterCustomFuseRules(List<OnionFuseRules> loadedTypes)
        {
            RegisterTypes(loadedTypes, RegisteredFuseRules, "Custom fuse rules",
                rules => rules.name, RegisterFuseRule);
        }

        private static void RegisterPiklopediaEntry(PiklopediaEntry entry, int index)
        {
            int id = RegisteredEntries.Count > 0 ? RegisteredEntries.Keys.Max() + 1 : 0;

            if (RegisteredEntries.ContainsKey(entry.PiklopediaID))
            {
                Logger.LogWarning($"Piklopedia Entry: {entry.EntryName} with ID {entry.PiklopediaID} has already been registered.");
                return;
            }

            if (entry.PiklopediaKeyword == null)
            {
                entry.PiklopediaKeyword = PikUtils.CreateTerminalKeyword(
                    $"Piklopedia{entry.EntryName}",
                    entry.EntryName.ToLower().Replace(" ", "") + " info",
                    entry.PiklopediaNode);
            }

            entry.PiklopediaID = id;
            RegisteredEntries[id] = entry;
        }

        internal static void LoadAssetBundle()
        {
            string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (assemblyLocation == null)
            {
                throw new InvalidOperationException("Unable to determine assembly location.");
            }

            string bundlePath = Path.Combine(assemblyLocation, "lethalminassets");
            assetBundle = AssetBundle.LoadFromFile(bundlePath);

            if (assetBundle == null)
            {
                throw new InvalidOperationException("Failed to load AssetBundle.");
            }
        }

        public void LoadMiscAssets()
        {
            PikminGhostPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PikminGhost.prefab");
            SproutPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Sprout.prefab");
            DefultPikminSproutMesh = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Types/DefultSproutMesh.prefab");
            PlayerPluckAnim = assetBundle.LoadAsset<AnimationClip>("Assets/LethalMin/Animations/PlayerPluckNew.anim");
            OnionPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Onion.prefab");
            DefultOnionMesh = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/OnionAssets/DefaultOnionMesh.prefab");
            OnionHUDSlotPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/OnionAssets/TypeSlot.prefab");
            SingleOnionHUDSlotPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/OnionAssets/TypeUI.prefab");
            OnionItemPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/OnionAssets/OnionGrabable.prefab");
            DefultOnionItemModel = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/OnionAssets/DefaultOnionItemMesh.prefab");
            GlowPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/IdleGlow.prefab");
            PikminZapPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Particles/elecpikiparticle/PikminZap.prefab");
            PikminFirePrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Particles/fixed FireGasPrefabs/PikminFire.prefab");
            PikminGasPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Particles/fixed FireGasPrefabs/PikminGas.prefab");
            PlayerSproutPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PlayerSprout.prefab");
            DefaultOnionSoundPack = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/OnionAssets/DefaultSoundPack.prefab").GetComponent<OnionSoundPack>();
            NoticeZonePrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Puffmin/PuffminWhistleZone.prefab");
            EnemyItemNode = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/EnemyGrabbableItem.prefab");
            GlowSeedPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Types/Glow Pikmin/GlowSeed.prefab");
            ItemCounterPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PikminItemCounter.prefab");
            GrabPosPrefab = assetBundle.LoadAsset<GameObject>("Assets/LethalMin/GrabPos.prefab");
            ShipOnionType = assetBundle.LoadAsset<OnionType>("Assets/LethalMin/Types/ShipOnion.asset");
        }

        public void RegisterPikmin()
        {
            EnemyType PikminType = assetBundle.LoadAsset<EnemyType>("Assets/LethalMin/PikminEnemyType.asset");
            TerminalNode node = assetBundle.LoadAsset<TerminalNode>("Assets/LethalMin/Pikmin TN.asset");
            PikminType.spawningDisabled = UseLCSpawnSystem ? false : true;
            Enemies.RegisterEnemy(PikminType, null, null, node);
            PikminEnemyType = PikminType;
            PikminAI ai = PikminEnemyType.enemyPrefab.GetComponent<PikminAI>();
            ai.enabled = false;
            PikminEnemyType.enemyPrefab.GetComponent<PikminTypeResolver>().PikminAIs.Add(ai);

            EnemyType PuffminType = assetBundle.LoadAsset<EnemyType>("Assets/LethalMin/Puffmin/PuffminEnemyType.asset");
            TerminalNode puffminNode = assetBundle.LoadAsset<TerminalNode>("Assets/LethalMin/Puffmin/PuffminTN.asset");
            Enemies.RegisterEnemy(PuffminType, null, null, puffminNode);
            PuffminEnemyType = PuffminType;
        }

        internal static void RegisterPikminType(PikminType Ptype, int id)
        {
            Logger.LogMessage($"Registering Pikmin Type: {Ptype.PikminName}");
            if (RegisteredPikminTypes.ContainsKey(Ptype.PikminTypeID))
            {
                Logger.LogWarning($"Pikmin Type: {Ptype.PikminName} with ID {Ptype.PikminTypeID} has already been registered.");
                return;
            }

            if (GeneratePikminConfigs && Ptype.GenerateConfigFile)
            {
                GeneratePikminTypeConfig(Ptype);
            }

            ConfigItem<bool> PikCFG = new ConfigItem<bool>(
             "Pikmin",
             Ptype.PikminName,
             true,
             $"Enable {Ptype.PikminName} Pikmin",
             true,
             ConfigItemAuthority.Host);

            if (!PikCFG)
            {
                Logger.LogWarning($"Pikmin Type: {Ptype.PikminName} is disabled in the config. Skipping registration.");
                return;
            }
            if (Ptype.DisableRegistration)
            {
                Logger.LogWarning($"Pikmin Type: {Ptype.PikminName} is set to be disabled. Skipping registration.");
                return;
            }

            if (Ptype.ModelPrefab == null)
            {
                Logger.LogError($"Pikmin Type: {Ptype.PikminName} has no model prefab!");
                return;
            }
            if (Ptype.modelRefernces == null)
            {
                Logger.LogError($"Pikmin Type: {Ptype.PikminName} has no model references!");
                return;
            }
            if (Ptype.ModelPrefab.GetComponentInChildren<PikminAnimatorController>() == null)
            {
                Logger.LogWarning($"Pikmin Type: {Ptype.PikminName} has no animator controller!");
            }
            if (Ptype.PikminIcon == null)
            {
                Logger.LogWarning($"Pikmin Type: {Ptype.PikminName} has no icon!");
            }
            if (Ptype.SoundPack == null)
            {
                Ptype.SoundPack = DefaultSoundPack;
                Logger.LogWarning($"Pikmin Type: {Ptype.PikminName} has no Sound Pack! Using default.");
            }
            if (Ptype.TargetOnion != null && !Ptype.TargetOnion.TypesCanHold.ToList().Contains(Ptype))
            {
                if (Ptype.TargetOnion.AllowPikminToBeAddedToOnion == false)
                {
                    Logger.LogWarning($"Pikmin Type: {Ptype.PikminName} has a Target Onion that does not contain this type in its TypesCanHold list! Won't add it.");
                }
                else
                {
                    Logger.LogWarning($"Pikmin Type: {Ptype.PikminName} has a Target Onion that does not contain this type in its TypesCanHold list! Adding it now.");
                    Array.Resize(ref Ptype.TargetOnion.TypesCanHold, Ptype.TargetOnion.TypesCanHold.Length + 1);
                    Ptype.TargetOnion.TypesCanHold[Ptype.TargetOnion.TypesCanHold.Length - 1] = Ptype;
                }
            }
            else if (!Ptype.SoundPack.IsDefaultSoundPack)
            {
                Ptype.SoundPack.InitalizeDict();
            }
            if (Ptype.UsesPikminContainer && !ShipOnionType.TypesCanHold.ToList().Contains(Ptype))
            {
                Logger.LogInfo($"Pikmin Type: {Ptype.PikminName} has UsesPikminContainer set to true, adding it to the Ship Onion's TypesCanHold list.");
                Array.Resize(ref ShipOnionType.TypesCanHold, ShipOnionType.TypesCanHold.Length + 1);
                ShipOnionType.TypesCanHold[ShipOnionType.TypesCanHold.Length - 1] = Ptype;
            }
            if (Ptype.UsePiklopedia)
            {
                string str = "";
                if (Ptype.UsePresetFormatting)
                {
                    string name = Ptype.piklopediaName == "" ? Ptype.PikminName : Ptype.piklopediaName;
                    str = name
                    + "\n"
                    + "\n"
                    + $"Sigurd's helpfulness level: {Ptype.HelpfulLevel}"
                    + "\n"
                    + "\n"
                    + $"Scientific name: {Ptype.ScientificName}"
                    + "\n"
                    + "\n"
                    + $"{Ptype.piklopediaDescription}"
                    + "\n"
                    + "\n";
                }
                else
                {
                    str = Ptype.piklopediaDescription;
                }
                if (string.IsNullOrEmpty(Ptype.piklopediaDescription))
                {
                    Ptype.piklopediaDescription = $"Type Id {id}, with empty description.";
                    Logger.LogWarning($"Pikmin Type: {Ptype.PikminName} has no description and uses Piklopedia!");
                }

                TerminalNode PikNode = null!;
                if (Ptype.OverridePiklopediaNode == null)
                {
                    PikNode = PikUtils.CreateTerminalNode($"TerminalNode{Ptype.PikminName}", str);
                    PikNode.creatureName = string.IsNullOrEmpty(Ptype.piklopediaName) ? Ptype.PikminName.Replace(" ", "-") : Ptype.piklopediaName;
                    if (!string.IsNullOrEmpty(Ptype.piklopediaName) && string.IsNullOrWhiteSpace(Ptype.piklopediaName))
                    {
                        Logger.LogWarning($"{Ptype.PikminName} White space counts as the pikolopedia name not being empty, Pikmin name may show as empty in the terminal");
                    }
                    PikNode.displayVideo = Ptype.piklopediaVideo;
                    PikNode.loadImageSlowly = true;
                }
                else
                {
                    PikNode = Ptype.OverridePiklopediaNode;
                }

                TerminalKeyword PikKeyword = null!;
                if (Ptype.OverridePiklopediaKeyword == null)
                {
                    PikKeyword = PikUtils.CreateTerminalKeyword($"TerminalKeyword{Ptype.PikminName}", Ptype.PikminName.ToLower().Replace(" ", "") + " info", PikNode);
                }
                else
                {
                    PikKeyword = Ptype.OverridePiklopediaKeyword;
                }

                PiklopediaEntry entry = ScriptableObject.CreateInstance<PiklopediaEntry>();
                entry.name = $"PiklopediaEntry{Ptype.PikminName}";
                entry.EntryName = PikNode.creatureName;
                Ptype.PiklopediaEntry = entry;
                entry.PiklopediaID = id;
                entry.PiklopediaKeyword = PikKeyword;
                entry.PiklopediaNode = PikNode;
                RegisteredEntries[id] = entry;
            }
            for (int i = 0; i < Ptype.SoundPackGenerations.Length; i++)
            {
                PikminSoundPack sp = Ptype.SoundPackGenerations[i];
                if (sp == null)
                {
                    Logger.LogWarning($"Pikmin Type: {Ptype.PikminName} has a null SoundPack for generation {i}! Defaulting to DefaultSoundPack.");
                    Ptype.SoundPackGenerations[i] = DefaultSoundPack; // Fallback to default if null
                }
                else if (!sp.IsDefaultSoundPack)
                {
                    sp.InitalizeDict();
                }
            }
            if (Ptype.CustomTypeScript != null)
            {
                PikminAI TypeAdded = AddCustomScriptToPikminAI(Ptype.CustomTypeScript);
                Destroy(Ptype.CustomTypeScript);
                Ptype.CustomTypeScript = TypeAdded;
            }

            PikminAnimationPack? pack = Ptype.modelRefernces.AnimatorController?.AnimPack;
            if (pack != null)
            {
                pack.AddEventsToOneshotIdleAnims();
            }

            foreach (PikminModelGeneration gen in Ptype.modelRefernces.Generations)
            {
                PikminAnimationPack? Gpack = gen.AnimatorController?.AnimPack;
                if (Gpack != null)
                {
                    Gpack.AddEventsToOneshotIdleAnims();
                }
            }

            Ptype.PikminTypeID = id;
            RegisteredPikminTypes[id] = Ptype;
            if (Ptype.SpawnsNaturally)
            {
                NaturalSpawningTypes.Add(Ptype);
            }

            Logger.LogMessage($"Registered Pikmin Type: {Ptype.PikminName} with ID: {id}");
        }

        internal static void RegisterOnionType(OnionType Otype, int id)
        {
            Logger.LogMessage($"Registering Onion Type: {Otype.TypeName}");
            if (RegisteredOnionTypes.ContainsKey(id))
            {
                Logger.LogWarning($"Onion Type with ID {id} has already been registered.");
                return;
            }
            int UnregistedType = 0;
            foreach (PikminType type in Otype.TypesCanHold)
            {
                if (!RegisteredPikminTypes.ContainsKey(type.PikminTypeID))
                {
                    Logger.LogDebug($"Onion Type: {Otype.TypeName} has a type {type.PikminName} that is not registered! Skipping registration.");
                    UnregistedType++;
                }
            }
            if (UnregistedType == Otype.TypesCanHold.Length)
            {
                Logger.LogWarning($"Onion Type: {Otype.TypeName} has no registered types! Skipping registration.");
                return;
            }
            Otype.OnionTypeID = id;
            RegisteredOnionTypes[id] = Otype;
            Logger.LogMessage($"Registered Onion Type: {Otype.TypeName} with ID: {id}");
        }

        internal static void RegisterFuseRule(OnionFuseRules FuseR, int id)
        {
            Logger.LogMessage($"Registering Fuse Rules: {FuseR.name}");
            if (RegisteredFuseRules.ContainsKey(id))
            {
                Logger.LogWarning($"Fuse Rules with ID {id} has already been registered.");
                return;
            }
            FuseR.FuseRulesTypeID = id;
            RegisteredFuseRules[id] = FuseR;
            Logger.LogMessage($"Registered Fuse Rules: {FuseR.name} with ID: {id}");
        }

        public void RegisterLobbyCompat()
        {
            PluginHelper.RegisterPlugin(MyPluginInfo.PLUGIN_GUID, new Version(MyPluginInfo.PLUGIN_VERSION), CompatibilityLevel.Everyone, VersionStrictness.Patch);
        }

        public void RegisterDuskModCompat()
        {
            Logger.LogInfo("LethalMin: Registering DuskMod Compat");
            string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (assemblyLocation == null)
            {
                throw new InvalidOperationException("Unable to determine assembly location.");
            }

            string duskModBundlePath = Path.Combine(assemblyLocation, "lethalminassetsdawnlib");
            AssetBundle duskModAssetBundle = AssetBundle.LoadFromFile(duskModBundlePath);
            string achivmentsPath = Path.Combine(assemblyLocation, "lethalminachivements");
            AssetBundle achivmentsBundle = AssetBundle.LoadFromFile(achivmentsPath);

            if (duskModAssetBundle == null)
            {
                throw new InvalidOperationException("Failed to load DuskMod AssetBundle.");
            }
            if (achivmentsBundle == null)
            {
                throw new InvalidOperationException("Failed to load Achivments AssetBundle.");
            }
            DuskMod DM = DuskMod.RegisterMod(this, duskModAssetBundle);
            DM.Content.assetBundles.Add(new AssetBundleData
            {
                assetBundleName = "lethalminachivements",
                AlwaysKeepLoaded = true,
                configName = "LethalMinAchivements",
            });
            DefaultContentHandler contentHandler = new DefaultContentHandler(DM);
            contentHandler.LoadAllContent(new DefaultBundle(achivmentsBundle)
            {
                AssetBundleData = DM.Content.assetBundles[0]
            }); // Because I do NOT want to add a whole sub directory just for a single bundle
 
            Type achivmentControllerType = typeof(AchivementController);
            if (achivmentControllerType != null)
            {
                AchivementController = Activator.CreateInstance(achivmentControllerType, DM);
                ((AchivementController)AchivementController).ModAssetBundle = duskModAssetBundle;
                ((AchivementController)AchivementController).AchivementAssetBundle = achivmentsBundle;
            }
            else
            {
                Logger.LogError("Failed to find AchivementController type.");
            }
        }


        #region CONFIG

        //public static List<object> ConfigItems = new List<object>();

        #region General
        public static ConfigItem<int> MaxPikmin = null!;
        public static ConfigItem<bool> FriendlyFire = null!;
        public static ConfigItem<bool> SkipPluckAnimation = null!;
        public static ConfigItem<float> WhistleVolume = null!;
        public static ConfigItem<bool> AllowOnionsToRevivePlayers = null!;
        public static ConfigItem<bool> AllowOnionToReviveMaskeds = null!;
        public static ConfigItem<bool> AllowOnionFusing = null!;
        public static ConfigItem<bool> HideResultsWhenMoreThanFour = null!;
        public static ConfigItem<bool> ConvertEnemyBodiesToItems = null!;
        public static ConfigItem<bool> ShowOnionsInSpace = null!;
        public static ConfigItem<Vector3> SpaceOnionPosition = null!;
        public static ConfigItem<bool> DontUpdateSpaceOnionPosition = null!;
        public static ConfigItem<bool> MakeItemsScanable = null!;
        public static ConfigItem<bool> MakePikminScanable = null!;
        public static ConfigItem<bool> TriggerLandmines = null!;
        public static ConfigItem<bool> GlobalGhostSFX = null!;
        public static ConfigItem<bool> DieInPlayerDeathZone = null!;
        public static ConfigItem<bool> TakeItemsFromPikmin = null!;
        #endregion

        #region Apperance
        public static ConfigItem<float> PikminScale = null!;
        public static ConfigItem<float> SproutScale = null!;
        public static ConfigItem<int> CurWhistPack = null!;
        public static ConfigItem<bool> BigEyesEnabled = null!;
        public static ConfigItem<PikminScanNodeProperties.ScanNodeType> PikminScanNodeColorType = null!;
        #endregion

        #region Generations
        public static ConfigItem<PikminGeneration> DefaultGeneration = null!;
        public static ConfigItem<CfgPikminGeneration> PikminModelGeneration = null!;
        public static ConfigItem<CfgPikminGeneration> SoulSpriteGeneration = null!;
        public static ConfigItem<CfgPikminGeneration> SproutModelGeneration = null!;
        public static ConfigItem<CfgPikminGeneration> OnionModelGeneration = null!;
        public static ConfigItem<CfgPikminGeneration> OnionItemModelGeneration = null!;
        public static ConfigItem<CfgPikminGeneration> PikminSoundGeneration = null!;
        public static ConfigItem<CfgPikminGeneration> PuffminGeneration = null!;
        #endregion

        #region Controls        
        public static ConfigItem<string> ThrowAction = null!;
        public static ConfigItem<string> ThrowCancelAction = null!;
        public static ConfigItem<string> SwitchForwardAction = null!;
        public static ConfigItem<string> SwitchBackwawrdsAction = null!;
        public static ConfigItem<string> WhisleAction = null!;
        public static ConfigItem<string> DismissAction = null!;
        public static ConfigItem<string> SwitchWhistleSoundAction = null!;
        public static ConfigItem<string> ChargeAction = null!;
        public static ConfigItem<string> GlowmobAction = null!;
        public static ConfigItem<string> OnionHudSpeedAction = null!;
        #endregion

        #region Spawning
        public static ConfigItem<bool> UsePowerLVForSprouts = null!;
        public static ConfigItem<bool> UseLCSpawnSystem = null!;
        public static ConfigItem<float> IndoorSpawnChance = null!;
        public static ConfigItem<float> OutdoorSpawnChance = null!;
        public static ConfigItem<float> OnionSpawnChance = null!;
        #endregion

        #region Pikmin Behavior
        public static ConfigItem<List<string>> AttackBlacklistConfig = null!;
        public static ConfigItem<List<string>> ItemBlacklistConfig = null!;
        public static ConfigItem<List<string>> EnemyBodyConvertBlacklistConfig = null!;
        public static ConfigItem<bool> UseExitsWhenCarryingItems = null!;
        public static ConfigItem<PfollowMode> PikminFollowMode = null!;
        public static ConfigItem<float> TimeFallingFailSafe = null!;
        public static ConfigItem<bool> CarryNonScrapItemsToCompany = null!;
        public static ConfigItem<bool> IgnoreNonScrapItemsToCompany = null!;
        public static ConfigItem<bool> TakeItemsToOnionOnCompany = null!;
        public static ConfigItem<bool> TakeItemsToTheCar = null!;
        public static ConfigItem<bool> TakeItemsToTheOnion = null!;
        public static ConfigItem<bool> DisperseWhenDismissed = null!;
        public static ConfigItem<bool> AllowOnBBtobeGrabed = null!;
        public static ConfigItem<float> DismissWindownTime = null!;
        public static ConfigItem<bool> WildPikminCarry = null!;
        public static ConfigItem<bool> WildPikminAttack = null!;
        public static ConfigItem<bool> WildPikminAttackDamage = null!;
        public static ConfigItem<bool> WildPikminNoDeath = null!;
        #endregion

        #region Enemy Behavior
        public static ConfigItem<bool> UseConfigsForEnemies = null!;

        // ClaySurgeon
        public static ConfigItem<float> ClaySurgeon_SnipCooldown = null!;
        public static ConfigItem<int> ClaySurgeon_SnipLimit = null!;
        // Crawler
        public static ConfigItem<float> Crawler_BiteCooldown = null!;
        public static ConfigItem<int> Crawler_BiteLimit = null!;
        // SandSpider
        public static ConfigItem<float> SandSpider_BiteCooldown = null!;
        public static ConfigItem<int> SandSpider_BiteLimit = null!;
        // HoarderBug
        public static ConfigItem<float> HoarderBug_BiteCooldown = null!;
        public static ConfigItem<int> HoarderBug_BiteLimit = null!;
        public static ConfigItem<bool> HoarderBug_AggroWhenPikminTakesItem = null!;
        // MouthDog
        public static ConfigItem<float> MouthDog_BiteCooldown = null!;
        public static ConfigItem<int> MouthDog_BiteLimit = null!;
        // Blob
        public static ConfigItem<bool> Blob_TrapPikmin = null!;
        public static ConfigItem<bool> Blob_KillPikmin = null!;
        // RedLocustBees
        public static ConfigItem<float> RedLocustBees_ZapCooldown = null!;
        public static ConfigItem<bool> RedLocustBees_ZapPikmin = null!;
        public static ConfigItem<bool> RedLocustBees_KillPikmin = null!;
        // CaveDweller
        public static ConfigItem<bool> CaveDweller_AllowCarry = null!;
        public static ConfigItem<bool> CaveDweller_AttackAsBaby = null!;
        public static ConfigItem<float> CaveDweller_BiteCooldown = null!;
        public static ConfigItem<int> CaveDweller_BiteLimit = null!;
        // RadMech
        public static ConfigItem<int> RadMech_GrabLimmit = null!;
        public static ConfigItem<bool> RadMech_GrabsPikmin = null!;
        // ForestGiant
        public static ConfigItem<int> ForestGiant_GrabLimit = null!;
        public static ConfigItem<bool> ForestGiant_GrabsPikmin = null!;
        // Jester
        public static ConfigItem<float> Jester_BiteCooldown = null!;
        public static ConfigItem<int> Jester_BiteLimit = null!;
        // ButlerEnemy
        public static ConfigItem<float> ButlerEnemy_StabCooldown = null!;
        public static ConfigItem<int> ButlerEnemy_StabLimit = null!;
        // BaboonBird
        public static ConfigItem<float> BaboonBird_BiteCooldown = null!;
        // MaskedPlayerEnemey
        public static ConfigItem<bool> MaskedPlayerEnemy_ConvertPikmin = null!;
        // Puffer
        public static ConfigItem<bool> Puffer_CanPoisonPikmin = null!;
        // Company
        public static ConfigItem<bool> Company_GrabsPikmin = null!;
        public static ConfigItem<bool> Company_HearsPikmin = null!;
        #endregion

        #region Puffmin Behavior
        public static ConfigItem<bool> TurnToNormalOnDeath = null!;
        #endregion

        #region HUD
        public static ConfigItem<PikminHUDManager.HUDLayoutPresets> HUDPreset = null!;
        public static ConfigItem<Vector3> PikminSelectionPosition = null!;
        public static ConfigItem<Vector3> PikminSelectionRotation = null!;
        public static ConfigItem<float> PikminSelectionScale = null!;
        public static ConfigItem<float> PikminSelectionAlpha = null!;
        public static ConfigItem<bool> EnableCurSlot = null!;
        public static ConfigItem<bool> EnableNextSlot = null!;
        public static ConfigItem<bool> EnablePreviousSlot = null!;
        public static ConfigItem<Vector3> PikminCounterPosition = null!;
        public static ConfigItem<Vector3> PikminCounterRotation = null!;
        public static ConfigItem<float> PikminCounterScale = null!;
        public static ConfigItem<float> PikminCounterAlphaActive = null!;
        public static ConfigItem<float> PikminCounterAlphaIdle = null!;
        public static ConfigItem<bool> EnableInExistanceCounter = null!;
        public static ConfigItem<bool> EnableInSquadCounter = null!;
        public static ConfigItem<bool> EnableInFieldCounter = null!;
        public static ConfigItem<bool> HideSelectedWhenScanNotifcation = null!;
        public static ConfigItem<bool> DontUpdateHudConfigs = null!;
        public static ConfigItem<bool> GrayoutButtonsInOnionHUD = null!;

        #endregion

        #region Items
        public static ConfigItem<int> WhistlePriceConfig = null!;
        public static ConfigItem<int> ShipOnionPriceConfig = null!;
        public static ConfigItem<int> PluckaPhonePriceConfig = null!;
        public static ConfigItem<int> ShipWhistlePriceConfig = null!;
        public static ConfigItem<bool> IsWhistleConductive = null!;
        #endregion

        #region Glow Pikmin
        public static ConfigItem<float> LumiknullActivateTime = null!;
        public static ConfigItem<float> LumiknullActivateDistance = null!;
        public static ConfigItem<float> LumiknullSpawnChance = null!;
        public static ConfigItem<int> SpawnLumiknullAfterDays = null!;
        public static ConfigItem<float> GlowOddsToTurnIntoSeed = null!;
        public static ConfigItem<bool> GlowsUseableAtCompany = null!;
        #endregion

        #region LethalMon
        public static ConfigItem<bool> AttackOwnTamedEnemies = null!;
        public static ConfigItem<bool> AttackOthersTamedEnemies = null!;
        #endregion

        #region LC-Office
        public static ConfigItem<bool> AddNavLinkToThridFloorOffice = null!;
        public static ConfigItem<bool> BlockEnemiesFromEnteringThirdFloorOffice = null!;
        public static ConfigItem<bool> AllowMetalDetectorToZap = null!;
        #endregion

        #region Zeranos
        public static ConfigItem<bool> AddNavLinkToZeranosShip = null!;
        public static ConfigItem<bool> BlockEnemiesFromEnteringZeranosShip = null!;
        public static ConfigItem<bool> AddNavLinkToZeranosElevator = null!;
        #endregion

        #region LCVR
        public static ConfigItem<bool> AutoSetHudVRPreset = null!;
        public static ConfigItem<bool> DontUseInputUtilsForVR = null!;
        public static ConfigItem<bool> DisableSproutInteraction = null!;
        public static ConfigItem<bool> DisableWhistleFix = null!;
        public static ConfigItem<float> OnionHUDZDistance = null!;
        public static ConfigItem<string> ThrowVRAction = null!;
        public static ConfigItem<string> ThrowCancelVRAction = null!;
        public static ConfigItem<string> SecondaryThrowVRAction = null!;
        public static ConfigItem<string> SwitchForwardVRAction = null!;
        public static ConfigItem<string> SwitchBackwawrdsVRAction = null!;
        public static ConfigItem<string> WhisleVRAction = null!;
        public static ConfigItem<bool> UseMouthTriggerForWhistle = null!;
        public static ConfigItem<string> DismissVRAction = null!;
        public static ConfigItem<string> SwitchWhistleSoundVRAction = null!;
        public static ConfigItem<string> ChargeVRAction = null!;
        public static ConfigItem<string> SecondaryChargeVRAction = null!;
        public static ConfigItem<bool> DisableChargeMotionBlur = null!;
        public static ConfigItem<string> GlowmobVRAction = null!;
        public static ConfigItem<bool> GlowmobDelay = null!;
        #endregion

        #region Imperium
        public static ConfigItem<bool> DontDoAudibleNoiseCalcuationsForPikmin = null!;
        public static ConfigItem<bool> DontResimulateOracleOnPikminDeath = null!;
        public static ConfigItem<bool> RemovePuffminFromSpawnSearch = null!;
        #endregion

        #region WiderShipMod
        public static ConfigItem<bool> MakeCustomBoundsForWideShip = null!;
        #endregion

        #region Advanced
        public static ConfigItem<bool> CheckNavMesh = null!;
        public static ConfigItem<List<string>> NavmeshCheckBlacklistConfig = null!;
        public static ConfigEntry<bool> UseCommasInVectorConfigs = null!;
        public static ConfigItem<bool> GeneratePikminConfigs = null!;
        public static ConfigItem<bool> UseModDataLibConfig = null!;
        //public static ConfigEntry<bool> DisableConfigAuthority = null!;
        #endregion

        #region Cheats
        public static ConfigItem<bool> UseBetaItemWeightCalculation = null!;
        public static ConfigItem<bool> WhistleMakesNoiseAtNoiceZone = null!;
        public static ConfigItem<bool> DontMakeAudibleNoises = null!;
        public static ConfigItem<float> PikminSignalCooldown = null!;
        public static ConfigItem<bool> NoKnockBack = null!;
        public static ConfigItem<bool> InvinceablePikmin = null!;
        public static ConfigItem<bool> UselessBluesMode = null!;
        public static ConfigItem<float> MaxWhistleZoneRadius = null!;
        public static ConfigItem<float> MaxWhistleZoneDistance = null!;
        public static ConfigItem<float> PlayerNoticeZoneSize = null!;
        public static ConfigItem<float> PikminDamageMultipler = null!;
        public static ConfigItem<float> PikminSpeedMultipler = null!;
        public static ConfigItem<float> ChargeCooldown = null!;
        public static ConfigItem<float> ChargeDistance = null!;
        public static ConfigItem<bool> DontPurgeAfterFire = null!;
        #endregion

        #region Funi
        public static ConfigItem<bool> YeetAfterLatchOn = null!;
        public static ConfigItem<bool> RandomizeGenerationModels = null!;
        public static ConfigItem<bool> AddCollisionToGhostSprites = null!;
        public static ConfigItem<bool> FuniOnion = null!;
        public static ConfigItem<bool> GiantWhistleMode = null!;
        #endregion

        public void BindConfigs()
        {
            #region General Binding
            UseCommasInVectorConfigs = Config.Bind(
            "Advanced",
            "Use Commas In Vector Configs",
            false,
            "Whether or not to use commas in vector configs (true = x,y,z | false = x y z)");

            MaxPikmin = new ConfigItem<int>(
                "General",
                "Max Pikmin",
                100,
                "The maximun ammount of pikmin that can be spawned in at once before being despawned or sent to the onion",
                false,
                ConfigItemAuthority.Host);

            FriendlyFire = new ConfigItem<bool>(
                "General",
                "Friendly Fire",
                false,
                "Allows for leaders to kill pikmin in their squad",
                false,
                ConfigItemAuthority.Client);

            SkipPluckAnimation = new ConfigItem<bool>(
                "General",
                "Skip Pluck Animation",
                false,
                "Skip the player's pluck animation when plucking a pikmin",
                false,
                ConfigItemAuthority.Client);
            SkipPluckAnimation.OnValueChanged += (_) => UpdateSyncedConfigs();

            WhistleVolume = new ConfigItem<float>(
                "General",
                "Whistle Volume",
                1.0f,
                "The volume of the whistle sound effect (0.0 - 1.0)",
                false,
                ConfigItemAuthority.Local,
                "slider(0,1)");

            AllowOnionsToRevivePlayers = new ConfigItem<bool>(
                "General",
                "Allow Onions To Revive Players",
                true,
                "Allow onions to revive players when they are killed",
                false,
                ConfigItemAuthority.Host);

            AllowOnionToReviveMaskeds = new ConfigItem<bool>(
                "General",
                "Allow Onions To Revive Masked Players",
                true,
                "Allow onions to revive masked players when they are killed",
                false,
                ConfigItemAuthority.Host);

            AllowOnionFusing = new ConfigItem<bool>(
                "General",
                "Allow Onion Fusing",
                true,
                "Allow onions to fuse together after a round",
                false,
                ConfigItemAuthority.Host);

            ConvertEnemyBodiesToItems = new ConfigItem<bool>(
                "General",
                "Convert Enemy Bodies To Items",
                true,
                "Convert enemy bodies to items for pikmin to take to the onion",
                false,
                ConfigItemAuthority.Client);

            ShowOnionsInSpace = new ConfigItem<bool>(
                "General",
                "Show Onions In Space",
                true,
                "Show onions next to the ship in space when in space (requires ship windows mod)",
                false,
                ConfigItemAuthority.Local);

            SpaceOnionPosition = new ConfigItem<Vector3>(
                "General",
                "Space Onion Position",
                new Vector3(-5f, 0, 20f),
                "The position of the onion in space",
                false,
                ConfigItemAuthority.Local);

            DontUpdateSpaceOnionPosition = new ConfigItem<bool>(
                "General",
                "Dont Update Space Onion Position",
                false,
                "Used for debugging, dont lock the space onion position's to the config position",
                false,
                ConfigItemAuthority.Local);

            MakeItemsScanable = new ConfigItem<bool>(
                "General",
                "Make Items Scanable",
                true,
                "Make pikmin items scanable",
                false,
                ConfigItemAuthority.Local);

            MakePikminScanable = new ConfigItem<bool>(
                "General",
                "Make Pikmin Scanable",
                true,
                "Make pikmin scanable",
                false,
                ConfigItemAuthority.Local);

            TriggerLandmines = new ConfigItem<bool>(
                "General",
                "Trigger Landmines",
                true,
                "Make pikmin trigger landmines",
                false,
                ConfigItemAuthority.Local);

            GlobalGhostSFX = new ConfigItem<bool>(
                "General",
                "Global Ghost SFX",
                true,
                "When true, you will be able to hear Pikmin Ghosts no matter where you are on the moon.",
                false,
                ConfigItemAuthority.Local);


            HideResultsWhenMoreThanFour = new ConfigItem<bool>(
                "General",
                "Hide Custom Reports With More Players",
                true,
                "Hide the Pikmin raised counters for every player when there are more than four players in the game. To prevent names from being covered.",
                false,
                ConfigItemAuthority.Local);

            DieInPlayerDeathZone = new ConfigItem<bool>(
                "General",
                "Pikmin Die In Player Death Zones",
                false,
                "Makes it so pikmin dies when enter death zones that kill players. For example the catwalk jump pit.",
                false,
                ConfigItemAuthority.Client);

            TakeItemsFromPikmin = new ConfigItem<bool>(
                "General",
                "Take Items From Pikmin",
                false,
                "Allows players to take items from pikmin",
                false,
                ConfigItemAuthority.Client);
            #endregion

            #region Appearance Binding
            PikminScale = new ConfigItem<float>(
                "Appearances",
                "Pikmin Scale",
                1,
                "The scale of the pikmin",
                false,
                ConfigItemAuthority.Local
            );

            SproutScale = new ConfigItem<float>(
                "Appearances",
                "Sprout Scale",
                1,
                "The scale of the sprout",
                false,
                ConfigItemAuthority.Local
            );

            CurWhistPack = new ConfigItem<int>(
                "Appearances",
                "Whistle Sound Index",
                0,
                "press ] while holding a whistle to cycle through the available sounds (0-3)",
                false,
                ConfigItemAuthority.Client);

            BigEyesEnabled = new ConfigItem<bool>(
                "Appearances",
                "Big Eyes Enabled",
                false,
                "Enables the Big Eyes from the HeyPikmin generation.",
                false,
                ConfigItemAuthority.Local);
            BigEyesEnabled.OnValueChanged += (_) => UpdateCurrentGeneration(1, ref PikminModelGeneration);
            BigEyesEnabled.OnValueChanged += (_) => UpdateCurrentGeneration(7, ref PuffminGeneration);

            PikminScanNodeColorType = new ConfigItem<PikminScanNodeProperties.ScanNodeType>(
                "Appearances",
                "Pikmin Scan Node Color Type",
                PikminScanNodeProperties.ScanNodeType.Enemy,
                "The type of color to use for the scan node (Point of Intrest: Blue, Enemy: Red, Item: Green)",
                false,
                ConfigItemAuthority.Local);
            #endregion

            #region Generation Binding
            DefaultGeneration = new ConfigItem<PikminGeneration>(
                   "Generations",
                   "Default Generation",
                   PikminGeneration.Pikmin4,
                   "The default generation to use when no specific generation is set",
                   false,
                   ConfigItemAuthority.Local);
            DefaultGeneration.OnValueChanged += (_) => UpdateDefaultGeneration();

            PikminModelGeneration = new ConfigItem<CfgPikminGeneration>(
                "Generations",
                "Pikmin Model Generation",
                CfgPikminGeneration.Default,
                "The generation to use for Pikmin models",
                false,
                ConfigItemAuthority.Local);
            PikminModelGeneration.OnValueChanged += (_) => UpdateCurrentGeneration(1, ref PikminModelGeneration);

            SproutModelGeneration = new ConfigItem<CfgPikminGeneration>(
                "Generations",
                "Sprout Model Generation",
                CfgPikminGeneration.Default,
                "The generation to use for Sprout models",
                false,
                ConfigItemAuthority.Local);
            SproutModelGeneration.OnValueChanged += (_) => UpdateCurrentGeneration(2, ref SproutModelGeneration);

            PikminSoundGeneration = new ConfigItem<CfgPikminGeneration>(
                "Generations",
                "Pikmin Sound Generation",
                CfgPikminGeneration.Default,
                "The generation to use for Pikmin sounds",
                false,
                ConfigItemAuthority.Local);
            PikminSoundGeneration.OnValueChanged += (_) => UpdateCurrentGeneration(3, ref PikminSoundGeneration);

            SoulSpriteGeneration = new ConfigItem<CfgPikminGeneration>(
                 "Generations",
                 "Soul Sprite Generation",
                 CfgPikminGeneration.Default,
                 "The generation to use for Pikmin Souls",
                 false,
                 ConfigItemAuthority.Local);
            SoulSpriteGeneration.OnValueChanged += (_) => UpdateCurrentGeneration(4, ref SoulSpriteGeneration);

            OnionModelGeneration = new ConfigItem<CfgPikminGeneration>(
                "Generations",
                "Onion Model Generation",
                CfgPikminGeneration.Default,
                "The generation to use for Onion models",
                false,
                ConfigItemAuthority.Local);
            OnionModelGeneration.OnValueChanged += (_) => UpdateCurrentGeneration(5, ref OnionModelGeneration);

            OnionItemModelGeneration = new ConfigItem<CfgPikminGeneration>(
                "Generations",
                "Onion Item Model Generation",
                CfgPikminGeneration.Default,
                "The generation to use for Onion Item models",
                false,
                ConfigItemAuthority.Local);
            OnionItemModelGeneration.OnValueChanged += (_) => UpdateCurrentGeneration(6, ref OnionItemModelGeneration);

            PuffminGeneration = new ConfigItem<CfgPikminGeneration>(
                "Generations",
                "Puffmin Generation",
                CfgPikminGeneration.Default,
                "The generation to use for Puffmin Models",
                false,
                ConfigItemAuthority.Local);
            PuffminGeneration.OnValueChanged += (_) => UpdateCurrentGeneration(7, ref PuffminGeneration);
            #endregion

            #region Controls Binding
            ThrowAction = new ConfigItem<string>(
                "Controls",
                "Throw Button",
                "<Keyboard>/r",
                "",
                true,
                ConfigItemAuthority.Local);

            ThrowCancelAction = new ConfigItem<string>(
                "Controls",
                "Throw Cancel Button",
                "<Keyboard>/q",
                "",
                true,
                ConfigItemAuthority.Local);

            SwitchForwardAction = new ConfigItem<string>(
                "Controls",
                "Switch Forward Button",
                "<Keyboard>/3",
                "",
                true,
                ConfigItemAuthority.Local);

            SwitchBackwawrdsAction = new ConfigItem<string>(
                "Controls",
                "Switch Backwards Button (hold)",
                "<Keyboard>/2",
                "",
                true,
                ConfigItemAuthority.Local);

            WhisleAction = new ConfigItem<string>(
                "Controls",
                "Whistle Button",
                "<Mouse>/leftButton",
                "",
                true,
                ConfigItemAuthority.Local);

            DismissAction = new ConfigItem<string>(
                "Controls",
                "Dismiss Button",
                "<Mouse>/middleButton",
                "",
                true,
                ConfigItemAuthority.Local);

            SwitchWhistleSoundAction = new ConfigItem<string>(
                "Controls",
                "Switch Whistle Sound Button",
                "<Keyboard>/]",
                "",
                true,
                ConfigItemAuthority.Local);

            ChargeAction = new ConfigItem<string>(
                "Controls",
                "Charge Button",
                "<Keyboard>/4",
                "",
                true,
                ConfigItemAuthority.Local);

            GlowmobAction = new ConfigItem<string>(
                "Controls",
                "Glowmob Button (hold)",
                "<Keyboard>/4",
                "",
                true,
                ConfigItemAuthority.Local);

            OnionHudSpeedAction = new ConfigItem<string>(
                "Controls",
                "Onion Menu 10-withdraw Button",
                "<Keyboard>/shift",
                "",
                true,
                ConfigItemAuthority.Local);

            #endregion

            #region Spawning Binding
            IndoorSpawnChance = new ConfigItem<float>(
                "Spawning",
                "Indoor Pikmin Spawn Chance",
                0.05f,
                "The odds for pikmin that spawn inside to spawn",
                false,
                ConfigItemAuthority.Host,
                "slider(0,1)");

            OutdoorSpawnChance = new ConfigItem<float>(
                "Spawning",
                "Outdoor Pikmin Spawn Chance",
                0.35f,
                "The odds for pikmin that spawn outside to spawn",
                false,
                ConfigItemAuthority.Host,
                "slider(0,1)");

            OnionSpawnChance = new ConfigItem<float>(
                "Spawning",
                "Onion Spawn Chance",
                0.45f,
                "The odds for an onion to spawn",
                false,
                ConfigItemAuthority.Host,
                "slider(0,1)");

            UseLCSpawnSystem = new ConfigItem<bool>(
                "Spawning",
                "Use LC Spawn System",
                true,
                "Have Pikmin be spawnable using LC's spawning system, the will result on rare occasition for pikmin to randomly spawn on the map",
                false,
                ConfigItemAuthority.Host);

            UsePowerLVForSprouts = new ConfigItem<bool>(
                "Spawning",
                "Use Power Level For Sprouts",
                false,
                "Use the Power Level of a moon to determine sprout count (overrides SpawnChance configs for sprout spawning pikmin)",
                false,
                ConfigItemAuthority.Host,
                "slider(0,1)");
            #endregion

            #region Pikmin Behavior Binding 
            AttackBlacklistConfig = new ConfigItem<List<string>>(
                "Pikmin Behavior",
                "Attack Blacklist",
                new List<string> { { "Docile Locust Bees" }, { "Manticoil" }, { "Red Locust Bees" }, { "Blob" },
                {"Nemo"}, {"InternNPC"}, {"BellCrab"}, {"Nancy"}, {"Transporter"}, {"Janior"}, {"Peace Keeper"},
                {"Guardsman"}, {"Tornado"}, {"FireStorm"}, {"Hurricane"}, {"Cabinet"}},
                "The list of enemy names that pikmin can't attack",
                true,
                ConfigItemAuthority.Client);

            ItemBlacklistConfig = new ConfigItem<List<string>>(
                "Pikmin Behavior",
                "Item Blacklist",
                new List<string>(),
                "The list of item names that pikmin can't pick up",
                true,
                ConfigItemAuthority.Client);

            EnemyBodyConvertBlacklistConfig = new ConfigItem<List<string>>(
                "Pikmin Behavior",
                "Enemy Body Convert Blacklist",
                new List<string> { { "Flowerman" }, { "GiantKiwi" } },
                "The list of enemy names that the mod won't be converted into items",
                true,
                ConfigItemAuthority.Host);

            UseExitsWhenCarryingItems = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Use Exits When Carrying Items",
                true,
                "Whether or not Pikmin can path outside directly to the ship when inside, or just leave it at an exit when inside",
                false,
                ConfigItemAuthority.Client);

            PikminFollowMode = new ConfigItem<PfollowMode>(
                "Pikmin Behavior",
                "Follow Mode",
                PfollowMode.LegacyBehind,
                "The way pikmin follow their leader." +
                @"
                 New: Makes the pikmin follow the leader in a formation simular to the pikmin games
                 LegacyBehind Makes the pikmin looesly follow the leader
                 LegacyFollow Makes the pikmin path directly to the leader (can be buggy)",
                false,
                ConfigItemAuthority.Client);
            PikminFollowMode.OnValueChanged += (_) => SetFollowMode();

            TimeFallingFailSafe = new ConfigItem<float>(
                "Pikmin Behavior",
                "Time Falling Fail Safe",
                10f,
                "The time it takes for a pikmin to be concitered falling infintly and teleport back into bounds",
                false,
                ConfigItemAuthority.Client);

            CarryNonScrapItemsToCompany = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Carry Non-Scrap Items To Company",
                false,
                "Makes it so pikmin carry Non-Scrap items (Flashlights, Keys, etc.) to the company counter instead of the ship",
                false,
                ConfigItemAuthority.Client);
            
            IgnoreNonScrapItemsToCompany = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Ignore Non-Scrap Items On Company",
                false,
                "Makes it so pikmin ignore Non-Scrap items (Flashlights, Keys, etc.) on the company entirely",
                false,
                ConfigItemAuthority.Client);

            TakeItemsToOnionOnCompany = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Take Items To Onion On Company",
                true,
                "Whether or not items can be taken to the onion on company",
                false,
                ConfigItemAuthority.Client);

            TakeItemsToTheCar = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Take Items To The Car",
                true,
                "Whether or not Pikmin can take items to the car",
                false,
                ConfigItemAuthority.Client);

            TakeItemsToTheOnion = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Take Items To The Onion",
                true,
                "Whether or not Pikmin can take items to the onion",
                true,
                ConfigItemAuthority.Client);

            DisperseWhenDismissed = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Disperse When Dismissed",
                true,
                "Whether or not Pikmin disperse into groups when dismissed",
                false,
                ConfigItemAuthority.Client);

            AllowOnBBtobeGrabed = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Allow On BoomBox to be Grabbed",
                false,
                "Allows Pikmin to grab the boombox when it is on",
                false,
                ConfigItemAuthority.Client);

            DismissWindownTime = new ConfigItem<float>(
                "Pikmin Behavior",
                "Dismiss Windown Time",
                2f,
                "The ammount of seconds to wait before pikmin can be assigned a leader again after being dismissed",
                false,
                ConfigItemAuthority.Client);

            WildPikminCarry = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Wild Pikmin Can Carry Items",
                false,
                "Whether or not wild pikmin can carry items",
                false,
                ConfigItemAuthority.Host);

            WildPikminAttack = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Wild Pikmin Can Attack Enemies",
                true,
                "Whether or not wild pikmin can attack enemies",
                false,
                ConfigItemAuthority.Host);

            WildPikminAttackDamage = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Wild Pikmin Attack Does Damage",
                false,
                "Whether or not wild pikmin's attacks do damage",
                false,
                ConfigItemAuthority.Host);

            WildPikminNoDeath = new ConfigItem<bool>(
                "Pikmin Behavior",
                "Wild Pikmin Dont Die",
                true,
                "Whether or not wild pikmin can die",
                false,
                ConfigItemAuthority.Host);
            #endregion

            #region Enemy Behavior Binding
            UseConfigsForEnemies = new ConfigItem<bool>(
                "Enemy Behavior",
                "Use Enemy Configs",
                false,
                "Enables the use of enemy configs",
                false,
                ConfigItemAuthority.Client);

            // ClaySurgeon configs
            ClaySurgeon_SnipCooldown = new ConfigItem<float>(
                "Enemy Behavior",
                "Barber Snip Cooldown",
                1.0f,
                "How long the Barber waits between snipping Pikmin (in seconds)",
                false,
                ConfigItemAuthority.Client);

            ClaySurgeon_SnipLimit = new ConfigItem<int>(
                "Enemy Behavior",
                "Barber Snip Limit",
                3,
                "Maximum number of Pikmin that can be snipped by a Barber at once",
                false,
                ConfigItemAuthority.Client);

            // Crawler configs
            Crawler_BiteCooldown = new ConfigItem<float>(
                "Enemy Behavior",
                "Thumper Bite Cooldown",
                2.0f,
                "How long the Thumper waits between biting Pikmin (in seconds)",
                false,
                ConfigItemAuthority.Client);

            Crawler_BiteLimit = new ConfigItem<int>(
                "Enemy Behavior",
                "Thumper Bite Limit",
                3,
                "Maximum number of Pikmin that can be bitten by a Thumper at once",
                false,
                ConfigItemAuthority.Client);

            // SandSpider configs
            SandSpider_BiteCooldown = new ConfigItem<float>(
                "Enemy Behavior",
                "Bunker Spider Bite Cooldown",
                3.5f,
                "How long the Bunker Spider waits between biting Pikmin (in seconds)",
                false,
                ConfigItemAuthority.Client);

            SandSpider_BiteLimit = new ConfigItem<int>(
                "Enemy Behavior",
                "Bunker Spider Bite Limit",
                2,
                "Maximum number of Pikmin that can be bitten by a Bunker Spider at once",
                false,
                ConfigItemAuthority.Client);

            // HoarderBug configs
            HoarderBug_BiteCooldown = new ConfigItem<float>(
                "Enemy Behavior",
                "Hoarding Bug Bite Cooldown",
                3.0f,
                "How long the Hoarding Bug waits between biting Pikmin (in seconds)",
                false,
                ConfigItemAuthority.Client);

            HoarderBug_BiteLimit = new ConfigItem<int>(
                "Enemy Behavior",
                "Hoarding Bug Bite Limit",
                1,
                "Maximum number of Pikmin that can be bitten by a Hoarding Bug at once",
                false,
                ConfigItemAuthority.Client);

            HoarderBug_AggroWhenPikminTakesItem = new ConfigItem<bool>(
                "Enemy Behavior",
                "Hoarding Bug Aggro When Item Taken",
                true,
                "Whether Hoarding Bugs becomes aggressive when Pikmin take their items",
                false,
                ConfigItemAuthority.Client);

            // MouthDog configs
            MouthDog_BiteCooldown = new ConfigItem<float>(
                "Enemy Behavior",
                "Eyeless Dog Bite Cooldown",
                5.5f,
                "How long the Eyeless Dog waits between biting Pikmin (in seconds)",
                false,
                ConfigItemAuthority.Client);

            MouthDog_BiteLimit = new ConfigItem<int>(
                "Enemy Behavior",
                "Eyeless Dog Bite Limit",
                7,
                "Maximum number of Pikmin that can be bitten by a Eyeless Dog at once",
                false,
                ConfigItemAuthority.Client);

            // Blob configs
            Blob_TrapPikmin = new ConfigItem<bool>(
                "Enemy Behavior",
                "Hydrogere Traps Pikmin",
                true,
                "Whether the Hydrogere can trap Pikmin inside it",
                false,
                ConfigItemAuthority.Client);

            Blob_KillPikmin = new ConfigItem<bool>(
                "Enemy Behavior",
                "Hydrogere Kills Pikmin",
                false,
                "Whether the Hydrogere can kill trapped Pikmin",
                false,
                ConfigItemAuthority.Client);

            // RedLocustBees configs
            RedLocustBees_ZapCooldown = new ConfigItem<float>(
                "Enemy Behavior",
                "Circuit Bees Zap Cooldown",
                0.25f,
                "How long the Circuit Bees wait between zapping Pikmin (in seconds)",
                false,
                ConfigItemAuthority.Client);

            RedLocustBees_ZapPikmin = new ConfigItem<bool>(
                "Enemy Behavior",
                "Circuit Bees Zap Pikmin",
                true,
                "Whether Circuit Bees can zap Pikmin",
                false,
                ConfigItemAuthority.Client);

            RedLocustBees_KillPikmin = new ConfigItem<bool>(
                "Enemy Behavior",
                "Circuit Bees Kill Pikmin",
                false,
                "Whether Circuit Bees can kill Pikmin",
                false,
                ConfigItemAuthority.Client);

            // CaveDweller configs
            CaveDweller_BiteCooldown = new ConfigItem<float>(
                "Enemy Behavior",
                "Maneater Bite Cooldown",
                1.0f,
                "How long the Maneater waits between biting Pikmin (in seconds)",
                false,
                ConfigItemAuthority.Client);

            CaveDweller_BiteLimit = new ConfigItem<int>(
                "Enemy Behavior",
                "Maneater Bite Limit",
                7,
                "Maximum number of Pikmin that can be bitten by a Maneater at once",
                false,
                ConfigItemAuthority.Client);

            CaveDweller_AllowCarry = new ConfigItem<bool>(
                "Enemy Behavior",
                "Maneater Allow Carry",
                true,
                "Whether the Maneater can be carried by Pikmin",
                false,
                ConfigItemAuthority.Client);

            CaveDweller_AttackAsBaby = new ConfigItem<bool>(
                "Enemy Behavior",
                "Maneater Attack As Baby",
                false,
                "Whether the Pikmin can attack the Maneater as a baby",
                false,
                ConfigItemAuthority.Client);

            // RadMech configs
            RadMech_GrabLimmit = new ConfigItem<int>(
                "Enemy Behavior",
                "Old Bird Grab Limit",
                10,
                "Maximum number of Pikmin that can be grabbed by a Old Bird at once",
                false,
                ConfigItemAuthority.Client);

            RadMech_GrabsPikmin = new ConfigItem<bool>(
                "Enemy Behavior",
                "Old Bird Grabs Pikmin",
                true,
                "Whether the Old Bird can grab and torch Pikmin",
                false,
                ConfigItemAuthority.Client);

            // ForestGiant configs
            ForestGiant_GrabLimit = new ConfigItem<int>(
                "Enemy Behavior",
                "Forest Keeper Grab Limit",
                25,
                "Maximum number of Pikmin that can be grabbed by a Forest Keeper at once",
                false,
                ConfigItemAuthority.Client);

            ForestGiant_GrabsPikmin = new ConfigItem<bool>(
                "Enemy Behavior",
                "Forest Keeper Grabs Pikmin",
                true,
                "Whether the Forest Keeper can grab and eat Pikmin",
                false,
                ConfigItemAuthority.Client);

            // Jester configs
            Jester_BiteCooldown = new ConfigItem<float>(
                "Enemy Behavior",
                "Jester Bite Cooldown",
                5.0f,
                "How long the Jester waits between biting Pikmin (in seconds)",
                false,
                ConfigItemAuthority.Client);

            Jester_BiteLimit = new ConfigItem<int>(
                "Enemy Behavior",
                "Jester Bite Limit",
                10,
                "Maximum number of Pikmin that can be bitten by a Jester at once",
                false,
                ConfigItemAuthority.Client);

            // ButlerEnemy configs
            ButlerEnemy_StabCooldown = new ConfigItem<float>(
                "Enemy Behavior",
                "Butler Stab Cooldown",
                1.0f,
                "How long the Butler waits between stabbing Pikmin (in seconds)",
                false,
                ConfigItemAuthority.Client);

            ButlerEnemy_StabLimit = new ConfigItem<int>(
                "Enemy Behavior",
                "Butler Stab Limit",
                1,
                "Maximum number of Pikmin that can be stabbed by a Butler at once",
                false,
                ConfigItemAuthority.Client);

            // BaboonBird configs
            BaboonBird_BiteCooldown = new ConfigItem<float>(
                "Enemy Behavior",
                "Baboon Hawk Bite Cooldown",
                1.5f,
                "How long the Baboon Hawk waits between biting Pikmin (in seconds)",
                false,
                ConfigItemAuthority.Client);

            // MaskedPlayerEnemy configs
            MaskedPlayerEnemy_ConvertPikmin = new ConfigItem<bool>(
                "Enemy Behavior",
                "Masked Player Enemy Convert Pikmin",
                true,
                "Whether the Masked Player Enemy can convert Pikmin into Puffmin",
                false,
                ConfigItemAuthority.Client);

            // Puffer Configs
            Puffer_CanPoisonPikmin = new ConfigItem<bool>(
                "Enemy Behavior",
                "Puffer Can Poison Pikmin",
                true,
                "Whether the Puffer's smoke can poison Pikmin",
                false,
                ConfigItemAuthority.Client);

            // Company configs
            Company_GrabsPikmin = new ConfigItem<bool>(
                "Enemy Behavior",
                "Company Grabs Pikmin",
                true,
                "Whether the Company can grab and eat Pikmin",
                false,
                ConfigItemAuthority.Client);

            Company_HearsPikmin = new ConfigItem<bool>(
                "Enemy Behavior",
                "Company Hears Pikmin",
                false,
                "Makes it so any sound the pikmin make will agro the company",
                false,
                ConfigItemAuthority.Client);
            #endregion

            #region Puffmin Behavior Binding
            TurnToNormalOnDeath = new ConfigItem<bool>(
                "Puffmin Behavior",
                "Turn To Normal On Death",
                false,
                "Whether or not Puffmin turn back into normal pikmin when they die",
                false,
                ConfigItemAuthority.Host);
            #endregion

            #region HUD Bindings
            HUDPreset = new ConfigItem<PikminHUDManager.HUDLayoutPresets>(
                    "HUD",
                    "HUD Layout Preset",
                    PikminHUDManager.HUDLayoutPresets.Default,
                    "The layout preset for the Pikmin HUD elements",
                    false,
                    ConfigItemAuthority.Local);
            HUDPreset.OnValueChanged += (_) => UpdateHUDLayout();

            PikminSelectionPosition = new ConfigItem<Vector3>(
                    "HUD",
                    "Pikmin Selection Position",
                    new Vector3(0, -120, 28),
                    "The position of the Pikmin selection UI element",
                    false,
                    ConfigItemAuthority.Local);

            PikminSelectionRotation = new ConfigItem<Vector3>(
                "HUD",
                "Pikmin Selection Rotation",
                new Vector3(0, 0, 0),
                "The rotation of the Pikmin selection UI element",
                false,
                ConfigItemAuthority.Local);

            PikminSelectionScale = new ConfigItem<float>(
                "HUD",
                "Pikmin Selection Scale",
                1.0f,
                "The scale of the Pikmin selection UI element",
                false,
                ConfigItemAuthority.Local);

            PikminSelectionAlpha = new ConfigItem<float>(
                "HUD",
                "Pikmin Selection Alpha",
                1.0f,
                "The alpha or transparency of the Pikmin selection UI element",
                false,
                ConfigItemAuthority.Local);

            EnableCurSlot = new ConfigItem<bool>(
                "HUD",
                "Enable Current Slot",
                true,
                "Whether to show the current Pikmin type slot in the HUD",
                false,
                ConfigItemAuthority.Local);

            EnableNextSlot = new ConfigItem<bool>(
                "HUD",
                "Enable Next Slot",
                true,
                "Whether to show the next Pikmin type slot in the HUD",
                false,
                ConfigItemAuthority.Local);

            EnablePreviousSlot = new ConfigItem<bool>(
                "HUD",
                "Enable Previous Slot",
                true,
                "Whether to show the previous Pikmin type slot in the HUD",
                false,
                ConfigItemAuthority.Local);

            PikminCounterPosition = new ConfigItem<Vector3>(
                "HUD",
                "Pikmin Counter Position",
                new Vector3(8, -225, 0),
                "The position of the Pikmin counter UI element",
                false,
                ConfigItemAuthority.Local);

            PikminCounterRotation = new ConfigItem<Vector3>(
                "HUD",
                "Pikmin Counter Rotation",
                new Vector3(0, 0, 0),
                "The rotation of the Pikmin counter UI element",
                false,
                ConfigItemAuthority.Local);

            PikminCounterScale = new ConfigItem<float>(
                "HUD",
                "Pikmin Counter Scale",
                1.0f,
                "The scale of the Pikmin counter UI element",
                false,
                ConfigItemAuthority.Local);

            PikminCounterAlphaActive = new ConfigItem<float>(
                "HUD",
                "Pikmin Counter Alpha Active",
                1.0f,
                "The alpha or transparency of the Pikmin counter UI element when active",
                false,
                ConfigItemAuthority.Local);

            PikminCounterAlphaIdle = new ConfigItem<float>(
                "HUD",
                "Pikmin Counter Alpha Idle",
                0.15f,
                "The alpha or transparency of the Pikmin counter UI element when idle",
                false,
                ConfigItemAuthority.Local);

            EnableInExistanceCounter = new ConfigItem<bool>(
                "HUD",
                "Enable Pikmin In Existence Counter",
                true,
                "Whether to show the total number of Pikmin in existence",
                false,
                ConfigItemAuthority.Local);

            EnableInSquadCounter = new ConfigItem<bool>(
                "HUD",
                "Enable Pikmin In Squad Counter",
                true,
                "Whether to show the number of Pikmin in your current squad",
                false,
                ConfigItemAuthority.Local);

            EnableInFieldCounter = new ConfigItem<bool>(
                "HUD",
                "Enable Pikmin In Field Counter",
                true,
                "Whether to show the number of Pikmin in the field",
                false,
                ConfigItemAuthority.Local);

            HideSelectedWhenScanNotifcation = new ConfigItem<bool>(
                "HUD",
                "Hide Selected When Scan Notification",
                true,
                "Whether to hide the selected Pikmin when a scan notification is shown",
                false,
                ConfigItemAuthority.Local);

            DontUpdateHudConfigs = new ConfigItem<bool>(
                "HUD",
                "Dont Update Hud Configs",
                false,
                "For Debugging purposes, dont update the hud configs",
                false,
                ConfigItemAuthority.Local);

            GrayoutButtonsInOnionHUD = new ConfigItem<bool>(
                "HUD",
                "Grayout Buttons In Onion HUD",
                true,
                "Whether to gray out the buttons in the Onion HUD when there are no pikmin of that type available",
                false,
                ConfigItemAuthority.Local);
            #endregion

            #region Items Binding
            WhistlePriceConfig = new ConfigItem<int>(
                "Items",
                "Whistle Price",
                15,
                "",
                true,
                ConfigItemAuthority.Client);

            ShipOnionPriceConfig = new ConfigItem<int>(
                "Items",
                "Ship Onion Price",
                251,
                "",
                true,
                ConfigItemAuthority.Client);

            PluckaPhonePriceConfig = new ConfigItem<int>(
                "Items",
                "PluckaPhone Price",
                150,
                "",
                true,
                ConfigItemAuthority.Client);

            ShipWhistlePriceConfig = new ConfigItem<int>(
                "Items",
                "Ship Whistle Price",
                100,
                "",
                true,
                ConfigItemAuthority.Client);

            IsWhistleConductive = new ConfigItem<bool>(
                "Items",
                "Is Whistle Conductive",
                false,
                "Makes it so the whistle is conductive to electricity, making it dangerous on stormy moons.",
                true,
                ConfigItemAuthority.Client
            );
            #endregion

            #region Glow Pikmin Binding
            LumiknullActivateTime = new ConfigItem<float>(
                "Glow Pikmin",
                "Activate Time",
                660.0f,
                "The time in seconds it takes for a Lumiknull to activate when a player is nearby",
                false,
                ConfigItemAuthority.Host);

            LumiknullActivateDistance = new ConfigItem<float>(
                "Glow Pikmin",
                "Activate Distance",
                20f,
                "The distance in meters at which a Lumiknull will detect and activate when a player approaches",
                false,
                ConfigItemAuthority.Host);

            LumiknullSpawnChance = new ConfigItem<float>(
                "Glow Pikmin",
                "Spawn Chance",
                0.45f,
                "The chance of a Lumiknull spawning on applicable moons",
                false,
                ConfigItemAuthority.Host,
                "slider(0,1)");

            SpawnLumiknullAfterDays = new ConfigItem<int>(
                "Glow Pikmin",
                "Spawn After Days",
                6,
                "The number of days that must pass before Lumiknulls can start spawning (-1 = can spawn from day 1)",
                false,
                ConfigItemAuthority.Host);

            GlowOddsToTurnIntoSeed = new ConfigItem<float>(
                "Glow Pikmin",
                "Odds To Turn Into Seed",
                0.3f,
                "The odds of a glow pikmin turning into a glow seed when leaving",
                false,
                ConfigItemAuthority.Host,
                "slider(0,1)");

            GlowsUseableAtCompany = new ConfigItem<bool>(
                "Glow Pikmin",
                "Useable At Company",
                true,
                "Whether or not glow pikmin are useable at the company",
                false,
                ConfigItemAuthority.Host);
            #endregion

            #region LethalMon Binding
            AttackOwnTamedEnemies = new ConfigItem<bool>(
                "LethalMon",
                "Attack Own Tamed Enemies",
                false,
                "Whether or not pikmin will attack their leader's tammed enemies",
                false,
                ConfigItemAuthority.Client);

            AttackOthersTamedEnemies = new ConfigItem<bool>(
                "LethalMon",
                "Attack Others Tamed Enemies",
                false,
                "Whether or not pikmin will attack other player's tammed enemies",
                false,
                ConfigItemAuthority.Client);
            #endregion

            #region LC-Office Binding
            AddNavLinkToThridFloorOffice = new ConfigItem<bool>(
                "LC-Office",
                "Add NavLink To LC-Office Third Floor",
                true,
                "Due to a bug in LC-Office at the time of making LethalMin, Entites cannot enter the elevator on the third floor. This adds a navlink to the office so that entities can enter it.",
                true,
                ConfigItemAuthority.Client);

            BlockEnemiesFromEnteringThirdFloorOffice = new ConfigItem<bool>(
                "LC-Office",
                "Block Enemies From Entering Third Floor Office",
                true,
                "Blocks enemies (aside from pikmin) from entering the elevator on the third floor of LC-Office. Too keep the bugged functionailty of other entites not being able to enter the elevator.",
                false,
                ConfigItemAuthority.Client);

            AllowMetalDetectorToZap = new ConfigItem<bool>(
                "LC-Office",
                "Allow Metal Detector To Zap",
                true,
                "Allows the metal detector to zap pikmin.",
                false,
                ConfigItemAuthority.Client);
            #endregion

            #region Zeranos Binding
            AddNavLinkToZeranosShip = new ConfigItem<bool>(
                "Zeranos",
                "Add NavLink to the ship on Zeranos",
                true,
                "Due to a bug in Zeranos at the time of making LethalMin, Entites cannot enter the ship. This adds a navlink to the ship so that entities can enter it.",
                true,
                ConfigItemAuthority.Client);

            BlockEnemiesFromEnteringZeranosShip = new ConfigItem<bool>(
                "Zeranos",
                "Block Enemies From Entering Zeranos Ship",
                true,
                "Blocks enemies (aside from pikmin) from entering the ship on Zeranos. Too keep the bugged functionailty of other entites not being able to enter the ship.",
                false,
                ConfigItemAuthority.Client);

            AddNavLinkToZeranosElevator = new ConfigItem<bool>(
                "Zeranos",
                "Add NavLink to the Elevator on Zeranos",
                true,
                "Due to a bug in Zeranos at the time of making LethalMin, Entites cannot exit the elevator after it goes back up. This adds a navlink to the ship so that entities can enter it.",
                true,
                ConfigItemAuthority.Client);
            #endregion

            #region LCVR Binding
            AutoSetHudVRPreset = new ConfigItem<bool>(
                "LCVR",
                "Auto Set Hud Preset",
                true,
                "Whether or not to automatically set the HUD preset for LCVR",
                true,
                ConfigItemAuthority.Local);

            DontUseInputUtilsForVR = new ConfigItem<bool>(
                "LCVR",
                "Dont Use Input Utils For VR",
                true,
                "Whether or not to use the InputUtils for VR controls due to a bug at the time of making this mod, LCVR breaks input utils.",
                false,
                ConfigItemAuthority.Local);

            DisableSproutInteraction = new ConfigItem<bool>(
                "LCVR",
                "Disable Sprout Interaction",
                false,
                "Whether or not to disable the sprout interaction for LCVR",
                false,
                ConfigItemAuthority.Local);

            DisableWhistleFix = new ConfigItem<bool>(
                "LCVR",
                "Disable Whistle Fix",
                false,
                "Whether or not to disable the whistle fix for LCVR",
                false,
                ConfigItemAuthority.Client);

            OnionHUDZDistance = new ConfigItem<float>(
                "LCVR",
                "Onion HUD Z Distance",
                100f,
                "The Z distance of the onion HUD",
                false,
                ConfigItemAuthority.Local);

            ThrowVRAction = new ConfigItem<string>(
                "LCVR",
                "Throw Button",
                "<XRController>{RightHand}/triggerPressed",
                "",
                true,
                ConfigItemAuthority.Local);

            SecondaryThrowVRAction = new ConfigItem<string>(
                "LCVR",
                "Secondary Throw Button",
                "<XRController>{RightHand}/gripPressed",
                "",
                true,
                ConfigItemAuthority.Local);

            SwitchForwardVRAction = new ConfigItem<string>(
                "LCVR",
                "Switch Forward Button",
                "<XRController>{LeftHand}/secondaryButton",
                "",
                true,
                ConfigItemAuthority.Local);

            SwitchBackwawrdsVRAction = new ConfigItem<string>(
                "LCVR",
                "Switch Backwards Button",
                "",
                "",
                true,
                ConfigItemAuthority.Local);

            WhisleVRAction = new ConfigItem<string>(
                "LCVR",
                "Whistle Button",
                "<XRController>{RightHand}/triggerPressed",
                "",
                true,
                ConfigItemAuthority.Local);

            UseMouthTriggerForWhistle = new ConfigItem<bool>(
                "LCVR",
                "Use Mouth to Dismiss",
                true,
                "Makes it so you have to bring the whistle to your mouth and press the dismiss button to dismiss",
                true,
                ConfigItemAuthority.Local);

            DismissVRAction = new ConfigItem<string>(
                "LCVR",
                "Dismiss Button",
                "<XRController>{RightHand}/gripPressed",
                "",
                true,
                ConfigItemAuthority.Local);

            SwitchWhistleSoundVRAction = new ConfigItem<string>(
                "LCVR",
                "Switch Whistle Sound Button",
                "",
                "",
                true,
                ConfigItemAuthority.Local);

            ChargeVRAction = new ConfigItem<string>(
                "LCVR",
                "Charge Button",
                "<XRController>{RightHand}/gripPressed",
                "",
                true,
                ConfigItemAuthority.Local);

            SecondaryChargeVRAction = new ConfigItem<string>(
                "LCVR",
                "Secondary Charge Button",
                "<XRController>{LeftHand}/gripPressed",
                "",
                true,
                ConfigItemAuthority.Local);

            DisableChargeMotionBlur = new ConfigItem<bool>(
                "LCVR",
                "Disable Charge Motion Blur",
                true,
                "Whether or not to disable the motion blur when charging",
                false,
                ConfigItemAuthority.Local);

            GlowmobVRAction = new ConfigItem<string>(
                "LCVR",
                "Glowmob Button (hold)",
                "<XRController>{LeftHand}/gripPressed",
                "",
                true,
                ConfigItemAuthority.Local);

            GlowmobDelay = new ConfigItem<bool>(
                "LCVR",
                "Glowmob Delay",
                true,
                "Adds a small delay for holding the glowmob button to prevent accidental activation",
                false,
                ConfigItemAuthority.Local);

            ThrowCancelVRAction = new ConfigItem<string>(
                "LCVR",
                "Throw Cancel Button",
                "",
                "",
                true,
                ConfigItemAuthority.Local);
            #endregion

            #region Imperium Binding
            DontDoAudibleNoiseCalcuationsForPikmin = new ConfigItem<bool>(
                "Imperium",
                "Dont Do Audible Noise Calculations For Pikmin",
                true,
                "Whether or not to ignore audible noise calculations for Pikmin (This Reduces lag when there are tons of pikmin)",
                false,
                ConfigItemAuthority.Local);

            DontResimulateOracleOnPikminDeath = new ConfigItem<bool>(
                "Imperium",
                "Dont Resimulate Oracle On Pikmin Death",
                true,
                "Whether or not to ignore resimulating the oracle when a pikmin dies (This Reduces lag when lots of pikmin die)",
                false,
                ConfigItemAuthority.Local);

            RemovePuffminFromSpawnSearch = new ConfigItem<bool>(
                "Imperium",
                "Remove Puffmin From Spawn Search",
                true,
                "Whether or not to remove puffmin from the spawn search when pressing f2." +
                " This makes it so you wouldn't accidently spawn a puffmin when trying to spawn a pikmin.",
                true,
                ConfigItemAuthority.Local);
            #endregion

            #region WiderShipMod Binding
            MakeCustomBoundsForWideShip = new ConfigItem<bool>(
                "WiderShipMod",
                "Make Custom Bounds For Wide Ship",
                true,
                "Whether or not to make custom bounds for the wider ship mod (auto disabled if the 2story ship mod is installed with wider ship)",
                true,
                ConfigItemAuthority.Client);
            #endregion

            #region Advanced Binding
            NavmeshCheckBlacklistConfig = new ConfigItem<List<string>>(
                "Advanced",
                "Navmesh Check Blacklist",
                new List<string>(),
                "The list of moon names that the mod won't check for navmesh",
                true,
                ConfigItemAuthority.Host);

            CheckNavMesh = new ConfigItem<bool>(
                "Advanced",
                "Check Navmesh",
                true,
                "Check if the navmesh a moon has a navmesh if it does then it spawns pikmin and onions on the moon",
                false,
                ConfigItemAuthority.Host);

            GeneratePikminConfigs = new ConfigItem<bool>(
                "Advanced",
                "Generate Pikmin Configs",
                false,
                "Whether or not to generate configs for each Pikmin type",
                false,
                ConfigItemAuthority.Local);

            UseModDataLibConfig = new ConfigItem<bool>(
                "Advanced",
                "Use ModDataLib",
                true,
                "Whether or not to use ModDataLib for saving and loading data",
                false,
                ConfigItemAuthority.Host);

            // DisableConfigAuthority = Config.Bind(
            //     "Advanced",
            //     "Disable Config Authority",
            //     false,
            //     "Whether or not to disable config authority");
            #endregion

            #region Cheat Binding
            UseBetaItemWeightCalculation = new ConfigItem<bool>(
                "`Cheats`",
                "Use Beta Item Weight Calculation",
                false,
                "Use the beta item weight calculation for pikmin items (overrides the default weight calculation)",
                false,
                ConfigItemAuthority.Client);

            WhistleMakesNoiseAtNoiceZone = new ConfigItem<bool>(
                "`Cheats`",
                "Whistle Makes Noise At Noice Zone",
                false,
                "Whether or not the whistle makes noise at the noice zone",
                false,
                ConfigItemAuthority.Host);

            DontMakeAudibleNoises = new ConfigItem<bool>(
                "`Cheats`",
                "Dont Make Audible Noises",
                false,
                "Makes it so Pikmin's voices won't be audible to enemies",
                false,
                ConfigItemAuthority.Host);

            PikminSignalCooldown = new ConfigItem<float>(
                "`Cheats`",
                "Pikmin Signal Cooldown",
                -1f,
                "The cooldown for the pikmin signal (in seconds)",
                false,
                ConfigItemAuthority.Host);

            NoKnockBack = new ConfigItem<bool>(
                "`Cheats`",
                "No Knock Back",
                false,
                "Whether or not pikmin will be knocked back by enemies",
                false,
                ConfigItemAuthority.Host);

            InvinceablePikmin = new ConfigItem<bool>(
                "`Cheats`",
                "Invinceable Pikmin",
                false,
                "Whether or not pikmin are invincible to damage from enemies",
                false,
                ConfigItemAuthority.Host);

            UselessBluesMode = new ConfigItem<bool>(
                "`Cheats`",
                "Useless Blues Mode",
                false,
                "Makes it so blues pikmin cannot swim and drown in water",
                false,
                ConfigItemAuthority.Host);

            MaxWhistleZoneRadius = new ConfigItem<float>(
                "`Cheats`",
                "Max Whistle Zone Radius",
                -1f,
                "The maximum radius for the whistle zone (in meters)",
                false,
                ConfigItemAuthority.Host);

            MaxWhistleZoneDistance = new ConfigItem<float>(
                "`Cheats`",
                "Max Whistle Zone Distance",
                -1f,
                "The maximum distance for the whistle zone (in meters)",
                false,
                ConfigItemAuthority.Host);

            PlayerNoticeZoneSize = new ConfigItem<float>(
                "`Cheats`",
                "Player Notice Zone Size",
                -1f,
                "The size of the player notice zone (in meters)",
                false,
                ConfigItemAuthority.Host);

            PikminDamageMultipler = new ConfigItem<float>(
                "`Cheats`",
                "Pikmin Damage Multiplier",
                -1.0f,
                "The multiplier for pikmin damage to enemies (1.0 = normal, 2.0 = double damage, etc.)",
                false,
                ConfigItemAuthority.Host);

            PikminSpeedMultipler = new ConfigItem<float>(
                "`Cheats`",
                "Pikmin Speed Multiplier",
                -1.0f,
                "The multiplier for pikmin speed (1.0 = normal, 2.0 = double speed, etc.)",
                false,
                ConfigItemAuthority.Host);

            ChargeDistance = new ConfigItem<float>(
                "`Cheats`",
                "Charge Distance",
                -1.0f,
                "The distance pikmin can be charged (in units)",
                false,
                ConfigItemAuthority.Host);

            ChargeCooldown = new ConfigItem<float>(
                "`Cheats`",
                "Charge Cooldown",
                -1.0f,
                "The cooldown for charging pikmin (in seconds)",
                false,
                ConfigItemAuthority.Host);

            DontPurgeAfterFire = new ConfigItem<bool>(
                "`Cheats`",
                "Dont Remove Save After Fire",
                false,
                "Makes it so the save file is not cleared after being fired",
                false,
                ConfigItemAuthority.Host);
            #endregion

            #region Funi Binding
            YeetAfterLatchOn = new ConfigItem<bool>(
                "Funi",
                "Yeet After Latch On",
                false,
                "Whether or not pikmin will be yeeted after latching onto an enemy",
                false,
                ConfigItemAuthority.Client);

            RandomizeGenerationModels = new ConfigItem<bool>(
                "Funi",
                "Randomize Generation Models",
                false,
                "Randomizes what generation is used for pikmin/onion models",
                false,
                ConfigItemAuthority.Local);

            AddCollisionToGhostSprites = new ConfigItem<bool>(
                "Funi",
                "Add Collision To Ghost Sprites",
                false,
                "Whether or not to add collision to ghost sprites (This really does nothing except make some goofy physics happen in the cars)",
                false,
                ConfigItemAuthority.Client);

            FuniOnion = new ConfigItem<bool>(
                "Funi",
                "Funi Onion",
                false,
                "",
                false,
                ConfigItemAuthority.Local);

            GiantWhistleMode = new ConfigItem<bool>(
                "Funi",
                "Giant Whistle With Pikmin Signal",
                false,
                "",
                false,
                ConfigItemAuthority.Local);
            #endregion

            //if (!DisableConfigAuthority.Value)
            //ConfigItemAuthorityManager.SetUpAuthConfigs();
        }

        public static bool WhistleMakesNoiseAtNoticeZoneCheat => PikminManager.instance != null && PikminManager.instance.Cheat_WhistleMakesNoiseAtNoticeZone.Value;
        public static bool DontMakeAudibleNoisesCheat => PikminManager.instance != null && PikminManager.instance.Cheat_DontMakeAudibleNoises.Value;
        public static float PikminSignalCooldownCheat => PikminManager.instance != null ? PikminManager.instance.Cheat_PikminSingalCooldown.Value : -1;
        public static bool UselessBluesCheat => PikminManager.instance != null && PikminManager.instance.Cheat_UselessBluesMode.Value;
        public static bool InviciblePikminCheat => PikminManager.instance != null && PikminManager.instance.Cheat_InvinceablePikmin.Value;
        public static bool NoKnockbackCheat => PikminManager.instance != null && PikminManager.instance.Cheat_NoKnockback.Value;
        public static float WhistleZoneRadiusCheat => PikminManager.instance != null ? PikminManager.instance.Cheat_MaxWhistleZoneRadius.Value : -1f;
        public static float WhistleZoneDistanceCheat => PikminManager.instance != null ? PikminManager.instance.Cheat_MaxWhistleZoneDistance.Value : -1f;
        public static float PlayerNoticeZoneSizeCheat => PikminManager.instance != null ? PikminManager.instance.Cheat_PlayerNoticeZoneSize.Value : -1f;
        public static float PikminDamageMultiplerCheat => PikminManager.instance != null ? PikminManager.instance.Cheat_PikminDamageMultipler.Value : -1f;
        public static float PikminSpeedMultiplerCheat => PikminManager.instance != null ? PikminManager.instance.Cheat_PikminSpeedMultipler.Value : -1f;

        public void BindLCconfigs()
        {
            LethalConfigManager.SetModDescription("Adds Functional Pikmin to Lethal Company!");
        }

        public void UpdateSyncedConfigs()
        {
            if (PikminManager.instance == null)
            {
                return;
            }

            if (PikminManager.instance.LocalLeader == null)
            {
                return;
            }
        }

        public static void UpdateDefaultGeneration()
        {
            Logger.LogInfo($"Current Generation set to: {DefaultGeneration.InternalValue}");
            GenerationManager.Instance.SwitchGeneration(DefaultGeneration.InternalValue);
            PikminModelGeneration.Entry.BoxedValue = -1;
            SoulSpriteGeneration.Entry.BoxedValue = -1;
            OnionModelGeneration.Entry.BoxedValue = -1;
            OnionItemModelGeneration.Entry.BoxedValue = -1;
            SproutModelGeneration.Entry.BoxedValue = -1;
            PikminSoundGeneration.Entry.BoxedValue = -1;
            PuffminGeneration.Entry.BoxedValue = -1;
        }
        /// <summary>
        /// 1 => "Pikmin",
        /// 2 => "Sprout",
        /// 3 => "PikminSound",
        /// 4 => "Soul",
        /// 5 => "Onion",
        /// 6 => "OnionItem",
        /// 7 => "Puffmin",
        /// </summary>
        /// <param name="numb"></param>
        public static void UpdateCurrentGeneration(int numb, ref ConfigItem<CfgPikminGeneration> itm)
        {
            string identifier = numb switch
            {
                1 => "Pikmin",
                2 => "Sprout",
                3 => "PikminSound",
                4 => "Soul",
                5 => "Onion",
                6 => "OnionItem",
                7 => "Puffmin",
                _ => "???"
            };
            Logger.LogInfo($"{identifier}'s Current Generation set to: {itm.InternalValue}");
            PikminGeneration generation = default;
            if (itm.InternalValue == CfgPikminGeneration.Default)
            {
                generation = DefaultGeneration.InternalValue;
            }
            else
            {
                generation = (PikminGeneration)itm.InternalValue;
            }
            switch (identifier)
            {
                case "Pikmin":
                    foreach (PikminAI pikmin in FindObjectsByType<PikminAI>(FindObjectsSortMode.None))
                    {
                        pikmin.SwitchGeneration(generation);
                    }
                    break;
                case "Sprout":
                    foreach (Sprout sprout in FindObjectsByType<Sprout>(FindObjectsSortMode.None))
                    {
                        sprout.SwitchGeneration(generation);
                    }
                    break;
                case "PikminSound":
                    foreach (PikminAI pikmin in FindObjectsByType<PikminAI>(FindObjectsSortMode.None))
                    {
                        pikmin.SwitchSoundGeneration(generation);
                    }
                    break;
                case "Soul":
                    break;
                case "Onion":
                    foreach (BaseOnion onion in FindObjectsByType<BaseOnion>(FindObjectsSortMode.None))
                    {
                        onion.SwitchGeneration(generation);
                    }
                    if (PikminManager.instance != null && PikminManager.instance.shipPhaseOnionContainer != null)
                    {
                        PikminManager.instance.shipPhaseOnionContainer.SwitchGeneration(generation);
                    }
                    break;
                case "OnionItem":
                    foreach (OnionItem onionItem in FindObjectsByType<OnionItem>(FindObjectsSortMode.None))
                    {
                        onionItem.SwitchGeneration(generation);
                    }
                    break;
                case "Puffmin":
                    foreach (PuffminAI puffmin in FindObjectsByType<PuffminAI>(FindObjectsSortMode.None))
                    {
                        puffmin.SwitchGeneration(generation);
                    }
                    break;
            }
        }

        public void UpdateHUDLayout()
        {
            if (PikminHUDManager.instance == null)
            {
                return;
            }
            PikminHUDManager.instance.SetLayout(HUDPreset.InternalValue);
        }

        public void SetFollowMode()
        {
            if (PikminManager.instance == null)
            {
                return;
            }

            if (PikminManager.instance.LocalLeader == null)
            {
                return;
            }

            PikminManager.instance.LocalLeader.formManager.enabled = PikminFollowMode.InternalValue == PfollowMode.New;
        }
        #endregion


        public static PikminAI AddCustomScriptToPikminAI(PikminAI script)
        {
            Type customAIType = script.GetType();
            GameObject prefab = PikminEnemyType.enemyPrefab;
            PikminAI pikminAI = prefab.GetComponent<PikminAI>();

            // 1. Add custom component first without destroying original
            PikminAI customAI = (PikminAI)prefab.AddComponent(customAIType);

            // 2. Copy all fields from original PikminAI to custom component using reflection
            FieldInfo[] fields = typeof(PikminAI).GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            foreach (var field in fields)
            {
                field.SetValue(customAI, field.GetValue(pikminAI));
            }

            // 3. Copy fields from prefab's custom script to override with defaults
            FieldInfo[] customFields = customAIType.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            foreach (var field in customFields)
            {
                // Skip fields that exist in the base class, as we've already copied those
                if (typeof(PikminAI).GetField(field.Name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance) != null)
                    continue;

                field.SetValue(customAI, field.GetValue(script));
            }

            PikminEnemyType.enemyPrefab.GetComponent<PikminTypeResolver>().PikminAIs.Add(customAI);

            // 4. Disable the custom script
            customAI.enabled = false;

            LethalMin.Logger.LogInfo($"Added Custom Script: {customAIType.Name} to PikminAI!");

            return customAI;
        }


        public static void GeneratePikminTypeConfig(PikminType pikminType)
        {
            if (pikminType == null)
            {
                Logger.LogWarning("Cannot generate config for null PikminType");
                return;
            }

            Logger.LogInfo($"Generating config file for Pikmin Type: {pikminType.PikminName}");

            // Create Pikmin subfolder in config directory
            string configPath = Path.Combine(Paths.ConfigPath, "Pikmin");
            Directory.CreateDirectory(configPath);

            // Create config file for this specific Pikmin type
            string configFileName = $"{SanitizeFileName(pikminType.PikminName)}.cfg";
            string fullConfigPath = Path.Combine(configPath, configFileName);

            // Create a separate config file for this Pikmin type
            var pikminConfig = new ConfigFile(fullConfigPath, true);

            // Get all fields from PikminType
            FieldInfo[] fields = typeof(PikminType).GetFields(BindingFlags.Public | BindingFlags.Instance);

            string currentSection = "General";

            foreach (var field in fields)
            {
                // Check if field has Header attribute (for sections) - handle multiple headers
                var headerAttributes = field.GetCustomAttributes<HeaderAttribute>();
                var headerAttribute = headerAttributes.LastOrDefault();
                if (headerAttribute != null)
                {
                    currentSection = headerAttribute.header;
                    continue;
                }

                // Skip fields that shouldn't be configurable
                if (ShouldSkipField(field))
                    continue;

                // Check if type is supported by ConfigItem
                if (!IsSupportedConfigType(field.FieldType))
                {
                    Logger.LogDebug($"Skipping unsupported field type: {field.Name} ({field.FieldType})");
                    continue;
                }

                // Get tooltip for description - handle multiple tooltips
                var tooltipAttributes = field.GetCustomAttributes<TooltipAttribute>();
                var tooltipAttribute = tooltipAttributes.FirstOrDefault();
                string description = tooltipAttribute?.tooltip ?? $"Configuration for {field.Name}";

                // Get current value from the PikminType instance
                object currentValue = field.GetValue(pikminType);

                // Create config entry and apply value back to PikminType
                CreateAndBindConfigEntry(pikminConfig, field, pikminType, currentSection, description, currentValue);
            }

            string bepInExPath = Path.GetDirectoryName(Path.GetDirectoryName(Paths.ConfigPath)); // Gets BepInEx folder
            string relativePath = Path.GetRelativePath(bepInExPath, fullConfigPath);
            Logger.LogInfo($"Config file generated for Pikmin Type: {pikminType.PikminName} at BepInEx/{relativePath}");
        }

        private static bool ShouldSkipField(FieldInfo field)
        {
            // Skip fields that shouldn't be configurable
            var skipFields = new HashSet<string>
            {
                "PikminTypeID",
                "PiklopediaEntry",
                "modelRefernces",
                "ModelPrefab",
                "SoundPack",
                "SoundPackGenerations",
                "PikminGhostOverrideTexture",
                "PikminGhostOverrideModel",
                "PikminSproutOverrideMaterial",
                "SproutOverrideModel",
                "piklopediaVideo",
                "OverridePiklopediaNode",
                "OverridePiklopediaKeyword",
                "CustomTypeScript",
                "TargetOnion",
                "piklopediaDescription",
            };

            // Use GetCustomAttributes to handle multiple HideInInspector attributes
            var hideInInspectorAttributes = field.GetCustomAttributes<HideInInspector>();
            return skipFields.Contains(field.Name) || hideInInspectorAttributes.Any();
        }

        private static bool IsSupportedConfigType(Type type)
        {
            // Check if type is supported by ConfigItem based on the class definition
            if (type == typeof(bool) || type == typeof(int) || type == typeof(float) || type == typeof(string))
                return true;

            if (type.IsEnum)
                return true;

            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Color))
                return true;

            // Check for List types
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = type.GetGenericArguments()[0];
                return elementType == typeof(string) || elementType == typeof(int) || elementType == typeof(float) ||
                       elementType == typeof(bool) || elementType == typeof(Color) || elementType == typeof(Vector2) ||
                       elementType == typeof(Vector3) || elementType.IsEnum;
            }

            // Check for Array types
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                return elementType == typeof(string) || elementType == typeof(int) || elementType == typeof(float) ||
                       elementType == typeof(bool) || elementType == typeof(Color) || elementType == typeof(Vector2) ||
                       elementType == typeof(Vector3) || elementType.IsEnum;
            }

            return false;
        }

        private static void CreateAndBindConfigEntry(ConfigFile config, FieldInfo field, PikminType pikminType, string section, string description, object currentValue)
        {
            Type fieldType = field.FieldType;

            try
            {
                // Use reflection to create ConfigItem<T> of the appropriate type
                Type configItemType = typeof(ConfigItem<>).MakeGenericType(fieldType);

                // Create the ConfigItem instance
                object configItem = Activator.CreateInstance(configItemType,
                    section,                           // section
                    field.Name,                       // key
                    currentValue,                     // defaultValue
                    description,                      // description
                    true,                           // needsRestart
                    ConfigItemAuthority.Client,       // authority
                    config,                          // config
                    GetItemArgs(fieldType)           // args
                );

                if (configItem != null)
                {
                    // Get the InternalValue property to read the config value
                    FieldInfo internalValueProp = configItemType.GetField("InternalValue");

                    if (internalValueProp != null)
                    {
                        // Apply the config value back to the PikminType field
                        object configValue = internalValueProp.GetValue(configItem);
                        field.SetValue(pikminType, configValue);
                        Logger.LogDebug($"{field.Name} set to {configValue}");

                        //Logger.LogDebug($"Created config entry for {field.Name} with value: {configValue}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create config entry for field {field.Name}: {ex}");
            }
        }

        private static void UpdatePikminTypeField(PikminType pikminType, FieldInfo field, object newValue)
        {
            try
            {
                field.SetValue(pikminType, newValue);
                Logger.LogDebug($"Updated {pikminType.PikminName}.{field.Name} to: {newValue}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to update field {field.Name}: {ex}");
            }
        }

        private static string GetItemArgs(Type type)
        {
            // Return appropriate args for ConfigItem based on type
            if (type == typeof(float))
            {
                return ""; // Could add slider args here if needed
            }
            else if (type.IsEnum)
            {
                return ""; // Could add boolenum for bool-like enums
            }

            return "";
        }

        private static string SanitizeFileName(string fileName)
        {
            // Remove invalid file name characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }

        public static bool IsDependencyLoaded(string pluginGUID)
        {
            return Chainloader.PluginInfos.ContainsKey(pluginGUID);
        }

        public static bool GetLeaderViaID(ulong ClientID, out Leader leader)
        {
            leader = GetLeaderViaID(ClientID)!;
            return leader != null;
        }

        public static PikminType GetPikminTypeByName(string name)
        {
            return RegisteredPikminTypes.Values.FirstOrDefault(t => t.PikminName == name);
        }

        public static PikminType GetPikminTypeByID(int ID)
        {
            return RegisteredPikminTypes.TryGetValue(ID, out PikminType pikminType) ? pikminType : null!;
        }

        public static Leader? GetLeaderViaID(ulong ClientID)
        {
            foreach (var leader in PikminManager.instance.Leaders)
            {
                if (leader.Controller.OwnerClientId == ClientID && PikChecks.IsPlayerConnected(leader.Controller))
                    return leader;
            }
            return null;
        }

        public static OnionType GetOnionTypeByID(int ID)
        {
            return RegisteredOnionTypes[ID];
        }

        public static bool IsValidPikminTypeID(int ID)
        {
            return RegisteredPikminTypes.ContainsKey(ID);
        }

        public static bool UseInputUtils => IsDependencyLoaded("com.rune580.LethalCompanyInputUtils") && (!InVRMode || !(DontUseInputUtilsForVR && InVRMode));
        public static bool UseModDataLib => IsDependencyLoaded("MaxWasUnavailable.LethalModDataLib") && UseModDataLibConfig.InternalValue;
        public static bool OnCompany => RoundManager.Instance != null &&
        (RoundManager.Instance.currentLevel.sceneName == "CompanyBuilding" || RoundManager.Instance.currentLevel.sceneName == "MusemaScene");

        /// <summary>
        /// This is only set to true when LethalMin VR is loaded so let's hope we don't need to use it during startup.
        /// </summary>
        public static bool InVRMode = false;

        internal void NetcodePatcher()
        {
            var types = GetTypesWithErrorHandling();
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
                        catch (FileNotFoundException e)
                        {
                            // Log warning about missing dependency and continue
                            Logger.LogDebug($"Skipping method {method.Name} due to missing dependency: {e}");
                        }
                        catch (Exception e)
                        {
                            Logger.LogWarning($"Error processing method {method.Name}: {e}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Error processing type {type.FullName}: {e}");
                }
            }
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

                    // Check for compatibility attribute
                    var compatAttribute = type.GetCustomAttribute<CompatClassAttribute>();
                    if (compatAttribute != null)
                    {
                        string modGUID = compatAttribute.ModGUID;
                        if (IsDependencyLoaded(modGUID))
                        {
                            Logger.LogMessage($"{modGUID} detected, Patching {type.FullName}");
                        }
                        else
                        {
                            continue;
                        }
                    }

                    try
                    {
                        Harmony.PatchAll(type);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Error patching type {type.FullName}: {e}");
                        if (e.InnerException != null)
                        {
                            Logger.LogError($"Inner exception: {e.InnerException.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error during patching process: {e}");
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
                    Harmony?.PatchAll(type);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error patching type {type.FullName}: {e}");
                    if (e.InnerException != null)
                    {
                        Logger.LogError($"Inner exception: {e.InnerException.Message}");
                    }
                }
            }
        }
        internal static Type[] GetTypesWithErrorHandling()
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
                    Logger.LogDebug($"Loader Exception: {loaderException.Message}");
                    if (loaderException is FileNotFoundException fileNotFound)
                    {
                        Logger.LogDebug($"Could not load file: {fileNotFound.FileName}");
                    }
                }
                return e.Types.Where(t => t != null).ToArray();
            }
            catch (Exception e)
            {
                Logger.LogError($"Unexpected error while getting types: {e}");
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
