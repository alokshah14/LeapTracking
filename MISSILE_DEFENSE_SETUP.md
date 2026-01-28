# Missile Defense - Unity Setup Guide

Complete step-by-step instructions to create the Missile Defense scene and prefab.

---

## ⚡ QUICK START (2 Minutes!)

### Automatic Setup (Recommended)

1. **Create new scene**:
   - File → New Scene → 3D
   - Save as `Assets/Scenes/MissileDefense.unity`

2. **Add Scene Builder**:
   - GameObject → Create Empty
   - Name: `SceneBuilder`
   - Add Component: `MissileDefenseSceneBuilder`
   - **Click "Build Complete Scene" button** in Inspector
   - ✨ Done! All components auto-added

3. **Add Targets & Manager**:
   - Add Component: `MissileDefenseSetup` to same GameObject
   - **Click "Setup Scene" button**
   - GameObject → Create Empty → Name: `MissileDefenseManager`
   - Add Component: `MissileDefenseManager`

4. **Create Missile Prefab** (see Part 3 below)

5. **Assign References** (see Part 4 below)

6. **Press Play!** 🎮

---

## 📖 Manual Setup (If Auto-Setup Doesn't Work)

## Part 1: Create the Scene

### Step 1: Create New Scene
1. In Unity, go to **File → New Scene**
2. Choose **3D (Built-in Render Pipeline)**
3. Save as `Assets/Scenes/MissileDefense.unity`

### Step 2: Set Up Basic Scene Objects
The scene should already have:
- Main Camera
- Directional Light

If not, create them:
- **GameObject → Camera** (name it "Main Camera")
- **GameObject → Light → Directional Light**

### Step 3: Add Required Components (AUTO or MANUAL)

#### A. Create FingerIndividuationGame GameObject
1. **GameObject → Create Empty**
2. Name it: `FingerIndividuationGame`
3. Add component: **FingerIndividuationGame** script
4. In Inspector, assign:
   - **Leap Provider**: Drag your Leap Provider from the scene
   - Leave other settings at default (calibration will auto-configure)

#### B. Create GameUIManager GameObject
1. **GameObject → UI → Canvas** (if not exists)
2. **GameObject → Create Empty** (under Canvas)
3. Name it: `GameUIManager`
4. Add component: **GameUIManager** script

#### C. Create HandDataLogger GameObject
1. **GameObject → Create Empty**
2. Name it: `HandDataLogger`
3. Add component: **HandDataLogger** script

---

## Part 2: Auto-Generate Finger Targets

### Step 4: Use MissileDefenseSetup Helper

1. **GameObject → Create Empty**
2. Name it: `SceneSetup`
3. Add component: **MissileDefenseSetup** script
4. In Inspector, you'll see:
   - Finger Spacing: `0.5` (distance between fingers)
   - Hand Separation: `2.0` (distance between left/right hands)
   - Target Height: `-2.0` (Y position where missiles hit)

5. **Click the "Setup Scene" button** in the Inspector
   - This automatically creates:
     - `LeftHandTargets` parent with 5 child targets (Thumb → Pinky)
     - `RightHandTargets` parent with 5 child targets (Thumb → Pinky)
     - `MissileSpawnParent` for organizing spawned missiles
     - Color-coded sphere markers for each finger

6. Check the Hierarchy - you should see:
   ```
   LeftHandTargets
     ├── Left_Thumb_Target
     ├── Left_Index_Target
     ├── Left_Middle_Target
     ├── Left_Ring_Target
     └── Left_Pinky_Target

   RightHandTargets
     ├── Right_Thumb_Target
     ├── Right_Index_Target
     ├── Right_Middle_Target
     ├── Right_Ring_Target
     └── Right_Pinky_Target

   MissileSpawnParent
   ```

---

## Part 3: Create Missile Prefab

### Step 5: Create Missile GameObject

1. **GameObject → 3D Object → Capsule**
2. Name it: `Missile`
3. Transform settings:
   - Position: (0, 0, 0)
   - Rotation: (90, 0, 0) - point it downward
   - Scale: (0.3, 0.5, 0.3) - make it missile-shaped

### Step 6: Configure Missile

1. Select the `Missile` GameObject
2. **Add Component → Missile** script
3. In Inspector:
   - **Speed**: `2.0` (adjust later for difficulty)
   - Leave other fields empty (filled at runtime)

4. **Add a Rigidbody** (optional, if you want physics):
   - **Add Component → Rigidbody**
   - Uncheck "Use Gravity" (we control movement in script)
   - Check "Is Kinematic"

### Step 7: Create Missile Materials

#### Normal Material (Blue)
1. **Assets → Create → Material**
2. Name: `Missile_Normal`
3. Set color: Blue `(0, 0.5, 1, 1)`
4. Drag to Missile object to apply

#### Warning Material (Red)
1. **Assets → Create → Material**
2. Name: `Missile_Warning`
3. Set color: Red `(1, 0, 0, 1)`
4. Emission: Check "Emission", set to red

#### Assign to Missile Script
1. Select `Missile` GameObject
2. In Missile component:
   - **Normal Material**: Drag `Missile_Normal`
   - **Warning Material**: Drag `Missile_Warning`

### Step 8: Create Prefab

1. Drag the `Missile` GameObject from Hierarchy to `Assets/Prefabs/` folder
2. You now have `Missile.prefab`
3. Delete the original `Missile` from the Hierarchy (we only need the prefab)

---

## Part 4: Create MissileDefenseManager

### Step 9: Set Up Game Manager

1. **GameObject → Create Empty**
2. Name it: `MissileDefenseManager`
3. **Add Component → MissileDefenseManager** script

### Step 10: Assign References in Inspector

With `MissileDefenseManager` selected, fill in these fields:

#### Game Settings
- **Starting Lives**: `5`
- **Initial Missile Speed**: `1.5`
- **Speed Increase Per Wave**: `0.2`
- **Time Between Missiles**: `2.0`
- **Points Per Destroy**: `10`
- **Points Lost Per Miss**: `-20`

#### Missile Spawning
- **Missile Prefab**: Drag `Missile` prefab from Assets/Prefabs
- **Missile Spawn Parent**: Drag `MissileSpawnParent` from Hierarchy
- **Spawn Height**: `5.0`
- **Target Height**: `-2.0`

#### Finger Target Positions
**Left Hand Targets** (size 5):
- Element 0: Drag `Left_Thumb_Target`
- Element 1: Drag `Left_Index_Target`
- Element 2: Drag `Left_Middle_Target`
- Element 3: Drag `Left_Ring_Target`
- Element 4: Drag `Left_Pinky_Target`

**Right Hand Targets** (size 5):
- Element 0: Drag `Right_Thumb_Target`
- Element 1: Drag `Right_Index_Target`
- Element 2: Drag `Right_Middle_Target`
- Element 3: Drag `Right_Ring_Target`
- Element 4: Drag `Right_Pinky_Target`

#### Materials
- **Missile Material**: Drag `Missile_Normal`
- **Warning Material**: Drag `Missile_Warning`

#### Audio (Optional - leave empty for now)
- **Missile Spawn Sound**: (create later)
- **Destroy Sound**: (create later)
- **Miss Sound**: (create later)

---

## Part 5: Final Setup

### Step 11: Add Leap Motion Provider

1. If you don't have a Leap Provider in the scene:
   - **GameObject → Create Empty**
   - Name: `LeapProvider`
   - Add: **Leap Provider** component (or **Leap Service Provider**)

2. Assign to FingerIndividuationGame:
   - Select `FingerIndividuationGame` GameObject
   - Drag `LeapProvider` to the **Leap Provider** field

### Step 12: Configure Camera

Position the camera to see the finger targets and missile spawn area:
- **Position**: (0, 1, -5)
- **Rotation**: (0, 0, 0) or slight downward angle
- Adjust so you can see Y range from -2 (targets) to +5 (spawn)

### Step 13: Test Without Leap Motion (Optional)

For testing without hands, you can temporarily modify `MissileDefenseManager.cs`:
- Comment out calibration check
- Add keyboard controls for finger presses

But for now, save the scene and test with Leap Motion!

---

## Part 6: Add to Menu System

### Step 14: Update GameMenuManager

1. Open `Assets/Scripts/GameMenuManager.cs`
2. Find the `GameType` enum
3. Add: `MissileDefense`

Example:
```csharp
public enum GameType
{
    PianoGame,
    PracticePiano,
    MissileDefense  // ADD THIS
}
```

4. In `SelectGame()` method, add case:
```csharp
case GameType.MissileDefense:
    SceneManager.LoadScene("MissileDefense");
    break;
```

5. In `CreateMenuUI()`, add button:
```csharp
CreateMenuButton("Missile Defense", GameType.MissileDefense);
```

---

## Testing Checklist

### Before Running:
- [ ] Scene saved as `MissileDefense.unity`
- [ ] FingerIndividuationGame has Leap Provider assigned
- [ ] MissileDefenseManager has all 10 finger targets assigned
- [ ] Missile prefab has Missile script and materials
- [ ] Camera positioned to see play area

### During Play:
- [ ] Calibration starts automatically
- [ ] After calibration, missiles spawn
- [ ] UI shows which finger to press (e.g., "LEFT THUMB")
- [ ] Missiles move downward toward targets
- [ ] Pressing correct finger destroys missile
- [ ] Missiles turn red when close to target
- [ ] Missing a missile reduces lives
- [ ] Score updates correctly
- [ ] Game over at 0 lives
- [ ] Auto-restart after game over

---

## Troubleshooting

### Missiles don't spawn
- Check MissileDefenseManager has Missile Prefab assigned
- Check console for errors about missing finger targets

### Finger presses don't destroy missiles
- Verify FingerIndividuationGame is calibrated
- Check console logs for finger press detection
- Make sure you're pressing the correct hand/finger shown in UI

### Missiles spawn at wrong location
- Check finger target positions in Scene view
- Verify Spawn Height and Target Height in MissileDefenseManager
- Check that target transforms are assigned correctly (Left0=Thumb, etc.)

### UI doesn't show
- Make sure GameUIManager exists in scene
- Check Canvas and EventSystem are present
- Verify GameUIManager.Instance is not null in console

---

## Next Steps

Once basic gameplay works:

1. **Polish Visuals**:
   - Better missile model (rocket shape)
   - Particle effects on destroy
   - Explosion animations
   - Background starfield

2. **Add Audio**:
   - Spawn sound (missile launch)
   - Destroy sound (explosion)
   - Miss sound (impact)
   - Background music

3. **Enhance Gameplay**:
   - Power-ups
   - Multiple simultaneous missiles
   - Special missile types
   - Boss waves

4. **Balance Difficulty**:
   - Adjust spawn rate
   - Tune speed progression
   - Test with different skill levels

---

## Quick Reference

**Key GameObjects**:
- `FingerIndividuationGame` - Hand tracking & calibration
- `MissileDefenseManager` - Game logic
- `LeftHandTargets` / `RightHandTargets` - Missile target positions
- `MissileSpawnParent` - Container for active missiles

**Key Scripts**:
- `MissileDefenseSetup.cs` - Auto-generates scene (use once, then delete)
- `MissileDefenseManager.cs` - Main game controller
- `Missile.cs` - Individual missile behavior

**Key Prefabs**:
- `Missile.prefab` - Spawned for each missile

**Materials**:
- `Missile_Normal` - Blue (normal state)
- `Missile_Warning` - Red (close to target)
