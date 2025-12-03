using UnityEngine;

public class ButtonAnimator : MonoBehaviour
{
    private Animator animator;

    [Header("Animation Clip Names")]
    public string backAnimation;
    public string awayAnimation;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayBack()
    {
        animator.Play(backAnimation);
    }

    public void PlayAway()
    {
        animator.Play(awayAnimation);
    }
}
