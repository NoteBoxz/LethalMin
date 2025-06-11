using System.Collections;
using System.Collections.Generic;
using LethalMin;
using UnityEngine;
using UnityEngine.AI;


namespace LethalMin
{
    public class LeaderFormationTarget : MonoBehaviour
    {
        public NavMeshAgent agent = null!;
        public LeaderFormationManager formationManager = null!;
        float CalcualtionTimer = 0f;

        void Start()
        {
            agent.speed = 50;
            agent.angularSpeed = 1000;
            agent.acceleration = 1000;
        }


        void Update()
        {
            if (CalcualtionTimer > 0)
            {
                CalcualtionTimer -= Time.deltaTime;
            }
            else
            {
                CalcualtionTimer = formationManager.UpdateInterval;
                CheckToMove();
            }
        }

        void CheckToMove()
        {
            if (StartOfRound.Instance.inShipPhase || formationManager.leader.Controller.isPlayerDead || !PikminManager.CanPathOnMoonGlobal)
            {
                agent.enabled = false;
                return;
            }

            agent.enabled = true;
            if (!agent.isOnNavMesh)
            {
                agent.enabled = false;
                transform.position = formationManager.transform.position;
                return;
            }

            agent.stoppingDistance = Mathf.Lerp(4.0f, 9.0f, (float)formationManager.leader.PikminInSquad.Count / LethalMin.MaxPikmin.InternalValue);

            //LethalMin.Logger.LogInfo(agent.stoppingDistance);
            agent.SetDestination(formationManager.transform.position);

            if (Vector3.Distance(transform.position, formationManager.transform.position) > 20 + agent.stoppingDistance)
            {
                agent.Warp(formationManager.transform.position);
            }
        }
    }
}