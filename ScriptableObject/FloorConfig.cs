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
    public int maxFloorLevel = 999;

    [Header("要有的 任務/紀錄 前置需求")]
    public List<QuestData> reqQuests;           // 進行中的任務
    public List<QuestData> reqCompletedQuests;  // 已完成的任務
    public List<EventData> reqCompletedEvents;  // 已觸發過的事件
    public List<EventBattleData> reqCompletedBattles; // 已完成的戰鬥

    [Header("不能有的 任務/紀錄 前置需求")]
    public List<QuestData> excludeQuests;           // 不能進行中的任務
    public List<QuestData> excludeCompletedQuests;  // 不能已完成的任務
    public List<EventData> excludeCompletedEvents;  // 不能已觸發過的事件
    public List<EventBattleData> excludeCompletedBattles; // 不能已完成的戰鬥

    // 檢查當前條件是否滿足
    public bool IsSatisfied(int currentFloor)
    {
        // 1. 樓層檢查
        if (currentFloor <= minFloorLevel && currentFloor >= maxFloorLevel) 
            return false;

        // 2. 接取過任務檢查
        if (reqQuests != null)
        {
            foreach (var q in reqQuests)
                if (!QuestManager.MainQuests.Contains(q)) return false;
        }
        if (excludeQuests != null)
        {
            foreach(var q in excludeQuests)
                if (QuestManager.MainQuests.Contains(q)) return false;
        }

        // 3. 完成任務檢查
        if (reqCompletedQuests != null)
        {
            foreach (var q in reqCompletedQuests)
                if (!QuestManager.OverQuests.Contains(q)) return false;
        }
        if (excludeCompletedQuests != null)
        {
            foreach (var q in excludeCompletedQuests)
                if (QuestManager.OverQuests.Contains(q)) return false;
        }

        // 4. 完成事件檢查
        if (reqCompletedEvents != null)
        {
            foreach (var e in reqCompletedEvents)
                if (!QuestManager.OverEvents.Contains(e)) return false;
        }
        if (excludeCompletedEvents != null)
        {
            foreach (var e in excludeCompletedEvents)
                if (QuestManager.OverEvents.Contains(e)) return false;
        }

        // 5. 完成戰鬥檢查
        if (reqCompletedBattles != null)
        {
            foreach (var b in reqCompletedBattles)
                if (!QuestManager.OverBattles.Contains(b)) return false;
        }
        if (excludeCompletedBattles != null)
        {
            foreach (var b in excludeCompletedBattles)
                if (QuestManager.OverBattles.Contains(b)) return false;
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
