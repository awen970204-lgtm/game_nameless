using UnityEngine;

public class Closure : MonoBehaviour
{
    // 點擊確認關閉對話框
    public void Click()
    {
        gameObject.SetActive(false);
    }
}
