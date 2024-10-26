// Replace these lines at the beginning of the file
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using System.IO;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using System;
using GameNetcodeStuff;
using LethalMin.Patches;
using LethalModDataLib.Events;
using LethalModDataLib.Features;
using LethalModDataLib.Helpers;


namespace LethalMin
{
    // Add this struct to represent player data
    public struct PlayerPikminData : INetworkSerializable
    {
        public ulong PlayerId;
        public int PikminRaised;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref PikminRaised);
        }
    }
    public class PikminManager : NetworkBehaviour
    {
        public static PikminManager Instance { get; private set; }

        public int PikminBorn, PikminKilled, PikminLived;

        public Dictionary<ulong, int> PikminRaisedPerPlayer = new Dictionary<ulong, int>();
        public int TotalPikminKilled = 0;
        public int TotalPikminLeftBehind = 0;
        DebugMenu DBM;
        SpawnPointInfo spawnInfo;
        public bool CachedSpawnPoints = false;
        public string SaveFilePath = Path.Combine(Application.persistentDataPath, "LethalMinSave.json");
        public bool IsSaving;
        public GameObject OnionPrefab;

        #region Initialization and Core Management

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                //DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            DBM = GetComponent<DebugMenu>();
        }
        void LateUpdate()
        {
            DBM.enabled = LethalMin.DebugMode;
            if (StartOfRound.Instance != null && StartOfRound.Instance.shipHasLanded)
            {
                if (PIOMTimer >= 0)
                {
                    PIOMTimer -= Time.deltaTime;
                }
                else
                {
                    RefreshPikminItemsInMapList();
                    PIOMTimer = LethalMin.ManagerRefreshRate;
                }
            }
            else
            {
                if (_currentPikminItemsInMap.Count > 0)
                {
                    _currentPikminItemsInMap.Clear();
                    PikminItemsExclusion.Clear();
                }
            }
        }
        public void OnGameStarted()
        {
            if (!IsServer) { return; }
            if (StartOfRound.Instance == null) { return; }
            if (StartOfRound.Instance.inShipPhase) { return; }
            if (LethalMin.IsUsingModLib())
            {
                LethalMin.Logger.LogMessage("Using ModLib, loading EZOnion data.");
                LoadEZOnionData();
            }
            else
            {
                SaveFilePath = Path.Combine(Application.persistentDataPath, $"{GetSaveFileName()}.json");
                LoadOnionData();
            }
            if (RoundManager.Instance.currentLevel.sceneName != "CompanyBuilding")
            {
                ResetCountersServerRpc();
                StartCoroutine(CacheOnionSpawnPoints());
                StartCoroutine(SpawnOnions());
                StartCoroutine(Spawn1());
                StartCoroutine(Spawn2());
            }
            else if (LethalMin.CanWalkAtCompany())
            {
                ResetCountersServerRpc();
                StartCoroutine(CacheOnionSpawnPoints());
                StartCoroutine(SpawnOnions());
            }
        }

        #endregion

        #region Spawning and Generation

        public void SpawnOnionItems()
        {
            if (!IsServer) return;
            Item OnionI = LethalMin.OnionItem;

            int maxOnions = LethalMin.RegisteredOnionTypes.Count;
            List<OnionType> availableTypes = LethalMin.SpawnableOnionTypes.Values.ToList();

            List<OnionType> spawnedTypes = new List<OnionType>();
            List<OnionType> availableTypesCopy = new List<OnionType>(availableTypes);

            // Iterate over the copy
            foreach (var item in availableTypesCopy)
            {
                if (CollectedOnions.Contains(item.OnionTypeID))
                {
                    availableTypes.Remove(item);
                }
            }

            // Get existing Onion and OnionItem instances
            Onion[] existingOnions = UnityEngine.Object.FindObjectsOfType<Onion>();
            OnionItem[] existingOnionItems = UnityEngine.Object.FindObjectsOfType<OnionItem>();

            // Update spawnedTypes based on existing Onions and OnionItems
            foreach (Onion onion in existingOnions)
            {
                spawnedTypes.Add(onion.type);
                availableTypes.Remove(onion.type);
            }
            foreach (OnionItem item in existingOnionItems)
            {
                spawnedTypes.Add(item.type);
                availableTypes.Remove(item.type);
            }
            LethalMin.Logger.LogInfo($"AvaibleTypes: \n{string.Join("\n", availableTypes)}");

            // Calculate how many new onion items to spawn
            int onionsToSpawn = Mathf.Min(maxOnions - existingOnions.Length - existingOnionItems.Length, availableTypes.Count);

            // Get spawn points
            RandomScrapSpawn[] spawnPoints = UnityEngine.Object.FindObjectsOfType<RandomScrapSpawn>();
            List<RandomScrapSpawn> usedSpawns = new List<RandomScrapSpawn>();

            for (int i = 0; i < onionsToSpawn; i++)
            {
                if (availableTypes.Count == 0)
                {
                    LethalMin.Logger.LogMessage("No aviable onion types to spawn lol");
                    break;
                }
                // Choose a random available type
                OnionType chosenType = availableTypes[UnityEngine.Random.Range(0, availableTypes.Count)];

                // 45% chance to spawn this onion
                if (UnityEngine.Random.value > LethalMin.OnionSpawnChanceValue)
                {
                    LethalMin.Logger.LogInfo($"Skipped spawning {chosenType} onion cuz u got unlucky.");
                    availableTypes.Remove(chosenType);
                    continue;
                }


                // Find a suitable spawn point
                List<RandomScrapSpawn> availableSpawnPoints = spawnPoints.Where(sp => !usedSpawns.Contains(sp)).ToList();
                if (availableSpawnPoints.Count == 0)
                {
                    LethalMin.Logger.LogWarning("No more available spawn points for OnionItems.");
                    break;
                }

                RandomScrapSpawn chosenSpawnPoint = availableSpawnPoints[UnityEngine.Random.Range(0, availableSpawnPoints.Count)];
                usedSpawns.Add(chosenSpawnPoint);

                // Get spawn position
                Vector3 spawnPosition;
                if (chosenSpawnPoint.spawnedItemsCopyPosition)
                {
                    spawnPosition = chosenSpawnPoint.transform.position;
                }
                else
                {
                    spawnPosition = GetRandomNavMeshPositionInBoxPredictable(
                        chosenSpawnPoint.transform.position,
                        chosenSpawnPoint.itemSpawnRange,
                        RoundManager.Instance.navHit,
                        RoundManager.Instance.AnomalyRandom
                    );
                }

                // Spawn the OnionItem
                GameObject onionItemObj = Instantiate(OnionI.spawnPrefab, spawnPosition, Quaternion.identity);
                OnionItem onionItem = onionItemObj.GetComponent<OnionItem>();

                if (onionItem != null)
                {
                    NetworkObject networkObject = onionItem.GetComponent<NetworkObject>();
                    if (networkObject != null)
                    {
                        networkObject.Spawn();
                        onionItem.Initialize(chosenType);
                    }
                    else
                    {
                        LethalMin.Logger.LogError("NetworkObject component not found on OnionItem prefab.");
                    }
                }
                else
                {
                    LethalMin.Logger.LogError("OnionItem component not found on spawned object.");
                }

                // Update lists
                spawnedTypes.Add(chosenType);
                availableTypes.Remove(chosenType);
                LethalMin.Logger.LogInfo($"Spawned {chosenType} Onion Item at {spawnPosition}");
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            // Implement logic to find a suitable spawn position
            // This could involve using NavMesh, raycasts, or predefined spawn points
            // For now, we'll just return a random position as a placeholder
            return new Vector3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f));
        }

        IEnumerator CacheOnionSpawnPoints()
        {
            CachedSpawnPoints = false;
            spawnInfo = new SpawnPointInfo();
            yield return new WaitUntil(() => StartOfRound.Instance.fullyLoadedPlayers.Count >= GameNetworkManager.Instance.connectedPlayers);
            yield return new WaitUntil(() => StartOfRound.Instance.shipHasLanded);
            LethalMin.Logger.LogInfo("Caching onion spawn points");
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            Vector3 shipPosition = StartOfRound.Instance.elevatorTransform.position;
            List<OnionType> availableTypes = LethalMin.SpawnableOnionTypes.Values.ToList();

            spawnInfo = new SpawnPointInfo()
            {
                NearbyNodes = FindOnionSpawnPositions(spawnPoints, shipPosition, availableTypes.Count),
            };

            CachedSpawnPoints = true;

            LethalMin.Logger.LogInfo($"Cached {spawnInfo.NearbyNodes.Count} onion spawn points");
        }

        IEnumerator Spawn1()
        {
            if (!IsServer) { yield return null; }
            if (LethalMin.OutdoorTypes.Count == 0) { yield return null; }
            LethalMin.Logger.LogInfo("Waiting for ship to land before doing outdoor spawns");
            yield return new WaitUntil(() => StartOfRound.Instance.fullyLoadedPlayers.Count >= GameNetworkManager.Instance.connectedPlayers);
            yield return new WaitUntil(() => RoundManager.Instance.dungeonCompletedGenerating);

            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            LethalMin.Logger.LogInfo($"Found {spawnPoints.Length} outdor spawns");

            foreach (GameObject spawnPoint in spawnPoints)
            {
                // Random chance to spawn a sprout (e.g., 35% chance)
                if (UnityEngine.Random.value <= LethalMin.OutdoorSpawnChanceValue)
                {
                    PikminType Type = LethalMin.GetRandomOutdoorPikminType();
                    if (Type.SpawnsAsSprout && Type.SpawnsOutdoors)
                    {
                        Transform pos2 = spawnPoint.transform;
                        GameObject SproutInstance2 = Instantiate(LethalMin.sproutPrefab, pos2.position, pos2.rotation);
                        Sprout SproteScript2 = SproutInstance2.GetComponent<Sprout>();
                        SproteScript2.NetworkObject.Spawn();
                        PikminType pikminType = DeterminePikminType(RoundManager.Instance.currentLevel);
                        SproteScript2.InitalizeTypeClientRpc(pikminType.PikminTypeID);
                    }
                    else if (Type.SpawnsOutdoors)
                    {
                        Transform pos = spawnPoint.transform;
                        GameObject SproutInstance = Instantiate(LethalMin.pikminPrefab, pos.position, pos.rotation);
                        PikminAI SproteScript = SproutInstance.GetComponent<PikminAI>();
                        SproteScript.isOutside = false;
                        SproteScript.NetworkObject.Spawn();
                        SpawnPikminClientRpc(new NetworkObjectReference(SproteScript.NetworkObject));
                        CreatePikminClientRPC(new NetworkObjectReference(SproteScript.NetworkObject), Type.PikminTypeID, true);
                    }
                }
            }
            yield return new WaitForSeconds(0.5f); // Short delay to ensure all spawns are complete
            CleanupExcessPikmin();
        }

        IEnumerator Spawn2()
        {
            if (!IsServer) { yield return null; }
            if (LethalMin.IndoorTypes.Count == 0) { yield return null; }
            LethalMin.Logger.LogInfo("Waiting for ship to land before doing indoor spawns");
            yield return new WaitUntil(() => StartOfRound.Instance.fullyLoadedPlayers.Count >= GameNetworkManager.Instance.connectedPlayers);
            yield return new WaitUntil(() => RoundManager.Instance.dungeonCompletedGenerating);

            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("AINode");
            LethalMin.Logger.LogInfo($"Found {spawnPoints.Length} indor spawns");

            foreach (GameObject spawnPoint in spawnPoints)
            {
                // Random chance to spawn a sprout (e.g., 5% chance)
                if (UnityEngine.Random.value <= LethalMin.IndoorSpawnChanceValue)
                {
                    PikminType Type = LethalMin.GetRandomIndoorPikminType();
                    if (Type.SpawnsAsSprout && Type.SpawnsIndoors)
                    {
                        Transform pos2 = spawnPoint.transform;
                        GameObject SproutInstance2 = Instantiate(LethalMin.sproutPrefab, pos2.position, pos2.rotation);
                        Sprout SproteScript2 = SproutInstance2.GetComponent<Sprout>();
                        SproteScript2.NetworkObject.Spawn();
                        PikminType pikminType = DeterminePikminType(RoundManager.Instance.currentLevel);
                        SproteScript2.InitalizeTypeClientRpc(pikminType.PikminTypeID);
                    }
                    else if (Type.SpawnsIndoors)
                    {
                        Transform pos = spawnPoint.transform;
                        GameObject SproutInstance = Instantiate(LethalMin.pikminPrefab, pos.position, pos.rotation);
                        PikminAI SproteScript = SproutInstance.GetComponent<PikminAI>();
                        SproteScript.isOutside = false;
                        SproteScript.NetworkObject.Spawn();
                        SpawnPikminClientRpc(new NetworkObjectReference(SproteScript.NetworkObject));
                        CreatePikminClientRPC(new NetworkObjectReference(SproteScript.NetworkObject), Type.PikminTypeID, false);
                    }
                }
            }
            yield return new WaitForSeconds(0.5f); // Short delay to ensure all spawns are complete
            CleanupExcessPikmin();
        }

        private PikminType DeterminePikminType(SelectableLevel level)
        {
            Dictionary<PikminType, float> typeWeights = new Dictionary<PikminType, float>();

            // Initialize weights for all outdoor types
            foreach (var type in LethalMin.SproutTypes.Values)
            {
                if (LethalMin.AllowSpawnMultiplier)
                {
                    typeWeights[type] = 1f * type.SpawnChanceMultiplier;
                }
                else
                {
                    typeWeights[type] = 1f;
                }
            }

            // Check for fire-related enemies or hazards
            bool hasFireHazards = level.OutsideEnemies.Any(e => e.enemyType.enemyName.ToLower().Contains("old birds"));
            if (hasFireHazards)
            {
                var fireResistantTypes = LethalMin.SproutTypes.Values.Where(t => t.IsResistantToFire);
                foreach (var type in fireResistantTypes)
                {
                    typeWeights[type] *= 2f; // Increase chance for fire-resistant Pikmin
                }
            }

            // Check for water-related hazards
            bool hasWaterHazards = level.spawnableMapObjects.Any(o => o.prefabToSpawn.name.ToLower().Contains("water")) ||
                level.currentWeather == LevelWeatherType.Rainy || level.currentWeather == LevelWeatherType.Flooded;
            if (hasWaterHazards)
            {
                var waterResistantTypes = LethalMin.SproutTypes.Values.Where(t => t.IsResistantToWater);
                foreach (var type in waterResistantTypes)
                {
                    typeWeights[type] *= 2f; // Increase chance for water-resistant Pikmin
                }
            }

            // Check for electric-related enemies or hazards
            bool hasElectricHazards = level.currentWeather == LevelWeatherType.Stormy;
            if (hasElectricHazards)
            {
                var electricResistantTypes = LethalMin.SproutTypes.Values.Where(t => t.IsResistantToElectricity);
                foreach (var type in electricResistantTypes)
                {
                    typeWeights[type] *= 2f; // Increase chance for electricity-resistant Pikmin
                }
            }

            // Calculate total weight
            float totalWeight = typeWeights.Values.Sum();

            // Generate a random value
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);

            // Select the Pikmin type based on the weights
            float cumulativeWeight = 0f;
            foreach (var kvp in typeWeights)
            {
                cumulativeWeight += kvp.Value;
                if (randomValue <= cumulativeWeight)
                {
                    return kvp.Key;
                }
            }

            // Fallback (should never reach here, but just in case)
            return LethalMin.OutdoorTypes[UnityEngine.Random.Range(0, LethalMin.OutdoorTypes.Count)];
        }

        private void CleanupExcessPikmin()
        {
            if (!IsServer) return;

            PikminAI[] allPikmin = UnityEngine.Object.FindObjectsOfType<PikminAI>();
            int excessCount = allPikmin.Length - LethalMin.MaxMinValue;

            if (excessCount > 0)
            {
                LethalMin.Logger.LogInfo($"Cleaning up {excessCount} excess Pikmin");

                // Sort Pikmin by their growth stage (assuming lower number means less mature)
                var sortedPikmin = allPikmin.OrderBy(p => p.GrowStage).ToList();

                for (int i = 0; i < excessCount; i++)
                {
                    if (i < sortedPikmin.Count)
                    {
                        PikminAI pikmin = sortedPikmin[i];
                        if (pikmin.NetworkObject != null && pikmin.NetworkObject.IsSpawned)
                        {
                            RoundManager.Instance.DespawnEnemyOnServer(new NetworkObjectReference(pikmin.NetworkObject));
                        }
                        UnityEngine.Object.Destroy(pikmin.gameObject);
                    }
                }
            }
            StartCoroutine(CleanupExcessPikminCoroutine());
        }

        private IEnumerator CleanupExcessPikminCoroutine()
        {
            while (true)
            {
                PikminAI[] allPikmin = UnityEngine.Object.FindObjectsOfType<PikminAI>();
                int excessCount = allPikmin.Length - LethalMin.MaxMinValue; ;

                if (excessCount > 0)
                {
                    LethalMin.Logger.LogInfo($"Cleaning up {excessCount} excess Pikmin");

                    // Sort Pikmin by their growth stage (assuming lower number means less mature)
                    var sortedPikmin = allPikmin.OrderBy(p => p.GrowStage).ToList();

                    for (int i = 0; i < excessCount; i++)
                    {
                        if (i < sortedPikmin.Count)
                        {
                            PikminAI pikmin = sortedPikmin[i];
                            if (pikmin.TargetOnion != null)
                            {
                                // Send pikmin to its onion
                                SendPikminToOnion(pikmin);
                            }
                            else
                            {
                                // If no onion, destroy the pikmin
                                if (pikmin.NetworkObject != null && pikmin.NetworkObject.IsSpawned)
                                {
                                    DespawnPikminClientRpc(new NetworkObjectReference(pikmin.NetworkObject));
                                }
                                UnityEngine.Object.Destroy(pikmin.gameObject);
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(3f); // Wait for 1 second before next check
            }
        }

        [ClientRpc]
        public void CreatePikminClientRPC(NetworkObjectReference network1, int type, bool IsOutside)
        {
            LethalMin.Logger.LogInfo("Creating Pikmin");
            network1.TryGet(out NetworkObject PikObj);
            PikminAI script = PikObj.GetComponent<PikminAI>();
            if (script == null) { return; }
            script.PreDefinedType = true;
            script.PminType = LethalMin.GetPikminTypeById(type);
            script.isOutside = IsOutside;
            StartCoroutine(waitForInitalizePik(PikObj.GetComponent<PikminAI>()));
        }

        IEnumerator waitForInitalizePik(PikminAI pikminAI)
        {
            while (!pikminAI.HasInitalized)
            {
                yield return new WaitForSeconds(0.1f);
            }
            pikminAI.isOutside = false;
            LethalMin.Logger.LogInfo($"Pikmin {pikminAI.uniqueDebugId} has initalized {(pikminAI.isOutside ? "outside" : "inside")}");
        }

        #endregion

        #region Pikmin and Item Management
        float PIOMTimer;
        private static List<GameObject> _currentPikminItemsInMap = new List<GameObject>();
        private static List<GameObject> _nextPikminItemsInMap = new List<GameObject>();
        private static List<GameObject> _currentNonPikminEnemies = new List<GameObject>();
        private static List<GameObject> _nextNonPikminEnemies = new List<GameObject>();
        private static List<GameObject> _currentPikminEnemies = new List<GameObject>();
        private static List<GameObject> _nextPikminEnemies = new List<GameObject>();
        private static object _listLock = new object();
        public static List<GameObject> PikminItemsExclusion = new List<GameObject>();
        public static Onion[] _currentOnions = new Onion[0];
        public static VehicleController[] _currentCars = new VehicleController[0];

        public static void RefreshPikminItemsInMapList()
        {
            _nextPikminItemsInMap.Clear();
            _nextNonPikminEnemies.Clear();
            _nextPikminEnemies.Clear();

            // Refresh Pikmin Items
            PikminItem[] allGrabbables = UnityEngine.Object.FindObjectsOfType<PikminItem>();
            foreach (PikminItem grabbable in allGrabbables)
            {
                if (grabbable == null || grabbable.Root == null) continue;
                if (PikminItemsExclusion.Contains(grabbable.gameObject)) continue;
                if (!grabbable.Root.deactivated)
                {
                    _nextPikminItemsInMap.Add(grabbable.gameObject);
                }
            }

            // Refresh Non-Pikmin Enemies
            EnemyAI[] allEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            foreach (EnemyAI enemy in allEnemies)
            {
                if (enemy == null) continue;
                if (!enemy.enemyType.canDie) continue;
                if (enemy.GetComponent<PikminAI>() == null) // Check if it doesn't have PikminAI component
                {
                    _nextNonPikminEnemies.Add(enemy.gameObject);
                }
            }

            // Refresh Pikmin Enemies
            foreach (EnemyAI enemy in allEnemies)
            {
                if (enemy == null) continue;
                if (enemy.GetComponent<PikminAI>() != null) // Check if it have PikminAI component
                {
                    _nextPikminEnemies.Add(enemy.gameObject);
                }
            }

            // Refresh Onions
            Onion[] allOnions = UnityEngine.Object.FindObjectsOfType<Onion>();
            if (_currentOnions.Length != allOnions.Length)
            {
                LethalMin.Logger.LogInfo($"Onion count changed from {_currentOnions.Length} to {allOnions.Length}");
                _currentOnions = allOnions;
                foreach (var item in FindObjectsOfType<PikminAI>())
                {
                    if (item.TargetOnion == null)
                        item.CheckForOnion(allOnions);
                }
            }

            // Refresh Cars
            if (LethalMin.TargetCar)
            {
                VehicleController[] allCars = UnityEngine.Object.FindObjectsOfType<VehicleController>();
                if (_currentCars.Length != allCars.Length)
                {
                    LethalMin.Logger.LogInfo($"VehicleController count changed from {_currentCars.Length} to {allCars.Length}");
                    _currentCars = allCars;
                }
            }

            lock (_listLock)
            {
                var tempItems = _currentPikminItemsInMap;
                _currentPikminItemsInMap = _nextPikminItemsInMap;
                _nextPikminItemsInMap = tempItems;

                var tempEnemies = _currentNonPikminEnemies;
                _currentNonPikminEnemies = _nextNonPikminEnemies;
                _nextNonPikminEnemies = tempEnemies;

                var tempPikminEnemies = _currentPikminEnemies;
                _currentPikminEnemies = _nextPikminEnemies;
                _nextPikminEnemies = tempPikminEnemies;
            }

            //LethalMin.Logger.LogInfo($"Refreshed PikminItemsInMap. Current count: {_currentPikminItemsInMap.Count}");
            //LethalMin.Logger.LogInfo($"Refreshed NonPikminEnemies. Current count: {_currentNonPikminEnemies.Count}");

            // Call GetPikminItemsInMapList for all Pikmin
            PikminAI.GetPikminItemsInMapList();
        }

        public static List<GameObject> GetPikminItemsInMap()
        {
            lock (_listLock)
            {
                return new List<GameObject>(_currentPikminItemsInMap);
            }
        }

        public static List<GameObject> GetNonPikminEnemies()
        {
            lock (_listLock)
            {
                return new List<GameObject>(_currentNonPikminEnemies);
            }
        }

        public static List<GameObject> GetPikminEnemies()
        {
            lock (_listLock)
            {
                return new List<GameObject>(_currentPikminEnemies);
            }
        }

        public void SyncAllPikminItems()
        {
            if (!IsServer) { return; }
            PikminItem[] allPikminItems = UnityEngine.Object.FindObjectsOfType<PikminItem>();
            foreach (PikminItem item in allPikminItems)
            {
                item.SyncRoot();
            }
        }

        public void SyncAllWhistles()
        {
            if (!IsServer) { return; }
            WhistleItem[] allWhistleItems = UnityEngine.Object.FindObjectsOfType<WhistleItem>();
            foreach (WhistleItem item in allWhistleItems)
            {
                item.SyncZone();
            }
        }

        #endregion

        #region Player and Vehicle Interaction

        public static bool IsPlayerInCar(PlayerControllerB player, out VehicleController car)
        {
            car = null!;
            if (_currentCars.Length == 1)
            {
                if (_currentCars[0].currentDriver == player)
                {
                    car = _currentCars[0];
                    return true;
                }
            }
            foreach (var item in _currentCars)
            {
                if (item.currentDriver == player)
                {
                    car = item;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Counters and Statistics
        private int pikminInDangerCount = 0;

        [ServerRpc(RequireOwnership = false)]
        public void UpdatePikminInDangerCountServerRpc()
        {
            if (!IsServer) return;
            pikminInDangerCount = CountPikminInDanger();
            UpdatePikminInDangerCountClientRpc(pikminInDangerCount);
        }

        [ClientRpc]
        private void UpdatePikminInDangerCountClientRpc(int count)
        {
            if (IsServer) return;
            pikminInDangerCount = count;
        }

        public int GetPikminInDangerCount()
        {
            return pikminInDangerCount;
        }

        public static int CountPikminInDanger()
        {
            PikminAI[] allPikmin = UnityEngine.Object.FindObjectsOfType<PikminAI>();
            Vector3 shipPosition = StartOfRound.Instance.elevatorTransform.position;
            int dangerCount = 0;
            int withonion = 0;

            foreach (PikminAI pikmin in allPikmin)
            {
                if (pikmin.TargetOnion == null) continue;
                withonion++;
                bool isNearShip = Vector3.Distance(pikmin.transform.position, shipPosition) <= PikminManager.Instance.ShipPickupRange;
                bool isNearOnion = pikmin.TargetOnion != null && Vector3.Distance(pikmin.transform.position, pikmin.TargetOnion.transform.position) <= PikminManager.Instance.OnionPickupRange;

                if (!isNearShip && !isNearOnion)
                {
                    dangerCount++;
                }
            }

            return dangerCount;
        }

        public void IncrementPikminRaised(ulong playerId)
        {
            if (!PikminRaisedPerPlayer.ContainsKey(playerId))
            {
                PikminRaisedPerPlayer[playerId] = 0;
            }
            PikminRaisedPerPlayer[playerId]++;
        }

        public void IncrementPikminKilled()
        {
            TotalPikminKilled++;
        }

        public void SetPikminLeftBehind(int count)
        {
            TotalPikminLeftBehind = count;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResetCountersServerRpc()
        {
            ResetCounters();
            ResetCountersClientRpc();
        }

        [ClientRpc]
        private void ResetCountersClientRpc()
        {
            ResetCounters();
        }

        private void ResetCounters()
        {
            PikminRaisedPerPlayer.Clear();
            TotalPikminKilled = 0;
            TotalPikminLeftBehind = 0;
        }

        [ClientRpc]
        public void SyncEndgameDataClientRpc(int totalKilled, int totalLeftBehind, int currentTotalPikminCount, PlayerPikminData[] playerDataArray, ClientRpcParams clientRpcParams = default)
        {
            TotalPikminKilled = totalKilled;
            TotalPikminLeftBehind = totalLeftBehind;

            // Convert the array back to a dictionary
            PikminRaisedPerPlayer = playerDataArray.ToDictionary(data => data.PlayerId, data => data.PikminRaised);

            LethalMin.Logger.LogInfo($"Synced endgame data: Killed: {TotalPikminKilled}, Left Behind: {TotalPikminLeftBehind}, Total: {currentTotalPikminCount}");
            LethalMin.Logger.LogInfo($"Pikmin raised per player: {string.Join(", ", PikminRaisedPerPlayer.Select(kvp => $"Player {kvp.Key}: {kvp.Value}"))}");
        }


        [ServerRpc(RequireOwnership = false)]
        public void SyncEndgameDataServerRpc()
        {
            if (!IsServer) return;

            int currentTotalPikminCount = 0;
            Onion[] onions = UnityEngine.Object.FindObjectsOfType<Onion>();
            foreach (Onion onion in onions)
            {
                currentTotalPikminCount += onion.GetPikminCount();
            }

            // Convert the dictionary to an array of PlayerPikminData
            PlayerPikminData[] playerDataArray = PikminRaisedPerPlayer
                .Select(kvp => new PlayerPikminData { PlayerId = kvp.Key, PikminRaised = kvp.Value })
                .ToArray();

            SyncEndgameDataClientRpc(TotalPikminKilled, TotalPikminLeftBehind, currentTotalPikminCount, playerDataArray);
        }

        public NetworkVariable<int> PLLR = new NetworkVariable<int>();
        [ServerRpc]
        public void SetPikminCountFromSaveServerRpc()
        {
            if (LethalMin.IsUsingModLib())
            {
                PLLR.Value = saveDataEz.PikminLeftLastRound;
            }
            else
            {
                PLLR.Value = saveData.PikminLeftLastRound;
            }
        }
        #endregion

        #region Utility Methods
        public Vector3 GetRandomNavMeshPositionInBoxPredictable(Vector3 pos, float radius = 10f, NavMeshHit navHit = default(NavMeshHit), System.Random randomSeed = null, int layerMask = -1)
        {
            float y = pos.y;
            float x = RandomNumberInRadius(radius, randomSeed);
            float y2 = RandomNumberInRadius(radius, randomSeed);
            float z = RandomNumberInRadius(radius, randomSeed);
            Vector3 vector = new Vector3(x, y2, z) + pos;
            vector.y = y;
            float num = Vector3.Distance(pos, vector);
            if (NavMesh.SamplePosition(vector, out navHit, num + 2f, layerMask))
            {
                return navHit.position;
            }
            return pos;
        }

        private float RandomNumberInRadius(float radius, System.Random randomSeed)
        {
            return ((float)randomSeed.NextDouble() - 0.5f) * radius;
        }

        #endregion

        #region Save and Load
        public OnionSaveData saveData = null;
        public InstancedOnionEzSaveData saveDataEz = new InstancedOnionEzSaveData();
        public List<int> CollectedOnions;

        public string GetSaveFileName()
        {
            int saveFileNum = GameNetworkManager.Instance.saveFileNum;
            switch (saveFileNum)
            {
                case -1:
                    return "LethalMinChallengeFile";
                case 0:
                    return "LethalMinSaveFile1";
                case 1:
                    return "LethalMinSaveFile2";
                case 2:
                    return "LethalMinSaveFile3";
                default:
                    return "LethalMinSaveFile1";
            }
        }

        [ClientRpc]
        public void SetOnionCollectedClientRpc(int typeID)
        {
            OnionType type = LethalMin.GetOnionTypeById(typeID);
            if (!CollectedOnions.Contains(type.OnionTypeID))
            {
                CollectedOnions.Add(type.OnionTypeID);
                LethalMin.Logger.LogInfo($"Added {type.TypeName} Onion to collected list.");
            }
        }

        public void SaveOnionData()
        {
            if (!IsServer) { return; }
            IsSaving = true;
            // Load existing save data
            OnionSaveData existingSaveData = LoadExistingSaveData();
            OnionSaveData newSaveData = new OnionSaveData();
            Onion[] onions = FindObjectsOfType<Onion>();

            // Save collected onions and fused onions
            newSaveData.OnionsCollected = CollectedOnions;
            newSaveData.OnionsFused = FusedOnions;

            // Helper function to check if an onion type is part of a fusion
            bool IsOnionFused(int onionTypeId) => FusedOnions.Any(kvp => kvp.Value.Contains(onionTypeId));

            // Save Onion counts
            foreach (var item in LethalMin.RegisteredOnionTypes)
            {
                int onionTypeId = item.Value.OnionTypeID;
                Onion onion = onions.FirstOrDefault(o => o.type == item.Value || (o.IsFuesion() && o.FusedTypes.Contains(item.Value)));

                if (onion != null)
                {
                    OnionPikmin[] allPikmin = onion.GetPikminInOnion();
                    OnionPikmin[] filteredPikmin = allPikmin.Where(p => item.Value.TypesCanHold.Any(t => t.PikminTypeID == p.PikminTypeID)).ToArray();

                    if (IsOnionFused(onionTypeId))
                    {
                        // For fused onions, we only save the actual count without multiplication
                        newSaveData.PikminStored.Add(new OnionPikminStorage { ID = onionTypeId, Pikmin = filteredPikmin });
                    }
                    else
                    {
                        // For non-fused onions, save as normal
                        newSaveData.PikminStored.Add(new OnionPikminStorage { ID = onionTypeId, Pikmin = filteredPikmin });
                    }
                    LethalMin.Logger.LogInfo($"Saved {item.Value.TypeName} Onion data from current game state. Total Pikmin: {filteredPikmin.Length}");
                }
                else if (existingSaveData.OnionsCollected.Contains(onionTypeId))
                {
                    // Use existing data if the onion is not present in the current game
                    var existingPikminStorage = existingSaveData.PikminStored.FirstOrDefault(storage => storage.ID == onionTypeId);
                    if (existingPikminStorage.Pikmin != null)
                    {
                        OnionPikmin[] filteredPikmin = existingPikminStorage.Pikmin.Where(p => item.Value.TypesCanHold.Any(t => t.PikminTypeID == p.PikminTypeID)).ToArray();
                        newSaveData.PikminStored.Add(new OnionPikminStorage { ID = onionTypeId, Pikmin = filteredPikmin });
                        LethalMin.Logger.LogInfo($"Using existing save data for {item.Value.TypeName} Onion. Total Pikmin: {filteredPikmin.Length}");
                    }
                    else
                    {
                        LethalMin.Logger.LogInfo($"No existing pikmin data found for {item.Value.TypeName} Onion.");
                    }
                }
                else
                {
                    LethalMin.Logger.LogInfo($"{item.Value.TypeName} Onion not found and no existing save data. Skipping {item.Value.TypeName} Onion data save.");
                }
            }

            newSaveData.PikminLeftLastRound = 0;
            foreach (Onion onion in onions)
            {
                newSaveData.PikminLeftLastRound += onion.GetPikminCount();
                LethalMin.Logger.LogInfo($"Pikmin left: {newSaveData.PikminLeftLastRound} Onion: {onion.GetPikminCount()}");
            }

            string json = JsonConvert.SerializeObject(newSaveData);
            File.WriteAllText(SaveFilePath, json);
            IsSaving = false;
            LethalMin.Logger.LogMessage("Onion data saved successfully.");
        }

        public void SaveEZOnionData()
        {
            if (!IsServer) { return; }
            IsSaving = true;
            // Load existing save data
            Onion[] onions = FindObjectsOfType<Onion>();
            InstancedOnionEzSaveData newSaveData = new InstancedOnionEzSaveData();
            // Save collected onions and fused onions
            newSaveData.OnionsCollected = CollectedOnions;
            newSaveData.OnionsFused = FusedOnions;

            // Helper function to check if an onion type is part of a fusion
            bool IsOnionFused(int onionTypeId) => FusedOnions.Any(kvp => kvp.Value.Contains(onionTypeId));

            // Save Onion counts
            foreach (var item in LethalMin.RegisteredOnionTypes)
            {
                int onionTypeId = item.Value.OnionTypeID;
                Onion onion = onions.FirstOrDefault(o => o.type == item.Value || (o.IsFuesion() && o.FusedTypes.Contains(item.Value)));

                if (onion != null)
                {
                    OnionPikmin[] allPikmin = onion.GetPikminInOnion();
                    OnionPikmin[] filteredPikmin = allPikmin.Where(p => item.Value.TypesCanHold.Any(t => t.PikminTypeID == p.PikminTypeID)).ToArray();

                    if (IsOnionFused(onionTypeId))
                    {
                        // For fused onions, we only save the actual count without multiplication
                        newSaveData.PikminStored.Add(new OnionPikminStorage { ID = onionTypeId, Pikmin = filteredPikmin });
                    }
                    else
                    {
                        // For non-fused onions, save as normal
                        newSaveData.PikminStored.Add(new OnionPikminStorage { ID = onionTypeId, Pikmin = filteredPikmin });
                    }
                    LethalMin.Logger.LogInfo($"Saved {item.Value.TypeName} Onion data from current game state. Total Pikmin: {filteredPikmin.Length}");
                }
                else if (saveDataEz.OnionsCollected.Contains(onionTypeId))
                {
                    // Use existing data if the onion is not present in the current game
                    var existingPikminStorage = saveDataEz.PikminStored.FirstOrDefault(storage => storage.ID == onionTypeId);
                    if (existingPikminStorage.Pikmin != null)
                    {
                        OnionPikmin[] filteredPikmin = existingPikminStorage.Pikmin.Where(p => item.Value.TypesCanHold.Any(t => t.PikminTypeID == p.PikminTypeID)).ToArray();
                        newSaveData.PikminStored.Add(new OnionPikminStorage { ID = onionTypeId, Pikmin = filteredPikmin });
                        LethalMin.Logger.LogInfo($"Using existing save data for {item.Value.TypeName} Onion. Total Pikmin: {filteredPikmin.Length}");
                    }
                    else
                    {
                        LethalMin.Logger.LogInfo($"No existing pikmin data found for {item.Value.TypeName} Onion.");
                    }
                }
                else
                {
                    LethalMin.Logger.LogInfo($"{item.Value.TypeName} Onion not found and no existing save data. Skipping {item.Value.TypeName} Onion data save.");
                }
            }

            newSaveData.PikminLeftLastRound = 0;
            foreach (Onion onion in onions)
            {
                newSaveData.PikminLeftLastRound += onion.GetPikminCount();
                LethalMin.Logger.LogInfo($"Pikmin left: {newSaveData.PikminLeftLastRound} Onion: {onion.GetPikminCount()}");
            }

            newSaveData.ConvertFromInstanced();

            IsSaving = false;
            LethalMin.Logger.LogMessage("Onion data saved successfully.");
        }

        private OnionSaveData LoadExistingSaveData()
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                return JsonConvert.DeserializeObject<OnionSaveData>(json);
            }
            return new OnionSaveData();
        }

        public void LoadOnionData()
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                saveData = JsonConvert.DeserializeObject<OnionSaveData>(json);
                if (saveData == null)
                {
                    LethalMin.Logger.LogWarning("Save datat corupted. Creating...");
                    OnionSaveData NewsaveData = new OnionSaveData();
                    string json2 = JsonConvert.SerializeObject(NewsaveData);
                    File.WriteAllText(SaveFilePath, json2);
                    LethalMin.Logger.LogMessage("Onion data saved successfully.");
                    saveData = NewsaveData;
                    LethalMin.Logger.LogMessage("Onion data loaded successfully.");
                    return;
                }
                CollectedOnions = saveData.OnionsCollected;
                FusedOnions = saveData.OnionsFused;
                if (FindObjectOfType<DualOnion>() != null)
                {
                    var OnionInstace = FindObjectOfType<DualOnion>();
                    var pikminStorage = saveData.PikminStored.FirstOrDefault(storage => storage.ID == OnionInstace.type.OnionTypeID);
                    if (pikminStorage.Pikmin != null)
                    {
                        OnionInstace.SyncPikminListServerRpc(pikminStorage.Pikmin);
                    }
                }
                LethalMin.Logger.LogMessage("Onion data loaded successfully.");
            }
            else
            {
                LethalMin.Logger.LogWarning("No saved onion data found. Creating...");
                OnionSaveData NewsaveData = new OnionSaveData();
                string json = JsonConvert.SerializeObject(NewsaveData);
                File.WriteAllText(SaveFilePath, json);
                LethalMin.Logger.LogMessage("Onion data saved successfully.");
                saveData = NewsaveData;
                LethalMin.Logger.LogMessage("Onion data loaded successfully.");
            }
        }

        public void LoadEZOnionData()
        {
            saveDataEz = OnionEzSaveData.ConvertToInstanced();

            CollectedOnions = saveDataEz.OnionsCollected;
            FusedOnions = saveDataEz.OnionsFused;

            if (FindObjectOfType<DualOnion>() != null)
            {
                var OnionInstace = FindObjectOfType<DualOnion>();
                var pikminStorage = saveDataEz.PikminStored.FirstOrDefault(storage => storage.ID == OnionInstace.type.OnionTypeID);
                if (pikminStorage.Pikmin != null)
                {
                    OnionInstace.SyncPikminListServerRpc(pikminStorage.Pikmin);
                }
            }
            LethalMin.Logger.LogMessage("Onion data loaded successfully.");
        }

        #endregion

        #region Pikmin Methods 
        private void SendPikminToOnion(PikminAI pikmin)
        {
            if (pikmin.TargetOnion != null)
            {
                pikmin.TargetOnion.pikminInOnion.Add(new OnionPikmin { GrowStage = pikmin.GrowStage });
                pikmin.agent.updatePosition = false;
                pikmin.agent.updateRotation = false;
                pikmin.transform.position = pikmin.TargetOnion.AnimPos.position;
                pikmin.ReqeustPlayEnterOnionClientRpc();
                pikmin.PlayAnimClientRpc("EnterOnion");
                pikmin.StartCoroutine(pikmin.DestoryMin());
            }
        }

        [ClientRpc]
        public void DespawnPikminClientRpc(NetworkObjectReference networkObjectRef)
        {
            if (RoundManager.Instance == null) { return; }
            if (networkObjectRef.TryGet(out NetworkObject networkObject))
            {
                PikminAI pikminAI = networkObject.GetComponent<PikminAI>();
                if (pikminAI != null)
                {
                    if (RoundManager.Instance.SpawnedEnemies.Contains(pikminAI))
                    {
                        RoundManager.Instance.SpawnedEnemies.Remove(pikminAI);
                        if (LethalMin.DebugMode)
                        {
                            LethalMin.Logger.LogInfo($"Removed Pikmin {pikminAI.name} from RoundManager");
                        }
                    }
                    if (IsServer)
                        networkObject.Despawn(true);
                }
            }
        }

        [ClientRpc]
        public void SpawnPikminClientRpc(NetworkObjectReference networkObjectRef)
        {
            if (RoundManager.Instance == null) { return; }
            if (networkObjectRef.TryGet(out NetworkObject networkObject))
            {
                PikminAI pikminAI = networkObject.GetComponent<PikminAI>();
                if (pikminAI != null)
                {
                    if (!RoundManager.Instance.SpawnedEnemies.Contains(pikminAI))
                    {
                        RoundManager.Instance.SpawnedEnemies.Add(pikminAI);
                        if (LethalMin.DebugMode)
                        {
                            LethalMin.Logger.LogInfo($"Added Pikmin {pikminAI.name} to RoundManager");
                        }
                    }
                }
            }
        }

        public float ShipPickupRange = 20f;
        public float OnionPickupRange = 20f;
        public void HandlePikminWhenShipLeaving()
        {
            if (!IsServer) { return; }

            Vector3 shipPosition = StartOfRound.Instance.elevatorTransform.position;
            Onion[] onions = UnityEngine.Object.FindObjectsOfType<Onion>();
            PikminAI[] allPikmin = UnityEngine.Object.FindObjectsOfType<PikminAI>();

            int leftBehindCount = 0;

            foreach (var leaderManager in FindObjectsOfType<LeaderManager>())
            {
                if (leaderManager.Controller != null && leaderManager.Controller.isPlayerControlled
                || leaderManager.Controller != null && leaderManager.Controller.IsLocalPlayer)
                {
                    leaderManager.RemoveAllPikminServerRpc(false);
                }
            }
            foreach (PikminAI pikmin in allPikmin)
            {
                if (!pikmin.isOutside)
                {
                    LethalMin.Logger.LogInfo($"{pikmin.uniqueDebugId} Is indoors");
                    pikmin.IsLeftBehind = true;
                    continue;
                }
                if (pikmin.TargetOnion == null)
                {
                    LethalMin.Logger.LogInfo($"{pikmin.uniqueDebugId} does not have an onion :(");
                    pikmin.IsLeftBehind = true;
                    pikmin.SwitchToBehaviourClientRpc((int)PState.Leaveing);
                    continue;
                }
                bool isNearShip =
                Vector3.Distance(pikmin.transform.position, shipPosition) <= ShipPickupRange ||
                pikmin.rb != null && Vector3.Distance(pikmin.rb.position, shipPosition) <= ShipPickupRange ||
                Vector3.Distance(pikmin.agent.nextPosition, shipPosition) <= ShipPickupRange; // Adjust distance as needed
                bool isNearAnyOnion = pikmin.TargetOnion != null && Vector3.Distance(pikmin.transform.position, pikmin.TargetOnion.transform.position) <= OnionPickupRange; // Adjust distance as needed


                LethalMin.Logger.LogInfo($"{pikmin.uniqueDebugId} is {Vector3.Distance(pikmin.transform.position, shipPosition)}" +
                $"Distance from agent.nextPosition: {Vector3.Distance(pikmin.agent.nextPosition, shipPosition)}" +
                $"Distance from rb.position: {Vector3.Distance(pikmin.rb.position, shipPosition)}, " +
                $" away from Ship with the range: {ShipPickupRange} So it is {isNearShip} in ship. ");


                LethalMin.Logger.LogInfo($"{pikmin.uniqueDebugId} is {Vector3.Distance(pikmin.transform.position, pikmin.TargetOnion.transform.position)}" +
                $"Distance from agent.nextPosition: {Vector3.Distance(pikmin.agent.nextPosition, pikmin.TargetOnion.transform.position)}" +
                $"Distance from rb.position: {Vector3.Distance(pikmin.rb.position, pikmin.TargetOnion.transform.position)}, " +
                $" away from Ship with the range: {ShipPickupRange} So it is {isNearAnyOnion} in Onion. ");


                if (!isNearShip && !isNearAnyOnion)
                {
                    leftBehindCount++;
                    pikmin.SwitchToBehaviourClientRpc((int)PState.Leaveing);
                    continue;
                }
                if (isNearShip || isNearAnyOnion)
                {
                    if (pikmin.IsDrowing)
                    {
                        pikmin.KillEnemyOnOwnerClient();
                        continue;
                    }
                    Onion targetOnion = pikmin.TargetOnion;
                    if (targetOnion != null)
                    {
                        // Add pikmin to the onion's list
                        targetOnion.pikminInOnion.Add(new OnionPikmin { GrowStage = pikmin.GrowStage, PikminTypeID = pikmin.PminType.PikminTypeID });

                        // Switch pikmin state to Leaving
                        pikmin.SwitchToBehaviourClientRpc((int)PState.Leaveing);

                        LethalMin.Logger.LogInfo($"{pikmin.uniqueDebugId} Is leaving {targetOnion.type.ToString()} Onion");
                    }
                }
            }
            SetPikminLeftBehind(leftBehindCount);

            // Update the pikmin lists for all onions
            foreach (Onion onion in onions)
            {
                onion.UpdatePikminListClientRpc(onion.pikminInOnion.ToArray());
            }
        }

        #endregion

        #region Post-Game
        public IEnumerator DespawnOnions()
        {
            if (!IsServer) { yield return null; }
            while (IsSaving)
            {
                yield return new WaitForSeconds(1f);
            }
            Onion[] onions = UnityEngine.Object.FindObjectsOfType<Onion>();
            foreach (Onion onion in onions)
            {
                if (onion.GetComponent<DualOnion>() != null) { continue; }
                if (onion.NetworkObject != null)
                {
                    if (onion.NetworkObject.IsSpawned)
                    {
                        try
                        {
                            onion.NetworkObject.Despawn(true);
                        }
                        catch (System.Exception e)
                        {
                            LethalMin.Logger.LogError($"Error despawning onion: {e.Message}");
                        }
                    }
                    else
                    {
                        LethalMin.Logger.LogWarning($"Attempted to despawn an unspawned onion: {onion.name}");
                    }
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Onion {onion.name} has no NetworkObject component");
                }
            }
            LethalMin.Logger.LogMessage("All onions have been despawned and destroyed.");
        }

        public void DespawnSprouts()
        {
            Onion[] onions = UnityEngine.Object.FindObjectsOfType<Onion>();
            foreach (Onion onion in onions)
            {
                if (onion.GetComponent<DualOnion>() != null) { continue; }
                // foreach (var item in onion.gameObject.GetComponentsInChildren<Renderer>())
                // {
                //     item.enabled = false;
                // }
            }
            if (!IsServer) { return; }
            Sprout[] sprouts = UnityEngine.Object.FindObjectsOfType<Sprout>();
            int sproutCount = sprouts.Length;
            foreach (Sprout sprout in sprouts)
            {
                if (sprout.NetworkObject != null && sprout.NetworkObject.IsSpawned)
                {
                    sprout.NetworkObject.Despawn(true);
                }
                UnityEngine.Object.Destroy(sprout.gameObject);
            }
            LethalMin.Logger.LogMessage($"All ({sproutCount}) sprouts have been despawned and destroyed.");
        }
        public Dictionary<int, int[]> FusedOnions;
        public void FuseOnions()
        {
            if (!LethalMin.Pikmin3Style) { return; }
            FusedOnions = new Dictionary<int, int[]>();

            // Get every registered Fuse Rule
            Dictionary<int, OnionFuseRules> fuseRules = LethalMin.RegisteredFuseRules;

            foreach (var rule in fuseRules)
            {
                int fuseResultId = rule.Key;
                OnionFuseRules fuseRule = rule.Value;

                // Get the compatible onions for this rule
                List<int> compatibleOnions = fuseRule.CompatibleOnions.Select(o => o.OnionTypeID).ToList();

                // Check which of the compatible onions have been collected
                List<int> collectedCompatibleOnions = compatibleOnions.Intersect(CollectedOnions).ToList();

                // Remove already fused onions with this ID
                collectedCompatibleOnions.RemoveAll(id => FusedOnions.ContainsKey(fuseResultId) && FusedOnions[fuseResultId].Contains(id));

                // If we have at least 2 compatible onions collected, we can fuse
                if (collectedCompatibleOnions.Count >= 2 ||
                FusedOnions.ContainsKey(fuseResultId) && FusedOnions[fuseResultId].Length > 0)
                {
                    FusedOnions[fuseResultId] = collectedCompatibleOnions.ToArray();
                    LethalMin.Logger.LogInfo($"Fused new onion with ID {fuseResultId}. Fused onions: {string.Join(", ", FusedOnions[fuseResultId])}");
                }
                else
                {
                    LethalMin.Logger.LogInfo($"Not enough compatible onions to fuse for ID {fuseResultId}.");
                }
            }

            LethalMin.Logger.LogInfo($"Fusion process completed. Total fused onions: {FusedOnions.Count}");
        }
        #endregion

        #region Onion Spawning
        public IEnumerator SpawnOnions()
        {
            LethalMin.Logger.LogInfo("Waiting for ship to land before doing onion spawns");
            yield return new WaitUntil(() => StartOfRound.Instance.fullyLoadedPlayers.Count >= GameNetworkManager.Instance.connectedPlayers);
            yield return new WaitUntil(() => StartOfRound.Instance.shipHasLanded);
            while (!CachedSpawnPoints)
            {
                yield return new WaitForSeconds(0.1f);
            }
            OnionPrefab = LethalMin.OnionPrefab;

            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            Vector3 shipPosition = StartOfRound.Instance.elevatorTransform.position;

            // Sort NearbyNodes by distance from ship
            spawnInfo.NearbyNodes = spawnInfo.NearbyNodes
                .OrderBy(node => Vector3.Distance(node, shipPosition))
                .ToList();

            // Create a list to keep track of onions we've already handled (either spawned or part of a fusion)
            List<int> handledOnions = new List<int>();

            // First, handle fused onions
            if (FusedOnions != null && FusedOnions.Count > 0)
            {
                foreach (var fusedOnion in FusedOnions)
                {
                    if (fusedOnion.Value.Length >= 2)
                    {
                        Vector3 spawnPosition;
                        if (spawnInfo.NearbyNodes.Count > 0)
                        {
                            spawnPosition = spawnInfo.NearbyNodes[0];
                            spawnInfo.NearbyNodes.RemoveAt(0);
                        }
                        else
                        {
                            spawnPosition = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform.position;
                            LethalMin.Logger.LogWarning($"Ran out of calculated spawn points. Choosing random spawn point for fused onion.");
                        }

                        yield return SpawnOnion(spawnPosition, fusedOnion.Value);
                        yield return new WaitForSeconds(0.5f);

                        // Mark these onions as handled
                        handledOnions.AddRange(fusedOnion.Value);
                    }
                }
            }

            // Then, spawn the remaining collected onions that weren't part of a fusion
            if (!LethalMin.IsUsingModLib() && saveData != null && CollectedOnions.Count > 0)
            {
                foreach (int onionId in saveData.OnionsCollected)
                {
                    if (!handledOnions.Contains(onionId) && LethalMin.RegisteredOnionTypes.ContainsKey(onionId))
                    {
                        OnionType onionType = LethalMin.RegisteredOnionTypes[onionId];
                        Vector3 spawnPosition;

                        if (spawnInfo.NearbyNodes.Count > 0)
                        {
                            spawnPosition = spawnInfo.NearbyNodes[0];
                            spawnInfo.NearbyNodes.RemoveAt(0);
                        }
                        else
                        {
                            spawnPosition = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform.position;
                            LethalMin.Logger.LogWarning($"Ran out of calculated spawn points. Choosing random spawn point for {onionType.TypeName} Onion.");
                        }

                        yield return SpawnOnion(spawnPosition, onionType);
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
            else if (LethalMin.IsUsingModLib() && saveDataEz != null && CollectedOnions.Count > 0)
            {
                foreach (int onionId in saveDataEz.OnionsCollected)
                {
                    if (!handledOnions.Contains(onionId) && LethalMin.RegisteredOnionTypes.ContainsKey(onionId))
                    {
                        OnionType onionType = LethalMin.RegisteredOnionTypes[onionId];
                        Vector3 spawnPosition;

                        if (spawnInfo.NearbyNodes.Count > 0)
                        {
                            spawnPosition = spawnInfo.NearbyNodes[0];
                            spawnInfo.NearbyNodes.RemoveAt(0);
                        }
                        else
                        {
                            spawnPosition = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform.position;
                            LethalMin.Logger.LogWarning($"Ran out of calculated spawn points. Choosing random spawn point for {onionType.TypeName} Onion.");
                        }

                        yield return SpawnOnion(spawnPosition, onionType);
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }

            PikminAI[] pikminAIs = GameObject.FindObjectsOfType<PikminAI>();
            foreach (var pikminAI in pikminAIs)
            {
                pikminAI.CheckForOnion();
            }
        }

        public IEnumerator SpawnOnion(Vector3 position, OnionType onionType)
        {
            if (!IsServer)
            {
                LethalMin.Logger.LogWarning("Attempted to spawn Onion on client. This should not happen.");
                yield break;
            }

            try
            {
                GameObject onionInstance = UnityEngine.Object.Instantiate(OnionPrefab, position, Quaternion.identity);
                NetworkObject networkObject = onionInstance.GetComponent<NetworkObject>();
                Onion onionComponent = networkObject.GetComponent<Onion>();
                AnimatedOnion animedOnion = onionComponent.GetComponent<AnimatedOnion>();
                networkObject.GetComponent<Onion>().type = onionType;
                if (networkObject != null)
                {
                    networkObject.Spawn();
                    if (animedOnion != null)
                        animedOnion.SyncOnionTypeClientRpc(onionType.OnionTypeID);
                }

                // Find the corresponding pikmin list in saveData
                var pikminStorage = new OnionPikminStorage();
                if (LethalMin.IsUsingModLib())
                {
                    pikminStorage = saveDataEz.PikminStored.FirstOrDefault(storage => storage.ID == onionType.OnionTypeID);
                }
                else
                {
                    pikminStorage = saveData.PikminStored.FirstOrDefault(storage => storage.ID == onionType.OnionTypeID);
                }
                if (pikminStorage.Pikmin != null)
                {
                    networkObject.GetComponent<Onion>().SyncPikminListServerRpc(pikminStorage.Pikmin);
                }

                LethalMin.Logger.LogInfo($"Spawned {onionType.TypeName} Onion at {position}");

                // Destroy the OnionItem on all clients
                OnionItem[] onionItems = UnityEngine.Object.FindObjectsOfType<OnionItem>();
                OnionItem itemToDestroy = onionItems.FirstOrDefault(item => item.type == onionType);
                if (itemToDestroy != null)
                {
                    NetworkObject itemNetObj = itemToDestroy.GetComponent<NetworkObject>();
                    if (itemNetObj != null && itemNetObj.IsSpawned)
                    {
                        itemNetObj.Despawn(true);
                    }
                    UnityEngine.Object.Destroy(itemToDestroy.gameObject);
                }
            }
            catch (Exception ex)
            {
                LethalMin.Logger.LogError($"Failed to spawn onion due to: {ex.Message}");
            }
        }
        public IEnumerator SpawnOnion(Vector3 position, int[] IDs)
        {
            if (!IsServer)
            {
                LethalMin.Logger.LogWarning("Attempted to spawn Onion on client. This should not happen.");
                yield break;
            }

            try
            {
                GameObject onionInstance = UnityEngine.Object.Instantiate(OnionPrefab, position, Quaternion.identity);
                NetworkObject networkObject = onionInstance.GetComponent<NetworkObject>();
                Onion onionComponent = networkObject.GetComponent<Onion>();
                AnimatedOnion animedOnion = onionComponent.GetComponent<AnimatedOnion>();

                if (networkObject != null)
                {
                    networkObject.Spawn();
                    if (animedOnion != null)
                        animedOnion.SyncOnionTypeClientRpc(IDs);
                }

                // Find the corresponding pikmin list in saveData
                List<OnionPikminStorage> pikminStorage = new List<OnionPikminStorage>();
                foreach (int ID in IDs)
                {
                    if (LethalMin.IsUsingModLib())
                    {
                        pikminStorage.Add(saveDataEz.PikminStored.FirstOrDefault(storage => storage.ID == ID));
                    }
                    else
                    {
                        pikminStorage.Add(saveData.PikminStored.FirstOrDefault(storage => storage.ID == ID));
                    }
                }
                List<OnionPikmin> finalPikminStorage = new List<OnionPikmin>();
                foreach (var storage in pikminStorage)
                {
                    if (storage.Pikmin != null)
                    {
                        finalPikminStorage.AddRange(storage.Pikmin);
                    }
                }
                networkObject.GetComponent<Onion>().SyncPikminListServerRpc(finalPikminStorage.ToArray());

                LethalMin.Logger.LogInfo($"Spawned fused {string.Join(", ", IDs)}Onion at {position}");
            }
            catch (Exception ex)
            {
                LethalMin.Logger.LogError($"Failed to spawn fused onion due to: {ex.Message}");
            }
        }

        public IEnumerator SpawnSpecificOnion(OnionType onionType)
        {
            while (!CachedSpawnPoints)
            {
                yield return new WaitForSeconds(0.1f);
            }
            LethalMin.Logger.LogInfo($"Attempting to spawn a {onionType.TypeName} Onion");

            if (OnionPrefab == null)
            {
                OnionPrefab = LethalMin.OnionPrefab;
            }

            if (spawnInfo.NearbyNodes.Count > 0)
            {
                int ind = 0;
                Vector3 spawnPosition = spawnInfo.NearbyNodes[ind];
                spawnInfo.NearbyNodes.RemoveAt(ind);
                yield return SpawnOnion(spawnPosition, onionType);
                LethalMin.Logger.LogInfo($"{onionType.TypeName} Onion spawned successfully");
            }
            else
            {
                GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
                // If we've run out of calculated spawn points, choose a random outdoor node
                Vector3 spawnPosition = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform.position;
                LethalMin.Logger.LogWarning($"Ran out of calculated spawn points. Choosing random spawn point for {onionType.TypeName} Onion.");
                yield return SpawnOnion(spawnPosition, onionType);
                LethalMin.Logger.LogInfo($"{onionType.TypeName} Onion spawned successfully");
            }

            PikminAI[] pikminAIs = GameObject.FindObjectsOfType<PikminAI>();
            foreach (var pikminAI in pikminAIs)
            {
                pikminAI.CheckForOnion();
            }
        }

        public struct SpawnPointInfo
        {
            public List<Vector3> NearbyNodes;

            public SpawnPointInfo(List<Vector3> nearbyNodes)
            {
                NearbyNodes = nearbyNodes;
            }
        }

        private List<Vector3> FindOnionSpawnPositions(GameObject[] spawnPoints, Vector3 shipPosition, int onionsToSpawn)
        {
            List<Vector3> spawnPositions = new List<Vector3>();

            // Check for specific onion spawn points first
            if (LethalMin.CustomOnionAllowedValue)
            {
                List<Vector3> specificSpawnPoints = FindSpecificOnionSpawnPoints();
                spawnPositions.AddRange(specificSpawnPoints);
            }

            // If we still need more spawn points, calculate them
            if (spawnPositions.Count < onionsToSpawn)
            {
                SpawnPointInfo spawnInfo = FindSuitableSpawnPoint(spawnPoints, shipPosition);
                if (spawnInfo.NearbyNodes.Count > 0)
                {
                    spawnPositions.AddRange(spawnInfo.NearbyNodes);
                }
            }

            return spawnPositions;
        }

        public SpawnPointInfo FindSuitableSpawnPoint(GameObject[] spawnPoints, Vector3 shipPosition)
        {
            LethalMin.Logger.LogInfo($"Starting FindSuitableSpawnPoint. Total spawn points: {spawnPoints.Length}");
            LethalMin.Logger.LogInfo($"Ship position: {shipPosition}");

            SpawnPointInfo result;

            // Strategy 1: Normal check, excluding nodes behind the ship
            LethalMin.Logger.LogMessage($"Searching outdoor nodes to spawn onions using Strategy 1...");
            result = CheckWithFixedDistances(spawnPoints, shipPosition, 50f, true);
            if (result.NearbyNodes.Count > 0) return result;

            // Strategy 2: Check all nodes, including those behind the ship
            LethalMin.Logger.LogMessage($"Searching outdoor nodes to spawn onions using Strategy 2...");
            result = CheckWithFixedDistances(spawnPoints, shipPosition, 50f, false);
            if (result.NearbyNodes.Count > 0) return result;

            // Strategy 3: Average distances, excluding nodes behind the ship
            LethalMin.Logger.LogMessage($"Searching outdoor nodes to spawn onions using Strategy 3...");
            result = CheckWithAveragedDistances(spawnPoints, shipPosition, true);
            if (result.NearbyNodes.Count > 0) return result;

            // Strategy 4: Average distances, including nodes behind the ship
            LethalMin.Logger.LogMessage($"Searching outdoor nodes to spawn onions using Strategy 4...");
            result = CheckWithAveragedDistances(spawnPoints, shipPosition, false);
            if (result.NearbyNodes.Count > 0) return result;

            // Strategy 5: Unlimited distance from ship, excluding nodes behind ship
            LethalMin.Logger.LogWarning($"Searching outdoor nodes to spawn onions using Strategy 5...");
            result = CheckWithUnlimitedDistance(spawnPoints, shipPosition, true);
            if (result.NearbyNodes.Count > 0) return result;

            // Strategy 6: Unlimited distance from ship, including nodes behind ship
            LethalMin.Logger.LogWarning($"Searching outdoor nodes to spawn onions using Strategy 6...");
            result = CheckWithUnlimitedDistance(spawnPoints, shipPosition, false);
            if (result.NearbyNodes.Count > 0) return result;

            LethalMin.Logger.LogFatal("No suitable spawn points found after all strategies.");
            return new SpawnPointInfo(new List<Vector3>());
        }

        public SpawnPointInfo CheckWithFixedDistances(GameObject[] spawnPoints, Vector3 shipPosition, float maxDistanceFromShip, bool excludeBehindShip)
        {
            LethalMin.Logger.LogInfo($"Checking with fixed distances: maxDistance={maxDistanceFromShip}, excludeBehindShip={excludeBehindShip}");

            var suitablePoints = spawnPoints
                .Select(sp => sp.transform.position)
                .Where(pos =>
                {
                    bool isCloseToShip = Vector3.Distance(pos, shipPosition) <= maxDistanceFromShip;
                    bool isNotBehindShip = !excludeBehindShip || !IsPointBehindShip(pos, shipPosition);
                    return isCloseToShip && isNotBehindShip;
                })
                .OrderBy(pos => Vector3.Distance(pos, shipPosition))
                .ToList();

            LethalMin.Logger.LogInfo($"Suitable points found: {suitablePoints.Count}");

            return new SpawnPointInfo(suitablePoints);
        }


        public SpawnPointInfo CheckWithAveragedDistances(GameObject[] spawnPoints, Vector3 shipPosition, bool excludeBehindShip, bool pickAtRandom = false)
        {
            LethalMin.Logger.LogInfo($"Checking with averaged distances, excludeBehindShip={excludeBehindShip}, pickAtRandom={pickAtRandom}");

            float avgDistance = spawnPoints.Average(sp => Vector3.Distance(sp.transform.position, shipPosition));
            float avgDistanceBetweenPoints = spawnPoints.Average(sp1 =>
                spawnPoints.Where(sp2 => sp1 != sp2).Min(sp2 => Vector3.Distance(sp1.transform.position, sp2.transform.position)));

            LethalMin.Logger.LogInfo($"Average distance from ship: {avgDistance}, Average distance between points: {avgDistanceBetweenPoints}");

            return CheckWithFixedDistances(spawnPoints, shipPosition, avgDistance, excludeBehindShip);
        }

        public SpawnPointInfo CheckWithUnlimitedDistance(GameObject[] spawnPoints, Vector3 shipPosition, bool excludeBehindShip, bool pickAtRandom = false)
        {
            LethalMin.Logger.LogInfo($"Checking with unlimited distance, excludeBehindShip={excludeBehindShip}, pickAtRandom={pickAtRandom}");

            var suitablePoints = spawnPoints
                .Select(sp => sp.transform.position)
                .Where(pos => !excludeBehindShip || !IsPointBehindShip(pos, shipPosition))
                .ToList();

            LethalMin.Logger.LogInfo($"Suitable points found: {suitablePoints.Count}");

            if (suitablePoints.Count > 0)
            {
                List<Vector3> nearbyNodes;
                if (pickAtRandom)
                {
                    Vector3 chosenPoint = suitablePoints[UnityEngine.Random.Range(0, suitablePoints.Count)];
                    nearbyNodes = GetNearbyNodes(chosenPoint, spawnPoints, float.MaxValue);
                }
                else
                {
                    Vector3 point = suitablePoints.OrderBy(pos => Vector3.Distance(pos, shipPosition)).ToList()[0];
                    nearbyNodes = GetNearbyNodes(point, spawnPoints, float.MaxValue);
                }
                return new SpawnPointInfo(nearbyNodes);
            }

            return new SpawnPointInfo(new List<Vector3>());
        }

        private List<Vector3> FindSpecificOnionSpawnPoints()
        {
            List<Vector3> specificSpawnPoints = new List<Vector3>();

            if (LethalMin.IsUsingModLib())
            {
                for (int i = 1; i <= saveDataEz.OnionsCollected.Count; i++) // Assuming we're looking for up to 3 spawn points
                {
                    GameObject spawnPoint = GameObject.Find($"ONION_SPAWN_POINT_{i}");
                    if (spawnPoint != null)
                    {
                        specificSpawnPoints.Add(spawnPoint.transform.position);
                    }
                }
            }
            else
            {
                for (int i = 1; i <= saveData.OnionsCollected.Count; i++) // Assuming we're looking for up to 3 spawn points
                {
                    GameObject spawnPoint = GameObject.Find($"ONION_SPAWN_POINT_{i}");
                    if (spawnPoint != null)
                    {
                        specificSpawnPoints.Add(spawnPoint.transform.position);
                    }
                }
            }
            return specificSpawnPoints;
        }

        public bool IsPointBehindShip(Vector3 point, Vector3 shipPosition)
        {
            // Assuming the ship faces the positive Z direction
            return point.z < shipPosition.z;
        }

        public bool HasNearbySpawnPoints(Vector3 point, GameObject[] allSpawnPoints, float minDistance)
        {
            int nearbyPoints = allSpawnPoints
                .Count(sp => Vector3.Distance(sp.transform.position, point) <= minDistance && sp.transform.position != point);

            return nearbyPoints >= 2;
        }

        public List<Vector3> GetNearbyNodes(Vector3 point, GameObject[] allSpawnPoints, float maxDistance)
        {
            return allSpawnPoints
                .Select(sp => sp.transform.position)
                .Where(pos => Vector3.Distance(pos, point) <= maxDistance && pos != point)
                .OrderBy(pos => Vector3.Distance(pos, point))
                .ToList();
        }
        #endregion
    }
}