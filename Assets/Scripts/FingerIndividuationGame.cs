using UnityEngine;
using Leap;
using System.Collections.Generic;

public class FingerIndividuationGame : MonoBehaviour
{
    [Header("Leap Motion")]
    [SerializeField] private LeapProvider leapProvider;

    [Header("Calibration Settings")]
    [SerializeField] private float minDetectionThreshold = 15.0f; // Minimum angle change to detect a press during calibration
    [SerializeField] private float calibrationTime = 2.0f; // Seconds to hold for each finger calibration
    [SerializeField] private float preCalibrationCountdown = 10.0f; // Countdown before calibration starts
    [SerializeField] private float pressedThresholdRatio = 0.6f; // If distance to pressed < baseline * this ratio, finger is pressed

    [Header("Hand Tracking Settings")]
    [SerializeField] private float maxPositionDrift = 0.15f; // Max distance (meters) hands can drift from baseline
    [SerializeField] private float handLostTimeout = 1.0f; // Seconds before showing "hand lost" warning

    // These will be set by a GameManager
    public Chirality TargetHand { get; set; } = Chirality.Left;
    public int TargetFingerIndex { get; set; } = 1; // 0=thumb, 1=index, 2=middle, 3=ring, 4=pinky

    // Events
    public static event System.Action<Chirality, int> OnGestureSuccess;
    public static event System.Action<string> OnCalibrationStatus;
    public static event System.Action<int> OnCountdownTick; // For countdown display
    public static event System.Action<float> OnCalibrationProgress; // 0-1 progress
    public static event System.Action OnHandsLost;
    public static event System.Action OnHandsDrifted;
    public static event System.Action OnHandsRestored;
    public static event System.Action OnGamePaused;
    public static event System.Action OnGameResumed;

    private int score = 0;
    private bool exerciseComplete = false;

    private enum ExerciseState { Pending, Correct, Incorrect }
    private ExerciseState currentExerciseState = ExerciseState.Pending;

    public enum GameState { WaitingForCalibration, PreCalibrationCountdown, CalibratingBaseline, CalibratingFingers, Playing, Paused }
    private GameState gameState = GameState.WaitingForCalibration;

    // Calibration data
    private bool isCalibrated = false;
    private bool isCalibrating = false;
    private float calibrationStartTime;
    private float countdownStartTime;
    private Dictionary<Chirality, float[]> baselineAngles = new Dictionary<Chirality, float[]>();
    private Dictionary<Chirality, float[]> pressedAngles = new Dictionary<Chirality, float[]>(); // NEW: angles when finger is pressed
    private Dictionary<Chirality, Vector3> baselinePositions = new Dictionary<Chirality, Vector3>();
    private List<float[]> calibrationSamples = new List<float[]>();

    // Per-finger calibration state
    private int currentCalibrationFingerIndex = 0; // 0-4
    private Chirality currentCalibrationHand = Chirality.Left;
    private int totalFingersToCalibrate = 10; // 5 per hand
    private int currentFingerCalibrationNumber = 0; // 0-9 (for progress)

    // Hand tracking state
    private float lastHandSeenTime;
    private bool handsVisible = false;
    private bool handsDrifted = false;
    private bool isPaused = false;

    // Public properties
    public bool IsExerciseComplete => exerciseComplete;
    public int CurrentScore => score;
    public bool IsCalibrated => isCalibrated;
    public bool IsPaused => isPaused;
    public bool IsCalibrating => isCalibrating;
    public GameState CurrentGameState => gameState;
    public Vector3 GetBaselinePosition(Chirality hand) => baselinePositions.ContainsKey(hand) ? baselinePositions[hand] : Vector3.zero;

    private string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
    private int lastCountdownValue = -1;

    void Start()
    {
        if (leapProvider == null)
        {
            leapProvider = FindObjectOfType<LeapProvider>();
        }

        if (leapProvider != null)
        {
            Debug.Log("FingerIndividuationGame Started - LeapProvider found!");
        }
        else
        {
            Debug.LogError("FingerIndividuationGame: No LeapProvider found! Gesture detection will not work.");
        }

        // Initialize baseline dictionaries
        baselineAngles[Chirality.Left] = new float[5];
        baselineAngles[Chirality.Right] = new float[5];
        pressedAngles[Chirality.Left] = new float[5];
        pressedAngles[Chirality.Right] = new float[5];
        baselinePositions[Chirality.Left] = Vector3.zero;
        baselinePositions[Chirality.Right] = Vector3.zero;

        lastHandSeenTime = Time.time;
    }

    void Update()
    {
        if (leapProvider == null) return;

        Frame frame = leapProvider.CurrentFrame;

        // Track hand visibility
        UpdateHandVisibility(frame);

        // State machine
        switch (gameState)
        {
            case GameState.WaitingForCalibration:
                // Just waiting - UI should show instructions
                break;

            case GameState.PreCalibrationCountdown:
                ProcessPreCalibrationCountdown(frame);
                break;

            case GameState.CalibratingBaseline:
                ProcessBaselineCalibration(frame);
                break;

            case GameState.CalibratingFingers:
                ProcessFingerCalibration(frame);
                break;

            case GameState.Playing:
                ProcessGameplay(frame);
                break;

            case GameState.Paused:
                CheckForResume(frame);
                break;
        }
    }

    private void UpdateHandVisibility(Frame frame)
    {
        bool hadHands = handsVisible;
        handsVisible = frame.Hands.Count > 0;

        if (handsVisible)
        {
            lastHandSeenTime = Time.time;
        }
    }

    private void ProcessPreCalibrationCountdown(Frame frame)
    {
        float elapsed = Time.time - countdownStartTime;
        float remaining = preCalibrationCountdown - elapsed;

        int currentCountdown = Mathf.CeilToInt(remaining);

        // Fire event when countdown changes
        if (currentCountdown != lastCountdownValue && currentCountdown > 0)
        {
            lastCountdownValue = currentCountdown;
            OnCountdownTick?.Invoke(currentCountdown);

            // Also update status text
            if (currentCountdown > 5)
            {
                OnCalibrationStatus?.Invoke("Position your hands flat above the sensor\nwith fingers extended");
            }
            else if (currentCountdown > 2)
            {
                OnCalibrationStatus?.Invoke("Hold steady...");
            }
            else
            {
                OnCalibrationStatus?.Invoke("Recording baseline...");
            }
        }

        // Check if hands are visible during countdown
        if (!handsVisible && currentCountdown <= 5)
        {
            OnCalibrationStatus?.Invoke("No hands detected! Please position your hands.");
        }

        if (remaining <= 0)
        {
            // Start baseline calibration
            gameState = GameState.CalibratingBaseline;
            isCalibrating = true;
            calibrationStartTime = Time.time;
            OnCalibrationStatus?.Invoke("Calibrating baseline... Hold still!");
        }
    }

    private void ProcessGameplay(Frame frame)
    {
        // Check for hand loss
        if (!handsVisible)
        {
            float timeSinceSeen = Time.time - lastHandSeenTime;
            if (timeSinceSeen > handLostTimeout)
            {
                PauseGame("Hands not detected!\n\nPlease return your hands to the sensor.");
                OnHandsLost?.Invoke();
                return;
            }
        }

        // Check for position drift
        if (handsVisible && CheckForDrift(frame))
        {
            PauseGame("Hands have drifted too far!\n\nPlease return to your baseline position.");
            OnHandsDrifted?.Invoke();
            return;
        }

        // Only check the TARGET hand
        foreach (Hand hand in frame.Hands)
        {
            Chirality handChirality = hand.IsLeft ? Chirality.Left : Chirality.Right;
            if (handChirality == TargetHand)
            {
                CheckFingerPress(hand);
                break;
            }
        }
    }

    private void CheckFingerPress(Hand hand)
    {
        if (exerciseComplete) return; // Already detected a press this round

        Chirality chirality = hand.IsLeft ? Chirality.Left : Chirality.Right;

        // CRITICAL: Only check if this is the target hand
        if (chirality != TargetHand)
        {
            return; // Wrong hand, skip
        }

        float[] currentAngles = GetFingerAngles(hand);
        float[] baseline = baselineAngles[chirality];
        float[] pressed = pressedAngles[chirality];

        // NEW DETECTION LOGIC: Compare distance to baseline vs distance to pressed state
        // For each finger, calculate how close it is to baseline vs pressed
        float[] distanceToBaseline = new float[5];
        float[] distanceToPressed = new float[5];
        bool[] fingerIsPressed = new bool[5];

        for (int i = 0; i < 5; i++)
        {
            distanceToBaseline[i] = Mathf.Abs(currentAngles[i] - baseline[i]);
            distanceToPressed[i] = Mathf.Abs(currentAngles[i] - pressed[i]);

            // Finger is considered pressed if it's much closer to pressed state than baseline
            fingerIsPressed[i] = distanceToPressed[i] < (distanceToBaseline[i] * pressedThresholdRatio);
        }

        // Debug: show all states periodically
        if (Time.frameCount % 30 == 0)
        {
            string pressedStr = "";
            for (int i = 0; i < 5; i++)
            {
                pressedStr += fingerIsPressed[i] ? "1" : "0";
            }
            Debug.Log($"[{chirality}] Pressed:[{pressedStr}] T:{distanceToBaseline[0]:F0}/{distanceToPressed[0]:F0} " +
                     $"I:{distanceToBaseline[1]:F0}/{distanceToPressed[1]:F0} " +
                     $"M:{distanceToBaseline[2]:F0}/{distanceToPressed[2]:F0} " +
                     $"R:{distanceToBaseline[3]:F0}/{distanceToPressed[3]:F0} " +
                     $"P:{distanceToBaseline[4]:F0}/{distanceToPressed[4]:F0} | Target: {fingerNames[TargetFingerIndex]}");
        }

        // Find which finger is most clearly pressed
        int mostPressedFinger = -1;
        float maxPressedConfidence = 0f;

        for (int i = 0; i < 5; i++)
        {
            if (fingerIsPressed[i])
            {
                // Confidence is how much closer to pressed than baseline
                float confidence = distanceToBaseline[i] - distanceToPressed[i];
                if (confidence > maxPressedConfidence)
                {
                    maxPressedConfidence = confidence;
                    mostPressedFinger = i;
                }
            }
        }

        // If no finger is clearly pressed, wait
        if (mostPressedFinger == -1)
        {
            return; // No clear press detected
        }

        // Check if the pressed finger matches the target
        Debug.Log($"[DETECTION] Pressed={fingerNames[mostPressedFinger]}, Target={fingerNames[TargetFingerIndex]}, " +
                 $"Confidence={maxPressedConfidence:F0}°");

        if (mostPressedFinger == TargetFingerIndex)
        {
            // CORRECT!
            Debug.Log($"=== CORRECT: {chirality} {fingerNames[TargetFingerIndex]} idx={TargetFingerIndex} " +
                     $"(baseline dist: {distanceToBaseline[TargetFingerIndex]:F0}°, pressed dist: {distanceToPressed[TargetFingerIndex]:F0}°) ===");
            OnGestureSuccess?.Invoke(chirality, TargetFingerIndex);
            OnExerciseSuccessInternal();
            currentExerciseState = ExerciseState.Correct;
        }
        else
        {
            // WRONG finger pressed
            Debug.Log($"=== WRONG: Pressed {fingerNames[mostPressedFinger]} idx={mostPressedFinger} instead of {fingerNames[TargetFingerIndex]} idx={TargetFingerIndex} ===");
            OnGestureSuccess?.Invoke(chirality, mostPressedFinger);
            OnExerciseSuccessInternal();
            currentExerciseState = ExerciseState.Incorrect;
        }
    }

    private bool CheckForDrift(Frame frame)
    {
        foreach (Hand hand in frame.Hands)
        {
            Chirality chirality = hand.IsLeft ? Chirality.Left : Chirality.Right;

            if (!baselinePositions.ContainsKey(chirality)) continue;

            Vector3 baseline = baselinePositions[chirality];
            if (baseline == Vector3.zero) continue; // Not calibrated for this hand

            Vector3 currentPos = hand.PalmPosition;
            float distance = Vector3.Distance(baseline, currentPos);

            if (distance > maxPositionDrift)
            {
                handsDrifted = true;
                return true;
            }
        }

        handsDrifted = false;
        return false;
    }

    private void CheckForResume(Frame frame)
    {
        if (!handsVisible) return;

        // Check if hands are back in position
        bool handsInPosition = true;

        foreach (Hand hand in frame.Hands)
        {
            Chirality chirality = hand.IsLeft ? Chirality.Left : Chirality.Right;

            if (!baselinePositions.ContainsKey(chirality)) continue;

            Vector3 baseline = baselinePositions[chirality];
            if (baseline == Vector3.zero) continue;

            Vector3 currentPos = hand.PalmPosition;
            float distance = Vector3.Distance(baseline, currentPos);

            // Use a smaller threshold for resuming (must be closer than the drift threshold)
            if (distance > maxPositionDrift * 0.7f)
            {
                handsInPosition = false;
                break;
            }
        }

        if (handsInPosition)
        {
            ResumeGame();
        }
    }

    private void PauseGame(string reason)
    {
        if (isPaused) return;

        isPaused = true;
        gameState = GameState.Paused;
        OnGamePaused?.Invoke();
        OnCalibrationStatus?.Invoke(reason);
        Debug.Log($"Game Paused: {reason}");
    }

    private void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        gameState = GameState.Playing;
        handsDrifted = false;
        OnGameResumed?.Invoke();
        OnHandsRestored?.Invoke();
        OnCalibrationStatus?.Invoke("Welcome back! Continue playing.");
        Debug.Log("Game Resumed");
    }

    public void StartCalibration()
    {
        if (gameState == GameState.PreCalibrationCountdown || gameState == GameState.CalibratingBaseline || gameState == GameState.CalibratingFingers)
            return;

        // Reset state
        isCalibrated = false;
        isCalibrating = false;
        lastCountdownValue = -1;

        // Reset baseline data
        baselineAngles[Chirality.Left] = new float[5];
        baselineAngles[Chirality.Right] = new float[5];
        pressedAngles[Chirality.Left] = new float[5];
        pressedAngles[Chirality.Right] = new float[5];
        baselinePositions[Chirality.Left] = Vector3.zero;
        baselinePositions[Chirality.Right] = Vector3.zero;
        calibrationSamples.Clear();

        // Start countdown
        gameState = GameState.PreCalibrationCountdown;
        countdownStartTime = Time.time;

        string msg = "Get Ready!\n\nPosition your hands flat above the sensor.";
        Debug.Log(msg);
        OnCalibrationStatus?.Invoke(msg);
        OnCountdownTick?.Invoke((int)preCalibrationCountdown);
    }

    private void ProcessBaselineCalibration(Frame frame)
    {
        float elapsed = Time.time - calibrationStartTime;
        float remaining = calibrationTime - elapsed;
        float progress = elapsed / calibrationTime;

        OnCalibrationProgress?.Invoke(progress);

        if (remaining > 0)
        {
            if (!handsVisible)
            {
                OnCalibrationStatus?.Invoke("No hands detected!\nPlease keep your hands visible.");
                return;
            }

            // Collect samples from all visible hands
            foreach (Hand hand in frame.Hands)
            {
                float[] angles = GetFingerAngles(hand);
                Chirality chirality = hand.IsLeft ? Chirality.Left : Chirality.Right;

                // Average angles into baseline
                for (int i = 0; i < 5; i++)
                {
                    // Weighted running average
                    if (baselineAngles[chirality][i] == 0)
                        baselineAngles[chirality][i] = angles[i];
                    else
                        baselineAngles[chirality][i] = baselineAngles[chirality][i] * 0.8f + angles[i] * 0.2f;
                }

                // Average position into baseline
                Vector3 palmPos = hand.PalmPosition;
                if (baselinePositions[chirality] == Vector3.zero)
                    baselinePositions[chirality] = palmPos;
                else
                    baselinePositions[chirality] = Vector3.Lerp(baselinePositions[chirality], palmPos, 0.2f);
            }

            OnCalibrationStatus?.Invoke($"Recording baseline... {remaining:F1}s\nKeep hands still!");
        }
        else
        {
            // Baseline complete - now calibrate individual fingers
            Debug.Log("=== BASELINE CALIBRATION COMPLETE ===");
            Debug.Log("Left hand baseline angles: " + FormatAngles(baselineAngles[Chirality.Left]));
            Debug.Log("Right hand baseline angles: " + FormatAngles(baselineAngles[Chirality.Right]));

            // Start per-finger calibration
            gameState = GameState.CalibratingFingers;
            currentCalibrationHand = Chirality.Left;
            currentCalibrationFingerIndex = 0;
            currentFingerCalibrationNumber = 0;
            calibrationStartTime = Time.time;

            string handName = currentCalibrationHand == Chirality.Left ? "LEFT" : "RIGHT";
            OnCalibrationStatus?.Invoke($"Now press individual fingers\n\nPress {handName} {fingerNames[currentCalibrationFingerIndex].ToUpper()}");
            OnCalibrationProgress?.Invoke(0f);
        }
    }

    private void ProcessFingerCalibration(Frame frame)
    {
        if (!handsVisible)
        {
            OnCalibrationStatus?.Invoke("Hands lost! Please return hands to sensor.");
            return;
        }

        // Find the target hand
        Hand targetHand = null;
        foreach (Hand hand in frame.Hands)
        {
            Chirality handChirality = hand.IsLeft ? Chirality.Left : Chirality.Right;
            if (handChirality == currentCalibrationHand)
            {
                targetHand = hand;
                break;
            }
        }

        if (targetHand == null)
        {
            OnCalibrationStatus?.Invoke($"{currentCalibrationHand} hand not detected!\nPlease show both hands.");
            return;
        }

        // Get current angles
        float[] currentAngles = GetFingerAngles(targetHand);
        float[] baseline = baselineAngles[currentCalibrationHand];

        // Calculate deltas from baseline
        float[] deltas = new float[5];
        for (int i = 0; i < 5; i++)
        {
            deltas[i] = (i == 0) ? Mathf.Abs(currentAngles[i] - baseline[i]) : (currentAngles[i] - baseline[i]);
        }

        // Check if target finger is being pressed
        float targetDelta = (currentCalibrationFingerIndex == 0) ?
            Mathf.Abs(currentAngles[currentCalibrationFingerIndex] - baseline[currentCalibrationFingerIndex]) :
            (currentAngles[currentCalibrationFingerIndex] - baseline[currentCalibrationFingerIndex]);

        if (targetDelta >= minDetectionThreshold)
        {
            // Target finger is being pressed - hold steady to calibrate
            float elapsed = Time.time - calibrationStartTime;
            float progress = elapsed / calibrationTime;

            if (elapsed < calibrationTime)
            {
                // Collecting samples
                OnCalibrationProgress?.Invoke(progress);

                // Average the pressed angle
                if (pressedAngles[currentCalibrationHand][currentCalibrationFingerIndex] == 0)
                    pressedAngles[currentCalibrationHand][currentCalibrationFingerIndex] = currentAngles[currentCalibrationFingerIndex];
                else
                    pressedAngles[currentCalibrationHand][currentCalibrationFingerIndex] =
                        pressedAngles[currentCalibrationHand][currentCalibrationFingerIndex] * 0.8f + currentAngles[currentCalibrationFingerIndex] * 0.2f;

                string handName = currentCalibrationHand == Chirality.Left ? "LEFT" : "RIGHT";
                OnCalibrationStatus?.Invoke($"Hold {handName} {fingerNames[currentCalibrationFingerIndex].ToUpper()}\n{(calibrationTime - elapsed):F1}s");
            }
            else
            {
                // This finger is done!
                Debug.Log($"Calibrated {currentCalibrationHand} {fingerNames[currentCalibrationFingerIndex]}: " +
                         $"Baseline={baseline[currentCalibrationFingerIndex]:F0}°, " +
                         $"Pressed={pressedAngles[currentCalibrationHand][currentCalibrationFingerIndex]:F0}°, " +
                         $"Delta={targetDelta:F0}°");

                // Move to next finger
                currentFingerCalibrationNumber++;
                currentCalibrationFingerIndex++;

                if (currentCalibrationFingerIndex >= 5)
                {
                    // Move to next hand or finish
                    if (currentCalibrationHand == Chirality.Left)
                    {
                        currentCalibrationHand = Chirality.Right;
                        currentCalibrationFingerIndex = 0;
                    }
                    else
                    {
                        // ALL DONE!
                        FinishCalibration();
                        return;
                    }
                }

                // Start calibrating next finger
                calibrationStartTime = Time.time;
                string handName = currentCalibrationHand == Chirality.Left ? "LEFT" : "RIGHT";
                OnCalibrationStatus?.Invoke($"Finger {currentFingerCalibrationNumber + 1} of {totalFingersToCalibrate}\n\nPress {handName} {fingerNames[currentCalibrationFingerIndex].ToUpper()}");
                OnCalibrationProgress?.Invoke((float)currentFingerCalibrationNumber / totalFingersToCalibrate);
            }
        }
        else
        {
            // Finger not pressed enough - wait
            calibrationStartTime = Time.time; // Reset timer
            string handName = currentCalibrationHand == Chirality.Left ? "LEFT" : "RIGHT";
            OnCalibrationStatus?.Invoke($"Finger {currentFingerCalibrationNumber + 1} of {totalFingersToCalibrate}\n\nPress {handName} {fingerNames[currentCalibrationFingerIndex].ToUpper()}");
        }
    }

    private void FinishCalibration()
    {
        isCalibrating = false;
        isCalibrated = true;
        gameState = GameState.Playing;

        Debug.Log("=== ALL CALIBRATION COMPLETE ===");
        Debug.Log($"Baseline Left: {FormatAngles(baselineAngles[Chirality.Left])}");
        Debug.Log($"Pressed Left: {FormatAngles(pressedAngles[Chirality.Left])}");
        Debug.Log($"Baseline Right: {FormatAngles(baselineAngles[Chirality.Right])}");
        Debug.Log($"Pressed Right: {FormatAngles(pressedAngles[Chirality.Right])}");
        Debug.Log($"Max drift allowed: {maxPositionDrift}m");

        // Automatically save calibration data
        SaveCalibration();

        OnCalibrationProgress?.Invoke(1f);
        OnCalibrationStatus?.Invoke("Calibration complete!\n\nGame starting...");
    }

    private float[] GetFingerAngles(Hand hand)
    {
        float[] angles = new float[5];
        List<Finger> fingers = new List<Finger>
        {
            hand.Thumb,
            hand.Index,
            hand.Middle,
            hand.Ring,
            hand.Pinky
        };

        for (int i = 0; i < 5; i++)
        {
            if (i == 0)
            {
                // Thumb - measure distance from palm for opposition movement
                // Thumb moves perpendicular to palm plane, not just curling
                Bone metacarpal = fingers[i].bones[0];
                Bone proximal = fingers[i].bones[1];
                Bone intermediate = fingers[i].bones[2];
                Bone distal = fingers[i].bones[3];

                // Use multiple joint angles for better thumb press detection
                float angle1 = Vector3.Angle(metacarpal.Direction, proximal.Direction);
                float angle2 = Vector3.Angle(proximal.Direction, intermediate.Direction);
                float angle3 = Vector3.Angle(intermediate.Direction, distal.Direction);

                angles[i] = angle1 + angle2 + angle3;
            }
            else
            {
                Bone metacarpal = fingers[i].bones[0];
                Bone proximal = fingers[i].bones[1];
                Bone intermediate = fingers[i].bones[2];

                float mcpAngle = Vector3.Angle(metacarpal.Direction, proximal.Direction);
                float pipAngle = Vector3.Angle(proximal.Direction, intermediate.Direction);
                angles[i] = mcpAngle + pipAngle;
            }
        }

        return angles;
    }

    private string FormatAngles(float[] angles)
    {
        return $"T:{angles[0]:F0}° I:{angles[1]:F0}° M:{angles[2]:F0}° R:{angles[3]:F0}° P:{angles[4]:F0}°";
    }

    // Renamed to internal as GameManager will control public events
    private void OnExerciseSuccessInternal()
    {
        exerciseComplete = true;
        score++;
        
        Debug.Log("Success! Finger Individuation Detected.");
        // Using the new logger
        if (HandDataLogger.Instance != null)
            HandDataLogger.Instance.LogGameEvent("GestureSuccess", GetTargetFingerName());
    }
    
    // Method for GameManager to reset and set new targets
    public void ResetExercise(Chirality newTargetHand, int newTargetFingerIndex)
    {
        TargetHand = newTargetHand;
        TargetFingerIndex = newTargetFingerIndex;
        exerciseComplete = false;
        currentExerciseState = ExerciseState.Pending;
        Debug.Log($"New Target: {newTargetHand} {GetFingerName(newTargetFingerIndex)}");
    }
    
        private string GetTargetFingerName()
        {
            string handName = (TargetHand == Chirality.Left) ? "Left" : "Right";
            string fingerName = GetFingerName(TargetFingerIndex);
            return $"{handName} {fingerName}";
        }
    
        private string GetFingerName(int index)
        {
            string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
            if (index >= 0 && index < fingerNames.Length)
            {
                return fingerNames[index];
            }
            return "Unknown";
        }
    
        public Vector3 GetTargetFingerKnucklePosition()
        {
            if (leapProvider == null) return Vector3.zero;
    
            Frame frame = leapProvider.CurrentFrame;
            if (frame.Hands.Count == 0) return Vector3.zero;
    
            Hand targetHandData = null;
            foreach (Hand hand in frame.Hands)
            {
                if (hand.IsLeft && TargetHand == Chirality.Left)
                {
                    targetHandData = hand;
                    break;
                }
                else if (hand.IsRight && TargetHand == Chirality.Right)
                {
                    targetHandData = hand;
                    break;
                }
            }
    
            if (targetHandData == null) return Vector3.zero;
    
            List<Finger> fingers = new List<Finger>
            {
                targetHandData.Thumb,
                targetHandData.Index,
                targetHandData.Middle,
                targetHandData.Ring,
                targetHandData.Pinky
            };
    
            if (TargetFingerIndex >= fingers.Count || fingers[TargetFingerIndex] == null) return Vector3.zero;
    
            // Return the position of the metacarpal joint (base knuckle) for the target finger
            return fingers[TargetFingerIndex].bones[0].NextJoint; // Bone 0 is metacarpal, NextJoint is its tip
        }
    
        // New method to get the fingertip position
        public Vector3 GetTargetFingerTipPosition()
        {
            if (leapProvider == null) return Vector3.zero;
    
            Frame frame = leapProvider.CurrentFrame;
            if (frame.Hands.Count == 0) return Vector3.zero;
    
            Hand targetHandData = null;
            foreach (Hand hand in frame.Hands)
            {
                if (hand.IsLeft && TargetHand == Chirality.Left)
                {
                    targetHandData = hand;
                    break;
                }
                else if (hand.IsRight && TargetHand == Chirality.Right)
                {
                    targetHandData = hand;
                    break;
                }
            }
    
            if (targetHandData == null) return Vector3.zero;
    
            List<Finger> fingers = new List<Finger>
            {
                targetHandData.Thumb,
                targetHandData.Index,
                targetHandData.Middle,
                targetHandData.Ring,
                targetHandData.Pinky
            };
    
            if (TargetFingerIndex >= fingers.Count || fingers[TargetFingerIndex] == null) return Vector3.zero;
    
            // Return the position of the tip of the target finger
            return fingers[TargetFingerIndex].TipPosition;
        }

        // ===== CALIBRATION SAVE/LOAD SYSTEM =====

        /// <summary>
        /// Check if saved calibration data exists
        /// </summary>
        public bool HasSavedCalibration()
        {
            return PlayerPrefs.HasKey("CalibrationSaved");
        }

        /// <summary>
        /// Save current calibration data to PlayerPrefs
        /// </summary>
        public void SaveCalibration()
        {
            if (!isCalibrated)
            {
                Debug.LogWarning("Cannot save - not calibrated yet!");
                return;
            }

            // Save each hand's data
            foreach (Chirality hand in new[] { Chirality.Left, Chirality.Right })
            {
                if (baselineAngles.ContainsKey(hand) && pressedAngles.ContainsKey(hand))
                {
                    // Save baseline angles
                    for (int i = 0; i < 5; i++)
                    {
                        PlayerPrefs.SetFloat($"Baseline_{hand}_{i}", baselineAngles[hand][i]);
                        PlayerPrefs.SetFloat($"Pressed_{hand}_{i}", pressedAngles[hand][i]);
                    }

                    // Save baseline position
                    if (baselinePositions.ContainsKey(hand))
                    {
                        Vector3 pos = baselinePositions[hand];
                        PlayerPrefs.SetFloat($"BaselinePos_{hand}_X", pos.x);
                        PlayerPrefs.SetFloat($"BaselinePos_{hand}_Y", pos.y);
                        PlayerPrefs.SetFloat($"BaselinePos_{hand}_Z", pos.z);
                    }
                }
            }

            PlayerPrefs.SetInt("CalibrationSaved", 1);
            PlayerPrefs.Save();

            Debug.Log("✓ Calibration data saved!");
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowSuccess("Calibration saved!");
            }
        }

        /// <summary>
        /// Load calibration data from PlayerPrefs
        /// </summary>
        public bool LoadCalibration()
        {
            if (!HasSavedCalibration())
            {
                Debug.LogWarning("No saved calibration data found!");
                return false;
            }

            // Load each hand's data
            foreach (Chirality hand in new[] { Chirality.Left, Chirality.Right })
            {
                float[] baseline = new float[5];
                float[] pressed = new float[5];

                for (int i = 0; i < 5; i++)
                {
                    baseline[i] = PlayerPrefs.GetFloat($"Baseline_{hand}_{i}", 0f);
                    pressed[i] = PlayerPrefs.GetFloat($"Pressed_{hand}_{i}", 0f);
                }

                baselineAngles[hand] = baseline;
                pressedAngles[hand] = pressed;

                // Load baseline position
                Vector3 pos = new Vector3(
                    PlayerPrefs.GetFloat($"BaselinePos_{hand}_X", 0f),
                    PlayerPrefs.GetFloat($"BaselinePos_{hand}_Y", 0f),
                    PlayerPrefs.GetFloat($"BaselinePos_{hand}_Z", 0f)
                );
                baselinePositions[hand] = pos;
            }

            isCalibrated = true;
            gameState = GameState.Playing;

            Debug.Log("✓ Calibration data loaded!");
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowSuccess("Calibration loaded!");
                GameUIManager.Instance.HideProgress();
                GameUIManager.Instance.HideCountdown();
            }

            return true;
        }

        /// <summary>
        /// Delete saved calibration data
        /// </summary>
        public void ClearSavedCalibration()
        {
            PlayerPrefs.DeleteKey("CalibrationSaved");

            foreach (Chirality hand in new[] { Chirality.Left, Chirality.Right })
            {
                for (int i = 0; i < 5; i++)
                {
                    PlayerPrefs.DeleteKey($"Baseline_{hand}_{i}");
                    PlayerPrefs.DeleteKey($"Pressed_{hand}_{i}");
                }
                PlayerPrefs.DeleteKey($"BaselinePos_{hand}_X");
                PlayerPrefs.DeleteKey($"BaselinePos_{hand}_Y");
                PlayerPrefs.DeleteKey($"BaselinePos_{hand}_Z");
            }

            PlayerPrefs.Save();
            Debug.Log("Saved calibration cleared");
        }

        /// <summary>
        /// Prompt user to choose between loading saved calibration or recalibrating
        /// Returns true if user wants to load saved data
        /// </summary>
        public bool PromptCalibrationChoice()
        {
            // This will be called by game managers
            // They can use this to decide whether to call LoadCalibration() or StartCalibration()
            return HasSavedCalibration();
        }
    }
