using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

/// <summary>
/// Centralized manager for RBC flow along a spline.
/// Provides spline utilities and handles RBC pooling/recycling.
/// </summary>
[RequireComponent(typeof(SplineContainer))]
public class SplineFlowManager : MonoBehaviour
{
    [Header("Spline Reference")]
    [Tooltip("The spline container (auto-assigned from this GameObject)")]
    private SplineContainer splineContainer;
    
    [Header("RBC Spawning")]
    [Tooltip("RBC prefab to spawn")]
    public GameObject rbcPrefab;
    
    [Tooltip("Number of RBCs to spawn initially")]
    [Range(1, 500)] public int initialRBCCount = 50;
    
    [Tooltip("Spacing between RBCs along the spline (in meters)")]
    [Range(0.1f, 5f)] public float rbcSpacing = 0.5f;
    
    [Header("Pooling")]
    [Tooltip("Pool manager reference (optional, auto-finds if null)")]
    public RBCPoolManager poolManager;

    // Internal tracking
    private List<RBCController> activeRBCs = new List<RBCController>();
    private Spline spline;
    private float splineLength;

    /// <summary>
    /// Data structure for spline point queries
    /// </summary>
    public struct SplinePointData
    {
        public float t;              // Normalized position along spline [0, 1]
        public Vector3 position;     // World position on spline
        public Vector3 tangent;      // Forward direction at this point
        public Vector3 up;           // Up vector at this point
        
        public SplinePointData(float t, Vector3 position, Vector3 tangent, Vector3 up)
        {
            this.t = t;
            this.position = position;
            this.tangent = tangent;
            this.up = up;
        }
    }

    void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
        if (splineContainer == null || splineContainer.Spline == null)
        {
            Debug.LogError("[SplineFlowManager] No SplineContainer or Spline found!", this);
            enabled = false;
            return;
        }
        
        spline = splineContainer.Spline;
        splineLength = spline.GetLength();
        
        Debug.Log($"[SplineFlowManager] Spline length: {splineLength:F2}m", this);
    }

    void Start()
    {
        // Find pool manager if not assigned
        if (poolManager == null)
        {
            poolManager = FindObjectOfType<RBCPoolManager>();
            if (poolManager == null)
            {
                Debug.LogWarning("[SplineFlowManager] No RBCPoolManager found. Creating one dynamically.", this);
                GameObject poolObj = new GameObject("RBC_Pool");
                poolManager = poolObj.AddComponent<RBCPoolManager>();
                poolManager.rbcPrefab = rbcPrefab;
                poolManager.poolSize = initialRBCCount;
            }
        }
        
        // Spawn initial RBC population
        SpawnInitialRBCs();
    }

    void SpawnInitialRBCs()
    {
        if (rbcPrefab == null)
        {
            Debug.LogError("[SplineFlowManager] RBC Prefab is not assigned!", this);
            return;
        }
        
        float currentDistance = 0f;
        int spawned = 0;
        
        while (currentDistance < splineLength && spawned < initialRBCCount)
        {
            float t = currentDistance / splineLength;
            
            // Get spline position
            if (SplineUtility.Evaluate(spline, t, out float3 pos, out float3 tangent, out float3 up))
            {
                GameObject rbc = poolManager.GetRBC();
                if (rbc != null)
                {
                    // Position at spline point (transform to world space)
                    rbc.transform.position = transform.TransformPoint((Vector3)pos);
                    
                    // Orient along spline (transform rotation to world space)
                    Vector3 worldTangent = transform.TransformDirection((Vector3)tangent);
                    Vector3 worldUp = transform.TransformDirection((Vector3)up);
                    rbc.transform.rotation = Quaternion.LookRotation(worldTangent, worldUp);
                    
                    rbc.SetActive(true);
                    spawned++;
                }
            }
            
            currentDistance += rbcSpacing;
        }
        
        Debug.Log($"[SplineFlowManager] Spawned {spawned} RBCs along spline", this);
    }

    /// <summary>
    /// Finds the closest point on the spline to the given world position.
    /// Uses manual sampling to ensure correct world-space calculations.
    /// </summary>
    public SplinePointData GetClosestPointOnSpline(Vector3 worldPosition)
    {
        if (spline == null)
            return new SplinePointData(-1, Vector3.zero, Vector3.forward, Vector3.up);
        
        // Sample the spline at regular intervals to find closest point
        // This is more reliable than GetNearestPoint with coordinate transformations
        int samples = 50; // More samples = more accurate but slower
        float closestDistance = float.MaxValue;
        float closestT = 0f;
        
        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            
            if (SplineUtility.Evaluate(spline, t, out float3 pos, out float3 _, out float3 _))
            {
                Vector3 worldPos = transform.TransformPoint((Vector3)pos);
                float distance = Vector3.Distance(worldPosition, worldPos);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestT = t;
                }
            }
        }
        
        // Refine the closest t value with a local search
        float searchRange = 1f / samples;
        int refinementSteps = 5;
        
        for (int step = 0; step < refinementSteps; step++)
        {
            float stepSize = searchRange / 4f;
            
            for (int i = -2; i <= 2; i++)
            {
                float testT = Mathf.Clamp01(closestT + i * stepSize);
                
                if (SplineUtility.Evaluate(spline, testT, out float3 pos, out float3 _, out float3 _))
                {
                    Vector3 worldPos = transform.TransformPoint((Vector3)pos);
                    float distance = Vector3.Distance(worldPosition, worldPos);
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestT = testT;
                    }
                }
            }
            
            searchRange = stepSize;
        }
        
        // Get final data at the closest t value
        if (!SplineUtility.Evaluate(spline, closestT, out float3 finalPos, out float3 tangent, out float3 up))
        {
            return new SplinePointData(-1, Vector3.zero, Vector3.forward, Vector3.up);
        }
        
        // Convert everything to world space
        Vector3 worldFinalPos = transform.TransformPoint((Vector3)finalPos);
        Vector3 worldTangent = transform.TransformDirection((Vector3)tangent).normalized;
        Vector3 worldUp = transform.TransformDirection((Vector3)up).normalized;
        
        return new SplinePointData(closestT, worldFinalPos, worldTangent, worldUp);
    }

    /// <summary>
    /// Converts normalized t-value to distance along spline in meters.
    /// </summary>
    public float GetDistanceAlongSpline(float t)
    {
        return t * splineLength;
    }

    /// <summary>
    /// Returns total spline length in meters.
    /// </summary>
    public float GetSplineLength()
    {
        return splineLength;
    }

    /// <summary>
    /// Recycles an RBC back to the pool (called when RBC reaches end).
    /// </summary>
    public void RecycleRBC(GameObject rbc)
    {
        if (poolManager != null)
        {
            poolManager.ReturnRBC(rbc);
        }
    }

    /// <summary>
    /// Registers an RBC with this manager.
    /// </summary>
    public void RegisterRBC(RBCController rbc)
    {
        if (!activeRBCs.Contains(rbc))
        {
            activeRBCs.Add(rbc);
        }
    }

    /// <summary>
    /// Unregisters an RBC from this manager.
    /// </summary>
    public void UnregisterRBC(RBCController rbc)
    {
        activeRBCs.Remove(rbc);
    }

    /// <summary>
    /// Returns the number of currently active RBCs.
    /// </summary>
    public int GetActiveRBCCount()
    {
        return activeRBCs.Count;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (spline == null && splineContainer != null)
            spline = splineContainer.Spline;
        
        if (spline == null) return;
        
        // Draw spline path
        int samples = 100;
        Vector3 prevPos = Vector3.zero;
        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            if (SplineUtility.Evaluate(spline, t, out float3 pos, out float3 _, out float3 _))
            {
                Vector3 worldPos = transform.TransformPoint((Vector3)pos);
                
                if (i > 0)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(prevPos, worldPos);
                }
                
                prevPos = worldPos;
            }
        }
        
        // Draw start/end markers
        if (SplineUtility.Evaluate(spline, 0f, out float3 startPos, out float3 _, out float3 _))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint((Vector3)startPos), 0.2f);
        }
        
        if (SplineUtility.Evaluate(spline, 1f, out float3 endPos, out float3 _, out float3 _))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.TransformPoint((Vector3)endPos), 0.2f);
        }
    }
#endif
}
