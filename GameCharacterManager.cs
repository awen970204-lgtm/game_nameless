using UnityEngine;
using System;

public class GameCharacterManager : MonoBehaviour
{
    public static GameCharacterManager Instance { get; private set; }
    public static event Action<GameObject> SetCharacter;
    public GameObject actingCharacter;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        transform.GetChild(1).gameObject.SetActive(true);
    }

    public void SetActing(GameObject act)
    {
        if (act == null)
        {
            Debug.LogWarning("沒有設定角色");
            return;
        }
        if (actingCharacter != null)
            actingCharacter.SetActive(false);
        actingCharacter = act;
        act.SetActive(true);
        SetCharacter?.Invoke(act);
    }
}
