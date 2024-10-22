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
    public class PikminAnimEvents : MonoBehaviour
    {
        public PikminAI AI;

        public void Hit()
        {
            if (AI.EnemyAttacking != null && !AI.EnemyAttacking.isEnemyDead)
            {
                LethalMin.Logger.LogInfo($"{AI.uniqueDebugId}: HIT!!!!!!!");
                if (AI.previousLeader != null)
                {
                    AI.EnemyDamager.HitInAirQoutes(AI.PminType.GetDamage(), AI.previousLeader.Controller, true, 1);
                }
                else
                {
                    AI.EnemyDamager.HitInAirQoutes(AI.PminType.GetDamage(), null, true, 1);
                }
                AI.ReqeustAttackAndHitSFXClientRpc();
            }
        }
        public void InitHit()
        {
            if (AI.EnemyAttacking != null && !AI.EnemyAttacking.isEnemyDead)
            {
                AI.ReqeustAttackSFXClientRpc();
            }
        }

        public void HitCastable()
        {

            if (AI.EnemyAttacking != null && !AI.EnemyAttacking.isEnemyDead)
            {
                RaycastHit hit;
                bool linecastHit = Physics.Linecast(transform.position, AI.EnemyAttacking.transform.position, out hit, LethalMin.Instance.PikminColideable);

                if ((!linecastHit) || hit.collider == null || (hit.collider != null && hit.collider.gameObject.name.Contains("AnomalySpawnBox")))
                {
                    LethalMin.Logger.LogInfo($"{AI.uniqueDebugId}: HIT!!!!!!!");
                    if (AI.previousLeader != null)
                    {
                        AI.EnemyDamager.HitInAirQoutes(AI.PminType.GetDamage(), AI.previousLeader.Controller, true, 1);
                    }
                    else
                    {
                        AI.EnemyDamager.HitInAirQoutes(AI.PminType.GetDamage(), null, true, 1);
                    }
                    AI.ReqeustHitSFXClientRpc();
                }
                else
                {
                    LethalMin.Logger.LogInfo($"({AI.uniqueDebugId}) Linecast blocked for enemy {AI.EnemyAttacking.name} by: {hit.collider.gameObject.name}");
                }
            }
        }

    }
}