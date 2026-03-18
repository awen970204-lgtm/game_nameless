using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Event/Battle")]
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
    public List<EventEffectEntry> eventEffectEntries;
}
public enum BattleResult
{
    None,
    Victory,
    Defeat,
}

