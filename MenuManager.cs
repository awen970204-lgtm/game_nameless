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
using NUnit.Framework.Internal;



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
    [SerializeField] private Button ExitGameButton;
    [SerializeField] private GameObject illuatratedGuidePanel;
    [SerializeField] private Button illuatratedGuideButton;
    [SerializeField] private Transform panelTransform;
    [SerializeField] private GameObject relatedButton;

    [Header("Character Icon")]
    [SerializeField] private Transform characterIconsTransform;
    [SerializeField] private GameObject characterIcon;
    [SerializeField] private GameObject characterPanelPrefab;
    [SerializeField] private TMP_Dropdown characterLayoutTypeDropdown;
    private enum LayoutType
    {
        normal,
        Camp,
        MaxHP,
        StartHP,
        AttackPower,
        Defense,
        TauntLevel,
    }
    [SerializeField] private Toggle ReverseToggle;
    private bool Reverse = false;

    [Header("Card Icon")]
    [SerializeField] private Transform cardIconsTransform;
    [SerializeField] private GameObject cardIcon;
    [SerializeField] private GameObject cardPanelPrefab;

    [Header("StoryEnd Icon")]
    [SerializeField] private Transform storyEndIconsTransform;
    [SerializeField] private GameObject storyEndIcon;
    [SerializeField] private GameObject storyEndPanelPrefab;

    [Header("Panel Active")]
    [SerializeField] private GameObject basePanel;
    [SerializeField] private Button characterCheckButton;
    [SerializeField] private Button cardCheckButton;
    [SerializeField] private Button storyEndCheckButton;

    [Header("Free Battle Sets")]
    [SerializeField] private Toggle AISetToggle;
    public static bool useEnemyAI = false;
    [SerializeField] private Button StartFreeModeButton;

    [Header("Level")]
    [SerializeField] private TMP_Text playerLevelText;
    [SerializeField] private Slider playerLevelSlider;
    public static int playerLevel = 1;

    [SerializeField] private TMP_Text enemyLevelText;
    [SerializeField] private Slider enemyLevelSlider;
    public static int enemyLevel = 1;

    [Header("Card")]
    [SerializeField] private GameObject CardSetPrefab;
    [SerializeField] private Transform Player1CardSetContent;
    [SerializeField] private Transform Player2CardSetContent;

    public static List<Card> player1Cards = new List<Card>();
    public static List<Card> player2Cards = new List<Card>();

    [Header("Story Select")]
    public static Character pendingInitialCharacter;
    public static int currentCharacterNumber = 0;
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text characterName;
    [SerializeField] private GameObject skillButtonPref;
    [SerializeField] private Transform skillTransform;
    [SerializeField] private TMP_Text skillIntro;
    [SerializeField] private Button ContinueStoryButton;
    [SerializeField] private Button StartStoryModeButton;

    [SerializeField] private GameObject characterButtonPrefab;
    [SerializeField] private Transform characterButtonContent;

    [SerializeField] private Button getCharacterButton;
    [SerializeField] private GameObject WarningHoldText;

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
            if (ReverseToggle != null)
            {
                ReverseToggle.isOn = false;
                ReverseToggle.onValueChanged.AddListener(ChangeCharacterIconSort);
            }
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
            $"受傷減免:{character.damageReduction}\n"+
            $"基礎抽牌數:{character.drawCount}\n"+
            $"嘲諷等級:{character.tauntLevel}";

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

    private void ChangeCharacterIconSort(bool reverse)
    {
        if (ReverseToggle != null)
        {
            Reverse = reverse;
            Text label = ReverseToggle.GetComponentInChildren<Text>();
            label.text = reverse ? "由大到小" : "由小到大";
        }
        if (characterLayoutTypeDropdown != null)
        {
            ChangeLayoutType(characterLayoutTypeDropdown.value);
        }
    }

    private void ChangeLayoutType(int value) // 改變圖標排序
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
            LayoutType.MaxHP => characters.OrderBy(c => c.characterMaxHP).ToList(),
            LayoutType.StartHP => characters.OrderBy(c => c.characterStartHP).ToList(),
            LayoutType.AttackPower => characters.OrderBy(c => c.attackPower).ToList(),
            LayoutType.Defense => characters.OrderBy(c => c.defense).ToList(),
            LayoutType.TauntLevel => characters.OrderBy(c => c.tauntLevel).ToList(),
            _ => characters,
        };
        if (Reverse)
            characters.Reverse();

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
