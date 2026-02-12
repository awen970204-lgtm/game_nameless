using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

// 掛在角色上
public class ContinuedEffectCtrl : MonoBehaviour
{
    private CharacterHealth self;
    public GameObject effectPrefab;

    // 角色當前持有的 Buff/Debuff
    [HideInInspector] public List<ContinuedEffect> activeEffects = new();

    // 每個效果的剩餘回合數
    [HideInInspector] public Dictionary<ContinuedEffect, int> effectDurations = new();

    // 每個效果當前回合已觸發次數
    [HideInInspector] public Dictionary<ContinuedEffect, int> effectTriggerCounts = new();

    // 事件
    public static event System.Action<ContinuedEffect, CharacterHealth> OnEffectGot;
    public static event System.Action<ContinuedEffect, CharacterHealth> OnEffectTriggered;
    public static event System.Action<ContinuedEffect, CharacterHealth> OnEffectExpired;
    public static event System.Action<ContinuedEffect, CharacterHealth> OnEffectRemake;

    void Awake()
    {
        self = GetComponent<CharacterHealth>();
    }
    void OnEnable()
    {
        if (TurnManager.Instance == null) return;

        TurnManager.OnBattleBegin += HandleBattleStart;
        TurnManager.OnTurnStart += HandleTurnStart;
        TurnManager.OnTurnEnd += HandleTurnEnd;
        TurnManager.OnAttackEvent += HandleAttactEvent;
        TurnManager.OnAnyBeHealed += HandleBeHealed;
        TurnManager.OnAnyCharacterDead += HandleCharacterDead;
        TurnManager.OnRealTurnEnd += HandleRealTurnEnd;
        ContinuedEffectCtrl.OnEffectGot += HandleEffectGot;
        ContinuedEffectCtrl.OnEffectExpired += HandleEffectLosed;
    }
    void OnDisable()
    {
        if (TurnManager.Instance == null) return;

        TurnManager.OnBattleBegin -= HandleBattleStart;
        TurnManager.OnTurnStart -= HandleTurnStart;
        TurnManager.OnTurnEnd -= HandleTurnEnd;
        TurnManager.OnAttackEvent -= HandleAttactEvent;
        TurnManager.OnAnyBeHealed -= HandleBeHealed;
        TurnManager.OnAnyCharacterDead -= HandleCharacterDead;
        TurnManager.OnRealTurnEnd -= HandleRealTurnEnd;
        ContinuedEffectCtrl.OnEffectGot -= HandleEffectGot;
        ContinuedEffectCtrl.OnEffectExpired -= HandleEffectLosed;
    }

    public void AddContinueEffect(ContinuedEffect effect) // 套用持續效果
    {
        // 先找是否已有同名、同持續時間(或不限時)的效果
        ContinuedEffect existed = null;
        foreach (var e in activeEffects)
        {
            bool sameName = e.name == effect.name;
            bool sameDuration = (!e.endable && !effect.endable) ||
                                (e.endable && effect.endable && e.Duration == effect.Duration);

            if (sameName && sameDuration)
            {
                existed = e;
                break;
            }
        }
        // 若找到可堆疊的效果
        if (existed != null)
        {
            if (existed.stack < existed.MaxOverlay)
            {
                // 增加堆疊
                existed.stack++;
                effectTriggerCounts[existed] = 0;
                OnEffectRemake?.Invoke(existed, self); // UI更新
            }
            else
            {
                // 已達上限 → 只刷新時間，不增加堆疊
                effectDurations[existed] = existed.Duration;
                effectTriggerCounts[existed] = 0;
                OnEffectRemake?.Invoke(existed, self);
            }

            return;
        }
        // 若沒有相同效果 → 新增 instance + UI
        ContinuedEffect newInstance = Instantiate(effect);
        newInstance.stack = 1;

        activeEffects.Add(newInstance);
        effectDurations[newInstance] = newInstance.Duration;
        effectTriggerCounts[newInstance] = 0;

        createEffect(newInstance);
        OnEffectGot?.Invoke(newInstance, self);
    }
    private void createEffect(ContinuedEffect effect) // 產生圖標
    {
        GameObject go = Instantiate(effectPrefab, self.stateArea);
        ContinuedEffect_display CED = go.GetComponent<ContinuedEffect_display>();
        CED.effectData = effect;
        CED.selfHealth = self;
        go.SetActive(true);
    }
    private void RemoveEffect(ContinuedEffect effect) // 移除圖標
    {
        foreach (Transform child in self.stateArea)
        {
            ContinuedEffect_display CED = child.GetComponent<ContinuedEffect_display>();
            if (CED != null && CED.effectData == effect) // 直接比實例
            {
                Destroy(child.gameObject);
                break;
            }
        }
    }

    public void LoseContinueEffect(ContinuedEffect continuedEffect) // 解除效果
    {
        if (!continuedEffect.Removable)
        {
            Debug.LogError($"效果:{continuedEffect.EffectName}無法解除");
            return;
        }
        if (!activeEffects.Contains(continuedEffect))
        {
            Debug.LogError($"未持有效果:{continuedEffect.EffectName}");
            return;
        }
        continuedEffect.stack--;
        OnEffectExpired?.Invoke(continuedEffect, self);
        if (continuedEffect.stack <= 0 || effectDurations[continuedEffect] <= 0)
        {
            activeEffects.Remove(continuedEffect);
            effectDurations.Remove(continuedEffect);
            effectTriggerCounts.Remove(continuedEffect);
            RemoveEffect(continuedEffect);
        }
    }

    // 檢查條件並觸發
    private void TryTrigger(TriggerTime time, CharacterHealth acting)
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.GameStart) return;

        if (acting == null)
            acting = TurnManager.Instance.actingPlayer.playerCharacters[0];
        if (acting == null) return;

        
        
        List<ContinuedEffect> expired = new();

        foreach (var effect in activeEffects)
        {
            foreach (var entry in effect.continuedEffectEntrys)
            {
                if (entry.triggerTime != time) continue;

                bool restricted = false;
                if (entry.Needs != null)
                {
                    foreach (var need in entry.Needs)
                    {
                        if (!LimitChecker.CheckLimit(need, self, acting)) restricted = true;
                    }
                }
                if (restricted) continue;

                bool match = false;
                switch (entry.triggerCharacter)
                {
                    case Trigger_Character.Self:
                        match = (acting == self);
                        break;
                    case Trigger_Character.Other:
                        match = (acting != self);
                        break;
                    case Trigger_Character.All:
                        match = (acting != null);
                        break;
                }
                if (!match) continue;
                
                // 檢查是否達到上限
                if (effectTriggerCounts.ContainsKey(effect) &&
                    effectTriggerCounts[effect] >= effect.TriggerTimes)
                {
                    if (effect.LimitedTimes) continue;
                }
                // 套用效果
                List<CharacterHealth> targets = new();
                switch (entry.effectTarget)
                {
                    case TargetType.Self:
                        targets.Add(self);
                        break;
                    case TargetType.AllOther:
                        targets = new List<CharacterHealth>(TurnManager.Instance.turnOrder);
                        targets.Remove(self);
                        break;
                    case TargetType.All:
                        targets = new List<CharacterHealth>(TurnManager.Instance.turnOrder);
                        break;
                }
                Debug.Log($"持續效果:{effect.EffectName}觸發");
                StartCoroutine(EffectExecutor.ApplyEffects(self, targets, entry.effects));
                // 紀錄觸發次數
                if (!effectTriggerCounts.ContainsKey(effect))
                    effectTriggerCounts[effect] = 0;
                effectTriggerCounts[effect]++;
                OnEffectTriggered?.Invoke(effect, self);
                
            }

            // 扣減持續時間
            if (time == TriggerTime.OnRealTurnEnd && effectDurations.ContainsKey(effect))
            {
                switch (effect.endtrigger)
                {
                    case Trigger_Character.Self:
                        if (acting != self) continue;
                        break;
                    case Trigger_Character.Other:
                        if (acting == self) continue;
                        break;
                }
                effectDurations[effect]--;
                if (effectDurations[effect] <= 0 && effect.Removable)
                {
                    expired.Add(effect);
                }
            }
        }

        // 移除過期效果
        foreach (var e in expired)
        {
            LoseContinueEffect(e);
        }
    }
    // 僅檢查單一效果
    private void TryTriggerSingle(ContinuedEffect effect, TriggerTime time, CharacterHealth acting)
    {        
        if (!activeEffects.Contains(effect)) return;

        foreach (var entry in effect.continuedEffectEntrys)
        {
            if (entry.triggerTime != time) continue;

            bool match = false;
            switch (entry.triggerCharacter)
            {
                case Trigger_Character.Self:
                    match = (acting == self);
                    break;
                case Trigger_Character.Other:
                    match = (acting != self);
                    break;
                case Trigger_Character.All:
                    match = (acting != null);
                    break;
            }

            if (!match) continue;

            // 檢查觸發次數
            if (effectTriggerCounts.ContainsKey(effect) &&
                effectTriggerCounts[effect] >= effect.TriggerTimes)
            {
                continue;
            }

            // 找目標
            List<CharacterHealth> targets = new();
            switch (entry.effectTarget)
            {
                case TargetType.Self:
                    targets.Add(self);
                    break;
                case TargetType.AllOther:
                    targets = new List<CharacterHealth>(TurnManager.Instance.turnOrder);
                    targets.Remove(self);
                    break;
                case TargetType.All:
                    targets = new List<CharacterHealth>(TurnManager.Instance.turnOrder);
                    break;
            }
            Debug.Log($"持續效果:{effect.EffectName}觸發");
            StartCoroutine(EffectExecutor.ApplyEffects(self, targets, entry.effects));

            // 紀錄觸發次數
            if (!effectTriggerCounts.ContainsKey(effect))
                effectTriggerCounts[effect] = 0;
            effectTriggerCounts[effect]++;

            OnEffectTriggered?.Invoke(effect, self);
        }
    }

    // 不同時機觸發
    private void HandleBattleStart() => TryTrigger(TriggerTime.OnBattleStart, self);
    private void HandleTurnStart(Player player) => TryTrigger(TriggerTime.OnTurnStart, 
        TurnManager.Instance.actingPlayer.team == self.team ? 
        self : TurnManager.Instance.actingPlayer.playerCharacters.Find(c => c.currentHealth > 0));
    private void HandleTurnEnd(Player player) => TryTrigger(TriggerTime.OnTurnEnd, 
        TurnManager.Instance.actingPlayer.team == self.team ? 
        self : TurnManager.Instance.actingPlayer.playerCharacters.Find(c => c.currentHealth > 0));
    private void HandleBeHealed(CharacterHealth acting) => TryTrigger(TriggerTime.OnBeHealed, acting);
    private void HandleCharacterDead(CharacterHealth acting) => TryTrigger(TriggerTime.OnCharacterDeath, acting);
    private void HandleAttactEvent(CharacterHealth attacker, CharacterHealth injured)
    {
        TryTrigger(TriggerTime.OnBeAttacted, attacker);
        TryTrigger(TriggerTime.OnBeAttacted, injured);
    }

    private void HandleEffectGot(ContinuedEffect e, CharacterHealth acting)
    {
        if (acting == self) // 只處理自己身上的效果
            TryTriggerSingle(e, TriggerTime.OnGetEffect, acting);
    }
    private void HandleEffectLosed(ContinuedEffect e, CharacterHealth acting)
    {
        if (acting == self) // 只處理自己身上的效果
            TryTriggerSingle(e, TriggerTime.OnLoseEffect, acting);
    }

    // 結束
    private void HandleRealTurnEnd(Player player)
    {
        // 回合結束 -> 重置每個效果的觸發次數
        List<ContinuedEffect> effectsCopy = new(activeEffects);
        foreach (var e in effectsCopy)
        {
            if (effectTriggerCounts.ContainsKey(e))
                effectTriggerCounts[e] = 0;
        }
        TryTrigger(TriggerTime.OnRealTurnEnd, null);
    }
}
