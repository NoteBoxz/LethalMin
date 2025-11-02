using System.Collections;
using System.Collections.Generic;
using LethalMin.Routeing;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;

namespace LethalMin.Pikmin
{
    public class ReturnToShipTask : PikminTask
    {
        private PikminRoute route = null!;

        public ReturnToShipTask(PikminAI pikminAssigningTo) : base(pikminAssigningTo)
        {

        }

        public override void OnTaskCreated()
        {
            CreateRoute();
        }

        private void CreateRoute()
        {
            PikminRouteRequest request = new PikminRouteRequest
            {
                Pikmin = pikmin,
                Intent = RouteIntent.ToShip
            };

            route = PikminRouteManager.Instance.CreateRoute(request);

            if (route == null)
            {
                LethalMin.Logger.LogWarning($"{pikmin.DebugID}: Failed to create route to ship!");
                pikmin.FinishTask();
                return;
            }

            route.OnRouteComplete.AddListener(OnRouteComplete);
            route.OnRouteInvalidated.AddListener(OnRouteInvalidated);
        }

        private void OnRouteInvalidated(RouteValidation.InvalidationReason reason)
        {
            LethalMin.Logger.LogDebug($"{pikmin.DebugID}: Route invalidated on node {route.CurNode.name} ({reason}), regenerating");
            CreateRoute();
        }

        private void OnRouteComplete()
        {
            pikmin.FinishTask();
        }

        public override void Update()
        {
            route?.Update();
        }
    }
}
