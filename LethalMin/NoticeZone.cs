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
    public class NoticeZone : NetworkBehaviour
    {
        public bool IsActive = true;
        public bool InstantNotice = false;
        public PlayerControllerB? leader;
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
            if (!IsActive || !IsServer)
            {
                return;
            }

            if (other.name == "WhistleDetection")
            {
                if (LethalMin.DebugMode)
                    LethalMin.Logger.LogInfo("WhistleDetection entered");
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
                    if (InstantNotice)
                    {
                        pikmin.NoticeInstant(leader, true);
                    }
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
            if (!IsActive || InstantNotice || !IsServer)
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