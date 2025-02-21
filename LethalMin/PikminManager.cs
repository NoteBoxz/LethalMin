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
using LethalMin.Patches.AI;
using LCOffice.Patches;
using ElevatorMod.Patches;
using LethalMin.Patches.OtherMods;
using Unity.Mathematics;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;


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
        public GameObject OnionPrefab, OnionContainer;

        #region Initialization and Core Management
        public float CarRefreshTimer, ItemRefreshTimer, OnionRefreshTimer, PikminEntityTimer, PuffminEntityTime, NonPikminEnetityTimer;

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
            if (OnionContainer == null)
            {
                OnionContainer = Instantiate(AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/ShipModeOnionContainer.prefab"));
                OnionContainer.AddComponent<ShipPhaseOnionContainer>();
            }
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
                StartCoroutine(SpawnShipPhaseOnions());
        }
        void LateUpdate()
        {
            DBM.enabled = LethalMin.DebugMode;
            OnionContainer.transform.position = new Vector3(LethalMin.ShipPhaseOnionX, LethalMin.ShipPhaseOnionY, LethalMin.ShipPhaseOnionZ);
            if (StartOfRound.Instance == null || !StartOfRound.Instance.shipHasLanded)
            {
                if (_currentPikminItemsInMap.Count > 0)
                {
                    _currentPikminItemsInMap.Clear();
                    PikminItemsExclusion.Clear();
                }
                return;
            }
            if (PIOMTimer >= 0)
            {
                PIOMTimer -= Time.deltaTime;
            }
            else
            {
                RefreshPikminItemsInMapList();
                RefreshNonPikminEnemiesList();
                RefreshPuffminList();
                RefreshPikminList();
                RefreshOnionsList();
                RefreshCarsList();
                RefreshLocks();

                if (!LethalMin.HidePuffminPrompt)
                {
                    foreach (var item in _currentPuffminEnemies)
                    {
                        if (item.PlayerLatchedOn == StartOfRound.Instance.localPlayerController && !item.IsDying)
                        {
                            PikminHUD.pikminHUDInstance.WigglePrompt.SetActive(true);
                            break;
                        }
                        PikminHUD.pikminHUDInstance.WigglePrompt.SetActive(false);
                    }
                }
                if (_currentPuffminEnemies.Count == 0 || LethalMin.HidePuffminPrompt)
                {
                    PikminHUD.pikminHUDInstance.WigglePrompt.SetActive(false);
                }

                PIOMTimer = LethalMin.ManagerRefreshRate;
            }
        }
        public void OnGameStarted()
        {
            if (!IsServer) { return; }
            if (StartOfRound.Instance == null) { return; }
            if (StartOfRound.Instance.inShipPhase) { return; }
            DespawnShipPhaseOnionsClientRpc();
            OverriddenIndoorTargets.Clear();
            OverriddenOutdoorTargets.Clear();

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
                StartCoroutine(AddPikminBridge());
                RefreshPikminItemsInMapList();
                RefreshNonPikminEnemiesList();
                RefreshPuffminList();
                RefreshPikminList();
                RefreshOnionsList();
                RefreshCarsList();
                RefreshLocks();
                if (RoundManager.Instance.currentLevel.sceneName == "Level7Offense")
                {
                    StartCoroutine(SpawnTPZones(new Vector3(-6.9494f, 18.3041f, -134.7692f), new Vector3(10f, 10f, 10f), new Vector3(7.9994f, 0.4394f, -136.0314f)));
                }
                if (RoundManager.Instance.currentLevel.sceneName == "Level2Assurance")
                {
                    StartCoroutine(SpawnTPZones(new Vector3(101.3612f, 15.7869f, -74.0713f), new Vector3(2.5527f, 4.6818f, 7.8745f), new Vector3(99.9956f, 2.2635f, -57.4636f)));
                }
                if (RoundManager.Instance.currentLevel.sceneName == "Level4March")
                {
                    StartCoroutine(SpawnTPZones(new Vector3(125.5447f, 8.4454f, -17.235f), new Vector3(1.0982f, 0.8345f, 5.8745f), new Vector3(129.4601f, 6.4522f, -16.061f)));
                }
            }
            else if (LethalMin.CanWalkAtCompany())
            {
                ResetCountersServerRpc();
                StartCoroutine(CacheOnionSpawnPoints());
                StartCoroutine(SpawnOnions());
            }
        }

        IEnumerator SpawnTPZones(Vector3 TPostion, Vector3 Scale, Vector3 DestPos)
        {
            yield return new WaitUntil(() => StartOfRound.Instance.fullyLoadedPlayers.Count >= GameNetworkManager.Instance.connectedPlayers);
            yield return new WaitUntil(() => RoundManager.Instance.dungeonCompletedGenerating);

            // Get the current active scene
            Scene currentScene = SceneManager.GetSceneByName(RoundManager.Instance.currentLevel.sceneName);

            bool dbMode = false;

            GameObject TPZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            TPZone.transform.position = TPostion;
            TPZone.transform.localScale = Scale;
            TPZone.AddComponent<PikminTPZone>();
            TPZone.GetComponent<Collider>().isTrigger = true;
            SceneManager.MoveGameObjectToScene(TPZone, currentScene);

            GameObject Dest = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Dest.transform.position = DestPos;
            TPZone.GetComponent<PikminTPZone>().Destination = Dest.transform;
            SceneManager.MoveGameObjectToScene(Dest, currentScene);

            if (dbMode)
            {
                TPZone.GetComponent<Renderer>().material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
                Dest.GetComponent<Renderer>().material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
            }
            else
            {
                GameObject.Destroy(TPZone.GetComponent<Renderer>());
                GameObject.Destroy(Dest.GetComponent<Renderer>());
            }
            GameObject.Destroy(Dest.GetComponent<Collider>());
        }

        public static List<FloorData> CurrentFloorData = new List<FloorData>();
        public static FloorData DefultFloorData = null;
        public static List<Transform> OverriddenIndoorTargets = new List<Transform>();
        public static List<Transform> OverriddenOutdoorTargets = new List<Transform>();
        bool isgettingFloorData;
        public IEnumerator GetFloorData()
        {
            if (isgettingFloorData) { yield break; }
            isgettingFloorData = true;
            List<EntranceTeleport> FindFireExits()
            {
                EntranceTeleport[] allEntrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
                List<EntranceTeleport> allExits = new List<EntranceTeleport>();
                foreach (EntranceTeleport entrance in allEntrances)
                {
                    if (entrance.isEntranceToBuilding)
                    {
                        continue;
                    }
                    if (entrance.entranceId != 0)
                    {
                        allExits.Add(entrance);
                    }
                }
                if (allExits.Count == 0)
                {
                    return null!;
                }
                return allExits.OrderBy(exit => Vector3.Distance(transform.position, exit.transform.position)).ToList();
            }

            yield return new WaitForSeconds(2f);

            LethalMin.Logger.LogInfo("Getting floor data");

            CurrentFloorData.Clear();
            DefultFloorData = null;

            if (RoundManager.Instance.currentMineshaftElevator != null)
            {
                FloorData F1 = new FloorData();
                F1.MainExits.Add(RoundManager.FindMainEntranceScript());
                F1.FloorRoot = RoundManager.FindMainEntrancePosition();
                F1.Elevators.Add(RoundManager.Instance.currentMineshaftElevator.transform);
                F1.ElevatorBounds = RoundManager.Instance.currentMineshaftElevator.
                elevatorInsidePoint.transform.parent.GetComponentInChildren<PlayerPhysicsRegion>().GetComponent<Collider>();
                F1.FloorTitle = "(Floor1) Entrance";
                CurrentFloorData.Add(F1);

                DefultFloorData = F1;

                FloorData F2 = new FloorData();
                F2.FireExits.AddRange(FindFireExits());
                F2.FloorRoot = RoundManager.Instance.currentMineshaftElevator.elevatorBottomPoint.position;
                F2.Elevators.Add(RoundManager.Instance.currentMineshaftElevator.transform);
                F2.ElevatorBounds = RoundManager.Instance.currentMineshaftElevator.
                elevatorInsidePoint.transform.parent.GetComponentInChildren<PlayerPhysicsRegion>().GetComponent<Collider>();
                F2.FloorTitle = "(Floor2) Mineshaft";
                CurrentFloorData.Add(F2);

                LethalMin.Logger.LogInfo("Registered Vanilla Minshaft Floors");
                isgettingFloorData = false;
                yield break;
            }

            if (LethalMin.IsDependencyLoaded("Piggy.LCOffice"))
            {
                if (GetPiggyFloorData())
                {
                    isgettingFloorData = false;
                    yield break;
                }
            }

            if (LethalMin.IsDependencyLoaded("kite.ZelevatorCode"))
            {
                if (GetZelevatorFloorData())
                {
                    isgettingFloorData = false;
                    yield break;
                }
            }
            isgettingFloorData = false;
        }

        public bool GetPiggyFloorData()
        {
            if (FindAnyObjectByType<ElevatorSystem>() == null)
            {
                return false;
            }


            LethalMin.Logger.LogInfo("Office detected. Loading Piggy LCOffice data.");

            List<EntranceTeleport> FindFireExits()
            {
                EntranceTeleport[] allEntrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
                List<EntranceTeleport> allExits = new List<EntranceTeleport>();
                foreach (EntranceTeleport entrance in allEntrances)
                {
                    if (entrance.isEntranceToBuilding)
                    {
                        continue;
                    }
                    if (entrance.entranceId != 0)
                    {
                        allExits.Add(entrance);
                    }
                }
                if (allExits.Count == 0)
                {
                    return null!;
                }
                return allExits.OrderBy(exit => Vector3.Distance(transform.position, exit.transform.position)).ToList();
            }

            GameObject CreateDebugCube(Vector3 LocalPos)
            {
                //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                GameObject cube = new GameObject("Floor Ref Pos");
                cube.transform.SetParent(ElevatorSystem.animator.transform.parent);
                cube.transform.localPosition = LocalPos;
                //cube.GetComponent<Renderer>().material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
                return cube;
            }

            FloorData F1 = new FloorData();
            F1.FloorTitle = "(Floor1) Basement";
            F1.Elevators.Add(ElevatorSystem.animator.transform.Find("AIelevatorFloor (4 Picles)"));
            F1.ElevatorBounds = ElevatorSystem.animator.transform.Find("ELevator Zone (4 Picles)").GetComponent<Collider>();
            GameObject cubeA = CreateDebugCube(new Vector3(-2.32f, -9.71f, 1.01f));
            F1.FloorRoot = cubeA.transform.position;

            CurrentFloorData.Add(F1);

            FloorData F2 = new FloorData();
            F2.FloorTitle = "(Floor2) Lobby";
            F2.Elevators.Add(ElevatorSystem.animator.transform.Find("AIelevatorFloor (4 Picles)"));
            F2.ElevatorBounds = ElevatorSystem.animator.transform.Find("ELevator Zone (4 Picles)").GetComponent<Collider>();
            GameObject cubeB = CreateDebugCube(new Vector3(-2.32f, 23.79f, 1.01f));
            F2.FloorRoot = cubeB.transform.position;
            F2.MainExits.Add(RoundManager.FindMainEntranceScript());

            CurrentFloorData.Add(F2);

            DefultFloorData = F2;

            FloorData F3 = new FloorData();
            F3.FloorTitle = "(Floor3) Upstairs Basement";
            F3.Elevators.Add(ElevatorSystem.animator.transform.Find("AIelevatorFloor (4 Picles)"));
            F3.ElevatorBounds = ElevatorSystem.animator.transform.Find("ELevator Zone (4 Picles)").GetComponent<Collider>();
            GameObject cubeC = CreateDebugCube(new Vector3(-2.32f, 63.14f, 1.01f));
            F3.FloorRoot = cubeC.transform.position;

            CurrentFloorData.Add(F3);

            FloorData DetermineFloor(GameObject obj)
            {
                Vector3 objPosition = obj.transform.position;
                return CurrentFloorData.OrderBy(floor =>
                    Mathf.Abs(objPosition.y - floor.FloorRoot.y))
                    .FirstOrDefault();
            }

            Dictionary<EntranceTeleport, FloorData> FireExitsWithFloorData = new Dictionary<EntranceTeleport, FloorData>();
            foreach (EntranceTeleport exit in FindFireExits())
            {
                FloorData floorData = DetermineFloor(exit.gameObject);
                if (floorData != null)
                {
                    FireExitsWithFloorData.Add(exit, floorData);
                }
            }

            foreach (KeyValuePair<EntranceTeleport, FloorData> kvp in FireExitsWithFloorData)
            {
                EntranceTeleport exit = kvp.Key;
                FloorData floorData = kvp.Value;

                foreach (var floor in CurrentFloorData)
                {
                    if (floorData == CurrentFloorData[CurrentFloorData.IndexOf(floor)])
                    {
                        CurrentFloorData[CurrentFloorData.IndexOf(floor)].FireExits.Add(exit);
                        LethalMin.Logger.LogInfo($"Added {exit.name} to {floorData.FloorTitle} floor.");
                    }
                }
            }

            LethalMin.Logger.LogInfo("Registered LC-Office Floors");
            return true;
        }

        public bool GetZelevatorFloorData()
        {
            if (FindObjectOfType<EndlessElevator>() == null)
            {
                return false;
            }

            LethalMin.Logger.LogInfo("Zelevator detected. Loading Zelevator data.");
            OverriddenIndoorTargets.Add(EndlessElevatorPatch.ElevatorPos);
            return true;
        }

        #region This is the most hackiest networking i've ever done
        public List<GameObject> SpawnedShipPhaseOnions = new List<GameObject>();
        Coroutine SSPOCoroutine;

        [ServerRpc(RequireOwnership = false)]
        public void SpawnShipPhaseOnionsServerRpc()
        {
            LethalMin.Logger.LogInfo("Requiring to spawn ship phase onions.");
            if (SSPOCoroutine != null)
            {
                LethalMin.Logger.LogWarning("Already spawning ship phase onions. Cannot cancle current coroutine!");
                return;
            }
            SSPOCoroutine = StartCoroutine(SpawnShipPhaseOnions());
        }
        public IEnumerator SpawnShipPhaseOnions()
        {
            LethalMin.Logger.LogInfo("Spawning ship phase onions.");
            List<int> LoadedOnions = new List<int>();
            Dictionary<int, int[]> LoadedFusedOnions;

            if (CollectedOnions.Count == 0)
            {
                if (LethalMin.IsUsingModLib())
                {
                    ezSaveData = new OnionEzSaveData();
                    ezSaveData.Load();

                    LoadedOnions = ezSaveData.OnionsCollected;
                    LoadedFusedOnions = ezSaveData.OnionsFused;
                }
                else
                {
                    string json = File.ReadAllText(Path.Combine(Application.persistentDataPath, $"{GetSaveFileName()}.json"));
                    LoadedOnions = JsonConvert.DeserializeObject<OnionSaveData>(json)?.OnionsCollected ?? new List<int>();
                    LoadedFusedOnions = JsonConvert.DeserializeObject<OnionSaveData>(json)?.OnionsFused ?? new Dictionary<int, int[]>();
                }
            }
            else
            {
                LoadedOnions = CollectedOnions;
                LoadedFusedOnions = FusedOnions;
            }
            // Log the loaded onions list
            LethalMin.Logger.LogInfo($"Loaded onions: {string.Join(", ", LoadedOnions)}");
            LethalMin.Logger.LogInfo($"Loaded fused onions: {string.Join(", ", LoadedFusedOnions.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}"))}");
            List<int> handledOnions = new List<int>();
            if (LoadedFusedOnions != null && LoadedFusedOnions.Count > 0)
            {
                int val1 = 0;
                foreach (var fusedOnion in LoadedFusedOnions)
                {
                    if (fusedOnion.Value.Length >= 2)
                    {
                        val1++;
                    }
                }
                SetExpectedFSPOClientRpc(val1);
                foreach (var fusedOnion in LoadedFusedOnions)
                {
                    List<OnionType> FusedTypes = new List<OnionType>();
                    if (fusedOnion.Value.Length >= 2)
                    {
                        foreach (var item in fusedOnion.Value)
                        {
                            FusedTypes.Add(LethalMin.GetOnionTypeById(item));
                        }

                        LethalMin.Logger.LogInfo("Calling to spawn fused onion: " + fusedOnion.Key);
                        SpawnFusedShipPhaseOnionClientRpc(fusedOnion.Value);

                        handledOnions.AddRange(fusedOnion.Value);
                    }
                }
            }

            foreach (int i in handledOnions)
            {
                LoadedOnions.RemoveAt(LoadedOnions.IndexOf(i));
            }
            int val2 = LoadedOnions.Count;
            SetExpectedSPOClientRpc(val2);
            List<OnionType> typesToSpawn = new List<OnionType>();
            foreach (int i in LoadedOnions)
            {
                typesToSpawn.Add(LethalMin.GetOnionTypeById(i));
            }
            foreach (OnionType item in typesToSpawn)
            {
                yield return new WaitForSeconds(1f);
                LethalMin.Logger.LogInfo("Calling to spawn onion: " + item.OnionTypeID);
                SpawnShipPhaseOnionClientRpc(item.OnionTypeID);
            }
            SSPOCoroutine = null!;
        }
        int ExpectedSPO, CurSPO, ExpectedFSPO, CurFSPO;
        [ClientRpc]
        public void SetExpectedSPOClientRpc(int expected)
        {
            ExpectedSPO = expected;
        }
        [ClientRpc]
        public void SetExpectedFSPOClientRpc(int cur)
        {
            ExpectedFSPO = cur;
        }
        [ClientRpc]
        public void SpawnFusedShipPhaseOnionClientRpc(int[] fusedOnion)
        {
            if (ExpectedFSPO <= CurFSPO) { LethalMin.Logger.LogInfo("Client already SpawnedSPFusedOnions"); return; }
            LethalMin.Logger.LogInfo("Spawning fused onion: " + string.Join(", ", fusedOnion));
            List<OnionType> FusedTypes = new List<OnionType>();
            foreach (var item in fusedOnion)
            {
                FusedTypes.Add(LethalMin.GetOnionTypeById(item));
            }

            GameObject instance = Instantiate(
                AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/ShipModeOnion.prefab"), OnionContainer.transform);
            SpawnedShipPhaseOnions.Add(instance);

            Renderer onionRenderer = instance.transform.Find("SK_stg_OnyonCarry.001").GetComponent<Renderer>();

            List<Color> colors = new List<Color>();

            colors.Add(FusedTypes[0].OnionColor);
            foreach (var item in FusedTypes)
            {
                colors.Add(item.OnionColor);
            }

            Texture2D gradient = GradientTextureGenerator.Generate90DegreeGradient(colors, 0.1f);

            onionRenderer.material.color = Color.white;
            onionRenderer.material.SetTexture("_BaseColorMap", gradient);

            CurFSPO++;
        }
        [ClientRpc]
        public void SpawnShipPhaseOnionClientRpc(int onionTypeId)
        {
            if (ExpectedSPO <= CurSPO) { LethalMin.Logger.LogInfo("Client already SpawnedSPOnions"); return; }
            LethalMin.Logger.LogInfo("Spawning onion: " + onionTypeId);
            OnionType item = LethalMin.GetOnionTypeById(onionTypeId);
            GameObject instance = Instantiate(
                AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/ShipModeOnion.prefab"), OnionContainer.transform);
            SpawnedShipPhaseOnions.Add(instance);
            instance.transform.Find("SK_stg_OnyonCarry.001").GetComponent<Renderer>().material.color = item.OnionColor;

            CurSPO++;
        }

        [ClientRpc]
        public void DespawnShipPhaseOnionsClientRpc()
        {
            foreach (var item in SpawnedShipPhaseOnions)
            {
                Destroy(item);
            }
            SpawnedShipPhaseOnions.Clear();
            CurSPO = 0;
            CurFSPO = 0;
        }
        #endregion

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
                    PikminType pikminType = DeterminePikminTypeOutdoor(RoundManager.Instance.currentLevel);
                    if (pikminType.SpawnsAsSprout)
                    {
                        Transform pos2 = spawnPoint.transform;
                        GameObject SproutInstance2 = Instantiate(LethalMin.sproutPrefab, pos2.position, pos2.rotation);
                        Sprout SproteScript2 = SproutInstance2.GetComponent<Sprout>();
                        SproteScript2.NetworkObject.Spawn();
                        SproteScript2.InitalizeTypeClientRpc(pikminType.PikminTypeID);
                        SproteScript2.AdjustPositionClientRpc();
                    }
                    else
                    {
                        Transform pos = spawnPoint.transform;
                        GameObject SproutInstance = Instantiate(LethalMin.pikminPrefab, pos.position, pos.rotation);
                        PikminAI SproteScript = SproutInstance.GetComponent<PikminAI>();
                        SproteScript.isOutside = false;
                        SproteScript.NetworkObject.Spawn();
                        SpawnPikminClientRpc(new NetworkObjectReference(SproteScript.NetworkObject));
                        CreatePikminClientRPC(new NetworkObjectReference(SproteScript.NetworkObject), pikminType.PikminTypeID, true);
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
                    PikminType pikminType = DeterminePikminTypeIndoor();
                    if (pikminType.SpawnsAsSprout)
                    {
                        Transform pos2 = spawnPoint.transform;
                        GameObject SproutInstance2 = Instantiate(LethalMin.sproutPrefab, pos2.position, pos2.rotation);
                        Sprout SproteScript2 = SproutInstance2.GetComponent<Sprout>();
                        SproteScript2.NetworkObject.Spawn();
                        SproteScript2.InitalizeTypeClientRpc(pikminType.PikminTypeID);
                        SproteScript2.AdjustPositionClientRpc();
                    }
                    else
                    {
                        Transform pos = spawnPoint.transform;
                        GameObject SproutInstance = Instantiate(LethalMin.pikminPrefab, pos.position, pos.rotation);
                        PikminAI SproteScript = SproutInstance.GetComponent<PikminAI>();
                        SproteScript.isOutside = false;
                        SproteScript.NetworkObject.Spawn();
                        SpawnPikminClientRpc(new NetworkObjectReference(SproteScript.NetworkObject));
                        CreatePikminClientRPC(new NetworkObjectReference(SproteScript.NetworkObject), pikminType.PikminTypeID, false);
                    }
                }
            }
            yield return new WaitForSeconds(0.5f); // Short delay to ensure all spawns are complete
            CleanupExcessPikmin();
        }
        public static List<GameObject> _BridgeColiders = new List<GameObject>();
        IEnumerator AddPikminBridge()
        {
            yield return new WaitUntil(() => StartOfRound.Instance.fullyLoadedPlayers.Count >= GameNetworkManager.Instance.connectedPlayers);
            yield return new WaitUntil(() => RoundManager.Instance.dungeonCompletedGenerating);
            _BridgeColiders.Clear();

            foreach (var bridge in GameObject.FindObjectsOfType<BridgeTrigger>())
            {
                if (bridge.GetComponent<PikminBridgeTrigger>() == null)
                {
                    bridge.gameObject.AddComponent<PikminBridgeTrigger>();
                    _BridgeColiders.AddRange(bridge.transform.GetComponentsInChildren<GameObject>());
                    LethalMin.Logger.LogInfo("Added PikminBridge component to " + bridge.name);
                }
            }
            foreach (var bridge in GameObject.FindObjectsOfType<BridgeTriggerType2>())
            {
                if (bridge.GetComponent<PikminBridgeTrigger>() == null)
                {
                    bridge.gameObject.AddComponent<PikminBridgeTrigger>();
                    _BridgeColiders.AddRange(bridge.transform.GetComponentsInChildren<GameObject>());
                    LethalMin.Logger.LogInfo("Added {ol,oi} component to " + bridge.name);
                }
            }
        }

        private PikminType DeterminePikminTypeOutdoor(SelectableLevel level)
        {
            Dictionary<PikminType, float> typeWeights = new Dictionary<PikminType, float>();

            // Initialize weights for all outdoor types
            foreach (var type in LethalMin.OutdoorTypes.Values)
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
                var fireResistantTypes = LethalMin.OutdoorTypes.Values.Where(t => LethalMin.IsPikminResistantToHazard(t, HazardType.Fire));
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
                var waterResistantTypes = LethalMin.OutdoorTypes.Values.Where(t => LethalMin.IsPikminResistantToHazard(t, HazardType.Water));
                foreach (var type in waterResistantTypes)
                {
                    typeWeights[type] *= 2f; // Increase chance for water-resistant Pikmin
                }
            }

            // Check for electric-related enemies or hazards
            bool hasElectricHazards = level.currentWeather == LevelWeatherType.Stormy;
            if (hasElectricHazards)
            {
                var electricResistantTypes = LethalMin.OutdoorTypes.Values.Where(t => LethalMin.IsPikminResistantToHazard(t, HazardType.Electric));
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

        private PikminType DeterminePikminTypeIndoor()
        {
            Dictionary<PikminType, float> typeWeights = new Dictionary<PikminType, float>();

            // Initialize weights for all outdoor types
            foreach (var type in LethalMin.IndoorTypes.Values)
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

            List<PikminAI> allPikmin = GetPikminEnemies();
            int excessCount = allPikmin.Count - LethalMin.MaxMinValue;

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
                        
                        if (pikmin == null) { continue; }
                        
                        if (pikmin.TargetOnion != null)
                        {
                            SendPikminToOnion(pikmin);
                        }
                        else if (pikmin.NetworkObject != null && pikmin.NetworkObject.IsSpawned)
                        {
                            DespawnPikminClientRpc(pikmin.NetworkObject);
                        }
                    }
                }
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

        [ServerRpc(RequireOwnership = false)]
        public void SpawnInPikminServerRpc(Vector3 position, Quaternion rotation, NetworkObjectReference leader, int GrowStage, int pikminTypeID, bool IsOutside, string Name)
        {
            GameObject pikminPrefab = LethalMin.pikminPrefab;

            GameObject pikminObj = GameObject.Instantiate(pikminPrefab, position, rotation);
            NetworkObject networkObject = pikminObj.GetComponent<NetworkObject>();
            PikminAI pikminAI = pikminObj.GetComponent<PikminAI>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                SpawnPikminClientRpc(networkObject);
            }
            else
            {
                LethalMin.Logger.LogError("NetworkObject component not found on Pikmin prefab.");
            }

            SpawnInPikminClientRPC(pikminAI.NetworkObject, leader, GrowStage, pikminTypeID, IsOutside, Name);
        }

        [ClientRpc]
        public void SpawnInPikminClientRPC(NetworkObjectReference network1, NetworkObjectReference network2, int growStage, int pikminTypeId, bool isOutside, string Name)
        {
            network1.TryGet(out NetworkObject PikObj);
            PikminAI script = PikObj.GetComponent<PikminAI>();
            if (script == null) { return; }
            script.HideMeshOnStart = false;
            script.GrowStage = growStage;
            script.PreDefinedType = true;
            script.PminType = LethalMin.GetPikminTypeById(pikminTypeId);

            network2.TryGet(out NetworkObject PlaObj);
            PlayerControllerB pl = PlaObj.GetComponent<PlayerControllerB>();

            StartCoroutine(waitForPikmin(script, pl, isOutside, Name));
        }
        public IEnumerator waitForPikmin(PikminAI ai, PlayerControllerB player, bool isOutside, string Name)
        {
            while (!ai.HasInitalized)
            {
                yield return new WaitForSeconds(0.1f);
            }

            ai.isOutside = isOutside;
            ai.uniqueDebugId = Name;
            ai.gameObject.name = Name;

            if (player != null)
            {
                ai.AssignLeader(player, false);
            }
            else
            {
                LethalMin.Logger.LogError("attempted to assing a null leader.");
            }
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
        private static List<PikminItem> _currentPikminItemsInMap = new List<PikminItem>();
        private static List<PikminItem> _nextPikminItemsInMap = new List<PikminItem>();
        private static List<EnemyAI> _currentNonPikminEnemies = new List<EnemyAI>();
        private static List<EnemyAI> _nextNonPikminEnemies = new List<EnemyAI>();
        private static List<PikminAI> _currentPikminEnemies = new List<PikminAI>();
        private static List<PikminAI> _nextPikminEnemies = new List<PikminAI>();
        private static List<PuffminAI> _currentPuffminEnemies = new List<PuffminAI>();
        private static List<PuffminAI> _nextPuffminEnemies = new List<PuffminAI>();
        public static Onion[] _currentOnions = new Onion[0];
        public static VehicleController[] _currentCars = new VehicleController[0];
        private static object _listLock = new object();
        public static List<PikminItem> PikminItemsExclusion = new List<PikminItem>();

        public static void RefreshPikminItemsInMapList()
        {
            _nextPikminItemsInMap.Clear();

            // Refresh Pikmin Items
            PikminItem[] allGrabbables = UnityEngine.Object.FindObjectsOfType<PikminItem>();
            foreach (PikminItem grabbable in allGrabbables)
            {
                if (grabbable == null || grabbable.Root == null) continue;
                if (PikminItemsExclusion.Contains(grabbable)) continue;
                if (!grabbable.Root.deactivated)
                {
                    _nextPikminItemsInMap.Add(grabbable);
                }
            }
        }
        public static void RefreshNonPikminEnemiesList()
        {
            _nextNonPikminEnemies.Clear();
            // Refresh Non-Pikmin Enemies
            EnemyAI[] allEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            foreach (EnemyAI enemy in allEnemies)
            {
                if (enemy == null) continue;
                if (enemy.enemyType == null) continue;
                if (!enemy.enemyType.canDie) continue;
                if (enemy.enemyType != LethalMin.pikminEnemyType) // Check if it doesn't have PikminAI component
                {
                    _nextNonPikminEnemies.Add(enemy);
                }
            }
        }
        public static void RefreshPikminList()
        {
            _nextPikminEnemies.Clear();
            // Refresh Pikmin Enemies
            PikminAI[] allEnemies = UnityEngine.Object.FindObjectsOfType<PikminAI>();
            foreach (PikminAI Pikmin in allEnemies)
            {
                if (Pikmin == null) continue;
                _nextPikminEnemies.Add(Pikmin);
            }
            PikminManager.Instance.CleanupExcessPikmin();
        }
        public static void RefreshOnionsList()
        {
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
        }
        public static void RefreshCarsList()
        {
            // Refresh Cars
            if (LethalMin.TargetCar)
            {
                VehicleController[] allCars = UnityEngine.Object.FindObjectsOfType<VehicleController>();
                _currentCars = allCars;
            }
        }
        public static void RefreshPuffminList()
        {
            _nextPuffminEnemies.Clear();
            // Refresh Puffmin Enemies
            PuffminAI[] allPuffmin = UnityEngine.Object.FindObjectsOfType<PuffminAI>();
            foreach (PuffminAI puffmin in allPuffmin)
            {
                if (puffmin == null) continue;
                _nextPuffminEnemies.Add(puffmin);
            }
        }

        public static void RefreshLocks()
        {
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

                var tempPuffminEnemies = _currentPuffminEnemies;
                _currentPuffminEnemies = _nextPuffminEnemies;
                _nextPuffminEnemies = tempPuffminEnemies;
            }

            //LethalMin.Logger.LogInfo($"Refreshed PikminItemsInMap. Current count: {_currentPikminItemsInMap.Count}");
            //LethalMin.Logger.LogInfo($"Refreshed puffminEnemies. Current count: {_currentPuffminEnemies.Count}");
            //LethalMin.Logger.LogInfo($"Refreshed NonPikminEnemies. Current count: {_currentNonPikminEnemies.Count}");
        }

        public static List<PikminItem> GetPikminItemsInMap()
        {
            lock (_listLock)
            {
                return new List<PikminItem>(_currentPikminItemsInMap);
            }
        }

        public static List<EnemyAI> GetNonPikminEnemies()
        {
            lock (_listLock)
            {
                return new List<EnemyAI>(_currentNonPikminEnemies);
            }
        }

        public static List<PikminAI> GetPikminEnemies()
        {
            lock (_listLock)
            {
                return new List<PikminAI>(_currentPikminEnemies);
            }
        }

        public static List<PuffminAI> GetPuffminEnemies()
        {
            lock (_listLock)
            {
                return new List<PuffminAI>(_currentPuffminEnemies);
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

        private static ulong currentEnemy = 9999999;
        [ServerRpc(RequireOwnership = false)]
        public void CreateItemNodeOnBodyServerRpc(NetworkObjectReference EnemyRef)
        {
            if (LethalMin.IsDependencyLoaded("Entity378.sellbodies")) return;

            EnemyAI __instance = null!;

            EnemyRef.TryGet(out NetworkObject enemyNetObj);
            if (enemyNetObj != null)
            {
                __instance = enemyNetObj.GetComponent<EnemyAI>();
            }

            if (currentEnemy == __instance.NetworkObject.NetworkObjectId) return;
            if (__instance.GetComponentInChildren<PlayerControllerB>()) return;

            currentEnemy = __instance.NetworkObject.NetworkObjectId;

            LethalMin.Logger.LogInfo("Creating item node on enemy body " + __instance.gameObject.name);

            __instance.moveTowardsDestination = false;
            __instance.movingTowardsTargetPlayer = false;
            __instance.updatePositionThreshold = 9000;
            __instance.syncMovementSpeed = 0f;

            Item Iprops = ScriptableObject.CreateInstance(typeof(Item)) as Item;
            Iprops.restingRotation = __instance.transform.rotation.eulerAngles;
            Iprops.itemName = __instance.enemyType.enemyName;

            if (__instance.enemyType.canDie && EnemyAIPatch.HPDict.ContainsKey(__instance) && EnemyAIPatch.HPDict[__instance] > 0f)
            {
                if (EnemyAIPatch.HPDict[__instance] <= 1f)
                {
                    Iprops.weight = 0;
                }
                else
                {
                    Iprops.weight = EnemyAIPatch.HPDict[__instance] * 0.4f;
                }
            }
            else
            {
                Iprops.weight = Mathf.Clamp((float)(__instance.enemyType.PowerLevel * 0.6), 1f, 5f);
            }

            PhysicsProp prop = __instance.gameObject.AddComponent<PhysicsProp>();
            prop.itemProperties = Iprops;
            prop.grabbable = false;
            SyncBodyPropClientRpc(__instance.NetworkObject, Iprops.weight, Iprops.restingRotation);
            PikminItem Pitem = EnemyAIPatch.CreatePikminItemForBody(prop);
            SyncBodyItemNodeClientRpc(Pitem.NetworkObject);
        }

        [ClientRpc]
        public void SyncBodyPropClientRpc(NetworkObjectReference EnemyRef, float Weight, Vector3 RestingRotation)
        {
            bool shouldothrick = false;
            if (IsServer)
                return;
            EnemyAI __instance = null!;
            EnemyRef.TryGet(out NetworkObject enemyNetObj);
            if (enemyNetObj != null)
            {
                __instance = enemyNetObj.GetComponent<EnemyAI>();
            }
            if (__instance.enabled)
            {
                __instance.enabled = false;
                shouldothrick = true;
            }
            currentEnemy = __instance.NetworkObject.NetworkObjectId;

            LethalMin.Logger.LogInfo("Creating item node on enemy body on the client side " + __instance.gameObject.name);

            __instance.moveTowardsDestination = false;
            __instance.movingTowardsTargetPlayer = false;
            __instance.updatePositionThreshold = 9000;
            __instance.syncMovementSpeed = 0f;

            Item Iprops = ScriptableObject.CreateInstance(typeof(Item)) as Item;
            Iprops.restingRotation = RestingRotation;
            Iprops.itemName = __instance.enemyType.enemyName;

            Iprops.weight = Weight;

            PhysicsProp prop = __instance.gameObject.AddComponent<PhysicsProp>();
            prop.itemProperties = Iprops;
            prop.grabbable = false;
            if (shouldothrick)
            {
                __instance.enabled = true;
            }
        }

        [ClientRpc]
        public void SyncBodyItemNodeClientRpc(NetworkObjectReference PminRef)
        {
            if (IsServer)
                return;
            PikminItem pikminItem = null!;
            PminRef.TryGet(out NetworkObject pminNetObj);
            if (pminNetObj != null)
            {
                pikminItem = pminNetObj.GetComponent<PikminItem>();
            }
            pikminItem.CanBeConvertedIntoSprouts = true;
            pikminItem.UsePikminAsParent = true;
            pikminItem.DontParentToObjects = true;
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
                PLLR.Value = ezSaveData.PikminLeftLastRound;
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
        public OnionEzSaveData ezSaveData = null;
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

            // Sprout[] sprouts = FindObjectsOfType<Sprout>();

            // // Add existing sprouts to newSaveData
            // foreach (SproutData Sdata in existingSaveData.Sprouts)
            // {
            //     if (Sdata.SceneName != RoundManager.Instance.currentLevel.sceneName)
            //     {
            //         newSaveData.Sprouts.Add(Sdata);
            //     }
            // }

            // // Save Sprout data
            // foreach (Sprout sprout in sprouts)
            // {
            //     if (sprout.IsSaved)
            //     {
            //         SproutData sproutData = new SproutData();
            //         sproutData.GrowStage = 0;
            //         sproutData.SceneName = RoundManager.Instance.currentLevel.sceneName;
            //         sproutData.Position = new SerializableVector3(sprout.transform.position);
            //         sproutData.Rotation = new SerializableQuaternion(sprout.transform.rotation);
            //         sproutData.PikminTypeID = sprout.PminType.PikminTypeID;
            //         newSaveData.Sprouts.Add(sproutData);
            //     }
            // }

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
            OnionEzSaveData existingSaveData = LoadExistingEzSaveData();
            OnionEzSaveData newSaveData = new OnionEzSaveData();
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

            // Sprout[] sprouts = FindObjectsOfType<Sprout>();

            // // Add existing sprouts to newSaveData
            // foreach (SproutData Sdata in existingSaveData.Sprouts)
            // {
            //     if (Sdata.SceneName != RoundManager.Instance.currentLevel.sceneName)
            //     {
            //         newSaveData.Sprouts.Add(Sdata);
            //     }
            // }

            // // Save Sprout data
            // foreach (Sprout sprout in sprouts)
            // {
            //     if (sprout.IsSaved)
            //     {
            //         SproutData sproutData = new SproutData();
            //         sproutData.GrowStage = 0;
            //         sproutData.SceneName = RoundManager.Instance.currentLevel.sceneName;
            //         sproutData.Position = new SerializableVector3(sprout.transform.position);
            //         sproutData.Rotation = new SerializableQuaternion(sprout.transform.rotation);
            //         sproutData.PikminTypeID = sprout.PminType.PikminTypeID;
            //         newSaveData.Sprouts.Add(sproutData);
            //     }
            // }

            LethalMin.Logger.LogInfo($"IsStoredNull = {newSaveData.PikminStored == null}");

            newSaveData.Save();

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
        private OnionEzSaveData LoadExistingEzSaveData()
        {
            OnionEzSaveData NewSaveData = new OnionEzSaveData();
            NewSaveData.Load();
            return NewSaveData;
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
            //Load ez save
            ezSaveData = new OnionEzSaveData();
            ezSaveData.Load();

            CollectedOnions = ezSaveData.OnionsCollected;
            FusedOnions = ezSaveData.OnionsFused;

            if (FindObjectOfType<DualOnion>() != null)
            {
                var OnionInstace = FindObjectOfType<DualOnion>();
                var pikminStorage = ezSaveData.PikminStored.FirstOrDefault(storage => storage.ID == OnionInstace.type.OnionTypeID);
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
                PuffminAI puffminAI = networkObject.GetComponent<PuffminAI>();
                if (puffminAI != null)
                {
                    if (RoundManager.Instance.SpawnedEnemies.Contains(puffminAI))
                    {
                        RoundManager.Instance.SpawnedEnemies.Remove(puffminAI);
                        if (LethalMin.DebugMode)
                        {
                            LethalMin.Logger.LogInfo($"Removed Puffmin {puffminAI.name} from RoundManager");
                        }
                    }
                    if (IsServer)
                        networkObject.Despawn(true);
                }
            }
        }
        [ClientRpc]
        public void DespawnPikminClientRpc(NetworkObjectReference[] networkObjectRefz)
        {
            if (RoundManager.Instance == null) { return; }
            int Batch = 0;
            foreach (NetworkObjectReference networkObjectRef in networkObjectRefz)
            {
                Batch++;
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
                                LethalMin.Logger.LogInfo($"Removed Pikmin {pikminAI.name} from RoundManager Batch #{Batch}");
                            }
                        }
                        if (IsServer)
                            networkObject.Despawn(true);
                    }
                    PuffminAI puffminAI = networkObject.GetComponent<PuffminAI>();
                    if (puffminAI != null)
                    {
                        if (RoundManager.Instance.SpawnedEnemies.Contains(puffminAI))
                        {
                            RoundManager.Instance.SpawnedEnemies.Remove(puffminAI);
                            if (LethalMin.DebugMode)
                            {
                                LethalMin.Logger.LogInfo($"Removed Puffmin {puffminAI.name} from RoundManager Batch #{Batch}");
                            }
                        }
                        if (IsServer)
                            networkObject.Despawn(true);
                    }
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
                PuffminAI puffminAI = networkObject.GetComponent<PuffminAI>();
                if (puffminAI != null)
                {
                    if (!RoundManager.Instance.SpawnedEnemies.Contains(puffminAI))
                    {
                        RoundManager.Instance.SpawnedEnemies.Add(puffminAI);
                        if (LethalMin.DebugMode)
                        {
                            LethalMin.Logger.LogInfo($"Added Puffmin {puffminAI.name} to RoundManager");
                        }
                    }
                }
            }
        }
        [ClientRpc]
        public void SpawnPikminClientRpc(NetworkObjectReference[] networkObjectRefz)
        {
            if (RoundManager.Instance == null) { return; }
            int Batch = 0;
            foreach (NetworkObjectReference networkObjectRef in networkObjectRefz)
            {
                Batch++;
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
                                LethalMin.Logger.LogInfo($"Added Pikmin {pikminAI.name} to RoundManager Batch #{Batch}");
                            }
                        }
                    }
                    PuffminAI puffminAI = networkObject.GetComponent<PuffminAI>();
                    if (puffminAI != null)
                    {
                        if (!RoundManager.Instance.SpawnedEnemies.Contains(puffminAI))
                        {
                            RoundManager.Instance.SpawnedEnemies.Add(puffminAI);
                            if (LethalMin.DebugMode)
                            {
                                LethalMin.Logger.LogInfo($"Added Puffmin {puffminAI.name} to RoundManager Batch #{Batch}");
                            }
                        }
                    }
                }
            }
        }


        public float ShipPickupRange = 20f;
        public float OnionPickupRange = 20f;
        public List<GrabbableObject> ObjectsToDestroyOnLeave = new List<GrabbableObject>();
        public void HandlePikminWhenShipLeaving()
        {
            RemoveAllRadiuses();
            if (!IsServer) { return; }
            foreach (var item in ObjectsToDestroyOnLeave)
            {
                if (item.isInElevator || item.isInShipRoom)
                {
                    Destroy(item);
                }
            }
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

        public bool CreatedSafetyRings = false;
        public void CreateSafetyRings()
        {
            if (CreatedSafetyRings || !LethalMin.ShowSafety) { return; }
            CreatedSafetyRings = true;
            //Create Radiuses around each onion and the ship
            Onion[] onions = UnityEngine.Object.FindObjectsOfType<Onion>();
            foreach (var onion in onions)
            {
                CreateRadius(onion.transform.position, 20);
            }
            CreateRadius(StartOfRound.Instance.elevatorTransform.position, 20f);
        }

        #endregion

        #region Post-Game

        List<GameObject> Radiuses = new List<GameObject>();
        public void RemoveAllRadiuses()
        {
            CreatedSafetyRings = false;
            foreach (var item in Radiuses)
            {
                Destroy(item);
            }
        }
        public void CreateRadius(Vector3 position, float radius)
        {
            GameObject radiusObject = new GameObject($"Radius ({Radiuses.Count})");
            radiusObject.transform.position = position;
            radiusObject.transform.rotation = Quaternion.Euler(90, 0, 0);
            radiusObject.AddComponent<LineRenderer>();
            radiusObject.AddComponent<CircleRenderer>().radius = radius;
            Radiuses.Add(radiusObject);
            LethalMin.Logger.LogInfo($"Created radius at {position} with radius {radius}");
        }
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

        public IEnumerator DespawnSprouts()
        {
            if (!IsServer) { yield return null; }
            while (IsSaving)
            {
                yield return new WaitForSeconds(1f);
            }
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
            else if (LethalMin.IsUsingModLib() && ezSaveData != null && CollectedOnions.Count > 0)
            {
                foreach (int onionId in ezSaveData.OnionsCollected)
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
                    pikminStorage = ezSaveData.PikminStored.FirstOrDefault(storage => storage.ID == onionType.OnionTypeID);
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
                        pikminStorage.Add(ezSaveData.PikminStored.FirstOrDefault(storage => storage.ID == ID));
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

            for (int i = 1; i < LethalMin.RegisteredOnionTypes.Count; i++) // Assuming we're looking for up to 3 spawn points
            {
                GameObject spawnPoint = GameObject.Find($"ONION_SPAWN_POINT_{i}");
                if (spawnPoint != null)
                {
                    specificSpawnPoints.Add(spawnPoint.transform.position);
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