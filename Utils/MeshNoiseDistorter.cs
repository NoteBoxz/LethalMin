using UnityEngine;

namespace LethalMin.Utils
{
    [RequireComponent(typeof(MeshFilter))]
    public class MeshNoiseDistorter : MonoBehaviour
    {
        public float noiseScale = 1f;
        public float distortionStrength = 0.1f;
        public float animationSpeed = 1f;
        public Vector3 noiseOffset;

        private Mesh originalMesh = null!;
        private Vector3[] originalVertices = new Vector3[0];
        private Vector3[] distortedVertices = new Vector3[0];
        private MeshFilter meshFilter = null!;

        private void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            originalMesh = meshFilter.mesh;
            originalVertices = originalMesh.vertices;
            distortedVertices = new Vector3[originalVertices.Length];
        }

        private void Update()
        {
            for (int i = 0; i < originalVertices.Length; i++)
            {
                Vector3 vertex = originalVertices[i];

                float noiseX = Mathf.PerlinNoise((vertex.x + noiseOffset.x) * noiseScale,
                                                 (vertex.y + noiseOffset.y) * noiseScale);
                float noiseY = Mathf.PerlinNoise((vertex.y + noiseOffset.y) * noiseScale,
                                                 (vertex.z + noiseOffset.z) * noiseScale);
                float noiseZ = Mathf.PerlinNoise((vertex.z + noiseOffset.z) * noiseScale,
                                                 (vertex.x + noiseOffset.x) * noiseScale);

                Vector3 distortion = new Vector3(noiseX, noiseY, noiseZ) * distortionStrength;
                distortedVertices[i] = vertex + distortion;
            }

            Mesh distortedMesh = new Mesh();
            distortedMesh.vertices = distortedVertices;
            distortedMesh.triangles = originalMesh.triangles;
            distortedMesh.normals = originalMesh.normals;
            distortedMesh.uv = originalMesh.uv;

            meshFilter.mesh = distortedMesh;

            // Animate the noise offset
            noiseOffset += Vector3.one * animationSpeed * Time.deltaTime;
        }

        private void OnDisable()
        {
            // Restore the original mesh when the script is disabled
            if (meshFilter != null)
            {
                meshFilter.mesh = originalMesh;
            }
        }
    }
}