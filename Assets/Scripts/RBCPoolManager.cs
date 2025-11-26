using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Object pool for RBCs to enable perfect looping without physics glitches.
/// Handles proper recycling: disable → reposition → reset velocity → wait frame → enable
/// </summary>
public class RBCPoolManager : MonoBehaviour
{
    [Header("Pool Configuration")]
    [Tooltip("RBC prefab to pool")]
    public GameObject rbcPrefab;
    
    [Tooltip("Number of RBCs to pre-instantiate")]
    [Range(1, 500)] public int poolSize = 100;
    
    [Header("Recycling")]
    [Tooltip("Reference to the spline flow manager (auto-finds if null)")]
    public SplineFlowManager flowManager;
    
    [Tooltip("Offset from spline start when recycling (in meters)")]
    public float recycleStartOffset = 0f;

    // Pool storage
    private Queue<GameObject> availableRBCs = new Queue<GameObject>();
    private HashSet<GameObject> activeRBCs = new HashSet<GameObject>();
    
    void Awake()
    {
        if (rbcPrefab == null)
        {
            Debug.LogError("[RBCPoolManager] RBC Prefab is not assigned!", this);
            enabled = false;
            return;
        }
        
        // Pre-instantiate pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject rbc = Instantiate(rbcPrefab, transform);
            rbc.name = $"RBC_{i:D3}";
            rbc.SetActive(false);
            availableRBCs.Enqueue(rbc);
        }
        
        Debug.Log($"[RBCPoolManager] Pre-instantiated {poolSize} RBCs", this);
    }

    void Start()
    {
        // Find flow manager if not assigned
        if (flowManager == null)
        {
            flowManager = FindObjectOfType<SplineFlowManager>();
            if (flowManager == null)
            {
                Debug.LogWarning("[RBCPoolManager] No SplineFlowManager found in scene!", this);
            }
        }
    }

    /// <summary>
    /// Gets an RBC from the pool. Returns null if pool is exhausted.
    /// </summary>
    public GameObject GetRBC()
    {
        if (availableRBCs.Count == 0)
        {
            Debug.LogWarning("[RBCPoolManager] Pool exhausted! Consider increasing pool size.", this);
            return null;
        }
        
        GameObject rbc = availableRBCs.Dequeue();
        activeRBCs.Add(rbc);
        return rbc;
    }

    /// <summary>
    /// Returns an RBC to the pool with proper physics reset.
    /// Uses coroutine to wait one frame before re-enabling.
    /// </summary>
    public void ReturnRBC(GameObject rbc)
    {
        if (!activeRBCs.Contains(rbc))
        {
            Debug.LogWarning($"[RBCPoolManager] Attempted to return RBC {rbc.name} that is not tracked as active!", this);
            return;
        }
        
        // Note: Remove from activeRBCs happens in coroutine after recycling
        StartCoroutine(RecycleRBCCoroutine(rbc));
    }

    /// <summary>
    /// Coroutine to safely recycle an RBC:
    /// 1. Disable
    /// 2. Reposition to spline start
    /// 3. Reset physics
    /// 4. Wait one frame
    /// 5. Re-enable
    /// </summary>
    private IEnumerator RecycleRBCCoroutine(GameObject rbc)
    {
        // Step 1: Disable
        rbc.SetActive(false);
        
        // Step 2: Reposition to spline start
        if (flowManager != null)
        {
            float recycleT = recycleStartOffset / flowManager.GetSplineLength();
            recycleT = Mathf.Clamp01(recycleT);
            
            SplineFlowManager.SplinePointData startPoint = flowManager.GetClosestPointOnSpline(
                flowManager.transform.TransformPoint(Vector3.zero)
            );
            
            // Get exact start position
            var splineContainer = flowManager.GetComponent<SplineContainer>();
            if (splineContainer != null && splineContainer.Spline != null)
            {
                if (UnityEngine.Splines.SplineUtility.Evaluate(
                    splineContainer.Spline, 
                    recycleT, 
                    out Unity.Mathematics.float3 pos, 
                    out Unity.Mathematics.float3 tangent, 
                    out Unity.Mathematics.float3 up))
                {
                    rbc.transform.position = flowManager.transform.TransformPoint((Vector3)pos);
                    rbc.transform.rotation = Quaternion.LookRotation((Vector3)tangent, (Vector3)up);
                }
            }
        }
        
        // Step 3: Reset physics
        Rigidbody rb = rbc.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Reset RBC controller state
        RBCController controller = rbc.GetComponent<RBCController>();
        if (controller != null)
        {
            controller.OnRecycled();
        }
        
        // Step 4: Wait one frame (critical for physics stability)
        yield return null;
        
        // Step 5: Remove from active tracking and return to pool
        activeRBCs.Remove(rbc);
        rbc.SetActive(true);
        availableRBCs.Enqueue(rbc);
    }

    /// <summary>
    /// Returns the number of available RBCs in the pool.
    /// </summary>
    public int GetAvailableCount()
    {
        return availableRBCs.Count;
    }

    /// <summary>
    /// Returns the number of currently active RBCs.
    /// </summary>
    public int GetActiveCount()
    {
        return activeRBCs.Count;
    }

#if UNITY_EDITOR
    void OnGUI()
    {
        // Debug overlay
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"RBC Pool Stats:", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
        GUILayout.Label($"  Available: {availableRBCs.Count}");
        GUILayout.Label($"  Active: {activeRBCs.Count}");
        GUILayout.Label($"  Total: {poolSize}");
        GUILayout.EndArea();
    }
#endif
}
