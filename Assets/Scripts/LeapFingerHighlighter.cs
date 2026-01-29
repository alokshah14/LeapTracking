using UnityEngine;
using Leap;
using Leap.Unity;

/// <summary>
/// Highlights a specific Leap Motion finger by making its capsules glow
/// </summary>
public class LeapFingerHighlighter : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightIntensity = 3.0f;

    private LeapProvider leapProvider;
    private Material highlightMaterial;
    private Chirality currentHighlightHand;
    private int currentHighlightFinger = -1;

    // Store original materials for unhighlighting
    private System.Collections.Generic.Dictionary<Renderer, Material> originalMaterials =
        new System.Collections.Generic.Dictionary<Renderer, Material>();

    void Start()
    {
        leapProvider = FindObjectOfType<LeapProvider>();

        // Create highlight material
        highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.color = highlightColor;
        highlightMaterial.SetColor("_EmissionColor", highlightColor * highlightIntensity);
        highlightMaterial.EnableKeyword("_EMISSION");
    }

    /// <summary>
    /// Highlight a specific finger on the Leap Motion hand visualization
    /// </summary>
    public void HighlightFinger(Chirality hand, int fingerIndex)
    {
        // Unhighlight previous finger
        UnhighlightFinger();

        currentHighlightHand = hand;
        currentHighlightFinger = fingerIndex;

        // Find all hand renderers in scene
        CapsuleHand[] capsuleHands = FindObjectsOfType<CapsuleHand>();

        foreach (CapsuleHand capsuleHand in capsuleHands)
        {
            if (capsuleHand.Handedness == hand)
            {
                // Get the finger we want to highlight
                // CapsuleHand has fingers array indexed like: 0=Thumb, 1=Index, etc.
                Transform fingerTransform = GetFingerTransform(capsuleHand, fingerIndex);

                if (fingerTransform != null)
                {
                    // Highlight all renderers in this finger
                    Renderer[] fingerRenderers = fingerTransform.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in fingerRenderers)
                    {
                        if (!originalMaterials.ContainsKey(renderer))
                        {
                            originalMaterials[renderer] = renderer.material;
                        }
                        renderer.material = highlightMaterial;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Remove highlighting from current finger
    /// </summary>
    public void UnhighlightFinger()
    {
        // Restore original materials
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.material = kvp.Value;
            }
        }
        originalMaterials.Clear();

        currentHighlightFinger = -1;
    }

    /// <summary>
    /// Get the transform of a specific finger from CapsuleHand
    /// </summary>
    private Transform GetFingerTransform(CapsuleHand capsuleHand, int fingerIndex)
    {
        // CapsuleHand has child transforms for each finger
        // Look for transforms with finger names
        string[] fingerNames = { "thumb", "index", "middle", "ring", "pinky" };

        if (fingerIndex < 0 || fingerIndex >= fingerNames.Length)
            return null;

        string searchName = fingerNames[fingerIndex];

        // Search through all children
        Transform[] children = capsuleHand.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.name.ToLower().Contains(searchName))
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>
    /// Get the current position of a finger tip in world space
    /// </summary>
    public Vector3 GetFingerTipPosition(Chirality hand, int fingerIndex)
    {
        if (leapProvider == null) return Vector3.zero;

        Frame frame = leapProvider.CurrentFrame;
        if (frame == null) return Vector3.zero;

        foreach (Hand leapHand in frame.Hands)
        {
            if (leapHand.IsLeft && hand == Chirality.Left ||
                leapHand.IsRight && hand == Chirality.Right)
            {
                Finger finger = GetFinger(leapHand, fingerIndex);
                if (finger != null)
                {
                    return finger.TipPosition.ToVector3();
                }
            }
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Get a specific finger from a Leap Hand
    /// </summary>
    private Finger GetFinger(Hand hand, int fingerIndex)
    {
        switch (fingerIndex)
        {
            case 0: return hand.Fingers[0]; // Thumb
            case 1: return hand.Fingers[1]; // Index
            case 2: return hand.Fingers[2]; // Middle
            case 3: return hand.Fingers[3]; // Ring
            case 4: return hand.Fingers[4]; // Pinky
            default: return null;
        }
    }

    void OnDestroy()
    {
        UnhighlightFinger();
    }
}
