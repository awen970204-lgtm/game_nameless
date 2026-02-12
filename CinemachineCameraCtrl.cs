using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraCtrl : MonoBehaviour
{
    public static CinemachineCameraCtrl Instance { get; private set; }
    public CinemachineCamera cinemachineCamera;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    void OnEnable()
    {
        GameCharacterManager.SetCharacter += BindCharacter;
    }

    void OnDisable()
    {
        GameCharacterManager.SetCharacter -= BindCharacter;
    }

    private void BindCharacter(GameObject character)
    {
        if (this == null || cinemachineCamera == null) return;
        cinemachineCamera.Follow = character.transform;
    }
}
