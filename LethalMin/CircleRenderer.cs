using UnityEngine;

namespace LethalMin
{
    [RequireComponent(typeof(LineRenderer))]
    public class CircleRenderer : MonoBehaviour
    {
        public float radius = 5f;
        public int segments = 60;
        public float lineWidth = 0.1f;
        public Color lineColor = Color.white;

        private LineRenderer lineRenderer;

        void Start()
        {
            lineRenderer = GetComponent<LineRenderer>();
            DrawCircle();
        }

        void DrawCircle()
        {
            lineRenderer.useWorldSpace = false;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = segments + 1;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;

            float deltaTheta = (2f * Mathf.PI) / segments;
            float theta = 0f;

            for (int i = 0; i <= segments; i++)
            {
                float x = radius * Mathf.Cos(theta);
                float y = radius * Mathf.Sin(theta);

                lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
                theta += deltaTheta;
            }
        }

        void OnValidate()
        {
            if (lineRenderer != null)
            {
                DrawCircle();
            }
        }
    }
}