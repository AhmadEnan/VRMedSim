using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ToothExtraction : MonoBehaviour
{
    public enum ToothState { Fixed, Loosening, Extracted, Replaced }
    public ToothState currentState = ToothState.Fixed;

    [Header("References")]
    public Transform socketTransform; // Assign the ToothSocket GameObject
    public ParticleSystem bloodEffect; // Assign a particle system
    public AudioSource audioSource;

    [Header("Audio Clips")]
    public AudioClip wiggleSound;
    public AudioClip popSound;
    public AudioClip snapSound;

    [Header("Extraction Settings")]
    public float extractionThreshold = 0.05f; // Distance in meters to trigger extraction
    public float wiggleAmount = 5f; // Degrees of rotation when loosening

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isGrabbed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Store original position
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Subscribe to grab events
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        if (currentState == ToothState.Fixed)
        {
            currentState = ToothState.Loosening;
            PlaySound(wiggleSound);
        }
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;

        if (currentState == ToothState.Loosening)
        {
            // Snap back to original position
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            currentState = ToothState.Fixed;
        }
    }

    void Update()
    {
        if (!isGrabbed) return;

        switch (currentState)
        {
            case ToothState.Loosening:
                HandleLooseningState();
                break;
            case ToothState.Extracted:
                // Tooth is free, no special logic needed
                break;
        }
    }

    void HandleLooseningState()
    {
        // Calculate pull distance
        float pullDistance = Vector3.Distance(transform.position, socketTransform.position);

        // Add wiggle effect (rotate slightly based on hand movement)
        float wiggleX = Mathf.Sin(Time.time * 10f) * wiggleAmount;
        float wiggleZ = Mathf.Cos(Time.time * 8f) * wiggleAmount;
        transform.rotation = originalRotation * Quaternion.Euler(wiggleX, 0, wiggleZ);

        // Check if pulled far enough
        if (pullDistance > extractionThreshold)
        {
            ExtractTooth();
        }
    }

    void ExtractTooth()
    {
        currentState = ToothState.Extracted;

        // Enable physics
        rb.isKinematic = false;
        rb.useGravity = true;

        // Visual feedback
        if (bloodEffect != null)
        {
            bloodEffect.transform.position = socketTransform.position;
            bloodEffect.Play();
        }

        // Audio feedback
        PlaySound(popSound);

        // Haptic feedback (if available)
        TriggerHaptic();

        Debug.Log("Tooth extracted!");
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void TriggerHaptic()
    {
        // For XR Interaction Toolkit
        if (grabInteractable.isSelected)
        {
            var controller = grabInteractable.firstInteractorSelecting as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor;
            if (controller != null)
            {
                controller.SendHapticImpulse(0.8f, 0.3f); // Intensity, Duration
            }
        }
    }

    // Called when tooth enters socket trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ToothSocket") && currentState == ToothState.Extracted && !isGrabbed)
        {
            SnapToSocket();
        }
    }

    void SnapToSocket()
    {
        currentState = ToothState.Replaced;

        // Snap to exact position
        transform.position = socketTransform.position;
        transform.rotation = socketTransform.rotation;

        // Lock in place
        rb.isKinematic = true;
        rb.useGravity = false;

        // Audio feedback
        PlaySound(snapSound);

        Debug.Log("Tooth replaced!");
    }
}