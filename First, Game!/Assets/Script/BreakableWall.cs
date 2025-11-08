using UnityEngine;

[RequireComponent(typeof(Health))]
public class BreakableWall : MonoBehaviour, IDamageable
{
    [Header("Break Conditions")]
    public bool requiresNormalAttack = false;
    public bool requiresHeavyAttack = false;
    public bool requiresDashAttack = false;

    [Header("Debug Options")]
    public bool debugBreak = true;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        // Walls don’t attack
        health.isAttacking = false;
        health.canClashAttacks = false;
        health.enableClashKnockback = false;
    }

    public void TakeDamage(int damage)
    {
        if (HitValidation(damage))
        {
            health.TakeDamage(damage);
            if (debugBreak) Debug.Log($"{gameObject.name} took {damage} damage! Current HP: {health.currentHealth}");
        }
        else if (debugBreak)
        {
            string reason = "";
            if (requiresDashAttack && !PlayerIsDashing()) reason = "requires dash attack, player not dashing";
            else if (requiresNormalAttack && damage != 20) reason = "requires normal attack, wrong damage";
            else if (requiresHeavyAttack && damage < 40) reason = "requires heavy attack, damage too low";
            else reason = "attack type not allowed";
            Debug.Log($"{gameObject.name} ignored attack. Reason: {reason}");
        }
    }

    private bool HitValidation(int damage)
    {
        // No requirements → allow any attack
        if (!requiresNormalAttack && !requiresHeavyAttack && !requiresDashAttack)
            return true;

        if (requiresNormalAttack)
            return damage == 20;

        if (requiresHeavyAttack)
            return damage >= 40;

        if (requiresDashAttack)
            return damage == 20 && PlayerIsDashing();

        return false;
    }

    private bool PlayerIsDashing()
    {
        if (DamageContext.LastAttacker != null &&
            DamageContext.LastAttacker.TryGetComponent<PlayerMovement>(out var movement))
        {
            return movement.IsDashing();
        }
        return false;
    }
}
