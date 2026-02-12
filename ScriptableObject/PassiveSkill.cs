using UnityEngine;
using System.Collections.Generic;

// 觸發角色
public enum Trigger_Character
{
    None,
    Self,
    Other,
    All,
    teammate,
    enemy,
    // 繼續添加...
}
// 觸發時機
public enum TriggerTime
{
    None,
    OnTurnStart,
    OnTurnEnd,
    OnAttact,
    OnBeAttacted,
    OnBeHealed,
    OnCharacterDeath,
    OnCardPlayed,
    OnRealTurnEnd,
    OnGetEffect,
    OnLoseEffect,
    OnConsumeHP,
    OnBattleStart,
    // 繼續添加...
}

// 時機組合
[System.Serializable]
public class PassiveTime
{
    public Trigger_Character trigger;    // 觸發時機對象
    public TriggerTime triggerTime;      // 技能時機
    public List<NeedState> passiveNeed;  // 發動前提
}
// 觸發條件
[System.Serializable]
public class NeedState
{
    public valueTarget valueTarget;
    public Limit limit;
    public Skill skill;
    public PassiveSkill passiveSkill;
    public ContinuedEffect continuedEffect;
    public ValueEntry max_targetValue;
    public int maxLimit;
    public ValueEntry min_targetValue;
    public int minLimit;
}
// 被動總組合
[System.Serializable]
public class PassiveEntry
{
    public List<PassiveTime> passiveTimes;        // 發動時機
    public List<EffectEntry> effectEntries = new List<EffectEntry>();  // 發動效果
}

[CreateAssetMenu(menuName = "BattleGameObjects/PassiveSkill")]
[System.Serializable]
public class PassiveSkill : ScriptableObject
{
    public string skillName;                 // 技能名
    [TextArea]
    public string skillEffect;
    public bool LimitedTimes = true;         // 是否限制次數
    public int maxTriggersPerTurn = 1;       // 總次數
    public List<PassiveEntry> passiveEntry;  // 被動總組合
}
