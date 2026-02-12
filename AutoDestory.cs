using UnityEngine;

public class AutoDestory : StateMachineBehaviour
{
    override public void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        Destroy(animator.gameObject);
    }
}
