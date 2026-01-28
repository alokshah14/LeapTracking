using UnityEngine;
using Leap;

public class Missile : MonoBehaviour
{
    [Header("Target")]
    public Chirality targetHand;
    public int targetFingerIndex; // 0-4

    [Header("Movement")]
    public float speed = 2.0f;
    public Vector3 targetPosition;

    [Header("Visuals")]
    public Renderer missileRenderer;
    public Material normalMaterial;
    public Material warningMaterial; // When close to target

    [Header("Audio")]
    public AudioClip explosionSound;
    public AudioClip shootSound;

    private bool isDestroyed = false;
    private float warningDistance = 2.0f; // Distance at which missile turns red

    // Event when missile is destroyed (correct press)
    public static event System.Action<Missile, bool> OnMissileDestroyed; // missile, wasCorrectPress

    // Event when missile reaches target (miss)
    public static event System.Action<Missile> OnMissileReachedTarget;

    void Start()
    {
        if (missileRenderer == null)
        {
            missileRenderer = GetComponent<Renderer>();
        }

        if (normalMaterial != null && missileRenderer != null)
        {
            missileRenderer.material = normalMaterial;
        }
    }

    void Update()
    {
        if (isDestroyed) return;

        // Move toward target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Check distance for warning visual
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance < warningDistance && warningMaterial != null && missileRenderer != null)
        {
            missileRenderer.material = warningMaterial;
        }

        // Check if reached target
        if (distance < 0.1f)
        {
            ReachedTarget();
        }
    }

    public void Initialize(Chirality hand, int fingerIndex, Vector3 startPos, Vector3 endPos, float missileSpeed)
    {
        targetHand = hand;
        targetFingerIndex = fingerIndex;
        transform.position = startPos;
        targetPosition = endPos;
        speed = missileSpeed;
    }

    public void Destroy(bool wasCorrectPress)
    {
        if (isDestroyed) return;

        isDestroyed = true;

        // Play explosion effect
        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, transform.position);
        }

        // Notify listeners
        OnMissileDestroyed?.Invoke(this, wasCorrectPress);

        // Visual feedback (particle effect could go here)
        // For now, just destroy
        Destroy(gameObject, 0.1f);
    }

    private void ReachedTarget()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        // Play hit sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // Notify listeners
        OnMissileReachedTarget?.Invoke(this);

        // Destroy missile
        Destroy(gameObject, 0.1f);
    }

    public string GetFingerName()
    {
        string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
        return fingerNames[targetFingerIndex];
    }
}
