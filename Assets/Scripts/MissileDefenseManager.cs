using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

public class MissileDefenseManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int startingLives = 5;
    [SerializeField] private float initialMissileSpeed = 1.5f;
    [SerializeField] private float speedIncreasePerWave = 0.2f;
    [SerializeField] private float timeBetweenMissiles = 2.0f;
    [SerializeField] private int pointsPerDestroy = 10;
    [SerializeField] private int pointsLostPerMiss = -20;

    [Header("Missile Spawning")]
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private Transform missileSpawnParent;
    [SerializeField] private float spawnHeight = 5.0f;
    [SerializeField] private float targetHeight = -2.0f;

    [Header("Finger Target Positions")]
    [SerializeField] private Transform[] leftFingerTargets = new Transform[5];  // Thumb to Pinky
    [SerializeField] private Transform[] rightFingerTargets = new Transform[5]; // Thumb to Pinky

    [Header("Materials")]
    [SerializeField] private Material missileMaterial;
    [SerializeField] private Material warningMaterial;

    [Header("Audio")]
    [SerializeField] private AudioClip missileSpawnSound;
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private AudioClip missSound;

    // Game state
    private int currentScore = 0;
    private int currentLives;
    private int currentWave = 1;
    private float currentMissileSpeed;
    private bool gameActive = false;
    private List<Missile> activeMissiles = new List<Missile>();

    // Finger detection
    private FingerIndividuationGame fingerGame;
    private string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

    // Target positions dictionary
    private Dictionary<Chirality, Transform[]> fingerTargets = new Dictionary<Chirality, Transform[]>();

    void Start()
    {
        // Setup finger target positions
        fingerTargets[Chirality.Left] = leftFingerTargets;
        fingerTargets[Chirality.Right] = rightFingerTargets;

        // Find and subscribe to finger game
        fingerGame = FindObjectOfType<FingerIndividuationGame>();
        if (fingerGame != null)
        {
            FingerIndividuationGame.OnGestureSuccess += HandleFingerPress;
        }
        else
        {
            Debug.LogError("MissileDefenseManager: FingerIndividuationGame not found!");
        }

        // Subscribe to missile events
        Missile.OnMissileDestroyed += HandleMissileDestroyed;
        Missile.OnMissileReachedTarget += HandleMissileReachedTarget;

        // Ensure UI exists
        if (GameUIManager.Instance == null)
        {
            GameObject uiGO = new GameObject("GameUIManager");
            uiGO.AddComponent<GameUIManager>();
        }

        // Start calibration then game
        StartCoroutine(CalibrationAndGameStart());
    }

    void OnDestroy()
    {
        if (fingerGame != null)
        {
            FingerIndividuationGame.OnGestureSuccess -= HandleFingerPress;
        }
        Missile.OnMissileDestroyed -= HandleMissileDestroyed;
        Missile.OnMissileReachedTarget -= HandleMissileReachedTarget;
    }

    IEnumerator CalibrationAndGameStart()
    {
        yield return new WaitForSeconds(0.5f);

        // Show instructions
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowInstruction("MISSILE DEFENSE");
            GameUIManager.Instance.ShowCalibrationStatus("Destroy incoming missiles by pressing\nthe correct finger!");
        }

        yield return new WaitForSeconds(2.0f);

        // Start calibration if needed
        if (fingerGame != null && !fingerGame.IsCalibrated)
        {
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

        // Start game
        StartGame();
    }

    void StartGame()
    {
        currentScore = 0;
        currentLives = startingLives;
        currentWave = 1;
        currentMissileSpeed = initialMissileSpeed;
        gameActive = true;

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateScore(currentScore);
            GameUIManager.Instance.ShowInstruction($"WAVE {currentWave} | LIVES: {currentLives}");
        }

        if (HandDataLogger.Instance != null)
        {
            HandDataLogger.Instance.LogGameEvent("MissileDefense", "Started");
        }

        Debug.Log("Missile Defense Started!");

        // Start spawning missiles
        StartCoroutine(SpawnMissiles());
    }

    IEnumerator SpawnMissiles()
    {
        yield return new WaitForSeconds(2f); // Initial delay

        while (gameActive && currentLives > 0)
        {
            // Wait if paused
            while (fingerGame != null && fingerGame.IsPaused)
            {
                yield return null;
            }

            SpawnRandomMissile();

            yield return new WaitForSeconds(timeBetweenMissiles);

            // Increase difficulty every 10 missiles
            if (activeMissiles.Count == 0 && currentScore > 0 && currentScore % 100 == 0)
            {
                StartNewWave();
            }
        }

        if (currentLives <= 0)
        {
            GameOver();
        }
    }

    void SpawnRandomMissile()
    {
        // Random hand and finger
        Chirality targetHand = (Random.value > 0.5f) ? Chirality.Left : Chirality.Right;
        int targetFinger = Random.Range(0, 5);

        SpawnMissile(targetHand, targetFinger);
    }

    void SpawnMissile(Chirality hand, int fingerIndex)
    {
        if (missilePrefab == null)
        {
            Debug.LogError("Missile prefab not assigned!");
            return;
        }

        // Get target position
        Transform targetTransform = fingerTargets[hand][fingerIndex];
        if (targetTransform == null)
        {
            Debug.LogError($"Target position not set for {hand} {fingerNames[fingerIndex]}");
            return;
        }

        Vector3 targetPos = targetTransform.position;

        // Spawn position (above target with some random offset)
        Vector3 spawnPos = new Vector3(
            targetPos.x + Random.Range(-0.5f, 0.5f),
            spawnHeight,
            targetPos.z + Random.Range(-0.5f, 0.5f)
        );

        // Create missile
        GameObject missileObj = Instantiate(missilePrefab, spawnPos, Quaternion.identity);
        if (missileSpawnParent != null)
        {
            missileObj.transform.SetParent(missileSpawnParent);
        }

        Missile missile = missileObj.GetComponent<Missile>();
        if (missile != null)
        {
            missile.Initialize(hand, fingerIndex, spawnPos, targetPos, currentMissileSpeed);
            missile.normalMaterial = missileMaterial;
            missile.warningMaterial = warningMaterial;
            missile.shootSound = destroySound;
            missile.explosionSound = missSound;

            activeMissiles.Add(missile);

            // Show visual indicator for which finger to press
            string handName = hand == Chirality.Left ? "LEFT" : "RIGHT";
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowTargetFinger(handName, fingerNames[fingerIndex].ToUpper());
            }

            Debug.Log($"Spawned missile targeting {hand} {fingerNames[fingerIndex]}");
        }

        // Play spawn sound
        if (missileSpawnSound != null)
        {
            AudioSource.PlayClipAtPoint(missileSpawnSound, spawnPos, 0.5f);
        }
    }

    private void HandleFingerPress(Chirality hand, int fingerIndex)
    {
        if (!gameActive) return;

        Debug.Log($"Finger pressed: {hand} {fingerNames[fingerIndex]}");

        // Check if any missile matches this finger
        Missile targetMissile = null;
        foreach (Missile missile in activeMissiles)
        {
            if (missile.targetHand == hand && missile.targetFingerIndex == fingerIndex)
            {
                targetMissile = missile;
                break;
            }
        }

        if (targetMissile != null)
        {
            // Correct press - destroy missile
            targetMissile.Destroy(true);
        }
        else
        {
            // Wrong finger pressed - no penalty, just feedback
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowError("No missile targeting that finger!");
            }
        }
    }

    private void HandleMissileDestroyed(Missile missile, bool wasCorrectPress)
    {
        activeMissiles.Remove(missile);

        if (wasCorrectPress)
        {
            // Add points
            currentScore += pointsPerDestroy;

            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.UpdateScore(currentScore);
                GameUIManager.Instance.ShowSuccess($"+{pointsPerDestroy}");
                GameUIManager.Instance.HideTargetFinger();
            }

            if (HandDataLogger.Instance != null)
            {
                HandDataLogger.Instance.LogGameEvent("MissileDestroyed",
                    $"Hand: {missile.targetHand}, Finger: {missile.GetFingerName()}");
            }

            Debug.Log($"Missile destroyed! Score: {currentScore}");
        }
    }

    private void HandleMissileReachedTarget(Missile missile)
    {
        activeMissiles.Remove(missile);

        // Lose a life
        currentLives--;
        currentScore += pointsLostPerMiss;
        if (currentScore < 0) currentScore = 0;

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateScore(currentScore);
            GameUIManager.Instance.ShowError($"MISS! Lives: {currentLives}");
            GameUIManager.Instance.ShowInstruction($"WAVE {currentWave} | LIVES: {currentLives}");
            GameUIManager.Instance.HideTargetFinger();
        }

        if (HandDataLogger.Instance != null)
        {
            HandDataLogger.Instance.LogGameEvent("MissileMissed",
                $"Hand: {missile.targetHand}, Finger: {missile.GetFingerName()}");
        }

        Debug.Log($"Missile hit! Lives remaining: {currentLives}");

        if (currentLives <= 0)
        {
            gameActive = false;
        }
    }

    private void StartNewWave()
    {
        currentWave++;
        currentMissileSpeed += speedIncreasePerWave;

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowInstruction($"WAVE {currentWave} | LIVES: {currentLives}");
            GameUIManager.Instance.ShowSuccess("New Wave!");
        }

        Debug.Log($"Wave {currentWave} started! Speed: {currentMissileSpeed}");
    }

    private void GameOver()
    {
        gameActive = false;

        // Clear remaining missiles
        foreach (Missile missile in activeMissiles)
        {
            if (missile != null)
            {
                Destroy(missile.gameObject);
            }
        }
        activeMissiles.Clear();

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowInstruction("GAME OVER");
            GameUIManager.Instance.ShowCalibrationStatus(
                $"Final Score: {currentScore}\n" +
                $"Wave Reached: {currentWave}\n\n" +
                "Great job!"
            );
        }

        if (HandDataLogger.Instance != null)
        {
            HandDataLogger.Instance.LogGameEvent("GameOver", $"Score: {currentScore}, Wave: {currentWave}");
        }

        Debug.Log($"Game Over! Final Score: {currentScore}, Wave: {currentWave}");

        // Restart after delay
        StartCoroutine(RestartDelay());
    }

    IEnumerator RestartDelay()
    {
        yield return new WaitForSeconds(5f);

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.HideAllUI();
        }

        yield return new WaitForSeconds(1f);

        StartGame();
    }
}
