using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;

namespace LethalMin.Pikmin
{
    /// <summary>
    /// Used to have a Pikmin walk to and grab and item
    /// </summary>
    public class CarryItemTask : PikminTask
    {
        public PikminItem? pikminItem => pikmin.TargetItem;

        public CarryItemTask(PikminAI pikminAssigningTo) : base(pikminAssigningTo)
        {

        }

        public override void IntervaledUpdate()
        {
            if (pikmin == null || pikminItem == null)
            {
                if (pikmin == null)
                    LethalMin.Logger.LogWarning("PCIT: Pikmin is null in IntervaledUpdate");
                if (pikminItem == null && pikmin != null)
                    LethalMin.Logger.LogWarning($"{pikmin.DebugID}: PCIT: PikminItem is null in IntervaledUpdate");

                TaskEnd();
                return;
            }

            NavMeshAgent agent = pikmin.agent;
            PikminType pikminType = pikmin.pikminType;
            string DebugID = $"{pikmin.DebugID} - CarryItemTask";
            bool IsOnItem = pikminItem.PikminOnItem.Contains(pikmin);

            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            if (IsOnItem && pikminItem.settings.RouteToPlayer 
            && pikminItem.CurrentRoute != null && pikminItem.CurrentRoute.CurNode.InstanceIdentifiyer == pikminItem.PrimaryLeader)
            {
                agent.stoppingDistance = pikminItem.settings.RouteToPlayerStoppingDistance;
            }
            else
            {
                agent.stoppingDistance = 0;
            }
            if (IsOnItem)
            {
                agent.speed = pikminItem.GetSpeed();
            }
            else
            {
                agent.speed = pikminType.GetSpeed(pikmin.CurrentGrowthStage, true);
            }

            if (!pikmin.pikminType.CanCarryObjects)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Pikmin type {pikminType.PikminName} cannot carry objects, switching to idle state");
                TaskEnd();
                return;
            }
            if (pikminItem == null)
            {
                // Guard clause: If there's no target item to work on, log a warning and switch to idle state
                LethalMin.Logger.LogWarning($"{DebugID}: OwnersItem is null when working");
                TaskEnd();
                return;
            }
            if (pikmin.TargetItemPoint == null)
            {
                // Guard clause: If there's no grab position on the target item, log a warning and switch to idle state
                LethalMin.Logger.LogWarning($"{DebugID}: OwnersItemPoint is null when working");
                TaskEnd();
                return;
            }
            if (pikminItem.settings.GrabableToPikmin == false)
            {
                // Guard clause: If the item is not grabable by Pikmin, log a warning and switch to idle state
                LethalMin.Logger.LogWarning($"{DebugID}: OwnersItem is stopped being grabable to Pikmin when working");
                TaskEnd();
                return;
            }
            if (Vector3.Distance(transform.position, pikmin.TargetItemPoint.transform.position) > pikminType.ItemDetectionRange * 2
            && !IsOnItem)
            {
                // If Pikmin is too far away from item and isn't already carrying it, give up and return to idle
                LethalMin.Logger.LogInfo($"{DebugID}: Is too far away");
                TaskEnd();
                return;
            }

            if (!IsOnItem || !pikminItem.IsBeingCarried)
            {
                // If Pikmin isn't yet on the item or the item isn't being carried, move toward the grab position
                pikmin.PathToPosition(pikmin.TargetItemPoint.transform.position);
            }
            if (Vector3.Distance(transform.position, pikmin.TargetItemPoint.transform.position) < 2f
            && !IsOnItem)
            {
                // If Pikmin is close enough to grab the item and isn't already carrying it
                if (pikminItem.HasArrived)
                {
                    // If the item has already reached its destination, no need to grab it
                    TaskEnd();
                    LethalMin.Logger.LogWarning($"{DebugID}: Is not grabbing item beacuse it has arrived");
                    return;
                }
                // If the item needs to be carried, grab it
                pikmin.GrabItemServerRpc();
                pikmin.GrabItemOnLocalClient(pikminItem);
            }
        }
    }
}
