using UnityEngine;
using System.Collections;

public class Hitbox : MonoBehaviour
{
    public bool debug = true;

    /// <summary>
    /// Activates the hitbox for an attack
    /// </summary>
    /// <param name="damageAmount">Damage of the attack</param>
    public void DoNormalAttack(int damageAmount)
    {
        gameObject.SetActive(true);
        StartCoroutine(DisableAfterTime(0.15f, damageAmount));
    }

    private IEnumerator DisableAfterTime(float duration, int damageAmount)
    {
        yield return null; // wait 1 frame to ensure OnTriggerEnter fires immediately
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IDamageable>(out var damageable))
        {
            // Check if collision has a BreakableWall and respects attack type
            if (collision.TryGetComponent<BreakableWall>(out var wall))
            {
                // Decide if the wall can be damaged by this attack
                if ((damageable is Health && wall.requiresNormalAttack && wall.debugBreak) ||
                    (damageable is Health && wall.requiresHeavyAttack && wall.debugBreak))
                {
                    if (debug) Debug.Log($"{wall.name} ignored attack due to type mismatch.");
                    return;
                }
            }

            damageable.TakeDamage(0); // actual damage is applied in PlayerCombat via DoNormalAttack
            if (debug) Debug.Log($"Hitbox hit {collision.name}!");
        }
    }
}
