using UnityEngine;

[CreateAssetMenu(menuName = "Event/New Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speaker; // 說話者
        public Sprite Illustration;
        [TextArea] 
        public string content; // 對話內容
    }

    public DialogueLine[] lines;
    [TextArea]
    public string introduction;
    
    public bool recordDialogue = true;
    public EventEffect DialogueOver;
    public EventData ContinuedEventData;
    public DialogueData ContinuedDialogueData;
    public EventBattleData ContinuedBattleData;
}