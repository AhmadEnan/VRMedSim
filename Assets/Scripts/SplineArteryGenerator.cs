using UnityEngine;
using UnityEngine.Splines; // The official package namespace
using Unity.Mathematics;   // Required for float3/quaternion math
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
[RequireComponent(typeof(SplineContainer))] // Automatically adds the Spline tool
public class SplineArteryGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    [Range(3, 32)] public int radialSegments = 12; // "Roundness" of the tube
    public float radius = 0.3f;
    public bool doubleSided = false; // Useful if we go inside the vein in VR

    [Header("Generation")]
    [Tooltip("How many edge loops along the length of the spline?")]
    public int segmentsPerUnit = 5; 
    [Tooltip("The Material used for the artery")]
    public Material arteryMaterial = null;
    
    // Runtime deformation support
    private Vector3[] originalVertices; // Cached original positions for deformation reference
    private Mesh generatedMesh;
    private float lastRadius; // Track radius changes
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    // --- The Button ---
    [ContextMenu("Generate from Spline")]
    public void GenerateMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        SplineContainer splineContainer = GetComponent<SplineContainer>();
        if (splineContainer == null || splineContainer.Spline == null) return;

        Spline spline = splineContainer.Spline; // We focus on the first spline in the container
        Mesh mesh = new Mesh();
        mesh.name = "Spline Artery";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // 1. Calculate step size based on spline length
        float length = spline.GetLength();
        int loopCount = Mathf.Max(2, Mathf.CeilToInt(length * segmentsPerUnit));
        
        // 2. Generate Vertices
        for (int i = 0; i <= loopCount; i++)
        {
            float t = (float)i / loopCount;

            // Get Position, Tangent, and Up vector from the spline package
            // These return types are float3, compatible with Vector3
            if (!SplineUtility.Evaluate(spline, t, out float3 pos, out float3 tangent, out float3 up))
                continue;

            // Construct the rotation (Local Frame) at this point
            // We use the spline's Forward (Tangent) and Up to Orient the ring
            Quaternion rotation = Quaternion.LookRotation(tangent, up);

            for (int j = 0; j <= radialSegments; j++)
            {
                // Create circle in local 2D space (Flat ring)
                float angle = j * Mathf.PI * 2f / radialSegments;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;

                // Rotate ring to match spline flow
                Vector3 localPos = rotation * new Vector3(x, y, 0);
                
                // Add the spline position offset
                // Note: The SplineContainer is usually local space, so we add directly.
                vertices.Add((Vector3)pos + localPos);

                // UVs
                float u = (float)j / radialSegments;
                float v = t; // Map length to 0-1
                uvs.Add(new Vector2(u, v));
            }
        }

        // 3. Generate Triangles
        int vertsPerRing = radialSegments + 1;
        for (int i = 0; i < loopCount; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int current = i * vertsPerRing + j;
                int next = (i + 1) * vertsPerRing + j;

                // Outer faces
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);

                triangles.Add(next);
                triangles.Add(next + 1);
                triangles.Add(current + 1);

                if (doubleSided)
                {
                    // Inner faces (reversed winding)
                    triangles.Add(current);
                    triangles.Add(current + 1);
                    triangles.Add(next);

                    triangles.Add(next);
                    triangles.Add(current + 1);
                    triangles.Add(next + 1);
                }
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && arteryMaterial != null)
            renderer.material = arteryMaterial;
        
        // Assign
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh; // Ready for VR squeezing
        
        // Cache for runtime deformation
        generatedMesh = mesh;
        originalVertices = mesh.vertices;
        lastRadius = radius;
        
        Debug.Log($"[SplineArteryGenerator] Generated mesh with {mesh.vertexCount} vertices");
    }
    
    /// <summary>
    /// Updates mesh deformation based on current radius.
    /// Safe to call at runtime for squeezing effects.
    /// </summary>
    public void UpdateMeshDeformation()
    {
        if (generatedMesh == null || originalVertices == null)
        {
            Debug.LogWarning("[SplineArteryGenerator] Cannot deform - mesh not generated yet. Call GenerateMesh() first.", this);
            return;
        }
        
        // Only update if radius actually changed
        if (Mathf.Approximately(radius, lastRadius))
            return;
        
        SplineContainer splineContainer = GetComponent<SplineContainer>();
        if (splineContainer == null || splineContainer.Spline == null) return;
        
        Spline spline = splineContainer.Spline;
        
        // Recalculate vertices with new radius
        Vector3[] newVertices = new Vector3[originalVertices.Length];
        int loopCount = Mathf.Max(2, Mathf.CeilToInt(spline.GetLength() * segmentsPerUnit));
        int vertsPerRing = radialSegments + 1;
        
        for (int i = 0; i <= loopCount; i++)
        {
            float t = (float)i / loopCount;
            
            if (!SplineUtility.Evaluate(spline, t, out float3 pos, out float3 tangent, out float3 up))
                continue;
            
            Quaternion rotation = Quaternion.LookRotation(tangent, up);
            
            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = j * Mathf.PI * 2f / radialSegments;
                float x = Mathf.Cos(angle) * radius; // Use current radius
                float y = Mathf.Sin(angle) * radius;
                
                Vector3 localPos = rotation * new Vector3(x, y, 0);
                int vertIndex = i * vertsPerRing + j;
                
                if (vertIndex < newVertices.Length)
                {
                    newVertices[vertIndex] = (Vector3)pos + localPos;
                }
            }
        }
        
        // Apply new vertices
        generatedMesh.vertices = newVertices;
        generatedMesh.RecalculateBounds();
        generatedMesh.RecalculateNormals();
        
        // Update collider (expensive - only when necessary)
        meshCollider.sharedMesh = null; // Clear first to force refresh
        meshCollider.sharedMesh = generatedMesh;
        
        lastRadius = radius;
    }
    
    /// <summary>
    /// Sets radius at a specific point along the spline (future enhancement).
    /// Currently applies uniform radius to entire mesh.
    /// </summary>
    public void SetRadiusAtT(float t, float newRadius)
    {
        // For now, just set global radius
        // Future: implement per-segment radius variation
        radius = newRadius;
        UpdateMeshDeformation();
    }
}
