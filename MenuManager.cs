using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using System.Linq;


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
    public GameObject illuatratedGuidePanel;
    public Button illuatratedGuideButton;
    public Transform panelTransform;
    public GameObject relatedButton;

    public Transform characterIconsTransform;
    public GameObject characterIcon;
    public GameObject characterPanelPrefab;
    public TMP_Dropdown characterLayoutTypeDropdown;
    private enum LayoutType
    {
        normal,
        Camp,
    }

    public Transform cardIconsTransform;
    public GameObject cardIcon;
    public GameObject cardPanelPrefab;

    public Transform storyEndIconsTransform;
    public GameObject storyEndIcon;
    public GameObject storyEndPanelPrefab;
    [Header("Panel Active")]
    public GameObject basePanel;
    public Button characterCheckButton;
    public Button cardCheckButton;
    public Button storyEndCheckButton;

    [Header("Free Battle Sets")]
    public Toggle AISetToggle;
    public static bool useEnemyAI = false;
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

        if (ExitGameButton != null && illuatratedGuideButton != null)
        {
            ExitGameButton.onClick.AddListener(()=> ExitGame());
            illuatratedGuideButton.onClick.AddListener(()=> SetIlluatratedGuidePanel());
        }

        if (characterIcon != null && characterIconsTransform != null && characterLayoutTypeDropdown != null)
        {
            LayoutCharacterIcon(LayoutType.normal);
            characterLayoutTypeDropdown.onValueChanged
                .AddListener(ChangeLayoutType);
        }
        if (cardIcon != null && cardIconsTransform != null)
        {
            foreach(var card in GameModeManager.Instance?.cardDatas)
            {
                GameObject go = Instantiate(cardIcon, cardIconsTransform);
                go.transform.GetChild(0).GetComponent<Image>().sprite = card.cardPicture;
                go.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(()=> CheckCardInMenu(card));
                go.transform.GetChild(1).GetComponent<TMP_Text>().text = card.cardName;
                go.SetActive(true);
            }
        }
        if (storyEndIcon != null && storyEndIconsTransform != null)
        {
            foreach(var end in GameModeManager.Instance?.StoryEndDatas)
            {
                GameObject go = Instantiate(storyEndIcon, storyEndIconsTransform);
                go.transform.GetChild(0).GetComponent<Image>().sprite = end.endImage;
                go.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(()=> CheckStoryEndInMenu(end));
                go.transform.GetChild(1).GetComponent<TMP_Text>().text = end.endName;
                go.SetActive(true);
            }
        }

        if (characterCheckButton != null && cardCheckButton != null && storyEndCheckButton != null)
        {
            characterCheckButton.onClick.AddListener(()=> ClickCharacetrCheckButton());
            cardCheckButton.onClick.AddListener(()=> ClickCardCheckButton());
            storyEndCheckButton.onClick.AddListener(()=> ClickStoryEndCheckButton());
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
    private void SetIlluatratedGuidePanel() // 開關圖鑑
    {
        illuatratedGuidePanel.SetActive(!illuatratedGuidePanel.activeInHierarchy);
    }
    private void ClickCharacetrCheckButton() // 點擊查看角色
    {
        if (basePanel == null) return;

        bool active = basePanel.transform.GetChild(0).gameObject.activeInHierarchy;
        basePanel.SetActive(!active);
        basePanel.transform.GetChild(0).gameObject.SetActive(!active);
        basePanel.transform.GetChild(1).gameObject.SetActive(false);
        basePanel.transform.GetChild(2).gameObject.SetActive(false);
    }
    private void ClickCardCheckButton() // 點擊查看卡片
    {
        if (basePanel == null) return;

        bool active = basePanel.transform.GetChild(1).gameObject.activeInHierarchy;
        basePanel.SetActive(!active);
        basePanel.transform.GetChild(0).gameObject.SetActive(false);
        basePanel.transform.GetChild(1).gameObject.SetActive(!active);
        basePanel.transform.GetChild(2).gameObject.SetActive(false);
    }
    private void ClickStoryEndCheckButton() // 點擊查看結局
    {
        if (basePanel == null) return;

        bool active = basePanel.transform.GetChild(2).gameObject.activeInHierarchy;
        basePanel.SetActive(!active);
        basePanel.transform.GetChild(0).gameObject.SetActive(false);
        basePanel.transform.GetChild(1).gameObject.SetActive(false);
        basePanel.transform.GetChild(2).gameObject.SetActive(!active);
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
    
    private void CheckCharacterInMenu(Character character)
    {
        if (characterPanelPrefab == null || panelTransform == null) return;
        if (character == null) return;

        GameObject cpGo = Instantiate(characterPanelPrefab, panelTransform);
        cpGo.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(()=> Destroy(cpGo));
        cpGo.transform.GetChild(1).GetComponent<TMP_Text>().text = character.characterName;
        cpGo.transform.GetChild(2).GetComponent<Image>().sprite = character.characterPicture;
        cpGo.transform.GetChild(3).GetChild(0).GetComponent<TMP_Text>().text = character.backgroundStory;
        cpGo.transform.GetChild(3).GetChild(1).GetComponent<TMP_Text>().text = 
        character.characterCamp switch
        {
            Character.CharacterCamp.Travelers => "陣營:旅者",
            Character.CharacterCamp.Demons => "陣營:惡魔",
            Character.CharacterCamp.Church => "陣營:教會",
            Character.CharacterCamp.Pirates => "陣營:海盜",
            Character.CharacterCamp.Magicians => "陣營:魔法師",
            _ => "陣營:無",
        };

        cpGo.transform.GetChild(3).GetChild(2).GetComponent<TMP_Text>().text = 
        $"相遇次數:{PlayerPrefs.GetInt($"UnlockCharacter{GameModeManager.Instance.characterDatas.IndexOf(character)}Times")}";

        Transform skillTransform = cpGo.transform.GetChild(4).GetChild(0).GetChild(0).GetChild(0);
        GameObject skillButton = cpGo.transform.GetChild(4).GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject;
        Transform relatedTransform = cpGo.transform.GetChild(4).GetChild(2).GetChild(0).GetChild(0);
        foreach(var skill in character.skills)
        {
            GameObject sGo = Instantiate(skillButton, skillTransform);
            sGo.GetComponent<Button>().onClick.AddListener(()=> 
                cpGo.transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = skill.skillEffect);
            sGo.transform.GetChild(0).GetComponent<TMP_Text>().text = skill.skillName;
            sGo.SetActive(true);

            foreach(var related in skill.relatedChracters)
            {
                GameObject cGo = Instantiate(relatedButton, relatedTransform);
                cGo.GetComponent<Button>().onClick.AddListener(()=> CheckCharacterInMenu(related));
                cGo.transform.GetChild(0).GetComponent<TMP_Text>().text = related.characterName;
                cGo.transform.GetChild(1).GetComponent<Image>().sprite = related.characterAvatar;
                cGo.SetActive(true);
            }
        }
        foreach(var passiveSkill in character.passiveSkills)
        {
            GameObject sGo = Instantiate(skillButton, skillTransform);
            sGo.GetComponent<Button>().onClick.AddListener(()=> 
                cpGo.transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = passiveSkill.skillEffect);
            sGo.transform.GetChild(0).GetComponent<TMP_Text>().text = passiveSkill.skillName;
            sGo.SetActive(true);

            foreach(var related in passiveSkill.relatedChracters)
            {
                GameObject cGo = Instantiate(relatedButton, relatedTransform);
                cGo.GetComponent<Button>().onClick.AddListener(()=> CheckCharacterInMenu(related));
                cGo.transform.GetChild(0).GetComponent<TMP_Text>().text = related.characterName;
                cGo.transform.GetChild(1).GetComponent<Image>().sprite = related.characterAvatar;
                cGo.SetActive(true);
            }
        }
        if (character.skills.Count > 0)
        {
            cpGo.transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = $"{character.skills[0].skillEffect}";
        }
        else if (character.passiveSkills.Count > 0)
        {
            cpGo.transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = $"{character.passiveSkills[0].skillEffect}";
        }
        else
        {
            cpGo.transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = "沒有技能";
        }
        
        cpGo.transform.GetChild(5).GetChild(0).GetComponent<TMP_Text>().text = 
            $"生命值:{character.characterStartHP}/{character.characterMaxHP}\n" +
            $"攻擊:{character.attackPower}\n"+
            $"回復力:{character.healPower}\n"+
            $"防禦:{character.defense}\n"+
            $"傷害倍率:{character.damageMultiplier*100}%\n"+
            $"受傷減免:{character.damageReduction}";

        cpGo.SetActive(true);
    }
    private void CheckCardInMenu(Card card)
    {
        if (cardPanelPrefab == null || panelTransform == null) return;

        GameObject cardGo = Instantiate(cardPanelPrefab, panelTransform);
        cardGo.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(()=> Destroy(cardGo));
        cardGo.transform.GetChild(1).GetComponent<TMP_Text>().text = card.cardName;
        cardGo.transform.GetChild(2).GetComponent<Image>().sprite = card.cardPicture;
        cardGo.transform.GetChild(3).GetChild(0).GetComponent<TMP_Text>().text = card.cardToolTip;

        Transform relatedTransform = cardGo.transform.GetChild(3).GetChild(1).GetChild(0).GetChild(0);
        if (card.holderCharacter != null)
        {
            GameObject cGo = Instantiate(relatedButton, relatedTransform);
            cGo.GetComponent<Button>().onClick.AddListener(()=> CheckCharacterInMenu(card.holderCharacter));
            cGo.transform.GetChild(0).GetComponent<TMP_Text>().text = card.holderCharacter.characterName;
            cGo.transform.GetChild(1).GetComponent<Image>().sprite = card.holderCharacter.characterAvatar;
            cGo.SetActive(true);
        }

        cardGo.SetActive(true);
    }
    private void CheckStoryEndInMenu(StoryEnd storyEnd)
    {
        if (storyEndPanelPrefab == null || panelTransform == null) return;

        GameObject endGo = Instantiate(storyEndPanelPrefab, panelTransform);
        endGo.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(()=> Destroy(endGo));
        endGo.transform.GetChild(1).GetComponent<TMP_Text>().text = storyEnd.endName;
        endGo.transform.GetChild(2).GetComponent<Image>().sprite = storyEnd.endImage;
        endGo.transform.GetChild(3).GetChild(0).GetComponent<TMP_Text>().text = storyEnd.endIntroduse;
        endGo.SetActive(true);
    }

    private void ChangeLayoutType(int value)
    {
        if (Enum.IsDefined(typeof(LayoutType), value))
        {
            LayoutCharacterIcon((LayoutType)characterLayoutTypeDropdown.value);
        }
    }
    private void LayoutCharacterIcon(LayoutType layoutType) // 產生圖標
    {
        foreach(Transform transform in characterIconsTransform)
        {
            Destroy(transform.gameObject);
        }
        List<Character> characters = new List<Character>(GameModeManager.Instance?.characterDatas);
        characters = layoutType switch
        {
            LayoutType.normal => characters,
            LayoutType.Camp => characters.OrderBy(c => c.characterCamp).ToList(),
            _ => characters,
        };

        foreach(var character in characters)
        {
            GameObject go = Instantiate(characterIcon, characterIconsTransform);
            go.transform.GetChild(0).GetComponent<Image>().sprite = character.characterAvatar;
            go.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(()=> CheckCharacterInMenu(character));
            go.transform.GetChild(1).GetComponent<TMP_Text>().text = character.characterName;
            go.SetActive(true);
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
