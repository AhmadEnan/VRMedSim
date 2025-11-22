# Member 2: Teeth Simulation & Extraction

**Role:** Interaction & Game Logic Specialist  
**Objective:** Simulate a dental replacement operation (extraction and replacement) using a robust State Machine approach.

**Estimated Time:** 6-8 days  
**Difficulty:** Medium (Simpler than Member 1, focuses on game logic)

---

## Prerequisites & Setup

### Required Software
1. **Unity 2021.3 LTS or newer**
2. **VR Toolkit** (Choose ONE):
   - **Unity XR Interaction Toolkit** (Recommended - built into Unity)
   - **SteamVR Plugin** (if using Valve Index/HTC Vive)
3. **3D Modeling Software** (Optional):
   - Blender (for customizing models)

### Initial Setup Steps
1. Create your work scene: `Member2_WorkScene.unity`
2. Import VR toolkit via Package Manager
3. Set up basic VR rig (Camera + Controllers)

---

## Concept: The State Machine

### Why State Machine Instead of Physics?
For a 2-week POC, a state-based approach is:
- ✅ **Simpler** to implement and debug
- ✅ **More stable** (no physics explosions)
- ✅ **Easier to control** (predictable behavior)
- ✅ **Performant** (no complex calculations)

The key is using **visual/audio feedback** to sell the realism.

### The States

```
┌─────────┐    Pull Force    ┌────────────┐    Release    ┌───────────┐
│  FIXED  │ ───────────────> │ LOOSENING  │ ───────────> │ EXTRACTED │
└─────────┘   (Optional)      └────────────┘              └───────────┘
                                                                 │
                                                                 │ Snap to Socket
                                                                 v
                                                           ┌───────────┐
                                                           │ REPLACED  │
                                                           └───────────┘
```

**State Descriptions:**
1. **FIXED:** Tooth is firmly in the jaw, cannot move
2. **LOOSENING (Optional):** Tooth wiggles when grabbed but doesn't come out (adds realism)
3. **EXTRACTED:** Tooth is free, can be picked up and moved
4. **REPLACED:** New tooth snapped into empty socket

---

## Step-by-Step Implementation

### Step 1: Scene Setup

#### 1.1 Jaw Model
1. **Find/Create a Jaw Model:**
   - Download from Sketchfab, TurboSquid, or use a simple cube with tooth-shaped holes
   - **No soft body needed** - just a static mesh
2. **Import to Unity:**
   - Place in `Assets/Member2_Teeth/Models/`
   - Add to scene, position at origin
3. **Material:**
   - Create a material with gum texture (pinkish-red)
   - **Tip:** Use a slightly glossy shader for wet look

#### 1.2 Tooth Model
1. **Create/Import Tooth:**
   - Simple tooth shape (can be a modified cylinder)
   - Position in the jaw socket
2. **Add Components:**
   ```
   Tooth GameObject:
   ├─ MeshFilter (tooth model)
   ├─ MeshRenderer (white material)
   ├─ Rigidbody
   │  └─ IsKinematic: TRUE (initially)
   │  └─ Use Gravity: FALSE (initially)
   ├─ BoxCollider or MeshCollider
   └─ Tag: "Tooth"
   ```

#### 1.3 Socket Reference Point
1. Create an empty GameObject as child of Jaw
2. Name it "ToothSocket"
3. Position it exactly where the tooth sits
4. **This is your snap point for replacement**

---

### Step 2: VR Interaction Setup

#### 2.1 Using Unity XR Interaction Toolkit

**Add to Tooth GameObject:**
1. Add component: `XR Grab Interactable`
   - Movement Type: `Instantaneous` (tooth follows hand exactly)
   - Throw on Detach: `false`

**Configure XR Rig:**
1. Ensure your controllers have `XR Direct Interactor` or `XR Ray Interactor`
2. Test: You should be able to grab the tooth (it won't pull out yet)

#### 2.2 Using SteamVR (Alternative)

**Add to Tooth GameObject:**
1. Add component: `Interactable`
2. Check "Hide Hand On Attach"

**Script needed:**
```csharp
using Valve.VR.InteractionSystem;

public class ToothInteractable : MonoBehaviour
{
    private Interactable interactable;
    
    void Start()
    {
        interactable = GetComponent<Interactable>();
    }
}
```

---

### Step 3: The Extraction Logic (Core Script)

Create a new script: `ToothExtraction.cs`

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ToothExtraction : MonoBehaviour
{
    [Header("State Machine")]
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
            var controller = grabInteractable.firstInteractorSelecting as XRBaseControllerInteractor;
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
```

---

### Step 4: Socket Trigger Setup

#### 4.1 Create Trigger Collider
1. Select the "ToothSocket" GameObject
2. Add `SphereCollider`
   - Is Trigger: ✅ TRUE
   - Radius: 0.02 (adjust based on tooth size)
3. Add Tag: Create a new tag "ToothSocket"

#### 4.2 Assign References in Inspector
On the Tooth GameObject's `ToothExtraction` script:
- **Socket Transform:** Drag the ToothSocket GameObject
- **Blood Effect:** Create and assign particle system (see Step 5)
- **Audio Source:** Add AudioSource component, assign it
- **Audio Clips:** Assign sound files (see Step 6)

---

### Step 5: Visual Feedback (Blood Particles)

#### 5.1 Create Blood Particle System
1. Create new GameObject: "BloodEffect"
2. Add `Particle System`
3. Configure:

**Main Module:**
- Duration: 0.5
- Looping: ❌ FALSE
- Start Lifetime: 0.3-0.5
- Start Speed: 0.5-1.0
- Start Size: 0.002-0.005
- Start Color: Dark red (#8B0000)

**Emission:**
- Rate over Time: 0
- Bursts: Add burst
  - Time: 0
  - Count: 50-100

**Shape:**
- Shape: Sphere
- Radius: 0.01

**Color over Lifetime:**
- Gradient from red to transparent

**Gravity Modifier:** 0.5 (slight drop)

#### 5.2 Position
- Make it a child of the Jaw
- Position at the socket location
- Disable "Play On Awake"

---

### Step 6: Audio Feedback

#### 6.1 Find/Create Sounds
You need 3 sounds:
1. **Wiggle Sound:** Subtle creaking/grinding (loop this)
2. **Pop Sound:** Sharp "pop" or "crack"
3. **Snap Sound:** Click or snap

**Free Sources:**
- Freesound.org
- Zapsplat.com
- Unity Asset Store (search "medical sounds")

#### 6.2 Import to Unity
1. Place in `Assets/Member2_Teeth/Audio/`
2. Import settings:
   - Load Type: `Decompress On Load` (small files)
   - Compression Format: `Vorbis`

---

### Step 7: Replacement Tooth

#### 7.1 Create Spare Tooth
1. Duplicate your original tooth
2. Name it "ReplacementTooth"
3. Position it on a "tray" or table in the scene
4. Attach the same `ToothExtraction.cs` script
5. Set initial state to `Extracted` (it starts free)

#### 7.2 Multiple Teeth (Optional)
For a full jaw:
1. Create multiple sockets
2. Create multiple teeth
3. Each tooth has its own script instance with its own socket reference

---

### Step 8: Visual Polish

#### 8.1 Gum Texture Swap (Optional)
To show an "empty" socket after extraction:

**Method 1: Texture Swap**
1. Create two materials: GumNormal, GumBloody
2. On extraction, swap the jaw's material

```csharp
public Material gumNormal;
public Material gumBloody;
private MeshRenderer jawRenderer;

void ExtractTooth()
{
    // ... existing code ...
    jawRenderer.material = gumBloody;
}
```

**Method 2: Decal**
1. Use a Unity Decal Projector
2. Project a "bloody hole" texture onto the socket

#### 8.2 Wiggle Animation Enhancement
For more realistic wiggle, use `Quaternion.Slerp`:

```csharp
void HandleLooseningState()
{
    float pullDistance = Vector3.Distance(transform.position, socketTransform.position);
    
    // Smooth wiggle
    Quaternion targetRotation = originalRotation * Quaternion.Euler(
        Mathf.Sin(Time.time * 10f) * wiggleAmount,
        0,
        Mathf.Cos(Time.time * 8f) * wiggleAmount
    );
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    
    if (pullDistance > extractionThreshold)
    {
        ExtractTooth();
    }
}
```

---

## Common Issues & Troubleshooting

| Problem | Cause | Solution |
|---------|-------|----------|
| Can't grab tooth | Missing XR Grab Interactable | Add component to tooth |
| Tooth extracts immediately | Threshold too low | Increase `extractionThreshold` to 0.1 |
| Tooth doesn't snap to socket | Missing trigger collider | Add SphereCollider (Is Trigger) to socket |
| No blood effect | Particle system not assigned | Assign in Inspector |
| Tooth falls through floor | Gravity enabled too early | Set `useGravity = false` initially |
| Wiggle too violent | Wiggle amount too high | Reduce `wiggleAmount` to 2-3 |

---

## Testing Checklist

- [ ] Can grab tooth with VR controller
- [ ] Tooth wiggles when grabbed (Loosening state)
- [ ] Tooth pops out when pulled far enough
- [ ] Blood particle effect plays on extraction
- [ ] "Pop" sound plays on extraction
- [ ] Controller vibrates on extraction
- [ ] Can pick up replacement tooth
- [ ] Replacement tooth snaps to socket when released nearby
- [ ] "Snap" sound plays on replacement

---

## Integration with Team

### File Organization
```
Assets/
  Member2_Teeth/
    Models/
      Jaw.fbx
      Tooth.fbx
    Materials/
      GumMaterial.mat
      ToothMaterial.mat
    Audio/
      wiggle.wav
      pop.wav
      snap.wav
    Scripts/
      ToothExtraction.cs
    Prefabs/
      ToothExtractionSystem.prefab
    Scenes/
      Member2_WorkScene.unity
```

### Creating the Prefab
1. Create an empty GameObject: "DentalSimulation"
2. Add as children:
   - Jaw
   - Tooth (with script)
   - ToothSocket
   - BloodEffect
   - ReplacementTooth
3. Drag "DentalSimulation" to Prefabs folder
4. **Do NOT include:** VR Rig, Camera, Lighting

---

## Timeline Recommendation

- **Day 1:** Setup scene, import models, basic VR grabbing
- **Day 2:** Implement state machine and extraction logic
- **Day 3:** Add wiggle effect and force calculation
- **Day 4:** Particle effects and audio
- **Day 5:** Replacement tooth and socket snapping
- **Day 6:** Polish and testing
- **Days 7-8:** Buffer for issues and integration

---

## Advanced Features (If Time Permits)

### Multiple Teeth Extraction
```csharp
// TeethManager.cs
public class TeethManager : MonoBehaviour
{
    public List<ToothExtraction> teeth;
    private int extractedCount = 0;
    
    void Start()
    {
        foreach (var tooth in teeth)
        {
            tooth.OnToothExtracted += HandleToothExtracted;
        }
    }
    
    void HandleToothExtracted()
    {
        extractedCount++;
        Debug.Log($"Extracted {extractedCount} teeth");
    }
}
```

### Difficulty Levels
- **Easy:** Low extraction threshold (0.03)
- **Hard:** High threshold (0.1), requires more force

### Score System
- Points for successful extraction
- Bonus for clean removal (no wiggle time)
- Penalty for dropping tooth

---

## Resources

- **Unity XR Toolkit Docs:** https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest
- **State Machine Pattern:** https://gameprogrammingpatterns.com/state.html
- **Particle System Tutorial:** https://learn.unity.com/tutorial/particle-systems
- **Free 3D Models:** https://sketchfab.com (search "tooth" or "jaw")

---

## When You're Stuck

1. **Grabbing not working:** Check XR Rig setup and Interaction Layers
2. **State transitions wrong:** Add `Debug.Log(currentState)` in Update()
3. **Socket not detecting:** Verify trigger collider and tags
4. **No sound:** Check AudioSource is not muted, clips are assigned
5. **Ask for help:** Share your Inspector screenshot and describe the behavior
