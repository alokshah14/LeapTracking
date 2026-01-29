using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Complete scene builder for Missile Defense game
/// Adds all required components: Leap Provider, Calibration, UI, etc.
/// Uses reflection to avoid direct Leap.Unity assembly dependency
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
        // Use reflection to find LeapServiceProvider type
        Type leapServiceProviderType = GetTypeByName("Leap.Unity.LeapServiceProvider");
        if (leapServiceProviderType == null)
        {
            Debug.LogError("❌ Could not find Leap.Unity.LeapServiceProvider type. Is Leap Motion SDK installed?");
            return null;
        }

        // Check if LeapServiceProvider already exists
        Component existingProvider = FindObjectOfType(leapServiceProviderType) as Component;
        if (existingProvider != null)
        {
            Debug.Log("✓ LeapServiceProvider already exists");
            return existingProvider.gameObject;
        }

        // Create new LeapServiceProvider
        GameObject providerObj = new GameObject("LeapServiceProvider");
        Component provider = providerObj.AddComponent(leapServiceProviderType);

        // Configure provider - disable visualization
        PropertyInfo vizProp = leapServiceProviderType.GetProperty("interactionVolumeVisualization");
        if (vizProp != null)
        {
            vizProp.SetValue(provider, false);
        }

        Debug.Log("✓ Created LeapServiceProvider");
        return providerObj;
    }

    GameObject SetupFingerIndividuationGame(GameObject leapProviderObj)
    {
        // Find FingerIndividuationGame type
        Type fingerGameType = GetTypeByName("FingerIndividuationGame");
        if (fingerGameType == null)
        {
            Debug.LogError("❌ Could not find FingerIndividuationGame type.");
            return null;
        }

        // Check if already exists
        Component existingGame = FindObjectOfType(fingerGameType) as Component;
        if (existingGame != null)
        {
            Debug.Log("✓ FingerIndividuationGame already exists");

            // Make sure it has the leap provider assigned
            FieldInfo field = existingGame.GetType().GetField("leapProvider",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null && field.GetValue(existingGame) == null && leapProviderObj != null)
            {
                Type leapProviderType = GetTypeByName("Leap.Unity.LeapProvider");
                if (leapProviderType != null)
                {
                    Component leapProvider = leapProviderObj.GetComponent(leapProviderType);
                    field.SetValue(existingGame, leapProvider);
                    Debug.Log("  → Assigned LeapProvider to FingerIndividuationGame");
                }
            }

            return existingGame.gameObject;
        }

        // Create new
        GameObject gameObj = new GameObject("FingerIndividuationGame");
        Component game = gameObj.AddComponent(fingerGameType);

        // Assign leap provider via reflection (since it's a SerializeField)
        if (leapProviderObj != null)
        {
            Type leapProviderType = GetTypeByName("Leap.Unity.LeapProvider");
            if (leapProviderType != null)
            {
                Component leapProvider = leapProviderObj.GetComponent(leapProviderType);
                FieldInfo field = game.GetType().GetField("leapProvider",
                    BindingFlags.NonPublic | BindingFlags.Instance);

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
        }

        return gameObj;
    }

    void SetupUI()
    {
        // Find GameUIManager type
        Type gameUIManagerType = GetTypeByName("GameUIManager");
        if (gameUIManagerType == null)
        {
            Debug.LogError("❌ Could not find GameUIManager type.");
            return;
        }

        // Check if GameUIManager exists
        Component existingUI = FindObjectOfType(gameUIManagerType) as Component;
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
        uiManagerObj.AddComponent(gameUIManagerType);

        Debug.Log("✓ Created GameUIManager");
    }

    void SetupHandDataLogger()
    {
        // Find HandDataLogger type
        Type handDataLoggerType = GetTypeByName("HandDataLogger");
        if (handDataLoggerType == null)
        {
            Debug.LogError("❌ Could not find HandDataLogger type.");
            return;
        }

        // Check if already exists
        Component existingLogger = FindObjectOfType(handDataLoggerType) as Component;
        if (existingLogger != null)
        {
            Debug.Log("✓ HandDataLogger already exists");
            return;
        }

        // Create new
        GameObject loggerObj = new GameObject("HandDataLogger");
        loggerObj.AddComponent(handDataLoggerType);

        Debug.Log("✓ Created HandDataLogger");
    }

    void CheckMissileDefenseManager()
    {
        // Find MissileDefenseManager type
        Type managerType = GetTypeByName("MissileDefenseManager");
        if (managerType == null)
        {
            Debug.LogWarning("⚠ Could not find MissileDefenseManager type.");
            return;
        }

        Component manager = FindObjectOfType(managerType) as Component;
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
            FieldInfo fingerGameField = manager.GetType().GetField("fingerGame",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (fingerGameField != null && fingerGameField.GetValue(manager) == null)
            {
                Debug.LogWarning("  → MissileDefenseManager needs FingerIndividuationGame reference");
                Debug.LogWarning("  → This will be auto-assigned when you press Play");
            }
        }
    }

    /// <summary>
    /// Helper method to find a type by name across all assemblies
    /// </summary>
    private Type GetTypeByName(string typeName)
    {
        Type type = Type.GetType(typeName);
        if (type != null) return type;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null) return type;
        }

        return null;
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
