# Project Integration & Merge Guide

**Objective:** Ensure that 3 team members can work simultaneously without overwriting each other's work and easily combine everything at the end.

**Critical Success Factors:**
- ✅ Separate work scenes (no conflicts)
- ✅ Shared VR setup and common assets
- ✅ Proper prefab creation
- ✅ Clean Git workflow
- ✅ Coordinated SofaUnity context management

---

## 1. Initial Project Setup (Do This FIRST - Together)

### 1.1 Create the Unity Project
**Designate ONE person** (recommend Member 2, simplest task) to:

1. Create new Unity project:
   - Version: **Unity 2021.3 LTS** or **2022.3 LTS** (everyone must use same version!)
   - Template: **3D (URP)** or **3D Core**
   - Name: `VR_Medical_Simulation`

2. Set up VR:
   - Install **Unity XR Interaction Toolkit** via Package Manager
   - Create basic VR rig (XR Origin + Controllers)
   - Test that VR works before proceeding

3. Install SofaUnity (for Member 1):
   - Clone SofaUnity repo
   - Follow installation instructions
   - **CRITICAL:** Test with a simple example scene to verify it works

### 1.2 Create Folder Structure

```
Assets/
├── _Project/
│   ├── Common/                    # Shared assets
│   │   ├── Materials/
│   │   ├── Prefabs/
│   │   │   └── VR_Rig.prefab     # Shared VR rig
│   │   ├── Scripts/
│   │   └── Audio/
│   ├── Member1_Flesh/
│   │   ├── Meshes/
│   │   ├── Materials/
│   │   ├── Scripts/
│   │   ├── Prefabs/
│   │   └── Scenes/
│   ├── Member2_Teeth/
│   │   ├── Models/
│   │   ├── Materials/
│   │   ├── Audio/
│   │   ├── Scripts/
│   │   ├── Prefabs/
│   │   └── Scenes/
│   └── Member3_Heart/
│       ├── Models/
│       ├── Materials/
│       ├── Audio/
│       ├── Scripts/
│       ├── Prefabs/
│       └── Scenes/
└── Plugins/
    └── SofaUnity/                 # Member 1's physics engine
```

### 1.3 Create Work Scenes

Create these scenes in `Assets/_Project/Scenes/`:

1. **`MasterScene.unity`** (Final integration scene - DON'T TOUCH until merge time)
2. **`Member1_WorkScene.unity`** (Member 1's workspace)
3. **`Member2_WorkScene.unity`** (Member 2's workspace)
4. **`Member3_WorkScene.unity`** (Member 3's workspace)

**Each work scene should contain:**
- VR Rig (instance of shared prefab)
- Directional Light
- Ground plane (for reference)
- Member 1's scene ONLY: SofaContext

### 1.4 Create Shared VR Rig Prefab

**Why:** All members need VR controllers. Create once, share everywhere.

1. Set up XR Origin with controllers in a test scene
2. Configure interaction layers
3. Drag to `Assets/_Project/Common/Prefabs/VR_Rig.prefab`
4. **Everyone uses this prefab** - don't modify locally

---

## 2. Git Repository Setup

### 2.1 Initialize Repository

**One person does this:**

```bash
cd VR_Medical_Simulation
git init
git add .
git commit -m "Initial Unity project setup"
```

### 2.2 Create .gitignore

**CRITICAL:** Use Unity's `.gitignore`. Create `.gitignore` file:

```gitignore
# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/

# Visual Studio cache
.vs/

# Rider cache
.idea/

# OS generated
.DS_Store
Thumbs.db

# Unity specific
*.csproj
*.unityproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# IMPORTANT: DO commit .meta files!
# They contain GUIDs that Unity needs
```

### 2.3 Push to Remote

```bash
# Create repo on GitHub/GitLab
git remote add origin <your-repo-url>
git branch -M main
git push -u origin main
```

### 2.4 Team Members Clone

```bash
git clone <your-repo-url>
cd VR_Medical_Simulation
```

**IMPORTANT:** Everyone must use the **same Unity version**!

---

## 3. The Golden Rule: Work in Separate Scenes

### Why This Matters
Unity scene files (`.unity`) are **binary-ish** and merge conflicts are nearly impossible to resolve. You'll lose work.

### The Workflow

**Member 1:**
- Opens `Member1_WorkScene.unity`
- Works on Flesh, Cutting, Blood
- **NEVER** opens Member2 or Member3's scenes

**Member 2:**
- Opens `Member2_WorkScene.unity`
- Works on Teeth extraction
- **NEVER** opens other member's scenes

**Member 3:**
- Opens `Member3_WorkScene.unity`
- Works on Heart beating
- **NEVER** opens other member's scenes

**Result:** No scene file conflicts in Git!

---

## 4. Daily Git Workflow

### 4.1 Feature Branches (Recommended)

Each member works on their own branch:

```bash
# Member 1
git checkout -b feature/flesh-simulation

# Member 2
git checkout -b feature/teeth-extraction

# Member 3
git checkout -b feature/heart-beating
```

### 4.2 Daily Routine

**Before starting work:**
```bash
git pull origin main  # Get latest changes
```

**During work:**
```bash
# Commit often (every hour or after completing a feature)
git add .
git commit -m "Added liver squeezing physics"
```

**End of day:**
```bash
git push origin feature/your-branch-name
```

### 4.3 Merging to Main

**When your feature is complete:**

```bash
# Update your branch with latest main
git checkout main
git pull origin main
git checkout feature/your-branch-name
git merge main

# Resolve any conflicts (should be minimal if following structure)
# Test that everything still works

# Push and create Pull Request
git push origin feature/your-branch-name
```

**On GitHub/GitLab:** Create Pull Request → Review → Merge

---

## 5. Shared Assets & Dependencies

### 5.1 Common Assets

Place in `Assets/_Project/Common/`:

**Materials:**
- `FleshMaterial.mat` (red, glossy)
- `BloodMaterial.mat` (dark red)
- `BoneMaterial.mat` (off-white)

**Scripts:**
- Any utility scripts used by multiple members

**Audio:**
- Background ambience (operating room sounds)

### 5.2 VR Rig Coordination

**CRITICAL:** Don't modify the shared VR rig prefab without coordinating!

**If you need to modify:**
1. Announce in team chat
2. Make changes
3. Test with all scenes
4. Commit with clear message: `"Modified VR_Rig: Added haptic feedback"`

### 5.3 Package Dependencies

**Everyone must have the same packages:**

Required packages (check `Packages/manifest.json`):
- `com.unity.xr.interaction.toolkit` (same version!)
- `com.unity.render-pipelines.universal` (if using URP)

**When adding a new package:**
1. Announce to team
2. Add via Package Manager
3. Commit `Packages/manifest.json`
4. Team members pull and Unity auto-installs

---

## 6. SofaUnity Specifics (Member 1 + Integration)

### 6.1 During Development

**Member 1's Work Scene:**
- Has `SofaContext` GameObject at root
- All SOFA simulations are children of this context

**Members 2 & 3's Scenes:**
- **NO SofaContext** needed
- They don't use SOFA physics

### 6.2 During Integration (Final Merge)

**MasterScene Setup:**
```
MasterScene
├── SofaContext (ONE instance only)
├── VR_Rig
├── Directional Light
├── Environment
├── [Member 1's Prefabs] (children of scene, not SofaContext)
│   ├── LiverSimulation
│   ├── VesselWithBlood
│   └── ArmCutting
├── [Member 2's Prefab]
│   └── DentalSimulation
└── [Member 3's Prefab]
    └── BeatingHeart
```

**CRITICAL:** Member 1's prefabs should **NOT** contain SofaContext inside them. The context is global in the scene.

---

## 7. Creating Prefabs (Each Member)

### 7.1 When to Create Prefabs

Create prefabs when:
- ✅ Your feature works in your work scene
- ✅ All scripts are attached and configured
- ✅ Materials are applied
- ✅ Audio is set up

### 7.2 Member 1: Flesh/Blood Prefabs

**Create 3 separate prefabs:**

1. **LiverSimulation.prefab**
   ```
   LiverSimulation
   ├── Liver (SofaMesh + FEM components)
   └── InteractionSphere
   ```

2. **ArmCutting.prefab**
   ```
   ArmCutting
   ├── Arm (SofaMesh + CuttingController)
   └── Scalpel
   ```

3. **VesselWithBlood.prefab**
   ```
   VesselWithBlood
   ├── Vessel (SofaMesh)
   └── BloodParticles (Particle System)
   ```

**Prefab Checklist:**
- [ ] Does NOT contain SofaContext
- [ ] Does NOT contain Camera or VR Rig
- [ ] All scripts have public fields assigned
- [ ] All materials are assigned (not missing/pink)
- [ ] Positioned at origin (0,0,0) or documented offset

### 7.3 Member 2: Teeth Prefab

**DentalSimulation.prefab**
```
DentalSimulation
├── Jaw (static mesh)
├── Tooth (with ToothExtraction.cs)
├── ToothSocket (empty transform + trigger)
├── BloodEffect (particle system)
├── ReplacementTooth
└── AudioSource
```

**Prefab Checklist:**
- [ ] ToothExtraction script has all references assigned
- [ ] Socket transform is assigned
- [ ] Audio clips are assigned
- [ ] Particle system is set to "Play On Awake: false"
- [ ] Positioned at origin or documented

### 7.4 Member 3: Heart Prefab

**BeatingHeart.prefab**
```
BeatingHeart
├── Heart (SkinnedMeshRenderer with blend shapes)
│   └── Heartbeat.cs
└── AudioSource (optional)
```

**Prefab Checklist:**
- [ ] Blend shapes imported correctly (or shader applied)
- [ ] Heartbeat script configured (BPM, contraction strength)
- [ ] Material applied
- [ ] Audio clip assigned (if using)
- [ ] Positioned at origin

---

## 8. Integration Day (Merging Everything)

### 8.1 Pre-Integration Checklist

**Each member verifies:**
- [ ] My work scene runs without errors
- [ ] All prefabs are created and saved
- [ ] All assets are committed to Git
- [ ] My branch is pushed to remote
- [ ] I've tested my prefabs in isolation

### 8.2 Integration Steps

**Step 1: Merge Code (Git)**

One person (recommend project lead):

```bash
git checkout main
git pull origin main

# Merge Member 1
git merge feature/flesh-simulation
# Resolve conflicts if any
git commit

# Merge Member 2
git merge feature/teeth-extraction
git commit

# Merge Member 3
git merge feature/heart-beating
git commit

git push origin main
```

**Step 2: Open MasterScene**

1. Open `MasterScene.unity`
2. Add `SofaContext` (for Member 1's simulations)
3. Add shared `VR_Rig` prefab

**Step 3: Add Prefabs to Scene**

Drag prefabs into MasterScene:

```
Hierarchy:
├── SofaContext
├── VR_Rig
├── Main Camera (from VR_Rig)
├── Directional Light
├── Environment
│   └── Floor
├── [Simulation Area]
│   ├── LiverSimulation (0, 1, 2)
│   ├── ArmCutting (2, 1, 2)
│   ├── VesselWithBlood (4, 1, 2)
│   ├── DentalSimulation (-2, 1, 0)
│   └── BeatingHeart (0, 1, -2)
```

**Position them spatially** so user can walk between them in VR.

**Step 4: Test Each System**

1. Press Play
2. Test VR controllers work
3. Test Member 1's systems:
   - Squeeze liver
   - Cut arm
   - Pinch vessel
4. Test Member 2's system:
   - Grab tooth
   - Extract tooth
   - Replace tooth
5. Test Member 3's system:
   - Heart beats
   - Touch heart (if interaction added)

**Step 5: Fix Issues**

Common integration issues:

| Problem | Cause | Solution |
|---------|-------|----------|
| Member 1's simulations don't work | SofaContext missing | Add to scene root |
| Prefab references broken | Missing assets | Check all members committed files |
| VR controllers don't grab | Interaction layers wrong | Verify XR Interaction Layer Mask |
| Audio doesn't play | AudioListener conflict | Only one AudioListener (on VR camera) |
| Performance issues | Too many simulations | Optimize or reduce quality |

---

## 9. Scene Layout Recommendations

### 9.1 Spatial Organization

Arrange simulations in a "medical training room":

```
        [Dental]
           |
    [Liver]---[Heart]
           |
    [Vessel]---[Arm]
```

**Distances:** 2-3 meters apart (comfortable VR walking distance)

### 9.2 UI/Instructions (Optional)

Add floating text panels explaining each simulation:
- "Squeeze the liver to test deformation"
- "Pull the tooth to extract"
- "Observe the beating heart"

---

## 10. Testing & Validation

### 10.1 Integration Test Checklist

- [ ] All 5 simulations present in scene
- [ ] VR controllers can interact with all objects
- [ ] No console errors
- [ ] Frame rate >30 FPS (preferably 60+)
- [ ] Audio plays correctly
- [ ] Haptic feedback works
- [ ] Can complete all required tasks:
  - [ ] Squeeze liver/nasal cavity
  - [ ] Cut arm/abdomen
  - [ ] Block blood flow
  - [ ] Extract tooth
  - [ ] Replace tooth
  - [ ] See heart beating

### 10.2 Performance Optimization

If FPS is low:

**Member 1 (most expensive):**
- Reduce tetrahedral mesh density
- Lower SOFA solver iterations
- Reduce particle count in blood flow

**Member 2:**
- Simplify jaw mesh
- Reduce particle count in blood effect

**Member 3:**
- Use shader method instead of blend shapes (more efficient)
- Reduce heart mesh poly count

---

## 11. Conflict Resolution

### 11.1 Scene Conflicts (Rare)

If two people accidentally edit the same scene:

```bash
# Keep your version
git checkout --ours path/to/scene.unity
git add path/to/scene.unity

# OR keep their version
git checkout --theirs path/to/scene.unity
git add path/to/scene.unity
```

**Better:** Communicate and avoid this!

### 11.2 Script Conflicts

If two people edit the same script:

1. Open the file in a text editor
2. Look for conflict markers:
   ```
   <<<<<<< HEAD
   Your code
   =======
   Their code
   >>>>>>> branch-name
   ```
3. Manually merge the changes
4. Remove conflict markers
5. Test the script

---

## 12. Final Deliverables

### 12.1 What to Submit

- [ ] `MasterScene.unity` with all simulations integrated
- [ ] All prefabs in their respective folders
- [ ] All scripts, models, materials, audio
- [ ] `README.md` with:
  - Setup instructions
  - Controls documentation
  - Known issues
  - Team member contributions

### 12.2 README.md Template

```markdown
# VR Medical Simulation - Proof of Concept

## Team Members
- Member 1: Flesh Simulation, Cutting, Blood Flow
- Member 2: Dental Extraction
- Member 3: Heart Beating

## Setup
1. Unity Version: 2022.3 LTS
2. Install XR Interaction Toolkit
3. Install SofaUnity (see Member1_Flesh/README.md)
4. Open MasterScene.unity
5. Connect VR headset
6. Press Play

## Controls
- Grip: Grab objects
- Trigger: Activate tools
- Thumbstick: Move (if locomotion enabled)

## Features
1. Liver deformation (squeeze with controllers)
2. Arm cutting (use scalpel tool)
3. Blood flow blockage (pinch vessel)
4. Tooth extraction (pull tooth)
5. Tooth replacement (place new tooth)
6. Heart beating animation

## Known Issues
- [List any bugs or limitations]

## Future Improvements
- [List potential enhancements]
```

---

## 13. Timeline for Integration

**Days 1-10:** Individual work (separate scenes)
**Day 11:** Create prefabs, commit all assets
**Day 12:** Integration day
  - Morning: Merge Git branches
  - Afternoon: Assemble MasterScene
  - Evening: Test and fix issues
**Day 13:** Polish, optimize, documentation
**Day 14:** Final testing, prepare presentation

---

## 14. Communication Best Practices

### 14.1 Daily Standup (5 minutes)

Each member shares:
1. What I did yesterday
2. What I'm doing today
3. Any blockers

### 14.2 Shared Document

Use Google Docs/Notion for:
- Asset naming conventions
- Shared material properties (so colors match)
- Known issues
- Integration notes

### 14.3 When to Ask for Help

**Immediately notify team if:**
- You need to modify shared assets (VR_Rig, Common materials)
- You're blocked and can't proceed
- You discover a major issue with SofaUnity or Unity version
- You need to change the project structure

---

## 15. Troubleshooting Integration Issues

### Issue: "Missing Prefab References"

**Cause:** Assets not committed or wrong path

**Solution:**
```bash
git status  # Check if files are committed
git pull    # Get latest from team
```

### Issue: "SofaContext not found"

**Cause:** Member 1's prefabs looking for context

**Solution:** Ensure SofaContext is in MasterScene root, not inside prefabs

### Issue: "Multiple AudioListeners"

**Cause:** Each prefab has an AudioListener

**Solution:** Only VR Camera should have AudioListener. Remove from prefabs.

### Issue: "Interaction Layers Mismatch"

**Cause:** Different XR Interaction Layer settings

**Solution:** 
1. Open Project Settings > XR Interaction Toolkit
2. Ensure all members have same layer setup
3. Commit `ProjectSettings/TagManager.asset`

---

## Summary

**Key Principles:**
1. 🔑 Work in separate scenes
2. 🔑 Use prefabs for integration
3. 🔑 Commit often, communicate always
4. 🔑 Test individually before merging
5. 🔑 One SofaContext in final scene

**Success Criteria:**
- ✅ All 5 features working in MasterScene
- ✅ No Git conflicts
- ✅ Smooth VR experience
- ✅ Runs at acceptable FPS
- ✅ Team learned collaboration skills

Good luck! 🚀
