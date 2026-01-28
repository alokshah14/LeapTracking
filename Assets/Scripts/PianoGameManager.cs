using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

public class PianoGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float roundTime = 4.0f;
    [SerializeField] private int pointsForCorrect = 10;
    [SerializeField] private int pointsForIncorrect = -5;
    
    [Header("Piano Keys - Assign in order: Thumb, Index, Middle, Ring, Pinky")]
    [SerializeField] private PianoKey[] leftHandKeys = new PianoKey[5];
    [SerializeField] private PianoKey[] rightHandKeys = new PianoKey[5];

    [Header("Materials (applied to keys if not set on them)")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material pressedMaterial;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    private NoteAudioGenerator noteGenerator;

    // --- Private members ---
    private Dictionary<Chirality, List<PianoKey>> keyboards = new Dictionary<Chirality, List<PianoKey>>();
    private int currentScore = 0;

    // Current target
    private Chirality currentTargetHand;
    private int currentTargetFingerIndex; // This now directly corresponds to the key index (0-4)
    private PianoKey currentTargetKey;

    private bool roundActive = false;
    private string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

    private FingerIndividuationGame fingerGame;

    void Start()
    {
        SetupKeyboards();
        SetupAudio();

        // Find the finger game and subscribe to events
        fingerGame = FindObjectOfType<FingerIndividuationGame>();
        FingerIndividuationGame.OnGestureSuccess += HandleGestureSuccess;
        FingerIndividuationGame.OnCalibrationStatus += HandleCalibrationStatus;
        FingerIndividuationGame.OnGamePaused += HandleGamePaused;
        FingerIndividuationGame.OnGameResumed += HandleGameResumed;

        // Ensure GameUIManager exists
        if (GameUIManager.Instance == null)
        {
            GameObject uiGO = new GameObject("GameUIManager");
            uiGO.AddComponent<GameUIManager>();
        }

        // Start calibration, then game loop
        StartCoroutine(CalibrationAndGameStart());
    }

    private void HandleCalibrationStatus(string status)
    {
        Debug.Log($"[Status] {status}");
    }

    private void SetupAudio()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        noteGenerator = GetComponent<NoteAudioGenerator>();
        if (noteGenerator == null)
        {
            noteGenerator = gameObject.AddComponent<NoteAudioGenerator>();
        }
    }

    IEnumerator CalibrationAndGameStart()
    {
        yield return new WaitForSeconds(0.5f); // Let everything initialize

        if (fingerGame != null)
        {
            // Show initial instructions via UI
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowInstruction("FINGER PIANO");
                GameUIManager.Instance.ShowCalibrationStatus("Starting calibration in a moment...\nPosition your hands above the sensor.");
            }

            yield return new WaitForSeconds(2.0f);

            // Start calibration with countdown
            fingerGame.StartCalibration();

            // Wait for calibration to complete
            while (!fingerGame.IsCalibrated)
            {
                yield return null;
            }

            // Hide calibration UI
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.HideCountdown();
                GameUIManager.Instance.HideProgress();
            }

            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            Debug.LogError("FingerIndividuationGame not found! Add it to a GameObject in the scene.");
        }

        // Start the actual game
        StartCoroutine(GameLoop());
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        FingerIndividuationGame.OnGestureSuccess -= HandleGestureSuccess;
        FingerIndividuationGame.OnCalibrationStatus -= HandleCalibrationStatus;
        FingerIndividuationGame.OnGamePaused -= HandleGamePaused;
        FingerIndividuationGame.OnGameResumed -= HandleGameResumed;
    }

    private void HandleGamePaused()
    {
        // Pause the game loop by setting roundActive to false
        roundActive = false;
        Debug.Log("PianoGameManager: Game paused");
    }

    private void HandleGameResumed()
    {
        Debug.Log("PianoGameManager: Game resumed");
        // The game will continue with the next round automatically
    }

    private void SetupKeyboards()
    {
        keyboards[Chirality.Left] = new List<PianoKey>();
        keyboards[Chirality.Right] = new List<PianoKey>();

        Debug.Log("=== Setting up piano keys ===");

        // Setup LEFT hand keys
        for (int i = 0; i < 5; i++)
        {
            if (leftHandKeys != null && i < leftHandKeys.Length && leftHandKeys[i] != null)
            {
                PianoKey key = leftHandKeys[i];
                keyboards[Chirality.Left].Add(key);

                // Apply materials from manager if not set on key
                if (key.normalMaterial == null && normalMaterial != null)
                    key.normalMaterial = normalMaterial;
                if (key.highlightMaterial == null && highlightMaterial != null)
                    key.highlightMaterial = highlightMaterial;
                if (key.pressedMaterial == null && pressedMaterial != null)
                    key.pressedMaterial = pressedMaterial;

                // Initialize the key
                key.InitializeRenderer();
                key.SetState(PianoKey.KeyState.Normal);

                Debug.Log($"  Left {fingerNames[i]}: {key.gameObject.name}");
            }
            else
            {
                Debug.LogError($"  Left {fingerNames[i]}: NOT ASSIGNED - assign it in the Inspector!");
            }
        }

        // Setup RIGHT hand keys
        for (int i = 0; i < 5; i++)
        {
            if (rightHandKeys != null && i < rightHandKeys.Length && rightHandKeys[i] != null)
            {
                PianoKey key = rightHandKeys[i];
                keyboards[Chirality.Right].Add(key);

                // Apply materials from manager if not set on key
                if (key.normalMaterial == null && normalMaterial != null)
                    key.normalMaterial = normalMaterial;
                if (key.highlightMaterial == null && highlightMaterial != null)
                    key.highlightMaterial = highlightMaterial;
                if (key.pressedMaterial == null && pressedMaterial != null)
                    key.pressedMaterial = pressedMaterial;

                // Initialize the key
                key.InitializeRenderer();
                key.SetState(PianoKey.KeyState.Normal);

                Debug.Log($"  Right {fingerNames[i]}: {key.gameObject.name}");
            }
            else
            {
                Debug.LogError($"  Right {fingerNames[i]}: NOT ASSIGNED - assign it in the Inspector!");
            }
        }

        Debug.Log($"Total keys: Left={keyboards[Chirality.Left].Count}, Right={keyboards[Chirality.Right].Count}");

        // Validate: check for duplicate key assignments between hands
        for (int i = 0; i < Mathf.Min(keyboards[Chirality.Left].Count, keyboards[Chirality.Right].Count); i++)
        {
            if (keyboards[Chirality.Left][i] == keyboards[Chirality.Right][i])
            {
                Debug.LogError($"WARNING: Same key assigned to BOTH hands at index {i} ({fingerNames[i]})! This will cause both to highlight.");
            }
        }
    }

    IEnumerator GameLoop()
    {
        if (HandDataLogger.Instance != null) HandDataLogger.Instance.LogGameEvent("PianoGame", "Started");

        // Show initial score
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateScore(currentScore);
            GameUIManager.Instance.ShowInstruction("Press the highlighted finger!");
        }

        yield return new WaitForSeconds(1.0f);

        while (true)
        {
            // Wait if game is paused
            while (fingerGame != null && fingerGame.IsPaused)
            {
                yield return null;
            }

            yield return StartCoroutine(RoundCoroutine());
            yield return new WaitForSeconds(1.5f);
        }
    }

    IEnumerator RoundCoroutine()
    {
        // Reset all keys before starting new round
        ResetAllKeys();

        roundActive = true;
        SelectNewTarget();

        if (currentTargetKey == null) yield break;

        string logDetails = $"Hand: {currentTargetHand}, Finger: {fingerNames[currentTargetFingerIndex]}";
        if (HandDataLogger.Instance != null) HandDataLogger.Instance.LogGameEvent("NewTarget", logDetails);

        // Highlight the key ONLY ONCE
        Debug.Log($">>> Highlighting key: {currentTargetKey.gameObject.name} (ID: {currentTargetKey.GetInstanceID()})");
        currentTargetKey.SetState(PianoKey.KeyState.Highlighted);

        // Also highlight the 3D finger model
        if (GlowFingerManager.Instance != null) GlowFingerManager.Instance.HighlightFinger(currentTargetHand, currentTargetFingerIndex);

        // Show target in UI
        string handName = currentTargetHand == Chirality.Left ? "LEFT" : "RIGHT";
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowTargetFinger(handName, fingerNames[currentTargetFingerIndex].ToUpper());
        }

        // Also, set the target in the FingerIndividuationGame
        if (fingerGame != null)
        {
            fingerGame.ResetExercise(currentTargetHand, currentTargetFingerIndex);
            Debug.Log($"Target: {currentTargetHand} {fingerNames[currentTargetFingerIndex]}");
        }
        else
        {
            Debug.LogError("FingerIndividuationGame not found in scene! Add it to a GameObject.");
        }

        // Wait for round time, but pause-aware
        float elapsed = 0f;
        while (elapsed < roundTime && roundActive)
        {
            // Pause handling
            if (fingerGame != null && fingerGame.IsPaused)
            {
                yield return null;
                continue; // Don't count time while paused
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (roundActive)
        {
            roundActive = false;
            if (HandDataLogger.Instance != null) HandDataLogger.Instance.LogGameEvent("RoundEnd", "Timeout");

            // Reset both highlights
            currentTargetKey.SetState(PianoKey.KeyState.Normal);
            if (GlowFingerManager.Instance != null) GlowFingerManager.Instance.ResetHighlights();
            if (GameUIManager.Instance != null) GameUIManager.Instance.HideTargetFinger();

            UpdateScore(false);
        }
    }

    private void SelectNewTarget()
    {
        // First, reset ALL keys to normal state
        ResetAllKeys();

        // Select a random hand
        currentTargetHand = (Random.value > 0.5f) ? Chirality.Left : Chirality.Right;

        // Select a random finger/key (0-4, including thumb)
        currentTargetFingerIndex = Random.Range(0, 5);

        // Verify we have enough keys
        if (keyboards[currentTargetHand].Count <= currentTargetFingerIndex)
        {
            Debug.LogError($"Not enough keys for {currentTargetHand} hand! Have {keyboards[currentTargetHand].Count}, need index {currentTargetFingerIndex}");
            return;
        }

        currentTargetKey = keyboards[currentTargetHand][currentTargetFingerIndex];

        Debug.Log($"=== NEW TARGET ===");
        Debug.Log($"  Hand: {currentTargetHand}");
        Debug.Log($"  Finger: {fingerNames[currentTargetFingerIndex]} (index {currentTargetFingerIndex})");
        Debug.Log($"  Key GameObject: {currentTargetKey.gameObject.name}");
        Debug.Log($"  Key Instance ID: {currentTargetKey.GetInstanceID()}");
        Debug.Log($"  UI will show: {(currentTargetHand == Chirality.Left ? "LEFT" : "RIGHT")} {fingerNames[currentTargetFingerIndex].ToUpper()}");

        // Check if this key appears in the other hand too
        Chirality otherHand = currentTargetHand == Chirality.Left ? Chirality.Right : Chirality.Left;
        if (keyboards[otherHand].Contains(currentTargetKey))
        {
            Debug.LogError($"  ERROR: This key is ALSO assigned to the {otherHand} hand! This will cause double-highlighting!");
        }
    }

    private void ResetAllKeys()
    {
        foreach (var hand in keyboards.Keys)
        {
            foreach (var key in keyboards[hand])
            {
                key.SetState(PianoKey.KeyState.Normal);
            }
        }
    }

    private void HandleGestureSuccess(Chirality hand, int fingerIndex)
    {
        if (!roundActive) return;

        bool isCorrectHand = (hand == currentTargetHand);
        bool isCorrectFinger = (fingerIndex == currentTargetFingerIndex);

        // DEBUG: Show exactly what was detected vs what was expected
        Debug.Log($"=== GAME CHECK ===");
        Debug.Log($"  DETECTED: {hand} {fingerNames[fingerIndex]} (index {fingerIndex})");
        Debug.Log($"  TARGET:   {currentTargetHand} {fingerNames[currentTargetFingerIndex]} (index {currentTargetFingerIndex})");
        Debug.Log($"  RESULT:   Hand correct: {isCorrectHand}, Finger correct: {isCorrectFinger}");

        string logDetails = $"Gesture: {hand} {fingerNames[fingerIndex]}, Correct: {isCorrectHand && isCorrectFinger}";
        if (HandDataLogger.Instance != null) HandDataLogger.Instance.LogGameEvent("GesturePress", logDetails);

        roundActive = false;

        // Highlight the TARGET key when pressed
        currentTargetKey.SetState(PianoKey.KeyState.Pressed);

        // Reset the 3D finger highlight immediately on a press
        if (GlowFingerManager.Instance != null) GlowFingerManager.Instance.ResetHighlights();
        if (GameUIManager.Instance != null) GameUIManager.Instance.HideTargetFinger();

        if (isCorrectHand && isCorrectFinger)
        {
            // Play sound for correct press
            PlayNoteSound(currentTargetFingerIndex);

            UpdateScore(true);
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.ShowSuccess("Correct!");
            Debug.Log("Correct!");
        }
        else
        {
            UpdateScore(false);
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.ShowError($"Wrong! Target was {currentTargetHand} {fingerNames[currentTargetFingerIndex]}");
            Debug.Log($"Incorrect! Target was {currentTargetHand} {fingerNames[currentTargetFingerIndex]}. You gestured with {hand} {fingerNames[fingerIndex]}");
        }

        StartCoroutine(ResetAndContinue(currentTargetKey));
    }

    private void UpdateScore(bool correct)
    {
        currentScore += correct ? pointsForCorrect : pointsForIncorrect;
        if (currentScore < 0) currentScore = 0; // Don't go negative

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.UpdateScore(currentScore);

        Debug.Log($"Score: {currentScore}");
    }

    IEnumerator ResetAndContinue(PianoKey key)
    {
        yield return new WaitForSeconds(0.5f);
        key.SetState(PianoKey.KeyState.Normal);

        // Continue to the next round
        yield return new WaitForSeconds(1.0f);

        // Clear any status messages before next round
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.HideAllUI();

        StartCoroutine(RoundCoroutine());
    }

    private void PlayNoteSound(int fingerIndex)
    {
        if (noteGenerator != null)
        {
            noteGenerator.PlayNote(fingerIndex);
        }
    }

}