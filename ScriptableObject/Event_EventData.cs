using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Event/Event")]
public class EventData : ScriptableObject
{
    public string eventName;
    public Sprite eventImage;
    [TextArea(3,10)]
    public string eventDescription;

    [Header("Choices")]
    public List<Choices> choices;
}
[System.Serializable]
public class Choices
{
    public string choiceName;        // 選項名稱
    public string choiceDescription; // 選項介紹
    public bool recordEvent = true;
    public List<EventEffectEntry> eventEffectEntries;
}
[System.Serializable]
public class EventEffectEntry
{
    public EventEffect eventEffect;
    public int value;

    public EventData eventData;
    public DialogueData dialogueData;
    public EventBattleData battleData;
    public QuestData questData;
    public Character characterData;
    public StoryEnd storyEndData;
    public Item itemData;
}
public enum EventEffect
{
    None,
    OpenNewEvent,
    OpenNewDialogue,
    OpenNewBattle,
    ClosureEvent,
    GetQuest,
    OverFloor,
    EndStory,
    // 遊戲資源
    GetMenber,
    GetItem,
}
// Event display
public enum EventCategory
{
    NonCombat,      // 無戰鬥 / 純敘事
    NormalBattle,   // 一般戰鬥
    HardBattle,     // 困難戰鬥 / 精英
    Shop,           // 商店
    Rest,           // 休息點
    Boss            // BOSS 戰
}

