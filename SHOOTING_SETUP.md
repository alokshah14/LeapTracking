# Missile Defense - Shooting Mechanics Setup

## New Features Added:
- ✅ **Leap Motion finger highlighting** (your actual fingers glow!)
- ✅ **Space Invaders shooting** (shoot projectiles UP at falling missiles)
- ✅ **Much slower missiles** (0.3 speed - very easy to hit)
- ✅ **Real finger positions** (projectiles shoot from your actual fingertips)

---

## Required Setup in Unity:

### Step 1: Add Leap Motion Hands to Scene

1. **Find Capsule Hands Prefab**:
   - In Project window, search for: `Capsule Hands`
   - Located in: `Packages/Ultraleap Tracking/Core/Runtime/Prefabs/Hands/`

2. **Add to Scene**:
   - Drag `Capsule Hands.prefab` into your Hierarchy
   - This creates visible 3D hand models that follow your Leap Motion

3. **Configure Leap Service Provider**:
   - Select your `LeapServiceProvider` in Hierarchy
   - In Inspector, find "Hand Models" section
   - Drag the `Capsule Hands` GameObject into the `Left Hand Model` and `Right Hand Model` slots

### Step 2: Create Player Projectile Prefab

1. **Create Projectile GameObject**:
   ```
   GameObject → 3D Object → Capsule
   Name: PlayerProjectile
   Transform:
     - Rotation: (90, 0, 0) [points upward]
     - Scale: (0.2, 0.3, 0.2) [small bullet shape]
   ```

2. **Add Component**:
   - Select `PlayerProjectile`
   - Add Component → `PlayerProjectile` script

3. **Create Material**:
   ```
   Assets → Create → Material
   Name: Projectile_Material
   Color: Bright Green (0, 1, 0, 1)
   Emission: Check "Emission", set to bright green
   ```

4. **Apply Material**:
   - Drag `Projectile_Material` onto the `PlayerProjectile` capsule

5. **Create Prefab**:
   - Drag `PlayerProjectile` from Hierarchy to `Assets/Prefabs/` folder
   - Delete the original from Hierarchy (we only need the prefab)

### Step 3: Update MissileDefenseManager References

Select `MissileDefenseManager` in Hierarchy, and in Inspector:

#### New Fields to Assign:

**Shooting Section**:
- **Projectile Prefab**: Drag `PlayerProjectile.prefab` from Assets/Prefabs
- **Projectile Spawn Parent**: Create empty GameObject named `ProjectileSpawnParent`, drag it here

**Audio Section** (optional):
- **Shoot Sound**: (Add a shooting sound clip if you have one)

#### Verify Existing Fields:

**Game Settings**:
- Initial Missile Speed: Should be `0.3` (auto-updated)
- Speed Increase Per Wave: Should be `0.05`
- Time Between Missiles: Should be `4.0`
- Player Projectile Speed: Should be `5.0`

**Missile Spawning**:
- Missile Prefab: Should already be assigned
- Missile Spawn Parent: Should already be assigned
- Left/Right Finger Targets: Should already be assigned (10 targets total)

---

## How to Play:

### 1. Start Game:
- Press Play in Unity
- If you've calibrated before, it loads automatically
- Otherwise, complete calibration (one time)

### 2. Gameplay:
- **Missile Spawns**: A missile falls from above targeting a specific finger
- **Your Finger GLOWS**: The Leap Motion finger capsule turns BRIGHT YELLOW
- **Target Sphere Pulses**: The target sphere at bottom also pulses (backup visual)
- **Press Glowing Finger**: Press down the highlighted finger on your hand
- **Projectile Shoots UP**: A green bullet fires from your fingertip
- **Hit the Missile**: Projectile flies up and destroys the missile
- **Score Points**: +10 points for each hit!

### 3. Visual Feedback:
- **Before Press**: Yellow glowing finger + pulsing sphere
- **After Press**: Green projectile shoots upward with trail
- **On Hit**: Missile explodes (can add particle effects later)
- **On Miss**: Missile reaches bottom, lose 1 life

---

## Testing Checklist:

- [ ] Capsule Hands visible in scene and tracking your real hands
- [ ] LeapServiceProvider has hand models assigned
- [ ] PlayerProjectile prefab created with green emissive material
- [ ] MissileDefenseManager has ProjectilePrefab assigned
- [ ] Press Play - calibration loads automatically (if done before)
- [ ] Missile spawns slowly (should have ~10 seconds to react)
- [ ] Your actual finger GLOWS YELLOW when missile targets it
- [ ] Pressing finger shoots green projectile upward
- [ ] Projectile hits and destroys missile
- [ ] Score increases (+10)
- [ ] Game feels like Space Invaders!

---

## Troubleshooting:

### Hands Not Visible:
- Make sure `Capsule Hands` prefab is in scene
- Check LeapServiceProvider has hand models assigned
- Verify Leap Motion device is connected and tracking

### Fingers Not Highlighting:
- LeapFingerHighlighter is auto-created, but check Console for errors
- Make sure Capsule Hands has finger child transforms
- Check that fingers are named: "thumb", "index", "middle", "ring", "pinky"

### Projectiles Not Shooting:
- Verify ProjectilePrefab is assigned in MissileDefenseManager
- Check that PlayerProjectile script is attached to prefab
- Look in Console for "Shot projectile from..." message

### Missiles Still Too Fast:
- Select MissileDefenseManager
- Change `initialMissileSpeed` to even lower (try 0.2 or 0.15)
- Change `timeBetweenMissiles` to 5.0 or 6.0 seconds

### Projectiles Missing Missiles:
- Increase `playerProjectileSpeed` (try 7.0 or 10.0)
- Check hit detection radius in PlayerProjectile.cs (currently 0.5f)
- Missiles might be too fast - slow them down more

---

## Next Enhancements:

1. **Particle Effects**:
   - Add explosion particles when missile destroyed
   - Add muzzle flash when projectile fires
   - Add trail renderer to projectiles

2. **Better Visuals**:
   - Replace capsule with rocket model for missiles
   - Replace capsule with laser beam for projectiles
   - Add starfield background

3. **Game Feel**:
   - Camera shake on explosion
   - Screen flash on hit
   - Combo multiplier for consecutive hits

4. **Audio**:
   - Laser shoot sound
   - Explosion sound with bass
   - Background music

5. **Alignment**:
   - Update finger target positions to match actual Leap Motion finger positions
   - Use calibration baseline positions for target sphere placement
   - Makes aiming more intuitive

---

## Key Files:

- `MissileDefenseManager.cs` - Main game controller
- `PlayerProjectile.cs` - Your fired projectiles
- `Missile.cs` - Enemy missiles falling down
- `LeapFingerHighlighter.cs` - Highlights Leap Motion fingers
- `FingerIndividuationGame.cs` - Finger press detection + calibration save/load

---

Enjoy your Space Invaders finger game! 🎮🚀
