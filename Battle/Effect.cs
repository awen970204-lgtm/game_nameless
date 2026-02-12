using UnityEngine;
using System.Collections.Generic;

public enum EffectType
{
    None,
    // 血量相關
    ChangeMaxHP,
    ConsumeHP,
    Damage,
    Heal,
    // 卡牌相關
    DrawCard,
    Discard_Range,
    Discard_Specific,
    // 技能相關
    GetSkill,
    LoseSkill,
    GetPassiveSkill,
    LosePassiveSkill,
    // 角色數值
    ChangeAttackPower,
    ChangeHealPower,
    ChangeDefense,
    ChangeDamageMultiplier,
    ChangeDamageReduction,
    // 取得狀態
    GetContinuedEffect,
    LoseContinuedEffect,
    // 偷手牌
    StealCards_Range,
    StealCards_Specific,
    // 技能失/生效
    InvalidSkill,
    ReplySkill,
    InvalidPassiveSkill,
    ReplyPassiveSkill,
    // 之後繼續擴充
}
public enum TargetValue
{
    None,
    InputValue,
    MaxHealth,
    Health,
    LossesHealth,
    HoldCards,
    HoldContinuedEffect,
}
public enum valueTarget{Initiator, target}
[System.Serializable]
public class ValueEntry
{
    public valueTarget valueTurget;
    public TargetValue targetValue;
    public float multiplier = 1f;
    public ContinuedEffect continuedEffect;
}

public enum SpecialEffects
{
    None,
    OnDamage_Normal,
    OnHeal_Normal,
}

[System.Serializable]
public class EffectEntry
{
    public TargetType targetType;      // 生效的目標
    public bool NeedChoose = false;    // 是否需要玩家手動選擇
    public bool canInputValue = false;
    public int MaxInputValue = 1;
    public bool canCancle = true;      // 是否能取消
    public int maxTargets = 1;         // 目標數上限
    public int minTargets = 0;         // 目標數下限
    public List<Effect> effects;       // 對該目標套用的效果
}
// 效果
[System.Serializable]
public class Effect
{
    public List<NeedState> targetNeeds;
    public EffectType effectType;
    public Skill skill;
    public PassiveSkill passiveSkill;
    public ContinuedEffect continuedEffect;
    public Character character;
    public ValueEntry targetValueEntry;
    public int value = 1;
    public float multiplier = 1f;
    public SpecialEffects special;
}

