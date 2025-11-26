using UnityEngine;

/// <summary>
/// Debug helper to visualize RBC physics forces in real-time.
/// Attach to any RBC to see detailed force information.
/// </summary>
[RequireComponent(typeof(RBCController), typeof(Rigidbody))]
public class RBCDebugVisualizer : MonoBehaviour
{
    [Header("Visualization")]
    public bool showForces = true;
    public bool showVelocity = true;
    public bool logToConsole = false;
    
    [Header("Display Scaling")]
    public float forceScale = 0.1f;
    public float velocityScale = 0.5f;

    private RBCController controller;
    private Rigidbody rb;
    private SplineFlowManager flowManager;

    void Awake()
    {
        controller = GetComponent<RBCController>();
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        flowManager = FindObjectOfType<SplineFlowManager>();
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || rb == null || flowManager == null) return;

        if (showVelocity)
        {
            // Draw velocity vector (blue)
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, rb.linearVelocity * velocityScale);
        }

        if (showForces)
        {
            var pointData = flowManager.GetClosestPointOnSpline(transform.position);
            if (pointData.t < 0) return;

            // Flow force (green)
            Vector3 flowForce = pointData.tangent * controller.flowStrength;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, flowForce * forceScale);

            // Centering force (yellow)
            Vector3 toSpline = pointData.position - transform.position;
            Vector3 centeringForce = toSpline.normalized * (toSpline.magnitude * controller.centeringStrength);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, centeringForce * forceScale);

            // Closest spline point (cyan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, pointData.position);
            Gizmos.DrawWireSphere(pointData.position, 0.02f);
        }
    }

    void Update()
    {
        if (logToConsole && Time.frameCount % 60 == 0) // Log once per second at 60fps
        {
            var pointData = flowManager.GetClosestPointOnSpline(transform.position);
            Debug.Log($"[{name}] t:{pointData.t:F3} | vel:{rb.linearVelocity.magnitude:F2} | dist:{Vector3.Distance(transform.position, pointData.position):F3}");
        }
    }
}
