using UnityEngine;
using Leap.Unity;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Complete scene builder for Missile Defense game
/// Adds all required components: Leap Provider, Calibration, UI, etc.
/// </summary>
public class MissileDefenseSceneBuilder : MonoBehaviour
{
    [Header("Click 'Build Complete Scene' to add all components")]
    [SerializeField] private bool showInstructions = true;

    /// <summary>
    /// Builds the entire scene with all required components
    /// </summary>
    public void BuildCompleteScene()
    {
        Debug.Log("=== Building Missile Defense Scene ===");

        // 1. Add Leap Provider
        GameObject leapProvider = SetupLeapProvider();

        // 2. Add FingerIndividuationGame
        GameObject fingerGame = SetupFingerIndividuationGame(leapProvider);

        // 3. Add UI components
        SetupUI();

        // 4. Add HandDataLogger
        SetupHandDataLogger();

        // 5. Find or warn about MissileDefenseManager
        CheckMissileDefenseManager();

        Debug.Log("=== Scene Build Complete! ===");
        Debug.Log("Next Steps:");
        Debug.Log("1. Press Play to test calibration");
        Debug.Log("2. Make sure your Leap Motion is connected");
        Debug.Log("3. Follow on-screen calibration instructions");
    }

    GameObject SetupLeapProvider()
    {
        // Check if LeapServiceProvider already exists
        LeapServiceProvider existingProvider = FindObjectOfType<LeapServiceProvider>();
        if (existingProvider != null)
        {
            Debug.Log("✓ LeapServiceProvider already exists");
            return existingProvider.gameObject;
        }

        // Create new LeapServiceProvider
        GameObject providerObj = new GameObject("LeapServiceProvider");
        LeapServiceProvider provider = providerObj.AddComponent<LeapServiceProvider>();

        // Configure provider
        provider.interactionVolumeVisualization = false; // Disable visualization for cleaner scene

        Debug.Log("✓ Created LeapServiceProvider");
        return providerObj;
    }

    GameObject SetupFingerIndividuationGame(GameObject leapProviderObj)
    {
        // Check if already exists
        FingerIndividuationGame existingGame = FindObjectOfType<FingerIndividuationGame>();
        if (existingGame != null)
        {
            Debug.Log("✓ FingerIndividuationGame already exists");

            // Make sure it has the leap provider assigned
            if (existingGame.GetType().GetField("leapProvider",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
            {
                var field = existingGame.GetType().GetField("leapProvider",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field.GetValue(existingGame) == null && leapProviderObj != null)
                {
                    var leapProvider = leapProviderObj.GetComponent<LeapProvider>();
                    field.SetValue(existingGame, leapProvider);
                    Debug.Log("  → Assigned LeapProvider to FingerIndividuationGame");
                }
            }

            return existingGame.gameObject;
        }

        // Create new
        GameObject gameObj = new GameObject("FingerIndividuationGame");
        FingerIndividuationGame game = gameObj.AddComponent<FingerIndividuationGame>();

        // Assign leap provider via reflection (since it's a SerializeField)
        if (leapProviderObj != null)
        {
            var leapProvider = leapProviderObj.GetComponent<LeapProvider>();
            var field = game.GetType().GetField("leapProvider",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null && leapProvider != null)
            {
                field.SetValue(game, leapProvider);
                Debug.Log("✓ Created FingerIndividuationGame with LeapProvider");
            }
            else
            {
                Debug.LogWarning("⚠ Created FingerIndividuationGame but couldn't auto-assign LeapProvider - assign manually in Inspector");
            }
        }

        return gameObj;
    }

    void SetupUI()
    {
        // Check if GameUIManager exists
        GameUIManager existingUI = FindObjectOfType<GameUIManager>();
        if (existingUI != null)
        {
            Debug.Log("✓ GameUIManager already exists");
            return;
        }

        // Check if Canvas exists
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("  → Created Canvas");
        }

        // Check if EventSystem exists
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("  → Created EventSystem");
        }

        // Create GameUIManager
        GameObject uiManagerObj = new GameObject("GameUIManager");
        uiManagerObj.transform.SetParent(canvas.transform);
        GameUIManager uiManager = uiManagerObj.AddComponent<GameUIManager>();

        Debug.Log("✓ Created GameUIManager");
    }

    void SetupHandDataLogger()
    {
        // Check if already exists
        HandDataLogger existingLogger = FindObjectOfType<HandDataLogger>();
        if (existingLogger != null)
        {
            Debug.Log("✓ HandDataLogger already exists");
            return;
        }

        // Create new
        GameObject loggerObj = new GameObject("HandDataLogger");
        loggerObj.AddComponent<HandDataLogger>();

        Debug.Log("✓ Created HandDataLogger");
    }

    void CheckMissileDefenseManager()
    {
        MissileDefenseManager manager = FindObjectOfType<MissileDefenseManager>();
        if (manager == null)
        {
            Debug.LogWarning("⚠ MissileDefenseManager not found in scene!");
            Debug.LogWarning("  → Create an empty GameObject and add MissileDefenseManager component");
            Debug.LogWarning("  → Then assign finger targets and missile prefab in Inspector");
        }
        else
        {
            Debug.Log("✓ MissileDefenseManager exists");

            // Check if it has required references
            var fingerGameField = manager.GetType().GetField("fingerGame",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fingerGameField != null && fingerGameField.GetValue(manager) == null)
            {
                Debug.LogWarning("  → MissileDefenseManager needs FingerIndividuationGame reference");
                Debug.LogWarning("  → This will be auto-assigned when you press Play");
            }
        }
    }

    void OnValidate()
    {
        if (showInstructions)
        {
            Debug.Log("=== Missile Defense Scene Builder ===");
            Debug.Log("Click the 'Build Complete Scene' button in Inspector to:");
            Debug.Log("  • Add Leap Motion Provider");
            Debug.Log("  • Add Calibration System (FingerIndividuationGame)");
            Debug.Log("  • Add UI System (GameUIManager)");
            Debug.Log("  • Add Data Logger");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MissileDefenseSceneBuilder))]
public class MissileDefenseSceneBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MissileDefenseSceneBuilder builder = (MissileDefenseSceneBuilder)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will automatically add all required components:\n" +
            "• Leap Motion Provider (hand tracking)\n" +
            "• FingerIndividuationGame (calibration)\n" +
            "• GameUIManager (UI for calibration/game)\n" +
            "• HandDataLogger (data logging)\n" +
            "• Canvas & EventSystem (if needed)",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Build Complete Scene", GUILayout.Height(50)))
        {
            builder.BuildCompleteScene();
            EditorUtility.SetDirty(builder);

            // Mark scene as dirty so Unity knows to save
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "After building:\n" +
            "1. Select FingerIndividuationGame\n" +
            "2. Verify 'Leap Provider' field is assigned\n" +
            "3. Press Play to test calibration\n" +
            "4. Make sure Leap Motion device is connected",
            MessageType.Warning);
    }
}
#endif
