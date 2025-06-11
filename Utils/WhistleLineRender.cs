using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalMin.Utils
{
    [RequireComponent(typeof(LineRenderer))]
    [ExecuteInEditMode]
    public class WhistleLineRender : MonoBehaviour
    {
        LineRenderer render = null!;
        public Transform Start = null!, End = null!;
        public int segmentCount = 20; // Number of segments for the curve
        public float curveHeight = 1.0f; // Height of the curve

        void Awake()
        {
            render = GetComponent<LineRenderer>();
        }

        void LateUpdate()
        {
            List<Vector3> vect = new List<Vector3>();

            Vector3 startPosition = Start.localPosition;
            Vector3 endPosition = End.localPosition;
            Vector3 controlPoint = (startPosition + endPosition) / 2 + Vector3.up * curveHeight;

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                Vector3 point = CalculateQuadraticBezierPoint(t, startPosition, controlPoint, endPosition);
                vect.Add(point);
            }

            render.positionCount = vect.Count;
            render.SetPositions(vect.ToArray());
        }

        Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 p = uu * p0; // (1-t)^2 * p0
            p += 2 * u * t * p1; // 2 * (1-t) * t * p1
            p += tt * p2; // t^2 * p2

            return p;
        }
    }
}