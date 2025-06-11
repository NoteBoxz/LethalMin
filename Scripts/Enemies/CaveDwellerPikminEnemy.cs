using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GameNetcodeStuff;
using LethalMin.Patches.AI;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class CaveDwellerPikminEnemy : PikminEnemy
    {
        List<NetworkObjectReference> PikminRefs = new List<NetworkObjectReference>();
        CaveDwellerAI caveDwellerAI = null!;
        CaveDwellerPikminItem pikminItem = null!;
        float CheckInterval = 0.25f;
        public float BiteCooldown;
        public float BiteCooldownRest = 1f;
        public int BiteLimmit = 7;

        public override void OnAddedToEnemy(EnemyAI enemy)
        {
            base.OnAddedToEnemy(enemy);
            GameObject Node = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PikminItemNode.prefab");
            PikminItem pikminItemNode = Node.GetComponent<PikminItem>();
            pikminItem = gameObject.AddComponent<CaveDwellerPikminItem>();
            FieldInfo[] fields = typeof(PikminItem).GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);
            foreach (var field in fields)
            {
                field.SetValue(pikminItem, field.GetValue(pikminItemNode));
            }
            pikminItem.HasOwnNetworkObject = false;
            pikminItem.settings = gameObject.AddComponent<PikminItemSettings>();
            pikminItem.settings.CarryStrength = 5;
            pikminItem.settings.OverrideGrabbableToEnemeis = true;
            pikminItem.settings.RouteToPlayer = true;
            pikminItem.settings.ChangeOwnershipOnCarry = false;
        }

        protected override void Start()
        {
            base.Start();
            caveDwellerAI = enemyScript as CaveDwellerAI ?? throw new System.Exception("CaveDwellerPE: enemyScript is not a CaveDwellerAI");
            pikminItem = GetComponent<CaveDwellerPikminItem>();
            pikminItem.caveDwellerAI = caveDwellerAI;
            pikminItem.caveDwellerPikminEnemy = this;
            if (caveDwellerAI == null)
            {
                enabled = false;
                return;
            }
            if (IsServer)
            {
                pikminItem.InitalizeClientRpc(NetworkObject, "CaveDwellerPikminEnemy");
            }
            if (LethalMin.UseConfigsForEnemies)
            {
                BiteCooldown = LethalMin.CaveDweller_BiteCooldown.InternalValue;
            }
        }

        void LateUpdate()
        {
            CanBeAttacked = caveDwellerAI.currentBehaviourStateIndex != 0 || LethalMin.CaveDweller_AttackAsBaby;
            if (!IsOwner || caveDwellerAI.inKillAnimation || caveDwellerAI.inSpecialAnimation)
            {
                return;
            }
            if (enemyScript.isEnemyDead)
            {
                return;
            }
            if (caveDwellerAI.leapTimer < 0.02f || caveDwellerAI.currentBehaviourStateIndex != 3)
            {
                return;
            }
            if (BiteCooldown > 0)
            {
                BiteCooldown -= Time.deltaTime;
                return;
            }

            if (LethalMin.UseConfigsForEnemies)
            {
                BiteCooldownRest = LethalMin.CaveDweller_BiteCooldown.InternalValue;
                BiteLimmit = LethalMin.CaveDweller_BiteLimit.InternalValue;
            }

            if (CheckInterval > 0)
            {
                CheckInterval -= Time.deltaTime;
            }
            else
            {
                DoCheckInterval();
                CheckInterval = caveDwellerAI.AIIntervalTime + Random.Range(-0.015f, 0.015f);
            }
        }

        public void DoCheckInterval()
        {
            PikminRefs.Clear();
            foreach (PikminAI ai in PikminManager.instance.PikminAIs)
            {
                if (ai.IsDeadOrDying || ai.IsAirborn || ai.CurrentLatchTrigger != null)
                {
                    continue;
                }
                if (Vector3.Distance(ai.transform.position, transform.position) < 5f)
                {
                    PikminRefs.Add(ai.NetworkObject);
                }
                if (PikminRefs.Count >= BiteLimmit)
                {
                    break;
                }
            }
            if (PikminRefs.Count > 0)
            {
                BiteCooldown = BiteCooldownRest;
                BiteNearbyPikminServerRpc(PikminRefs.ToArray());
            }
        }

        [ServerRpc]
        public void BiteNearbyPikminServerRpc(NetworkObjectReference[] Pikmins)
        {
            BiteNearbyPikminClientRpc(Pikmins);
        }
        [ClientRpc]
        public void BiteNearbyPikminClientRpc(NetworkObjectReference[] Pikmins)
        {
            List<PikminAI> PikminsB = new List<PikminAI>();
            foreach (NetworkObjectReference refPikmin in Pikmins)
            {
                if (refPikmin.TryGet(out NetworkObject netObj) && netObj.TryGetComponent(out PikminAI pikminAI))
                {
                    PikminsB.Add(pikminAI);
                }
                else
                {
                    LethalMin.Logger.LogError("Failed to get PikminAI from NetworkObjectReference in BiteNearbyPikminClientRpc");
                }
            }
            if (PikminsB.Count > 0)
            {
                BiteNearbyPikmin(PikminsB);
            }
            else
            {
                LethalMin.Logger.LogError("Failed to get PikminAI from NetworkObjectReference in BiteNearbyPikminClientRpc");
            }
        }

        public void BiteNearbyPikmin(List<PikminAI> Pikmins)
        {
            BiteCooldown = BiteCooldownRest;
            foreach (PikminAI pikminAI in Pikmins)
            {
                pikminAI.DeathSnapToPos = caveDwellerAI.bodyRagdollPoint;
                pikminAI.OverrideDelay = 0.45f;
                pikminAI.HitEnemy(5);
            }
        }
    }
}
