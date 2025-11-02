using System.Collections.Generic;
using LethalMin;
using LethalMin.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class WingedPikminAI : PikminAI
    {
        public FlyingNavAgentController FlyingNavAgentController { get; private set; } = null!;
        public NoticeZoneOnlyDetect NoticeZoneOnlyDetect { get; private set; } = null!;
        List<int> NoFlyOffsetStaets = new List<int>
        {
            3,
            4,
            5
        };
        public override void Start()
        {
            base.Start();

            FlyingNavAgentController = gameObject.AddComponent<FlyingNavAgentController>();

            GameObject AgentHolder = new GameObject($"{gameObject.name}_NavAgent");
            AgentHolder.transform.position = transform.position;
            NavMeshAgent movedAgent = AgentHolder.AddComponent<NavMeshAgent>();
            movedAgent.speed = 12;
            movedAgent.angularSpeed = 500;
            movedAgent.acceleration = 500;
            movedAgent.stoppingDistance = 0f;
            movedAgent.radius = 0.5f;
            movedAgent.height = 1.75f;
            movedAgent.avoidancePriority = 100;
            movedAgent.baseOffset = 0;
            Destroy(agent);
            agent = movedAgent;
            FlyingNavAgentController.GroundAgent = movedAgent;
            FlyingNavAgentController.ceilingLayerMask = StartOfRound.Instance.collidersRoomDefaultAndFoliage;

            GameObject SecondaryDetect = Instantiate(gameObject.GetComponentInChildren<PikminCollisionDetect>().gameObject, transform);
            SecondaryDetect.name = "WingedPikminNoticeZoneOnlyDetect";
            // GameObject dbc = PikUtils.CreateDebugCube(Color.cyan);
            // dbc.transform.SetParent(movedAgent.transform);
            // dbc.transform.localPosition = Vector3.zero;
            // dbc.transform.localScale = Vector3.one;
            if (SecondaryDetect.TryGetComponent(out PikminCollisionDetect existingDetect))
            {
                Destroy(existingDetect);
            }
            NoticeZoneOnlyDetect = SecondaryDetect.AddComponent<NoticeZoneOnlyDetect>();
            NoticeZoneOnlyDetect.mainPikmin = this;

            TempObjects.Add(AgentHolder);
        }

        public override void Update()
        {
            base.Update();

            FlyingNavAgentController.enabled = agent.enabled;
            NoticeZoneOnlyDetect.transform.position = agent.transform.position;
            if (!agent.enabled)
            {
                agent.transform.position = transform.position;
            }
        }

        public override void DoAIInterval()
        {
            FlyingNavAgentController.baseFlightHeight = GetFlightHeight();
            FlyingNavAgentController.MinFlightHeight = GetMinFlightHeight();
            FlyingNavAgentController.EnableFlying = ShouldFly();

            base.DoAIInterval();
        }

        private bool ShouldFly()
        {
            if (Laying)
                return false; // Don't fly while laying
            if (currentBehaviourStateIndex == PANIC)
                return false; // Don't fly while Panicing
            if (TargetItem != null && TargetItem.PrimaryPikminOnItem == this && !IsWingedPikminOnItem(TargetItem))
                return false; // Don't fly while carrying items and no other Winged Pikmin is on it

            return true;
        }

        private float GetMinFlightHeight()
        {
            // Check for water below using downward raycast from ground agent position
            Vector3 groundAgentPos = agent.transform.position;
            Vector3 transformPos = transform.position;

            // Calculate the distance we need to check (distance between ground agent and transform)
            float checkDistance = Mathf.Abs(transformPos.y - groundAgentPos.y) + 5f; // Add 5f for extra safety

            // Get the Triggers layer mask
            int layerMask = LayerMask.GetMask("Triggers");

            // Perform downward raycast from ground agent position
            if (Physics.Raycast(transformPos, Vector3.down, out RaycastHit hit, checkDistance, layerMask, QueryTriggerInteraction.Collide))
            {
                // Check if the hit object has a QuickSand component with IsWater = true
                if (hit.collider.TryGetComponent(out QuicksandTrigger quickSand) && quickSand.isWater)
                {
                    // Set minimum flight height to be above the water surface
                    float waterSurfaceHeight = hit.point.y;
                    float safeHeight = waterSurfaceHeight + 2f; // Add 2 units above water for safety
                    return Mathf.Max(0f, safeHeight - transformPos.y);
                }
            }

            return -9999f;
        }

        private float GetFlightHeight()
        {
            if (currentBehaviourStateIndex == FOLLOW)
                return OverrideFollowPosition != null ? 0f : 4f; // Following state

            if (currentBehaviourStateIndex == WORK && TargetItem != null && !TargetItem.PikminOnItem.Contains(this))
            {
                return 0f;
            }

            if (currentBehaviourStateIndex == WORK && TargetItem != null && TargetItem.PrimaryPikminOnItem == this && TargetItem.CurrentRoute != null)
            {
                return TargetItem.CurrentRoute.Nodes[TargetItem.CurrentRoute.CurrentNodeIndex].IsPikminNearNode(this, 10f) ? 0f : 4f;
            }

            if (NoFlyOffsetStaets.Contains(currentBehaviourStateIndex))
                return 0f; // Disable flying offset for certain states

            return 2f; // Default flight height
        }

        public override void HandleCarrySnapping()
        {
            if (CurrentIntention == Pintent.Carry && TargetItem != null && TargetItemPoint != null
            && TargetItem.PrimaryPikminOnItem != this)
            {
                transform.position = TargetItemPoint.transform.position;
                Vector3 directionToTarget = TargetItem.transform.position - transform.position;
                directionToTarget.y = 0; // This ensures rotation only on Y axis

                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15 * Time.deltaTime);
                }
            }
        }

        public static bool IsWingedPikminOnItem(PikminItem itm)
        {
            if (itm.PikminOnItem.Count == 1 && itm.PrimaryPikminOnItem is WingedPikminAI)
            {
                return true; // Only one Winged Pikmin on the item
            }
            foreach (PikminAI pikmin in itm.PikminOnItem)
            {
                if (pikmin != itm.PrimaryPikminOnItem && pikmin is WingedPikminAI)
                {
                    return true; // Found a Winged Pikmin on the item
                }
            }
            return false; // No Winged Pikmin found on the item
        }
    }
}