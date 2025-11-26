using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Runtime validator to check physics setup and detect common issues.
/// Attach to any GameObject in the scene to enable validation.
/// </summary>
public class RuntimePhysicsValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [Tooltip("Check physics setup on Start")]
    public bool validateOnStart = true;
    
    [Tooltip("Continuously monitor for physics issues")]
    public bool continuousMonitoring = true;
    
    [Tooltip("Log interval for continuous monitoring (seconds)")]
    public float monitoringInterval = 5f;
    
    [Header("Thresholds")]
    [Tooltip("Velocity magnitude threshold for spike detection")]
    public float velocitySpikeTh = 20f;
    
    [Tooltip("Expected drag value for RBCs")]
    public float expectedDrag = 2f;
    
    [Tooltip("Expected angular drag value for RBCs")]
    public float expectedAngularDrag = 1.5f;

    private SplineFlowManager flowManager;
    private float nextMonitorTime;
    private int lastRBCCount = 0;
    private Dictionary<GameObject, float> maxVelocities = new Dictionary<GameObject, float>();

    void Start()
    {
        flowManager = FindObjectOfType<SplineFlowManager>();
        
        if (validateOnStart)
        {
            ValidatePhysicsSetup();
        }
        
        nextMonitorTime = Time.time + monitoringInterval;
    }

    void Update()
    {
        if (!continuousMonitoring) return;
        
        if (Time.time >= nextMonitorTime)
        {
            MonitorPhysicsIssues();
            nextMonitorTime = Time.time + monitoringInterval;
        }
    }

    [ContextMenu("Validate Physics Setup")]
    void ValidatePhysicsSetup()
    {
        Debug.Log("=== [RuntimePhysicsValidator] Starting Validation ===");
        
        RBCController[] rbcs = FindObjectsOfType<RBCController>();
        
        if (rbcs.Length == 0)
        {
            Debug.LogWarning("[RuntimePhysicsValidator] No RBCs found in scene!");
            return;
        }
        
        int issueCount = 0;
        
        foreach (RBCController rbc in rbcs)
        {
            Rigidbody rb = rbc.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError($"[RuntimePhysicsValidator] {rbc.name} missing Rigidbody!", rbc);
                issueCount++;
                continue;
            }
            
            // Check drag
            if (rb.linearDamping < expectedDrag - 0.5f)
            {
                Debug.LogWarning($"[RuntimePhysicsValidator] {rbc.name} has low drag ({rb.linearDamping}), expected ~{expectedDrag}", rbc);
                issueCount++;
            }
            
            // Check angular drag
            if (rb.angularDamping < expectedAngularDrag - 0.5f)
            {
                Debug.LogWarning($"[RuntimePhysicsValidator] {rbc.name} has low angular drag ({rb.angularDamping}), expected ~{expectedAngularDrag}", rbc);
                issueCount++;
            }
            
            // Check collision detection mode
            if (rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
            {
                Debug.LogWarning($"[RuntimePhysicsValidator] {rbc.name} using Discrete collision detection. Recommend Continuous Dynamic.", rbc);
                issueCount++;
            }
            
            // Check interpolation
            if (rb.interpolation == RigidbodyInterpolation.None)
            {
                Debug.LogWarning($"[RuntimePhysicsValidator] {rbc.name} has no interpolation. May appear jittery.", rbc);
                issueCount++;
            }
            
            // Check physics material
            Collider col = rbc.GetComponent<Collider>();
            if (col != null && col.sharedMaterial != null)
            {
                if (col.sharedMaterial.bounciness > 0.1f)
                {
                    Debug.LogWarning($"[RuntimePhysicsValidator] {rbc.name} has bouncy physics material ({col.sharedMaterial.bounciness}). May cause instability.", rbc);
                    issueCount++;
                }
            }
        }
        
        if (issueCount == 0)
        {
            Debug.Log($"<color=green>[RuntimePhysicsValidator] ✓ All {rbcs.Length} RBCs passed validation!</color>");
        }
        else
        {
            Debug.LogWarning($"[RuntimePhysicsValidator] Found {issueCount} issues across {rbcs.Length} RBCs");
        }
        
        Debug.Log("=== [RuntimePhysicsValidator] Validation Complete ===");
    }

    void MonitorPhysicsIssues()
    {
        if (flowManager == null) return;
        
        RBCController[] rbcs = FindObjectsOfType<RBCController>();
        int currentCount = rbcs.Length;
        
        // Check for RBC count changes (pooling issues)
        if (lastRBCCount > 0 && currentCount != lastRBCCount)
        {
            Debug.LogWarning($"[RuntimePhysicsValidator] RBC count changed: {lastRBCCount} → {currentCount}");
        }
        lastRBCCount = currentCount;
        
        // Check for velocity spikes
        foreach (RBCController rbc in rbcs)
        {
            Rigidbody rb = rbc.GetComponent<Rigidbody>();
            if (rb == null) continue;
            
            float velocity = rb.linearVelocity.magnitude;
            
            if (velocity > velocitySpikeTh)
            {
                Debug.LogError($"[RuntimePhysicsValidator] VELOCITY SPIKE detected on {rbc.name}: {velocity:F2} m/s (threshold: {velocitySpikeTh})", rbc);
            }
            
            // Track max velocity per RBC
            if (!maxVelocities.ContainsKey(rbc.gameObject))
            {
                maxVelocities[rbc.gameObject] = 0f;
            }
            maxVelocities[rbc.gameObject] = Mathf.Max(maxVelocities[rbc.gameObject], velocity);
        }
        
        Debug.Log($"[RuntimePhysicsValidator] Monitoring: {currentCount} active RBCs");
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 150));
        GUILayout.Label("Physics Validation:", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
        
        RBCController[] rbcs = FindObjectsOfType<RBCController>();
        GUILayout.Label($"  Active RBCs: {rbcs.Length}");
        
        if (flowManager != null)
        {
            GUILayout.Label($"  Pool Available: {flowManager.GetComponent<RBCPoolManager>()?.GetAvailableCount() ?? 0}");
        }
        
        float maxVel = 0f;
        foreach (var rbc in rbcs)
        {
            Rigidbody rb = rbc.GetComponent<Rigidbody>();
            if (rb != null) maxVel = Mathf.Max(maxVel, rb.linearVelocity.magnitude);
        }
        
        Color prevColor = GUI.color;
        if (maxVel > velocitySpikeTh)
            GUI.color = Color.red;
        GUILayout.Label($"  Max Velocity: {maxVel:F2} m/s");
        GUI.color = prevColor;
        
        if (GUILayout.Button("Re-validate Physics"))
        {
            ValidatePhysicsSetup();
        }
        
        GUILayout.EndArea();
    }
}
