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
        public ReturnToShipTask(PikminAI owner) : base(owner)
        {

        }

        public override void OnTaskCreated()
        {
            base.OnTaskCreated();
            ReturnToShipRoute = new PikminRoute(Owner);
            ReturnToShipRoute.OnRouteEnd.AddListener(() => TaskEnd(true));
        }

        public override void IntervaledUpdate()
        {
            if (Owner == null)
            {
                LethalMin.Logger.LogWarning($"PCIT: Owner or OwnersItem is null in IntervaledUpdate");
                return;
            }
            NavMeshAgent agent = Owner.agent;

            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.stoppingDistance = 0;

            ReturnToShipRoute.UpdateRoute();
        }
    }
}
