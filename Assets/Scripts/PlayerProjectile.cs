using UnityEngine;
using Leap;

/// <summary>
/// Projectile shot from player's finger toward falling missile
/// </summary>
public class PlayerProjectile : MonoBehaviour
{
    public Chirality sourceHand;
    public int sourceFingerIndex;
    public float speed = 5.0f;
    public Missile targetMissile;

    private Vector3 direction;
    private bool hasHit = false;

    public static event System.Action<PlayerProjectile, Missile> OnProjectileHit;

    public void Initialize(Chirality hand, int fingerIndex, Vector3 startPos, Vector3 targetPos, Missile target, float projectileSpeed)
    {
        sourceHand = hand;
        sourceFingerIndex = fingerIndex;
        transform.position = startPos;
        targetMissile = target;
        speed = projectileSpeed;

        // Calculate direction toward target
        direction = (targetPos - startPos).normalized;

        // Rotate to point in direction of travel
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void Update()
    {
        if (hasHit) return;

        // Move upward toward target
        transform.position += direction * speed * Time.deltaTime;

        // Check for collision with target missile
        if (targetMissile != null)
        {
            float distance = Vector3.Distance(transform.position, targetMissile.transform.position);
            if (distance < 0.5f)  // Hit detection radius
            {
                HitMissile();
            }
        }

        // Destroy if went too far (missed)
        if (transform.position.y > 10f)
        {
            Destroy(gameObject);
        }
    }

    void HitMissile()
    {
        if (hasHit) return;
        hasHit = true;

        // Notify that we hit
        OnProjectileHit?.Invoke(this, targetMissile);

        // Visual effect (optional - could add particle explosion here)

        // Destroy projectile
        Destroy(gameObject, 0.1f);
    }

    void OnDrawGizmos()
    {
        // Draw debug sphere to see projectile path
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
