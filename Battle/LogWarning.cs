using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LogWarning : MonoBehaviour
{
    public static LogWarning Instance;

    public GameObject warnigTextPref;
    public Transform warnigTextTransform;
    // 啟動
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Warning(string warning)
    {
        GameObject WTP = Instantiate(warnigTextPref, warnigTextTransform);
        TextMeshProUGUI TMP = WTP.GetComponent<TextMeshProUGUI>();
        TMP.text = warning;
        WTP.SetActive(true);
    }
}
