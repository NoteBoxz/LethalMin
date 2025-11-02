using System.Collections.Generic;
using LethalMin;
using UnityEngine;
// this code was 'borrowed' from https://github.com/ForlornU/Trajectory-Predictor
namespace LethalMin
{
    public struct ProjectileProperties
    {
        public PikminAI pikminAI;
        public Vector3 direction;
        public Vector3 throwForce; // Change force to throwForce Vector3
        public float mass;
        public float drag;
    }

    // Interface for special trajectory modifiers
    public interface ITrajectoryModifier
    {
        bool CanModify(PikminAI pikminAI);
        void ModifyTrajectory(ref Vector3[] points, PikminAI aiModifying, int pointCount, float timeIncrement, out RaycastHit finalhit);
    }

    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPredictor : MonoBehaviour
    {
        #region Members
        public LineRenderer trajectoryLine = null!;
        [SerializeField, Tooltip("The marker will show where the projectile will hit")]
        public Transform hitMarker = null!;
        [SerializeField, Range(10, 100), Tooltip("The maximum number of points the LineRenderer can have")]
        private int maxPoints = 30;
        [SerializeField, Range(0.01f, 0.5f), Tooltip("The time increment used to calculate the trajectory")]
        private float increment = 0.05f;
        [SerializeField, Range(1.05f, 2f), Tooltip("The raycast overlap between points in the trajectory, this is a multiplier of the length between points. 2 = twice as long")]
        private float rayOverlap = 1.1f;

        [SerializeField, Tooltip("Reference to the throw origin transform")]
        public Transform throwOrigin = null!;
        #endregion

        private LayerMask collidersAndRoomMask;
        private Vector3[] trajectoryPoints = new Vector3[0];

        private void Start()
        {
            if (trajectoryLine == null)
                trajectoryLine = GetComponent<LineRenderer>();

            if (throwOrigin == null)
                LethalMin.Logger.LogWarning("TrajectoryPredictor: Throw origin is not set. Please assign it in the LeaderManager.");

            hitMarker = Instantiate(LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Target.prefab")).transform;
            collidersAndRoomMask = LethalMin.PikminColideable;
            trajectoryPoints = new Vector3[maxPoints];
            SetTrajectoryVisible(false);
        }


        public void PredictTrajectory(ProjectileProperties projectile)
        {
            if (throwOrigin == null)
            {
                LethalMin.Logger.LogError("TrajectoryPredictor: Throw origin is not set. Cannot predict trajectory.");
                return;
            }

            // Calculate initial velocity using directional force
            Vector3 initialVelocity = new Vector3(
                projectile.direction.x * projectile.throwForce.x,
                projectile.direction.y * projectile.throwForce.y,
                projectile.direction.z * projectile.throwForce.z
            );
            Vector3 velocity = initialVelocity / projectile.mass;
            Vector3 position = throwOrigin.position;
            int validPoints = 0;
            bool hitSomething = false;
            RaycastHit finalHit = new RaycastHit();

            // Calculate standard trajectory
            for (int i = 0; i < maxPoints; i++)
            {
                trajectoryPoints[i] = position;
                validPoints++;

                if (i > 0)
                {
                    // Calculate next position
                    velocity = CalculateNewVelocity(velocity, projectile.drag, increment);
                    Vector3 nextPosition = position + velocity * increment;

                    // Check for collisions
                    float overlap = Vector3.Distance(position, nextPosition) * rayOverlap;
                    if (Physics.Raycast(position, velocity.normalized, out RaycastHit hit, overlap, collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                    {
                        if (!hit.collider.isTrigger)
                        {
                            trajectoryPoints[i] = hit.point;
                            finalHit = hit;
                            hitSomething = true;
                            break;
                        }
                    }

                    position = nextPosition;
                }
            }

            // Apply special trajectory modifications if applicable
            if (projectile.pikminAI != null && projectile.pikminAI.trajectoryModifier != null)
            {
                if (projectile.pikminAI.trajectoryModifier.CanModify(projectile.pikminAI))
                {
                    projectile.pikminAI.trajectoryModifier.ModifyTrajectory(ref trajectoryPoints, projectile.pikminAI, validPoints, increment, out RaycastHit FfinalHit);
                    if(!FfinalHit.Equals(default(RaycastHit)))
                    {
                        finalHit = FfinalHit;
                    }
                }
                else
                {
                    LethalMin.Logger.LogWarning($"TrajectoryPredictor: The provided PikminAI ({projectile.pikminAI.DebugID}) does not have a valid trajectory modifier.");
                }
            }

            // Update line renderer
            trajectoryLine.positionCount = validPoints;
            for (int i = 0; i < validPoints; i++)
            {
                trajectoryLine.SetPosition(i, trajectoryPoints[i]);
            }

            // Update hit marker
            if (hitSomething)
            {
                MoveHitMarker(finalHit);
            }
            else
            {
                hitMarker.gameObject.SetActive(false);
            }

            SetTrajectoryVisible(true);
        }


        /// <summary>
        /// Allows us to set line count and an individual position at the same time
        /// </summary>
        /// <param name="count">Number of points in our line</param>
        /// <param name="pointPos">The position of an individual point</param>
        private void UpdateLineRender(int count, (int point, Vector3 pos) pointPos)
        {
            trajectoryLine.positionCount = count;
            trajectoryLine.SetPosition(pointPos.point, pointPos.pos);
        }

        private Vector3 CalculateNewVelocity(Vector3 velocity, float drag, float increment)
        {
            velocity += Physics.gravity * increment;
            velocity *= Mathf.Clamp01(1f - drag * increment);
            return velocity;
        }

        private void MoveHitMarker(RaycastHit hit)
        {
            hitMarker.gameObject.SetActive(true);

            // Offset marker from surface
            float offset = 0.025f;
            hitMarker.position = hit.point + hit.normal * offset;
            //hitMarker.rotation = Quaternion.LookRotation(hit.normal, Vector3.right);
        }

        public void SetTrajectoryVisible(bool visible)
        {
            trajectoryLine.enabled = visible;
            hitMarker.gameObject.SetActive(visible);
        }
    }
}