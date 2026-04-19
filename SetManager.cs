using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Data.Common;
using System.Linq;

// backgroundMusicVolume
// AutoAcceptQuest

public class SetManager : MonoBehaviour
{
    [Header("Audio")]
    public static float backgroundMusicVolume = 1;
    public Slider backgroundMusicVolumeSlider;
    public TMP_Text backgroundMusicVolumeText;

    public static float specialEffectsVolume = 1;
    public Slider specialEffectsVolumeSlider;
    public TMP_Text specialEffectsVolumeText;

    [Header("Player pref")]
    public static bool AutoAcceptQuest = false;
    public Toggle AutoAcceptQuest_Toggle;

    void Start()
    {
        Initialize();
        // 背景音量
        ChangeBackgroundVolume(backgroundMusicVolume);
        backgroundMusicVolumeSlider.onValueChanged.AddListener(ChangeBackgroundVolume);
        // 特效音量
        ChangeSpecialEffectsVolume(specialEffectsVolume);
        specialEffectsVolumeSlider.onValueChanged.AddListener(ChangeBackgroundVolume);
        // 自動接任務
        AutoAcceptQuest_Toggle.SetIsOnWithoutNotify(AutoAcceptQuest);
        AutoAcceptQuest_Toggle.onValueChanged.AddListener(ChangeAutoAccept);
    }
    public static void Initialize() // 初始化
    {
        // 背景音量
        if (!PlayerPrefs.HasKey("backgroundMusicVolume"))
        {
            PlayerPrefs.SetFloat("backgroundMusicVolume", 5);
        }
        backgroundMusicVolume = PlayerPrefs.GetFloat("backgroundMusicVolume");
        if (EventUI.Instance != null)
        {
            EventUI.Instance.backgroundMusicSource.volume = backgroundMusicVolume;
        }
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.backgroundMusicSource.volume = backgroundMusicVolume;
        }
        // 特效音量
        if (!PlayerPrefs.HasKey("specialEffectsVolume"))
        {
            PlayerPrefs.SetFloat("specialEffectsVolume", 5);
        }
        specialEffectsVolume = PlayerPrefs.GetFloat("specialEffectsVolume");

        // 自動接任務
        if (!PlayerPrefs.HasKey("AutoAcceptQuest"))
        {
            PlayerPrefs.SetInt("AutoAcceptQuest", 1);
        }
        AutoAcceptQuest = PlayerPrefs.GetInt("AutoAcceptQuest") == 1;
    }

    public void ChangeBackgroundVolume(float value) // 背景音量
    {
        SetManager.backgroundMusicVolume = value / backgroundMusicVolumeSlider.maxValue;
        PlayerPrefs.SetFloat("backgroundMusicVolume", backgroundMusicVolume);
        backgroundMusicVolumeText.text = $"{Mathf.RoundToInt(value)}";
        backgroundMusicVolumeSlider.SetValueWithoutNotify(value);

        if (EventUI.Instance != null)
        {
            EventUI.Instance.backgroundMusicSource.volume = backgroundMusicVolume;
        }
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.backgroundMusicSource.volume = backgroundMusicVolume;
        }
    }

    public void ChangeSpecialEffectsVolume(float value) // 特效音量
    {
        SetManager.specialEffectsVolume = value  / specialEffectsVolumeSlider.maxValue;
        PlayerPrefs.SetFloat("specialEffectsVolume", specialEffectsVolume);
        specialEffectsVolumeText.text = $"{Mathf.RoundToInt(value)}";
        specialEffectsVolumeSlider.SetValueWithoutNotify(value);

        if (TooltipUI.Instance != null)
        {
            TooltipUI.Instance.audioSource.volume = specialEffectsVolume;
        }
    }

    public void ChangeAutoAccept(bool auto) // 設定自動接取
    {
        AutoAcceptQuest = auto;
        AutoAcceptQuest_Toggle.SetIsOnWithoutNotify(auto);

        PlayerPrefs.SetInt("AutoAcceptQuest", auto ? 1 : 0);
    }
}
