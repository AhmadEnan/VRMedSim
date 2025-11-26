using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Handles VR hand interaction and safe artery squeezing.
/// Prevents "popcorn effect" by slowly animating radius changes and throttling collider updates.
/// </summary>
[RequireComponent(typeof(SplineArteryGenerator))]
public class ArteryController : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Enable VR hand squeezing")]
    public bool enableVRInteraction = true;
    
    [Tooltip("XR Grab Interactable for hand detection (optional)")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    
    [Header("Squeezing Parameters")]
    [Tooltip("Minimum radius multiplier when fully squeezed (0.0 = closed, 1.0 = no change)")]
    [Range(0.1f, 1f)] public float minRadiusMultiplier = 0.3f;
    
    [Tooltip("How fast the artery squeezes (smaller = smoother, safer)")]
    [Range(0.1f, 5f)] public float squeezeSpeed = 1f;
    
    [Tooltip("How fast the artery releases back to normal")]
    [Range(0.1f, 5f)] public float releaseSpeed = 1.5f;
    
    [Header("Collision Safety")]
    [Tooltip("Maximum frequency for mesh collider updates (Hz)")]
    [Range(1f, 30f)] public float maxColliderUpdateHz = 10f;
    
    [Tooltip("Only update collider when actively squeezing (performance optimization)")]
    public bool onlyUpdateWhileSqueezing = true;

    [Header("Debug")]
    [Tooltip("Allow keyboard control for testing (Q=squeeze, E=release)")]
    public bool enableKeyboardDebug = true;

    // References
    private SplineArteryGenerator arteryGenerator;
    
    // State
    private float currentRadiusMultiplier = 1f;
    private float targetRadiusMultiplier = 1f;
    private bool isSqueezing = false;
    private float lastColliderUpdateTime = 0f;
    
    // Original radius (cached)
    private float originalRadius;

    void Awake()
    {
        arteryGenerator = GetComponent<SplineArteryGenerator>();
        if (arteryGenerator == null)
        {
            Debug.LogError("[ArteryController] SplineArteryGenerator not found!", this);
            enabled = false;
            return;
        }
        
        originalRadius = arteryGenerator.radius;
    }

    void Start()
    {
        // Set up VR interaction if enabled
        if (enableVRInteraction && grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabStarted);
            grabInteractable.selectExited.AddListener(OnGrabEnded);
        }
    }

    void Update()
    {
        HandleInput();
        UpdateRadiusAnimation();
    }

    void HandleInput()
    {
        // Keyboard debug controls (using new Input System)
        if (enableKeyboardDebug && Keyboard.current != null)
        {
            if (Keyboard.current.qKey.isPressed)
            {
                StartSqueeze();
            }
            else if (Keyboard.current.eKey.isPressed)
            {
                StopSqueeze();
            }
        }
    }

    void UpdateRadiusAnimation()
    {
        // Smoothly interpolate current radius toward target
        float speed = isSqueezing ? squeezeSpeed : releaseSpeed;
        currentRadiusMultiplier = Mathf.Lerp(
            currentRadiusMultiplier, 
            targetRadiusMultiplier, 
            Time.deltaTime * speed
        );
        
        // Apply to artery generator
        float newRadius = originalRadius * currentRadiusMultiplier;
        arteryGenerator.radius = newRadius;
        
        // Update mesh collider with throttling
        bool shouldUpdate = !onlyUpdateWhileSqueezing || isSqueezing;
        float timeSinceLastUpdate = Time.time - lastColliderUpdateTime;
        float minUpdateInterval = 1f / maxColliderUpdateHz;
        
        if (shouldUpdate && timeSinceLastUpdate >= minUpdateInterval)
        {
            arteryGenerator.UpdateMeshDeformation();
            lastColliderUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Start squeezing the artery (called by VR interaction or keyboard)
    /// </summary>
    public void StartSqueeze()
    {
        isSqueezing = true;
        targetRadiusMultiplier = minRadiusMultiplier;
    }

    /// <summary>
    /// Stop squeezing and return to normal radius
    /// </summary>
    public void StopSqueeze()
    {
        isSqueezing = false;
        targetRadiusMultiplier = 1f;
    }

    /// <summary>
    /// Set custom squeeze amount (for programmatic control)
    /// </summary>
    public void SetSqueezeAmount(float amount)
    {
        amount = Mathf.Clamp01(amount);
        targetRadiusMultiplier = Mathf.Lerp(1f, minRadiusMultiplier, amount);
    }

    // VR Interaction Callbacks
    void OnGrabStarted(SelectEnterEventArgs args)
    {
        StartSqueeze();
        Debug.Log("[ArteryController] Hand grab started - squeezing artery");
    }

    void OnGrabEnded(SelectExitEventArgs args)
    {
        StopSqueeze();
        Debug.Log("[ArteryController] Hand grab released - releasing artery");
    }

    void OnDestroy()
    {
        // Clean up VR listeners
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabStarted);
            grabInteractable.selectExited.RemoveListener(OnGrabEnded);
        }
    }

#if UNITY_EDITOR
    void OnGUI()
    {
        if (!enableKeyboardDebug) return;
        
        GUILayout.BeginArea(new Rect(10, 120, 300, 100));
        GUILayout.Label("Artery Controls:", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
        GUILayout.Label($"  Q = Squeeze | E = Release");
        GUILayout.Label($"  Current Radius: {arteryGenerator.radius:F3}m");
        GUILayout.Label($"  Multiplier: {currentRadiusMultiplier:F2}x");
        GUILayout.EndArea();
    }
#endif
}
