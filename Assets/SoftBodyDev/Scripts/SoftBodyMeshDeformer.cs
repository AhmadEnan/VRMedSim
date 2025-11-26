using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Real-time mesh deformation script that simulates soft-body behavior.
/// Supports vertex displacement via raycasts, collisions, or manual input.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class SoftBodyMeshDeformer : MonoBehaviour
{
    [Header("Deformation Parameters")]
    [SerializeField] private float stiffness = 50f;
    [Tooltip("Higher values make the mesh return to its original shape faster")]
    [SerializeField] private float recoverySpeed = 5f;
    [Tooltip("Radius of influence for deformation")]
    [SerializeField] private float deformationRadius = 0.5f;
    [Tooltip("Maximum distance a vertex can be displaced")]
    [SerializeField] private float maxDeformation = 0.3f;

    [Header("Interaction Settings")]
    [SerializeField] private bool enableMouseInteraction = true;
    [SerializeField] private float mouseForce = 0.1f;
    [SerializeField] private bool updateCollider = true;
    [Tooltip("How often to update the mesh collider (in seconds). Lower = more expensive")]
    [SerializeField] private float colliderUpdateInterval = 0.1f;

    [Header("Smoothing")]
    [SerializeField] private bool enableSmoothing = true;
    [Tooltip("Number of smoothing passes to reduce artifacts")]
    [SerializeField] private int smoothingPasses = 1;

    // Internal references
    private Mesh originalMesh;
    private Mesh deformableMesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    // Vertex data
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private Vector3[] vertexVelocities;

    // Collider update timing
    private float lastColliderUpdate;

    void Start()
    {
        InitializeMesh();
    }

    /// <summary>
    /// Initialize mesh and vertex arrays
    /// </summary>
    private void InitializeMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        // Create a copy of the original mesh
        originalMesh = meshFilter.sharedMesh;
        deformableMesh = Instantiate(originalMesh);
        meshFilter.mesh = deformableMesh;

        // Store original vertex positions
        originalVertices = originalMesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        vertexVelocities = new Vector3[originalVertices.Length];

        // Initialize displaced vertices to original positions
        System.Array.Copy(originalVertices, displacedVertices, originalVertices.Length);

        // Ensure mesh collider uses the deformable mesh
        if (meshCollider != null && updateCollider)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = deformableMesh;
        }
    }

    void Update()
    {
        // Handle mouse interaction
        if (enableMouseInteraction && Input.GetMouseButton(0))
        {
            HandleMouseInteraction();
        }

        // Apply spring forces and recover vertices
        RecoverVertices();

        // Apply smoothing if enabled
        if (enableSmoothing)
        {
            SmoothVertices();
        }

        // Update the mesh
        UpdateMesh();

        // Update collider periodically
        if (updateCollider && Time.time - lastColliderUpdate > colliderUpdateInterval)
        {
            UpdateMeshCollider();
            lastColliderUpdate = Time.time;
        }
    }

    /// <summary>
    /// Handle mouse-based mesh deformation via raycasting
    /// </summary>
    private void HandleMouseInteraction()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
        {
            Vector3 hitPoint = transform.InverseTransformPoint(hit.point);
            Vector3 hitNormal = transform.InverseTransformDirection(hit.normal);

            DeformMesh(hitPoint, hitNormal, mouseForce);
        }
    }

    /// <summary>
    /// Deform the mesh at a specific point with a given force
    /// </summary>
    /// <param name="point">Point of impact in local space</param>
    /// <param name="normal">Normal direction of the force</param>
    /// <param name="force">Magnitude of the force</param>
    public void DeformMesh(Vector3 point, Vector3 normal, float force)
    {
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            float distance = Vector3.Distance(displacedVertices[i], point);

            if (distance < deformationRadius)
            {
                // Calculate falloff using smooth interpolation
                float falloff = 1f - (distance / deformationRadius);
                falloff = Mathf.Pow(falloff, 2); // Squared falloff for smoother effect

                // Calculate displacement
                Vector3 displacement = normal * force * falloff;

                // Apply displacement with max limit
                Vector3 newPos = displacedVertices[i] + displacement;
                Vector3 totalDisplacement = newPos - originalVertices[i];

                if (totalDisplacement.magnitude > maxDeformation)
                {
                    totalDisplacement = totalDisplacement.normalized * maxDeformation;
                    newPos = originalVertices[i] + totalDisplacement;
                }

                displacedVertices[i] = newPos;
            }
        }
    }

    /// <summary>
    /// Apply spring forces to return vertices to their original positions
    /// </summary>
    private void RecoverVertices()
    {
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            // Calculate spring force (Hooke's Law)
            Vector3 displacement = displacedVertices[i] - originalVertices[i];
            Vector3 springForce = -stiffness * displacement;

            // Apply force to velocity
            vertexVelocities[i] += springForce * Time.deltaTime;

            // Apply damping
            vertexVelocities[i] *= (1f - recoverySpeed * Time.deltaTime);

            // Update position
            displacedVertices[i] += vertexVelocities[i] * Time.deltaTime;
        }
    }

    /// <summary>
    /// Smooth vertices to reduce sharp artifacts
    /// </summary>
    private void SmoothVertices()
    {
        Vector3[] smoothedVertices = new Vector3[displacedVertices.Length];
        System.Array.Copy(displacedVertices, smoothedVertices, displacedVertices.Length);

        for (int pass = 0; pass < smoothingPasses; pass++)
        {
            for (int i = 0; i < displacedVertices.Length; i++)
            {
                // Simple Laplacian smoothing
                Vector3 sum = Vector3.zero;
                int count = 0;

                // Average with neighboring vertices (simplified approach)
                for (int j = 0; j < displacedVertices.Length; j++)
                {
                    if (i != j)
                    {
                        float distance = Vector3.Distance(originalVertices[i], originalVertices[j]);
                        if (distance < 0.1f) // Only consider very close vertices
                        {
                            sum += smoothedVertices[j];
                            count++;
                        }
                    }
                }

                if (count > 0)
                {
                    Vector3 smoothed = sum / count;
                    smoothedVertices[i] = Vector3.Lerp(smoothedVertices[i], smoothed, 0.5f);
                }
            }

            System.Array.Copy(smoothedVertices, displacedVertices, displacedVertices.Length);
        }
    }

    /// <summary>
    /// Update the mesh with displaced vertices
    /// </summary>
    private void UpdateMesh()
    {
        deformableMesh.vertices = displacedVertices;
        deformableMesh.RecalculateNormals();
        deformableMesh.RecalculateBounds();
    }

    /// <summary>
    /// Update the mesh collider to match deformed mesh
    /// </summary>
    private void UpdateMeshCollider()
    {
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = deformableMesh;
        }
    }

    /// <summary>
    /// Public method to apply external deformation
    /// </summary>
    /// <param name="worldPoint">Point of impact in world space</param>
    /// <param name="worldNormal">Normal direction in world space</param>
    /// <param name="force">Force magnitude</param>
    public void ApplyDeformation(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        Vector3 localNormal = transform.InverseTransformDirection(worldNormal);
        DeformMesh(localPoint, localNormal, force);
    }

    /// <summary>
    /// Reset the mesh to its original shape
    /// </summary>
    public void ResetMesh()
    {
        System.Array.Copy(originalVertices, displacedVertices, originalVertices.Length);
        for (int i = 0; i < vertexVelocities.Length; i++)
        {
            vertexVelocities[i] = Vector3.zero;
        }
        UpdateMesh();
        UpdateMeshCollider();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Deform mesh based on collision
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 localPoint = transform.InverseTransformPoint(contact.point);
            Vector3 localNormal = transform.InverseTransformDirection(contact.normal);
            float impactForce = collision.relativeVelocity.magnitude * 0.01f;
            DeformMesh(localPoint, -localNormal, impactForce);
        }
    }

    void OnDestroy()
    {
        // Clean up instantiated mesh
        if (deformableMesh != null)
        {
            Destroy(deformableMesh);
        }
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (enableMouseInteraction && Application.isPlaying)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(hit.point, deformationRadius);
            }
        }
    }
}