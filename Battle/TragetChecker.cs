using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 目標類型
public enum TargetType
{
    Self,
    Other,
    AllOther,
    All,
    Any,
    EventTrigger,
    teammate,
    enemy,
}
// 限制效果
public enum Limit
{
    // 狀態類
    selfHealth,
    selfMaxHP,
    // 傷害類
    MaxCauseDamage,
    MinCauseDamage,
    MaxAffectedDamage,
    MinAffectedDamage,
    // 持有類
    holdSkill_self,
    NotHoldSkill_self,
    holdPassiveSkill_self,
    NotHoldPassiveSkill_self,
    // 手牌數
    holdCards_Max,
    holdCards_Min,
    holdCards_Now,
    holdCards_Delay,
    holdCards_Wait,
    // 單回合
    MaxAttackValueInTrun,
    MinAttackValueInTrun,
    MaxDamageValueInTrun,
    MinDamageValueInTrun,
    MaxHealValueInTrun,
    MinHealValueInTrun,
    MaxDrawCardsInTrun,
    MinDrawCardsInTrun,
    MaxUseCardTimesInTrun,
    MinUseCardTimesInTrun,
    MaxUseSkillTimesInTrun,
    MinUseSkillTimesInTrun,
    // 持續效果
    holdContinueEffect,
    NotholdContinueEffect,
    ContinueEffect_StackTimes,
    ContinueEffect_MaxStackTimes,
    ContinueEffect_MinStackTimes,
}
// 檢查限制
public static class LimitChecker
{
    public static bool CheckLimit(NeedState need, CharacterHealth trigger, CharacterHealth acting)
    {
        CharacterHealth ch = null;
        switch (need.valueTarget)
        {
            case valueTarget.Initiator:
                ch = acting;
                break;
            case valueTarget.target:
                ch = trigger;
                break;
        }
        if (ch = null) return false;

        int MinLimit = EffectExecutor.GetValue(need.min_targetValue, trigger, acting) + need.minLimit;
        int MaxLimit = EffectExecutor.GetValue(need.max_targetValue, trigger, acting) + need.maxLimit;
        var effects = ch.effectCtrl.activeEffects
            .Where(e => e.EffectName == need.continuedEffect.EffectName);
        int totalStack = effects.Sum(e => e.stack);
        switch (need.limit)
        {
            // 狀態類
            case Limit.selfHealth:
                if (ch.currentHealth >= MinLimit && ch.currentHealth <= MaxLimit)return(true);
                break;
            case Limit.selfMaxHP:
                if (ch.currentMaxHP >= MinLimit && ch.currentMaxHP <= MaxLimit)return(true);
                break;
            
            // 傷害類
            case Limit.MaxCauseDamage:
                if (ch.lastAttackDamage <= MaxLimit)return(true);
                break;
            case Limit.MinCauseDamage:
                if (ch.lastAttackDamage >= MinLimit)return(true);
                break;
            case Limit.MaxAffectedDamage:
                if (ch.lastBeAttackedDamage <= MaxLimit)return(true);
                break;
            case Limit.MinAffectedDamage:
                if (ch.lastBeAttackedDamage >= MinLimit)return(true);
                break;
            case Limit.MaxAttackValueInTrun:
                if (ch.AttackValueInTrun <= MaxLimit)return(true);
                break;
            case Limit.MinAttackValueInTrun:
                if (ch.AttackValueInTrun >= MinLimit)return(true);
                break;
            case Limit.MaxDamageValueInTrun:
                if (ch.DamageValueInTrun <= MaxLimit)return(true);
                break;
            case Limit.MinDamageValueInTrun:
                if (ch.DamageValueInTrun >= MinLimit)return(true);
                break;
            case Limit.MaxHealValueInTrun:
                if (ch.HealValueInTrun <= MaxLimit)return(true);
                break;
            case Limit.MinHealValueInTrun:
                if (ch.HealValueInTrun >= MinLimit)return(true);
                break;
            case Limit.MaxDrawCardsInTrun:
                if (ch.ownerPlayer.DrawCardsInTrun <= MaxLimit)return(true);
                break;
            case Limit.MinDrawCardsInTrun:
                if (ch.ownerPlayer.DrawCardsInTrun >= MinLimit)return(true);
                break;
            case Limit.MaxUseCardTimesInTrun:
                // if (ch.ownerPlayer.UseCardTimesInTrun <= MaxLimit)return(true);
                break;
            case Limit.MinUseCardTimesInTrun:
                // if (ch.ownerPlayer.UseCardTimesInTrun >= MinLimit)return(true);
                break;
            case Limit.MaxUseSkillTimesInTrun:
                if (ch.UseSkillTimesInTrun <= MaxLimit)return(true);
                break;
            case Limit.MinUseSkillTimesInTrun:
                if (ch.UseSkillTimesInTrun >= MinLimit)return(true);
                break;
            
            // 持有類
            case Limit.holdSkill_self:
                if (ch.currentSkills.Contains(need.skill))return(true);
                break;
            case Limit.NotHoldSkill_self:
                if (!ch.currentSkills.Contains(need.skill))return(true);
                break;
            case Limit.holdPassiveSkill_self:
                if (ch.currentPassiveSkills.Contains(need.passiveSkill))return(true);
                break;
            case Limit.NotHoldPassiveSkill_self:
                if (!ch.currentPassiveSkills.Contains(need.passiveSkill))return(true);
                break;

            case Limit.holdContinueEffect:
                if (ch.effectCtrl.activeEffects.Any(e => e.EffectName == need.continuedEffect.EffectName))
                    return true;
                break;
            case Limit.NotholdContinueEffect:
                if (ch.effectCtrl.activeEffects.All(e => e.EffectName != need.continuedEffect.EffectName))
                    return true;
                break;
            case Limit.ContinueEffect_StackTimes:
                if (totalStack == MaxLimit || totalStack == MinLimit) return true;
                break;
            case Limit.ContinueEffect_MaxStackTimes:
                if (totalStack <= MaxLimit)return true;
                break;
            case Limit.ContinueEffect_MinStackTimes:
                if (totalStack >= MinLimit)return true;
                break;
            
            // 卡牌
            case Limit.holdCards_Max:
                if (ch.ownerPlayer.hand.Count <= MaxLimit)return(true);
                break;
            case Limit.holdCards_Min:
                if (ch.ownerPlayer.hand.Count >= MinLimit)return(true);
                break;
            case Limit.holdCards_Now:
                foreach (var card in ch.ownerPlayer.hand)
                {
                    if (card.cardType == Card.CARD_TYPE.NOW)
                    {
                        return(true);
                    }
                }
                break;
            case Limit.holdCards_Delay:
                foreach (var card in ch.ownerPlayer.hand)
                {
                    if (card.cardType == Card.CARD_TYPE.DELAY)
                    {
                        return(true);
                    }
                }
                break;
            case Limit.holdCards_Wait:
                foreach (var card in ch.ownerPlayer.hand)
                {
                    if (card.cardType == Card.CARD_TYPE.WAIT)
                    {
                        return(true);
                    }
                }
                break;
        }
        return false;   
    }
}
// 檢查最小目標數及目標
public static class TragetChecker
{
    // 主動技能
    public static bool SkillCheckTarget(EffectEntry entry, List<CharacterHealth> targets, CharacterHealth user, Skill skill)
    {
        if (skill != null && entry != null)  
        {
            if (entry.targetType == TargetType.Self && (!targets.Contains(user) || targets.Any(c => c != user)))
            {
                LogWarning.Instance.Warning($"{skill.skillName}需要指定自己");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }
            else if (entry.targetType == TargetType.Other && targets.Contains(user))
            {
                LogWarning.Instance.Warning($"{skill.skillName}不能指定自己");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }
            else if (entry.targetType == TargetType.teammate && targets.Any(c => c.team != user.team))
            {
                LogWarning.Instance.Warning($"{skill.skillName}只能指定隊友");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }
            else if (entry.targetType == TargetType.enemy && targets.Any(c => c.team == user.team))
            {
                LogWarning.Instance.Warning($"{skill.skillName}只能指定敵人");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }

            // 目標數確認
            if (targets.Count < entry.minTargets)
            {
                LogWarning.Instance.Warning($"{skill.skillName}未達到最小目標數");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }
            return(true);
        }
        return(false);
    }
    // 被動技能
    public static bool PassiveSkillCheckTarget(EffectEntry entry, List<CharacterHealth> targets, CharacterHealth user, PassiveSkill skill)
    {
        if (entry != null && TurnManager.Instance.pendingPassiveCtrl != null)  
        {
            if (entry.targetType == TargetType.Self && (!targets.Contains(user) || targets.Any(c => c != user)))
            {
                LogWarning.Instance?.Warning($"{skill.skillName}需要指定自己");
                TurnManager.Instance.checkButton.SetActive(true);
                return false;
            }
            else if (entry.targetType == TargetType.Other && targets.Contains(user))
            {
                LogWarning.Instance?.Warning($"{skill.skillName}不能指定自己");
                TurnManager.Instance.checkButton.SetActive(true);
                return false;
            }
            else if (entry.targetType == TargetType.teammate && targets.Any(c => c.team != user.team))
            {
                LogWarning.Instance?.Warning($"{skill.skillName}只能指定隊友");
                TurnManager.Instance.checkButton.SetActive(true);
                return false;
            }
            else if (entry.targetType == TargetType.enemy && targets.Any(c => c.team == user.team))
            {
                LogWarning.Instance?.Warning($"{skill.skillName}只能指定敵人");
                TurnManager.Instance.checkButton.SetActive(true);
                return false;
            }

            // 目標數確認
            if (targets.Count < entry.minTargets)
            {
                LogWarning.Instance?.Warning($"{skill.skillName}未達到最小目標數");
                TurnManager.Instance.checkButton.SetActive(true);
                return false;
            }
            return true;
        }
        return false;
    }
    // 卡片
    public static bool CardCheckTarget(EffectEntry entry, List<CharacterHealth> targets, CharacterHealth user, Card card)
    {
        if (card != null && user != null)  
        {
            if (entry.targetType == TargetType.Self && !targets.Contains(user))
            {
                LogWarning.Instance?.Warning($"{card.cardName}需要指定自己");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }
            else if (entry.targetType == TargetType.Other && targets.Contains(user))
            {
                LogWarning.Instance?.Warning($"{card.cardName}不能指定自己");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }
            else if (entry.targetType == TargetType.EventTrigger && !targets.Contains(WaitCardManager.Instance.currentEvent.actor))
            {
                LogWarning.Instance?.Warning($"{card.cardName}需要指定觸發者");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }
            else if (entry.targetType == TargetType.teammate && targets.Any(c => c.team != user.team))
            {
                LogWarning.Instance?.Warning($"{card.cardName}只能指定隊友");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }
            else if (entry.targetType == TargetType.enemy && targets.Any(c => c.team == user.team))
            {
                LogWarning.Instance?.Warning($"{card.cardName}只能指定敵人");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }

            // 目標數確認
            if(targets.Count < entry.minTargets)
            {
                LogWarning.Instance?.Warning($"{card.cardName}未達到最小目標數");
                TurnManager.Instance.checkButton.SetActive(true);
                return(false);
            }
            return(true);
        }
        return(false);
    }

}
