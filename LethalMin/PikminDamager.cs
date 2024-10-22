using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Linq;
namespace LethalMin
{
    public class PikminDamager : NetworkBehaviour
    {
        public float FakeHP;
        public EnemyAI RootScript;
        public List<PikminAI> PikminLatchedOn = new List<PikminAI>();

        public void Update()
        {
        }

        [ServerRpc(RequireOwnership = false)]
        public void LatchOnServerRpc(NetworkBehaviourReference reff)
        {
            if (reff.TryGet(out NetworkBehaviour be))
            {
                PikminAI min = be.gameObject.GetComponent<PikminAI>();
                if (min != null && !PikminLatchedOn.Contains(min))
                    PikminLatchedOn.Add(min);
            }
        }


        [ServerRpc(RequireOwnership = false)]
        public void LatchOffServerRpc(NetworkBehaviourReference reff)
        {
            if (reff.TryGet(out NetworkBehaviour be))
            {
                PikminAI min = be.gameObject.GetComponent<PikminAI>();
                if (min != null && PikminLatchedOn.Contains(min))
                    PikminLatchedOn.Remove(min);
            }
        }
        public void ShakePikmin(Vector3 knockbackForce, bool IsLethal, bool KillOnLanding, float DeathTimer = 0)
        {
            if (PikminLatchedOn.Count > 0)
                ShakePikminServerRpc(knockbackForce, IsLethal, KillOnLanding, DeathTimer);
        }

        public void KnockOffAllPikmin(Vector3 knockbackForce, bool IsLethal, bool KillOnLanding, float DeathTimer = 0)
        {
            if (PikminLatchedOn.Count > 0)
            {
                ShakePikminServerRpc(knockbackForce, IsLethal, KillOnLanding, DeathTimer, 10);
                PikminLatchedOn.Clear();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ShakePikminServerRpc(Vector3 knockbackForce, bool IsLethal, bool KillOnLanding, float DeathTimer = 0, float F = 0)
        {
            foreach (var item in PikminLatchedOn)
            {
                item.KnockbackOnEnemy(knockbackForce, IsLethal, KillOnLanding, DeathTimer, F);
            }
        }

        public void HitInAirQoutes(float force = 0.25f, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            FakeHP += force;
            if (FakeHP >= 1)
            {
                RootScript.HitEnemyOnLocalClient(1, default, playerWhoHit, playHitSFX, hitID);
                FakeHP = 0;
            }
        }
    }
}