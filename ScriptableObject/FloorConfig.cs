using System;
using System.Collections.Generic;
using UnityEngine;

// 樓層事件分類
public enum FloorEventCategory
{
    NonCombat,      // 無戰鬥
    NormalBattle,   // 一般戰鬥
    HardBattle,     // 困難戰鬥
    Shop,           // 商店
    Rest,           // 休息點
    Boss            // BOSS 戰
}

// 事件前置條件檢核
[Serializable]
public class FloorEventCondition
{
    [Header("樓層限制")]
    public int minFloorLevel = 1;

    [Header("任務/紀錄 前置需求")]
    public List<QuestData> requiredQuests;       // 必須任務
    public List<QuestData> requiredOverQuests;   // 必須已完成的任務
    public List<EventData> requiredOverEvents;   // 必須已觸發過的事件
    public List<EventBattleData> requiredOverBattles; // 必須戰鬥

    // 檢查當前條件是否滿足
    public bool IsSatisfied(int currentFloor)
    {
        // 1. 樓層檢查
        if (currentFloor < minFloorLevel) return false;

        // 2. 必須接取過任務檢查
        if (requiredQuests != null)
        {
            foreach (var q in requiredQuests)
                if (!QuestManager.MainQuests.Contains(q)) return false;
        }

        // 3. 必須完成任務檢查
        if (requiredOverQuests != null)
        {
            foreach (var q in requiredOverQuests)
                if (!QuestManager.OverQuests.Contains(q)) return false;
        }

        // 4. 必須完成事件檢查
        if (requiredOverEvents != null)
        {
            foreach (var e in requiredOverEvents)
                if (!QuestManager.OverEvents.Contains(e)) return false;
        }

        // 5. 必須完成戰鬥檢查
        if (requiredOverBattles != null)
        {
            foreach (var b in requiredOverBattles)
                if (!QuestManager.OverBattles.Contains(b)) return false;
        }

        return true;
    }
}

// 事件包裝
[CreateAssetMenu(menuName = "Event/FloorEvent")]
public class FloorEventEntry : ScriptableObject
{
    public string entryName;
    public bool fixedFloor;
    public int nodeIndex;    // 樓層位置

    public FloorEventCategory category;         // 事件分類
    public EventData targetEventData;           // 觸發對象
    public int weight = 10;                     // 隨機抽樣權重
    public FloorEventCondition condition;       // 前置條件
}
