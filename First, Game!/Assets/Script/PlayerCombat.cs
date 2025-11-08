using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Keybinds")]
    public PlayerKeybinds keybinds;

    [Header("Attack Hitbox (Child in front)")]
    public Hitbox attackHitbox;
    public int normalAttackDamage = 20;
    public int heavyAttackDamage = 40;
    public float attackDuration = 0.15f;

    [Header("Attack Cooldowns")]
    public float normalAttackCooldown = 0.5f;
    public float heavyAttackCooldown = 1f;
    public float dashAttackCooldown = 0.3f;

    [Header("Attack Locks (prevents other attacks)")]
    public float normalAttackLock = 0.2f;
    public float heavyAttackLock = 0.5f;
    public float dashAttackLock = 0.3f;

    [Header("Dash Attack Collider (On Player)")]
    public GameObject dashHitboxObject;
    public int dashAttackDamage = 20;
    public float dashAttackDuration = 0.15f;
    public bool dashAttackAutomatic = true;

    [Header("Debug Options")]
    public bool debugNormalAttack = true;
    public bool debugHeavyAttack = true;
    public bool debugDashAttack = true;

    private Health health;
    private PlayerMovement movement;

    // Cooldown tracking
    private float lastNormalAttackTime = -10f;
    private float lastHeavyAttackTime = -10f;
    private float lastDashAttackTime = -10f;

    // Attack lock tracking
    private float lastAttackTime = -10f;
    private float currentAttackLock = 0f;

    private void Awake()
    {
        health = GetComponent<Health>();
        movement = GetComponent<PlayerMovement>();

        if (dashHitboxObject != null) dashHitboxObject.SetActive(false);
        if (attackHitbox != null) attackHitbox.gameObject.SetActive(false);
    }

    private void Update()
    {
        // No need to call attacks here if called from PlayerMovement
    }

    #region Normal & Heavy Attacks
    private void HandleNormalAttack()
    {
        if (Input.GetKeyDown(keybinds.attack) &&
            Time.time >= lastNormalAttackTime + normalAttackCooldown &&
            Time.time >= lastAttackTime + currentAttackLock)
        {
            lastNormalAttackTime = Time.time;
            lastAttackTime = Time.time;
            currentAttackLock = normalAttackLock;

            StartCoroutine(AttackWindow(normalAttackDamage, "Normal Attack", debugNormalAttack));
        }
    }

    private void HandleHeavyAttack()
    {
        if (Input.GetKeyDown(keybinds.heavyAttack) &&
            Time.time >= lastHeavyAttackTime + heavyAttackCooldown &&
            Time.time >= lastAttackTime + currentAttackLock)
        {
            lastHeavyAttackTime = Time.time;
            lastAttackTime = Time.time;
            currentAttackLock = heavyAttackLock;

            StartCoroutine(AttackWindow(heavyAttackDamage, "Heavy Attack", debugHeavyAttack));
        }
    }

    private IEnumerator AttackWindow(int damage, string attackName, bool debug)
    {
        if (health != null) health.isAttacking = true;

        if (attackHitbox != null)
        {
            attackHitbox.DoNormalAttack(damage);
            if (debug) Debug.Log($"{attackName} executed with {damage} damage.");
        }

        yield return new WaitForSeconds(attackDuration);

        if (health != null) health.isAttacking = false;
    }
    #endregion

    #region Dash Attack
    private void HandleDashAttack()
    {
        if (movement == null || !movement.IsDashing()) return;

        if (Time.time < lastDashAttackTime + dashAttackCooldown ||
            Time.time < lastAttackTime + currentAttackLock) return;

        if (!(dashAttackAutomatic || Input.GetKeyDown(keybinds.dashAttack))) return;

        lastDashAttackTime = Time.time;
        lastAttackTime = Time.time;
        currentAttackLock = dashAttackLock;

        ActivateDashHitbox();
    }

    private void ActivateDashHitbox()
    {
        if (dashHitboxObject == null) return;

        DashHitbox dash = dashHitboxObject.GetComponent<DashHitbox>();
        dash.ResetHits();
        dashHitboxObject.SetActive(true);

        if (debugDashAttack) Debug.Log("Dash attack started");
        StartCoroutine(DisableDashHitboxAfterTime(dashAttackDuration));
    }

    private IEnumerator DisableDashHitboxAfterTime(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (dashHitboxObject != null) dashHitboxObject.SetActive(false);
        if (debugDashAttack) Debug.Log("Dash attack ended");
    }
    #endregion

    #region PUBLIC WRAPPERS
    public void HandleNormalAttackInput() => HandleNormalAttack();
    public void HandleHeavyAttackInput() => HandleHeavyAttack();
    public void HandleDashAttackInput() => HandleDashAttack();
    #endregion
}
