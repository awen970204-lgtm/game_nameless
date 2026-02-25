using UnityEngine;

public class AutoDestroyByAnim : MonoBehaviour
{
    private void Start()
    {
        Animator anim = GetComponent<Animator>();
        if (anim == null) return;

        float length = anim.GetCurrentAnimatorStateInfo(0).length;
        Destroy(gameObject, length);
    }
}
