using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    [RequireComponent(typeof(PikminAI))]
    public class PikminPathVisualizer : MonoBehaviour
    {
        private PikminAI pikminAI;
        private NavMeshAgent agent;
        private LineRenderer lineRenderer;
        private Color pathColor;
        private Color pathColor2;

        private void Start()
        {
            pikminAI = GetComponent<PikminAI>();
            agent = pikminAI.agent;

            // Create a new LineRenderer component
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // Set the path color based on the Pikmin type
            pathColor = pikminAI.PminType.PikminColor;
            pathColor.a = 0.5f; // Set some transparency
            // Set the path color based on the Pikmin type
            pathColor2 = pikminAI.PminType.PikminColor2;
            pathColor2.a = 0.5f; // Set some transparency
            lineRenderer.startColor = pathColor;
            lineRenderer.endColor = pathColor2;
        }

        private void Update()
        {
            if (agent.hasPath)
            {
                //DrawPath();
            }
            else
            {
                //ClearPath();
            }
        }

        private void DrawPath()
        {
            var path = agent.path;

            lineRenderer.positionCount = path.corners.Length;
            lineRenderer.SetPositions(path.corners);
        }
        public void DrawPath(NavMeshPath path)
        {
            lineRenderer.positionCount = path.corners.Length;
            lineRenderer.SetPositions(path.corners);
            StartCoroutine(ClearPathAfterTime(1f));
        }
        private System.Collections.IEnumerator ClearPathAfterTime(float time)
        {
            yield return new WaitForSeconds(time);
            ClearPath();
        }

        private void ClearPath()
        {
            lineRenderer.positionCount = 0;
        }
    }
}