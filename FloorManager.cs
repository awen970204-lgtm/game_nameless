using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Data.Common;
using System.Linq;

// floorNumber
// finishedEventEntry0

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    public List<FloorEventEntry> floorEventEntries = new List<FloorEventEntry>();
    [HideInInspector] public List<FloorEventEntry> finishedEventEntries = new List<FloorEventEntry>();
    [HideInInspector] public FloorEventEntry pendingEventEntry;

    public static int floorNumber = 0;

    [Header("UI")]
    public GameObject backgroundPanel;
    public TMP_Text floorNumberText;
    public GameObject choiceButtunPreb;
    public Transform choiceTransform;

    void Awake()
    {
        if (FloorManager.Instance == null)
            Instance = this;
        else Destroy(this.gameObject);
    }
    void Start()
    {
        backgroundPanel.SetActive(false);
        floorNumberText.gameObject.SetActive(false);
        choiceButtunPreb.SetActive(false);
        choiceTransform.gameObject.SetActive(false);

        if (!PlayerPrefs.HasKey($"floorNumber"))
        {
            PlayerPrefs.SetInt("floorNumber", 0);
        }
    }

    public void GameStart() // 遊戲開始
    {
        floorNumber = 0;
        pendingEventEntry = null;
        finishedEventEntries.Clear();

        PlayerPrefs.SetInt("pendingEventEntry", -1);
        foreach(var eventEntry in floorEventEntries)
        {
            PlayerPrefs.SetInt($"finishedEventEntry"+
                $"{floorEventEntries.IndexOf(eventEntry)}", 0);
        }

        NextFloor();
    }
    
    public void GameContinue() // 遊戲繼續
    {
        floorNumber = PlayerPrefs.GetInt("floorNumber");
        if (floorEventEntries[PlayerPrefs.GetInt("pendingEventEntry")] != null &&
            PlayerPrefs.GetInt("pendingEventEntry") >= 0)
        {
            pendingEventEntry = floorEventEntries[PlayerPrefs.GetInt("pendingEventEntry")];
        }
        foreach(var eventEntry in floorEventEntries)
        {
            if (PlayerPrefs.GetInt($"finishedEventEntry"+
                $"{floorEventEntries.IndexOf(eventEntry)}") == 1)
            {
                finishedEventEntries.Add(eventEntry);
            }
        }
    }

    public void OverFloor() // 完成樓層
    {
        EventUI.Instance.ClosePanel();
        if (pendingEventEntry != null)
        {
            finishedEventEntries.Add(pendingEventEntry);
        }
        NextFloor();
    }

    private void NextFloor()
    {
        floorNumber += 1;
        LoadingFloor(floorNumber);
    }

    private void LoadingFloor(int nowFloor) // 載入樓層
    {
        List<FloorEventEntry> triggerableEvents = new List<FloorEventEntry>
            (floorEventEntries.Where(e => !finishedEventEntries.Contains(e) && 
                e.condition.IsSatisfied(nowFloor)).ToList());
            
        List<FloorEventEntry> finalEvents = new List<FloorEventEntry>();

        if (triggerableEvents.Any(e => e.fixedFloor && e.nodeIndex == nowFloor))
        {
            finalEvents = RollDifferentCategoryEvents(
                triggerableEvents.FindAll(e => e.fixedFloor && e.nodeIndex == nowFloor), 3);
        }
        else
        {
            finalEvents = RollDifferentCategoryEvents(
                triggerableEvents.FindAll(e => !e.fixedFloor), 3);
        }

        foreach (var eventEntry in finalEvents)
        {
            if (eventEntry == null) continue;

            GameObject go = Instantiate(choiceButtunPreb, choiceTransform);
            go.GetComponent<FloorChoiceCtrl>()?.SetEvent(eventEntry);

            go.SetActive(true);
        }

        floorNumberText.text = $"day {nowFloor}";

        backgroundPanel.SetActive(true);
        floorNumberText.gameObject.SetActive(true);
        choiceTransform.gameObject.SetActive(true);
    }

    private static List<FloorEventEntry> RollDifferentCategoryEvents
        (List<FloorEventEntry> availableEntries, int count)
    {
        List<FloorEventEntry> results = new List<FloorEventEntry>();
        if (availableEntries == null || availableEntries.Count == 0 || count <= 0) 
            return results;

        // 建立一份副本，避免抽取時改動到原本的清單
        List<FloorEventEntry> pool = new List<FloorEventEntry>(availableEntries);
        
        // 記錄已經被抽過的分類，用來確保「不同種類型」
        HashSet<FloorEventCategory> selectedCategories = new HashSet<FloorEventCategory>();

        for (int i = 0; i < count; i++)
        {
            // 從池子中排除「已經被選過的分類」的事件
            List<FloorEventEntry> validPool = pool.FindAll(e => !selectedCategories.Contains(e.category));

            // 如果沒有符合條件的事件可抽了，就提早結束
            if (validPool.Count == 0) break;

            // 進行權重抽樣
            FloorEventEntry rolledEntry = RollByWeight(validPool);
            if (rolledEntry != null)
            {
                results.Add(rolledEntry);
                selectedCategories.Add(rolledEntry.category); // 記錄此分類已使用
                pool.Remove(rolledEntry); // 從候選池移除，避免同一次抽樣重複抽到同一個事件
            }
        }

        return results;
    }

    // 根據權重隨機抽取單一事件
    private static FloorEventEntry RollByWeight(List<FloorEventEntry> entries)
    {
        int totalWeight = 0;
        foreach (var entry in entries)
        {
            totalWeight += Mathf.Max(0, entry.weight);
        }

        if (totalWeight <= 0) return entries[Random.Range(0, entries.Count)];

        int randomPoint = Random.Range(0, totalWeight);
        int currentSum = 0;

        foreach (var entry in entries)
        {
            currentSum += Mathf.Max(0, entry.weight);
            if (randomPoint < currentSum)
            {
                return entry;
            }
        }

        return entries[entries.Count - 1];
    }

    public void CompletedSelectionEvent(FloorEventEntry eventEntry) // 選擇完樓層事件
    {
        foreach (Transform child in choiceTransform)
            Destroy(child.gameObject);

        backgroundPanel.SetActive(false);
        floorNumberText.gameObject.SetActive(false);
        choiceTransform.gameObject.SetActive(false);

        EventUI.Instance.ShowEvent(eventEntry.targetEventData);
    }
}
