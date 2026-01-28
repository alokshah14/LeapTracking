using UnityEngine;
using Leap;
using System.Collections.Generic;

public class MainController : MonoBehaviour
{
    [Header("Leap Motion")]
    [SerializeField]
    [Tooltip("The LeapProvider to use for getting frame data.")]
    private LeapProvider leapProvider;

    [Header("Hand Setup")]
    [SerializeField]
    [Tooltip("A parent GameObject containing both the left and right hand models.")]
    private GameObject handModelsRoot;

    [Header("Game Setup")]
    [SerializeField]
    [Tooltip("Use the game selection menu on startup")]
    private bool useGameMenu = true;

    [SerializeField]
    [Tooltip("The GameMenuManager prefab (shows game selection)")]
    private GameObject gameMenuManagerPrefab;

    [SerializeField]
    [Tooltip("The RhythmGameManager prefab (for practice piano)")]
    private GameObject rhythmGameManagerPrefab;

    [SerializeField]
    [Tooltip("The PianoGameManager prefab (for random practice mode)")]
    private GameObject pianoGameManagerPrefab;
    
    private string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

    void Start()
    {
        // Find the LeapProvider if it's not assigned
        if (leapProvider == null)
        {
            leapProvider = FindObjectOfType<LeapProvider>();
            if (leapProvider == null)
            {
                Debug.LogError("MainController cannot find a LeapProvider in the scene. The data logger will not function.");
                enabled = false;
                return;
            }
        }
        
        // Setup the colliders on the fingertips for interaction
        SetupFingertipColliders();

        // Start game menu or direct game
        if (useGameMenu)
        {
            // Create menu manager dynamically
            GameObject menuGO = new GameObject("GameMenuManager");
            GameMenuManager menu = menuGO.AddComponent<GameMenuManager>();

            // Pass prefab references
            menu.SetPrefabs(rhythmGameManagerPrefab, pianoGameManagerPrefab);

            Debug.Log("Game Menu started - select a game mode");
        }
        else
        {
            // Direct start without menu (legacy behavior)
            if (rhythmGameManagerPrefab != null)
            {
                Instantiate(rhythmGameManagerPrefab);
                Debug.Log("Started Practice Piano directly (no menu)");
            }
            else if (pianoGameManagerPrefab != null)
            {
                Instantiate(pianoGameManagerPrefab);
                Debug.Log("Started Random Practice directly (no menu)");
            }
            else
            {
                Debug.LogError("No game manager prefab assigned in MainController!");
            }
        }
    }

    void Update()
    {
        // Ensure the logger instance exists and is in logging mode
        if (HandDataLogger.Instance != null && leapProvider != null)
        {
            // Pass the current frame to the logger for continuous data recording
            HandDataLogger.Instance.LogHandFrame(leapProvider.CurrentFrame);
        }
    }

    private void SetupFingertipColliders()
    {
        if (handModelsRoot == null)
        {
            Debug.LogError("Hand Models Root is not assigned in MainController. Cannot setup fingertip colliders.");
            return;
        }

        // Search for fingertip bones by name in the hand model hierarchy
        // Common naming patterns for distal (fingertip) bones

        // Find all transforms in the hand hierarchy
        var allTransforms = handModelsRoot.GetComponentsInChildren<Transform>(true);

        foreach (var t in allTransforms)
        {
            string nameLower = t.name.ToLower();

            // Check if this is a distal bone (fingertip)
            if (!nameLower.Contains("distal") && !nameLower.Contains("_end")) continue; // Also check for common "_end" suffix

            // Determine which finger and hand this belongs to
            Chirality handedness = Chirality.Left;
            if (nameLower.Contains("right") || (t.root != null && t.root.name.ToLower().Contains("right")))
            {
                handedness = Chirality.Right;
            }
            else if (t.parent != null)
            {
                // Check parent hierarchy for hand indication
                Transform current = t.parent;
                while (current != null)
                {
                    if (current.name.ToLower().Contains("right"))
                    {
                        handedness = Chirality.Right;
                        break;
                    }
                    else if (current.name.ToLower().Contains("left"))
                    {
                        handedness = Chirality.Left;
                        break;
                    }
                    current = current.parent;
                }
            }

            // Determine finger index
            int fingerIndex = -1; // -1 for not found
            for (int i = 0; i < fingerNames.Length; i++)
            {
                if (nameLower.Contains(fingerNames[i].ToLower()))
                {
                    fingerIndex = i;
                    break;
                }
            }
            if (fingerIndex == -1) continue; // Skip if finger not identified


            // Skip if already has a collider
            if (t.GetComponent<SphereCollider>() != null) continue;

            // Add a sphere collider as trigger for piano key detection
            var collider = t.gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.015f;
            collider.isTrigger = true; // Must be trigger for OnTriggerEnter detection

            // Add a rigidbody - must be kinematic since hand tracking controls position
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true; // Must be kinematic for tracking-controlled objects

            // Add and configure our custom script
            var ftc = t.gameObject.AddComponent<FingertipCollider>();
            ftc.Handedness = handedness;
            ftc.FingerIndex = fingerIndex;

            Debug.Log($"Added FingertipCollider to: {handedness} {fingerNames[fingerIndex]} ({t.name})");
        }
    }
}
