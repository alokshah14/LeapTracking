using UnityEngine;
using Leap;
using System.Collections.Generic;

public class GlowFingerManager : MonoBehaviour
{
    // Singleton instance for easy access from other scripts
    public static GlowFingerManager Instance { get; private set; }

    [Header("Glow Object")]
    [Tooltip("A prefab to instantiate as the highlight object (e.g., a semi-transparent sphere).")]
    [SerializeField] private GameObject glowPrefab;

    [Header("Left Hand Fingertip Transforms")]
    [SerializeField] private Transform leftThumbTip;
    [SerializeField] private Transform leftIndexTip;
    [SerializeField] private Transform leftMiddleTip;
    [SerializeField] private Transform leftRingTip;
    [SerializeField] private Transform leftPinkyTip;

    [Header("Right Hand Fingertip Transforms")]
    [SerializeField] private Transform rightThumbTip;
    [SerializeField] private Transform rightIndexTip;
    [SerializeField] private Transform rightMiddleTip;
    [SerializeField] private Transform rightRingTip;
    [SerializeField] private Transform rightPinkyTip;
    
    private List<GameObject> allGlowObjects = new List<GameObject>();
    private Dictionary<Chirality, Dictionary<int, GameObject>> glowObjectMap;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (glowPrefab == null)
        {
            Debug.LogError("GlowFingerManager: Glow Prefab is not assigned!");
            return;
        }

        InitializeGlowObjects();
    }

    private void InitializeGlowObjects()
    {
        glowObjectMap = new Dictionary<Chirality, Dictionary<int, GameObject>>
        {
            [Chirality.Left] = new Dictionary<int, GameObject>(),
            [Chirality.Right] = new Dictionary<int, GameObject>()
        };

        var leftFingers = new List<Transform> { leftThumbTip, leftIndexTip, leftMiddleTip, leftRingTip, leftPinkyTip };
        var rightFingers = new List<Transform> { rightThumbTip, rightIndexTip, rightMiddleTip, rightRingTip, rightPinkyTip };

        for (int i = 0; i < leftFingers.Count; i++)
        {
            glowObjectMap[Chirality.Left][i] = CreateGlowObject(leftFingers[i]);
        }
        for (int i = 0; i < rightFingers.Count; i++)
        {
            glowObjectMap[Chirality.Right][i] = CreateGlowObject(rightFingers[i]);
        }
    }

    private GameObject CreateGlowObject(Transform parent)
    {
        if (parent == null) 
        {
            Debug.LogWarning("A fingertip transform is not assigned in GlowFingerManager. The glow object for it will not be created.");
            return null;
        }
        
        GameObject glowInstance = Instantiate(glowPrefab, parent);
        glowInstance.transform.localPosition = Vector3.zero;
        glowInstance.SetActive(false);
        allGlowObjects.Add(glowInstance);
        return glowInstance;
    }

    public void HighlightFinger(Chirality hand, int fingerIndex)
    {
        ResetHighlights();

        if (glowObjectMap.ContainsKey(hand) && glowObjectMap[hand].ContainsKey(fingerIndex))
        {
            GameObject glowObject = glowObjectMap[hand][fingerIndex];
            if (glowObject != null)
            {
                glowObject.SetActive(true);
            }
        }
    }

    public void ResetHighlights()
    {
        foreach (var glowObject in allGlowObjects)
        {
            if (glowObject != null)
            {
                glowObject.SetActive(false);
            }
        }
    }
}