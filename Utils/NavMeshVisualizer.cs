using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Utils
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class NavMeshVisualizer : MonoBehaviour
    {
        public Material? navMeshMaterial;

        void Start()
        {
            VisualizeNavMesh();
        }

        void VisualizeNavMesh()
        {
            // Get NavMesh triangulation data
            NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

            // Create new mesh and populate with NavMesh data
            Mesh mesh = new Mesh();
            mesh.name = "NavMesh Visualization";
            mesh.vertices = navMeshData.vertices;
            mesh.triangles = navMeshData.indices;

            // Recalculate mesh properties
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Set up mesh components
            GetComponent<MeshFilter>().mesh = mesh;

            // Configure material
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (navMeshMaterial != null)
            {
                renderer.material = navMeshMaterial;
            }
            else
            {
                // Create default transparent blue material
                renderer.material = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Materials/LathType4.mat");
            }
        }
    }
}