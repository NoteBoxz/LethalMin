using System.Collections.Generic;
using LethalMin;
using UnityEngine;

namespace LethalMin
{
    public class PurplePikminTrajectoryModifier : ITrajectoryModifier
    {
        public bool CanModify(PikminAI pikminAI)
        {
            return pikminAI is PurplePikminAI;
        }

        public void ModifyTrajectory(ref Vector3[] points, PikminAI aiModifying, int pointCount, float timeIncrement, out RaycastHit finalhit)
        {
            PurplePikminAI? purplePikmin = aiModifying as PurplePikminAI;
            finalhit = default;
            List<Vector3> NewPoints = new List<Vector3>(points);  
            if (purplePikmin == null)
            {
                LethalMin.Logger.LogError("PurplePikminTrajectoryModifier: The provided PikminAI is not a PurplePikminAI.");
                return;
            }

            // Calculate the point at which the slam should occur (time)
            float slamTime = purplePikmin.SlamDelay;
            int slamPointIndex = Mathf.FloorToInt(slamTime / timeIncrement);

            // Ensure we don't go out of bounds
            if (slamPointIndex >= pointCount || slamPointIndex < 1)
                return;

            // From the slam point forward, recalculate trajectory with downward force
            bool hitSomething = false;
            int lastValidIndex = pointCount - 1;

            for (int i = slamPointIndex; i < pointCount; i++)
            {
                // Get position at slam point
                Vector3 slamPosition = NewPoints[slamPointIndex];

                // Calculate the time since the slam
                float timeSinceSlam = (i - slamPointIndex) * timeIncrement;

                // Apply straight down trajectory from the slam point
                // v = u + at (where u is initial velocity after slam which is just vertical)
                float slamVelocityY = -purplePikmin.SlamForce / purplePikmin.rb.mass;
                //float currentVelocityY = slamVelocityY + (Physics.gravity.y * timeSinceSlam);

                // s = ut + 0.5at² (where u is initial velocity)
                float displacementY = (slamVelocityY * timeSinceSlam) + (0.5f * Physics.gravity.y * timeSinceSlam * timeSinceSlam);

                // Calculate new position
                Vector3 newPosition = new Vector3(
                    slamPosition.x,
                    slamPosition.y + displacementY,
                    slamPosition.z
                );

                // Check for collision between current point and previous point
                if (i > slamPointIndex)
                {
                    Vector3 direction = (newPosition - NewPoints[i - 1]).normalized;
                    float distance = Vector3.Distance(NewPoints[i - 1], newPosition) * 1.1f; // Similar to rayOverlap

                    if (Physics.Raycast(NewPoints[i - 1], direction, out RaycastHit hit, distance, LethalMin.PikminColideable, QueryTriggerInteraction.Ignore))
                    {
                        if (!hit.collider.isTrigger)
                        {
                            // Set the hit point as the last valid point
                            NewPoints[i] = hit.point;
                            finalhit = hit;
                            hitSomething = true;
                            lastValidIndex = i;
                            break;
                        }
                    }
                }

                NewPoints[i] = newPosition;
            }

            // If we hit something, THEN set the positions
            if (hitSomething)
            {
                for (int i = 0; i < pointCount; i++)
                {
                    if (i > lastValidIndex)
                    {
                        points[i] = NewPoints[lastValidIndex];
                    }
                    else
                    {
                        points[i] = NewPoints[i];
                    }
                }
            }
        }
    }
}