using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }
    
    public GameObject battleCanvas;
    public GameObject freeModeSets;

    [Header("FreeBattleSets")]
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
        
        StartFreeModeButton.onClick.AddListener(()=> StartFreeMode());
        StartStoryModeButton.onClick.AddListener(()=> StartStoryMode());
        AISetToggle.SetIsOnWithoutNotify(useEnemyAI);
        AISetToggle.onValueChanged.AddListener(OnEnemyToggleChange);
        enemyLevelSlider?.onValueChanged.AddListener(ChangeEnemyLevel);
        enemyLevelSlider?.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => PlusEnemyLevel(1));
        enemyLevelSlider?.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(() => PlusEnemyLevel(-1));
        playerLevelSlider?.onValueChanged.AddListener(ChangePlayerLevel);
        playerLevelSlider?.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => PlusPlayerLevel(1));
        playerLevelSlider?.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(() => PlusPlayerLevel(-1));

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
            cardset.SetActive(true);
        }
        foreach(Card card in player2Cards)
        {
            GameObject cardset = Instantiate(CardSetPrefab, Player2CardSetContent);
            CardToggle CT = cardset.GetComponentInChildren<CardToggle>();
            CT.card = card;
            CT.player = 2;
            cardset.SetActive(true);
        }
    }

    public void SetCardDeck(Card card, int player, bool addIn)
    {
        if (player == 1)
        {
            if (addIn && !player1Cards.Contains(card))
            {
                player1Cards.Add(card);
            }
            else player1Cards.Remove(card);
        }
        else
        {
            if (addIn && !player2Cards.Contains(card))
            {
                player2Cards.Add(card);
            }
            else player2Cards.Remove(card);
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
    }

    void ChangePlayerLevel(float value)
    {
        playerLevel = Mathf.FloorToInt(value);
        playerLevelText.text = $"Player_Level:{playerLevel}";
    }
    void ChangeEnemyLevel(float value)
    {
        enemyLevel = Mathf.FloorToInt(value);
        enemyLevelText.text = $"Enemy_Level:{enemyLevel}";
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
    }

    #endregion
}
