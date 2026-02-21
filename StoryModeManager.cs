using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;
using System.Linq;

public class StoryModeManager : MonoBehaviour
{
    public static StoryModeManager Instance { get; private set; }

    public static int playerLevel = 1;
    private int nowExperience = 0;
    private int levelupExperience = 2;

    [Header("level")]
    public TMP_Text levelText;
    public static int LevelUpChange = 2;
    [Header("Team")]
    public GameObject characterPrefab;
    public Transform chracterContainer;

    public Button teamButton;
    public GameObject teamPanel;

    [HideInInspector] public List<Character> characters = new List<Character>();
    [HideInInspector] public List<Card> cards = new List<Card>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        CheckLevel();
        teamButton.onClick.AddListener(()=> OnClickTeamButton());
    }

    #region level
    
    public void GetExperience(int EX)
    {
        nowExperience += EX;
        CheckLevel();
    }
    private void CheckLevel()
    {
        while(nowExperience >= levelupExperience)
        {
            nowExperience -= levelupExperience;
            levelupExperience += LevelUpChange;
            playerLevel ++;
        }

        levelText.text = $"Level:{playerLevel}({nowExperience}/{levelupExperience})";
    }
    #endregion

    #region character
    
    public void GetNewMenber(Character character)
    {
        if (characters.Contains(character)) return;

        characters.Add(character);

        GameObject CG = Instantiate(characterPrefab, chracterContainer);
        CG.transform.GetChild(0).GetComponent<Image>().sprite = character.characterAvatar;
        CG.transform.GetChild(1).GetComponent<TMP_Text>().text = character.characterName;
        CG.gameObject.SetActive(true);

        cards.AddRange(GameModeManager.Instance.cardDatas.Where(c => c.holderCharacter == character));

        PlayerPrefs.SetInt($"PlayerUnlockedCharacter{GameModeManager.Instance.characterDatas.IndexOf(character)}", 1);
    }

    private void OnClickTeamButton()
    {
        QuestManager.Instance.questsPanel.SetActive(false);
        teamPanel.SetActive(!teamPanel.activeInHierarchy);
    }

    #endregion
}
