using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Data.Common;
using System.Data;

public class EventUI : MonoBehaviour
{
    public static EventUI Instance { get; private set; }

    [Header("UI Control")]
    public static int NowScene = 1;

    public GameObject StoryUI;
    public GameObject SetPanel;
    public Button SaveButton;

    private static int backgroundMusicVoice = 10;
    public AudioSource backgroundMusicSource;
    public Slider backgroundMusicVoiceSlider;
    public TMP_Text backgroundMusicVoiceText;

    [Header("Event UI References")]
    public GameObject panel;
    public GameObject eventPanel;
    public TMP_Text eventNameText;
    public Image eventImage;
    public TMP_Text descriptionText;

    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;

    public GameObject triggerEventButton;
    private EventData pendingEventData;
    private EventData lastEventData;
    public static bool InEvent = false;

    [Header("Dialogue UI References")]
    public GameObject DialoguePanel;
    public Image portraitImage;
    public TMP_Text speakerText;
    public TMP_Text contentText;
    public TMP_Text recordText;
    public GameObject skipPanel;
    public TMP_Text skipText;
    private int currentIndex = 0;
    private DialogueData lastDialogueData;
    [Header("Battle References")]
    [HideInInspector] public EventBattleData lastEventBattleData;

    [Header("Typing Effect")]
    [SerializeField] private float typingSpeed = 0.05f;
    private Coroutine typingCoroutine;
    private string fullText = "";
    private bool typing = false;
    [Header("End")]
    public Image endPanel;
    public Button endStoryButton;
    public GameObject endOverPanel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else Destroy(gameObject);
        
    }
    void Start()
    {
        GameModeManager.Instance.GameEventUI = this.GetComponentInParent<Canvas>().gameObject;
        SaveButton.onClick.AddListener(()=> StartCoroutine(SaveStoryMode()));
        endStoryButton.onClick.AddListener(()=> StartCoroutine(GameModeManager.Instance.StoryModeEnd()));
        triggerEventButton.GetComponent<Button>().onClick.AddListener(()=> TriggerPendingEvent());

        StoryUI.SetActive(true);

        if (!PlayerPrefs.HasKey("backgroundMusicVoice"))
        {
            PlayerPrefs.SetInt("backgroundMusicVoice", 10);
        }
        backgroundMusicVoice = PlayerPrefs.GetInt("backgroundMusicVoice");
        ChangeBackgroundVoice(backgroundMusicVoice);
        backgroundMusicVoiceSlider.onValueChanged.AddListener(ChangeBackgroundVoice);
    }

    // 介面互動
    private IEnumerator SaveStoryMode()
    {
        if (!GameModeManager.GameStarted) yield break;
        
        SaveStoryData();
        yield return (GameModeManager.Instance.BackToMenu());

        SetPanel.SetActive(false);
        StoryUI.SetActive(true);
        eventPanel.SetActive(false);
        DialoguePanel.SetActive(false);
        panel.SetActive(false);
    }
    private void SaveStoryData()
    {
        PlayerPrefs.SetInt("StroyBegin", 1);
        foreach(var character in StoryModeManager.characters)
        {
            PlayerPrefs.SetInt($"PlayerHoldCharacter{GameModeManager.Instance.characterDatas.IndexOf(character)}InStory", 1);
        }
        foreach(var quest in QuestManager.MainQuests)
        {
            PlayerPrefs.SetInt($"StoryModeHasQuest:{quest.questName}", QuestManager.MainQuestShedules[quest]);
        }
        foreach(var card in StoryModeManager.cards)
        {
            PlayerPrefs.SetInt($"StoryModeHoldCard:{GameModeManager.Instance.cardDatas.IndexOf(card)}", 1);
        }
    }

    public void OnPlayerInEvent(EventData Data)
    {
        pendingEventData = Data;
        if (triggerEventButton != null)
            triggerEventButton.SetActive(true);
    }
    public void OnPlayerExitEvent()
    {
        pendingEventData = null;
        if (triggerEventButton != null)
            triggerEventButton.SetActive(false);
    }
    public void TriggerPendingEvent()
    {
        if (pendingEventData != null)
        {
            Debug.Log($"事件:{pendingEventData.eventName}觸發");
            ShowEvent(pendingEventData);
            pendingEventData = null;
            if (triggerEventButton != null)
                triggerEventButton.SetActive(false);
        }
    }

    #region Event

    public void ShowEvent(EventData data)
    {
        panel.SetActive(true);
        eventPanel.SetActive(true);
        GamecharacterControl.CanMove = false;
        lastEventData = data;
        InEvent = true;

        eventNameText.text = data.eventName;
        eventImage.sprite = data.eventImage;

        // 生成敘述文字
        fullText = data.eventDescription;
        descriptionText.text = "";

        // 清空選項區
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        typingCoroutine = StartCoroutine(TypingEffect());
    }
    private IEnumerator TypingEffect()
    {
        typing = true;

        foreach (char c in fullText)
        {
            descriptionText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        typing = false;
        yield return GenerateChoices();
    }
    private IEnumerator GenerateChoices() // 生成選擇按鈕
    {
        if (lastEventData == null || choiceButtonPrefab == null || choicesContainer == null) yield break;
        foreach (var choice in lastEventData.choices)
        {
            GameObject btn = Instantiate(choiceButtonPrefab, choicesContainer);

            btn.transform.GetChild(0).GetComponent<TMP_Text>().text = choice.choiceName;
            btn.transform.GetChild(1).GetComponent<TMP_Text>().text = choice.choiceDescription;

            foreach(var entry in choice.eventEffectEntries)
            {
                switch(entry.eventEffect)
                {
                    case EventEffect.OpenNewEvent:
                        if (entry.eventData != null)
                            btn.GetComponent<Button>().onClick.AddListener(() => ShowEvent(entry.eventData));
                        break;
                    case EventEffect.OpenNewDialogue:
                        if (entry.dialogueData != null)
                            btn.GetComponent<Button>().onClick.AddListener(() => StartDialogue(entry.dialogueData));
                        break;
                    case EventEffect.OpenNewBattle:
                        if (entry.battleData != null)
                        {
                            btn.GetComponent<Button>().onClick.
                            AddListener(() => GameModeManager.Instance?.StartStoryBattle(entry.battleData));
                        }
                        break;
                    case EventEffect.ClosureEvent:
                        btn.GetComponent<Button>().onClick.AddListener(() => ClosePanel());
                        break;
                    case EventEffect.GetQuest:
                        if (entry.questData != null)
                        {
                            btn.GetComponent<Button>().onClick.AddListener(() => 
                                QuestManager.Instance.GetQuest(entry.questData));
                        }
                        break;
                    case EventEffect.EndStory:
                        if (entry.storyEndData != null)
                        {
                            btn.GetComponent<Button>().onClick.AddListener(() => 
                                StartCoroutine(ShowStoryEnd(entry.storyEndData)));
                        }
                        break;
                    case EventEffect.GetMenber:
                        if (entry.characterData != null)
                            StoryModeManager.Instance.GetNewMenber(entry.characterData);
                        break;
                }
                if (choice.recordEvent)
                {
                    btn.GetComponent<Button>().onClick.AddListener(() => LogEvent(lastEventData));
                }
            }

            btn.SetActive(true);
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
        eventPanel.SetActive(false);
        InEvent = false;
        GamecharacterControl.CanMove = true;
    }
    void LogEvent(EventData eventData) // 完成事件
    {
        if (!QuestManager.OverEvents.Contains(eventData))
        {
            QuestManager.Instance.OnOverEvent(eventData);
            
        }
    }

    public void TryToStopType() // 點擊後顯示全部
    {
        if (!panel.activeSelf) return;
        if (recordText.gameObject.activeInHierarchy) return;

        StartCoroutine(StraightwayType());
    }
    private IEnumerator StraightwayType()
    {
        yield return null;
        if (recordText.gameObject.activeInHierarchy) yield break;
        if (eventPanel.activeInHierarchy && typing)
        {
            StopCoroutine(typingCoroutine);
            Debug.Log("立即顯示所有介紹");
            descriptionText.text = fullText;
            typing = false;
            StartCoroutine(GenerateChoices());
        }
        else if (DialoguePanel.activeInHierarchy)
        {
            if (typing)
            {
                Debug.Log("立即顯示所有劇情");
                StopCoroutine(typingCoroutine);
                contentText.text = fullText;
                typing = false;
            }
            else
            {
                // 已經打完 → 下一句
                currentIndex++;
                Debug.Log("顯示下一個劇情");
                if (currentIndex < lastDialogueData.lines.Length)
                    ShowLine();
                else
                    EndDialogue();
            }
        }
    }

    #endregion

    #region Dialogue
    // 對話
    public void StartDialogue(DialogueData data)
    {
        lastDialogueData = data;
        currentIndex = 0;
        skipPanel.SetActive(false);

        if (InEvent)
        {
            eventPanel.SetActive(false);
        }
        recordText.text = "";
        skipText.text = data.introduction;
        panel.SetActive(true);
        DialoguePanel.SetActive(true);
        ShowLine();
    }
    private void ShowLine()
    {
        var line = lastDialogueData.lines[currentIndex];

        speakerText.text = line.speaker;
        portraitImage.sprite = line.Illustration;
        fullText = line.content;

        contentText.text = "";
        typingCoroutine = StartCoroutine(TypeEffect());
        recordText.text += $"\n{line.speaker}:\n{line.content}\n";
    }
    private IEnumerator TypeEffect()
    {
        typing = true;

        foreach (char c in fullText)
        {
            contentText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        typing = false;
    }
    public void EndDialogue()
    {
        if (lastDialogueData == null) return;

        panel.SetActive(false);
        DialoguePanel.SetActive(false);
        if (lastDialogueData.recordDialogue)
            QuestManager.Instance.OnOverDialogue(lastDialogueData);
        
        if (InEvent)
        {
            panel.SetActive(true);
            eventPanel.SetActive(true);
        }
        foreach(var entry in lastDialogueData.eventEffectEntries)
        {
            switch(entry.eventEffect)
            {
                case EventEffect.OpenNewEvent:
                    if (entry.eventData != null)
                    {
                        ShowEvent(entry.eventData);
                        return;
                    }
                    break;
                case EventEffect.OpenNewDialogue:
                    if (entry.dialogueData != null)
                    {
                        StartDialogue(entry.dialogueData);
                        return;
                    }
                    break;
                case EventEffect.OpenNewBattle:
                    if (entry.battleData != null)
                    {
                        GameModeManager.Instance?.StartStoryBattle(entry.battleData);
                        return;
                    }
                    break;
                case EventEffect.GetQuest:
                    if (entry.questData != null)
                        QuestManager.Instance.GetQuest(entry.questData);
                    break;
                case EventEffect.EndStory:
                    if (entry.storyEndData != null)
                    {
                        StartCoroutine(ShowStoryEnd(entry.storyEndData));
                    }
                    break;
                case EventEffect.GetMenber:
                    if (entry.characterData != null)
                        StoryModeManager.Instance.GetNewMenber(entry.characterData);
                    break;
            }
        }
        
        lastDialogueData = null;
        // 恢復角色移動
        GamecharacterControl.CanMove = true;
    }

    #endregion

    #region Battle
    // 戰鬥
    public void BattleBegin(EventBattleData data)
    {
        lastEventBattleData = data;
        backgroundMusicSource.volume = 0f;
        panel.SetActive(false);
    }
    public void BattleOverCheck()
    {
        backgroundMusicSource.volume = backgroundMusicVoice;
        if (GameModeManager.Instance.gameMode == GameMode.story)
        {
            if (lastEventBattleData != null)
            {
                if (lastEventBattleData.recordBattle)
                {
                    QuestManager.OverBattles.Add(lastEventBattleData);
                }
                // 經驗值
                int EX = lastEventBattleData.enemys.Length * lastEventBattleData.enemyLevel;
                StoryModeManager.Instance.GetExperience(TurnManager.playerVictory ? EX * 2 : EX);

                foreach(var over in lastEventBattleData.battleOvers)
                {
                    if (over.result == BattleResult.Victory && !TurnManager.playerVictory) return;
                    else if (over.result == BattleResult.Defeat && TurnManager.playerVictory) return;
                    
                    foreach(var entry in over.eventEffectEntries)
                    switch(entry.eventEffect)
                    {
                        case EventEffect.OpenNewEvent:
                            if (entry.eventData != null)
                                ShowEvent(entry.eventData);
                            break;
                        case EventEffect.OpenNewDialogue:
                            if (entry.dialogueData != null)
                                StartDialogue(entry.dialogueData);
                            break;
                        case EventEffect.OpenNewBattle:
                            if (entry.battleData != null)
                                GameModeManager.Instance?.StartStoryBattle(entry.battleData);
                            break;
                        case EventEffect.GetQuest:
                            if (entry.questData != null)
                                QuestManager.Instance.GetQuest(entry.questData);
                            break;
                        case EventEffect.EndStory:
                            if (entry.storyEndData != null)
                            {
                                StartCoroutine(ShowStoryEnd(entry.storyEndData));
                            }
                            break;
                        case EventEffect.GetMenber:
                            if (entry.characterData != null)
                                StoryModeManager.Instance.GetNewMenber(entry.characterData);
                            break;
                    }
                }
            }
        }
    }

    #endregion

    // 聲音
    private void ChangeBackgroundVoice(float value)
    {
        backgroundMusicVoice = Mathf.FloorToInt(value);
        PlayerPrefs.SetInt("backgroundMusicVoice", backgroundMusicVoice);
        float volume = Mathf.Clamp01(backgroundMusicVoice / backgroundMusicVoiceSlider.maxValue);
        backgroundMusicSource.volume = volume;
        backgroundMusicVoiceText.text = $"{backgroundMusicVoice}";
    }

    // 結局
    private IEnumerator ShowStoryEnd(StoryEnd storyEndData)
    {
        GamecharacterControl.CanMove = false;
        panel.gameObject.SetActive(true);
        eventPanel.SetActive(false);
        DialoguePanel.SetActive(false);

        endOverPanel.SetActive(false);
        endOverPanel.transform.GetChild(0).GetComponent<Image>().sprite = storyEndData.endImage;
        endOverPanel.transform.GetChild(1).GetComponent<TMP_Text>().text = $"達成結局:{storyEndData.endName}";
        endOverPanel.transform.GetChild(2).GetComponent<TMP_Text>().text = storyEndData.endIntroduse;

        endPanel.sprite = storyEndData.endImage;
        endPanel.gameObject.SetActive(true);
        endPanel.transform.GetComponent<Button>().enabled = false;

        float duration = 2f;
        float t = 0f;
        Color c = endPanel.color;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / duration);
            endPanel.color = c;
            yield return null;
        }
        
        endPanel.transform.GetComponent<Button>().enabled = true;
    }
}
