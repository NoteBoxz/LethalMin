using System.Collections;
using System.Collections.Generic;
using LethalMin.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class FlyingNavAgentController : MonoBehaviour
    {

        [Header("References")]
        public NavMeshAgent GroundAgent = null!;

        [Header("Flying Settings")]
        public float baseFlightHeight = 2f; // Base height above ground
        public float maxFlightHeight = 9999f; // Maximum flight height
        public float ceilingDistance = 1f; // The ammount of space to keep below the ceiling
        public float MinFlightHeight = -9999f; // Minimum flight height
        public LayerMask ceilingLayerMask = -1; // What counts as ceiling
        public bool EnableFlying = true; // Enable or disable flying behavior

        [Header("Easing Settings")]
        public float positionEaseSpeed = 5f;
        public float rotationEaseSpeed = 8f;
        public float heightEaseSpeed = 3f;

        [Header("Debug Visualization")]
        public bool showGizmos = true;
        public Color flightPathColor = Color.cyan;
        public Color ceilingRayColor = Color.red;
        public Color targetPositionColor = Color.green;
        public Color groundAgentColor = Color.blue;

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private float targetHeight;
        float desiredHeight;

        void Start()
        {
            if (GroundAgent == null)
            {
                Debug.LogError("GroundAgent is not assigned!");
                return;
            }

            // Initialize target values
            targetPosition = transform.position;
            targetRotation = transform.rotation;
            targetHeight = baseFlightHeight;
        }

        void Update()
        {
            if (GroundAgent == null) return;

            // Calculate the desired flying position
            CalculateTargetPosition();

            // Apply easing to position and rotation
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionEaseSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationEaseSpeed * Time.deltaTime);
        }

        void CalculateTargetPosition()
        {
            targetRotation = GroundAgent.transform.rotation;

            if (!EnableFlying)
            {
                targetPosition = GroundAgent.transform.position;
                return;
            }

            Vector3 groundAgentPos = GroundAgent.transform.position;

            if (GroundAgent.isOnOffMeshLink)
            {
                GroundAgent.CompleteOffMeshLink();
            }

            // Ease the height offset (not the absolute position)
            desiredHeight = Mathf.Clamp(GroundAgent.destination.y + baseFlightHeight, groundAgentPos.y, groundAgentPos.y + maxFlightHeight);
            targetHeight = Mathf.Lerp(targetHeight, desiredHeight, heightEaseSpeed * Time.deltaTime);

            Vector3 flyPos = new Vector3(groundAgentPos.x, targetHeight, groundAgentPos.z);

            if (Physics.Linecast(groundAgentPos, flyPos, out RaycastHit hit, ceilingLayerMask, QueryTriggerInteraction.Ignore))
            {
                flyPos = new Vector3(flyPos.x, hit.point.y - (GroundAgent.height + ceilingDistance), flyPos.z);
            }

            if(flyPos.y < MinFlightHeight)
            {
                flyPos.y = MinFlightHeight + (GroundAgent.height + ceilingDistance); // Ensure we don't go below the minimum flight height
            }

            // Set the target position using the eased height offset
            targetPosition = flyPos;
        }

        public Vector3 GetCurrentAgentCorner()
        {
            if (GroundAgent == null) return Vector3.zero;

            // Check if path has corners
            if (GroundAgent.path.corners.Length == 0) return GroundAgent.transform.position;

            Vector3 agentPosition = GroundAgent.transform.position;
            Vector3 closestCorner = GroundAgent.path.corners[0];
            float closestDistance = Vector3.Distance(agentPosition, closestCorner);

            // Find the closest corner of the agent
            foreach (Vector3 corner in GroundAgent.path.corners)
            {
                float distance = Vector3.Distance(agentPosition, corner);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCorner = corner;
                }
            }

            return closestCorner;
        }

        void OnDrawGizmos()
        {
            if (GroundAgent == null) return;

            // Draw the flight path
            Gizmos.color = flightPathColor;
            Gizmos.DrawLine(GroundAgent.transform.position, targetPosition);

            // Draw the ceiling ray
            Gizmos.color = ceilingRayColor;
            Gizmos.DrawLine(GroundAgent.transform.position, new Vector3(GroundAgent.transform.position.x, GroundAgent.transform.position.y + targetHeight, GroundAgent.transform.position.z));

            // Draw the target position
            Gizmos.color = targetPositionColor;
            Gizmos.DrawSphere(targetPosition, 0.1f);

            // Draw the ground agent
            Gizmos.color = groundAgentColor;
            Gizmos.DrawSphere(GroundAgent.transform.position, 0.1f);
        }
    }
}
