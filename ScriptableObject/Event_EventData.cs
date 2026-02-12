using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Event/New Event Data")]
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
    public EventEffect eventEffect;
    public bool recordEvent = true;

    public EventData eventData;
    public DialogueData dialogueData;
    public EventBattleData battleData;

    public bool getQuest = false;
    public QuestData questData;
}
public enum EventEffect
{
    None,
    OpenNewEvent,
    OpenNewDialogue,
    OpenNewBattle,
    ClosureEvent,
    BegetEnd,
    // 遊戲資源
    GetMenber,

}


