using UnityEngine;
using System;

public class PianoKey : MonoBehaviour
{
    [Header("Settings")]
    public string noteName = "C4";
    public int midiNote = 60;

    [Header("Visuals")]
    public Material normalMaterial;
    public Material highlightMaterial;
    public Material pressedMaterial;
    
    private Renderer keyRenderer;

    // Event to notify the game manager that this key was pressed
    // It passes the key itself and the Collider of the object that pressed it
    public static event Action<PianoKey, Collider> OnKeyPressed;

    void Awake()
    {
        InitializeRenderer();
    }

    public void InitializeRenderer()
    {
        if (keyRenderer == null)
        {
            keyRenderer = GetComponent<Renderer>();
            // Also check children if not found on this object
            if (keyRenderer == null)
            {
                keyRenderer = GetComponentInChildren<Renderer>();
            }
        }

        if (keyRenderer == null)
        {
            Debug.LogError($"PianoKey '{gameObject.name}': No Renderer found on this object or its children!");
        }
    }

    public enum KeyState
    {
        Normal,
        Highlighted,
        Pressed
    }

    public void SetState(KeyState state)
    {
        // Ensure renderer is initialized
        if (keyRenderer == null)
        {
            InitializeRenderer();
        }

        if (keyRenderer == null)
        {
            Debug.LogError($"PianoKey '{gameObject.name}': Cannot set state - no Renderer!");
            return;
        }

        switch (state)
        {
            case KeyState.Normal:
                if (normalMaterial != null)
                    keyRenderer.material = normalMaterial;
                else
                    Debug.LogWarning($"PianoKey '{gameObject.name}': normalMaterial is null");
                break;
            case KeyState.Highlighted:
                if (highlightMaterial != null)
                    keyRenderer.material = highlightMaterial;
                else
                    Debug.LogWarning($"PianoKey '{gameObject.name}': highlightMaterial is null");
                // Only log highlights, not normal resets
                Debug.Log($"PianoKey '{gameObject.name}': SetState(Highlighted)");
                break;
            case KeyState.Pressed:
                if (pressedMaterial != null)
                    keyRenderer.material = pressedMaterial;
                else
                    Debug.LogWarning($"PianoKey '{gameObject.name}': pressedMaterial is null");
                Debug.Log($"PianoKey '{gameObject.name}': SetState(Pressed)");
                break;
        }
    }

    public void Press()
    {
        SetState(KeyState.Pressed);
        Debug.Log($"Invoking OnKeyPressed for '{this.noteName}'.");
        OnKeyPressed?.Invoke(this, null); // We pass null for the collider because the press is not from a direct collision
        Debug.Log($"'{this.noteName}' was programmatically pressed.");
    }

    public void Release()
    {
        SetState(KeyState.Normal);
        Debug.Log($"'{this.noteName}' was released.");
    }

    // Disabled - using gesture-based detection instead of collision-based
    // private void OnTriggerEnter(Collider other)
    // {
    //     Debug.Log($"'{this.noteName}' OnTriggerEnter detected by '{other.name}'");
    //     Press();
    // }
}
