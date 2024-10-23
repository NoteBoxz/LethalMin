using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshGroundWrapper : MonoBehaviour
{
    [Header("Wrapping Settings")]
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer = 1107298561 | (1 << 28);
    
    [Tooltip("Maximum distance to check for ground")]
    public float maxGroundDistance = 100f;
    
    [Tooltip("Offset from the ground")]
    public float heightOffset = 0.2f;
    
    [Header("Performance Settings")]
    [Tooltip("Update method for the mesh wrapping")]
    public UpdateMode updateMode = UpdateMode.LateUpdate;
    
    [Tooltip("Number of vertices to process per frame")]
    [Range(1, 100)]
    public int verticesPerFrame = 50;
    
    [Tooltip("Only update vertices if they've moved more than this distance")]
    public float updateThreshold = 0.01f;

    public enum UpdateMode
    {
        Manual,
        Update,
        LateUpdate,
        FixedUpdate
    }

    private MeshFilter meshFilter;
    private Vector3[] originalVertices;
    private Vector3[] wrappedVertices;
    private Vector3[] lastWorldPositions;
    private Mesh deformedMesh;
    private int currentVertexIndex;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private bool needsFullUpdate;

    private void Start()
    {
        Initialize();
        needsFullUpdate = true;
    }

    private void Initialize()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            Mesh originalMesh = meshFilter.sharedMesh;
            
            deformedMesh = new Mesh();
            deformedMesh.name = "Wrapped " + originalMesh.name;
            
            deformedMesh.vertices = originalMesh.vertices;
            deformedMesh.triangles = originalMesh.triangles;
            deformedMesh.uv = originalMesh.uv;
            deformedMesh.normals = originalMesh.normals;
            deformedMesh.tangents = originalMesh.tangents;
            
            meshFilter.mesh = deformedMesh;
            
            originalVertices = originalMesh.vertices;
            wrappedVertices = new Vector3[originalVertices.Length];
            lastWorldPositions = new Vector3[originalVertices.Length];
            
            // Copy current vertices
            System.Array.Copy(originalVertices, wrappedVertices, originalVertices.Length);
            
            // Initialize last positions
            for (int i = 0; i < originalVertices.Length; i++)
            {
                lastWorldPositions[i] = transform.TransformPoint(originalVertices[i]);
            }
            
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }
    }

    private void Update()
    {
        if (updateMode == UpdateMode.Update)
            UpdateWrapper();
    }

    private void LateUpdate()
    {
        if (updateMode == UpdateMode.LateUpdate)
            UpdateWrapper();
    }

    private void FixedUpdate()
    {
        if (updateMode == UpdateMode.FixedUpdate)
            UpdateWrapper();
    }

    private void UpdateWrapper()
    {
        if (meshFilter == null) return;

        // Check if transform has changed significantly
        if (Vector3.Distance(lastPosition, transform.position) > updateThreshold ||
            Quaternion.Angle(lastRotation, transform.rotation) > updateThreshold)
        {
            needsFullUpdate = true;
        }

        // Process vertices in batches
        int verticesProcessed = 0;
        while (verticesProcessed < verticesPerFrame && 
              (needsFullUpdate || currentVertexIndex < originalVertices.Length))
        {
            if (currentVertexIndex >= originalVertices.Length)
            {
                if (needsFullUpdate)
                {
                    currentVertexIndex = 0;
                    needsFullUpdate = false;
                }
                else
                {
                    break;
                }
            }

            ProcessVertex(currentVertexIndex);
            currentVertexIndex++;
            verticesProcessed++;
        }

        // Update mesh if any vertices were processed
        if (verticesProcessed > 0)
        {
            deformedMesh.vertices = wrappedVertices;
            deformedMesh.RecalculateNormals();
            deformedMesh.RecalculateBounds();
        }

        // Update transform tracking
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    private void ProcessVertex(int index)
    {
        // Transform vertex position to world space
        Vector3 worldVertex = transform.TransformPoint(originalVertices[index]);
        
        // Only update if the vertex has moved significantly
        if (!needsFullUpdate && Vector3.Distance(worldVertex, lastWorldPositions[index]) < updateThreshold)
        {
            return;
        }

        // Cast ray downward to find ground
        RaycastHit hit;
        Ray ray = new Ray(worldVertex + Vector3.up * maxGroundDistance, Vector3.down);
        
        if (Physics.Raycast(ray, out hit, maxGroundDistance * 2f, groundLayer))
        {
            Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);
            wrappedVertices[index] = new Vector3(
                originalVertices[index].x,
                localHitPoint.y + heightOffset,
                originalVertices[index].z
            );
        }
        else
        {
            wrappedVertices[index] = originalVertices[index];
        }

        lastWorldPositions[index] = worldVertex;
    }

    // Force a full update of all vertices
    public void ForceFullUpdate()
    {
        needsFullUpdate = true;
        currentVertexIndex = 0;
    }

    private void OnValidate()
    {
        if (Application.isPlaying && meshFilter != null)
        {
            ForceFullUpdate();
        }
    }
}