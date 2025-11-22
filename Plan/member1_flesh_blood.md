# Member 1: Flesh Simulation, Cutting & Blood Flow

**Role:** Soft Body Physics & Fluid Dynamics Specialist  
**Objective:** Implement the core soft tissue mechanics for the Nasal Cavity/Liver, enable surgical incision capabilities, and simulate interactive blood flow.

**Estimated Time:** 10-12 days  
**Difficulty:** High (Most complex of the three tasks)

---

## Prerequisites & Setup

### Required Software
1. **Unity 2021.3 LTS or newer** (Recommended: 2022.3 LTS)
2. **SofaUnity Plugin** - Clone from: https://github.com/InfinyTech3D/SofaUnity
3. **Mesh Generation Tools:**
   - **Gmsh** (Free): https://gmsh.info/ - For tetrahedral mesh generation
   - **OR MeshLab** (Free): https://www.meshlab.net/

### Initial Setup Steps
1. Create a new Unity 3D project (URP or Built-in Render Pipeline)
2. Clone the SofaUnity repository
3. Follow the SofaUnity installation instructions in their README
4. **CRITICAL:** Ensure you're on Windows or Linux (SofaUnity has limited macOS support)

---

## Task 1: Flesh & Meat Squeezing (Nasal Cavity / Liver)

### Goal
Create a realistic, deformable tissue simulation that reacts to external forces (squeezing) using physics-based differential equations (FEM).

### Why FEM?
The professor mentioned **differential equations**. In computer graphics/physics, this is handled by **FEM (Finite Element Method)**. SOFA solves the partial differential equations (PDEs) of elasticity (Continuum Mechanics). This is what makes the tissue behave realistically when squeezed.

---

### Step 1: Mesh Preparation (CRITICAL - Most Common Failure Point)

#### 1.1 Obtain a 3D Model
- **Option A:** Download from free sources (TurboSquid, Sketchfab, CGTrader)
- **Option B:** Use a simple primitive (Sphere/Cube) for initial testing
- **Recommended:** Start with a simple liver model (easier than nasal cavity)

#### 1.2 Convert to Tetrahedral Mesh
**CRITICAL:** You need a **Tetrahedral Mesh** (volumetric mesh), NOT just a surface mesh (OBJ/FBX). This is the #1 mistake beginners make.

**Using Gmsh (Recommended):**
1. Open Gmsh
2. `File > Open` your surface mesh (.obj, .stl)
3. `Mesh > 3D` to generate volume mesh
4. `Mesh > Optimize 3D (Netgen)` for better quality
5. **Set element size:** `Tools > Options > Mesh > General > Element size factor` (Start with 0.1)
6. `File > Export` as `.msh` or `.vtk`

**Common Mistakes:**
- ❌ Using surface mesh directly → SOFA will crash or not deform
- ❌ Too fine mesh (>10k tetrahedra) → Slow performance
- ❌ Too coarse mesh (<500 tetrahedra) → Unrealistic deformation
- ✅ **Sweet spot:** 2000-5000 tetrahedra for POC

**Testing Your Mesh:**
- Open the `.vtk` file in ParaView (free software) to verify it's volumetric
- You should see the interior filled with tetrahedra, not just the surface

---

### Step 2: Unity Scene Setup

#### 2.1 Create the Scene
1. Create a new scene: `Member1_WorkScene.unity`
2. Add a `SofaContext` GameObject (Right-click Hierarchy > SOFA > Context)
   - This is the root manager for all SOFA simulations
   - **Only ONE per scene**

#### 2.2 Create the Organ GameObject
1. Create an empty GameObject, name it "Liver"
2. Add component: `SofaMesh`
   - Load your `.vtk` file in the `Mesh File` field
   - **Path must be relative to Assets folder** (e.g., `Assets/Member1_Flesh/Meshes/liver.vtk`)

---

### Step 3: Physics Components (The "Differential Equations" Part)

Add these components **in this exact order** to your "Liver" GameObject:

#### 3.1 Solvers (Required for time integration)
1. **`EulerImplicitSolver`**
   - Rayleigh Stiffness: `0.1`
   - Rayleigh Mass: `0.1`
   
2. **`CGLinearSolver`** (Conjugate Gradient - solves the linear system)
   - Iterations: `25` (increase if unstable)
   - Tolerance: `1e-5`

#### 3.2 Mechanical Object (Stores the physics state)
3. **`MechanicalObject`**
   - Template: `Vec3d` (3D vectors)
   - This stores positions, velocities, forces for each node

#### 3.3 Force Field (The FEM magic)
4. **`TetrahedronFEMForceField`**
   - **Young Modulus:** `3000` (Liver softness - experiment between 1000-5000)
   - **Poisson Ratio:** `0.45` (Near-incompressible like real tissue)
   - Method: `Large` (for large deformations)
   
   **What these mean:**
   - Young's Modulus = Stiffness (higher = harder)
   - Poisson's Ratio = Volume preservation (0.5 = incompressible like rubber)

#### 3.4 Mass
5. **`UniformMass`**
   - Total Mass: `1.0` (kg)

---

### Step 4: Collision & Interaction

#### 4.1 Add Collision Models to Liver
1. **`TriangleCollisionModel`** (Surface collision)
2. **`LineCollisionModel`** (Edge collision - optional but helps)
3. **`PointCollisionModel`** (Vertex collision - optional)

#### 4.2 Create the Interaction Tool (VR Controller or Mouse Sphere)

**For VR:**
1. Add your VR controller prefab
2. Add a child GameObject with a `SphereCollider` (Radius: 0.02)
3. Add `SofaCollider` component to the sphere

**For Testing (Mouse):**
1. Create a Sphere GameObject
2. Add `SphereCollider`
3. Add `SofaCollider`
4. Write a simple script to move it with mouse (or use Unity's built-in tools)

#### 4.3 Collision Pipeline (Add to SofaContext)
On your `SofaContext` GameObject:
1. Add `CollisionPipeline`
2. Add `BruteForceBroadPhase`
3. Add `BVHNarrowPhase`
4. Add `MinProximityIntersection` (Alarm Distance: 0.005, Contact Distance: 0.003)
5. Add `DefaultContactManager` (Response: `FrictionContactConstraint`)

**Result:** When you press Play and move the sphere into the liver, it should deform!

---

### Common Issues & Troubleshooting

| Problem | Cause | Solution |
|---------|-------|----------|
| Mesh doesn't appear | Wrong file path | Use relative path from Assets/ |
| Mesh appears but doesn't deform | Surface mesh instead of tetrahedral | Regenerate with Gmsh |
| Simulation explodes/flies away | Timestep too large or unstable solver | Reduce `Time.fixedDeltaTime` to 0.01 in Unity |
| Too slow | Mesh too dense | Reduce tetrahedra count to <3000 |
| Deformation looks wrong | Wrong Young's Modulus | Try values: Soft=1000, Medium=5000, Hard=10000 |

---

## Task 2: Simulation of Cutting/Incision (Forearm or Abdomen)

### Goal
Allow the user to slice through tissue and see the internal structure.

### Understanding Cutting
Cutting involves **Topological Changes** - you're actually removing/splitting tetrahedra in the mesh in real-time. This is computationally expensive.

---

### Step 1: Scene Setup
1. Create a new scene or duplicate your flesh scene
2. Use an Arm or Abdomen model (converted to tetrahedral mesh)
3. Set up the same physics components as Task 1

---

### Step 2: Cutting Components

#### 2.1 Add Cutting Controller
Add to your tissue GameObject:
- **`TetrahedronCuttingController`** (or `SurfaceCuttingController` depending on version)
  - **IMPORTANT:** Check SofaUnity documentation/examples for exact component name

#### 2.2 Create the Scalpel
1. Create a thin, sharp GameObject (use a Quad or import a scalpel model)
2. Add a `BoxCollider` (make it thin like a blade)
3. Tag it as "Scalpel"
4. The CuttingController will detect when this intersects the mesh

**Script Needed (C#):**
```csharp
// Attach to Scalpel GameObject
public class ScalpelCutter : MonoBehaviour
{
    private Vector3 lastPosition;
    
    void Update()
    {
        // Only cut when moving
        if (Vector3.Distance(transform.position, lastPosition) > 0.001f)
        {
            // The CuttingController automatically detects this
            // You might need to call a method depending on SofaUnity version
        }
        lastPosition = transform.position;
    }
}
```

---

### Step 3: Visuals (Showing Internal Tissue)

#### 3.1 Dual Mesh System
SofaUnity separates:
- **Physics Mesh:** Low-poly tetrahedral (for simulation)
- **Visual Mesh:** High-poly surface (for rendering)

#### 3.2 Setup Visual Mesh
1. Create a child GameObject under your tissue
2. Add `MeshFilter` and `MeshRenderer` with your high-res model
3. Add `BarycentricMapping` component
   - Input: Parent's MechanicalObject
   - Output: This visual mesh
   - This makes the visual mesh follow the physics mesh

#### 3.3 Internal Texture/Color
**Critical for realism:**
- Your mesh material needs to show "flesh" color when cut
- **Option A:** Use a double-sided shader (renders backfaces)
- **Option B:** Apply a volumetric texture
- **Simple Solution:** Use a red/pink material with `Cull Off` in shader settings

**Shader Settings (URP):**
1. Create a new Material
2. Set Render Face to `Both`
3. Base Color: Red/Pink (#FF6B6B)

---

### Common Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Cut doesn't happen | CuttingController not detecting scalpel | Ensure scalpel has collider and moves |
| Mesh disappears when cut | Topology broken | Check SofaUnity version compatibility |
| Inside looks hollow | Single-sided material | Use double-sided shader (Cull Off) |
| Performance drops | Too many cuts | Limit cutting to specific areas |

---

## Task 3: Blood Flow Simulation

### Goal
Simulate blood flowing in veins/arteries with interactive blocking (hemostasis).

### Why Not Full CFD?
Simulating fluid dynamics (CFD) inside SOFA is extremely complex and computationally heavy for a 2-week POC. **Solution:** Use Unity's Particle System constrained by the SOFA mesh.

---

### Step 1: Vessel Setup

#### 1.1 Create Vessel Mesh
- Model a simple tubular mesh (cylinder with hollow interior)
- Convert to tetrahedral mesh (same process as Task 1)
- **Tip:** Make it relatively stiff (YoungModulus: 8000-10000)

#### 1.2 Apply Flesh Simulation
- Use the exact same setup as Task 1
- The vessel should be deformable when squeezed

---

### Step 2: Particle System (The "Blood")

#### 2.1 Create Particle System
1. Create a new GameObject: "BloodFlow"
2. Add `Particle System` component

#### 2.2 Configure Emission
- **Rate over Time:** 100-200
- **Start Lifetime:** 5-10 seconds
- **Start Speed:** 0.5-1.0
- **Start Size:** 0.005-0.01 (small!)
- **Start Color:** Dark red (#8B0000)

#### 2.3 Configure Shape
- **Shape:** Mesh
- **Mesh:** Your vessel mesh (or a simplified tube)
- **Type:** Edge
- **Mode:** Random
- This makes particles spawn along the vessel path

#### 2.4 Add Velocity Over Lifetime
- **Linear:** Set Z or Y to 0.5 (direction of flow)
- This makes blood move through the vessel

---

### Step 3: Collision with Deforming Vessel

**This is the tricky part:** The vessel deforms via SOFA, but Unity particles need a Unity collider.

#### 3.1 Add Mesh Collider to Vessel
1. Add `MeshCollider` to your vessel GameObject
2. Set to `Convex: false`
3. **CRITICAL:** This collider needs to update every frame to match SOFA deformation

#### 3.2 Update Collider Script
```csharp
// Attach to Vessel GameObject
public class SofaMeshColliderUpdater : MonoBehaviour
{
    private MeshCollider meshCollider;
    private SofaMesh sofaMesh;
    
    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        sofaMesh = GetComponent<SofaMesh>();
    }
    
    void LateUpdate()
    {
        // Update collider to match deformed mesh
        if (sofaMesh != null && meshCollider != null)
        {
            meshCollider.sharedMesh = null; // Force refresh
            meshCollider.sharedMesh = sofaMesh.GetMesh();
        }
    }
}
```

**Note:** SofaUnity might provide `SofaMeshCollider` component - check documentation first!

#### 3.3 Configure Particle Collision
In Particle System:
1. Enable `Collision` module
2. Type: `World`
3. Mode: `3D`
4. Dampen: 0.5
5. Bounce: 0.1
6. Lifetime Loss: 0.0
7. Collides With: Everything (or specific layer)

---

### Step 4: Interactivity (Blocking)

#### 4.1 The Concept
When you squeeze the vessel:
1. SOFA deforms the mesh (narrows the tube)
2. Unity MeshCollider updates
3. Particles collide with the narrowed walls
4. Particles bunch up (blockage!)

#### 4.2 Visual Enhancement
Add a `SubEmitter` module:
- **Birth:** Spawn a small "splash" particle
- **Collision:** Spawn a "leak" effect if vessel is cut
- This adds visual feedback

---

### Step 5: Blood Components Visualization (Bonus)

To show "blood components" as required:
1. Create 3 particle systems:
   - **Red Blood Cells:** Red, size 0.01
   - **White Blood Cells:** White, size 0.008, lower emission rate
   - **Platelets:** Yellow/tan, size 0.005, lowest emission rate
2. All follow the same vessel path
3. When blocked, they separate by density (heavier ones settle)

---

### Common Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Particles go through vessel | Collider not updating | Implement update script |
| Performance issues | Too many particles | Reduce emission rate to 50-100 |
| Particles don't flow | No velocity | Add Velocity Over Lifetime module |
| Blockage doesn't work | Collider not convex enough | Simplify vessel mesh |

---

## Integration with Team

### File Organization
```
Assets/
  Member1_Flesh/
    Meshes/
      liver.vtk
      vessel.vtk
      arm.vtk
    Materials/
      FleshMaterial.mat
      BloodMaterial.mat
    Scripts/
      SofaMeshColliderUpdater.cs
      ScalpelCutter.cs
    Scenes/
      Member1_WorkScene.unity
```

### Creating Prefabs
Once each feature works:
1. Select the root GameObject (e.g., "Liver")
2. Drag into `Member1_Flesh/Prefabs/` folder
3. **IMPORTANT:** Do NOT include `SofaContext` in the prefab
4. The prefab should be a child of SofaContext in the final scene

---

## Testing Checklist

- [ ] Liver deforms when pressed with VR controller/sphere
- [ ] Deformation looks realistic (not exploding or too stiff)
- [ ] Arm/Abdomen can be cut with scalpel
- [ ] Cut reveals internal texture (not hollow)
- [ ] Vessel deforms when squeezed
- [ ] Blood particles flow through vessel
- [ ] Particles bunch up when vessel is pinched
- [ ] All scenes run at >30 FPS

---

## Timeline Recommendation

- **Days 1-2:** Setup, mesh generation, basic flesh simulation
- **Days 3-5:** Cutting implementation and troubleshooting
- **Days 6-8:** Blood flow particle system
- **Days 9-10:** Polish, optimization, integration
- **Days 11-12:** Buffer for issues and testing

---

## Resources

- **SofaUnity GitHub:** https://github.com/InfinyTech3D/SofaUnity
- **SOFA Documentation:** https://www.sofa-framework.org/community/doc/
- **Gmsh Tutorials:** https://gmsh.info/doc/texinfo/gmsh.html
- **Unity Particle System:** https://docs.unity3d.com/Manual/PartSysReference.html

---

## When You're Stuck

1. **Check SofaUnity Examples:** The repository has example scenes - study them
2. **Mesh Issues:** 90% of problems are bad meshes - regenerate with different settings
3. **Performance:** Reduce mesh density first, optimize later
4. **Ask for Help:** Share screenshots of your Inspector and Console errors
