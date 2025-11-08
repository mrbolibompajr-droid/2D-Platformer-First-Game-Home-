using UnityEngine;

public class DamageImmunity : MonoBehaviour
{
    private PlayerMovement movement;
    public bool immuneWhileDashing = true;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    public bool IsImmune()
    {
        if (immuneWhileDashing && movement.IsDashing())
            return true;

        return false;
    }
}
