using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(Rigidbody))]
public class MeshDeformation : MonoBehaviour
{
    [Header("Deformation Settings")]
    [SerializeField] private float deformationStrength = 0.5f;
    [SerializeField] private float deformationRadius = 1f;
    [SerializeField] private float springForce = 50f;
    [SerializeField] private float damping = 5f;

    private Mesh deformingMesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private Vector3[] vertexVelocities;

    private MeshCollider meshCollider;
    private Rigidbody rb;

    void Start()
    {
        InitializeMesh();
    }

    void InitializeMesh()
    {
        // Get the mesh and create a copy to modify
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        deformingMesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.mesh = deformingMesh;

        // Store original vertices
        originalVertices = deformingMesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        vertexVelocities = new Vector3[originalVertices.Length];

        // Copy original positions
        for (int i = 0; i < originalVertices.Length; i++)
        {
            displacedVertices[i] = originalVertices[i];
        }

        // Setup mesh collider
        meshCollider = GetComponent<MeshCollider>();
        meshCollider.convex = true; // Required for Rigidbody collisions
        meshCollider.sharedMesh = deformingMesh;

        // Setup rigidbody
        rb = GetComponent<Rigidbody>();
        // You can set this to kinematic if you don't want the object to fall
        // rb.isKinematic = true;
    }

    void Update()
    {
        // Apply spring forces to return vertices to original positions
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            // Spring force towards original position
            Vector3 displacement = displacedVertices[i] - originalVertices[i];
            Vector3 force = -springForce * displacement;

            // Apply damping
            force -= damping * vertexVelocities[i];

            // Update velocity and position
            vertexVelocities[i] += force * Time.deltaTime;
            displacedVertices[i] += vertexVelocities[i] * Time.deltaTime;
        }

        // Update the mesh
        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();

        // Update collider
        meshCollider.sharedMesh = deformingMesh;
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            DeformAtPoint(contact.point, collision.relativeVelocity.magnitude);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            DeformAtPoint(contact.point, collision.relativeVelocity.magnitude * 0.1f);
        }
    }

    public void DeformAtPoint(Vector3 worldPoint, float impactForce)
    {
        // Convert world point to local space
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        // Find and deform nearby vertices
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            Vector3 vertexPosition = displacedVertices[i];
            float distance = Vector3.Distance(vertexPosition, localPoint);

            if (distance < deformationRadius)
            {
                // Calculate deformation based on distance (closer = more deformation)
                float influence = 1f - (distance / deformationRadius);
                influence = Mathf.Pow(influence, 2); // Smooth falloff

                // Calculate deformation direction (push vertices inward)
                Vector3 deformDirection = (vertexPosition - localPoint).normalized;

                // Apply deformation
                float deformAmount = deformationStrength * influence * impactForce * 0.01f;
                displacedVertices[i] += deformDirection * deformAmount;

                // Add some velocity for springiness
                vertexVelocities[i] += deformDirection * deformAmount * 10f;
            }
        }
    }

    // Public method to manually deform from code or other scripts
    public void ApplyDeformation(Vector3 worldPoint, float force = 1f)
    {
        DeformAtPoint(worldPoint, force);
    }

    // Reset mesh to original shape
    public void ResetMesh()
    {
        for (int i = 0; i < originalVertices.Length; i++)
        {
            displacedVertices[i] = originalVertices[i];
            vertexVelocities[i] = Vector3.zero;
        }

        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();
    }

    void OnDrawGizmosSelected()
    {
        // Visualize deformation radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, deformationRadius);
    }
}