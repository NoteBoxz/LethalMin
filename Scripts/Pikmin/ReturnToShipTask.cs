using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;

namespace LethalMin.Pikmin
{
    public class ReturnToShipTask : PikminTask
    {
        public PikminRoute ReturnToShipRoute = null!; // Route to the ship, if any
        public ReturnToShipTask(PikminAI pikminAssigningTo) : base(pikminAssigningTo)
        {

        }

        public override void OnTaskCreated()
        {
            base.OnTaskCreated();
            ReturnToShipRoute = new PikminRoute(pikmin);
            ReturnToShipRoute.OnRouteEnd.AddListener(pikmin.FinishTask);
        }

        public override void IntervaledUpdate()
        {
            if (pikmin == null)
            {
                LethalMin.Logger.LogWarning($"RTST: Owner is null in IntervaledUpdate");
                return;
            }
            NavMeshAgent agent = pikmin.agent;

            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.stoppingDistance = 0;

            ReturnToShipRoute.UpdateRoute();
        }
    }
}
