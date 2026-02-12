using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float floatSpeed = 20f;
    public float duration = 1f;
    private TMP_Text tmp;
    private Color originalColor;
    private float timer;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        if (tmp != null)
            originalColor = tmp.color;
    }

    void Update()
    {
        // 往上飄
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // 淡出
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / duration);
        if (tmp != null)
            tmp.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        if (timer >= duration)
            Destroy(gameObject);
    }
}
