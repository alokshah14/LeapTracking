# LeapTracking - Finger Individuation Game

## Project Overview

A Unity-based rehabilitation game using Leap Motion hand tracking to help users improve finger independence and dexterity through piano-based exercises.

## Architecture

### Core Components

#### 1. FingerIndividuationGame.cs
**Purpose**: Core hand tracking and finger press detection system

**Key Features**:
- **Per-Finger Calibration System**: Personalized calibration for each of the 10 fingers
  - Phase 1: Baseline calibration (hands flat, all fingers extended)
  - Phase 2: Individual finger calibration (press each finger one at a time)
  - Stores both baseline and pressed angles for accurate detection

- **Detection Algorithm**:
  ```
  For each finger:
    distanceToBaseline = |currentAngle - baselineAngle|
    distanceToPressed = |currentAngle - pressedAngle|
    isPressed = distanceToPressed < (distanceToBaseline * 0.6)
  ```

- **Thumb Handling**: Uses 4 bone angles (metacarpal + proximal + intermediate + distal) instead of 2, accounts for thumb's unique opposition movement

**State Machine**:
- `WaitingForCalibration` → `PreCalibrationCountdown` → `CalibratingBaseline` → `CalibratingFingers` → `Playing` ↔ `Paused`

**Key Methods**:
- `GetFingerAngles()`: Calculates bone angles for all 5 fingers
- `CheckFingerPress()`: Determines which finger is pressed based on calibrated data
- `ProcessFingerCalibration()`: Guides user through per-finger calibration

#### 2. PianoGameManager.cs
**Purpose**: Game flow manager for piano practice mode

**Features**:
- Random finger selection (10 fingers total)
- Score tracking (+10 correct, -5 incorrect)
- Round-based gameplay with 4-second time limits
- Visual feedback through key highlighting
- Audio feedback using procedural piano sounds

**Key Data Structures**:
```csharp
Dictionary<Chirality, List<PianoKey>> keyboards
// Maps Left/Right hands to 5 PianoKey objects (Thumb, Index, Middle, Ring, Pinky)
```

#### 3. RhythmGameManager.cs
**Purpose**: Practice mode with pre-defined note sequences

**Features**:
- Song-based progression system
- Wait-for-correct-press mode (no time pressure)
- Score tracking with combo multipliers
- Four default songs: ScaleUp, FingerExercise, TwinkleTwinkle, SpeedChallenge

#### 4. PianoKey.cs
**Purpose**: Individual key visualization and state management

**States**:
- `Normal`: Default gray color
- `Highlighted`: Yellow/gold - target key to press
- `Pressed`: Green - key successfully pressed

#### 5. NoteAudioGenerator.cs
**Purpose**: Procedural piano sound generation

**Implementation**:
- Generates piano-like tones using sine wave synthesis
- Frequencies: C4 (261.63Hz), D4 (293.66Hz), E4 (329.63Hz), F4 (349.23Hz), G4 (392.00Hz)
- Adds harmonics for more realistic piano timbre
- Applies exponential decay envelope

### Game Modes

#### Piano Game (PianoGameManager)
- **Objective**: Press the highlighted finger/key within time limit
- **Difficulty**: Random finger selection, 4-second response time
- **Scoring**: Points-based with penalties for wrong presses

#### Practice Piano (RhythmGameManager)
- **Objective**: Follow note sequences from songs
- **Difficulty**: No time limit, must press correct finger to advance
- **Scoring**: Combo-based scoring with accuracy tracking

## Calibration System

### Why Per-Finger Calibration?

The original fixed-threshold approach failed because:
1. Hand biomechanics vary significantly between individuals
2. Thumb moves differently (opposition vs flexion)
3. Ring finger naturally couples with middle finger
4. Different fingers have different ranges of motion

### Calibration Process

**Step 1: Baseline (2 seconds)**
```
User: Hold hands flat, fingers extended
System: Records resting angle for all 10 fingers
```

**Step 2: Per-Finger Calibration (2 seconds × 10 fingers)**
```
For each finger (L-Thumb → L-Pinky → R-Thumb → R-Pinky):
  1. Display: "Press LEFT THUMB"
  2. Detect: Wait for angle change > 15° from baseline
  3. Record: Average angle over 2 seconds
  4. Store: pressedAngles[hand][fingerIndex] = recordedAngle
```

**Step 3: Gameplay Detection**
```
For current hand pose:
  For each finger:
    Calculate distance to baseline
    Calculate distance to pressed state
    If closer to pressed → finger is pressed
  Select finger with highest confidence
  Compare to target finger
```

## Hand Tracking Details

### Leap Motion Integration
- Uses LeapProvider for frame data
- Tracks both hands simultaneously
- Monitors hand drift from baseline position
- Auto-pauses if hands lost or drift too far (>0.15m)

### Finger Indexing
```
0 = Thumb
1 = Index
2 = Middle
3 = Ring
4 = Pinky
```

### Bone Structure
Each finger has 4 bones:
- Bone[0]: Metacarpal (palm)
- Bone[1]: Proximal phalanx
- Bone[2]: Intermediate/middle phalanx
- Bone[3]: Distal phalanx (tip)

## Data Logging

### HandDataLogger.cs
Logs all game events to CSV:
- Timestamp
- Event type (CalibrationComplete, NewTarget, GesturePress, etc.)
- Event details
- Location: `~/Library/Application Support/DefaultCompany/LeapTracking/HandData/`

## UI System

### GameUIManager.cs
- Singleton pattern for global UI access
- Displays: Score, target finger, feedback messages, calibration status
- Progress bars for calibration

### GameMenuManager.cs
- Game mode selection
- Scene management
- Settings (if implemented)

## Audio System

### NoteAudioGenerator
Generates procedural piano notes:
```csharp
sample = sin(2π × frequency × t) × 0.5
      + sin(4π × frequency × t) × 0.25  // 2nd harmonic
      + sin(6π × frequency × t) × 0.125 // 3rd harmonic
envelope = e^(-t × 5)  // Decay
output = sample × envelope × 0.5
```

## Known Issues & Solutions

### Issue: Thumb Not Detecting
**Cause**: Thumb uses different joint structure, negative angle deltas
**Solution**: Use 4-bone angle calculation + absolute value comparison

### Issue: Ring Finger False Negatives
**Cause**: Natural coupling with middle finger, threshold too strict
**Solution**: Per-finger calibration captures individual press patterns

### Issue: Multiple Keys Highlighting
**Cause**: Same key object assigned to both left and right hand arrays
**Solution**: Validation check in SetupKeyboards() to detect duplicates

## Development Guidelines

### Adding New Game Modes
1. Create new manager class inheriting from MonoBehaviour
2. Subscribe to `FingerIndividuationGame.OnGestureSuccess` event
3. Call `fingerGame.ResetExercise(hand, fingerIndex)` to set target
4. Implement scoring/progression logic
5. Handle calibration workflow

### Debugging Finger Detection
Enable detailed logs in `CheckFingerPress()`:
- Every 30 frames: Shows pressed state array [00101] and distances
- On detection: Shows which finger detected and confidence
- On result: Shows CORRECT/WRONG with angle data

### Modifying Calibration
Key parameters in Inspector:
- `minDetectionThreshold`: 15° - minimum movement to detect press during calibration
- `calibrationTime`: 2s - how long to hold each finger
- `pressedThresholdRatio`: 0.6 - sensitivity (lower = more sensitive)

## File Structure
```
Assets/
├── Scripts/
│   ├── FingerIndividuationGame.cs    # Core detection
│   ├── PianoGameManager.cs           # Random finger game
│   ├── RhythmGameManager.cs          # Song practice mode
│   ├── PianoKey.cs                   # Key visualization
│   ├── NoteAudioGenerator.cs         # Sound generation
│   ├── HandDataLogger.cs             # CSV logging
│   ├── GameUIManager.cs              # UI system
│   ├── GameMenuManager.cs            # Menu system
│   ├── MainController.cs             # Scene setup
│   └── Song.cs / SongCreator.cs      # Song data
└── Scenes/
    ├── MainMenu
    ├── PianoGame
    └── PracticePiano
```

## Testing Checklist

### Calibration
- [ ] Baseline captures both hands
- [ ] All 10 fingers calibrate successfully
- [ ] UI shows correct finger name during calibration
- [ ] Progress bar updates correctly

### Detection
- [ ] Thumb presses detect correctly
- [ ] Ring finger presses detect correctly
- [ ] No false positives from other fingers
- [ ] Wrong finger presses show correct error message

### Audio
- [ ] Sound plays on correct press
- [ ] Different notes for different fingers
- [ ] No sound on wrong press

### UI
- [ ] Correct finger highlights
- [ ] Only one key highlights at a time
- [ ] Score updates correctly
- [ ] Error messages show correct target

## Performance Considerations

- Angle calculations run every frame (~90 FPS)
- Calibration samples averaged with 0.2 lerp factor
- Debug logs limited to every 30 frames during gameplay
- Hand tracking pauses game if hands lost > 1 second

## Future Enhancements

1. **Adaptive Difficulty**: Adjust time limits based on success rate
2. **Multi-finger Chords**: Require multiple simultaneous presses
3. **Speed Challenges**: Progressively faster sequences
4. **Statistics Dashboard**: Track improvement over time
5. **Custom Songs**: Song editor for creating new practice sequences
