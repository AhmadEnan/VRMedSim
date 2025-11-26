using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Physics-based Red Blood Cell controller.
/// Applies forces (never sets velocity directly) to simulate flow along a spline.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RBCController : MonoBehaviour
{
    [Header("Flow Forces")]
    [Tooltip("Forward flow force strength along the spline tangent")]
    [Range(0f, 100f)] public float flowStrength = 50f;
    
    [Tooltip("Centering force pulling RBC back toward spline center")]
    [Range(0f, 20f)] public float centeringStrength = 2f;
    
    [Header("Tumble Rotation")]
    [Tooltip("Chaotic rotation force for organic movement")]
    [Range(0f, 5f)] public float tumbleStrength = 1f;
    
    [Tooltip("How often to apply random tumble torque (in seconds)")]
    [Range(0.1f, 2f)] public float tumbleInterval = 0.5f;
    
    [Header("Recycling")]
    [Tooltip("Distance threshold from spline end to trigger recycling")]
    public float recycleDistanceThreshold = 0.5f;
    
    [Header("Safety Limits")]
    [Tooltip("Maximum allowed velocity (prevents physics explosions)")]
    public float maxVelocity = 50f;

    // References
    private Rigidbody rb;
    private SplineFlowManager flowManager;
    
    // Tumble timing
    private float tumbleTimer;
    private Vector3 currentTumbleTorque;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // CRITICAL: Disable gravity - flow forces drive movement
        if (rb.useGravity)
        {
            Debug.LogWarning($"[RBCController] {name} has gravity enabled. Disabling for force-based flow.", this);
            rb.useGravity = false;
        }
        
        // Enforce proper physics setup (critical for stability)
        if (rb.linearDamping < 1f)
        {
            Debug.LogWarning($"[RBCController] {name} has low drag ({rb.linearDamping}). Setting to 2.0 for viscosity simulation.", this);
            rb.linearDamping = 2f;
        }
        
        if (rb.angularDamping < 1f)
        {
            Debug.LogWarning($"[RBCController] {name} has low angular drag ({rb.angularDamping}). Setting to 1.5.", this);
            rb.angularDamping = 1.5f;
        }
        
        if (rb.collisionDetectionMode != CollisionDetectionMode.ContinuousDynamic)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        
        if (rb.interpolation == RigidbodyInterpolation.None)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Start()
    {
        // Find the flow manager in the scene
        flowManager = FindObjectOfType<SplineFlowManager>();
        if (flowManager == null)
        {
            Debug.LogError("[RBCController] No SplineFlowManager found in scene! RBC will not move.", this);
            enabled = false;
            return;
        }
        
        // Register with the flow manager
        flowManager.RegisterRBC(this);
        
        // Initialize tumble timer with random offset
        tumbleTimer = Random.Range(0f, tumbleInterval);
    }

    void OnDestroy()
    {
        // Unregister when destroyed
        if (flowManager != null)
        {
            flowManager.UnregisterRBC(this);
        }
    }

    void FixedUpdate()
    {
        if (flowManager == null) return;

        // 1. Get closest point on spline
        SplineFlowManager.SplinePointData pointData = flowManager.GetClosestPointOnSpline(transform.position);
        
        if (pointData.t < 0) return; // Invalid spline data
        
        // 2. Apply Flow Force (forward along tangent)
        rb.AddForce(pointData.tangent * flowStrength, ForceMode.Force);
        
        // 3. Apply Centering Force (spring pulling toward spline)
        Vector3 toSpline = pointData.position - transform.position;
        float distanceFromCenter = toSpline.magnitude;
        
        // Only apply centering if we're drifting away from the spline
        if (distanceFromCenter > 0.01f)
        {
            Vector3 centeringForce = toSpline.normalized * (distanceFromCenter * centeringStrength);
            rb.AddForce(centeringForce, ForceMode.Force);
        }
        
        // 4. Apply Tumble Torque (periodic chaotic rotation)
        tumbleTimer += Time.fixedDeltaTime;
        if (tumbleTimer >= tumbleInterval)
        {
            tumbleTimer = 0f;
            // Generate new random torque direction using Perlin noise for smoother chaos
            float noiseX = Mathf.PerlinNoise(Time.time * 0.5f, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(Time.time * 0.5f, 100f) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(Time.time * 0.5f, 200f) * 2f - 1f;
            currentTumbleTorque = new Vector3(noiseX, noiseY, noiseZ) * tumbleStrength;
        }
        rb.AddTorque(currentTumbleTorque, ForceMode.Force);
        
        // 5. Clamp velocity to prevent physics explosions
        if (rb.linearVelocity.magnitude > maxVelocity)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
            Debug.LogWarning($"[RBCController] {name} velocity clamped from {rb.linearVelocity.magnitude} to {maxVelocity}", this);
        }
        
        // 6. Check if we've reached the end of the spline (for recycling)
        float distanceAlongSpline = flowManager.GetDistanceAlongSpline(pointData.t);
        float splineLength = flowManager.GetSplineLength();
        
        if (splineLength - distanceAlongSpline < recycleDistanceThreshold)
        {
            flowManager.RecycleRBC(gameObject);
        }
    }

    /// <summary>
    /// Called by pool manager when RBC is recycled. Resets physics state.
    /// </summary>
    public void OnRecycled()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        tumbleTimer = Random.Range(0f, tumbleInterval);
        currentTumbleTorque = Vector3.zero;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (flowManager == null || !Application.isPlaying) return;
        
        SplineFlowManager.SplinePointData pointData = flowManager.GetClosestPointOnSpline(transform.position);
        if (pointData.t < 0) return;
        
        // Draw line to closest spline point
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, pointData.position);
        
        // Draw flow direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, pointData.tangent * 0.5f);
        
        // Draw centering force
        Vector3 toSpline = pointData.position - transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, toSpline);
    }
#endif
}
