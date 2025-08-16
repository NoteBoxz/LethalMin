using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace LethalMin.Pikmin
{
    /// <summary>
    /// Task for having a Pikmin attack its target enemy
    /// </summary>
    public class AttackEnemyTask : PikminTask
    {
        public PikminEnemy? enemy => pikmin.TargetEnemy;
        public bool IsPikminOnEnemy => pikmin.CurrentLatchTrigger != null;
        string DebugID => $"{pikmin.DebugID} - AttackEnemyTask";
        public override bool DoYayOnTaskEnd => true;

        public AttackEnemyTask(PikminAI pikminAssigningTo) : base(pikminAssigningTo)
        {

        }

        float timeSinceAttacking = 0f;

        public override void IntervaledUpdate()
        {
            base.IntervaledUpdate();

            pikmin.agent.stoppingDistance = 0;

            // If the pikmin is on the enemy, we start the attack routine if it's not already running
            if (pikmin.CurrentIntention == Pintent.Attack && IsPikminOnEnemy && pikmin.attackRoutine == null)
            {
                pikmin.StartAttackOnLocalClient();
                pikmin.StartAttackServerRpc();
                return;
            }

            // If the pikmin is not on the enemy, we need to path to the enemy
            if (pikmin.CurrentIntention == Pintent.Attack && !IsPikminOnEnemy)
            {
                if (enemy == null)
                {
                    TaskEnd(true, true);
                    LethalMin.Logger.LogWarning($"{DebugID}: Was reset to idle because of null enemy");
                    return;
                }

                pikmin.PathToPosition(enemy.transform.position);

                if (enemy.enemyScript.isEnemyDead)
                {
                    pikmin.FinishTaskServerRpc();
                    return;
                }
                if (Vector3.Distance(transform.position, enemy.transform.position) >
                 pikmin.pikminType.EnemyDetectionRange * 4 + enemy.enemyScript.agent.radius + pikmin.agent.radius)
                {
                    TaskEnd(true, true);
                    LethalMin.Logger.LogWarning($"{DebugID}: Went out of range of enemy");
                    return;
                }
            }
        }

        public override void Update()
        {
            base.Update();

            if (IsPikminOnEnemy)
            {
                return;
            }
            if (timeSinceAttacking > 0f)
            {
                timeSinceAttacking -= Time.deltaTime;
            }
            else
            {
                pikmin.AttackEnemyWhenNear();
                timeSinceAttacking = pikmin.pikminType.AttackRate;
            }
        }
    }
}
