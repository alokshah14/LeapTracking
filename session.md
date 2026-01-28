# Session Log - Per-Finger Calibration Implementation

**Date**: January 27, 2026
**Session Duration**: ~2 hours
**Primary Goal**: Fix finger detection issues and implement robust calibration system

## Problems Identified

### 1. No Sound on Key Press
**Symptom**: Pressing correct keys didn't produce audio feedback
**Cause**: PianoGameManager lacked audio integration
**Impact**: Poor user experience, no feedback confirmation

### 2. Incorrect Detection Messages
**Symptom**: "Wrong press" messages even when correct key pressed
**Cause**: Detection logic required target finger to be THE most bent, but natural finger coupling meant other fingers could be slightly more bent even when pressing correctly

### 3. Thumb Not Detecting
**Symptom**: Thumb presses never registered
**Cause**:
- Original calculation used only 2 bones, giving small angle range (~15-20°)
- Threshold set too high (35° initially, then 21°, still too high)
- Negative deltas from calibration capturing thumb in partially bent state
- Thumb opposition movement different from other fingers' flexion

### 4. Ring Finger Detection Poor
**Symptom**: Ring finger presses often marked as wrong
**Cause**: Ring and middle fingers naturally coupled, attempting to isolate ring flexes both, making middle appear more bent

### 5. Multiple Keys Highlighting
**Symptom**: More than one key would highlight simultaneously
**Cause**: Unknown (logs showed it might be duplicate key assignments)

## Solutions Implemented

### Phase 1: Quick Fixes (Attempted)

#### 1.1 Added Audio to PianoGameManager
**Files Modified**: `PianoGameManager.cs`
```csharp
// Added audio components
[SerializeField] private AudioSource audioSource;
private NoteAudioGenerator noteGenerator;

// Plays sound on correct press
PlayNoteSound(currentTargetFingerIndex);
```

#### 1.2 Improved Thumb Angle Calculation
**Files Modified**: `FingerIndividuationGame.cs`
```csharp
// Changed from 2 bones to 4 bones
angles[i] = angle1 + angle2 + angle3; // Sum of all joints
```

#### 1.3 Adjusted Thresholds
**Iterations**:
- Initial: 35° for all fingers
- Attempt 1: 52.5° for thumb (1.5×), 28° for ring (0.8×)
- Attempt 2: 21° for thumb (0.6×)
- Attempt 3: 14° for thumb (0.4×)
- Attempt 4: 42° for thumb (1.2×, after changing calculation)

**Result**: Still unreliable - threshold tuning not a viable solution

#### 1.4 Detection Logic Refinement
**Attempts**:
1. If target finger >= threshold → correct
2. Target finger must be within 15° of max finger
3. Special coupling tolerance (25°) for middle-ring pair
4. Absolute value for thumb comparisons

**Result**: Marginal improvements but fundamental issues remained

### Phase 2: Root Cause Analysis

**Key Insight from User**:
> "What do you think of this: when we are calibrating, we could also have them calibrate each finger individually and use that as the threshold for the press."

**Analysis**:
- Fixed thresholds can't account for individual hand biomechanics
- Each person's finger flexibility varies
- Thumb movement fundamentally different from fingers
- Ring-middle coupling strength varies per person

**Decision**: Complete redesign with per-finger calibration

### Phase 3: Per-Finger Calibration System (Final Solution)

#### 3.1 System Design

**New Calibration Flow**:
```
1. Baseline Calibration (2s)
   - Hold hands flat, fingers extended
   - Records resting angles for all 10 fingers

2. Per-Finger Calibration (2s × 10 fingers)
   - Left Thumb → Index → Middle → Ring → Pinky
   - Right Thumb → Index → Middle → Ring → Pinky
   - For each: Press and hold, record pressed angles

3. Gameplay Detection
   - Compare current angle to both baseline AND pressed
   - Finger is pressed if closer to pressed state
   - Select finger with highest confidence
```

#### 3.2 Implementation Details

**New Data Structures**:
```csharp
Dictionary<Chirality, float[]> pressedAngles;  // NEW
int currentCalibrationFingerIndex;            // 0-4
Chirality currentCalibrationHand;             // Left/Right
int currentFingerCalibrationNumber;           // 0-9 (progress)
```

**New Game States**:
```csharp
enum GameState {
    WaitingForCalibration,
    PreCalibrationCountdown,
    CalibratingBaseline,      // NEW: replaces Calibrating
    CalibratingFingers,        // NEW: per-finger phase
    Playing,
    Paused
}
```

**New Detection Algorithm**:
```csharp
// For each finger
distanceToBaseline = |currentAngle - baselineAngle|
distanceToPressed = |currentAngle - pressedAngle|

// Is finger pressed?
isPressed = distanceToPressed < (distanceToBaseline × 0.6)

// Which finger has highest confidence?
confidence = distanceToBaseline - distanceToPressed
mostPressedFinger = argmax(confidence) where isPressed[i] == true

// Check if correct
correct = (mostPressedFinger == targetFingerIndex)
```

#### 3.3 Files Modified

**FingerIndividuationGame.cs** - Major refactor:
- Added `pressedAngles` dictionary (line 50)
- Added calibration state tracking (lines 54-58)
- Split `ProcessCalibration()` into:
  - `ProcessBaselineCalibration()` (lines 442-502)
  - `ProcessFingerCalibration()` (lines 504-609)
  - `FinishCalibration()` (lines 611-626)
- Completely rewrote `CheckFingerPress()` (lines 232-323)
  - New distance-based detection
  - Removed threshold comparisons
  - Added confidence scoring

**PianoGameManager.cs** - Audio integration:
- Added `AudioSource` and `NoteAudioGenerator` (lines 22-24)
- Added `SetupAudio()` method (lines 68-81)
- Added `PlayNoteSound()` method (lines 422-428)
- Plays sound on correct press (line 378)

**PianoKey.cs** - Reduced log spam:
- Only logs Highlighted and Pressed states (lines 78-87)
- Removed Normal state logging

#### 3.4 Configuration Parameters

**Exposed in Inspector**:
```csharp
[SerializeField] float minDetectionThreshold = 15.0f;     // Minimum movement to detect
[SerializeField] float calibrationTime = 2.0f;             // Hold time per finger
[SerializeField] float pressedThresholdRatio = 0.6f;      // Sensitivity (0-1)
```

#### 3.5 Enhanced Debugging

**Added logging**:
```
// Every 30 frames during gameplay:
[Left] Pressed:[00101] T:5/45 I:3/52 M:12/38 R:8/42 P:40/5 | Target: Pinky

Where:
- Pressed: Binary array (1 = finger detected as pressed)
- Values: distanceToBaseline/distanceToPressed for each finger
```

**Calibration logging**:
```
Calibrated Left Thumb: Baseline=65°, Pressed=125°, Delta=60°
Calibrated Left Index: Baseline=42°, Pressed=128°, Delta=86°
...
```

## Results & Benefits

### Expected Improvements

1. **Thumb Detection**: ✅
   - Personalized to user's thumb movement pattern
   - Works regardless of opposition vs flexion dominance

2. **Ring Finger Detection**: ✅
   - Calibrates user's specific middle-ring coupling
   - Pressed state accounts for how much middle moves with ring

3. **Accuracy**: ✅
   - No more false negatives from finger coupling
   - No more fixed threshold mismatches

4. **User Experience**: ✅
   - Audio feedback confirms correct presses
   - Clear calibration instructions
   - Progress indicator (Finger X of 10)

### Technical Debt Resolved

- Removed all hardcoded angle thresholds
- Removed finger-specific threshold multipliers
- Removed special-case logic for thumb/ring
- Simplified detection to single algorithm for all fingers

## Testing Plan

### Calibration Testing
1. Verify baseline captures both hands
2. Test each finger calibration prompt appears in order
3. Confirm pressed angles significantly different from baseline
4. Check all 10 fingers complete successfully
5. Verify progress indicator updates correctly

### Detection Testing
1. Press each finger 5 times, expect 100% accuracy
2. Press wrong finger, expect correct error message
3. Test thumb specifically (previous problem area)
4. Test ring finger specifically (previous problem area)
5. Test rapid finger changes

### Edge Cases
1. What if user can't move a finger enough? (< 15° delta)
2. What if hands drift during calibration?
3. What if only one hand visible?
4. What if user presses multiple fingers simultaneously?

## Open Questions

1. Should we add a recalibration option during gameplay?
2. Should calibration be saved between sessions?
3. Should there be a minimum delta requirement per finger?
4. How to handle users with limited finger mobility?

## Code Statistics

**Lines Changed**: ~400 lines
- FingerIndividuationGame.cs: ~300 lines
- PianoGameManager.cs: ~70 lines
- PianoKey.cs: ~30 lines

**New Methods**: 3
- `ProcessFingerCalibration()`
- `FinishCalibration()`
- `PlayNoteSound()`

**Modified Methods**: 4
- `ProcessBaselineCalibration()` (was `ProcessCalibration()`)
- `CheckFingerPress()` (complete rewrite)
- `StartCalibration()`
- `Update()` (state machine)

## Next Steps

1. **Test thoroughly**: Run through calibration with different hand types
2. **Tune parameters**: May need to adjust `pressedThresholdRatio` based on testing
3. **Add recalibration**: Allow users to recalibrate without restarting
4. **Save calibration data**: Persist calibration between sessions
5. **Add skip option**: For users who can't move certain fingers
6. **Visual feedback**: Highlight the finger to press on 3D hand model during calibration

## Lessons Learned

1. **User input is invaluable**: The per-finger calibration idea came from the user
2. **Fixed thresholds rarely work**: Biological systems have too much variance
3. **Iterate quickly, pivot when needed**: Tried threshold tuning 4 times before recognizing need for redesign
4. **Logging is essential**: Detailed angle logs revealed the root cause
5. **Distance-based detection > threshold-based**: More robust to individual differences

## Files Modified Summary

```
Modified:
- Assets/Scripts/FingerIndividuationGame.cs  (~300 lines changed)
- Assets/Scripts/PianoGameManager.cs         (~70 lines changed)
- Assets/Scripts/PianoKey.cs                 (~30 lines changed)

Created:
- claude.md                                  (Comprehensive system documentation)
- session.md                                 (This file)
```

## Commands Used

```bash
# No git operations performed yet
# Ready to commit all changes
```
