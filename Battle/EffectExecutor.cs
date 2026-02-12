using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public static class EffectExecutor
{
    public static IEnumerator ApplyEffects(CharacterHealth user, List<CharacterHealth> targets, List<Effect> effects)
    {
        // 建立副本避免被修改
        var targetsCopy = new List<CharacterHealth>(targets);
        var effectsCopy = new List<Effect>(effects);

        foreach (var target in targetsCopy)
        {
            foreach (var effect in effectsCopy)
            {
                yield return ExecuteEffectCoroutine(user, target, effect);
            }
        }
    }

    private static IEnumerator ExecuteEffectCoroutine(CharacterHealth user, CharacterHealth target, Effect effect)
    {
        foreach(var need in effect.targetNeeds)
        {
            if (!LimitChecker.CheckLimit(need, target, user)) yield break;
        }
        float changeValue = effect.value * effect.multiplier;
        float targetvalue = GetValue(effect.targetValueEntry, user, target);
        int value = Mathf.RoundToInt(changeValue + targetvalue);
        switch (effect.effectType)
        {
            // 血量相關
            case EffectType.ChangeMaxHP:
                target.ChangeMaxHP(value, effect.special);
                break;
            case EffectType.ConsumeHP:
                target.ConsumeHP(value, effect.special);
                break;
            case EffectType.Damage:
                float finalDamage = value + user.currentAttackPower - target.currentDefense;
                    finalDamage *= user.currentDamageMultiplier;
                    finalDamage -= target.currentDamageReduction;
                int damage = Mathf.Max(0, Mathf.RoundToInt(finalDamage));
                yield return user.ReadyToAttact(damage, target, effect.special);
                break;
            case EffectType.Heal:
                int heal = Mathf.Max(0, Mathf.RoundToInt(value + user.currentHealPower));
                target.Heal(heal, effect.special);
                break;
            
            // 手牌相關
            case EffectType.DrawCard:
                target.ownerPlayer?.DrawCard(Mathf.Max(0, Mathf.RoundToInt(value)));
                break;
            case EffectType.Discard_Range:
                target.ownerPlayer?.DiscardRandomCards(value);
                break;
            case EffectType.Discard_Specific:
                target.ownerPlayer?.DiscardSpecificCard(user.ownerPlayer, value);
                break;
            case EffectType.StealCards_Range:
                user.ownerPlayer?.StealCardsFrom(target.ownerPlayer, value);
                break;
            case EffectType.StealCards_Specific:
                target.ownerPlayer?.StartBeStealCards(user.ownerPlayer, value);
                break;

            // 技能相關
            case EffectType.GetSkill:
                if (effect.skill != null)
                    target.GetSkill(effect.skill);
                break;
            case EffectType.LoseSkill:
                if (effect.skill != null)
                    target.LoseSkill(effect.skill);
                break;
            case EffectType.InvalidSkill:
                if (effect.skill != null)
                    target.InvalidSkill(effect.skill);
                break;
            case EffectType.ReplySkill:
                if (effect.skill != null)
                    target.ReplySkill(effect.skill);
                break;
            case EffectType.GetPassiveSkill:
                if (effect.passiveSkill != null)
                    target.GetPassiveSkill(effect.passiveSkill);
                break;
            case EffectType.LosePassiveSkill:
                if (effect.passiveSkill != null)
                    target.LosePassiveSkill(effect.passiveSkill);
                break;
            case EffectType.InvalidPassiveSkill:
                if (effect.passiveSkill != null)
                    target.InvalidPassiveSkill(effect.passiveSkill);
                break;
            case EffectType.ReplyPassiveSkill:
                if (effect.passiveSkill != null)
                    target.ReplyPassiveSkill(effect.passiveSkill);
                break;
            // 角色數值
            case EffectType.ChangeAttackPower:
                target.currentAttackPower += value;
                target.AttackPowerText.text = $"{target.currentAttackPower}";
                break;
            case EffectType.ChangeHealPower:
                target.currentHealPower += value;
                break;
            case EffectType.ChangeDefense:
                target.currentDefense += value;
                target.DefenseText.text = $"{target.currentDefense}";
                break;
            case EffectType.ChangeDamageMultiplier:
                target.currentDamageMultiplier += value;
                break;
            case EffectType.ChangeDamageReduction:
                target.currentDamageReduction += value;
                break;
            case EffectType.GetContinuedEffect:
                if (effect.continuedEffect != null)
                {
                    for(int i = 0 ; i < value; i++)
                    {
                        target.effectCtrl.AddContinueEffect(effect.continuedEffect);
                    }
                }
                break;
            case EffectType.LoseContinuedEffect:
                if (effect.continuedEffect != null)
                {
                    for(int i = 0 ; i < value; i++)
                    {
                        target.effectCtrl.LoseContinueEffect(effect.continuedEffect);
                    }
                }
                break;
        }
    }
    
    public static int GetValue(ValueEntry targetEntry, CharacterHealth user, CharacterHealth target)
    {
        CharacterHealth ch = null;
        switch (targetEntry.valueTurget)
        {
            case valueTarget.Initiator:
                ch = user;
                break;
            case valueTarget.target:
                ch = target;
                break;
        }
        if (ch == null) return 0;

        float targetValue = 0;
        switch (targetEntry.targetValue)
        {
            case TargetValue.InputValue:
                targetValue = ch.EnterValue;
                break;
            case TargetValue.MaxHealth:
                targetValue = ch.currentMaxHP;
                break;
            case TargetValue.Health:
                targetValue = ch.currentHealth;
                break;
            case TargetValue.LossesHealth:
                targetValue = ch.currentMaxHP - ch.currentHealth;
                break;
            case TargetValue.HoldCards:
                targetValue = ch.ownerPlayer.hand.Count;
                break;
            case TargetValue.HoldContinuedEffect:
                targetValue = ch.effectCtrl.activeEffects
                    .Where(e => e.EffectName == targetEntry.continuedEffect.EffectName).ToList().Count;
                break;
        }
        return Mathf.FloorToInt(targetValue * targetEntry.multiplier);
    }
}

