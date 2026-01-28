using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

public class RhythmGameManager : MonoBehaviour
{
    [Header("Songs")]
    [SerializeField] private List<Song> songLibrary = new List<Song>();
    [SerializeField] private int currentSongIndex = 0;

    [Header("Piano Keys - Assign in order: Thumb, Index, Middle, Ring, Pinky")]
    [SerializeField] private PianoKey[] leftHandKeys = new PianoKey[5];
    [SerializeField] private PianoKey[] rightHandKeys = new PianoKey[5];

    [Header("Materials")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material pressedMaterial;
    [SerializeField] private Material missedMaterial; // Red for missed notes

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    private NoteAudioGenerator noteGenerator;

    [Header("Game Mode")]
    [SerializeField] private bool practiceMode = true; // Wait for correct press, no time limit
    [SerializeField] private float noteDisplayTime = 2.0f; // How long to show each note in practice mode

    [Header("Scoring")]
    [SerializeField] private int correctScore = 100;
    [SerializeField] private int wrongPressScore = -10;

    // Events for UI
    public static event System.Action<int, int, int> OnScoreUpdated; // score, combo, maxCombo
    public static event System.Action<string> OnHitFeedback; // "Perfect!", "Good!", "OK", "Miss"
    public static event System.Action<string> OnSongStarted; // song name
    public static event System.Action<int, int> OnSongEnded; // final score, max possible
    public static event System.Action<float> OnSongProgress; // 0-1 progress

    // Game state
    private Dictionary<Chirality, List<PianoKey>> keyboards = new Dictionary<Chirality, List<PianoKey>>();
    private Song currentSong;
    private bool isPlaying = false;
    private int currentNoteIndex = 0;
    private bool waitingForCorrectPress = false;

    // Current note being played
    private Song.NoteData currentNote;
    private PianoKey currentKey;

    // Scoring
    private int score = 0;
    private int combo = 0;
    private int maxCombo = 0;
    private int correctCount = 0;
    private int wrongCount = 0;

    private FingerIndividuationGame fingerGame;
    private string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

    // Track which note is currently expected
    private Chirality expectedHand;
    private int expectedFingerIndex = -1;

    void Start()
    {
        SetupKeyboards();
        SetupAudio();

        // Subscribe to finger detection
        fingerGame = FindObjectOfType<FingerIndividuationGame>();
        FingerIndividuationGame.OnGestureSuccess += HandleFingerPress;

        // Ensure UI manager exists
        if (GameUIManager.Instance == null)
        {
            GameObject uiGO = new GameObject("GameUIManager");
            uiGO.AddComponent<GameUIManager>();
        }

        // Wait for calibration then start
        StartCoroutine(WaitForCalibrationAndStart());
    }

    void OnDestroy()
    {
        FingerIndividuationGame.OnGestureSuccess -= HandleFingerPress;
    }

    private void SetupKeyboards()
    {
        keyboards[Chirality.Left] = new List<PianoKey>();
        keyboards[Chirality.Right] = new List<PianoKey>();

        // Setup left hand
        for (int i = 0; i < 5; i++)
        {
            if (leftHandKeys != null && i < leftHandKeys.Length && leftHandKeys[i] != null)
            {
                PianoKey key = leftHandKeys[i];
                keyboards[Chirality.Left].Add(key);
                ApplyMaterials(key);
                key.SetState(PianoKey.KeyState.Normal);
            }
        }

        // Setup right hand
        for (int i = 0; i < 5; i++)
        {
            if (rightHandKeys != null && i < rightHandKeys.Length && rightHandKeys[i] != null)
            {
                PianoKey key = rightHandKeys[i];
                keyboards[Chirality.Right].Add(key);
                ApplyMaterials(key);
                key.SetState(PianoKey.KeyState.Normal);
            }
        }

        Debug.Log($"RhythmGame: Keyboards setup - Left: {keyboards[Chirality.Left].Count}, Right: {keyboards[Chirality.Right].Count}");
    }

    private void ApplyMaterials(PianoKey key)
    {
        if (key.normalMaterial == null && normalMaterial != null)
            key.normalMaterial = normalMaterial;
        if (key.highlightMaterial == null && highlightMaterial != null)
            key.highlightMaterial = highlightMaterial;
        if (key.pressedMaterial == null && pressedMaterial != null)
            key.pressedMaterial = pressedMaterial;
        key.InitializeRenderer();
    }

    private void SetupAudio()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        // Create note generator for procedural sounds
        noteGenerator = GetComponent<NoteAudioGenerator>();
        if (noteGenerator == null)
        {
            noteGenerator = gameObject.AddComponent<NoteAudioGenerator>();
        }
    }

    IEnumerator WaitForCalibrationAndStart()
    {
        yield return new WaitForSeconds(0.5f);

        // Only calibrate if not already calibrated (menu may have done it)
        if (fingerGame != null && !fingerGame.IsCalibrated)
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowInstruction("PRACTICE PIANO");
                GameUIManager.Instance.ShowCalibrationStatus("Position hands for calibration...");
            }

            yield return new WaitForSeconds(2f);

            fingerGame.StartCalibration();

            while (!fingerGame.IsCalibrated)
            {
                yield return null;
            }

            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.HideCountdown();
                GameUIManager.Instance.HideProgress();
            }

            yield return new WaitForSeconds(1f);
        }
        else
        {
            // Already calibrated - just show ready message
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowInstruction("PRACTICE PIANO");
                GameUIManager.Instance.HideAllUI();
            }
            yield return new WaitForSeconds(0.5f);
        }

        // Start first song
        if (songLibrary.Count > 0)
        {
            StartSong(songLibrary[currentSongIndex]);
        }
        else
        {
            // Auto-create default songs if none assigned
            Debug.Log("No songs assigned - creating default songs...");
            songLibrary.Add(SongCreator.CreateScaleUp());
            songLibrary.Add(SongCreator.CreateFingerExercise());
            songLibrary.Add(SongCreator.CreateTwinkleTwinkle());
            songLibrary.Add(SongCreator.CreateSpeedChallenge());
            StartSong(songLibrary[0]);
        }
    }

    public void StartSong(Song song)
    {
        if (song == null || song.notes.Count == 0)
        {
            Debug.LogError("Cannot start song - null or empty!");
            return;
        }

        currentSong = song;
        isPlaying = true;
        currentNoteIndex = 0;
        waitingForCorrectPress = false;
        currentNote = null;
        currentKey = null;

        // Reset score
        score = 0;
        combo = 0;
        maxCombo = 0;
        correctCount = 0;
        wrongCount = 0;

        // Reset all keys
        ResetAllKeys();

        OnSongStarted?.Invoke(currentSong.songName);
        OnScoreUpdated?.Invoke(score, combo, maxCombo);

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowInstruction($"Playing: {currentSong.songName}");
            GameUIManager.Instance.UpdateScore(score);
        }

        Debug.Log($"Starting song: {currentSong.songName} ({currentSong.notes.Count} notes)");

        StartCoroutine(PracticeLoop());
    }

    IEnumerator PracticeLoop()
    {
        yield return new WaitForSeconds(1f);

        while (isPlaying && currentNoteIndex < currentSong.notes.Count)
        {
            // Wait if paused
            if (fingerGame != null && fingerGame.IsPaused)
            {
                yield return null;
                continue;
            }

            // Update progress
            float progress = (float)currentNoteIndex / currentSong.notes.Count;
            OnSongProgress?.Invoke(progress);

            // Show next note
            ShowNextNote();

            // Wait for correct press
            waitingForCorrectPress = true;
            while (waitingForCorrectPress && isPlaying)
            {
                if (fingerGame != null && fingerGame.IsPaused)
                {
                    yield return null;
                    continue;
                }
                yield return null;
            }

            // Brief pause between notes
            yield return new WaitForSeconds(0.3f);
        }

        if (isPlaying)
        {
            yield return new WaitForSeconds(1f);
            EndSong();
        }
    }

    private void ShowNextNote()
    {
        if (currentNoteIndex >= currentSong.notes.Count) return;

        // Reset previous key
        if (currentKey != null)
        {
            currentKey.SetState(PianoKey.KeyState.Normal);
        }

        currentNote = currentSong.notes[currentNoteIndex];

        if (!keyboards.ContainsKey(currentNote.hand) ||
            currentNote.fingerIndex >= keyboards[currentNote.hand].Count)
        {
            Debug.LogWarning($"Invalid note - skipping: {currentNote.hand} {currentNote.fingerIndex}");
            currentNoteIndex++;
            return;
        }

        currentKey = keyboards[currentNote.hand][currentNote.fingerIndex];

        // Highlight the key
        currentKey.SetState(PianoKey.KeyState.Highlighted);

        // Set expected note for finger detection
        expectedHand = currentNote.hand;
        expectedFingerIndex = currentNote.fingerIndex;

        // Tell finger game what to expect
        if (fingerGame != null)
        {
            fingerGame.ResetExercise(currentNote.hand, currentNote.fingerIndex);
        }

        // Show in UI
        if (GameUIManager.Instance != null)
        {
            string handName = currentNote.hand == Chirality.Left ? "LEFT" : "RIGHT";
            GameUIManager.Instance.ShowTargetFinger(handName, fingerNames[currentNote.fingerIndex].ToUpper());
            GameUIManager.Instance.ShowCalibrationStatus($"Note {currentNoteIndex + 1} of {currentSong.notes.Count}");
        }

        Debug.Log($"Note {currentNoteIndex + 1}/{currentSong.notes.Count}: {currentNote.hand} {fingerNames[currentNote.fingerIndex]}");
    }

    private void HandleFingerPress(Chirality hand, int fingerIndex)
    {
        if (!isPlaying || !waitingForCorrectPress || currentNote == null) return;

        // Check if correct finger was pressed
        bool isCorrect = (hand == expectedHand && fingerIndex == expectedFingerIndex);

        if (isCorrect)
        {
            CorrectPress();
        }
        else
        {
            WrongPress(hand, fingerIndex);
        }
    }

    private void CorrectPress()
    {
        // Show pressed state
        if (currentKey != null)
        {
            currentKey.SetState(PianoKey.KeyState.Pressed);
        }

        // Play note sound
        PlayNoteSound(expectedFingerIndex);

        // Update score
        combo++;
        if (combo > maxCombo) maxCombo = combo;
        int points = (int)(correctScore * (1f + combo * 0.1f)); // 10% bonus per combo
        score += points;
        correctCount++;

        // Update UI
        OnHitFeedback?.Invoke("CORRECT!");
        OnScoreUpdated?.Invoke(score, combo, maxCombo);

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateScore(score);
            GameUIManager.Instance.ShowSuccess($"Correct! +{points} (x{combo})");
        }

        Debug.Log($"CORRECT! {expectedHand} {fingerNames[expectedFingerIndex]} - combo: {combo}");

        // Move to next note
        currentNoteIndex++;
        waitingForCorrectPress = false;

        // Reset key after brief delay
        StartCoroutine(ResetKeyAfterDelay(currentKey, 0.2f));
    }

    private void WrongPress(Chirality hand, int fingerIndex)
    {
        // Record wrong press but don't advance
        combo = 0;
        wrongCount++;
        score += wrongPressScore;
        if (score < 0) score = 0;

        OnHitFeedback?.Invoke("WRONG");
        OnScoreUpdated?.Invoke(score, combo, maxCombo);

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateScore(score);
            GameUIManager.Instance.ShowError($"Wrong! Press {expectedHand} {fingerNames[expectedFingerIndex]}");
        }

        Debug.Log($"WRONG: Pressed {hand} {fingerNames[fingerIndex]}, expected {expectedHand} {fingerNames[expectedFingerIndex]}");

        // Don't advance - wait for correct press
    }

    IEnumerator ResetKeyAfterDelay(PianoKey key, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (key != null)
        {
            key.SetState(PianoKey.KeyState.Normal);
        }
    }

    private void PlayNoteSound(int fingerIndex)
    {
        // Try song-specific sounds first
        if (currentSong != null &&
            currentSong.noteSounds != null &&
            fingerIndex < currentSong.noteSounds.Length &&
            currentSong.noteSounds[fingerIndex] != null)
        {
            if (sfxSource != null)
                sfxSource.PlayOneShot(currentSong.noteSounds[fingerIndex]);
            return;
        }

        // Fallback to generated notes
        if (noteGenerator != null)
        {
            noteGenerator.PlayNote(fingerIndex);
        }
    }

    private void EndSong()
    {
        isPlaying = false;
        waitingForCorrectPress = false;

        if (musicSource != null)
            musicSource.Stop();

        ResetAllKeys();

        int maxPossible = currentSong.notes.Count * correctScore;
        float accuracy = (currentSong.notes.Count > 0)
            ? (float)correctCount / currentSong.notes.Count * 100f
            : 0f;

        OnSongEnded?.Invoke(score, maxPossible);

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowInstruction("SONG COMPLETE!");
            GameUIManager.Instance.ShowCalibrationStatus(
                $"Score: {score}\n" +
                $"Max Combo: {maxCombo}\n" +
                $"Accuracy: {accuracy:F1}%\n\n" +
                $"Correct: {correctCount} | Wrong Attempts: {wrongCount}"
            );
        }

        Debug.Log($"=== SONG COMPLETE ===");
        Debug.Log($"Score: {score}/{maxPossible}");
        Debug.Log($"Max Combo: {maxCombo}");
        Debug.Log($"Correct: {correctCount}, Wrong attempts: {wrongCount}");

        // Auto-advance to next song after delay
        StartCoroutine(NextSongDelay());
    }

    IEnumerator NextSongDelay()
    {
        yield return new WaitForSeconds(5f);

        currentSongIndex = (currentSongIndex + 1) % songLibrary.Count;

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.HideAllUI();
        }

        yield return new WaitForSeconds(1f);

        if (songLibrary.Count > 0)
        {
            StartSong(songLibrary[currentSongIndex]);
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

        expectedFingerIndex = -1;
    }

    // Public methods to control from UI
    public void PlaySong(int index)
    {
        if (index >= 0 && index < songLibrary.Count)
        {
            currentSongIndex = index;
            StartSong(songLibrary[index]);
        }
    }

    public void RestartSong()
    {
        if (currentSong != null)
        {
            StopAllCoroutines();
            StartSong(currentSong);
        }
    }

    public void StopSong()
    {
        isPlaying = false;
        if (musicSource != null)
            musicSource.Stop();
        ResetAllKeys();
    }
}
