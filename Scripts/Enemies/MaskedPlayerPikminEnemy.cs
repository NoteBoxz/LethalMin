using System.Collections;

using System.Collections.Generic;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class MaskedPlayerPikminEnemy : PikminEnemy
    {
        float CheckInterval = 0.25f;
        public PuffminLeader leader = null!;
        public MaskedPlayerEnemy maskedPlayerEnemy = null!;
        public bool IsLeafling = false;

        protected override void Start()
        {
            base.Start();

            if (maskedPlayerEnemy == null)
                maskedPlayerEnemy = enemyScript as MaskedPlayerEnemy ?? throw new System.Exception("maskedPlayerPE: enemyScript is not a maskedPlayerEnemy");
            if (maskedPlayerEnemy == null)
            {
                enabled = false;
                return;
            }

            if (IsServer)
                maskedPlayerEnemy.StartCoroutine(WaitForMimicToSpawn());
        }

        void LateUpdate()
        {
            if (!IsOwner)
            {
                return;
            }
            if (enemyScript.isEnemyDead)
            {
                return;
            }

            if (CheckInterval > 0)
            {
                CheckInterval -= Time.deltaTime;
            }
            else
            {
                DoCheckInterval();
                CheckInterval = maskedPlayerEnemy.AIIntervalTime + Random.Range(-0.015f, 0.015f);
            }
        }

        public void DoCheckInterval()
        {
            if (!LethalMin.MaskedPlayerEnemy_ConvertPikmin || maskedPlayerEnemy.isEnemyDead)
            {
                return;
            }

            if (!maskedPlayerEnemy.inKillAnimation && !maskedPlayerEnemy.handsOut && !leader.IsWhistleing && AreThereValidPikminNearby(maskedPlayerEnemy.transform.position, 10))
            {
                DoWhistleAnimServerRpc();
                LethalMin.Logger.LogDebug($"{maskedPlayerEnemy.gameObject.name}: Found pikmin to whistle at");
            }

            switch (maskedPlayerEnemy.currentBehaviourStateIndex)
            {
                case 0:
                    if (leader.PuffminHolding != null)
                    {
                        leader.DoThrow();
                        LethalMin.Logger.LogDebug($"{maskedPlayerEnemy.gameObject.name}: dropping held puffmin after exiting attack state");
                    }
                    if (!maskedPlayerEnemy.inKillAnimation && !leader.IsWhistleing && AreThereValidPuffminNearby(maskedPlayerEnemy.transform.position, 10))
                    {
                        DoWhistleAnimServerRpc();
                        LethalMin.Logger.LogDebug($"{maskedPlayerEnemy.gameObject.name}: Found puffmin to whistle at");
                    }
                    break;
                case 1:
                    if (leader.PuffminInSquad.Count > 0 && leader.PuffminHolding == null)
                    {
                        leader.StartThrow();
                    }
                    if (maskedPlayerEnemy.handsOut && leader.PuffminHolding != null)
                    {
                        Vector3 OD = maskedPlayerEnemy.targetPlayer.transform.position - leader.transform.position;
                        leader.OverrideThrowDirection = OD.normalized;
                        leader.DoThrow();
                    }
                    break;
            }
        }


        [ServerRpc]
        public void DoWhistleAnimServerRpc()
        {
            DoWhistleAnimClientRpc();
        }

        [ClientRpc]
        public void DoWhistleAnimClientRpc()
        {
            StartCoroutine(DoWhistle());
        }

        public IEnumerator DoWhistle()
        {
            CustomMaskedAnimationManager anim = GetComponent<CustomMaskedAnimationManager>();
            leader.IsWhistleing = true;
            yield return anim.SetWhistleToOverride();
            leader.StartWhistleingOnLocalClient();
            yield return new WaitForSeconds(3);
            yield return leader.TweenSize(true);
            leader.StopWhistleingOnLocalClient();
            StartCoroutine(anim.DeSetWhistleToOverride());
        }

        public IEnumerator WaitForMimicToSpawn()
        {
            float startTime = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => maskedPlayerEnemy.mimickingPlayer != null || Time.realtimeSinceStartup - startTime > 20f); // wait for the mimicking player to spawn
            if (maskedPlayerEnemy.mimickingPlayer != null)
            {
                SpawnSproutOnMaskedServerRpc(maskedPlayerEnemy.mimickingPlayer.OwnerClientId);
            }
        }


        public bool AreThereValidPikminNearby(Vector3 Pos, float range)
        {
            foreach (PikminAI ai in PikminManager.instance.PikminAIs)
            {
                if (Vector3.Distance(Pos, ai.transform.position) > range) continue; // Skip if not within range

                if (ai != null && ai.currentBehaviourStateIndex == PikminAI.IDLE && !ai.IsDeadOrDying
                && !PikChecks.IsPikminResistantToHazard(ai, PikminHazard.Spore, false))
                {
                    return true; // Found a valid pikmin nearby
                }
            }

            return false;
        }

        public bool AreThereValidPuffminNearby(Vector3 Pos, float range)
        {
            foreach (PuffminAI puffmin in PikminManager.instance.PuffminAIs)
            {
                if (Vector3.Distance(Pos, puffmin.transform.position) > range) continue; // Skip if not within range

                if (puffmin != null && puffmin.currentBehaviourStateIndex == 0 && !puffmin.IsDeadOrDying && puffmin.WhistleBuffer <= 0 && puffmin.Leader == null)
                {
                    return true; // Found a valid pikmin nearby
                }
            }

            return false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnSproutOnMaskedServerRpc(ulong PlayerID)
        {
            SpawnSproutOnMaskedClientRpc(PlayerID);
        }
        [ClientRpc]
        public void SpawnSproutOnMaskedClientRpc(ulong PlayerID)
        {
            SpawnSproutOnMasked(PlayerID);
        }

        public void SpawnSproutOnMasked(ulong PlayerThatWasMaskedID)
        {
            if (IsLeafling)
            {
                LethalMin.Logger.LogWarning($"Attempted to spawn a sprout on {maskedPlayerEnemy.gameObject.name} but it is already a Leafling!");
                return; // already spawned
            }
            Leader? maskedLeader = LethalMin.GetLeaderViaID(PlayerThatWasMaskedID);
            if (maskedLeader == null)
            {
                LethalMin.Logger.LogWarning($"SpawnSproutOnMasked: Leader for {maskedPlayerEnemy.gameObject.name} not found!");
                return; // leader not found
            }
            if (!maskedLeader.wasLeaflingBeforeDeath && !maskedLeader.IsLeafling)
            {
                return; // don't spawn if they weren't a leafling before death
            }
            maskedLeader.wasLeaflingBeforeDeath = false;
            SetAsLeafling();
        }
        public void SetAsLeafling()
        {
            IsLeafling = true;
            GameObject LeaflingSprout = Instantiate(LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/MaskedSprout.prefab"));
            LeaflingSprout.transform.SetParent(maskedPlayerEnemy.headTransform);
            LeaflingSprout.transform.localPosition = new Vector3(0, 0.3f, 0);
            LeaflingSprout.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            LeaflingSprout.transform.localRotation = new Quaternion(0.2164f, 0, 0, -0.9763f);
            LethalMin.Logger.LogInfo($"Spawned Leafling Sprout on {gameObject.name}");
        }

        public int DespawnCount = 0;

        [ServerRpc(RequireOwnership = false)]
        public void IncrumentDestoryCountServerRpc()
        {
            DespawnCount++;
            if (DespawnCount >= StartOfRound.Instance.connectedPlayersAmount + 1)
            {
                LethalMin.Logger.LogInfo($"All Clients marked to despawn {gameObject.name}");
                NetworkObject.Despawn(true);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void SpawnLeaflingGhostRpc()
        {
            GameObject ghost = GameObject.Instantiate(LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PlayerGhostPrefab.prefab"),
            transform.position, Quaternion.identity);
            PlayerGhost pg = ghost.GetComponent<PlayerGhost>();
            Renderer renderer = pg.GetComponentInChildren<Renderer>();
            renderer.material.color = new Color(0.18823529411f, 0.09803921568f, 0.20392156862f, 0.75f);
            AudioSource ghostAudio = pg.GetComponentInChildren<AudioSource>();
            ghostAudio.pitch = 0.8f; // slightly lower pitch for ghost
            IncrumentDestoryCountServerRpc();
            LethalMin.Logger.LogInfo($"Spawned ghost for Leafling {gameObject.name} at {ghost.transform.position}");
        }
    }
}