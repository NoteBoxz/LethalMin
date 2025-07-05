using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;

namespace LethalMin.Pikmin
{
    public class PikminCarryItemTask : PikminTask
    {
        public PikminItem OwnersItem = null!;

        public PikminCarryItemTask(PikminAI owner, PikminItem ownersItem) : base(owner)
        {
            OwnersItem = ownersItem;
        }

        public override void IntervaledUpdate()
        {
            if(Owner == null || OwnersItem == null)
            {
                LethalMin.Logger.LogWarning($"PCIT: Owner or OwnersItem is null in IntervaledUpdate");
                return;
            }

            NavMeshAgent agent = Owner.agent;
            PikminType pikminType = Owner.pikminType;
            string DebugID = $"{Owner.DebugID}";
            bool IsOnItem = OwnersItem.PikminOnItem.Contains(Owner);

            agent.speed = OwnersItem.GetSpeed();

            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            if (OwnersItem != null && IsOnItem && OwnersItem.settings.RouteToPlayer)
            {
                agent.stoppingDistance = OwnersItem.settings.RouteToPlayerStoppingDistance;
            }
            else
            {
                agent.stoppingDistance = 0;
            }

            if (!Owner.pikminType.CanCarryObjects)
            {
                LethalMin.Logger.LogWarning($"{DebugID}: Pikmin type {pikminType.PikminName} cannot carry objects, switching to idle state");
                Owner.SetToIdleServerRpc();
                return;
            }
            if (OwnersItem == null)
            {
                // Guard clause: If there's no target item to work on, log a warning and switch to idle state
                LethalMin.Logger.LogWarning($"{DebugID}: OwnersItem is null when working");
                Owner.SetToIdleServerRpc();
                return;
            }
            if (Owner.TargetItemPoint == null)
            {
                // Guard clause: If there's no grab position on the target item, log a warning and switch to idle state
                LethalMin.Logger.LogWarning($"{DebugID}: OwnersItemPoint is null when working");
                Owner.SetToIdleServerRpc();
                return;
            }
            if (OwnersItem.settings.GrabableToPikmin == false)
            {
                // Guard clause: If the item is not grabable by Pikmin, log a warning and switch to idle state
                LethalMin.Logger.LogWarning($"{DebugID}: OwnersItem is stopped being grabable to Pikmin when working");
                Owner.SetToIdleServerRpc();
                return;
            }
            if (Vector3.Distance(Owner.transform.position, Owner.TargetItemPoint.transform.position) > pikminType.ItemDetectionRange * 2
            && !IsOnItem)
            {
                // If Pikmin is too far away from item and isn't already carrying it, give up and return to idle
                LethalMin.Logger.LogInfo($"{DebugID}: Is too far away");
                Owner.SetToIdleServerRpc();
                return;
            }

            if (!IsOnItem || !OwnersItem.IsBeingCarried)
            {
                // If Pikmin isn't yet on the item or the item isn't being carried, move toward the grab position
                Owner.PathToPosition(Owner.TargetItemPoint.transform.position);
            }
            if (Vector3.Distance(Owner.transform.position, Owner.TargetItemPoint.transform.position) < 2f
            && !IsOnItem)
            {
                // If Pikmin is close enough to grab the item and isn't already carrying it
                if (OwnersItem.HasArrived)
                {
                    // If the item has already reached its destination, no need to grab it
                    Owner.SetToIdleServerRpc();
                    LethalMin.Logger.LogWarning($"{DebugID}: Is not grabbing item beacuse it has arrived");
                    return;
                }
                // If the item needs to be carried, grab it
                Owner.GrabItemServerRpc();
                Owner.GrabItemOnLocalClient(OwnersItem);
            }
        }
    }
}
