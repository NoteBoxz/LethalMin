using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using LethalMin;
using System.Reflection;
using System.Collections;
using System.Linq;
using System;
using Unity.Netcode.Components;

namespace LethalMin
{
    public class NoticeZone : NetworkBehaviour, IDebuggable
    {
        public bool IsActive = true;
        public bool InstantNotice = false;
        public bool UseCheckSpher = false;
        public bool CanConvertPikmin = false;
        [IDebuggable.Debug] public PlayerControllerB leader;
        [IDebuggable.Debug] public EnemyAI enemy;
        [ServerRpc(RequireOwnership = false)]
        public void SetLeaderOnServerRpc(NetworkObjectReference leaderref)
        {
            if (leaderref.TryGet(out NetworkObject leadNO))
            {
                leader = leadNO.GetComponent<PlayerControllerB>();
            }
        }
        public void OnTriggerEnter(Collider other)
        {
            if (!IsActive || !IsServer || UseCheckSpher)
            {
                return;
            }

            if (other.name == "WhistleDetection")
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo("WhistleDetection entered");
                PikminAI pikmin = other.GetComponentInParent<PikminAI>();
                if (pikmin != null && !pikmin.IsDying)// && !pikmin.IsWhistled && pikmin.whistlingPlayer != leader)
                {
                    StartCoroutine(CheckForPikminInWhistleZone());
                }
            }
            if (other.name == "WhistleDetectionWhistle" && InstantNotice)
            {
                PikminAI pikmin = other.GetComponentInParent<PikminAI>();
                if (pikmin != null && !pikmin.IsDying)
                {
                    if (IsServer)
                    {
                        pikmin.IsWhistled = true;
                        pikmin.whistlingPlayer = leader;
                    }
                    else
                    {
                        pikmin.SetWhistleingPlayerServerRpc(new NetworkObjectReference(leader?.NetworkObject));
                    }
                    pikmin.NoticeInstant(leader, true);
                }
            }
            if (other.name == "WhistleDetectionWhistle" && !InstantNotice)
            {
                PikminAI pikmin = other.GetComponentInParent<PikminAI>();
                if (pikmin != null && !pikmin.IsDying)
                {
                    if (IsServer)
                    {
                        pikmin.IsWhistled = true;
                        pikmin.whistlingPlayer = leader;
                    }
                    else
                    {
                        pikmin.SetWhistleingPlayerServerRpc(new NetworkObjectReference(leader?.NetworkObject));
                    }
                }
            }
        }
        public void OnTriggerExit(Collider other)
        {
            if (!IsActive || InstantNotice || !IsServer || UseCheckSpher)
            {
                return;
            }

            if (other.name == "WhistleDetectionWhistle" || other.name == "WhistleDetection")
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo("WhistleDetection entered");
                StopWhistle(other.GetComponentInParent<PikminAI>());
            }
        }
        void Update()
        {
            if (IsServer && IsActive && UseCheckSpher)
            {
                StartCoroutine(CheckForPikminInWhistleZone());
            }
        }
        private float CalculateRadius()
        {
            Vector3 scale = transform.lossyScale;
            // If it's scaled non-uniformly, this will use the largest dimension
            return Mathf.Max(scale.x, scale.y, scale.z) / 2f;
        }
        IEnumerator CheckForPikminInWhistleZone()
        {
            // Calculate the radius dynamically
            float radius = CalculateRadius();

            // Get all colliders within the whistle zone radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

            foreach (Collider collider in colliders)
            {
                try
                {
                    if (collider == null) { continue; }
                    if (collider.gameObject == null) { continue; }
                    if (string.IsNullOrEmpty(collider.name)) { continue; }
                    if (collider.name != "PikminColision" && collider.name != "PuffminColision") { continue; }
                }
                catch (Exception e)
                {
                    LethalMin.Logger.LogDebug($"An error occurred while doing invalid checks for Pikmin in the whistle zone: " + e.ToString());
                }

                yield return new WaitForSeconds(0.001f);

                try
                {
                    // Check if the collider belongs to a PikminAI
                    if (collider.name == "PikminColision" && collider.gameObject.GetComponentInParent<PikminAI>() != null)
                    {
                        ProcessPikminInZone(collider.gameObject.GetComponentInParent<PikminAI>());
                        continue; // Skip to the next collider to avoid duplicate processing if the same PikminAI is in multiple zones
                    }


                    // Check if the collider belongs to a PuffminAI and can be converteds
                    if (collider.name == "PuffminColision" && collider.gameObject.GetComponentInParent<PuffminAI>() != null)
                    {
                        ProcessPuffminInZone(collider.gameObject.GetComponentInParent<PuffminAI>());
                        continue; // Skip to the next collider to avoid duplicate processing if the same PuffminAI is in multiple zones
                    }
                }
                catch (Exception e)
                {
                    LethalMin.Logger.LogDebug($"An error occurred while processing for Pikmin in the whistle zone:  " + e.ToString());
                }
            }
        }
        public void ProcessPikminInZone(PikminAI pikminAI)
        {
            try
            {
                if (!CanConvertPikmin)
                {
                    if (pikminAI != null && !pikminAI.CannotEscape)
                    {
                        pikminAI.whistlingPlayer = leader;
                        pikminAI.IsWhistled = true;
                        if (InstantNotice)
                        {
                            pikminAI.NoticeInstant(leader, true);
                        }
                    }
                }
                else
                {
                    if (pikminAI != null && !pikminAI.CannotEscape && !pikminAI.IsDying
                     && pikminAI.currentBehaviourStateIndex == (int)PState.Idle
                     && !LethalMin.IsPikminResistantToHazard(pikminAI.PminType, HazardType.Spore))
                    {
                        pikminAI.TurnIntoPuffmin(enemy);
                    }
                }
            }
            catch (Exception e)
            {
                LethalMin.Logger.LogWarning($"An error occurred while processing for Pikmin in the whistle zone:  " + e.ToString());
            }
        }
        public void ProcessPuffminInZone(PuffminAI puffmin)
        {
            try
            {
                if (!CanConvertPikmin)
                {
                    if (puffmin != null && !puffmin.IsDying &&
                    (puffmin.currentBehaviourStateIndex == (int)PuffState.attacking || puffmin.currentBehaviourStateIndex == (int)PuffState.idle))
                    {
                        puffmin.TurnIntoPikmin();
                    }
                }
                else
                {
                    if (puffmin != null && !puffmin.IsDying &&
                    (puffmin.currentBehaviourStateIndex == (int)PuffState.attacking || puffmin.currentBehaviourStateIndex == (int)PuffState.idle))
                    {
                        puffmin.HasFreeWill = false;
                        if (enemy.GetComponentInChildren<PuffminOwnerManager>() != null)
                        {
                            enemy.GetComponentInChildren<PuffminOwnerManager>().AddPuffmin(puffmin);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LethalMin.Logger.LogWarning($"An error occurred while processing for Puffmin in the whistle zone: " + e.ToString());
            }
        }
        public void StopWhistle(PikminAI pikmin)
        {
            if (pikmin != null && !pikmin.IsDying)
            {
                if (IsServer)
                {
                    pikmin.whistlingPlayer = null;
                    pikmin.IsWhistled = false;
                }
                else
                {
                    pikmin.SetWhistleingPlayerServerRpc();
                }
            }
        }

    }
}