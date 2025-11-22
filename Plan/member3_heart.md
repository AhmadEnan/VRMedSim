# Member 3: Heart Simulation & Deformation

**Role:** Organ Dynamics & Animation Specialist  
**Objective:** Simulate the 3D deformation of a heart over time (beating).

**Estimated Time:** 5-7 days  
**Difficulty:** Low-Medium (Straightforward animation, optional physics challenge)

---

## Prerequisites & Setup

### Required Software
1. **Unity 2021.3 LTS or newer**
2. **3D Modeling Software:**
   - **Blender** (Free, required for Blend Shapes method)
   - OR **Maya** (if you have access)
3. **Optional (for Advanced):**
   - SofaUnity (only if attempting physics-based approach)

### Initial Setup Steps
1. Create your work scene: `Member3_WorkScene.unity`
2. Download or create a heart 3D model
3. Choose your implementation path (Animation or Shader - see below)

---

## Implementation Strategy

You have **TWO options** for the primary goal. Choose based on your comfort level:

| Method | Difficulty | Pros | Cons | Recommended For |
|--------|-----------|------|------|-----------------|
| **Blend Shapes** | Easy | Full control, easy to tweak | Requires 3D modeling | Beginners |
| **Shader Graph** | Medium | GPU-powered, looks organic | Harder to debug | Intermediate |

**Recommendation:** Start with Blend Shapes. If you finish early, try the Shader method.

---

## Method 1: Blend Shapes (Morph Targets) - PRIMARY

### Concept
Blend Shapes let you morph between different mesh states. You'll create two states:
- **Diastole:** Relaxed heart (full size)
- **Systole:** Contracted heart (squeezed)

Unity will interpolate between them smoothly.

---

### Step 1: Create the Heart Model in Blender

#### 1.1 Get a Base Heart Model
**Option A: Download**
- Sketchfab: https://sketchfab.com/search?q=heart&type=models
- TurboSquid, CGTrader
- **Filter:** Free, downloadable, .blend or .fbx format

**Option B: Model from Scratch** (Advanced)
- Use Blender's sculpting tools
- Not recommended for 2-week timeline

#### 1.2 Open in Blender
1. Open Blender (2.8 or newer)
2. Delete default cube
3. `File > Import` your heart model
4. **Important:** Ensure the model is a single mesh (not multiple parts)
   - Select all parts > `Ctrl+J` to join

---

### Step 2: Create Shape Keys (Blend Shapes)

#### 2.1 Add Basis Shape
1. Select the heart mesh
2. Go to **Object Data Properties** (green triangle icon)
3. Find **Shape Keys** panel
4. Click `+` to add a shape key
5. It will create "Basis" automatically (this is your default state)

#### 2.2 Create "Contracted" Shape
1. Click `+` again to add a second shape key
2. Rename it to "Contracted"
3. **Make sure "Contracted" is selected** (highlighted)
4. Enter **Edit Mode** (`Tab`)
5. **Now sculpt the contracted state:**
   - Enable Proportional Editing (`O` key)
   - Select vertices and scale inward (`S` key)
   - Focus on the ventricles (bottom chambers) - they contract most
   - **Tip:** Use `Alt+S` to scale along normals (shrinks uniformly)
6. Exit Edit Mode (`Tab`)

#### 2.3 Test the Shape Keys
1. In Object Mode, select "Basis"
2. Adjust the "Contracted" slider (Value: 0 to 1)
3. You should see the heart morph between relaxed and contracted
4. **If it looks wrong:** Go back to Edit Mode with "Contracted" selected and adjust

---

### Step 3: Export to Unity

#### 3.1 Export from Blender
1. `File > Export > FBX (.fbx)`
2. **Settings:**
   - Selected Objects: ✅ (if you have other objects in scene)
   - Apply Scalings: `FBX All`
   - **CRITICAL:** Check `Bake Animation` (even though we're not using animation timeline)
   - Path Mode: `Copy`
   - Embed Textures: ✅
3. Save as `Heart.fbx`

#### 3.2 Import to Unity
1. Place `Heart.fbx` in `Assets/Member3_Heart/Models/`
2. Select it in Unity
3. In Inspector > Model tab:
   - Import BlendShapes: ✅ **MUST BE CHECKED**
   - Click `Apply`

---

### Step 4: Animate in Unity

#### 4.1 Setup Scene
1. Drag `Heart.fbx` into your scene
2. It should have a `SkinnedMeshRenderer` component (not just MeshRenderer)
3. **Verify:** Expand the `BlendShapes` section in SkinnedMeshRenderer
   - You should see "Contracted" listed

#### 4.2 Create Heartbeat Script

Create `Heartbeat.cs`:

```csharp
using UnityEngine;

public class Heartbeat : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Beats per minute (normal human: 60-100)")]
    public float beatsPerMinute = 72f;
    
    [Tooltip("How much the heart contracts (0-100)")]
    [Range(0f, 100f)]
    public float contractionStrength = 80f;
    
    [Header("References")]
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private int contractedShapeIndex;
    
    [Header("Audio (Optional)")]
    public AudioSource heartbeatAudio;
    public AudioClip beatSound;
    
    private float beatTimer = 0f;
    private bool isContracting = false;
    
    void Start()
    {
        // Get the SkinnedMeshRenderer
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("No SkinnedMeshRenderer found! Make sure you imported with BlendShapes enabled.");
            enabled = false;
            return;
        }
        
        // Find the "Contracted" blend shape index
        contractedShapeIndex = FindBlendShapeIndex("Contracted");
        
        if (contractedShapeIndex == -1)
        {
            Debug.LogError("Blend shape 'Contracted' not found! Check your Blender export.");
            enabled = false;
            return;
        }
        
        Debug.Log($"Heartbeat initialized. Blend shape index: {contractedShapeIndex}");
    }
    
    void Update()
    {
        AnimateHeartbeat();
    }
    
    void AnimateHeartbeat()
    {
        // Convert BPM to frequency (beats per second)
        float frequency = beatsPerMinute / 60f;
        
        // Calculate the blend shape weight using a sine wave
        // This creates a smooth contraction/relaxation cycle
        float time = Time.time * frequency * Mathf.PI * 2f;
        float weight = (Mathf.Sin(time) + 1f) * 0.5f; // Normalize to 0-1
        
        // Apply contraction strength
        weight *= contractionStrength;
        
        // Set the blend shape weight
        skinnedMeshRenderer.SetBlendShapeWeight(contractedShapeIndex, weight);
        
        // Optional: Trigger sound on beat
        if (heartbeatAudio != null && beatSound != null)
        {
            // Detect the peak of contraction (when sine crosses 0.5 going up)
            float previousWeight = (Mathf.Sin((Time.time - Time.deltaTime) * frequency * Mathf.PI * 2f) + 1f) * 0.5f;
            if (weight > 0.5f && previousWeight <= 0.5f)
            {
                heartbeatAudio.PlayOneShot(beatSound);
            }
        }
    }
    
    int FindBlendShapeIndex(string shapeName)
    {
        int blendShapeCount = skinnedMeshRenderer.sharedMesh.blendShapeCount;
        
        for (int i = 0; i < blendShapeCount; i++)
        {
            string name = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
            if (name.Equals(shapeName, System.StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        
        return -1; // Not found
    }
}
```

#### 4.3 Attach Script
1. Select the Heart GameObject
2. Add Component > `Heartbeat`
3. Adjust settings in Inspector:
   - Beats Per Minute: 72 (normal resting)
   - Contraction Strength: 80

#### 4.4 Test
Press Play. The heart should beat rhythmically!

---

### Step 5: Visual Polish

#### 5.1 Material
Create a realistic heart material:

1. Create new Material: `HeartMaterial`
2. **For Built-in Pipeline:**
   - Shader: Standard
   - Albedo: Dark red (#8B0000)
   - Metallic: 0
   - Smoothness: 0.6 (slightly wet look)
   - Normal Map: (optional, for detail)

3. **For URP:**
   - Shader: Universal Render Pipeline/Lit
   - Base Map: Dark red texture
   - Smoothness: 0.6
   - Specular Highlights: On

#### 5.2 Lighting
1. Add a Directional Light (simulates operating room light)
2. Add a Point Light near the heart (subtle, warm color)
3. **Tip:** Use a slight blue rim light for depth

#### 5.3 Audio (Optional but Recommended)
1. Find a heartbeat sound (freesound.org, search "heartbeat")
2. Add `AudioSource` component to Heart
3. Assign to `heartbeatAudio` field in script
4. Assign clip to `beatSound` field

---

## Method 2: Vertex Displacement Shader - ALTERNATIVE

### Concept
Use a shader to move vertices along their normals based on a sine wave. This is GPU-powered and very efficient.

### When to Use This
- You're comfortable with Shader Graph
- You want a "procedural" look
- You don't want to model in Blender

---

### Step 1: Setup Shader Graph

#### 1.1 Create Shader Graph
1. In Unity, `Right-click > Create > Shader Graph > URP > Lit Shader Graph`
2. Name it `HeartbeatShader`
3. Double-click to open

#### 1.2 Build the Graph

**Nodes to Add:**

1. **Time Node**
   - Provides `Time.time`

2. **Multiply Node**
   - Input A: Time (from Time node)
   - Input B: `Float` property "Beat Speed" (default: 5)

3. **Sine Node**
   - Input: Output from Multiply

4. **Remap Node**
   - Input: Sine output
   - From Min: -1
   - From Max: 1
   - To Min: 0
   - To Max: 1
   - (This normalizes sine from -1→1 to 0→1)

5. **Multiply Node #2**
   - Input A: Remap output
   - Input B: `Float` property "Displacement Amount" (default: 0.02)

6. **Normal Vector Node**
   - Space: Object

7. **Multiply Node #3**
   - Input A: Displacement Amount (from #2)
   - Input B: Normal Vector

8. **Position Node**
   - Space: Object

9. **Add Node**
   - Input A: Position
   - Input B: Displaced Normal (from #3)

10. **Connect to Vertex Position Output**
    - Drag Add node output to `Vertex Position` on Master Stack

**Graph Flow:**
```
Time → Multiply(Speed) → Sine → Remap → Multiply(Amount) → Multiply(Normal) → Add(Position) → Vertex Position
```

#### 1.3 Add Properties
In the Blackboard (left panel):
1. Add `Float` property: "Beat Speed" (default: 5)
2. Add `Float` property: "Displacement Amount" (default: 0.02)
3. Add `Color` property: "Base Color" (default: dark red)

Connect Base Color to `Base Color` on Fragment

#### 1.4 Save
Click `Save Asset`

---

### Step 2: Apply to Heart

1. Create a Material using your shader
2. Apply to heart mesh
3. Adjust properties in Inspector:
   - Beat Speed: 3-5 (experiment)
   - Displacement Amount: 0.01-0.05

**Result:** The heart will pulse along its normals!

---

### Troubleshooting Shader Method

| Problem | Cause | Solution |
|---------|-------|----------|
| Heart doesn't move | Vertex Position not connected | Check Master Stack connection |
| Pulsing too fast | Beat Speed too high | Reduce to 2-3 |
| Pulsing too subtle | Displacement too low | Increase to 0.05 |
| Looks distorted | Normals wrong | Ensure Normal Vector is in Object Space |

---

## Advanced: Physics-Based (OPTIONAL - BONUS)

**Only attempt if you finish early and want a challenge.**

This uses SofaUnity to make the heart physically interactive while beating.

### Prerequisites
- SofaUnity installed (see Member 1's guide)
- Understanding of FEM basics

### Quick Steps
1. Convert heart to tetrahedral mesh (Gmsh)
2. Set up FEM components (EulerImplicitSolver, TetrahedronFEMForceField)
3. Create a central node inside the heart
4. Add `StiffSpringForceField` connecting outer nodes to center
5. Write a script to animate the spring `restLength`:

```csharp
public class PhysicsHeartbeat : MonoBehaviour
{
    public float beatsPerMinute = 72f;
    public float contractionAmount = 0.02f;
    private StiffSpringForceField springs;
    private float baseRestLength;
    
    void Start()
    {
        springs = GetComponent<StiffSpringForceField>();
        baseRestLength = springs.restLength;
    }
    
    void Update()
    {
        float frequency = beatsPerMinute / 60f;
        float contraction = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * contractionAmount;
        springs.restLength = baseRestLength - contraction;
    }
}
```

**Why do this?**
- The heart will react physically to touch/poke
- Deformation is more realistic
- Combines animation + physics

**Why skip this?**
- Complex setup
- Not required for POC
- Member 1 already demonstrates SofaUnity

---

## Interaction (Simple Collision)

Even with animation, you can add basic interaction:

### Add Collider
1. Add `MeshCollider` to heart
2. Set `Convex: true`
3. Add `Rigidbody` (IsKinematic: true)

### Haptic Feedback on Touch
```csharp
using UnityEngine.XR.Interaction.Toolkit;

public class HeartTouch : MonoBehaviour
{
    public AudioClip touchSound;
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Check if VR controller touched it
        if (collision.gameObject.CompareTag("Controller"))
        {
            // Play sound
            if (audioSource != null && touchSound != null)
            {
                audioSource.PlayOneShot(touchSound);
            }
            
            // Trigger haptic feedback
            var controller = collision.gameObject.GetComponent<XRBaseController>();
            if (controller != null)
            {
                controller.SendHapticImpulse(0.5f, 0.2f);
            }
        }
    }
}
```

---

## Common Issues & Troubleshooting

| Problem | Cause | Solution |
|---------|-------|----------|
| No blend shapes in Unity | Import settings wrong | Check "Import BlendShapes" in FBX import |
| Heart doesn't animate | Wrong component | Must use SkinnedMeshRenderer, not MeshRenderer |
| Blend shape not found | Name mismatch | Check exact name in Blender vs script |
| Animation too fast/slow | BPM setting | Adjust beatsPerMinute (normal: 60-100) |
| Heart looks flat | No lighting | Add lights to scene |
| Shader not working | URP not set up | Ensure project uses URP |

---

## Testing Checklist

- [ ] Heart beats rhythmically
- [ ] Beat speed is realistic (60-100 BPM)
- [ ] Contraction looks natural (not exploding or too subtle)
- [ ] Material looks like real tissue (wet, slightly glossy)
- [ ] Heartbeat sound syncs with visual beat (if using audio)
- [ ] Can touch heart with VR controller (if interaction added)
- [ ] Runs at 60+ FPS

---

## Integration with Team

### File Organization
```
Assets/
  Member3_Heart/
    Models/
      Heart.fbx
    Materials/
      HeartMaterial.mat
      HeartbeatShader.shadergraph (if using shader method)
    Audio/
      heartbeat.wav
    Scripts/
      Heartbeat.cs
      HeartTouch.cs (optional)
    Prefabs/
      BeatingHeart.prefab
    Scenes/
      Member3_WorkScene.unity
```

### Creating the Prefab
1. Select the Heart GameObject (with all components)
2. Drag to `Prefabs/` folder
3. Name it `BeatingHeart`
4. **Include:** Heart mesh, Heartbeat script, AudioSource
5. **Exclude:** Lights, Camera

---

## Timeline Recommendation

### Blend Shapes Path:
- **Day 1:** Find/download heart model, learn Blender basics
- **Day 2:** Create shape keys in Blender, export to Unity
- **Day 3:** Write Heartbeat script, test animation
- **Day 4:** Add material, lighting, audio
- **Day 5:** Polish and create prefab
- **Days 6-7:** Buffer for issues / attempt shader method

### Shader Graph Path:
- **Day 1:** Find heart model, import to Unity
- **Day 2:** Learn Shader Graph basics
- **Day 3:** Build heartbeat shader
- **Day 4:** Tweak parameters, add material
- **Day 5:** Polish and create prefab
- **Days 6-7:** Buffer / attempt physics method

---

## Advanced Features (If Time Permits)

### Variable Heart Rate
```csharp
public void SetHeartRate(float bpm)
{
    beatsPerMinute = Mathf.Clamp(bpm, 40f, 180f);
}

// Example: Increase heart rate when user is "stressed"
void Update()
{
    if (Input.GetKey(KeyCode.Space))
    {
        SetHeartRate(120f); // Stressed
    }
    else
    {
        SetHeartRate(72f); // Normal
    }
}
```

### Arrhythmia Simulation
```csharp
// Add irregular beats
if (Random.value < 0.05f) // 5% chance
{
    float irregularBeat = Random.Range(0.8f, 1.2f);
    weight *= irregularBeat;
}
```

### Heart Attack Simulation
```csharp
public void TriggerHeartAttack()
{
    StartCoroutine(HeartAttackSequence());
}

IEnumerator HeartAttackSequence()
{
    // Rapid beats
    beatsPerMinute = 150f;
    yield return new WaitForSeconds(3f);
    
    // Slow down
    beatsPerMinute = 30f;
    yield return new WaitForSeconds(2f);
    
    // Stop
    beatsPerMinute = 0f;
}
```

---

## Resources

- **Blender Shape Keys Tutorial:** https://docs.blender.org/manual/en/latest/animation/shape_keys/
- **Unity Blend Shapes:** https://docs.unity3d.com/Manual/BlendShapes.html
- **Shader Graph Tutorial:** https://learn.unity.com/tutorial/introduction-to-shader-graph
- **Free Heart Models:** https://sketchfab.com/search?q=heart+anatomy
- **Heartbeat Sounds:** https://freesound.org/search/?q=heartbeat

---

## When You're Stuck

1. **Blend shapes not importing:** Re-export from Blender with "Bake Animation" checked
2. **Script errors:** Check that blend shape name matches exactly (case-sensitive)
3. **Shader not working:** Ensure you're using URP, not Built-in pipeline
4. **Animation looks weird:** Adjust BPM and contraction strength in Inspector
5. **Ask for help:** Share a screenshot of your SkinnedMeshRenderer component

---

## Summary of Deliverables

### Minimum (Required):
1. ✅ Heart that beats visually using Blend Shapes OR Shader
2. ✅ Realistic beat rate (60-100 BPM)
3. ✅ Clean prefab for integration

### Bonus (Optional):
1. ⭐ Heartbeat audio synced to animation
2. ⭐ Touch interaction with haptic feedback
3. ⭐ Physics-based deformation (SofaUnity)
4. ⭐ Variable heart rate system
