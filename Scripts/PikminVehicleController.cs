using System.Collections;
using System.Collections.Generic;
using LethalMin.Pikmin;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class PikminVehicleController : MonoBehaviour
    {
        public VehicleController controller = null!;
        public BoxCollider PointsRegion = null!;
        public Collider PikminCheckRegion = null!;
        public Transform PikminWarpPoint = null!;
        public Dictionary<PikminAI, Transform> PikminPoints = new Dictionary<PikminAI, Transform>();
        public System.Random RNG = new System.Random(StartOfRound.Instance.randomMapSeed);
        public bool ForceWarpPointOntoNavmesh = true;
        Vector3 OriginalWTLocalPosition = Vector3.zero;

        public void Start()
        {
            InitializeReferences();
            PikminManager.instance.Vehicles.Add(this);
        }

        public void OnDestroy()
        {
            PikminManager.instance.Vehicles.Remove(this);
        }

        public void InitializeReferences()
        {
            if (controller == null)
                controller = GetComponent<VehicleController>();
            if (PointsRegion == null)
                PointsRegion = controller.transform.Find("InsideTruckNavBounds").GetComponent<BoxCollider>();
            if (PikminCheckRegion == null)
                PikminCheckRegion = transform.Find("VehicleBounds").GetComponent<Collider>();

            if (PikminWarpPoint == null)
            {
                // PikminWarpPoint = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                // PikminWarpPoint.GetComponent<Collider>().enabled = false;
                // PikminWarpPoint.gameObject.GetComponent<Renderer>().material = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Materials/MapDotA.mat");
                PikminWarpPoint = new GameObject("Pikmin Warp Point").transform;
                PikminWarpPoint.SetParent(transform);
                PikminWarpPoint.localPosition = new Vector3(0f, -2f, -5f);
                PikminWarpPoint.localScale = new Vector3(1f, 1f, 1f);
                OriginalWTLocalPosition = PikminWarpPoint.localPosition;
            }
        }

        public Transform GetAvaiblePikminPoint(PikminAI pikmin)
        {
            if (PikminPoints.ContainsKey(pikmin))
            {
                return PikminPoints[pikmin];
            }
            else
            {
                GameObject Point = new GameObject($"{pikmin.DebugID}Point");

                Vector3 extents = PointsRegion.size / 2f;
                Vector3 point = new Vector3(
                    Random.Range(-extents.x, extents.x),
                    Random.Range(-extents.y, extents.y),
                    Random.Range(-extents.z, extents.z)
                );

                // Convert local point to world space
                Vector3 randomPoint = PointsRegion.transform.TransformPoint(point);

                // Ensure the point is on the NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
                {
                    Point.transform.position = hit.position;
                }
                else
                {
                    // If no valid NavMesh position found, return the original random point
                    Point.transform.position = randomPoint;
                }
                Point.transform.SetParent(PointsRegion.transform, true);
                Point.transform.rotation = Quaternion.identity;

                PikminPoints[pikmin] = Point.transform;

                LethalMin.Logger.LogInfo($"Created point at {Point.transform.position} for {pikmin.DebugID}");
                return Point.transform;
            }
        }

        public void RemovePikminPoint(PikminAI pikmin)
        {
        }

        public bool IsNearByShip()
        {
            return Vector3.Distance(transform.position, StartOfRound.Instance.shipAnimatorObject.transform.position) < 20f;
        }

        void Update()
        {
            if (controller.carDestroyed &&
            PikminManager.instance.Vehicles.Contains(this))
            {
                PikminManager.instance.Vehicles.Remove(this);
                enabled = false;
            }
        }
        void LateUpdate()
        {
            if (ForceWarpPointOntoNavmesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.TransformPoint(OriginalWTLocalPosition), out hit, 5f, NavMesh.AllAreas))
                {
                    PikminWarpPoint.position = hit.position;
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Failed to ensure the point is on the NavMesh for: {PikminWarpPoint.name}");
                }
            }
        }
    }
}
