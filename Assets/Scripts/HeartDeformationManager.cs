using UnityEngine;
using System.Collections.Generic;

public class HeartDeformationManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Assign the empty GameObject located at your index finger tip.")]
    [SerializeField] private Transform vrInteractor;

    [Header("Tissue Properties")]
    [SerializeField] private float activationDistance = 0.3f; 
    [SerializeField] private float tissueRadius = 0.15f;
    [SerializeField] private float complianceForce = 0.05f;

    // Internal cache
    private Renderer[] _heartRenderers;
    private MaterialPropertyBlock _propBlock;
    
    // Shader Property IDs
    private int _hitPosID;
    private int _radiusID;
    private int _forceID;

    private void Start()
    {
        // 1. Automatically find all renderers in the child objects (parts 0-5)
        _heartRenderers = GetComponentsInChildren<Renderer>();
        
        if (_heartRenderers.Length == 0)
            Debug.LogError("HeartDeformationManager: No Renderers found in children! Make sure this script is on the parent 'Heart_Animated_Master'.");

        // 2. Initialize the Property Block
        _propBlock = new MaterialPropertyBlock();

        // 3. Cache Shader IDs
        _hitPosID = Shader.PropertyToID("_HitPosition");
        _radiusID = Shader.PropertyToID("_Radius");
        _forceID = Shader.PropertyToID("_SquishForce");
    }

    private void Update()
    {
        if (vrInteractor == null) return;

        // OPTIMIZATION: Check distance once for the whole object
        float distanceToHand = Vector3.Distance(transform.position, vrInteractor.position);

        // Prepare the data package
        if (distanceToHand < activationDistance)
        {
            _propBlock.SetVector(_hitPosID, vrInteractor.position);
            _propBlock.SetFloat(_radiusID, tissueRadius);
            _propBlock.SetFloat(_forceID, complianceForce);
        }
        else
        {
            // Reset force to 0 so the heart snaps back
            _propBlock.SetFloat(_forceID, 0f);
        }

        // BROADCAST: Apply this data block to all 6 heart pieces
        // This is like sending a single nerve impulse to all muscle fibers at once.
        for (int i = 0; i < _heartRenderers.Length; i++)
        {
            _heartRenderers[i].SetPropertyBlock(_propBlock);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (vrInteractor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, activationDistance);
        }
    }
}
