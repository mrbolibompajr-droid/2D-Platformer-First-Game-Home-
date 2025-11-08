using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool destroyOnDeath = true;

    [Header("Attack Clash Settings")]
    public bool canClashAttacks = false;
    public bool isAttacking = false;

    [Header("Clash Knockback")]
    public bool enableClashKnockback = false;
    public float clashKnockbackForce = 8f;
    public float clashVerticalBoost = 1f;

    [Header("Debug Options")]
    public bool debugDamageTaken = true;
    public bool debugClash = true;
    public bool debugKnockback = true;
    public bool debugDeath = true;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        // --- Clash check ---
        if (canClashAttacks && isAttacking)
        {
            if (debugClash) Debug.Log($"{gameObject.name} clash! Attack canceled.");
            if (enableClashKnockback) ApplyClashKnockback();
            return;
        }

        // --- Damage immunity check ---
        if (TryGetComponent(out DamageImmunity immunity))
        {
            if (immunity.IsImmune())
            {
                if (debugDamageTaken) Debug.Log($"{gameObject.name} is immune to damage.");
                return;
            }
        }

        // --- Apply damage ---
        currentHealth -= damage;
        if (debugDamageTaken) Debug.Log($"{gameObject.name} took {damage} damage. Current health: {currentHealth}");

        if (currentHealth <= 0) Die();
    }

    private void ApplyClashKnockback()
    {
        if (!TryGetComponent(out Rigidbody2D rb)) return;

        Transform attacker = DamageContext.LastAttacker;
        if (attacker == null) return;

        float direction = (transform.position.x < attacker.position.x) ? -1f : 1f;
        Vector2 force = new Vector2(direction * clashKnockbackForce, clashVerticalBoost);

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);

        if (debugKnockback) Debug.Log($"{gameObject.name} received clash knockback from {attacker.name}");
    }

    private void Die()
    {
        if (debugDeath) Debug.Log($"{gameObject.name} died!");
        if (destroyOnDeath) Destroy(gameObject);
    }
}
