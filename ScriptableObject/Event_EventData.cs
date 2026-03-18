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

    public EventData eventData;
    public DialogueData dialogueData;
    public EventBattleData battleData;
    public QuestData questData;
    public Character characterData;
    public StoryEnd storyEndData;
}
public enum EventEffect
{
    None,
    OpenNewEvent,
    OpenNewDialogue,
    OpenNewBattle,
    ClosureEvent,
    GetQuest,
    EndStory,
    // 遊戲資源
    GetMenber,

}


