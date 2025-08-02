using UnityEngine;
using Unity.Netcode;
using LethalMin.Pikmin;
using LethalMin.Utils;
using System.Collections.Generic;
using System;

namespace LethalMin
{
    public class PikminTypeResolver : NetworkBehaviour
    {
        public bool hasResolvedType = false;
        public PikminAI DefaultAI = null!;
        public PikminCollisionDetect detect = null!;
        [HideInInspector]
        public List<PikminAI> PikminAIs = new List<PikminAI>();
        public PikminType typeToResolve = null!;
        public int TypeIDToResolve = -1;
        public PikminSpawnProps SpawningProps;
        public Onion OnionToSpawnFrom = null!;
        public int OnionIndex;
        Vector3 StoredSpawnPosition = Vector3.zero;

        [ClientRpc]
        public void SyncSpawnClientRpc(int PikminTypeID)
        {
            TypeIDToResolve = PikminTypeID;
        }
        [ClientRpc]
        public void SyncSpawnClientRpc(int PikminTypeID, PikminSpawnProps props)
        {
            TypeIDToResolve = PikminTypeID;
            SpawningProps = props;
        }
        [ClientRpc]
        public void SyncSpawnFromOnionClientRpc(int PikminTypeID, int Index, NetworkObjectReference OnionRef, PikminSpawnProps props = default)
        {
            if (OnionRef.TryGet(out NetworkObject onionObject) && onionObject.TryGetComponent(out Onion onion))
            {
                TypeIDToResolve = PikminTypeID;
                SpawningProps = props;
                OnionToSpawnFrom = onion;
                OnionIndex = Index;
            }
            else
            {
                LethalMin.Logger.LogError($"{gameObject.name}: Failed to get onion network object or onion script when resloving type!");
            }
        }

        public void Start()
        {
            if (hasResolvedType) return;

            if (IsOwner)
            {
                StoredSpawnPosition = transform.position;
            }

            // First, determine the PikminType if not already set
            if (TypeIDToResolve >= 0)
            {
                typeToResolve = LethalMin.GetPikminTypeByID(TypeIDToResolve);
                if (typeToResolve == null)
                {
                    LethalMin.Logger.LogError($"PikminType with ID {TypeIDToResolve} not found when resloving...");
                    return;
                }
            }
            else
            {
                typeToResolve = ChooseRandomPikminType();
            }

            // Determine which AI type to keep
            Type typeToKeep = typeToResolve.CustomTypeScript != null
                ? typeToResolve.CustomTypeScript.GetType()
                : DefaultAI.GetType();

            // Find and configure the matching AI, destroy the others
            foreach (PikminAI ai in PikminAIs)
            {
                if (ai.GetType() == typeToKeep)
                {
                    ai.pikminType = typeToResolve;
                    detect.mainScript = ai;
                    detect.mainPikmin = ai;
                    ai.enabled = true;
                    ai.SpawnProps = SpawningProps;
                    ai.StoredSpawnPosition = StoredSpawnPosition;
                    if (OnionToSpawnFrom != null)
                    {
                        ai.SpawnFromOnion(OnionIndex, OnionToSpawnFrom);
                    }
                }
                else
                {
                    //The ONLY reason why we do somthing as risky and hacky as this is because when GetComponet is called, 
                    // it returns the first component it finds, which is the default AI. So we need to destroy the other AI scripts to prevent them from being used.
                    NetworkObject.ChildNetworkBehaviours.Remove(ai);
                    Destroy(ai);
                }
            }

            PikUtils.ReorganizeNetworkBehaviours(NetworkObject);

            hasResolvedType = true;
        }

        private PikminType ChooseRandomPikminType()
        {
            int seed = StartOfRound.Instance.randomMapSeed + (int)NetworkObjectId;
            //LethalMin.Logger.LogInfo($"{StartOfRound.Instance.randomMapSeed} + {(int)NetworkObjectId} = {seed}");
            //LethalMin.Logger.LogInfo($"random seed: {seed}");
            System.Random random = new System.Random(seed);
            PikminType type = LethalMin.NaturalSpawningTypes[random.Next(0, LethalMin.NaturalSpawningTypes.Count)];
            LethalMin.Logger.LogInfo($"Randomly selected PikminType: {type.name}");
            return type;
        }
    }
}