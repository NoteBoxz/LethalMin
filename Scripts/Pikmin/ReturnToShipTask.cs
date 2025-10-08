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
                Intent = pikmin.isOutside ? RouteIntent.ToShip : RouteIntent.ToExit,
                // ToExit will get them outside, then they can path normally to ship
                MustUseExits = true,
                PreferShortest = true
            };

            route = PikminRouteManager.Instance.CreateRoute(request);

            route.Update();

            if (route != null)
            {
                route.OnNodeReached.AddListener(OnNodeReached);
                route.OnRouteComplete.AddListener(OnRouteComplete);
                route.OnRouteInvalidated.AddListener(OnRouteInvalidated);
            }
        }

        private void OnNodeReached(RouteNode node)
        {
            // If we just exited and intent was ToExit, regenerate to actually go to ship
            if (route.Request.Intent == RouteIntent.ToExit && !pikmin.isOutside)
            {
                LethalMin.Logger.LogInfo($"{pikmin.DebugID}: Exited building, now routing to ship");
                CreateRoute(); // Will now use ToShip intent since we're outside
            }
        }

        private void OnRouteInvalidated(RouteValidation.InvalidationReason reason)
        {
            LethalMin.Logger.LogInfo($"{pikmin.DebugID}: Route invalidated ({reason}), regenerating");
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
