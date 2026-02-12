using UnityEngine;

[CreateAssetMenu(menuName = "Event/New Quest Data")]
public class QuestData : ScriptableObject
{
    public string questName;
    public QuestStep[] steps;
}
[System.Serializable]
public class QuestStep
{
    public int stepScene;
    [TextArea]
    public string stepDescription;

    // 完成條件
    public EventData eventData;
    public DialogueData dialogueData;
    public EventBattleData battleData;
    // 獎勵
    public EventEffect eventEffect;
    public Character characterData;
}
