using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// playerPrefs:
/// PlayerUnlockedCharacter0
/// StroyBegin
/// FreeModeSet:EnemyAI
/// FreeModeEnemyLevel
/// FreeModePlayerLevel
/// FreeModeSet:EnemyAI
/// FreeModeCard_Player1Hold0
/// FreeModeCard_Player2Hold0
/// </summary>

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }
    
    public GameObject battleCanvas;
    public GameObject freeModeSets;

    [Header("Menu Sets")]
    public Button ExitGameButton;

    [Header("Free Battle Sets")]
    public static bool useEnemyAI = false;
    public Toggle AISetToggle;
    public Button StartFreeModeButton;

    [Header("Level")]
    public TMP_Text playerLevelText;
    public Slider playerLevelSlider;
    public static int playerLevel = 1;

    public TMP_Text enemyLevelText;
    public Slider enemyLevelSlider;
    public static int enemyLevel = 1;

    [Header("Card")]
    public GameObject CardSetPrefab;
    public Transform Player1CardSetContent;
    public Transform Player2CardSetContent;

    public static List<Card> player1Cards = new List<Card>();
    public static List<Card> player2Cards = new List<Card>();

    [Header("Story Select")]
    public static Character pendingInitialCharacter;
    public static int currentCharacterNumber = 0;
    public Image characterImage;
    public TMP_Text characterName;
    public GameObject skillButtonPref;
    public Transform skillTransform;
    public TMP_Text skillIntro;
    public Button ContinueStoryButton;
    public Button StartStoryModeButton;

    public GameObject characterButtonPrefab;
    public Transform characterButtonContent;

    public Button getCharacterButton;
    public GameObject WarningHoldText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        CheckGameMode();
        PlayerPrefs.SetInt($"PlayerUnlockedCharacter0", 1);
        if (!gameObject.activeInHierarchy)
            return;
        
        SetDeck();
        CreateCharacterButtons();
        enemyLevelText.text = $"Enemy_Level:{enemyLevel}";
        
        if (StartFreeModeButton != null && StartStoryModeButton != null && ContinueStoryButton != null)
        {
            StartFreeModeButton.onClick.AddListener(()=> StartFreeMode());
            StartStoryModeButton.onClick.AddListener(()=> StartStoryMode());
            ContinueStoryButton.onClick.AddListener(()=> GameModeManager.Instance.ContinueStory());
            ContinueStoryButton.gameObject.SetActive(PlayerPrefs.GetInt("StroyBegin") == 1);
        }

        if (AISetToggle != null)
        {
            AISetToggle.onValueChanged.AddListener(OnEnemyToggleChange);
            AISetToggle.isOn = (PlayerPrefs.GetInt("FreeModeSet:EnemyAI") == 1);
        }
        
        if (enemyLevelSlider != null && playerLevelSlider != null)
        {
            enemyLevelSlider?.onValueChanged.AddListener(ChangeEnemyLevel);
            if (PlayerPrefs.GetInt("FreeModeEnemyLevel") != 0)
                enemyLevelSlider.value = PlayerPrefs.GetInt("FreeModeEnemyLevel");

            enemyLevelSlider?.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => PlusEnemyLevel(1));
            enemyLevelSlider?.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(() => PlusEnemyLevel(-1));

            playerLevelSlider?.onValueChanged.AddListener(ChangePlayerLevel);
            if (PlayerPrefs.GetInt("FreeModePlayerLevel") != 0)
                playerLevelSlider.value = PlayerPrefs.GetInt("FreeModePlayerLevel");

            playerLevelSlider?.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => PlusPlayerLevel(1));
            playerLevelSlider?.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(() => PlusPlayerLevel(-1));
        }

        if (ExitGameButton != null)
        {
            ExitGameButton.onClick.AddListener(()=> ExitGame());
        }

        StartCoroutine(SetInitialCharacter(0));
    }
    public void CheckGameMode()
    {
        if (GameModeManager.GameStarted)
        {
            if (GameModeManager.Instance.gameMode == GameMode.story)
            {
                gameObject.SetActive(false);
                battleCanvas.SetActive(GameModeManager.BattleStarted);
            }
            else if (GameModeManager.Instance.gameMode == GameMode.free)
            {
                battleCanvas.SetActive(true);
                gameObject.SetActive(false);
            }
        }
        else
        {
            gameObject.SetActive(true);
            battleCanvas.SetActive(false);
        }
    }
    private void ExitGame() // 關閉遊戲
    {
        // 檢查是否在編輯器中運行
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
        #else
            Application.Quit(); // 實際遊戲發佈後使用
        #endif
        
        Debug.Log("Game is exiting..."); // 測試用，確認函數有被呼叫
    }

    void StartStoryMode()
    {
        GameModeManager.pendingInitialCharacter = pendingInitialCharacter;
        GameModeManager.Instance.StartStoryMode();
    }
    void StartFreeMode()
    {
        GameModeManager.Instance.StartFreeMode();
    }
    private void CreateCharacterButtons()
    {
        foreach (Transform child in characterButtonContent)
            Destroy(child.gameObject);

        for (int i = 0; i < GameModeManager.Instance.characterDatas.Count; i++)
        {
            int index = i;

            Character character = GameModeManager.Instance.characterDatas[i];
            GameObject btnGO = Instantiate(characterButtonPrefab, characterButtonContent);

            Button btn = btnGO.GetComponent<Button>();
            TMP_Text txt = btnGO.GetComponentInChildren<TMP_Text>();

            btnGO.GetComponent<Image>().sprite = character.characterAvatar;
            txt.text = character.characterName;
            btn.onClick.AddListener(() => SelectCharacter(index));

            btnGO.SetActive(true);
        }
    }

    public void UnlockCharacter(Character character)
    {
        if (character == null) return;

        PlayerPrefs.SetInt($"PlayerUnlockedCharacter{GameModeManager.Instance.characterDatas.IndexOf(character)}", 1);

        if (PlayerPrefs.GetInt($"PlayerUnlockedCharacter{GameModeManager.Instance.characterDatas.IndexOf(character)}") == 1)
        {
            getCharacterButton.gameObject.SetActive(false);
            WarningHoldText.gameObject.SetActive(false);
            StartStoryModeButton.gameObject.SetActive(true);
            getCharacterButton.onClick.RemoveAllListeners();
        }
    }

    #region Card

    private void SetDeck() // 設定牌堆
    {
        if (player1Cards.Count <= 0)
            player1Cards = new List<Card>(GameModeManager.Instance?.cardDatas);
        if (player2Cards.Count <= 0)
            player2Cards = new List<Card>(GameModeManager.Instance?.cardDatas);

        foreach(Card card in player1Cards)
        {
            GameObject cardset = Instantiate(CardSetPrefab, Player1CardSetContent);
            CardToggle CT = cardset.GetComponentInChildren<CardToggle>();
            CT.card = card;
            CT.player = 1;
            if (!PlayerPrefs.HasKey($"FreeModeCard_Player1Hold{GameModeManager.Instance.cardDatas.IndexOf(card)}"))
            {
                PlayerPrefs.SetInt($"FreeModeCard_Player1Hold{GameModeManager.Instance.cardDatas.IndexOf(card)}", 1);
            }
            cardset.SetActive(true);
        }
        foreach(Card card in player2Cards)
        {
            GameObject cardset = Instantiate(CardSetPrefab, Player2CardSetContent);
            CardToggle CT = cardset.GetComponentInChildren<CardToggle>();
            CT.card = card;
            CT.player = 2;
            if (!PlayerPrefs.HasKey($"FreeModeCard_Player2Hold{GameModeManager.Instance.cardDatas.IndexOf(card)}"))
            {
                PlayerPrefs.SetInt($"FreeModeCard_Player2Hold{GameModeManager.Instance.cardDatas.IndexOf(card)}", 1);
            }
            cardset.SetActive(true);
        }
    }

    public void SetCardDeck(Card card, int player, bool addIn)
    {
        if (player == 1)
        {
            if (addIn && !player1Cards.Contains(card))
            {
                PlayerPrefs.SetInt($"FreeModeCard_Player1Hold{GameModeManager.Instance.cardDatas.IndexOf(card)}", 1);
                player1Cards.Add(card);
            }
            else
            {
                PlayerPrefs.SetInt($"FreeModeCard_Player1Hold{GameModeManager.Instance.cardDatas.IndexOf(card)}", 0);
                player1Cards.Remove(card);
            } 
        }
        else
        {
            if (addIn && !player2Cards.Contains(card))
            {
                PlayerPrefs.SetInt($"FreeModeCard_Player2Hold{GameModeManager.Instance.cardDatas.IndexOf(card)}", 1);
                player2Cards.Add(card);
            }
            else 
            {
                PlayerPrefs.SetInt($"FreeModeCard_Player2Hold{GameModeManager.Instance.cardDatas.IndexOf(card)}", 0);
                player2Cards.Remove(card);
            }
        }
    }

    #endregion

    #region Character Set
    // 設定角色
    public IEnumerator SetInitialCharacter(int number) // 設定預設角色
    {
        if (number < GameModeManager.Instance.characterDatas.Count)
            pendingInitialCharacter = GameModeManager.Instance.characterDatas[number];
        else yield break;

        currentCharacterNumber = number;

        characterImage.sprite = pendingInitialCharacter.characterPicture;
        characterName.text = pendingInitialCharacter.characterName;

        foreach(Transform child in skillTransform)
            Destroy(child.gameObject);

        yield return null;

        foreach(Skill skill in pendingInitialCharacter.skills)
        {
            GameObject skillGO = Instantiate(skillButtonPref, skillTransform);

            skillGO.transform.GetChild(0).GetComponent<TMP_Text>().text = skill.skillName;
            skillGO.transform.GetComponent<Button>().onClick.AddListener(()=> ShowSkill(skill));
            skillGO.SetActive(true);
        }
        foreach(PassiveSkill skill in pendingInitialCharacter.passiveSkills)
        {
            GameObject skillGO = Instantiate(skillButtonPref, skillTransform);

            skillGO.transform.GetChild(0).GetComponent<TMP_Text>().text = skill.skillName;
            skillGO.transform.GetComponent<Button>().onClick.AddListener(()=> ShowPassiveSkill(skill));
            skillGO.SetActive(true);
        }

        yield return null;
        if (skillTransform.childCount > 0)
        {
            skillTransform.GetChild(0).GetComponent<Button>().onClick.Invoke();
        }
        else if(pendingInitialCharacter.skills.Count + pendingInitialCharacter.passiveSkills.Count == 0)
            skillIntro.text = "沒有技能";
        else
            skillIntro.text = "沒有技能";

        if (PlayerPrefs.GetInt($"PlayerUnlockedCharacter{number}") == 1)
        {
            getCharacterButton.gameObject.SetActive(false);
            WarningHoldText.gameObject.SetActive(false);
            StartStoryModeButton.gameObject.SetActive(true);
            getCharacterButton.onClick.RemoveAllListeners();
        }
        else
        {
            getCharacterButton.gameObject.SetActive(true);
            WarningHoldText.gameObject.SetActive(true);
            StartStoryModeButton.gameObject.SetActive(false);
            getCharacterButton.onClick.RemoveAllListeners();
            getCharacterButton.onClick.AddListener(() => UnlockCharacter(pendingInitialCharacter));
        }

    }
    private void SelectCharacter(int index)
    {
        if (index < 0 || index >= GameModeManager.Instance.characterDatas.Count)
            return;

        currentCharacterNumber = index;
        StopAllCoroutines();
        StartCoroutine(SetInitialCharacter(index));
    }
    public void SetNextCharacter() // 選擇下一名
    {
        currentCharacterNumber ++;
        if (currentCharacterNumber >= GameModeManager.Instance.characterDatas.Count)
            currentCharacterNumber = 0;

        SelectCharacter(currentCharacterNumber);
    }
    public void SetPreviousCharacter() // 選擇上一名
    {
        currentCharacterNumber --;
        if (currentCharacterNumber < 0)
            currentCharacterNumber = GameModeManager.Instance.characterDatas.Count - 1;

        SelectCharacter(currentCharacterNumber);
    }
    private void ShowSkill(Skill skill)
    {
        skillIntro.text = skill.skillEffect;
    }
    private void ShowPassiveSkill(PassiveSkill skill)
    {
        skillIntro.text = skill.skillEffect;
    }

    #endregion

    #region Set level
    void OnEnemyToggleChange(bool isOn) // 啟用電腦
    {
        useEnemyAI = isOn;
        PlayerPrefs.SetInt("FreeModeSet:EnemyAI", isOn ? 1 : 0);
    }

    void ChangePlayerLevel(float value)
    {
        playerLevel = Mathf.FloorToInt(value);
        playerLevelText.text = $"Player_Level:{playerLevel}";
        PlayerPrefs.SetInt("FreeModePlayerLevel", playerLevel);
    }
    void ChangeEnemyLevel(float value)
    {
        enemyLevel = Mathf.FloorToInt(value);
        enemyLevelText.text = $"Enemy_Level:{enemyLevel}";
        PlayerPrefs.SetInt("FreeModeEnemyLevel", enemyLevel);
    }

    void PlusPlayerLevel(int value)
    {
        playerLevel += value;
        if (playerLevel > playerLevelSlider.maxValue)
            playerLevel = Mathf.FloorToInt(playerLevelSlider.maxValue);
        else if (playerLevel < playerLevelSlider.minValue)
            playerLevel = Mathf.Max(1 ,Mathf.FloorToInt(playerLevelSlider.minValue));

        playerLevelSlider.value = playerLevel;
        playerLevelText.text = $"Enemy_Level:{playerLevel}";
        PlayerPrefs.SetInt("FreeModePlayerLevel", playerLevel);
    }
    void PlusEnemyLevel(int value)
    {
        enemyLevel += value;
        if (enemyLevel > enemyLevelSlider.maxValue)
            enemyLevel = Mathf.FloorToInt(enemyLevelSlider.maxValue);
        else if (enemyLevel < enemyLevelSlider.minValue)
            enemyLevel = Mathf.Max(1 ,Mathf.FloorToInt(enemyLevelSlider.minValue));

        enemyLevelSlider.value = enemyLevel;
        enemyLevelText.text = $"Enemy_Level:{enemyLevel}";
        PlayerPrefs.SetInt("FreeModeEnemyLevel", enemyLevel);
    }

    #endregion
}
