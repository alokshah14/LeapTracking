using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

public class MissileDefenseManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int startingLives = 5;
    [SerializeField] private float initialMissileSpeed = 0.3f;  // Much slower - easy to shoot
    [SerializeField] private float speedIncreasePerWave = 0.05f;  // Gradual increase
    [SerializeField] private float timeBetweenMissiles = 4.0f;  // More time to aim
    [SerializeField] private int pointsPerDestroy = 10;
    [SerializeField] private int pointsLostPerMiss = -20;
    [SerializeField] private float playerProjectileSpeed = 5.0f;  // How fast we shoot up

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

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnParent;

    [Header("Audio")]
    [SerializeField] private AudioClip missileSpawnSound;
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private AudioClip missSound;
    [SerializeField] private AudioClip shootSound;

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

    // Visual highlighting (Leap Motion fingers)
    private LeapFingerHighlighter fingerHighlighter;

    // Old sphere highlighting (keeping for backup)
    private Transform currentHighlightedTarget;
    private Vector3 originalScale;
    private Material originalMaterial;
    private Material highlightMaterial;

    // Active projectiles
    private System.Collections.Generic.List<PlayerProjectile> activeProjectiles = new System.Collections.Generic.List<PlayerProjectile>();

    void Start()
    {
        // Setup finger target positions
        fingerTargets[Chirality.Left] = leftFingerTargets;
        fingerTargets[Chirality.Right] = rightFingerTargets;

        // Create highlight material
        CreateHighlightMaterial();

        // Create or find Leap Finger Highlighter
        fingerHighlighter = FindObjectOfType<LeapFingerHighlighter>();
        if (fingerHighlighter == null)
        {
            GameObject highlighterGO = new GameObject("LeapFingerHighlighter");
            fingerHighlighter = highlighterGO.AddComponent<LeapFingerHighlighter>();
        }

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

        // Subscribe to projectile events
        PlayerProjectile.OnProjectileHit += HandleProjectileHit;

        // Ensure UI exists
        if (GameUIManager.Instance == null)
        {
            GameObject uiGO = new GameObject("GameUIManager");
            uiGO.AddComponent<GameUIManager>();
        }

        // Start calibration then game
        StartCoroutine(CalibrationAndGameStart());
    }

    void CreateHighlightMaterial()
    {
        // Create bright yellow glowing material for highlighting
        highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.color = Color.yellow;
        highlightMaterial.SetColor("_EmissionColor", Color.yellow * 2f);
        highlightMaterial.EnableKeyword("_EMISSION");
    }

    void OnDestroy()
    {
        if (fingerGame != null)
        {
            FingerIndividuationGame.OnGestureSuccess -= HandleFingerPress;
        }
        Missile.OnMissileDestroyed -= HandleMissileDestroyed;
        Missile.OnMissileReachedTarget -= HandleMissileReachedTarget;
        PlayerProjectile.OnProjectileHit -= HandleProjectileHit;
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

        // Check if we have saved calibration
        if (fingerGame != null && !fingerGame.IsCalibrated)
        {
            if (fingerGame.HasSavedCalibration())
            {
                // Try to load saved calibration
                if (GameUIManager.Instance != null)
                {
                    GameUIManager.Instance.ShowCalibrationStatus("Found saved calibration!\nLoading...");
                }

                yield return new WaitForSeconds(1f);

                bool loaded = fingerGame.LoadCalibration();

                if (loaded)
                {
                    if (GameUIManager.Instance != null)
                    {
                        GameUIManager.Instance.ShowSuccess("Calibration loaded!\nPress SPACE to recalibrate");
                    }

                    yield return new WaitForSeconds(2f);
                }
                else
                {
                    // Failed to load, start calibration
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
            }
            else
            {
                // No saved calibration, start fresh
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

            // Highlight the actual Leap Motion finger AND the target sphere
            HighlightFingerTarget(hand, fingerIndex, targetTransform);

            Debug.Log($"Spawned missile targeting {hand} {fingerNames[fingerIndex]}");
        }

        // Play spawn sound
        if (missileSpawnSound != null)
        {
            AudioSource.PlayClipAtPoint(missileSpawnSound, spawnPos, 0.5f);
        }
    }

    void HighlightFingerTarget(Chirality hand, int fingerIndex, Transform target)
    {
        // Unhighlight previous target
        UnhighlightFingerTarget();

        // IMPORTANT: Highlight the actual Leap Motion finger!
        if (fingerHighlighter != null)
        {
            fingerHighlighter.HighlightFinger(hand, fingerIndex);
            Debug.Log($"Highlighting Leap Motion finger: {hand} {fingerNames[fingerIndex]}");
        }

        // Also highlight the target sphere (backup visual)
        Transform marker = target.Find("VisualMarker");
        if (marker != null)
        {
            currentHighlightedTarget = marker;

            // Store original properties
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                originalMaterial = renderer.material;
                originalScale = marker.localScale;

                // Apply highlight
                renderer.material = highlightMaterial;
                marker.localScale = originalScale * 2.5f; // Make it bigger

                // Start pulsing animation
                StartCoroutine(PulseTarget(marker));
            }
        }
    }

    void UnhighlightFingerTarget()
    {
        // Unhighlight Leap Motion finger
        if (fingerHighlighter != null)
        {
            fingerHighlighter.UnhighlightFinger();
        }

        // Unhighlight sphere
        if (currentHighlightedTarget != null)
        {
            Renderer renderer = currentHighlightedTarget.GetComponent<Renderer>();
            if (renderer != null && originalMaterial != null)
            {
                renderer.material = originalMaterial;
                currentHighlightedTarget.localScale = originalScale;
            }
            currentHighlightedTarget = null;
        }
    }

    IEnumerator PulseTarget(Transform target)
    {
        Vector3 baseScale = target.localScale;
        while (target == currentHighlightedTarget && currentHighlightedTarget != null)
        {
            // Pulse between 1x and 1.2x
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.2f;
            target.localScale = baseScale * pulse;
            yield return null;
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
            // Correct press - SHOOT A PROJECTILE at the missile!
            ShootProjectile(hand, fingerIndex, targetMissile);
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

    void ShootProjectile(Chirality hand, int fingerIndex, Missile targetMissile)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab not assigned! Falling back to instant destroy.");
            targetMissile.Destroy(true);
            return;
        }

        // Get the current finger tip position from Leap Motion
        Vector3 fingerTipPos = Vector3.zero;
        if (fingerHighlighter != null)
        {
            fingerTipPos = fingerHighlighter.GetFingerTipPosition(hand, fingerIndex);
        }

        // Fallback to target sphere position if we can't get finger tip
        if (fingerTipPos == Vector3.zero)
        {
            Transform targetTransform = fingerTargets[hand][fingerIndex];
            if (targetTransform != null)
            {
                fingerTipPos = targetTransform.position;
            }
        }

        // Create projectile
        GameObject projectileObj = Instantiate(projectilePrefab, fingerTipPos, Quaternion.identity);
        if (projectileSpawnParent != null)
        {
            projectileObj.transform.SetParent(projectileSpawnParent);
        }

        PlayerProjectile projectile = projectileObj.GetComponent<PlayerProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(hand, fingerIndex, fingerTipPos, targetMissile.transform.position, targetMissile, playerProjectileSpeed);
            activeProjectiles.Add(projectile);

            Debug.Log($"Shot projectile from {hand} {fingerNames[fingerIndex]} toward missile!");
        }

        // Play shoot sound
        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, fingerTipPos, 0.7f);
        }
    }

    private void HandleProjectileHit(PlayerProjectile projectile, Missile missile)
    {
        // Remove projectile from active list
        if (activeProjectiles.Contains(projectile))
        {
            activeProjectiles.Remove(projectile);
        }

        // Destroy the missile
        if (missile != null && activeMissiles.Contains(missile))
        {
            missile.Destroy(true);
        }

        Debug.Log("Projectile hit missile!");
    }

    private void HandleMissileDestroyed(Missile missile, bool wasCorrectPress)
    {
        activeMissiles.Remove(missile);

        // Unhighlight the finger target
        UnhighlightFingerTarget();

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

        // Unhighlight the finger target
        UnhighlightFingerTarget();

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
