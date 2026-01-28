using UnityEngine;
using Leap;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script to automatically set up the Missile Defense scene
/// Attach to an empty GameObject and click "Setup Scene" in Inspector
/// </summary>
public class MissileDefenseSetup : MonoBehaviour
{
    [Header("Setup Configuration")]
    [SerializeField] private bool autoSetup = false;

    [Header("Finger Target Spacing")]
    [SerializeField] private float fingerSpacing = 0.5f; // Distance between finger targets
    [SerializeField] private float handSeparation = 2.0f; // Distance between left and right hands
    [SerializeField] private float targetHeight = -2.0f; // Y position where missiles hit

    [Header("Generated References (Auto-filled)")]
    public Transform leftHandParent;
    public Transform rightHandParent;
    public Transform[] leftFingerTargets = new Transform[5];
    public Transform[] rightFingerTargets = new Transform[5];
    public Transform missileSpawnParent;

    private string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

    void Start()
    {
        if (autoSetup)
        {
            SetupScene();
        }
    }

    /// <summary>
    /// Creates all finger target positions in the scene
    /// Call this from Inspector button or code
    /// </summary>
    public void SetupScene()
    {
        Debug.Log("Setting up Missile Defense scene...");

        // Create parent objects
        CreateParentObjects();

        // Create left hand finger targets
        CreateFingerTargets(Chirality.Left);

        // Create right hand finger targets
        CreateFingerTargets(Chirality.Right);

        // Create missile spawn parent
        CreateMissileSpawnParent();

        Debug.Log("Scene setup complete! Assign these to MissileDefenseManager.");
    }

    void CreateParentObjects()
    {
        // Left hand parent
        if (leftHandParent == null)
        {
            GameObject leftParent = new GameObject("LeftHandTargets");
            leftParent.transform.position = new Vector3(-handSeparation / 2, targetHeight, 0);
            leftHandParent = leftParent.transform;
        }

        // Right hand parent
        if (rightHandParent == null)
        {
            GameObject rightParent = new GameObject("RightHandTargets");
            rightParent.transform.position = new Vector3(handSeparation / 2, targetHeight, 0);
            rightHandParent = rightParent.transform;
        }
    }

    void CreateFingerTargets(Chirality hand)
    {
        Transform parent = (hand == Chirality.Left) ? leftHandParent : rightHandParent;
        Transform[] targetArray = (hand == Chirality.Left) ? leftFingerTargets : rightFingerTargets;

        // Create 5 finger target positions (Thumb to Pinky)
        for (int i = 0; i < 5; i++)
        {
            if (targetArray[i] == null)
            {
                GameObject target = new GameObject($"{hand}_{fingerNames[i]}_Target");
                target.transform.SetParent(parent);

                // Position in a line (thumb on inside, pinky on outside)
                float xOffset = hand == Chirality.Left
                    ? (i - 2) * fingerSpacing  // Left: -2, -1, 0, 1, 2
                    : -(i - 2) * fingerSpacing; // Right: mirrored

                target.transform.localPosition = new Vector3(xOffset, 0, 0);

                // Add a visual marker (sphere)
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.transform.SetParent(target.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = Vector3.one * 0.2f;
                marker.name = "VisualMarker";

                // Color code the markers
                Renderer markerRenderer = marker.GetComponent<Renderer>();
                if (markerRenderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = GetFingerColor(i);
                    markerRenderer.material = mat;
                }

                // Remove collider (don't need collision)
                Collider markerCollider = marker.GetComponent<Collider>();
                if (markerCollider != null)
                {
                    DestroyImmediate(markerCollider);
                }

                targetArray[i] = target.transform;

                Debug.Log($"Created target: {target.name} at {target.transform.position}");
            }
        }

        // Store back in the arrays
        if (hand == Chirality.Left)
            leftFingerTargets = targetArray;
        else
            rightFingerTargets = targetArray;
    }

    void CreateMissileSpawnParent()
    {
        if (missileSpawnParent == null)
        {
            GameObject spawnParent = new GameObject("MissileSpawnParent");
            spawnParent.transform.position = Vector3.zero;
            missileSpawnParent = spawnParent.transform;
        }
    }

    Color GetFingerColor(int fingerIndex)
    {
        // Color code: Thumb=Red, Index=Orange, Middle=Yellow, Ring=Green, Pinky=Blue
        Color[] colors = {
            Color.red,      // Thumb
            new Color(1f, 0.5f, 0f),  // Orange - Index
            Color.yellow,   // Middle
            Color.green,    // Ring
            Color.cyan      // Pinky
        };
        return colors[fingerIndex];
    }

    /// <summary>
    /// Assigns the created targets to a MissileDefenseManager
    /// </summary>
    public void AssignToManager(MissileDefenseManager manager)
    {
        if (manager == null)
        {
            Debug.LogError("MissileDefenseManager is null!");
            return;
        }

        // This would need to use reflection or SerializedObject in the editor
        Debug.Log("Targets created! Manually assign them to MissileDefenseManager in Inspector.");
        Debug.Log("Left targets: " + string.Join(", ", System.Array.ConvertAll(leftFingerTargets, t => t ? t.name : "null")));
        Debug.Log("Right targets: " + string.Join(", ", System.Array.ConvertAll(rightFingerTargets, t => t ? t.name : "null")));
    }

    void OnDrawGizmos()
    {
        // Draw lines between finger targets
        if (leftFingerTargets != null && leftFingerTargets.Length == 5)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < 4; i++)
            {
                if (leftFingerTargets[i] != null && leftFingerTargets[i + 1] != null)
                {
                    Gizmos.DrawLine(leftFingerTargets[i].position, leftFingerTargets[i + 1].position);
                }
            }
        }

        if (rightFingerTargets != null && rightFingerTargets.Length == 5)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < 4; i++)
            {
                if (rightFingerTargets[i] != null && rightFingerTargets[i + 1] != null)
                {
                    Gizmos.DrawLine(rightFingerTargets[i].position, rightFingerTargets[i + 1].position);
                }
            }
        }

        // Draw spawn area
        if (missileSpawnParent != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(missileSpawnParent.position + Vector3.up * 5f, 0.5f);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MissileDefenseSetup))]
public class MissileDefenseSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MissileDefenseSetup setup = (MissileDefenseSetup)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click 'Setup Scene' to automatically create all finger target positions", MessageType.Info);

        if (GUILayout.Button("Setup Scene", GUILayout.Height(40)))
        {
            setup.SetupScene();
            EditorUtility.SetDirty(setup);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("After setup, drag the finger targets from the Hierarchy to MissileDefenseManager", MessageType.Info);
    }
}
#endif
