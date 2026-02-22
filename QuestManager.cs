using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Data.Common;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    public static bool AutoAcceptQuest = false;

    [Header("UI")]
    public Button openQuestButton;
    public Button closeQuestButton;
    public TMP_Text questName;
    public TMP_Text questDescription;

    public GameObject questPrefabs;
    public Transform questsTransform;
    public GameObject questsPanel;

    public Toggle AutoAcceptQuest_Toggle;

    [Header("Data")]
    public DialogueData defaultDialogue;
    public QuestData defaultQuest;

    public static List<QuestData> MainQuests = new List<QuestData>();
    private Dictionary<QuestData, int> MainQuestShedules = new Dictionary<QuestData, int>();

    public static List<QuestData> OverQuests = new List<QuestData>();
    public static List<EventData> OverEvents = new List<EventData>();
    public static List<DialogueData> OverDialogues = new List<DialogueData>();
    public static List<EventBattleData> OverBattles = new List<EventBattleData>();

    public static QuestData currentQuest;
    public static QuestStep currentStep;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else this.enabled = false;
    }
    void Start()
    {
        openQuestButton.gameObject.SetActive(true);
        closeQuestButton.gameObject.SetActive(false);

        if (!PlayerPrefs.HasKey("AutoAcceptQuest"))
        {
            PlayerPrefs.SetInt("AutoAcceptQuest", 1);
        }
        bool auto = PlayerPrefs.GetInt("AutoAcceptQuest") == 1;
        AutoAcceptQuest_Toggle.SetIsOnWithoutNotify(auto);
        AutoAcceptQuest = auto;

        AutoAcceptQuest_Toggle.onValueChanged.AddListener(ChangeAutoAccept);
        openQuestButton.onClick.AddListener(()=> OnClickQuestButton());
        closeQuestButton.onClick.AddListener(()=> OnClickQuestButton());
    }
    private void ChangeAutoAccept(bool auto) // 設定自動接取
    {
        AutoAcceptQuest_Toggle.SetIsOnWithoutNotify(auto);
        AutoAcceptQuest = auto;

        PlayerPrefs.SetInt("AutoAcceptQuest", auto ? 1 : 0);
    }
    private void OnClickQuestButton()
    {
        StoryModeManager.Instance.teamPanel.SetActive(false);
        
        openQuestButton.gameObject.SetActive(questsPanel.activeInHierarchy);
        closeQuestButton.gameObject.SetActive(!questsPanel.activeInHierarchy);
        questsPanel.SetActive(!questsPanel.activeInHierarchy);
    }

    #region Set Story Mode

    public void SetStoryMode()
    {
        ClearRecords();
        GetQuest(defaultQuest);
        SetCurrentQuest(defaultQuest);
        EventUI.Instance.StartDialogue(defaultDialogue);
    }

    private void ClearRecords() // 清空紀錄
    {
        MainQuests.Clear();

        OverQuests.Clear();
        OverEvents.Clear();
        OverDialogues.Clear();
        OverBattles.Clear();

        currentQuest = null;
        currentStep = null;

        questName.text = "";
        questDescription.text = "";

        foreach(Transform child in questsTransform)
            Destroy(child.gameObject);
    }

    #endregion

    #region Quest

    public void GetQuest(QuestData quest) // 取得任務
    {
        if (quest == null) return;
        if (MainQuests.Contains(quest)) return;

        MainQuests.Add(quest);
        MainQuestShedules.Add(quest, 0);

        GameObject questGO = Instantiate(questPrefabs, questsTransform);
        questGO.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(()=> SetCurrentQuest(quest));
        questGO.transform.GetChild(1).GetComponent<TMP_Text>().text = quest.questName;
        questGO.transform.GetChild(2).GetComponent<TMP_Text>().text = quest.steps[0].stepDescription;
        questGO.SetActive(true);
        
        StartCoroutine(StoryModeManager.Instance.ShowActivity(quest.questIcon, "獲得任務", $"{quest.questName}"));
    }

    private void SetCurrentQuest(QuestData quest) // 設定任務
    {
        if (quest == null) return;
        if (!MainQuests.Contains(quest)) return;
        if (!MainQuestShedules.ContainsKey(quest)) return;

        currentQuest = quest;
        questName.text = quest.questName;

        if (MainQuestShedules[quest] < quest.steps.Length)
        {
            currentStep =  quest.steps[MainQuestShedules[quest]];
            questDescription.text = quest.steps[MainQuestShedules[quest]].stepDescription;
        }
        else OverQuest(quest);
    }

    private bool questStepOver(QuestData quest)
    {
        if ((OverEvents.Contains(quest.steps[MainQuestShedules[quest]].eventData) || 
                    currentStep.eventData == null) && 
                (OverDialogues.Contains(quest.steps[MainQuestShedules[quest]].dialogueData) || 
                    currentStep.dialogueData == null) && 
                (OverBattles.Contains(quest.steps[MainQuestShedules[quest]].battleData) || 
                    currentStep.battleData == null)) return true;

        return false;
    }

    public void OnOverEvent(EventData eventData)
    {
        if (OverEvents.Contains(eventData)) return;

        OverEvents.Add(eventData);

        CheckQuestStep();
    }
    public void OnOverDialogue(DialogueData dialogueData)
    {
        if (OverDialogues.Contains(dialogueData)) return;

        OverDialogues.Add(dialogueData);

        CheckQuestStep();
    }
    private void CheckQuestStep()
    {
        foreach(var quest in MainQuests)
        {
            if (questStepOver(quest))
            {
                var step = quest.steps[MainQuestShedules[quest]];
                switch(step.eventEffect)
                {
                    case EventEffect.OpenNewEvent:
                        if (step.eventData != null)
                            EventUI.Instance.ShowEvent(step.eventData);
                        break;
                    case EventEffect.OpenNewDialogue:
                        if (step.dialogueData != null)
                            EventUI.Instance.StartDialogue(step.dialogueData);
                        break;
                    case EventEffect.OpenNewBattle:
                        if (step.battleData != null)
                            GameModeManager.Instance?.StartStoryBattle(step.battleData);
                        break;
                    case EventEffect.ClosureEvent:
                        EventUI.Instance?.ClosePanel();
                        break;
                    case EventEffect.GetMenber:
                        if (step.characterData != null)
                            StoryModeManager.Instance.GetNewMenber(step.characterData);
                        break;
                }
            }
        }
    }

    public void CheckQuestShedule() // 確認任務進度
    {
        if (currentQuest == null || currentStep == null) return;

        foreach(var quest in MainQuests)
        {
            if (questStepOver(quest))
            {
                MainQuestShedules[quest] ++;

                TMP_Text target = questsTransform
                    .GetComponentsInChildren<TMP_Text>(true)   // true = 包含 inactive
                    .FirstOrDefault(t => t.text == quest.questName);

                if (target == null)
                {
                    Debug.LogWarning($"找不到任務：{quest.questName}");
                    continue;
                }

                target.transform.parent.GetChild(2).GetComponent<TMP_Text>().text 
                    = quest.steps[MainQuestShedules[quest]].stepDescription;

            }
        }

        SetCurrentQuest(currentQuest);
        
    }

    private void OverQuest(QuestData quest)
    {
        if (quest == null) return;
        
        OverQuests.Add(quest);
        MainQuests.Remove(quest);
        MainQuestShedules.Remove(quest);

        questName.text = "";
        questDescription.text = "";

        currentQuest = null;
        currentStep = null;

        Destroy(questsTransform
                    .GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(t => t.text == quest.questName)?.transform.parent.gameObject);

        if (AutoAcceptQuest && MainQuests.Count > 0)
        {
            SetCurrentQuest(MainQuests[0]);
        }
    }

    #endregion
}
