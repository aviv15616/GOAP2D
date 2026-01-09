using UnityEngine;

public class SetScale : StateMachineBehaviour
{
    public Vector3 scale = Vector3.one;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.transform.localScale = scale;
    }
}
