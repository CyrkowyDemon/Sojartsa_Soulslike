using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    public Animator animator;
    public string property = "property";
    public void AnimateBool(bool value)
    {
        animator.SetBool(property, value);
    }
    public void AnimateTrigger()
    {
        animator.SetTrigger(property);
    }
    public void AnimateFloat(float value)
    {
        animator.SetFloat(property, value);
    }

}
