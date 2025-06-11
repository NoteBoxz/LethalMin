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
                PointsRegion = transform.Find("NavSurface").GetComponent<BoxCollider>();
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
                Point.transform.SetParent(PointsRegion.transform);
                Point.transform.rotation = Quaternion.identity;


                Vector3 extents = PointsRegion.size / 2f;
                Vector3 point = new Vector3(
                    -extents.x + (2 * extents.x) * (float)RNG.NextDouble(),
                    -extents.y + (2 * extents.y) * (float)RNG.NextDouble(),
                    -extents.z + (2 * extents.z) * (float)RNG.NextDouble()
                );

                // Convert local point to world space
                Vector3 randomPoint = PointsRegion.transform.TransformPoint(point);
                Vector3 finalPos = randomPoint;

                // Ensure the point is on the NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, 2.5f, NavMesh.AllAreas))
                {
                    finalPos = hit.position;
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Failed to ensure the point is on the NavMesh for: {pikmin.DebugID}");
                }

                Point.transform.position = finalPos;

                PikminPoints.Add(pikmin, Point.transform);

                // GameObject CustomBounds = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // CustomBounds.GetComponent<Collider>().enabled = false;
                // CustomBounds.transform.SetParent(Point.transform);
                // CustomBounds.transform.localPosition = new Vector3(0f, 0f, 0f);
                // CustomBounds.transform.localScale = new Vector3(1f, 1f, 1f);
                // CustomBounds.GetComponent<Renderer>().material = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Materials/MapDotA.mat");
                LethalMin.Logger.LogInfo($"Created point at {finalPos} for {pikmin.DebugID}");
                return Point.transform;
            }
        }

        public void RemovePikminPoint(PikminAI pikmin)
        {
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
