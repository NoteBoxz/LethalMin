using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public abstract class Onion : NetworkBehaviour
    {
        public OnionType onionType = null!;
        public List<OnionType> fusedTypes = new List<OnionType>();
        public List<PikminData> PikminInOnion = new List<PikminData>();
        public bool DontChooseRandomType = false;
        public bool DontDespawnOnGameEnd = false;
        public Dictionary<PikminType, int> TypesToExchange = new Dictionary<PikminType, int>();
        public Transform PikminSpawnPos = null!;
        public Transform SproutSpawnPos = null!;
        public Transform ItemDropPos = null!;
        public List<PikminLinkAnimation> ClimbLinks = null!;
        public List<PikminLinkAnimation> AllClimbLinks = null!;
        public Dictionary<PikminType, int> TypesToSpawn = new Dictionary<PikminType, int>();
        public float SproutSpawnTimer = 1f;
        public Coroutine? SpawnSproutsRoutine = null;
        public Dictionary<ulong, Coroutine?> SpawnedPikminRoutines = new Dictionary<ulong, Coroutine?>();
        bool CreatedFusedInstance = false;
        float sTimer;

        public virtual void Start()
        {
            if (onionType == null && DontChooseRandomType == false)
            {
                System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + PikminManager.instance.Onions.Count);
                onionType = LethalMin.RegisteredOnionTypes[random.Next(0, LethalMin.RegisteredOnionTypes.Count)];
                LethalMin.Logger.LogInfo($"Onion {gameObject.name} has been assigned a random type: {onionType.name}");
            }
            //so my IDE doesn't wine about possible null refence
            if (onionType == null)
            {
                LethalMin.Logger.LogError($"Onion type {gameObject.name} is some how null????????????!?!?!??!??!?!");
                return;
            }

            gameObject.name = $"{onionType.TypeName}";

            foreach (PikminType type in LethalMin.RegisteredPikminTypes.Values)
            {
                TypesToSpawn.Add(type, 0);
            }

            foreach (PikminType type in onionType.TypesCanHold)
            {
                TypesToExchange.Add(type, 0);
            }

            TypesToExchange = TypesToExchange.OrderBy(x => x.Key.PikminName).ToDictionary(x => x.Key, x => x.Value);

            PikminManager.instance.AddOnion(this);
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            PikminManager.instance.RemoveOnion(this);
            if (CreatedFusedInstance)
            {
                Destroy(onionType);
            }
        }

        public virtual void Update()
        {
            if (IsServer)
            {
                if (sTimer > 0 && SpawnSproutsRoutine == null)
                {
                    sTimer -= Time.deltaTime;
                    if (sTimer <= 0)
                    {
                        SpawnSproutsClientRpc();
                    }
                }
            }
        }


        #region Spawning
        [ClientRpc]
        public void InitalizeTypeClientRpc(int TypeID)
        {
            OnionType oT = LethalMin.GetOnionTypeByID(TypeID);
            if (oT != null)
            {
                onionType = oT;
            }
            else
            {
                LethalMin.Logger.LogError($"Invalid onion type: {TypeID} when initalizeing type!");
                DontChooseRandomType = false;
            }
        }

        [ClientRpc]
        public void InitalizeTypeClientRpc(int[] TypeIDs)
        {
            OnionType fusedType = (OnionType)ScriptableObject.CreateInstance(typeof(OnionType));
            fusedType.OnionTypeID = -1;
            List<PikminType> FusedTypesCanHold = new List<PikminType>();
            fusedType.TypeName = $"FusedType: {PikUtils.ParseListToString(TypeIDs)}";
            fusedType.name = $"FusedType: {PikUtils.ParseListToString(TypeIDs)}";
            for (int i = 0; i < TypeIDs.Length; i++)
            {
                OnionType oT = LethalMin.GetOnionTypeByID(TypeIDs[i]);

                if (oT == null)
                {
                    LethalMin.Logger.LogError($"Invalid onion type: {TypeIDs[i]} when initalizeing type!");
                    DontChooseRandomType = false;
                    return;
                }

                fusedTypes.Add(oT);
                FusedTypesCanHold.AddRange(oT.TypesCanHold);
            }
            CreatedFusedInstance = true;
            fusedType.TypesCanHold = FusedTypesCanHold.ToArray();
            onionType = fusedType;
        }

        [ServerRpc(RequireOwnership = false)]
        public virtual void WithdrawTypesServerRpc(NetworkObjectReference onionRef, ulong playerID, int[] PikIDkeys, int[] QuantityValues)
        {
            Leader? leaderWithdrawing = LethalMin.GetLeaderViaID(playerID);
            if (leaderWithdrawing == null)
            {
                LethalMin.Logger.LogError($"Unable to get player from ID: {playerID}");
                ReturnErrorWhenWithdrawingClientRpc("Unable to get player from ID.");
                return;
            }
            if (!onionRef.TryGet(out NetworkObject OnionNG) && !OnionNG.TryGetComponent(out Onion onion))
            {
                LethalMin.Logger.LogError($"Unable to get onion!");
                ReturnErrorWhenWithdrawingClientRpc("Unable to get onion.");
                return;
            }
            try
            {
                List<PikminType> typesWithdrawing = new List<PikminType>();
                foreach (int ID in PikIDkeys)
                {
                    typesWithdrawing.Add(LethalMin.GetPikminTypeByID(ID));
                }

                LethalMin.Logger.LogInfo($"{leaderWithdrawing.Controller.playerUsername}: withdrawing" +
                $" ({PikUtils.ParseListToString(QuantityValues)})," +
                $" of ({PikUtils.ParseListToString(typesWithdrawing.Select(p => p.PikminName).ToList())})");
                if (SpawnedPikminRoutines.ContainsKey(playerID))
                {
                    if (SpawnedPikminRoutines[playerID] != null)
                    {
                        StopCoroutine(SpawnedPikminRoutines[playerID]);
                        SpawnedPikminRoutines[playerID] = null;
                    }
                }
                SpawnedPikminRoutines[playerID] = StartCoroutine(SpawnedInterval(typesWithdrawing, QuantityValues, leaderWithdrawing));
            }
            catch (System.Exception ex)
            {
                LethalMin.Logger.LogError($"Error withdrawing types: {ex.Message}");
                ReturnErrorWhenWithdrawingClientRpc(ex.Message);
            }
        }

        [ClientRpc]
        public void ReturnErrorWhenWithdrawingClientRpc(string issue)
        {
            if (IsServer)
            {
                return;
            }
            LethalMin.Logger.LogError($"Error from server withdrawing types: {issue}");
        }

        public virtual IEnumerator SpawnedInterval(List<PikminType> typesWithdrawing, int[] QuantityValues, Leader Leader)
        {
            try
            {
                // First handle sending Pikmin to the onion (negative values)
                yield return SendPikminToOnion(typesWithdrawing, QuantityValues, Leader);

                // Then handle withdrawing Pikmin from the onion (positive values)
                yield return WithdrawPikminFromOnion(typesWithdrawing, QuantityValues, Leader);
            }
            finally
            {
                // Ensure we always clean up the coroutine reference
                SpawnedPikminRoutines[Leader.Controller.OwnerClientId] = null;
            }
        }

        private IEnumerator SendPikminToOnion(List<PikminType> types, int[] quantities, Leader leader)
        {
            for (int i = 0; i < types.Count; i++)
            {
                if (quantities[i] >= 0) continue;

                PikminType type = types[i];
                int count = -quantities[i];

                for (int l = 0; l < count; l++)
                {
                    PikminAI? ai = leader.GetClosestPikminInSquadOfType(type);
                    if (ai != null)
                    {
                        LethalMin.Logger.LogInfo($"Setting {ai.DebugID} to the onion");
                        ai.SetPikminToLeavingClientRpc(NetworkObject);
                    }
                    else
                    {
                        LethalMin.Logger.LogWarning($"Failed to find {type.PikminName} in {leader.Controller.playerUsername}'s squad");
                        break; // Stop trying if we can't find any more of this type
                    }
                    yield return new WaitForSeconds(0.01f);
                }
            }
        }

        private IEnumerator WithdrawPikminFromOnion(List<PikminType> types, int[] quantities, Leader leader)
        {
            const int BATCH_SIZE = 3;
            // so due to an oversight (both numbers in the devision equasion were ints), there was no delay between batches.
            const float BATCH_DELAY = 0.05f;
            const float PIKMIN_DELAY = 0.01f;

            for (int i = 0; i < types.Count; i++)
            {
                if (quantities[i] <= 0) continue;

                PikminType type = types[i];
                int requestedCount = quantities[i];

                // Find available Pikmin of this type, sorted by growth stage
                var pikminOfType = PikminInOnion
                    .Select((pikmin, index) => new { Pikmin = pikmin, Index = index })
                    .Where(p => LethalMin.GetPikminTypeByID(p.Pikmin.TypeID) == type)
                    .OrderByDescending(p => p.Pikmin.GrowthStage)
                    .ToList();

                // Check if we have enough Pikmin
                if (pikminOfType.Count < requestedCount)
                {
                    LethalMin.Logger.LogWarning($"Not enough {type.PikminName} in onion. Requested: {requestedCount}, Available: {pikminOfType.Count}");
                    continue;
                }

                // Process in batches
                for (int batchStart = 0; batchStart < requestedCount; batchStart += BATCH_SIZE)
                {
                    int batchCount = Mathf.Min(BATCH_SIZE, requestedCount - batchStart);
                    yield return new WaitForSeconds(BATCH_DELAY);

                    // Check field capacity before spawning batch
                    if (IsFieldAtMaxCapacity())
                    {
                        yield return WaitForFieldCapacity(requestedCount - batchStart);
                    }

                    // Spawn the batch
                    for (int j = 0; j < batchCount; j++)
                    {
                        int index = batchStart + j;
                        SpawnPikmin(pikminOfType[index].Pikmin, leader.Controller.OwnerClientId);
                        yield return new WaitForSeconds(PIKMIN_DELAY);
                    }
                }
            }
        }

        private bool IsFieldAtMaxCapacity()
        {
            return PikminManager.instance.PikminAICounter.Count >= LethalMin.MaxPikmin.InternalValue;
        }

        private IEnumerator WaitForFieldCapacity(int neededSpace)
        {
            LethalMin.Logger.LogInfo($"Field at max capacity ({PikminManager.instance.PikminAICounter.Count}+{neededSpace}/{LethalMin.MaxPikmin.InternalValue}). Waiting for space...");

            float waitStartTime = Time.time;
            const float MAX_WAIT_TIME = 10f;

            while (Time.time - waitStartTime < MAX_WAIT_TIME)
            {
                if (PikminManager.instance.PikminAICounter.Count + neededSpace <= LethalMin.MaxPikmin.InternalValue)
                {
                    LethalMin.Logger.LogInfo("Space available, continuing with spawn");
                    yield break; // Exit early if space becomes available
                }
                yield return new WaitForSeconds(0.5f);
            }

            LethalMin.Logger.LogInfo("Waited 10 seconds, spawning Pikmin anyway");
        }

        /// <summary>
        /// Should only be called on the server (ofc)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="PlayerSpawning"></param>
        public virtual void SpawnPikmin(PikminData data, ulong PlayerSpawning)
        {
            PikminSpawnProps props = new PikminSpawnProps();
            props.GrowthStage = data.GrowthStage;
            props.PlayerID = PlayerSpawning;
            props.SpawnSound = "ExitOnion";
            props.OverrideVolume = 1;
            props.OverrideDebugID = data.DebugName;
            props.OverrideBirthDate = data.BirthDate;
            PikminType pikminType = LethalMin.GetPikminTypeByID(data.TypeID);
            PikminSpawnPos = transform;
            RemovePikminDataClientRpc(data);
            PikminManager.instance.SpawnPikminOnServer(pikminType, PikminSpawnPos.position, transform.rotation, props, this);
        }
        #endregion



        #region Withdrawing
        public void ResetWithDrawAmmount()
        {
            var keys = TypesToExchange.Keys.ToList();
            foreach (PikminType type in keys)
            {
                TypesToExchange[type] = 0;
            }
        }
        #endregion



        #region Adding and Removing
        [ClientRpc]
        public void SetPikminClientRpc(PikminData[] dats)
        {
            PikminInOnion.Clear();
            foreach (PikminData data in dats)
            {
                AddPikmin(data);
            }
        }

        [ClientRpc]
        public void AddPikminClientRpc(PikminData[] dats)
        {
            foreach (PikminData data in dats)
            {
                AddPikmin(data);
            }
        }

        [ClientRpc]
        public void AddPikminClientRpc(PikminData dats)
        {
            AddPikmin(dats);
        }

        /// <summary>
        /// Adds a PikminAI's data to the Onion. If the Pikmin is already in the Onion, it will not be added again.
        /// </summary>
        /// <param name="pikmin"></param>
        public virtual void AddPikmin(PikminAI pikmin)
        {
            PikminData data = pikmin.GetPikminData();
            AddPikmin(data);
        }
        /// <summary>
        /// Directly adds PikminData to the onion.
        /// </summary>
        /// <param name="data"></param>
        public virtual void AddPikmin(PikminData data)
        {
            if (PikminInOnion.Any(p => p.DataID() == data.DataID()))
            {
                LethalMin.Logger.LogWarning($"PikminData ({data.DataID()}) is already in the Onion. Not adding again.");
                return;
            }
            PikminInOnion.Add(data);
        }

        [ClientRpc]
        public void RemovePikminDataClientRpc(PikminData data)
        {
            RemovePikmin(data);
        }

        /// <summary>
        /// Removes an index of PikminData from the Onion. If the Pikmin is not in the Onion, it will not be Removeed again.
        /// </summary>
        /// <param name="pikmin"></param>
        public virtual void RemovePikmin(int Index)
        {
            if (Index < 0 || Index >= PikminInOnion.Count)
            {
                return;
            }
            PikminData data = PikminInOnion[Index];
            RemovePikmin(data);
        }
        /// <summary>
        /// Directly Removes PikminData from the onion.
        /// </summary>
        /// <param name="data"></param>
        public virtual void RemovePikmin(PikminData data)
        {
            var pikminToRemove = PikminInOnion.FirstOrDefault(p => p.DebugName == data.DebugName);
            if (pikminToRemove.Equals(default(PikminData)))
            {
                LethalMin.Logger.LogWarning($"Pikmin {data.DebugName} is not in the Onion. Not removing.");
                return;
            }
            PikminInOnion.Remove(pikminToRemove);
        }

        public static bool IsPikminInOnion(PikminAI ai, Onion onion)
        {
            return onion.PikminInOnion.Any(p => p.DataID() == ai.GetPikminData().DataID());
        }
        #endregion


        #region Sprouting
        public virtual void SuckItemIntoOnion(PikminItem item, PikminType targetType = null!)
        {
            if (item == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null item when sucking into onion!");
                return;
            }
            if (item.settings == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null item settings when sucking into onion!");
                return;
            }
            if (item.settings.CanProduceSprouts)
            {
                if (item.settings.SproutsToSpawn <= 0)
                {
                    LethalMin.Logger.LogWarning($"LethalMin: item {item.gameObject.name} sprouts to spawn is less than or equal to 0!");
                    return;
                }
                if (!onionType.TypesCanHold.ToList().Contains(targetType))
                {
                    LethalMin.Logger.LogWarning($"LethalMin: item {item.gameObject.name} for {targetType.PikminName} cannot be sucked into onion of type {onionType.TypeName}!");
                    return;
                }
                if (item.ItemScript != null && item.ItemScript.TryGetComponent(out RagdollGrabbableObject ragdoll))
                {
                    if (IsServer)
                        SetPlayerToBeRevived(ragdoll, targetType.PikminTypeID);

                    return;
                }
        
                if (LethalMin.AllowOnionToReviveMaskeds &&
                item.hackEnemyGrabbableObject != null && item.hackEnemyGrabbableObject.ai is MaskedPlayerEnemy)
                {
                    if (IsServer)
                        SetEnemyToBeRevived(item.hackEnemyGrabbableObject);

                    return;
                }

                int sproutsToSpawn = item.settings.SproutsToSpawn;
                if (item.settings.PerferedType == targetType && item.settings.PerferedTypeMultipler > 0)
                {
                    sproutsToSpawn = Mathf.RoundToInt(item.settings.SproutsToSpawn * item.settings.PerferedTypeMultipler);
                }
                AddSproutsToSpawn(targetType, sproutsToSpawn);
                item.IncrumentDestoryCountServerRpc();
            }
        }

        public virtual void SetPlayerToBeRevived(RagdollGrabbableObject ragdoll, int TypeID = -1)
        {
            if (ragdoll == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null ragdoll when setting player to be revived!");
                return;
            }
            DeadBodyInfo info = ragdoll.ragdoll;
            if (info == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null info when setting player to be revived!");
                return;
            }
            if (info.playerScript == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null player script when setting player to be revived!");
                return;
            }
            if (LethalMin.AllowOnionsToRevivePlayers.InternalValue == false)
            {
                LethalMin.Logger.LogWarning($"LethalMin: Reviving players via onions is disabled!");
                return;
            }
            if (StartOfRound.Instance.allPlayersDead)
            {
                LethalMin.Logger.LogWarning($"LethalMin: all players are dead, not reviving player {info.playerScript.playerUsername}!");
                return;
            }
            RevivePlayerViaOnionClientRpc(info.playerScript.NetworkObject, TypeID);
        }

        [ClientRpc]
        public void RevivePlayerViaOnionClientRpc(NetworkObjectReference PlayerRef, int TypeID = -1)
        {
            if (!PlayerRef.TryGet(out NetworkObject obj))
            {
                LethalMin.Logger.LogError($"LethalMin: null object when reviving player via onion!");
                return;
            }
            if (!obj.TryGetComponent(out PlayerControllerB player))
            {
                LethalMin.Logger.LogError($"LethalMin: null player when reviving player via onion!");
                return;
            }
            RevivePlayerViaOnion(player, TypeID);
        }

        public virtual void RevivePlayerViaOnion(PlayerControllerB player, int TypeID = -1)
        {
            if (LethalMin.AllowOnionsToRevivePlayers.InternalValue == false)
            {
                LethalMin.Logger.LogWarning($"LethalMin: Reviving players via onions is disabled!");
                return;
            }
            if (StartOfRound.Instance.allPlayersDead)
            {
                LethalMin.Logger.LogWarning($"LethalMin: all players are dead, not reviving player {player.playerUsername}!");
                return;
            }
            LethalMin.Logger.LogMessage($"LethalMin: reviving player {player.playerUsername} via onion {gameObject.name}!");
            PikUtils.RevivePlayer(player, transform.position);
            player.GetComponent<Leader>().SetAsLeafling(TypeID);
        }

        public virtual void SetEnemyToBeRevived(EnemyGrabbableObject enemyGrab)
        {
            if (enemyGrab == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null enemy grab when setting enemy to be revived!");
                return;
            }
            if (enemyGrab.ai == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null ai when setting enemy to be revived!");
                return;
            }
            if (StartOfRound.Instance.allPlayersDead)
            {
                LethalMin.Logger.LogWarning($"LethalMin: all players are dead, not reviving enemy {enemyGrab.ai.gameObject.name}!");
                return;
            }

            // Revive the enemy
            LethalMin.Logger.LogMessage($"LethalMin: reviving enemy {enemyGrab.ai.gameObject.name} via onion {gameObject.name}!");
            EnemyAI ai = PikUtils.ReviveEnemy(enemyGrab.ai, transform.position);
            if (ai.TryGetComponent(out MaskedPlayerPikminEnemy MPPE))
            {
                MPPE.SetAsLeafling();
            }
        }

        public virtual void AddSproutsToSpawn(PikminType typeToAdd, int numberToAdd)
        {
            if (TypesToSpawn.ContainsKey(typeToAdd))
            {
                TypesToSpawn[typeToAdd] += numberToAdd;
                sTimer = SproutSpawnTimer;
                LethalMin.Logger.LogInfo($"LethalMin: {numberToAdd} sprouts of {typeToAdd.PikminName} added to onion {gameObject.name}!");
            }
            else
            {
                LethalMin.Logger.LogError($"LethalMin: onion {gameObject.name} does not contain type {typeToAdd.PikminName}!");
            }
        }

        [ClientRpc]
        public void SpawnSproutsClientRpc()
        {
            SpawnSprouts();
        }

        public virtual void SpawnSprouts()
        {
            if (!IsServer)
            {
                return;
            }
            SpawnSproutsRoutine = StartCoroutine(SproutSpawnRoutine());
            LethalMin.Logger.LogInfo($"LethalMin: Sprouts spawned from onion {gameObject.name}!");
        }

        IEnumerator SproutSpawnRoutine()
        {
            GameObject prefab = LethalMin.SproutPrefab;
            foreach (PikminType type in TypesToSpawn.Keys.ToList())
            {
                if (TypesToSpawn[type] > 0)
                {
                    int sproutsToSpawn = TypesToSpawn[type];
                    int b = 0;
                    for (int i = 0; i < sproutsToSpawn; i++)
                    {
                        if (PikminManager.IsTooManyPikminOnMap)
                        {
                            LethalMin.Logger.LogInfo($"Adding sprout directly into onion instead of spawning {gameObject.name}!");
                            AddPikminClientRpc(new PikminData()
                            {
                                TypeID = onionType.TypesCanHold[0].PikminTypeID,
                                GrowthStage = 0,
                                DebugName = "Sprout",
                                BirthDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            });

                            b++;
                            if (b >= 2)
                            {
                                b = 0;
                                yield return new WaitForSeconds(0.55f);
                            }
                            continue;
                        }
                        Vector3 spawnPos = GetPositionToSpawnSprout();
                        Quaternion RandomYRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                        Sprout sprout = Instantiate(prefab, spawnPos, Quaternion.identity).GetComponent<Sprout>();
                        sprout.pikminType = type;
                        sprout.IsPersistant = true;
                        sprout.NetworkObject.Spawn();
                        sprout.InitalizeClientRpc(spawnPos, RandomYRotation, type.PikminTypeID);
                        b++;
                        if (b >= 2)
                        {
                            b = 0;
                            yield return new WaitForSeconds(0.55f);
                        }
                    }
                    TypesToSpawn[type] = 0;
                }
            }
            SpawnSproutsRoutine = null;
        }

        public virtual Vector3 GetPositionToSpawnSprout(float radius = 8)
        {
            float angleStep = 30f;
            float startAngle = Random.Range(0f, 360f);

            // Calculate the spawn position on the circle
            float angle = startAngle + (LethalMin.MaxPikmin.InternalValue * angleStep) % 360f;
            float radian = angle * Mathf.Deg2Rad;
            float spawnX = Mathf.Sin(radian) * radius;
            float spawnZ = Mathf.Cos(radian) * radius;

            Vector3 airPosition = new Vector3(
                SproutSpawnPos.position.x + spawnX,
                SproutSpawnPos.position.y,
                SproutSpawnPos.position.z + spawnZ
            );

            // Raycast to find the ground position
            RaycastHit hit;
            Vector3 groundPosition;
            if (Physics.Raycast(airPosition, Vector3.down, out hit, Mathf.Infinity, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                groundPosition = hit.point;
            }
            else
            {
                groundPosition = new Vector3(airPosition.x, 0, airPosition.z); // Fallback if raycast fails
            }
            return groundPosition;
        }
        #endregion


        public int GetTypesCanHoldNotInsideOnion()
        {
            int count = 0;

            foreach (PikminAI ai in PikminManager.instance.PikminAIs)
            {
                if (ai.IsWildPikmin)
                {
                    continue;
                }
                if (ai.IsLeftBehind)
                {
                    continue;
                }
                if (onionType.TypesCanHold.Contains(ai.pikminType))
                {
                    count++;
                }
            }

            return count;
        }

        public static Onion? GetOnionOfPikmin(PikminAI pikmin, bool ExcludeNonSproutProducing = false)
        {
            // If pikmin should go to it's target Onion instead of ShipOnion
            if (pikmin.pikminType.TargetOnion != null)
            {
                // First try to find the target onion that can hold this pikmin type
                foreach (Onion onion in PikminManager.instance.Onions)
                {
                    if (ExcludeNonSproutProducing && !onion.onionType.CanCreateSprouts)
                    {
                        continue;
                    }
                    // Check if it's a BaseOnion and can hold this pikmin type
                    if (onion.onionType == pikmin.pikminType.TargetOnion
                    || onion.fusedTypes.Contains(pikmin.pikminType.TargetOnion))
                    {
                        if (!onion.onionType.TypesCanHold.Contains(pikmin.pikminType))
                        {
                            LethalMin.Logger.LogWarning($"Pikmin, {pikmin.DebugID}, is trying to go to its target onion, " +
                                $"but the onion {onion.onionType.TypeName} cannot hold this type of pikmin: {pikmin.pikminType.PikminName}");
                        }
                        return onion;
                    }
                }
            }

            // Fallback to any compatible onion (original behavior)
            foreach (Onion onion in PikminManager.instance.Onions)
            {
                if (ExcludeNonSproutProducing && !onion.onionType.CanCreateSprouts)
                {
                    continue;
                }
                if (onion.onionType.TypesCanHold.Contains(pikmin.pikminType))
                {
                    return onion;
                }
            }
            return null;
        }
    }
}
