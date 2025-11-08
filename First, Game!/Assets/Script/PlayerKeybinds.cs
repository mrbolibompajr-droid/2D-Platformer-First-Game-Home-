using UnityEngine;

[CreateAssetMenu(fileName = "PlayerKeybinds", menuName = "Config/PlayerKeybinds")]
public class PlayerKeybinds : ScriptableObject
{
    [Header("Horizontal Movement")]
    public KeyCode moveLeft = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;

    [Header("Vertical Movement")]
    public KeyCode jump = KeyCode.Space;
    public KeyCode doubleJump = KeyCode.W;

    [Header("Wall Movement")]
    public KeyCode wallGrab = KeyCode.LeftControl;
    public KeyCode wallJump = KeyCode.Space;

    [Header("Dash")]
    public KeyCode dash = KeyCode.LeftShift;
    public KeyCode dashAttack = KeyCode.K;

    [Header("Combat")]
    public KeyCode attack = KeyCode.J;
    public KeyCode heavyAttack = KeyCode.L;

    [Header("Other Actions")]
    public KeyCode interact = KeyCode.E;
}
