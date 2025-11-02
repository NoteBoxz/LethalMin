using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Utils
{
    public class NavMeshPathVisualizer : MonoBehaviour
    {
        public GameObject Point = null!;
        public NavMeshPath path = null!;
        public Color pathColor = Color.green;
        public float lineWidth = 0.2f;

        private LineRenderer lineRenderer = null!;
        public List<GameObject> pathPoints = new List<GameObject>();
        public Vector3 TargetPoint;

        private void Awake()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        public void SetPath(NavMeshPath newPath)
        {
            path = newPath;
            DrawPath();
        }

        private void DrawPath()
        {
            if (path == null || path.corners.Length < 2)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            if (path.status == NavMeshPathStatus.PathPartial)
            {
                lineRenderer.startColor = pathColor;
                lineRenderer.endColor = Color.red;
                if (!PikUtils.IsOutOfRange(pathPoints, pathPoints.Count - 1) && pathPoints[pathPoints.Count - 1] != null)
                {
                    pathPoints[pathPoints.Count - 1].GetComponent<Renderer>().material.color = Color.red;
                }
            }
            else
            {
                lineRenderer.startColor = pathColor;
                lineRenderer.endColor = pathColor;
                if (!PikUtils.IsOutOfRange(pathPoints, pathPoints.Count - 1) && pathPoints[pathPoints.Count - 1] != null)
                {
                    pathPoints[pathPoints.Count - 1].GetComponent<Renderer>().material.color = Color.blue;
                }
            }

            lineRenderer.positionCount = path.corners.Length;
            lineRenderer.SetPositions(path.corners);

            for (int i = 0; i < path.corners.Length; i++)
            {
                Vector3 corner = path.corners[i];
                if (PikUtils.IsOutOfRange(pathPoints, i) || pathPoints[i] == null)
                {
                    GameObject inst = Instantiate(Point, corner, Quaternion.identity);
                    inst.transform.SetParent(transform,true);
                    pathPoints.Add(inst);
                }
                if (pathPoints[i] != pathPoints[pathPoints.Count - 1])
                {
                    pathPoints[i].GetComponent<Renderer>().material.color = pathColor;
                }
                pathPoints[i].transform.localPosition = corner;
            }

            while (PikUtils.IsOutOfRange(path.corners, pathPoints.Count - 1))
            {
                Destroy(pathPoints[pathPoints.Count - 1]);
                pathPoints.RemoveAt(pathPoints.Count - 1);
            }
        }
    }
}