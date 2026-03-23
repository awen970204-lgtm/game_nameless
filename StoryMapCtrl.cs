using UnityEngine;

public class StoryMapCtrl : MonoBehaviour
{
    public GameObject map;

    void OnEnable()
    {
        GameCharacterManager.OpenMap += OpenMap;
    }
    void OnDisable()
    {
        GameCharacterManager.OpenMap -= OpenMap;
    }

    void OpenMap()
    {
        map.SetActive(true);
    }
}
