using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Patches;
using LethalMin.Pikmin;
using LethalMin.Utils;
using LethalMin.Patches.AI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using LethalLevelLoader;
using BepInEx.Configuration;
using BepInEx;
using System.IO;
using LethalModDataLib.Features;
using UnityEngine.Animations.Rigging;
using GameNetcodeStuff;
using LethalMin.Routeing;

namespace LethalMin
{
    public class PikminManager : NetworkBehaviour
    {
        public static PikminManager instance = null!;

        public enum PikminOddsPositions
        {
            Indoors,
            Outdoors,
            Sprouts,
            NoSprouts,
            Any
        }

        public float CleanupCheckTimer = 0;
        public static bool CanPathOnMoonGlobal;
        public static bool IsTooManyPikminOnMap => instance.PikminAIs.Count > LethalMin.MaxPikmin.InternalValue;
        public PikminEndOfGameStats EndOfGameStats = new PikminEndOfGameStats();
        public PikminFiredStats FiredStats = new PikminFiredStats();
        public Transform ShipPosition = null!;
        public Volume ChargeVolume = null!;
        public Coroutine ChargeTweenCoroutine = null!;
        public bool UseSaveModLib => LethalMin.IsDependencyLoaded("MaxWasUnavailable.LethalModDataLib");
        public ShipPhaseOnionContainer shipPhaseOnionContainer = null!;
        public MoonSettings? CurrentMoonSettings = null;
        public Dictionary<string, int> LeaflingPlayers = new Dictionary<string, int>();
        internal NetworkVariable<bool> Cheat_WhistleMakesNoiseAtNoticeZone = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<bool> Cheat_DontMakeAudibleNoises = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<float> Cheat_PikminSingalCooldown = new NetworkVariable<float>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<bool> Cheat_UselessBluesMode = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<bool> Cheat_InvinceablePikmin = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<bool> Cheat_NoKnockback = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<float> Cheat_MaxWhistleZoneRadius = new NetworkVariable<float>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<float> Cheat_MaxWhistleZoneDistance = new NetworkVariable<float>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<float> Cheat_PlayerNoticeZoneSize = new NetworkVariable<float>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<float> Cheat_PikminDamageMultipler = new NetworkVariable<float>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<float> Cheat_PikminSpeedMultipler = new NetworkVariable<float>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<float> Cheat_ChargeCoolDown = new NetworkVariable<float>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        internal NetworkVariable<float> Cheat_ChargeDistance = new NetworkVariable<float>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        #region Initialization & Core Methods
        void Awake()
        {
            instance = this;
            name = "Pikmin Manager";
            gameObject.AddComponent<PikminRouteManager>();
        }
        void Start()
        {
            foreach (EnemyType type in LethalMin.EnemyTypes)
            {
                try
                {
                    EnemyAIPatch.AddPikminEnemyToEnemyAI(type);
                }
                catch (System.Exception e)
                {
                    LethalMin.Logger.LogError($"Error adding Pikmin Enemy for {type.enemyName}: {e}");
                }
            }

            foreach (MaskedPlayerEnemy maskedPlayerEnemy in Resources.FindObjectsOfTypeAll<MaskedPlayerEnemy>())
            {
                try
                {
                    MaskedPlayerEnemyPatch.RegisterAsPuffminLeader(maskedPlayerEnemy);
                }
                catch (System.Exception e)
                {
                    LethalMin.Logger.LogError($"Error registering MaskedPlayerEnemy {maskedPlayerEnemy.name} as Puffmin Leader: {e}");
                }
            }

            EnemyAIPatch.FillinEnemyHPs();

            GameObject volPrefab = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/HUD/ChargeVolume.prefab");
            ChargeVolume = Instantiate(volPrefab).gameObject.GetComponentInChildren<Volume>();
            ChargeVolume.name = "Pikmin Charge Volume";

            shipPhaseOnionContainer = Instantiate(LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/ShipPhaseOnionContainer.prefab")).GetComponent<ShipPhaseOnionContainer>();

            LoadInitalData();

            if (ShipPosition == null)
            {
                ShipPosition = new GameObject("LethalMin_ShipPosition").transform;
            }
            ShipPosition.position = StartOfRound.Instance.elevatorTransform.position;
            ShipPosition.rotation = StartOfRound.Instance.elevatorTransform.rotation;

            SyncLeaflingPlayers();

            if (!IsServer)
            {
                OnPlayerJoinnedServerRpc();
                //ConfigItemAuthorityManager.Instance.FetchServerConfigsServerRpc();
            }

            Cheat_InvinceablePikmin.OnValueChanged += (oldValue, newValue) =>
            {
                LethalMin.Logger.LogInfo($"Cheat_InvinceablePikmin set to {newValue}");
            };
        }

        void Update()
        {
            if (IsServer)
            {
                Cheat_WhistleMakesNoiseAtNoticeZone.Value = LethalMin.WhistleMakesNoiseAtNoiceZone;
                Cheat_DontMakeAudibleNoises.Value = LethalMin.DontMakeAudibleNoises;
                Cheat_PikminSingalCooldown.Value = LethalMin.PikminSignalCooldown;
                Cheat_UselessBluesMode.Value = LethalMin.UselessBluesMode;
                Cheat_InvinceablePikmin.Value = LethalMin.InvinceablePikmin;
                Cheat_NoKnockback.Value = LethalMin.NoKnockBack;
                Cheat_MaxWhistleZoneRadius.Value = LethalMin.MaxWhistleZoneRadius;
                Cheat_MaxWhistleZoneDistance.Value = LethalMin.MaxWhistleZoneDistance;
                Cheat_PlayerNoticeZoneSize.Value = LethalMin.PlayerNoticeZoneSize;
                Cheat_PikminDamageMultipler.Value = LethalMin.PikminDamageMultipler;
                Cheat_PikminSpeedMultipler.Value = LethalMin.PikminSpeedMultipler;
                Cheat_ChargeCoolDown.Value = LethalMin.ChargeCooldown;
                Cheat_ChargeDistance.Value = LethalMin.ChargeDistance;
            }
            if (!StartOfRound.Instance.inShipPhase && IsServer)
            {
                if (CleanupCheckTimer >= 0)
                {
                    CleanupCheckTimer -= Time.deltaTime;
                }
                else
                {
                    CleanUpExcessPikmin();
                    CleanupCheckTimer = 1;
                }
            }
            if (shipPhaseOnionContainer != null)
                shipPhaseOnionContainer.gameObject.SetActive(LethalMin.ShowOnionsInSpace && StartOfRound.Instance.inShipPhase);
        }

        public IEnumerator TweenChargeWeight(float targetWeight, float duration = 0.5f, bool autoFadeOut = true, float holdTime = 0.2f)
        {
            if (ChargeTweenCoroutine != null)
            {
                StopCoroutine(ChargeTweenCoroutine);
                ChargeTweenCoroutine = null!;
            }

            // First tween up to target value
            float startWeight = ChargeVolume.weight;
            float startTime = Time.time;
            float endTime = startTime + duration;

            while (Time.time < endTime)
            {
                float t = (Time.time - startTime) / duration;
                // Use smoothstep for more natural easing
                float smoothT = t * t * (3f - 2f * t);
                ChargeVolume.weight = Mathf.Lerp(startWeight, targetWeight, smoothT);
                yield return null;
            }

            // Ensure we end exactly at target value
            ChargeVolume.weight = targetWeight;

            // Optional hold at max intensity
            if (holdTime > 0)
                yield return new WaitForSeconds(holdTime);

            // Automatically fade back to zero if requested
            if (autoFadeOut && targetWeight > 0)
            {
                startTime = Time.time;
                endTime = startTime + duration;
                startWeight = ChargeVolume.weight;

                while (Time.time < endTime)
                {
                    float t = (Time.time - startTime) / duration;
                    // Use smoothstep for more natural easing
                    float smoothT = t * t * (3f - 2f * t);
                    ChargeVolume.weight = Mathf.Lerp(startWeight, 0f, smoothT);
                    yield return null;
                }

                // Ensure we end at zero
                ChargeVolume.weight = 0f;
            }

            ChargeTweenCoroutine = null!;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }
        #endregion






        #region Collection Management
        public Leader LocalLeader = null!;
        public List<Leader> Leaders { get; private set; } = new List<Leader>();
        public HashSet<PikminAI> PikminAIs { get; private set; } = new HashSet<PikminAI>();
        public HashSet<PikminAI> PikminAICounter { get; private set; } = new HashSet<PikminAI>();
        public HashSet<EnemyAI> EnemyAIs { get; private set; } = new HashSet<EnemyAI>();
        public HashSet<EnemyAI> ConvertedAIs { get; private set; } = new HashSet<EnemyAI>();
        public HashSet<PikminItem> PikminItems { get; private set; } = new HashSet<PikminItem>();
        public HashSet<PikminEnemy> PikminEnemies { get; private set; } = new HashSet<PikminEnemy>();
        public HashSet<Onion> Onions { get; private set; } = new HashSet<Onion>();
        public HashSet<BaseOnion> OnionsSpawnable => new HashSet<BaseOnion>(Onions.Where(o => o != null).Select(o => o as BaseOnion).Where(o => o != null)!);
        public HashSet<PikminVehicleController> Vehicles { get; private set; } = new HashSet<PikminVehicleController>();
        public Dictionary<EnemyType, Item> EnemyItems { get; private set; } = new Dictionary<EnemyType, Item>();
        public HashSet<PuffminLeader> PuffminLeaders { get; private set; } = new HashSet<PuffminLeader>();
        public HashSet<PuffminAI> PuffminAIs { get; private set; } = new HashSet<PuffminAI>();
        public HashSet<ItemArrivalZone> ItemArrivalZones { get; private set; } = new HashSet<ItemArrivalZone>();
        public List<FreezeableWater> FreezeableWaters = new List<FreezeableWater>();
        public List<int> ScannedPiklopediaIDs = new List<int>();
        public List<int> NewlyScannedPiklopediaIDs = new List<int>();

        public void AddPikminAI(PikminAI pikminAI)
        {
            if (PikminAIs.Contains(pikminAI))
            {
                LethalMin.Logger.LogWarning($"Pikmin AI {pikminAI.DebugID} already exists in the PikminManager.");
                return;
            }
            PikminAIs.Add(pikminAI);
        }
        public void RemovePikminAI(PikminAI pikminAI)
        {
            PikminAICounter.Remove(pikminAI);
            if (!PikminAIs.Contains(pikminAI))
            {
                LethalMin.Logger.LogWarning($"Pikmin AI {pikminAI.DebugID} does not exist in the PikminManager.");
                return;
            }
            PikminAIs.Remove(pikminAI);
        }

        public void AddPikminItem(PikminItem pikminItem)
        {
            if (PikminItems.Contains(pikminItem))
            {
                LethalMin.Logger.LogWarning($"Pikmin Item {pikminItem.name} already exists in the PikminManager.");
                return;
            }
            PikminItems.Add(pikminItem);
        }
        public void RemovePikminItem(PikminItem pikminItem)
        {
            if (!PikminItems.Contains(pikminItem))
            {
                LethalMin.Logger.LogWarning($"Pikmin Item {pikminItem.name} does not exist in the PikminManager.");
                return;
            }
            PikminItems.Remove(pikminItem);
        }

        public void AddLeader(Leader leader)
        {
            if (Leaders.Contains(leader))
            {
                LethalMin.Logger.LogWarning($"Leader {leader.name} already exists in the PikminManager.");
                return;
            }
            Leaders.Add(leader);
            Leaders = Leaders.OrderBy(l => l.name).ThenBy(l => l.OwnerClientId).ToList();
        }

        public void AddOnion(Onion onion)
        {
            if (Onions.Contains(onion))
            {
                LethalMin.Logger.LogWarning($"Onion {onion.name} already exists in the PikminManager");
                return;
            }
            Onions.Add(onion);
        }

        public void RemoveOnion(Onion onion)
        {
            if (!Onions.Contains(onion))
            {
                LethalMin.Logger.LogWarning($"Onion {onion.name} Does not exists in the PikminManager");
                return;
            }
            Onions.Remove(onion);
        }

        [ServerRpc(RequireOwnership = false)]
        public void OnPlayerJoinnedServerRpc()
        {
            LethalMin.Logger.LogInfo($"Player joined the server, syncing PikminManager data.");
            SyncAllPikminItemsOnServer();
            SyncGameStatsClientRpc(EndOfGameStats, FiredStats);
            SyncAllGlowmobs();
            SyncScannedPikminClientRpc(ScannedPiklopediaIDs.ToArray());
            SyncLeaflingPlayers();
            shipPhaseOnionContainer.RefreshOnions();
        }

        public void SyncLeaflingPlayers()
        {
            if (LeaflingPlayers.Count == 0)
            {
                return;
            }

            foreach (Leader leader in FindObjectsOfType<Leader>())
            {
                if (!PikChecks.IsPlayerConnected(leader.Controller))
                {
                    LethalMin.Logger.LogInfo($"Leader {leader.name} is not connected, skipping sync.");
                    continue;
                }
                foreach (var kvp in LeaflingPlayers)
                {
                    LethalMin.Logger.LogInfo($"Checking Leafling Player: {kvp.Key} with Pikmin Type ID: {kvp.Value} = {leader.Controller.playerUsername} {leader.OwnerClientId}");
                    if (leader.Controller.playerUsername == kvp.Key)
                    {
                        SetLeaderAsLeaflingClientRpc(leader.OwnerClientId, kvp.Value);
                        LethalMin.Logger.LogInfo($" loaded Leader {kvp.Key} as leafling with Pikmin Type ID: {kvp.Value}");
                    }
                }
            }
        }
        [ClientRpc]
        public void SetLeaderAsLeaflingClientRpc(ulong ID, int pikminTypeID)
        {
            Leader? leader = LethalMin.GetLeaderViaID(ID);
            if (leader != null)
            {
                if (!leader.IsLeafling)
                {
                    leader.SetAsLeafling(pikminTypeID);
                    LethalMin.Logger.LogInfo($"Set Leader {leader.name} as Leafling with Pikmin Type ID: {pikminTypeID}");
                }
            }
            else
            {
                LethalMin.Logger.LogWarning($"Failed to set Leader as Leafling, Leader not found for Client ID: {ID}");
            }
        }
        public void SyncAllGlowmobs()
        {
            foreach (Glowmob mob in FindObjectsOfType<Glowmob>())
            {
                LethalMin.Logger.LogInfo($"Syncing Glowmob to {mob.leaderScript.Controller.playerUsername}");
                mob.SetLeaderClientRpc(mob.leaderScript.NetworkObject);
            }
        }
        public void SyncAllPikminItemsOnServer()
        {
            LethalMin.Logger.LogInfo("Syncing all Pikmin Items to clients");
            foreach (PikminItem pikminItem in PikminItems)
            {
                if (pikminItem.DontUseInitClientRpc)
                {
                    LethalMin.Logger.LogInfo($"skipping Pikmin Item {pikminItem.name} to clients");
                    continue;
                }
                LethalMin.Logger.LogInfo($"Syncing Pikmin Item {pikminItem.name} to clients");
                pikminItem.InitalizeClientRpc(pikminItem.ItemScript.NetworkObject, pikminItem.ItemScript.name);
            }
        }
        [ClientRpc]
        public void SyncGameStatsClientRpc(PikminEndOfGameStats stats, PikminFiredStats fired)
        {
            if (IsServer)
            {
                LethalMin.Logger.LogInfo("Syncing Game Stats to clients");
                return;
            }
            EndOfGameStats = stats;
            FiredStats = fired;
        }

        [ClientRpc]
        public void SyncScannedPikminClientRpc(int[] scannedIDs)
        {
            if (IsServer)
            {
                return;
            }
            LethalMin.Logger.LogInfo("Syncing Scanned Pikmin IDs to clients");
            ScannedPiklopediaIDs = scannedIDs.ToList();
        }
        #endregion






        #region Game State Methods
        /// <summary>
        /// Called when the planet scene is loaded (local client)
        /// </summary>
        public void OnGameStarted()
        {
            LethalMin.Logger.LogInfo($"__ LethalMin On Game Started __");
            //PikUtils.AddTextToChangeOnLocalClient($"Game started, initializing PikminManager");
            LethalMin.Logger.LogInfo($"__ LethalMin On Game Started Finished __");
        }

        /// <summary>
        /// Called when the dungen is generated
        /// </summary>
        public void OnGameLoaded()
        {
            LethalMin.Logger.LogInfo($"__ LethalMin On Game Loaded __");
            //PikUtils.AddTextToChangeOnLocalClient($"Game Loaded, initializing PikminManager");
            CanPathOnMoonGlobal = PikChecks.IsNavMeshOnMap();
            EndOfGameStats.Refresh();
            GetMoonSettings();
            SpawnTeleportTriggers();
            AddWaterTriggers();
            AddBridgeTriggers();
            if (LethalMin.DieInPlayerDeathZone)
            {
                AddDeathTriggers();
            }
            if (IsServer)
            {
                SpawnSprouts();
                StartCoroutine(SpawnOnionsAfterDelay());
                SpawnMapPikmin();
                SpawnLumiknulls();
            }
            if (LethalMin.OnCompany)
            {
                GameObject go = new GameObject("ONION_SPAWN_POINT_1");
                SceneManager.MoveGameObjectToScene(go, SceneManager.GetSceneByName(RoundManager.Instance.currentLevel.sceneName));
                go.transform.position = RoundManager.Instance.GetNavMeshPosition(new Vector3(0, 0, 0));

                if (IsServer)
                {
                    GameObject goB = Instantiate(LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/CompanyPikminEnemyScript.prefab"));
                    NetworkObject netObj = goB.GetComponent<NetworkObject>();
                    SceneManager.MoveGameObjectToScene(goB, SceneManager.GetSceneByName(RoundManager.Instance.currentLevel.sceneName));
                    netObj.Spawn();
                }
            }
            PikminRouteManager.Instance.OnGameLoaded();
            LethalMin.Logger.LogInfo($"Can path on moon: {CanPathOnMoonGlobal} {PikChecks.IsNavMeshOnMap()}");

            // if (IsServer)
            //     StartCoroutine(WaitDebugSpawn());

            LethalMin.Logger.LogInfo($"__ LethalMin On Game Loaded Finished __");
        }

        /// <summary>
        /// Called after the ship starts to leave
        /// </summary>
        public void OnGameEnd()
        {
            LethalMin.Logger.LogInfo($"Game Ended, Deinitializing PikminManager");

            foreach (PikminAI pikmin in PikminAIs)
            {
                if (!pikmin.isOutside || pikmin.IsWildPikmin || pikmin.IsDeadOrDying)
                {
                    continue;
                }
                LethalMin.Logger.LogInfo($"Pikmin {pikmin.DebugID} is outside and not dead/dying, checking if it can leave");
                if (!PikChecks.DoesPikminHaveRegisteredOnion(pikmin))
                {
                    pikmin.SetPikminToLeaving(null);
                    pikmin.IsLeftBehind = true;
                    continue;
                }

                if (IsPikminInSafetyRange(pikmin))
                {
                    Onion? PossibleOnion = Onion.GetOnionOfPikmin(pikmin);
                    pikmin.SetPikminToLeaving(PossibleOnion);
                    if (PossibleOnion != null)
                    {
                        PossibleOnion.AddPikmin(pikmin);
                        PikminAICounter.Remove(pikmin);
                        pikmin.DontAddToOnion = true; // Don't add to the onion again when it leaves, since it already left once.
                    }
                    if (PossibleOnion == null)
                    {
                        pikmin.SetPikminToLeaving(null);
                        pikmin.IsLeftBehind = true;
                        EndOfGameStats.PikminLeftBehind++;
                        FiredStats.TotalPikminLost++;
                    }
                }
                else
                {
                    pikmin.SetPikminToLeaving(null);
                    pikmin.IsLeftBehind = true;
                    EndOfGameStats.PikminLeftBehind++;
                    FiredStats.TotalPikminLost++;
                }
            }
        }

        /// <summary>
        /// Called when the game fully ends and the results screen is showed
        /// </summary>
        public void OnGameEnded()
        {
            PikminRouteManager.Instance.OnGameEnded();
        }

        public bool IsPikminInSafetyRange(PikminAI pikmin)
        {
            Onion? PossibleOnion = Onion.GetOnionOfPikmin(pikmin);
            bool val = false;

            val = Vector3.Distance(StartOfRound.Instance.elevatorTransform.position, pikmin.transform.position) < 30f
            || (PossibleOnion != null && Vector3.Distance(PossibleOnion.transform.position, pikmin.transform.position) < 30f);

            return val;
        }
        #endregion






        #region Game Load Initalizeations

        public void GetMoonSettings()
        {
            CurrentMoonSettings = null;
            foreach (MoonSettings settings in Resources.FindObjectsOfTypeAll<MoonSettings>())
            {
                if (settings.Level == RoundManager.Instance.currentLevel)
                {
                    CurrentMoonSettings = settings;
                    LethalMin.Logger.LogInfo($"Found Moon Settings for {settings.name} on {settings.Level.sceneName}");
                    return;
                }
            }
        }

        public void AddDeathTriggers()
        {
            foreach (KillLocalPlayer killLocalPlayer in FindObjectsOfType<KillLocalPlayer>(true))
            {
                PikminDamageTrigger pdm = killLocalPlayer.gameObject.AddComponent<PikminDamageTrigger>();
                pdm.InstaDeath = !killLocalPlayer.justDamage;
                pdm.DontKillInShip = killLocalPlayer.disallowKillingInShip;
            }
        }

        public void AddWaterTriggers()
        {
            FreezeableWaters.Clear();
            foreach (QuicksandTrigger sand in FindObjectsOfType<QuicksandTrigger>(true))
            {
                if (sand.isWater && sand.gameObject.GetComponent<PikminEffectTrigger>() == null)
                {
                    PikminEffectTrigger trigger = sand.gameObject.AddComponent<PikminEffectTrigger>();
                    trigger.EffectType = PikminEffectType.Paralized;
                    trigger.HazardType = PikminHazard.Water;
                    trigger.Mode = PikminEffectMode.Persitant;
                    LethalMin.Logger.LogInfo($"Added Pikmin Effect Trigger to: {sand.gameObject.name}");
                }

                FreezeableWater fw = null!;
                if (sand.isWater && !sand.gameObject.TryGetComponent(out fw))
                {
                    fw = sand.gameObject.AddComponent<FreezeableWater>();
                }
                if (fw != null)
                    FreezeableWaters.Add(fw);
            }
            FreezeableWaters.Sort((a, b) => string.Compare(a.gameObject.name, b.gameObject.name, System.StringComparison.Ordinal));
        }

        public void AddBridgeTriggers()
        {
            foreach (BridgeTrigger bridge in FindObjectsOfType<BridgeTrigger>(true))
            {
                if (bridge.GetComponent<PikminBridgeTrigger>() == null)
                {
                    bridge.gameObject.AddComponent<PikminBridgeTrigger>();
                }
            }

            foreach (BridgeTriggerType2 bridge2 in FindObjectsOfType<BridgeTriggerType2>(true))
            {
                if (bridge2.GetComponent<PikminBridgeTrigger>() == null)
                {
                    bridge2.gameObject.AddComponent<PikminBridgeTrigger>();
                }
            }
        }

        public void SpawnSprouts()
        {
            if (LethalMin.OnCompany)
            {
                LethalMin.Logger.LogInfo($"Cannot Spawn Sprouts on comapyn moon!");
                return;
            }
            if (!PikChecks.IsNavMeshOnMap())
            {
                LethalMin.Logger.LogWarning($"Current map does not have a navmesh, cannot spawn sprouts!");
                return;
            }

            StartCoroutine(LoadSproutData());

            int SPAWN_AMOUNT = 100; //Mathf.Clamp(GameObject.FindGameObjectsWithTag("OutsideAINode").Length * 2, 0, 150);
            int PowerLV = StartOfRound.Instance.currentLevel.maxEnemyPowerCount;
            float SpawnChance = LethalMin.OutdoorSpawnChance.InternalValue;

            // Apply PowerLV scaling if enabled
            if (LethalMin.UsePowerLVForSprouts)
            {
                // Use power level directly to determine spawn chance
                // This creates a curve that gives ~0.2 at PowerLV=0, ~0.5 at PowerLV=13, and ~0.8 at PowerLV=70
                SpawnChance = 0.2f * (PowerLV * 0.25f);

                LethalMin.Logger.LogDebug($"Moon power level: {PowerLV}, spawn chance: {SpawnChance:F2}");
            }

            // ;)
            if (StartOfRound.Instance.currentLevel.sceneName == "Level10Adamance")
            {
                SpawnChance *= 1.5f;
            }


            // Calculate spawn chance LethalMin.OutdoorSpawnChance.InternalValue * spawnChanceMultiplier;

            // Get reference positions

            System.Random RNG = new System.Random(StartOfRound.Instance.randomMapSeed);

            Dictionary<PikminType, float> odds = GetPikminSpawningOddsOnCurrentMoon(PikminOddsPositions.Sprouts);

            GameObject[] IndoorSpawnPoints = GameObject.FindGameObjectsWithTag("AINode");

            GameObject[] OutdoorSpawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");

            LethalMin.Logger.LogDebug($"Sprouts Odds: {string.Join(", ", odds.Select(kvp => $"{kvp.Key.PikminName}: {kvp.Value}"))}");

            for (int i = 0; i < SPAWN_AMOUNT; i++)
            {
                if (Random.value > SpawnChance)
                {
                    continue;
                }

                Vector3 spawnPos = new Vector3();
                Quaternion RandomYRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                PikminType randomType = null!;

                //Chose a radom pikmin type from the odds dictionary
                float totalOdds = odds.Values.Sum();
                float randomValue = Random.Range(0, totalOdds);
                float cumulativeOdds = 0f;
                foreach (var kvp in odds)
                {
                    cumulativeOdds += kvp.Value;
                    if (randomValue <= cumulativeOdds)
                    {
                        randomType = kvp.Key;
                        break;
                    }
                }
                if (randomType == null)
                {
                    LethalMin.Logger.LogWarning($"No random type found for {i}, using alterntive random");
                    try
                    {
                        randomType = odds.Keys.ToList()[RNG.Next(0, odds.Keys.ToList().Count)];
                    }
                    catch (System.Exception e)
                    {
                        LethalMin.Logger.LogError($"Failed to get random type for {i}: {e}");
                        continue;
                    }
                }

                if (randomType.SpawnsIndoors)
                {
                    spawnPos = IndoorSpawnPoints[RNG.Next(0, IndoorSpawnPoints.Length)].transform.position;
                }

                if (randomType.SpawnsOutdoors)
                {
                    spawnPos = RoundManager.Instance.GetRandomNavMeshPositionInRadius(
                        OutdoorSpawnPoints[RNG.Next(0, OutdoorSpawnPoints.Length)].transform.position,
                        50);
                }

                GameObject SproutInstance = Instantiate(LethalMin.SproutPrefab, spawnPos, RandomYRotation);
                Sprout SproutScript = SproutInstance.GetComponent<Sprout>();
                SproutScript.pikminType = randomType;

                LayerMask groundLayer = StartOfRound.Instance.collidersAndRoomMaskAndDefault;

                if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 50, groundLayer, QueryTriggerInteraction.Ignore))
                {
                    spawnPos = hit.point;
                }
                else
                {
                    LethalMin.Logger.LogWarning("No ground detected.");
                    Destroy(SproutInstance);
                    continue;
                }

                LethalMin.Logger.LogDebug($"Spawning sprout {i} at: [{spawnPos}, {RandomYRotation.eulerAngles}]" +
                $" With type: {SproutScript.pikminType.PikminName} odds:({odds[SproutScript.pikminType]}) rng: ({randomValue} / {totalOdds})");

                SproutScript.NetworkObject.Spawn();

                SproutScript.InitalizeClientRpc(spawnPos, RandomYRotation, SproutScript.pikminType.PikminTypeID);
            }
        }

        public void SpawnMapPikmin()
        {
            if (IsTooManyPikminOnMap)
            {
                LethalMin.Logger.LogInfo($"Max Pikmin count reached, cannot spawn map pikmin!");
                return;
            }
            if (LethalMin.OnCompany)
            {
                LethalMin.Logger.LogInfo($"Cannot Spawn map pikmin on comapyn moon!");
                return;
            }
            if (!PikChecks.IsNavMeshOnMap())
            {
                LethalMin.Logger.LogWarning($"Current map does not have a navmesh, cannot spawn map pikmin!");
                return;
            }


            int SPAWN_AMOUNT = GameObject.FindGameObjectsWithTag("AINode").Length - FindObjectsOfType<Sprout>().Length;
            LethalMin.Logger.LogInfo($"Map Possible Spawn Ammount: {SPAWN_AMOUNT}");

            System.Random RNG = new System.Random(StartOfRound.Instance.randomMapSeed);

            Dictionary<PikminType, float> odds = GetPikminSpawningOddsOnCurrentMoon(PikminOddsPositions.NoSprouts);
            GameObject[] IndoorSpawnPoints = GameObject.FindGameObjectsWithTag("AINode");
            GameObject[] OutdoorSpawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");

            LethalMin.Logger.LogInfo($"Map Pikmin Odds: {string.Join(", ", odds.Select(kvp => $"{kvp.Key.PikminName}: {kvp.Value}"))}");

            for (int i = 0; i < SPAWN_AMOUNT; i++)
            {
                if (Random.value > LethalMin.IndoorSpawnChance.InternalValue)
                {
                    continue;
                }

                Vector3 spawnPos = new Vector3();

                Quaternion RandomYRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                PikminType randomType = null!;

                //Chose a radom pikmin type from the odds dictionary
                float totalOdds = odds.Values.Sum();
                float randomValue = Random.Range(0, totalOdds);
                float cumulativeOdds = 0f;
                foreach (var kvp in odds)
                {
                    cumulativeOdds += kvp.Value;
                    if (randomValue <= cumulativeOdds)
                    {
                        randomType = kvp.Key;
                        break;
                    }
                }
                if (randomType == null)
                {
                    LethalMin.Logger.LogWarning($"No random type found for {i}, using alterntive random");
                    try
                    {
                        randomType = odds.Keys.ToList()[RNG.Next(0, odds.Keys.ToList().Count)];
                    }
                    catch (System.Exception e)
                    {
                        LethalMin.Logger.LogError($"Failed to get random type for {i}: {e}");
                        continue;
                    }
                }

                bool spawnedIndoors = false;
                if (randomType.SpawnsIndoors && randomType.SpawnsOutdoors)
                {
                    // Randomly choose one of the two spawn types
                    if (Random.value < 0.5f)
                    {
                        spawnPos = IndoorSpawnPoints[RNG.Next(0, IndoorSpawnPoints.Length)].transform.position;
                        spawnedIndoors = true;
                    }
                    else
                    {
                        spawnPos = OutdoorSpawnPoints[RNG.Next(0, OutdoorSpawnPoints.Length)].transform.position;
                    }
                }
                else if (randomType.SpawnsIndoors)
                {
                    spawnPos = IndoorSpawnPoints[RNG.Next(0, IndoorSpawnPoints.Length)].transform.position;
                    spawnedIndoors = true;
                }
                else if (randomType.SpawnsOutdoors)
                {
                    spawnPos = OutdoorSpawnPoints[RNG.Next(0, OutdoorSpawnPoints.Length)].transform.position;
                }

                spawnPos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(spawnPos, spawnedIndoors ? 8f : 35f,
                 default, new System.Random(StartOfRound.Instance.randomMapSeed));

                PikminSpawnProps props = new PikminSpawnProps();
                props.IsOutside = randomType.SpawnsOutdoors;
                props.AddToSpawnCountForWild = true;

                SpawnPikminOnServer(randomType, spawnPos, RandomYRotation, props);

                LethalMin.Logger.LogDebug($"Spawning Pikmin {i} at: [{spawnPos}, {RandomYRotation}]" +
                $" With type: {randomType.PikminName} odds:({odds[randomType]}) rng: ({randomValue} / {totalOdds})");
            }
        }

        public void SpawnTeleportTriggers()
        {
            GameObject SpawnTeleportTrigger(Vector3 TPostion, Vector3 Scale, Vector3 DestPos, int ID)
            {
                Scene currentScene = SceneManager.GetSceneByName(RoundManager.Instance.currentLevel.sceneName);

                GameObject TPZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                TPZone.name = "Pikmin Teleport Trigger";
                TPZone.transform.position = TPostion;
                TPZone.transform.localScale = Scale;
                TPZone.AddComponent<PikminTeleportTrigger>();
                TPZone.GetComponent<Collider>().isTrigger = true;
                SceneManager.MoveGameObjectToScene(TPZone, currentScene);

                GameObject Dest = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Dest.name = "Pikmin Teleport Destination";
                Dest.transform.position = DestPos;
                TPZone.GetComponent<PikminTeleportTrigger>().Destination = Dest.transform;
                SceneManager.MoveGameObjectToScene(Dest, currentScene);

                PikminRouteManager.Instance.AddedTelepointsForExits.Add(ID, Dest.transform);

                Destroy(TPZone.GetComponent<Renderer>());
                Destroy(Dest.GetComponent<Renderer>());
                Destroy(Dest.GetComponent<Collider>());

                return TPZone;
            }

            //Offense's FireExit
            if (RoundManager.Instance.currentLevel.sceneName == "Level7Offense")
            {
                SpawnTeleportTrigger(new Vector3(-6.9494f, 18.3041f, -134.7692f), new Vector3(10f, 10f, 10f), new Vector3(7.9994f, 0.4394f, -136.0314f), 1);
            }

            //Assurance's FireExit
            if (RoundManager.Instance.currentLevel.sceneName == "Level2Assurance")
            {
                SpawnTeleportTrigger(new Vector3(101.3612f, 15.7869f, -74.0713f), new Vector3(2.5527f, 4.6818f, 7.8745f), new Vector3(99.9956f, 2.2635f, -57.4636f), 1);
            }

            //March's 3rd FireExit
            if (RoundManager.Instance.currentLevel.sceneName == "Level4March")
            {
                SpawnTeleportTrigger(new Vector3(125.5447f, 8.4454f, -17.235f), new Vector3(1.0982f, 0.8345f, 5.8745f), new Vector3(129.4601f, 6.4522f, -16.061f), 3);
            }

            //Embrion's Main
            if (RoundManager.Instance.currentLevel.sceneName == "Level11Embrion")
            {
                GameObject point =
                SpawnTeleportTrigger(new Vector3(-190.1637f, 10.6708f, -16.6201f), new Vector3(4.0527f, 10.9381f, 18.4337f), new Vector3(-181.8325f, 0.5593f, -15.6719f), 0);
                point.transform.rotation = Quaternion.Euler(0, 40, 0);
            }
        }

        #endregion






        #region EnemyItem Conversion
        [ServerRpc(RequireOwnership = false)]
        public void ConvertEnemyToGrabbableObjectServerRpc(NetworkObjectReference enemyRef)
        {
            if (!enemyRef.TryGet(out NetworkObject enemyObj))
            {
                LethalMin.Logger.LogWarning("Failed to get enemy object from reference");
                return;
            }
            if (!enemyObj.TryGetComponent(out EnemyAI enemy))
            {
                LethalMin.Logger.LogWarning("Failed to get EnemyAI component from object");
                return;
            }

            LethalMin.Logger.LogInfo("Adding Physic Prop to enemy body " + enemy.gameObject.name);

            GameObject enemyGrabObject = Instantiate(LethalMin.EnemyItemNode, enemy.transform.position, enemy.transform.rotation);
            enemyGrabObject.name = "EnemyItemNode_" + enemy.enemyType.enemyName; // Name the object for easier debugging
            NetworkObject enemyGrabNetworkObject = enemyGrabObject.GetComponent<NetworkObject>();
            enemyGrabNetworkObject.Spawn();

            SyncEnemyToGrabbableObjectConversionClientRpc(enemy.NetworkObject, enemyGrabNetworkObject); // Notify clients of the conversion
        }

        [ClientRpc]
        public void SyncEnemyToGrabbableObjectConversionClientRpc(NetworkObjectReference enemyRef, NetworkObjectReference itemNode)
        {
            if (!enemyRef.TryGet(out NetworkObject enemyObj))
            {
                LethalMin.Logger.LogWarning("Failed to get enemy object from reference");
                return;
            }
            if (!enemyObj.TryGetComponent(out EnemyAI enemy))
            {
                LethalMin.Logger.LogWarning("Failed to get EnemyAI component from object");
                return;
            }
            if (!itemNode.TryGet(out NetworkObject itemObj))
            {
                LethalMin.Logger.LogWarning("Failed to get item object from reference");
                return;
            }
            if (!itemObj.TryGetComponent(out GrabbableObject prop))
            {
                LethalMin.Logger.LogWarning("Failed to get GrabbableObject component from object");
                return;
            }
            PikminItemSettings settings = prop.GetComponent<PikminItemSettings>();
            Item? Iprops = null;
            if (!EnemyItems.ContainsKey(enemy.enemyType))
            {
                Iprops = (Item)ScriptableObject.CreateInstance(typeof(Item));
                Iprops.name = $"(LethalMin_EnemyItem){enemy.enemyType.enemyName}";
                EnemyItems.Add(enemy.enemyType, Iprops);
            }
            else
            {
                Iprops = EnemyItems[enemy.enemyType];
            }
            if (Iprops == null)
            {
                LethalMin.Logger.LogError($"Failed to create Item instance for enemy conversion Enemy: {enemy.gameObject.name}");
                return;
            }

            LethalMin.Logger.LogInfo("Creating item node on enemy body " + enemy.gameObject.name);

            enemy.moveTowardsDestination = false;
            enemy.movingTowardsTargetPlayer = false;
            enemy.updatePositionThreshold = 9000;
            enemy.syncMovementSpeed = 0f;
            enemy.inSpecialAnimation = true;

            Iprops.restingRotation = enemy.transform.rotation.eulerAngles;
            Iprops.itemName = enemy.enemyType.enemyName;

            if (enemy.enemyType.canDie && EnemyAIPatch.EnemyHPs[enemy.enemyType] > 0f)
            {
                if (EnemyAIPatch.EnemyHPs[enemy.enemyType] <= 1f)
                {
                    Iprops.weight = 0;
                }
                else
                {
                    Iprops.weight = EnemyAIPatch.EnemyHPs[enemy.enemyType] * 0.4f;
                }
            }
            else
            {
                Iprops.weight = Mathf.Clamp((float)(enemy.enemyType.PowerLevel * 0.6), 1f, 5f);
            }
            prop.itemProperties = Iprops;
            prop.gameObject.name = $"EnemyItem_{enemy.enemyType.enemyName}";

            Dictionary<string, int> EnemyWeightsMap = new Dictionary<string, int>
            {
                { "Water Wraith", 100 },
                { "Baboon hawk", 12 },
                { "Maneater", 20 },
                { "Centipede", 5 },
                { "Clay Surgeon", 24 },
                { "Crawler", 20 },
                { "Manticoil", 2 },
                { "Tulip Snake", 3 },
                { "Flowerman", 20 },
                { "ForestGiant", 50 },
                { "GiantKiwi", 83 },
                { "Hoarding bug", 7 },
                { "Masked", 4 },
                { "MouthDog", 30 },
                { "Nutcracker", 27 },
                { "RadMech", 250 },
                { "Bunker Spider", 21 },
                { "Earth Leviathan", 1000 },
                { "Bush Wolf", 37 }
            };

            settings.CanProduceSprouts = true;
            settings.OverrideGrabPostionColider = enemy.GetComponentInChildren<Collider>();
            settings.OverrideGrabbableToEnemeis = true;
            settings.ExtraRenderers = enemy.GetComponentsInChildren<Renderer>().ToList();
            settings.CarryStrength = EnemyWeightsMap.ContainsKey(enemy.enemyType.enemyName) ? EnemyWeightsMap[enemy.enemyType.enemyName] :
            PikUtils.CalculatePikminItemWeight(Iprops);
            settings.SproutsToSpawn = settings.CarryStrength;

            PikminItem itm = prop.GetComponentInChildren<PikminItem>();
            EnemyGrabbableObject ego = enemy.gameObject.AddComponent<EnemyGrabbableObject>();
            itm.hackEnemyGrabbableObject = ego;
            ego.grabbableObject = prop;
            itm.Initalize();

            ConvertedAIs.Add(enemy); // Add to the converted AIs list for tracking
        }
        #endregion






        #region Pikmin
        [ServerRpc(RequireOwnership = false)]
        public void ApplyKnockbackServalPikminServerRpc(NetworkObjectReference[] Pikrefs, Vector3 direction, float force)
        {
            ApplyKnockbackServalPikminClientRpc(Pikrefs, direction, force);
        }

        [ClientRpc]
        public void ApplyKnockbackServalPikminClientRpc(NetworkObjectReference[] Pikrefs, Vector3 direction, float force)
        {
            foreach (NetworkObjectReference refPik in Pikrefs)
            {
                if (!refPik.TryGet(out NetworkObject pikObj))
                {
                    LethalMin.Logger.LogWarning("Failed to get pikmin object from reference when applying knockback sevral");
                    return;
                }
                if (!pikObj.TryGetComponent(out PikminAI pikmin))
                {
                    LethalMin.Logger.LogWarning("Failed to get PikminAI component from object when applying knockback sevral");
                    return;
                }
                pikmin.ApplyKnockBack(direction, force);
            }
        }

        #endregion





        #region Ice
        [ServerRpc]
        public void FreezeWaterAtIndexServerRpc(int index)
        {
            FreezeWaterAtIndexClientRpc(index);
        }
        [ClientRpc]
        public void FreezeWaterAtIndexClientRpc(int index)
        {
            FreezeWaterAtIndex(index);
        }
        public void FreezeWaterAtIndex(int index)
        {
            FreezeableWater water = FreezeableWaters[index];
            water.FreezeWater();
            LethalMin.Logger.LogInfo($"Freezing water at index {index} ({water.gameObject.name})");
        }


        [ServerRpc]
        public void UnfreezeWaterAtIndexServerRpc(int index)
        {
            UnfreezeWaterAtIndexClientRpc(index);
        }
        [ClientRpc]
        public void UnfreezeWaterAtIndexClientRpc(int index)
        {
            UnfreezeWaterAtIndex(index);
        }
        public void UnfreezeWaterAtIndex(int index)
        {
            FreezeableWater water = FreezeableWaters[index];
            water.UnfreezeWater();
            LethalMin.Logger.LogInfo($"Freezing water at index {index} ({water.gameObject.name})");
        }
        #endregion





        #region Glow
        public void SpawnLumiknulls()
        {
            if (StartOfRound.Instance.gameStats.daysSpent <= LethalMin.SpawnLumiknullAfterDays)
            {
                return;
            }

            if (!LethalMin.RegisteredPikminTypes.ContainsValue(LethalMin.assetBundle.LoadAsset<GameObject>
            ("Assets/LethalMin/Types/Glow Pikmin/Lumknul/Lumiknull.prefab").GetComponent<Lumiknull>().GlowPikminType))
            {
                return;
            }

            if (LethalMin.OnCompany)
            {
                LethalMin.Logger.LogInfo($"Cannot Lumiknull on company moon!");
                return;
            }

            if (!PikChecks.IsNavMeshOnMap())
            {
                LethalMin.Logger.LogWarning($"Current map does not have a navmesh, cannot spawn lumiknulls!");
                return;
            }

            if (Random.value > LethalMin.LumiknullSpawnChance)
            {
                return;
            }

            GameObject Prefab = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Types/Glow Pikmin/Lumknul/Lumiknull.prefab");

            Vector3 PositionBetweenMainAndShip = new Vector3(0, 0, 0);
            PositionBetweenMainAndShip = Vector3.Lerp(StartOfRound.Instance.elevatorTransform.position, RoundManager.FindMainEntrancePosition(false, true), 0.5f);

            List<GameObject> OutdoorSpawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
            OutdoorSpawnPoints.RemoveAll(sp => Vector3.Distance(sp.transform.position, StartOfRound.Instance.shipLandingPosition.position) < 20);
            OutdoorSpawnPoints.RemoveAll(sp => Vector3.Distance(sp.transform.position, RoundManager.FindMainEntrancePosition(false, true)) < 20);

            if (OutdoorSpawnPoints.Count == 0)
            {
                LethalMin.Logger.LogWarning("No valid outdoor spawn points found for lumiknulls using random!");
                OutdoorSpawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
            }

            Vector3 spawnPos = OutdoorSpawnPoints[Random.Range(0, OutdoorSpawnPoints.Count)].transform.position;

            GameObject LumiknullInst = Instantiate(Prefab, spawnPos, Quaternion.identity);
            LumiknullInst.transform.position = RoundManager.Instance.GetRandomNavMeshPositionInRadius(LumiknullInst.transform.position, 20);
            Lumiknull script = LumiknullInst.GetComponent<Lumiknull>();
            script.NetworkObject.Spawn();
            LethalMin.Logger.LogInfo($"Spawning Lumiknull at: {LumiknullInst.transform.position}");
        }
        #endregion





        #region Piklopedia

        public void AttemptScanNewType(int TypeID)
        {
            if (!ScannedPiklopediaIDs.Contains(TypeID))
            {
                ScanNewPikminTypeServerRpc(TypeID);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ScanNewPikminTypeServerRpc(int typeID)
        {
            if (ScannedPiklopediaIDs.Contains(typeID))
            {
                return;
            }
            HUDManager.Instance.DisplayGlobalNotification("New pikmin data sent to piklopedia!");
            ScannedPiklopediaIDs.Add(typeID);
            NewlyScannedPiklopediaIDs.Add(typeID);
            ScanNewPikminTypeClientRpc(typeID);
        }

        [ClientRpc]
        public void ScanNewPikminTypeClientRpc(int typeID)
        {
            if (IsServer || ScannedPiklopediaIDs.Contains(typeID))
            {
                return;
            }
            ScannedPiklopediaIDs.Add(typeID);
            NewlyScannedPiklopediaIDs.Add(typeID);
            HUDManager.Instance.DisplayGlobalNotification("New pikmin data sent to piklopedia!");
        }

        #endregion






        #region Pikmin Spawning
        public void SpawnPikmin(PikminType pikminType, int Quantity = -1,
         Vector3 spawnPosition = default(Vector3), Quaternion spawnRotation = default(Quaternion))
        {
            if (!PikChecks.IsNavMeshOnMap())
            {
                LethalMin.Logger.LogWarning($"Current map does not have a navmesh, cannot spawn pikmin!");
                return;
            }
            if (IsServer)
            {
                if (Quantity > 0)
                {
                    SpawnPikminOnServer(pikminType, Quantity, spawnPosition, spawnRotation);
                }
                else
                {
                    SpawnPikminOnServer(pikminType, spawnPosition, spawnRotation);
                }
            }
            else
            {
                SpawnPikminServerRpc(pikminType.PikminTypeID, Quantity, spawnPosition, spawnRotation);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnPikminServerRpc(int PikminID, int quantity, Vector3 SpawnPos, Quaternion SpawnRot)
        {
            PikminType type = LethalMin.GetPikminTypeByID(PikminID);

            if (type != null)
            {
                if (quantity > 0)
                {
                    SpawnPikminOnServer(type, quantity, SpawnPos, SpawnRot);
                }
                else
                {
                    SpawnPikminOnServer(type, SpawnPos, SpawnRot);
                }
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to get pikmin via ID {PikminID}");
            }
        }

        public PikminAI SpawnPikminOnServer(PikminType Ptype, Vector3 SpawnPos, Quaternion SpawnRot,
        PikminSpawnProps props = default, Onion OnionSpawningFrom = null!)
        {
            if (!PikChecks.IsNavMeshOnMap())
            {
                LethalMin.Logger.LogWarning($"Current map does not have a navmesh, cannot spawn pikmin!");
                return null!;
            }
            LethalMin.Logger.LogMessage($"Spawning: {Ptype.PikminName}");
            EnemyType enemyType = LethalMin.PikminEnemyType;

            PikminAI pikmin = Instantiate(enemyType.enemyPrefab, SpawnPos, SpawnRot).GetComponent<PikminAI>();
            PikminTypeResolver resolver = pikmin.GetComponent<PikminTypeResolver>();
            if (!props.Equals(default(PikminSpawnProps)) && LethalMin.GetLeaderViaID(props.PlayerID) != null)
            {
                pikmin.NetworkObject.SpawnWithOwnership(props.PlayerID);
            }
            else
            {
                pikmin.NetworkObject.Spawn();
            }

            if (OnionSpawningFrom == null)
            {
                if (props.Equals(default(PikminSpawnProps)))
                {
                    resolver.SyncSpawnClientRpc(Ptype.PikminTypeID);
                }
                else
                {
                    resolver.SyncSpawnClientRpc(Ptype.PikminTypeID, props);
                }
            }
            else
            {
                resolver.SyncSpawnFromOnionClientRpc(Ptype.PikminTypeID, Random.Range(0, OnionSpawningFrom.ClimbLinks.Count),
                OnionSpawningFrom.NetworkObject, props);
            }

            return gameObject.GetComponentInChildren<PikminAI>();
        }

        /// <summary>
        /// I don't recommend calling this method
        /// Use SpawnPikminIntervalled() instead
        /// </summary>
        /// <returns></returns>
        public PikminAI[] SpawnPikminOnServer(PikminType Ptype, int Quant, Vector3 SpawnPos, Quaternion SpawnRot)
        {
            if (!PikChecks.IsNavMeshOnMap())
            {
                LethalMin.Logger.LogWarning($"Current map does not have a navmesh, cannot spawn pikmin!");
                return null!;
            }
            LethalMin.Logger.LogMessage($"Spawning {Quant}: {Ptype.PikminName}");
            EnemyType enemyType = LethalMin.PikminEnemyType;
            PikminAI[] array = new PikminAI[Quant];

            for (int i = 0; i < Quant; i++)
            {
                PikminAI pikmin = Instantiate(enemyType.enemyPrefab, SpawnPos, SpawnRot).GetComponent<PikminAI>();
                pikmin.NetworkObject.Spawn(destroyWithScene: true);
                array[i] = pikmin;
            }
            return array;
        }

        IEnumerator SpawnPikminIntervalled(PikminType Ptype, int Quant, Vector3 SpawnPos, Quaternion SpawnRot, float interval = 0.1f)
        {
            if (!PikChecks.IsNavMeshOnMap())
            {
                LethalMin.Logger.LogWarning($"Current map does not have a navmesh, cannot spawn pikmin!");
                yield break;
            }
            for (int i = 0; i < Quant; i++)
            {
                yield return new WaitForSeconds(interval);
                SpawnPikminOnServer(Ptype, SpawnPos, SpawnRot);
            }
        }

        public Dictionary<PikminType, float> GetPikminSpawningOddsOnCurrentMoon(PikminOddsPositions props = PikminOddsPositions.Any)
        {
            Dictionary<PikminType, float> odds = new Dictionary<PikminType, float>();
            SelectableLevel curLevel = StartOfRound.Instance.currentLevel;

            foreach (PikminType type in LethalMin.RegisteredPikminTypes.Values)
            {
                switch (props)
                {
                    case PikminOddsPositions.Indoors:
                        if (!type.SpawnsIndoors)
                        {
                            continue;
                        }
                        break;
                    case PikminOddsPositions.Outdoors:
                        if (!type.SpawnsOutdoors)
                        {
                            continue;
                        }
                        break;
                    case PikminOddsPositions.Sprouts:
                        if (!type.SpawnsIndoors && !type.SpawnsOutdoors)
                        {
                            continue;
                        }
                        if (!type.SpawnsAsSprout)
                        {
                            continue;
                        }
                        break;
                    case PikminOddsPositions.NoSprouts:
                        if (!type.SpawnsIndoors && !type.SpawnsOutdoors)
                        {
                            continue;
                        }
                        if (type.SpawnsAsSprout)
                        {
                            continue;
                        }
                        break;
                }
                if (StartOfRound.Instance.gameStats.daysSpent <= type.SpawnAfterDay)
                {
                    continue;
                }
                float oddsValue = 0.5f;
                string cleanedPlanetName = System.Text.RegularExpressions.Regex.Replace(
                    curLevel.PlanetName,
                    "^\\d+",
                    "").Trim().ToLower();

                foreach (string MoonName in type.FavoredMoons)
                {
                    if (MoonName.ToLower().Contains(cleanedPlanetName))
                    {
                        oddsValue *= type.SpawnChanceMultiplier;
                    }
                }

                foreach (string MoonName in type.AvoidMoons)
                {
                    if (MoonName.ToLower().Contains(cleanedPlanetName))
                    {
                        oddsValue *= type.SpawnChanceDemultiplier;
                    }
                }

                if (LethalMin.IsDependencyLoaded("imabatby.lethallevelloader"))
                {
                    foreach (string MoonTag in type.FavoredMoonTags)
                    {
                        if (LLLIsOnMoonTag(MoonTag))
                        {
                            oddsValue *= type.SpawnChanceMultiplier;
                        }
                    }

                    foreach (string MoonTag in type.AvoidMoonTags)
                    {
                        if (LLLIsOnMoonTag(MoonTag))
                        {
                            oddsValue *= type.SpawnChanceDemultiplier;
                        }
                    }
                }

                foreach (string WeatherName in type.FavoredWeathers)
                {
                    if (curLevel.currentWeather.ToString().Contains(WeatherName))
                    {
                        oddsValue *= type.SpawnChanceMultiplier;
                    }
                }
                odds.Add(type, oddsValue);
            }

            return odds;
        }


        public bool LLLIsOnMoonTag(string tag)
        {
            foreach (ContentTag Ctag in LevelManager.CurrentExtendedLevel.ContentTags)
            {
                if (Ctag.contentTagName.ToLower().Contains(tag.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }


        public void DespawnPikmin(PikminAI pikmin)
        {
            if (IsServer)
            {
                pikmin.NetworkObject.Despawn(true);
            }
        }
        #endregion






        #region Onion Spawning
        public void SpawnOnionItems()
        {
            OnionsCollected = SaveManager.Load("collectedOnions", OnionsCollected);
            GameObject OnionItemPrefab = LethalMin.OnionItemPrefab;
            LethalMin.Logger.LogInfo($"Spawing Onion items");

            foreach (OnionType type in LethalMin.RegisteredOnionTypes.Values)
            {
                if (!type.SpawnsIndoors && !type.SpawnsOutdoors)
                {
                    continue;
                }

                if (OnionsCollected.Contains(type.OnionTypeID))
                {
                    continue;
                }

                if (StartOfRound.Instance.currentLevel.maxEnemyPowerCount < type.PreferedPowerLevel)
                {
                    continue;
                }

                if (Random.value > LethalMin.OnionSpawnChance.InternalValue * type.SpawnChanceMultiplier)
                {
                    continue;
                }

                if (StartOfRound.Instance.gameStats.daysSpent <= type.SpawnAfterDay)
                {
                    continue;
                }

                RandomScrapSpawn[] spawnPoints = FindObjectsOfType<RandomScrapSpawn>();

                if (spawnPoints == null || spawnPoints.Length == 0)
                {
                    break;
                }

                List<RandomScrapSpawn> useableSpawns = new List<RandomScrapSpawn>();

                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    RandomScrapSpawn spawn = spawnPoints[i];
                    if (!spawn.spawnUsed)
                    {
                        useableSpawns.Add(spawn);
                    }
                }

                LethalMin.Logger.LogDebug($"Spawning Onion {type.TypeName} with ID {type.OnionTypeID} at: {useableSpawns[0].transform.position}. Onions Collected: {PikUtils.ParseListToString(OnionsCollected)}");

                RandomScrapSpawn randomSpawn = useableSpawns[Random.Range(0, useableSpawns.Count)];
                OnionItem item = Instantiate(OnionItemPrefab, randomSpawn.transform.position, randomSpawn.transform.rotation).GetComponent<OnionItem>();
                randomSpawn.spawnUsed = true;
                item.NetworkObject.Spawn();
                item.DontChooseRandomType = true;
                item.InitalizeTypeClientRpc(type.OnionTypeID);
            }
        }

        IEnumerator SpawnOnionsAfterDelay()
        {
            yield return new WaitForSeconds(10f);
            SpawnOnions();
        }

        public void SpawnOnions()
        {
            if (!PikChecks.IsNavMeshOnMap())
            {
                LethalMin.Logger.LogWarning($"Current map does not have a navmesh, cannot spawn onions!");
                return;
            }

            OnionsCollected = SaveManager.Load("collectedOnions", OnionsCollected);
            int number = 0;
            List<int> IdsToSkip = new List<int>();
            LethalMin.Logger.LogInfo($"Spawing Onion");


            if (SaveManager.KeyExists("onionFusion"))
            {
                Dictionary<string, List<int>> onionFusionData = SaveManager.Load<Dictionary<string, List<int>>>("onionFusion");

                foreach (string key in onionFusionData.Keys)
                {
                    IdsToSkip.AddRange(onionFusionData[key]);
                    SpawnOnionOnServer(onionFusionData[key]);
                    number++;
                }
            }

            foreach (int ID in OnionsCollected)
            {
                if (IdsToSkip.Contains(ID))
                {
                    continue;
                }

                OnionType type = LethalMin.GetOnionTypeByID(ID);
                if (type == null)
                {
                    LethalMin.Logger.LogError($"Null ID: {ID}");
                    continue;
                }

                SpawnOnionOnServer(ID);
                number++;
            }

            if (number != 0)
                StartCoroutine(WaitForAllOnionsToInitalize(number));
        }

        public IEnumerator WaitForAllOnionsToInitalize(int AmmountToWaitFor)
        {
            LethalMin.Logger.LogInfo($"Waiting for {AmmountToWaitFor} onions to load... Total: {OnionsSpawnable.Count}");
            while (OnionsSpawnable.Count < AmmountToWaitFor)
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1f);
            LethalMin.Logger.LogInfo($"{AmmountToWaitFor}/{OnionsSpawnable.Count} onions loaded, loading data");
            LoadOnionData();
        }
        private Vector3 FindOpenSpaceForOnions(Transform ShipTransform)
        {
            Vector3 shipPos = ShipTransform.position;

            // Find all outside AI nodes
            GameObject[] outsideNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");

            if (outsideNodes == null || outsideNodes.Length == 0)
            {
                LethalMin.Logger.LogWarning("No OutsideAINodes found, falling back to default spawn logic");
                return shipPos + Vector3.forward * 20f;
            }

            // Find closest suitable node to ship
            GameObject closestNode = null!;
            float closestDistance = float.MaxValue;

            // Get ship's forward direction
            Vector3 shipForward = ShipTransform.forward;

            foreach (GameObject node in outsideNodes)
            {
                Vector3 directionToNode = (node.transform.position - shipPos).normalized;
                float angleToNode = Vector3.Angle(shipForward, directionToNode);
                float distance = Vector3.Distance(shipPos, node.transform.position);

                if (node.transform.position.z < shipPos.z)
                {
                    distance *= 2f; // Apply penalty to nodes behind the ship
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = node;
                }
            }

            // Use the closest node position as center, but ensure there's ground below
            Vector3 nodePosition = closestNode.transform.position;

            // if (nodePosition != Vector3.zero)
            // {
            //     return nodePosition;
            // }

            if (NavMesh.SamplePosition(nodePosition, out NavMeshHit NMhit, 15, NavMesh.AllAreas))
            {
                return NMhit.position;
            }

            return nodePosition;
        }

        private List<Vector3> FindSpecificOnionSpawnPoints()
        {
            List<Vector3> specificSpawnPoints = new List<Vector3>();

            for (int i = 1; i < LethalMin.RegisteredOnionTypes.Count; i++) // Assuming we're looking for up to 3 spawn points
            {
                GameObject spawnPoint
                = GameObject.Find($"ONION_SPAWN_POINT_{i}");
                if (spawnPoint != null)
                {
                    specificSpawnPoints.Add(spawnPoint.transform.position);
                }
            }

            return specificSpawnPoints;
        }

        /// <summary>
        /// Used of individual onions
        /// </summary>
        /// <param name="OnionID"> </param>
        public void SpawnOnionOnServer(int OnionID)
        {
            Vector3 spawnCenter = FindOpenSpaceForOnions(ShipPosition);
            List<Vector3> specificSpawnPoints = FindSpecificOnionSpawnPoints();
            int length = FindObjectsOfType<BaseOnion>().Length;
            bool usingPreset = false;

            // Calculate position in circle formation around the center point
            float angle = 360f / (LethalMin.OnionTypes.Count + 1) * (OnionID + 1);
            float radius = 5f; // Reduced radius since we're using AI nodes

            Vector3 spawnPos = spawnCenter + Quaternion.Euler(0, angle, 0) * (Vector3.forward * radius);
            LethalMin.Logger.LogInfo($"Pos: {spawnPos}, Angle: {angle}, ID: {OnionID}");
            if (!PikUtils.IsOutOfRange(specificSpawnPoints, length))
            {
                usingPreset = true;
                spawnPos = specificSpawnPoints[length];
                LethalMin.Logger.LogInfo($"Custom Pos: {spawnCenter}, Angle: {angle}, ID: {OnionID}");
            }

            // if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 50f,
            //   StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            // {
            //     spawnPos = hit.point;
            // }

            if (!usingPreset && NavMesh.SamplePosition(spawnPos, out NavMeshHit NMhit, 50, NavMesh.AllAreas))
            {
                spawnPos = NMhit.position;
            }

            // Make onions face center point
            //Quaternion spawnRot = Quaternion.LookRotation((spawnCenter - spawnPos).normalized);
            Quaternion spawnRot = Quaternion.identity;

            GameObject OnionInstance = Instantiate(LethalMin.OnionPrefab, spawnPos, spawnRot);
            OnionInstance.GetComponent<Onion>().NetworkObject.Spawn();
            OnionInstance.GetComponent<Onion>().DontChooseRandomType = true;
            OnionInstance.GetComponent<Onion>().InitalizeTypeClientRpc(OnionID);
        }

        /// <summary>
        /// Used for fused onions
        /// </summary>
        /// <param name="OnionIDs"></param>
        public void SpawnOnionOnServer(List<int> OnionIDs)
        {
            Vector3 spawnCenter = FindOpenSpaceForOnions(ShipPosition);
            List<Vector3> specificSpawnPoints = FindSpecificOnionSpawnPoints();
            int length = GetComponents<BaseOnion>().Length;
            bool usingPreset = false;

            float angle = 360f / (LethalMin.OnionTypes.Count + 1) * (OnionIDs[0] + 1);
            float radius = 5f;

            Vector3 spawnPos = spawnCenter + Quaternion.Euler(0, angle, 0) * (Vector3.forward * radius);
            LethalMin.Logger.LogInfo($"Pos: {spawnPos}, Angle: {angle}, IDs: {PikUtils.ParseListToString(OnionIDs)}");
            if (!PikUtils.IsOutOfRange(specificSpawnPoints, length))
            {
                usingPreset = true;
                spawnPos = specificSpawnPoints[length];
                LethalMin.Logger.LogInfo($"Custom Pos: {spawnPos}, Angle: {angle}, IDs: {PikUtils.ParseListToString(OnionIDs)}");
            }

            if (!usingPreset && NavMesh.SamplePosition(spawnPos, out NavMeshHit NMhit, 50, NavMesh.AllAreas))
            {
                spawnPos = NMhit.position;
            }

            //Quaternion spawnRot = Quaternion.LookRotation((spawnCenter - spawnPos).normalized);
            Quaternion spawnRot = Quaternion.identity;

            GameObject OnionInstance = Instantiate(LethalMin.OnionPrefab, spawnPos, spawnRot);
            OnionInstance.GetComponent<Onion>().NetworkObject.Spawn();
            OnionInstance.GetComponent<Onion>().DontChooseRandomType = true;
            OnionInstance.GetComponent<Onion>().InitalizeTypeClientRpc(OnionIDs.ToArray());
        }

        [ServerRpc]
        public void RemoveShipPhaseOnionsServerRpc()
        {
            RemoveShipPhaseOnionsClientRpc();
        }
        [ClientRpc]
        public void RemoveShipPhaseOnionsClientRpc()
        {
            LethalMin.Logger.LogInfo($"Removing ship phase onions");
            foreach (ShipPhaseOnionContainer.ShipPhaseOnion spo in shipPhaseOnionContainer.shipPhaseOnions)
            {
                Destroy(spo.Instance);
            }
            shipPhaseOnionContainer.shipPhaseOnions.Clear();
        }

        [ServerRpc]
        public void SpawnShipPhaseOnionServerRpc(int OnionID)
        {
            SpawnShipPhaseOnionClientRpc(OnionID);
        }
        [ClientRpc]
        public void SpawnShipPhaseOnionClientRpc(int OnionID)
        {
            OnionType type = LethalMin.GetOnionTypeByID(OnionID);
            if (type == null)
            {
                LethalMin.Logger.LogError($"Null ID: {OnionID}");
                return;
            }

            GameObject obj = null!;
            if (type.OnionOverrideModelPrefab == null)
            {
                obj = Instantiate(LethalMin.DefultOnionMesh, shipPhaseOnionContainer.transform);
            }
            else
            {
                obj = Instantiate(type.OnionOverrideModelPrefab, shipPhaseOnionContainer.transform);
            }

            ShipPhaseOnionContainer.ShipPhaseOnion onion = new ShipPhaseOnionContainer.ShipPhaseOnion(type, obj);

            shipPhaseOnionContainer.shipPhaseOnions.Add(onion);

            shipPhaseOnionContainer.
            SwitchGeneration(PikUtils.ConvertCfgGenerationToPikminGeneration(LethalMin.OnionModelGeneration.InternalValue));
        }
        [ServerRpc]
        public void SpawnShipPhaseOnionServerRpc(int[] OnionID)
        {
            SpawnShipPhaseOnionClientRpc(OnionID);
        }
        [ClientRpc]
        public void SpawnShipPhaseOnionClientRpc(int[] OnionID)
        {
            List<OnionType> types = new List<OnionType>();
            foreach (int ID in OnionID)
            {
                OnionType type = LethalMin.GetOnionTypeByID(ID);
                if (type == null)
                {
                    LethalMin.Logger.LogError($"Null ID: {ID}");
                    continue;
                }
                types.Add(type);
            }
            if (types.Count == 0)
            {
                LethalMin.Logger.LogError($"No valid onions to spawn");
                return;
            }

            GameObject obj = null!;
            obj = Instantiate(LethalMin.DefultOnionMesh, shipPhaseOnionContainer.transform);

            ShipPhaseOnionContainer.ShipPhaseOnion onion = new ShipPhaseOnionContainer.ShipPhaseOnion(types, obj);

            shipPhaseOnionContainer.shipPhaseOnions.Add(onion);

            shipPhaseOnionContainer.
            SwitchGeneration(PikUtils.ConvertCfgGenerationToPikminGeneration(LethalMin.OnionModelGeneration.InternalValue));
        }
        #endregion






        #region Onion Fuseing
        public List<int> FuseOnions(OnionFuseRules rules, List<int> onionIDs)
        {
            if (!LethalMin.AllowOnionFusing)
            {
                return new List<int>();
            }
            List<int> OnionFusion = new List<int>();
            foreach (OnionType oni in rules.OnionsToFuse)
            {
                if (onionIDs.Contains(oni.OnionTypeID))
                {
                    OnionFusion.Add(oni.OnionTypeID);
                }
            }
            if (OnionFusion.Count == 0 || OnionFusion.Count == 1)
            {
                OnionFusion.Clear();
            }
            LethalMin.Logger.LogInfo($"Created Fusion: '{rules.name}' " +
            $"{PikUtils.ParseListToString(OnionFusion)} from: {PikUtils.ParseListToString(onionIDs)}");
            return OnionFusion;
        }
        #endregion






        #region Saving and Loading
        public static bool IsSaving = false;
        public List<int> OnionsCollected = new List<int>();

        public void LoadInitalData()
        {
            if (!IsServer) return;
            LethalMin.Logger.LogDebug($"---Loading LethalMin Data---");
            LethalMin.Logger.LogDebug($"SavePath: {SaveManager.settings.path}");
            if (SaveManager.KeyExists("pikminLeft"))
            {
                EndOfGameStats.PikminLeft = SaveManager.Load("pikminLeft", EndOfGameStats.PikminLeft);
                LethalMin.Logger.LogDebug($"Loaded {EndOfGameStats.PikminLeft} pikmin left from save file");
            }
            if (SaveManager.KeyExists("firedStats"))
            {
                FiredStats = SaveManager.Load("firedStats", FiredStats);
                LethalMin.Logger.LogDebug($"Loaded {FiredStats} fired stats from save file");
            }
            if (SaveManager.KeyExists("collectedOnions"))
            {
                OnionsCollected = SaveManager.Load("collectedOnions", OnionsCollected);
                LethalMin.Logger.LogDebug($"Loaded ({PikUtils.ParseListToString(OnionsCollected)}) onions from save file");
            }
            if (SaveManager.KeyExists("scannedTypes"))
            {
                ScannedPiklopediaIDs = SaveManager.Load("scannedTypes", ScannedPiklopediaIDs);
                LethalMin.Logger.LogDebug($"Loaded ({PikUtils.ParseListToString(ScannedPiklopediaIDs)}) scanned types from save file");
            }
            if (SaveManager.KeyExists("LeaflingPlayers"))
            {
                LeaflingPlayers = SaveManager.Load("LeaflingPlayers", LeaflingPlayers);
                LethalMin.Logger.LogDebug($"Loaded {LeaflingPlayers.Count} leafling players from save file");
            }

            LethalMin.Logger.LogDebug($"---Finished Loading LethalMin Data---");
        }

        public void SaveData()
        {
            if (!IsServer) return;
            LethalMin.Logger.LogMessage($"---Saving LethalMin Data---");

            LethalMin.Logger.LogDebug($"SavePath: {SaveManager.settings.path}");

            IsSaving = true;

            SaveManager.Save("pikminLeft", EndOfGameStats.PikminLeft);

            SaveManager.Save("collectedOnions", OnionsCollected);

            SaveManager.Save("firedStats", FiredStats);

            SaveManager.Save("scannedTypes", ScannedPiklopediaIDs);

            // Save persisted sprouts
            List<SproutData> sproutDataList = null!;
            if (SaveManager.KeyExists("persistentSprouts"))
            {
                sproutDataList = SaveManager.Load<List<SproutData>>("persistentSprouts");
                LethalMin.Logger.LogInfo($"Loaded {sproutDataList.Count} persistent sprouts from save file");
            }
            else
            {
                sproutDataList = new List<SproutData>();
                LethalMin.Logger.LogInfo("No persistent sprouts found in save file created new data");
            }
            // Save persisted sprouts B
            List<SproutData> sproutDataListTemp = new List<SproutData>(sproutDataList);
            int Dcounter = 0;
            for (int i = 0; i < sproutDataListTemp.Count; i++)
            {
                SproutData data = sproutDataListTemp[i];
                LethalMin.Logger.LogInfo($"data moon: {data.MoonSpawnedOn}, current moon: {StartOfRound.Instance.currentLevelID}");
                if (data.MoonSpawnedOn == StartOfRound.Instance.currentLevelID)
                {
                    sproutDataList.Remove(data);
                    Dcounter++;
                }
            }
            LethalMin.Logger.LogInfo($"Removed {Dcounter} persistent sprouts from load due to them already being spawned on the current moon");
            foreach (var sprout in FindObjectsOfType<Sprout>())
            {
                if (sprout.IsPersistant)
                {
                    sproutDataList.Add(new SproutData(sprout));
                }
            }
            // Save persisted sprouts C
            if (sproutDataList.Count > 0)
            {
                SaveManager.Save("persistentSprouts", sproutDataList);
                LethalMin.Logger.LogInfo($"Saved {sproutDataList.Count} persistent sprouts");
            }
            else
            {
                SaveManager.Save("persistentSprouts", sproutDataList);
                LethalMin.Logger.LogInfo("No persistent sprouts to save");
            }

            LeaflingPlayers = null!;
            if (SaveManager.KeyExists("LeaflingPlayers"))
            {
                LeaflingPlayers = SaveManager.Load<Dictionary<string, int>>("LeaflingPlayers");
                LethalMin.Logger.LogInfo($"Loaded {LeaflingPlayers.Count} leafling players from save file");
            }
            else
            {
                LeaflingPlayers = new Dictionary<string, int>();
            }
            foreach (Leader lead in FindObjectsOfType<Leader>())
            {
                if (!PikChecks.IsPlayerConnected(lead.Controller))
                {
                    continue;
                }

                if (lead.IsLeafling)
                {
                    LeaflingPlayers[lead.Controller.playerUsername] = lead.LeaflingType == null ? -1 : lead.LeaflingType.PikminTypeID;
                }
                else if (LeaflingPlayers.ContainsKey(lead.Controller.playerUsername))
                {
                    // Remove leafling player if they are no longer a leafling
                    LeaflingPlayers.Remove(lead.Controller.playerUsername);
                }
            }
            SaveManager.Save("LeaflingPlayers", LeaflingPlayers);
            if (LeaflingPlayers.Count > 0)
            {
                LethalMin.Logger.LogInfo($"Saved {LeaflingPlayers.Count} leafling players");
            }


            // Save Onion Pikmin
            Dictionary<string, List<PikminData>> onionPikminData = null!;
            if (SaveManager.KeyExists("onionPikmin"))
            {
                onionPikminData = SaveManager.Load<Dictionary<string, List<PikminData>>>("onionPikmin");
                LethalMin.Logger.LogInfo($"Loaded {onionPikminData.Count} onions from save file");
            }
            else
            {
                onionPikminData = new Dictionary<string, List<PikminData>>();
                LethalMin.Logger.LogInfo("No onions found in save file created new data");
            }
            // Save Onion Pikmin B
            foreach (Onion onion in FindObjectsOfType<Onion>())
            {
                if (onion.fusedTypes.Count == 0)
                {
                    LethalMin.Logger.LogInfo($"Saving {onion.onionType.TypeName} type ({onion.onionType.OnionTypeID})");
                    string onionKey = $"onion_{onion.onionType.OnionTypeID}";
                    List<PikminData> pikminList = new List<PikminData>();

                    pikminList.AddRange(onion.PikminInOnion);

                    if (pikminList.Count > 0)
                    {
                        onionPikminData[onionKey] = pikminList;
                    }
                }
                else
                {
                    LethalMin.Logger.LogInfo($"Saving {onion.fusedTypes.Count} fused types for {onion.onionType.TypeName}");
                    foreach (OnionType Otype in onion.fusedTypes)
                    {
                        LethalMin.Logger.LogInfo($"Saving {Otype.TypeName} fused type ({Otype.OnionTypeID})");
                        string onionKey = $"onion_{Otype.OnionTypeID}";
                        List<PikminData> pikminList = new List<PikminData>();

                        // Only add pikmin that match the current onion type
                        foreach (PikminData pikmin in onion.PikminInOnion)
                        {
                            if (pikmin.LastOnionID == Otype.OnionTypeID)
                            {
                                pikminList.Add(pikmin);
                            }
                        }

                        if (pikminList.Count > 0)
                        {
                            onionPikminData[onionKey] = pikminList;
                        }
                    }
                }
            }
            // Save Onion Pikmin C
            if (onionPikminData.Count > 0)
            {
                SaveManager.Save("onionPikmin", onionPikminData);
                LethalMin.Logger.LogInfo($"Saved pikmin data for {onionPikminData.Count} onions");
            }
            else
            {
                LethalMin.Logger.LogInfo("No onion pikmin data to save");
            }


            //Create and save fusions
            Dictionary<string, List<int>> onionFusionData = null!;
            if (SaveManager.KeyExists("onionFusion"))
            {
                onionFusionData = SaveManager.Load<Dictionary<string, List<int>>>("onionFusion");
                LethalMin.Logger.LogInfo($"Loaded {onionFusionData.Count} fusions from save file");
            }
            else
            {
                onionFusionData = new Dictionary<string, List<int>>();
                LethalMin.Logger.LogInfo("No fusions found in save file created new data");
            }
            //Create and save fusions B
            foreach (OnionFuseRules rules in LethalMin.RegisteredFuseRules.Values)
            {
                string fusionKey = $"fusion_{rules.FuseRulesTypeID}";
                List<int> ints = FuseOnions(rules, OnionsCollected);

                if (ints.Count > 0)
                {
                    onionFusionData[fusionKey] = ints;
                }
            }
            //Create and save fusions C
            if (onionFusionData.Count > 0)
            {
                SaveManager.Save("onionFusion", onionFusionData);
                LethalMin.Logger.LogInfo($"Saved pikmin data for {onionFusionData.Count} fusions");
            }
            else
            {
                LethalMin.Logger.LogInfo("No onion fusion data to save");
            }


            IsSaving = false;

            LethalMin.Logger.LogMessage($"---Finished Saving LethalMin Data---");
        }

        public IEnumerator LoadSproutData()
        {
            if (!IsServer) yield break;

            int counter = 0;

            // Load and spawn persistent sprouts
            if (SaveManager.KeyExists("persistentSprouts"))
            {
                List<SproutData> sproutDataList = SaveManager.Load<List<SproutData>>("persistentSprouts");
                foreach (var sproutData in sproutDataList)
                {
                    if (sproutData.MoonSpawnedOn != StartOfRound.Instance.currentLevelID)
                    {
                        continue;
                    }
                    GameObject sproutObj = Instantiate(LethalMin.SproutPrefab, sproutData.Position, sproutData.Rotaion);
                    Sprout sproutComponent = sproutObj.GetComponent<Sprout>();
                    sproutComponent.IsPersistant = true;
                    sproutComponent.NetworkObject.Spawn();
                    sproutComponent.InitalizeClientRpc(sproutData);
                    counter++;
                    yield return new WaitForSeconds(0.05f);
                }
                if (counter > 0)
                    LethalMin.Logger.LogInfo($"Loaded {counter} persistent sprouts");
            }
        }

        public void LoadOnionData()
        {
            if (!IsServer) return;

            // Load Onion Pikmin
            if (SaveManager.KeyExists("onionPikmin"))
            {
                Dictionary<string, List<PikminData>> onionPikminData =
                    SaveManager.Load<Dictionary<string, List<PikminData>>>("onionPikmin");

                // Define a reasonable batch size to avoid overflow
                const int batchSize = 50;
                int batchCount = 0;

                foreach (Onion onion in Onions)
                {
                    if (onion.fusedTypes.Count == 0)
                    {
                        string onionKey = $"onion_{onion.onionType.OnionTypeID}";
                        if (onionPikminData.ContainsKey(onionKey))
                        {
                            // Reset the onion first
                            onion.SetPikminClientRpc(new PikminData[0]);

                            // Split into batches and send each batch
                            List<PikminData> pikminList = onionPikminData[onionKey];
                            for (int i = 0; i < pikminList.Count; i += batchSize)
                            {
                                int count = Mathf.Min(batchSize, pikminList.Count - i);
                                PikminData[] batch = pikminList.GetRange(i, count).ToArray();
                                onion.AddPikminClientRpc(batch);
                                batchCount++;
                            }

                            LethalMin.Logger.LogInfo($"Restored {onionPikminData[onionKey].Count} pikmin to onion {onion.onionType.OnionTypeID} in {batchCount} batches");
                            batchCount = 0;
                        }
                    }
                    else
                    {
                        onion.SetPikminClientRpc(new PikminData[0]);
                        foreach (OnionType OnType in onion.fusedTypes)
                        {
                            string onionKey = $"onion_{OnType.OnionTypeID}";
                            if (onionPikminData.ContainsKey(onionKey))
                            {
                                // Split into batches and send each batch
                                List<PikminData> pikminList = onionPikminData[onionKey];
                                for (int i = 0; i < pikminList.Count; i += batchSize)
                                {
                                    int count = Mathf.Min(batchSize, pikminList.Count - i);
                                    PikminData[] batch = pikminList.GetRange(i, count).ToArray();
                                    onion.AddPikminClientRpc(batch);
                                    batchCount++;
                                }

                                LethalMin.Logger.LogInfo($"Restored {onionPikminData[onionKey].Count} pikmin to onion {onion.onionType.OnionTypeID} in {batchCount} batches");
                                batchCount = 0;
                            }
                        }
                    }
                }
            }
        }

        public void ClearSavedData()
        {
            if (!IsServer) return;
            if (LethalMin.DontPurgeAfterFire)
            {
                LethalMin.Logger.LogInfo("Skipping clearing saved Pikmin data due to DontPurgeAfterFire being enabled");
                return;
            }

            SaveManager.DeleteFile();

            ClearLocalDataClientRpc();
            LethalMin.Logger.LogInfo("Cleared all saved Pikmin data");
        }

        [ClientRpc]
        public void ClearLocalDataClientRpc()
        {
            Onions.Clear();
            OnionsCollected.Clear();
            LeaflingPlayers.Clear();
            ScannedPiklopediaIDs.Clear();
            NewlyScannedPiklopediaIDs.Clear();
            FiredStats = new PikminFiredStats();
            EndOfGameStats = new PikminEndOfGameStats();
        }
        #endregion






        #region Clean-Up
        public IEnumerator WaitToDespawnObjects()
        {
            yield return new WaitUntil(() => !IsSaving);

            DespawnOnions();
            DespawnSprouts();
            DespawnLumiknulls();
        }

        public void CleanUpExcessPikmin()
        {
            if (IsTooManyPikminOnMap)
            {
                int excessCount = PikminAIs.Count - LethalMin.MaxPikmin.InternalValue;
                int wildRemoved = 0;
                int IdleRemoved = 0;
                int WorkingRemoved = 0;
                int FollowingRemoved = 0;

                LethalMin.Logger.LogInfo($"Cleaning up {excessCount} excess pikmin ({PikminAIs.Count} total) on map...");

                List<PikminAI> PrioritizedPikmin = PikminAIs
                    .OrderBy(p => !p.IsWildPikmin)          // Wild pikmin first (true comes before false)
                    .ThenBy(p => p.currentBehaviourStateIndex != PikminAI.IDLE) // BehaviourStateIndex 0 (idle) next
                    .ThenBy(p => p.currentBehaviourStateIndex != PikminAI.FOLLOW) // BehaviourStateIndex 1 (follow) next
                    .ThenBy(p => p.currentBehaviourStateIndex != PikminAI.WORK) // BehaviourStateIndex 2 (work) next
                    .ToList();

                foreach (PikminAI pikmin in PrioritizedPikmin)
                {
                    if (!IsTooManyPikminOnMap)
                        break;

                    Onion? possibleOnion = Onion.GetOnionOfPikmin(pikmin);
                    if (possibleOnion != null)
                        possibleOnion.AddPikminClientRpc(pikmin.GetPikminData());

                    if (pikmin.IsWildPikmin)
                    {
                        wildRemoved++;
                    }
                    else if (pikmin.currentBehaviourStateIndex == PikminAI.IDLE) // Idle
                    {
                        IdleRemoved++;
                    }
                    else if (pikmin.currentBehaviourStateIndex == PikminAI.FOLLOW) // Following
                    {
                        FollowingRemoved++;
                    }
                    else if (pikmin.currentBehaviourStateIndex == PikminAI.WORK) // Working
                    {
                        WorkingRemoved++;
                    }

                    DespawnPikmin(pikmin);
                }

                LethalMin.Logger.LogInfo($"Finished cleaning up excess pikmin. Current count: {PikminAIs.Count}");
                LethalMin.Logger.LogInfo($"Wild Removed: {wildRemoved}\n Idle Removed: {IdleRemoved}\n" +
                    $"Following Removed: {FollowingRemoved}\n  Working Removed: {WorkingRemoved}");
            }
        }

        public void DespawnSprouts()
        {
            if (IsServer)
            {
                foreach (Sprout spr in FindObjectsOfType<Sprout>())
                {
                    spr.NetworkObject.Despawn(true);
                }
            }
        }

        public void DespawnOnions()
        {
            if (IsServer)
            {
                foreach (Onion oni in FindObjectsOfType<Onion>())
                {
                    if (oni.DontDespawnOnGameEnd)
                    {
                        continue;
                    }
                    oni.NetworkObject.Despawn(true);
                }
            }
            Onions.RemoveWhere(oni => oni == null || oni.NetworkObject == null || !oni.NetworkObject.IsSpawned);
        }

        public void DespawnLumiknulls()
        {
            if (IsServer)
            {
                foreach (Lumiknull lumiknull in FindObjectsOfType<Lumiknull>())
                {
                    lumiknull.NetworkObject.Despawn(true);
                }
            }
        }
        #endregion
    }
}
