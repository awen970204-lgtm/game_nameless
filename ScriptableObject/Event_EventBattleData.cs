using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Event/New Battle Data")]
public class EventBattleData : ScriptableObject
{
    public bool FixedLevel = false;
    public bool Fixedteammate = false;
    public Character[] teammates;
    public int teammateLevel;

    public Character[] enemys;
    public int enemyLevel;
    public Card[] enemyCards;

    public bool recordBattle = true;
    public BattleOver[] battleOvers;
}

[System.Serializable]
public class BattleOver
{
    public BattleResult result;

    public EventEffect Effect;
    
    public EventData ContinuedEventData;
    public DialogueData ContinuedDialogueData;
    public EventBattleData ContinuedBattleData;
}
public enum BattleResult
{
    None,
    Victory,
    Defeat,
}

