using UnityEngine;
using System.Collections.Generic;

public class DashHitbox : MonoBehaviour
{
    public int dashDamage = 20;
    public bool debug = true;

    private HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (alreadyHit.Contains(collision)) return;

        if (collision.TryGetComponent<IDamageable>(out var damageable))
        {
            // Check if collision has a BreakableWall and respects dash attack
            if (collision.TryGetComponent<BreakableWall>(out var wall))
            {
                if (!wall.requiresDashAttack)
                {
                    if (wall.debugBreak) Debug.Log($"{wall.name} ignored dash attack.");
                    return;
                }
            }

            damageable.TakeDamage(dashDamage);
            alreadyHit.Add(collision);

            if (debug) Debug.Log($"DashHitbox hit {collision.name} for {dashDamage} damage!");
        }
    }

    /// <summary>
    /// Clears the list of already-hit objects, should be called when dash starts
    /// </summary>
    public void ResetHits()
    {
        alreadyHit.Clear();
    }
}
