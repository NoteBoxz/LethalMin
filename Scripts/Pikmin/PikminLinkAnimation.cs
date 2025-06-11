using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.PlayerLoop;

namespace LethalMin.Pikmin
{
    public enum PathType
    {
        Linear,
        QuadraticBezier,
        CubicBezier
    }

    public class PikminLinkAnimation : MonoBehaviour
    {
        public Transform StartPoint = null!, EndPoint = null!;
        public Transform ControlPoint1 = null!;  // Middle control point for quadratic, first control for cubic
        public Transform ControlPoint2 = null!;  // Second control point for cubic Bezier
        public PathType PathType = PathType.Linear;
        public Dictionary<PikminAI, float> PikminOnLink = new Dictionary<PikminAI, float>();
        public float AnimSpeedMultiplier;
        public bool DrawCurveDetails = true;  // Toggle for detailed curve visualization
        //public float PositionAnimOffset = 1, ScaleAnimOffset = 2f, RotaionAnimOffset = 1;

        // Get point on curve based on path type and t value (0-1)
        public Vector3 GetPointOnPath(float t)
        {
            switch (PathType)
            {
                case PathType.Linear:
                    return Vector3.Lerp(StartPoint.position, EndPoint.position, t);

                case PathType.QuadraticBezier:
                    if (ControlPoint1 == null)
                        return Vector3.Lerp(StartPoint.position, EndPoint.position, t);
                    return QuadraticBezier(StartPoint.position, ControlPoint1.position, EndPoint.position, t);

                case PathType.CubicBezier:
                    if (ControlPoint1 == null || ControlPoint2 == null)
                        return Vector3.Lerp(StartPoint.position, EndPoint.position, t);
                    return CubicBezier(StartPoint.position, ControlPoint1.position, ControlPoint2.position, EndPoint.position, t);

                default:
                    return Vector3.Lerp(StartPoint.position, EndPoint.position, t);
            }
        }

        // Calculate rotation along the curve
        public Quaternion GetRotationOnPath(float t)
        {
            return Quaternion.Lerp(StartPoint.rotation, EndPoint.rotation, t);
        }

        // Quadratic Bezier curve formula
        private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 result = uu * p0; // (1-t)²P₀
            result += 2 * u * t * p1; // 2(1-t)tP₁
            result += tt * p2; // t²P₂

            return result;
        }

        // Cubic Bezier curve formula
        private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 result = uuu * p0; // (1-t)³P₀
            result += 3 * uu * t * p1; // 3(1-t)²tP₁
            result += 3 * u * tt * p2; // 3(1-t)t²P₂
            result += ttt * p3; // t³P₃

            return result;
        }

        private void OnDrawGizmos()
        {
            if (StartPoint == null || EndPoint == null) return;

            // Draw the path based on the path type
            int segments = 20;
            Vector3 prevPoint = StartPoint.position;

            Gizmos.color = Color.yellow;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = GetPointOnPath(t);
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }

            // Draw spheres at start and end points
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(StartPoint.position, 0.3f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(EndPoint.position, 0.3f);

            // Draw control points if available
            if (PathType != PathType.Linear)
            {
                if (ControlPoint1 != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(ControlPoint1.position, 0.2f);
                    Gizmos.DrawLine(StartPoint.position, ControlPoint1.position);
                    Gizmos.DrawLine(EndPoint.position, ControlPoint1.position);
                }

                if (PathType == PathType.CubicBezier && ControlPoint2 != null && ControlPoint1 != null)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(ControlPoint2.position, 0.2f);
                    Gizmos.DrawLine(StartPoint.position, ControlPoint1.position);
                    Gizmos.DrawLine(ControlPoint1.position, ControlPoint2.position);
                    Gizmos.DrawLine(ControlPoint2.position, EndPoint.position);
                }
            }

            // Draw direction arrow
            Vector3 midPoint = GetPointOnPath(0.5f);
            Vector3 direction = (GetPointOnPath(0.55f) - GetPointOnPath(0.45f)).normalized;
            float arrowSize = 0.5f;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(midPoint, direction * arrowSize);
            Gizmos.DrawRay(midPoint + direction * arrowSize,
                Quaternion.Euler(0, 120, 0) * -direction * (arrowSize * 0.5f));
            Gizmos.DrawRay(midPoint + direction * arrowSize,
                Quaternion.Euler(0, -120, 0) * -direction * (arrowSize * 0.5f));

            // Draw animated sphere along the path
            float animationProgress = Mathf.PingPong(Time.time * AnimSpeedMultiplier, 1.0f);
            Vector3 animatedPosition = GetPointOnPath(animationProgress);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(animatedPosition, 0.2f);

            // Draw path details (more segments for visualization)
            if (DrawCurveDetails && PathType != PathType.Linear)
            {
                int detailSegments = 40;
                for (int i = 0; i <= detailSegments; i++)
                {
                    float t = i / (float)detailSegments;
                    Vector3 pos = GetPointOnPath(t);
                    Gizmos.color = Color.Lerp(Color.green, Color.red, t);
                    Gizmos.DrawSphere(pos, 0.05f);
                }
            }
        }
    }
}