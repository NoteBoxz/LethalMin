using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using LethalMin.Pikmin;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class SpecialPikminIdleInteraction : NetworkBehaviour
    {
        // made for that stupid chair interaction.
        // there has to be a better way to do this, but I don't want to move the launch date out of june.
        // this is a temporary solution to allow pikmin to interact with objects that are not interactable by default.

        public InteractTrigger trigger = null!;
        public Leader? leaderInSpecialAnim;
        public List<PikminAI> AIsInSpecialAnim = new List<PikminAI>();
        public string AnimIndexToForce = "Sit";
        Transform? prevLockedPlayer;
        int? prevPikminInSquadCount = 0;

        public virtual void Start()
        {
            if (trigger == null)
            {
                trigger = GetComponent<InteractTrigger>();
            }
        }

        public virtual void Update()
        {
            if (trigger.lockedPlayer != prevLockedPlayer)
            {
                prevLockedPlayer = trigger.lockedPlayer;
                leaderInSpecialAnim = prevLockedPlayer == null ? null : prevLockedPlayer.gameObject.GetComponent<Leader>();
                if (leaderInSpecialAnim == null)
                {
                    SetLeaderServerRpc();
                }
                else
                {
                    SetLeaderServerRpc(leaderInSpecialAnim.NetworkObject);
                }
            }
            if (leaderInSpecialAnim != null && (prevPikminInSquadCount == null || leaderInSpecialAnim.PikminInSquad.Count != prevPikminInSquadCount.Value))
            {
                bool arg1passed = prevPikminInSquadCount == null;
                prevPikminInSquadCount = leaderInSpecialAnim.PikminInSquad.Count;
                if (!arg1passed)
                {
                    OnPikminCountChanged();
                }
            }
            else
            {
                prevPikminInSquadCount = null;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetLeaderServerRpc()
        {
            SetLeaderClientRpc();
        }

        [ClientRpc]
        public void SetLeaderClientRpc()
        {
            leaderInSpecialAnim = null;
            OnLeaderChanged(leaderInSpecialAnim == null);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetLeaderServerRpc(NetworkObjectReference reff)
        {
            SetLeaderClientRpc(reff);
        }

        [ClientRpc]
        public void SetLeaderClientRpc(NetworkObjectReference reff)
        {
            if (reff.TryGet(out NetworkObject netobj) && netobj.TryGetComponent<Leader>(out Leader leader))
            {
                leaderInSpecialAnim = leader;
                LethalMin.Logger.LogInfo($"SetLeaderClientRpc: Leader set to {leader.Controller.playerUsername}.");
            }
            else
            {
                LethalMin.Logger.LogWarning($"{gameObject.name}: Invalid Leader reference in SetLeaderClientRpc!");
                leaderInSpecialAnim = null;
            }
            OnLeaderChanged(leaderInSpecialAnim == null);
        }

        public virtual void OnLeaderChanged(bool isNull)
        {
            LethalMin.Logger.LogInfo($"LeaderChanged {isNull}");
        }

        public virtual void OnPikminCountChanged()
        {
            LethalMin.Logger.LogInfo($"PikminCountChanged");
        }

        public virtual void OnFillAisList()
        {
            LethalMin.Logger.LogInfo($"AiListFiled");
        }

        public void SyncAIsList()
        {
            NetworkObjectReference[] pikRefs = new NetworkObjectReference[AIsInSpecialAnim.Count];
            for (int i = 0; i < AIsInSpecialAnim.Count; i++)
            {
                if (AIsInSpecialAnim[i] != null)
                {
                    pikRefs[i] = AIsInSpecialAnim[i].NetworkObject;
                }
            }
            SyncAIsListServerRpc(pikRefs);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncAIsListServerRpc(NetworkObjectReference[] pikRefs)
        {
            ReciveAIsListClientRpc(pikRefs);
        }

        [ClientRpc]
        public void ReciveAIsListClientRpc(NetworkObjectReference[] pikRefs)
        {
            if (leaderInSpecialAnim != null && leaderInSpecialAnim.IsOwner)
            {
                return;
            }

            AIsInSpecialAnim.Clear();
            foreach (NetworkObjectReference refPik in pikRefs)
            {
                if (refPik.TryGet(out NetworkObject netObj) && netObj.TryGetComponent<PikminAI>(out PikminAI ai))
                {
                    AIsInSpecialAnim.Add(ai);
                    LethalMin.Logger.LogInfo($"got ai: {ai.DebugID}");
                }
                else
                {
                    LethalMin.Logger.LogWarning($"{gameObject.name}: Invalid PikminAI reference in AIsInSpecialAnim!");
                }
            }

            OnFillAisList();
        }

        public List<PikminAI> GetRangeOfClosestPikminToLeader(Leader leader, int MaxCount, float MaxDistance)
        {
            LethalMin.Logger.LogInfo($"GetRangeOfClosestPikminToLeader: Searching for up to {MaxCount} pikmin within {MaxDistance} units of {leader.Controller.playerUsername}.");
            List<PikminAI> pikminList = new List<PikminAI>();
            foreach (PikminAI ai in leader.PikminInSquad)
            {
                if (ai == null || ai.leader != leader || ai.Unselectable || ai.CurrentIntention != Pintent.Idle)
                    continue;

                if (Vector3.Distance(ai.transform.position, leader.transform.position) > MaxDistance)
                    continue;

                pikminList.Add(ai);

                if (pikminList.Count >= MaxCount)
                    break;
            }
            return pikminList;
        }

        public static int FindIdleIndexForSpecialAnim(PikminAI ai, string anim, bool mustBeExact = false)
        {
            LethalMin.Logger.LogInfo($"FindIdleIndexForSpecialAnim: Searching for '{anim}' in {ai.DebugID}.");
            PikminAnimationPack? pack = ai.animController?.AnimPack;
            if (pack == null)
            {
                LethalMin.Logger.LogWarning($"FindIndexForSpecialAnim: {ai.DebugID} has no animpack or animcontroller!");
                return 0;
            }

            foreach (AnimationClip clip in pack.EditorIdleAnim)
            {
                if (!mustBeExact && clip.name.ToLower().Contains(anim.ToLower()))
                {
                    return pack.EditorIdleAnim.IndexOf(clip);
                }

                if (mustBeExact && clip.name.ToLower() == anim.ToLower())
                {
                    return pack.EditorIdleAnim.IndexOf(clip);
                }
            }

            return 0;
        }
    }
}
