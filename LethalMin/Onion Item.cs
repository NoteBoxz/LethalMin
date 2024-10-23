using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;
using Unity.Netcode;
using System;
using TMPro;
using System.Linq; // Add this for LINQ operations

namespace LethalMin
{
    public class OnionItem : GrabbableObject, IDebuggable
    {
        [IDebuggable.Debug] public OnionType type;

        public void Initialize(OnionType onionType)
        {
            LethalMin.Logger.LogInfo($"Syncing Onion type {onionType.TypeName} on server");
            try
            {
                type = onionType;
                SyncOnionTypeClientRpc(onionType.OnionTypeID);
            }
            catch (Exception e)
            {
                LethalMin.Logger.LogError($"Failed to sync onion type to client due to: {e}");
            }
        }

        [ClientRpc]
        private void SyncOnionTypeClientRpc(int typeId)
        {
            LethalMin.Logger.LogInfo($"Syncing Onion type ID {typeId} on client");
            try
            {
                OnionType syncedType = LethalMin.GetOnionTypeById(typeId);
                if (syncedType != null)
                {
                    type = syncedType;
                    ApplyColor(syncedType);
                }
                else
                {
                    LethalMin.Logger.LogError($"Failed to find OnionType with ID {typeId}");
                }
            }
            catch (Exception e)
            {
                LethalMin.Logger.LogError($"Failed to sync onion type on client due to: {e}");
            }
        }

        private void ApplyColor(OnionType onionType)
        {
            LethalMin.Logger.LogInfo($"Coloring onion {onionType.TypeName}");
            Color onionColor = onionType.OnionColor;
            transform.Find("OnionItem/SK_stg_OnyonCarry.001").GetComponent<Renderer>().material.color = onionColor;
            transform.Find("MapDot").GetComponent<Renderer>().material.color = onionColor;
            if (onionType.OnionItemMeshPrefab != null)
            {
                transform.Find("OnionItem").gameObject.SetActive(false);
                GameObject onionItem = Instantiate(onionType.OnionItemMeshPrefab, transform);
                onionItem.transform.SetParent(transform);
            }
            LethalMin.Logger.LogInfo($"Colored onion {onionColor}");
        }
        public bool Spawning;
        public override void Update()
        {
            base.Update();
            if (playerHeldBy != null && !playerHeldBy.isInsideFactory && !Spawning 
            || isInShipRoom && !Spawning 
            || isInElevator && !Spawning)
            {
                Spawning = true;
                isInFactory = false;
                StartCoroutine(SpawnIn());
            }
        }
        IEnumerator SpawnIn()
        {
            yield return new WaitForSeconds(1f);

            if (playerHeldBy != null)
            {
                // Get the forward direction of the player
                Vector3 playerForward = playerHeldBy.transform.forward;

                // Calculate a position in front of the player
                Vector3 spawnPosition = playerHeldBy.transform.position + playerForward * 2f; // 2 units in front of the player

                // Move the OnionItem to the calculated position
                transform.position = spawnPosition;

                playerHeldBy.DropAllHeldItemsAndSync();
            }

            if (playerHeldBy != null && !playerHeldBy.isInsideFactory
            || isInShipRoom
            || isInElevator)
                SpawnOnionClientRpc(type.OnionTypeID);
        }
        [ClientRpc]
        private void SpawnOnionClientRpc(int typeId)
        {
            OnionType onionType = LethalMin.GetOnionTypeById(typeId);
            if (onionType != null)
            {
                StartCoroutine(SpawnOnionCoroutine(onionType));
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to find OnionType with ID {typeId}");
            }
        }

        private IEnumerator SpawnOnionCoroutine(OnionType onionType)
        {
            grabbable = false;
            LethalMin.Logger.LogInfo($"Starting spawn sequence for {onionType.TypeName} Onion");

            GetComponentInChildren<Animator>().Play("OnionActive");

            yield return new WaitForSeconds(5f);

            try
            {
                if (PikminManager.Instance.CollectedOnions.Contains(onionType.OnionTypeID))
                {
                    LethalMin.Logger.LogInfo($"{onionType.TypeName} Onion already collected, destroying item");
                }
                else
                {
                    LethalMin.Logger.LogInfo($"Setting {onionType.TypeName} Onion as collected");
                    PikminManager.Instance.SetOnionCollectedClientRpc(onionType.OnionTypeID);

                    if (IsServer)
                    {
                        LethalMin.Logger.LogInfo($"Server is spawning {onionType.TypeName} Onion");
                        StartCoroutine(SpawnOnServerCoroutine(onionType));
                    }
                    else
                    {
                        LethalMin.Logger.LogInfo($"Client requesting server to spawn {onionType.TypeName} Onion");
                        RequestSpawnOnionServerRpc(onionType.OnionTypeID);
                    }
                }
            }
            catch (Exception ex)
            {
                LethalMin.Logger.LogError($"Failed to spawn onion due to: {ex.Message}");
            }
            finally
            {
                LethalMin.Logger.LogInfo($"Destroying {onionType.TypeName} Onion item");
                if (IsServer && NetworkObject.IsSpawned)
                    NetworkObject.Despawn();
                Destroy(gameObject);
            }
        }

        private IEnumerator SpawnOnServerCoroutine(OnionType onionType)
        {
            yield return StartCoroutine(PikminManager.Instance.SpawnSpecificOnion(onionType));
            LethalMin.Logger.LogInfo($"{onionType.TypeName} Onion spawned successfully on server");
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestSpawnOnionServerRpc(int typeId)
        {
            OnionType onionType = LethalMin.GetOnionTypeById(typeId);
            if (onionType != null)
            {
                LethalMin.Logger.LogInfo($"Server received request to spawn {onionType.TypeName} Onion");
                StartCoroutine(PikminManager.Instance.SpawnSpecificOnion(onionType));
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to find OnionType with ID {typeId}");
            }
        }
    }
}