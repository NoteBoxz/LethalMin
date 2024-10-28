using LethalMin;
using UnityEngine;
// this code was 'borrowed' from https://github.com/ForlornU/Trajectory-Predictor
namespace LethalMin
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPredictor : MonoBehaviour
    {
        #region Members
        private LineRenderer trajectoryLine;
        [SerializeField, Tooltip("The marker will show where the projectile will hit")]
        private Transform hitMarker;
        [SerializeField, Range(10, 100), Tooltip("The maximum number of points the LineRenderer can have")]
        private int maxPoints = 30;
        [SerializeField, Range(0.01f, 0.5f), Tooltip("The time increment used to calculate the trajectory")]
        private float increment = 0.05f;
        [SerializeField, Range(1.05f, 2f), Tooltip("The raycast overlap between points in the trajectory, this is a multiplier of the length between points. 2 = twice as long")]
        private float rayOverlap = 1.1f;

        [SerializeField, Tooltip("Reference to the throw origin transform")]
        private Transform throwOrigin;
        #endregion
        private LayerMask collidersAndRoomMask;
        private void Start()
        {
            if (trajectoryLine == null)
                trajectoryLine = GetComponent<LineRenderer>();

            SetupLineRenderer(); // Add this line

            if (throwOrigin == null)
                LethalMin.Logger.LogWarning("TrajectoryPredictor: Throw origin is not set. Please assign it in the LeaderManager.");

            hitMarker = Instantiate(AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/Target.prefab")).transform;
            if (LethalMin.MeshWrapping)
            {
                hitMarker.transform.Find("Plane").gameObject.AddComponent<MeshGroundWrapper>();
            }
            SetTrajectoryVisible(false);
            collidersAndRoomMask = LethalMin.Instance.PikminColideable;
        }
        private void SetupLineRenderer()
        {
            trajectoryLine.startWidth = 0.05f; // Adjust this value to make the line thinner
            trajectoryLine.endWidth = 0.05f; // Adjust this value to make the line thinner
            trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryLine.startColor = new Color(1f, 0.5f, 0f, 1f); // Orange color
            trajectoryLine.endColor = new Color(1f, 0.5f, 0f, 0.5f); // Orange color with some transparency at the end
        }
        void Update()
        {
            //LethalMin.Logger.LogInfo(throwOrigin);
        }

        public void SetThrowOrigin(Transform origin)
        {
            throwOrigin = origin;
        }

        public void PredictTrajectory(ProjectileProperties projectile)
        {
            if (throwOrigin == null)
            {
                LethalMin.Logger.LogError("TrajectoryPredictor: Throw origin is not set. Cannot predict trajectory.");
                return;
            }
            //LethalMin.Logger.LogInfo("Predicting...");

            SetTrajectoryVisible(true); // Ensure the trajectory is visible when predicting

            Vector3 velocity = projectile.direction * (projectile.initialSpeed / projectile.mass);
            Vector3 position = throwOrigin.position;
            Vector3 nextPosition;
            float overlap;

            UpdateLineRender(maxPoints, (0, position));

            for (int i = 1; i < maxPoints; i++)
            {
                // Estimate velocity and update next predicted position
                velocity = CalculateNewVelocity(velocity, projectile.drag, increment);
                nextPosition = position + velocity * increment;

                // Overlap our rays by small margin to ensure we never miss a surface
                overlap = Vector3.Distance(position, nextPosition) * rayOverlap;

                // When hitting a surface we want to show the surface marker and stop updating our line
                if (Physics.Raycast(position, velocity.normalized, out RaycastHit hit, overlap, collidersAndRoomMask))
                {
                    UpdateLineRender(i + 1, (i, hit.point));
                    MoveHitMarker(hit);
                    break;
                }

                // If nothing is hit, continue rendering the arc without a visual marker
                hitMarker.gameObject.SetActive(false);
                position = nextPosition;
                UpdateLineRender(maxPoints, (i, position));
            }
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
            if (trajectoryLine == null)
            {
                if (GetComponent<LineRenderer>() != null)
                {
                    trajectoryLine = GetComponent<LineRenderer>();
                    SetupLineRenderer();
                    LethalMin.Logger.LogWarning("TrajectoryPredictor: LineRenderer was not set. assinging it in the LeaderManager.");
                }
                else
                {
                    LethalMin.Logger.LogError("TrajectoryPredictor: LineRenderer not found. Cannot set trajectory visibility.");
                }
            }
            else
            {
                trajectoryLine.enabled = visible;
            }
            // add the same checks for hitMarker
            if (hitMarker == null)
            {
                hitMarker = Instantiate(AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Pikmin/Target.prefab")).transform;
                if (LethalMin.MeshWrapping)
                {
                    hitMarker.transform.Find("Plane").gameObject.AddComponent<MeshGroundWrapper>();
                }
                LethalMin.Logger.LogWarning("TrajectoryPredictor: Hit marker was not set. assinging it in the LeaderManager.");
            }
            else
            {
                hitMarker.gameObject.SetActive(visible);
            }
            //LethalMin.Logger.LogInfo($"Trajectory visibility set to: {visible}"); // Add this line for debugging
        }
    }
}