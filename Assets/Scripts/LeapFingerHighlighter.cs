using UnityEngine;
using Leap;
using System;
using System.Reflection;

/// <summary>
/// Highlights a specific Leap Motion finger by making its capsules glow
/// Uses reflection to avoid direct Leap.Unity assembly dependency
/// </summary>
public class LeapFingerHighlighter : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightIntensity = 3.0f;

    private Component leapProvider;
    private Material highlightMaterial;
    private Chirality currentHighlightHand;
    private int currentHighlightFinger = -1;

    // Store original materials for unhighlighting
    private System.Collections.Generic.Dictionary<Renderer, Material> originalMaterials =
        new System.Collections.Generic.Dictionary<Renderer, Material>();

    void Start()
    {
        // Find LeapProvider using reflection
        Type leapProviderType = GetTypeByName("Leap.Unity.LeapProvider");
        if (leapProviderType != null)
        {
            leapProvider = FindObjectOfType(leapProviderType) as Component;
        }

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

        // Find all CapsuleHand components in scene using reflection
        Type capsuleHandType = GetTypeByName("Leap.Unity.CapsuleHand");
        if (capsuleHandType == null)
        {
            Debug.LogWarning("CapsuleHand type not found. Make sure Capsule Hands prefab is in scene.");
            return;
        }

        Component[] capsuleHands = FindObjectsOfType(capsuleHandType) as Component[];

        foreach (Component capsuleHand in capsuleHands)
        {
            // Get Handedness property using reflection
            PropertyInfo handednessProperty = capsuleHandType.GetProperty("Handedness");
            if (handednessProperty != null)
            {
                Chirality handedness = (Chirality)handednessProperty.GetValue(capsuleHand);

                if (handedness == hand)
                {
                    // Get the finger we want to highlight
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

                        Debug.Log($"Highlighted {fingerRenderers.Length} renderers for {hand} finger {fingerIndex}");
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
    private Transform GetFingerTransform(Component capsuleHand, int fingerIndex)
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

        // Get CurrentFrame using reflection
        PropertyInfo frameProperty = leapProvider.GetType().GetProperty("CurrentFrame");
        if (frameProperty == null) return Vector3.zero;

        Frame frame = frameProperty.GetValue(leapProvider) as Frame;
        if (frame == null) return Vector3.zero;

        foreach (Hand leapHand in frame.Hands)
        {
            if (leapHand.IsLeft && hand == Chirality.Left ||
                leapHand.IsRight && hand == Chirality.Right)
            {
                Finger finger = GetFingerFromHand(leapHand, fingerIndex);
                if (finger != null)
                {
                    // Convert Leap.Vector to Unity Vector3
                    return new Vector3(
                        finger.TipPosition.x,
                        finger.TipPosition.y,
                        finger.TipPosition.z
                    );
                }
            }
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Get a specific finger from a Leap Hand using properties
    /// </summary>
    private Finger GetFingerFromHand(Hand hand, int fingerIndex)
    {
        switch (fingerIndex)
        {
            case 0: return hand.Thumb;
            case 1: return hand.Index;
            case 2: return hand.Middle;
            case 3: return hand.Ring;
            case 4: return hand.Pinky;
            default: return null;
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

    void OnDestroy()
    {
        UnhighlightFinger();
    }
}
