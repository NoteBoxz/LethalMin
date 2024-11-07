using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using GameNetcodeStuff;
using System.Linq;

namespace LethalMin
{
    public class Onion : NetworkBehaviour
    {
        public Transform AnimPos;
        protected InteractTrigger interactTrigger;
        public OnionType type;
        public List<OnionType> FusedTypes;
        public bool IsFuesion()
        {
            return FusedTypes.Count > 1;
        }

        [SerializeField] protected GameObject pikminPrefab;
        public List<OnionPikmin> pikminInOnion = new List<OnionPikmin>();
        protected System.Random OnionRandom;

        public Dictionary<PikminAI, PikminType> PikminInField;
        public bool HasDecidedToLeave;
        public Transform SucPoint;
        public Transform SpiPoint;
        public List<PikminType> TypesToSpawn;
        public float SpawnTimer;

        public virtual void Start()
        {
            OnionRandom = new System.Random(StartOfRound.Instance.randomMapSeed + type.GetInstanceID());
            pikminPrefab = LethalMin.pikminPrefab;
            SetupInteractTrigger();
            PikminInField = new Dictionary<PikminAI, PikminType>();
            StartCoroutine(UpdateFieldCount());
        }

        protected virtual void SetupInteractTrigger()
        {
            interactTrigger = GetComponentInChildren<InteractTrigger>(true);
            if (interactTrigger != null)
            {
                interactTrigger.onInteract.AddListener((PlayerControllerB player) =>
                {
                    if (OnionMenuManager.instance != null)
                    {
                        LeaderManager leaderManager = FindLeaderManagerForPlayer(player);
                        if (leaderManager != null)
                        {
                            OnionMenuManager.instance.OpenMenu(this, leaderManager);
                        }
                        else
                        {
                            LethalMin.Logger.LogError($"LeaderManager not found for player {player.name}");
                        }
                    }
                    else
                    {
                        LethalMin.Logger.LogError("OnionMenuManager instance is null");
                    }
                });
            }
            else
            {
                LethalMin.Logger.LogError("InteractTrigger not found in children of Onion object.");
            }
        }
        public virtual int GetPikminCount()
        {
            return pikminInOnion.Count;
        }
        public virtual int GetPikminCountByType(PikminType pikminType)
        {
            return pikminInOnion.Count(p => p.PikminTypeID == pikminType.PikminTypeID);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncPikminListServerRpc(OnionPikmin[] pikminArray)
        {
            pikminInOnion = new List<OnionPikmin>(pikminArray);
            SyncPikminListClientRpc(pikminArray);
        }

        [ClientRpc]
        private void SyncPikminListClientRpc(OnionPikmin[] pikminArray)
        {
            pikminInOnion = new List<OnionPikmin>(pikminArray);
        }
        protected IEnumerator UpdateFieldCount()
        {
            while (true)
            {
                if (StartOfRound.Instance.shipIsLeaving)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(2f);
                }
                PikminInField.Clear();
                PikminAI[] PikminInExistance = GameObject.FindObjectsOfType<PikminAI>();

                for (int i = 0; i < PikminInExistance.Length; i++)
                {
                    PikminInField.Add(PikminInExistance[i], PikminInExistance[i].PminType);
                }
            }
        }

        bool DoingSpawning = false;
        public virtual void LateUpdate()
        {
            if (TypesToSpawn.Count > 0)
            {
                SpawnTimer -= Time.deltaTime;
                if (SpawnTimer <= 0 && !DoingSpawning)
                {
                    StartCoroutine(DoSpawnPikmin());
                }
            }
        }
        IEnumerator DoSpawnPikmin()
        {
            DoingSpawning = true;
            int Number = 0;
            for (int i = 0; i < TypesToSpawn.Count; i++)
            {
                SpawnPikmin(TypesToSpawn[0]);
                TypesToSpawn.RemoveAt(0);
                Number++;
                if (Number >= 2)
                {
                    yield return new WaitForSeconds(0.1f);
                    Number = 0;
                }
            }
            DoingSpawning = false;
        }
        public virtual void SpawnPikmin(PikminType type)
        {
        }
        [ServerRpc(RequireOwnership = false)]
        public virtual void AddToTypesToSpawnServerRpc(int TypeID)
        {
            SpawnTimer = 1;
            TypesToSpawn.Add(LethalMin.GetPikminTypeById(TypeID));
        }
        // Update the CreatePikminServerRpc method to handle multiple Pikmin types
        [ServerRpc(RequireOwnership = false)]
        public virtual void CreatePikminServerRpc(int count, int pikminTypeId, Vector3 spawnPosition, NetworkObjectReference refz)
        {
            if (count <= 0 || count > pikminInOnion.Count) return;

            List<OnionPikmin> pikminToSpawn = pikminInOnion
                .Where(p => p.PikminTypeID == pikminTypeId)
                .OrderByDescending(p => p.GrowStage)
                .Take(count)
                .ToList();

            LethalMin.Logger.LogInfo($"Removing {pikminToSpawn.Count} pikmin of type {pikminTypeId}");
            foreach (var pikmin in pikminToSpawn)
            {
                pikminInOnion.Remove(pikmin);
            }

            for (int i = 0; i < count; i++)
            {
                if (pikminPrefab != null)
                {
                    GameObject pikminObj = Instantiate(pikminPrefab, spawnPosition, Quaternion.identity);
                    NetworkObject networkObject = pikminObj.GetComponent<NetworkObject>();
                    PikminAI pikminAI = pikminObj.GetComponent<PikminAI>();
                    if (networkObject != null)
                    {
                        networkObject.Spawn();
                        PikminManager.Instance.SpawnPikminClientRpc(new NetworkObjectReference(networkObject));
                    }
                    else
                    {
                        LethalMin.Logger.LogError("NetworkObject component not found on Pikmin prefab.");
                    }
                    CreatePikminClientRPC(new NetworkObjectReference(pikminAI.NetworkObject), refz, i, pikminToSpawn[i].GrowStage, pikminTypeId);
                }
                else
                {
                    LethalMin.Logger.LogError("Pikmin prefab is not set in the Onion script.");
                }
            }

            UpdatePikminListClientRpc(pikminInOnion.ToArray());
        }

        // Update the CreatePikminClientRPC method to handle PikminTypeID
        [ClientRpc]
        public virtual void CreatePikminClientRPC(NetworkObjectReference network1, NetworkObjectReference network2, float delay, int growStage, int pikminTypeId)
        {
            network1.TryGet(out NetworkObject PikObj);
            PikminAI script = PikObj.GetComponent<PikminAI>();
            if (script == null) { return; }
            network2.TryGet(out NetworkObject PlaObj);
            if (PlaObj.GetComponent<PlayerControllerB>() == null) { return; }
            script.HideMeshOnStart = true;
            script.GrowStage = growStage;
            script.PreDefinedType = true;
            script.PminType = LethalMin.GetPikminTypeById(pikminTypeId);
            script.inSpecialAnimation = true;
            script.TargetOnion = this;
            StartCoroutine(waitForInitalizePik(PikObj.GetComponent<PikminAI>(), PlaObj.GetComponent<PlayerControllerB>(), delay));
        }

        // Update the ReturnPikminToOnionServerRpc method to handle multiple Pikmin types
        [ServerRpc(RequireOwnership = false)]
        public virtual void ReturnPikminToOnionServerRpc(PikminData[] pikminDataArray, NetworkObjectReference LeaderRef)
        {
            Dictionary<ulong, PikminAI> pikminToReturn = new Dictionary<ulong, PikminAI>();
            List<NetworkObjectReference> pikminRefs = new List<NetworkObjectReference>();

            foreach (PikminData pikminData in pikminDataArray)
            {
                NetworkObject pikminNetObj = FindNetworkObjectById(pikminData.NetworkObjectId);
                if (pikminNetObj != null)
                {
                    PikminAI pikmin = pikminNetObj.GetComponent<PikminAI>();
                    pikminToReturn.Add(pikminData.NetworkObjectId, pikmin);
                    pikminRefs.Add(new NetworkObjectReference(pikminNetObj));
                }
            }

            if (LeaderRef.TryGet(out NetworkObject leaderNetObj))
            {
                PlayerControllerB leader = leaderNetObj.GetComponent<PlayerControllerB>();
                if (leader != null)
                {
                    leader.GetComponentInChildren<LeaderManager>().RemoveAllPikminServerRpc(pikminRefs.ToArray(), false);
                }
            }

            foreach (PikminData pikminData in pikminDataArray)
            {
                pikminInOnion.Add(new OnionPikmin { GrowStage = pikminData.GrowStage, PikminTypeID = pikminData.PikminTypeID });

                PikminAI pikmin = pikminToReturn[pikminData.NetworkObjectId];
                if (pikmin != null)
                {
                    pikmin.SwitchToBehaviourClientRpc((int)PState.Leaveing);
                }
                else
                {
                    LethalMin.Logger.LogWarning($"PikminAI component not found on NetworkObject with ID {pikminData.NetworkObjectId}");
                }
            }

            UpdatePikminListClientRpc(pikminInOnion.ToArray());
        }


        protected IEnumerator waitForInitalizePik(PikminAI pikminAI, PlayerControllerB Playwer, float Delay)
        {
            while (!pikminAI.HasInitalized)
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(Delay / 10);
            pikminAI.PlayAnimClientRpc("ExitOnion");
            pikminAI.ReqeustPlayExitOnionClientRpc();
            pikminAI.AssignLeader(Playwer.GetComponent<PlayerControllerB>(), false);
            pikminAI.inSpecialAnimation = false;
        }
        protected NetworkObject FindNetworkObjectById(ulong networkId)
        {
            foreach (NetworkObject netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                if (netObj.NetworkObjectId == networkId)
                {
                    return netObj;
                }
            }
            return null;
        }

        [ClientRpc]
        public virtual void UpdatePikminListClientRpc(OnionPikmin[] pikminArray)
        {
            pikminInOnion = new List<OnionPikmin>(pikminArray);
        }

        public virtual OnionPikmin[] GetPikminInOnion()
        {
            return pikminInOnion.ToArray();
        }
        public int GetPikminInOnionByType(PikminType pikminType)
        {
            return pikminInOnion.Count(p => p.PikminTypeID == pikminType.PikminTypeID);
        }

        protected LeaderManager FindLeaderManagerForPlayer(PlayerControllerB player)
        {
            // Option 1: If LeaderManager is a component on the same GameObject as PlayerControllerB
            LeaderManager leaderManager = player.GetComponent<LeaderManager>();
            if (leaderManager != null)
            {
                return leaderManager;
            }

            // Option 2: If LeaderManager is on a child GameObject
            leaderManager = player.GetComponentInChildren<LeaderManager>();
            if (leaderManager != null)
            {
                return leaderManager;
            }

            // Option 3: If there's a known relationship between PlayerControllerB and LeaderManager
            // For example, if LeaderManager is always on a child object named "LeaderManager"
            Transform leaderManagerTransform = player.transform.Find("LeaderManager");
            if (leaderManagerTransform != null)
            {
                leaderManager = leaderManagerTransform.GetComponent<LeaderManager>();
                if (leaderManager != null)
                {
                    return leaderManager;
                }
            }

            // Option 4: If there's no direct relationship, we might need to search all LeaderManagers in the scene
            LeaderManager[] allLeaderManagers = FindObjectsOfType<LeaderManager>();
            foreach (LeaderManager lm in allLeaderManagers)
            {
                if (lm.Controller == player)
                {
                    return lm;
                }
            }

            // If we couldn't find a LeaderManager, return null
            LethalMin.Logger.LogWarning("Could not find LeaderManager for player: " + player.name);
            return null;
        }
    }
}


public struct PikminData : INetworkSerializable
{
    public int GrowStage;
    public ulong NetworkObjectId;
    public int PikminTypeID; // Add this field

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref GrowStage);
        serializer.SerializeValue(ref NetworkObjectId);
        serializer.SerializeValue(ref PikminTypeID); // Serialize the PikminTypeID
    }
}