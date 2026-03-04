using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;
using System.Linq;

public class EffectInstance
{
    public ContinuedEffect effectData;
    public int stack;
    public int duration;
    public int triggerCount;
}
// 掛在角色上
public class ContinuedEffectCtrl : MonoBehaviour
{
    private CharacterHealth self;
    public GameObject effectPrefab;

    // 角色當前持有的 Buff/Debuff
    [HideInInspector] public List<EffectInstance> activeEffects = new();

    // // 每個效果的剩餘回合數
    // [HideInInspector] public Dictionary<ContinuedEffect, int> effectDurations = new();

    // // 每個效果當前回合已觸發次數
    // [HideInInspector] public Dictionary<ContinuedEffect, int> effectTriggerCounts = new();

    // 事件
    public static event System.Action<EffectInstance, CharacterHealth> OnEffectGot;
    public static event System.Action<EffectInstance, CharacterHealth> OnEffectTriggered;
    public static event System.Action<EffectInstance, CharacterHealth> OnEffectExpired;
    public static event System.Action<EffectInstance, CharacterHealth> OnEffectRemake;

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
        EffectInstance existed = null;

        // 先找是否已有同名的最大持續時間的效果
        if (activeEffects.Where(e => e.effectData == effect).Sum(e => e.stack) < effect.MaxOverlay)
        {
            existed = activeEffects.Where(e => e.effectData == effect).ToList().Find(e => e.duration == existed.duration);
            if (existed != null)
            {
                // 增加堆疊
                existed.stack++;
                OnEffectRemake?.Invoke(existed, self); // UI更新
                OnEffectGot?.Invoke(existed, self);
            }
            else
            {
                EffectInstance newInstance = new EffectInstance
                {
                    effectData = effect,
                    stack = 1,
                    duration = effect.Duration,
                    triggerCount = 0
                };

                activeEffects.Add(newInstance);
                createEffect(newInstance);
                OnEffectGot?.Invoke(newInstance, self);
            }
        }
        else
        {
            // 找最小
            foreach (var e in activeEffects.Where(e => e.effectData == effect))
            {
                if (existed == null || e.duration <= existed.duration)
                {
                    existed = e;
                }
            }
            if (existed != null)
            {
                existed.stack --;
                OnEffectRemake?.Invoke(existed, self); // UI更新
                OnEffectExpired?.Invoke(existed, self);

                if (activeEffects.Where(e => e.effectData == effect).Any(e => e.duration == effect.Duration))
                {
                    existed = existed = activeEffects
                        .Where(e => e.effectData == effect).ToList().Find(e => e.duration == existed.duration);
                    existed.stack ++;
                    OnEffectGot?.Invoke(existed, self);
                }
                else
                {
                    EffectInstance newInstance = new EffectInstance
                    {
                        effectData = effect,
                        stack = 1,
                        duration = effect.Duration,
                        triggerCount = 0
                    };

                    activeEffects.Add(newInstance);
                    createEffect(newInstance);
                    OnEffectGot?.Invoke(newInstance, self);
                }
            }
        }
    }
    private void createEffect(EffectInstance effect) // 產生圖標
    {
        GameObject go = Instantiate(effectPrefab, self.stateArea);
        ContinuedEffect_display CED = go.GetComponent<ContinuedEffect_display>();
        CED.effectDataInstance = effect;
        CED.selfHealth = self;
        go.SetActive(true);
    }
    private void RemoveEffect(EffectInstance effect) // 移除圖標
    {
        foreach (Transform child in self.stateArea)
        {
            ContinuedEffect_display CED = child.GetComponent<ContinuedEffect_display>();
            if (CED != null && CED.effectDataInstance == effect) // 直接比實例
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
            Debug.Log($"效果:{continuedEffect.EffectName}無法解除");
            return;
        }
        if (!activeEffects.Any(e => e.effectData == continuedEffect))
        {
            Debug.Log($"未持有效果:{continuedEffect.EffectName}");
            return;
        }

        EffectInstance effectInstance = activeEffects.Find(e => e.effectData == continuedEffect);
        effectInstance.stack--;
        OnEffectExpired?.Invoke(effectInstance, self);
        if (effectInstance.stack <= 0 || effectInstance.duration <= 0)
        {
            activeEffects.Remove(effectInstance);
            RemoveEffect(effectInstance);
        }
    }

    // 檢查條件並觸發
    private void TryTrigger(TriggerTime time, CharacterHealth acting)
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.GameStart) return;

        if (acting == null)
            acting = TurnManager.Instance.actingPlayer.playerCharacters[0];
        if (acting == null) return;
        
        List<EffectInstance> expired = new();

        foreach (var effect in activeEffects)
        {
            foreach (var entry in effect.effectData.continuedEffectEntrys)
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
                if (effect.triggerCount >= effect.effectData.TriggerTimes)
                {
                    if (effect.effectData.LimitedTimes) continue;
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
                Debug.Log($"持續效果:{effect.effectData.EffectName}觸發");
                StartCoroutine(EffectExecutor.ApplyEffects(self, targets, entry.effects));
                // 紀錄觸發次數
                effect.triggerCount++;
                OnEffectTriggered?.Invoke(effect, self);
                
            }

            // 扣減持續時間
            if (time == TriggerTime.OnRealTurnEnd)
            {
                switch (effect.effectData.endtrigger)
                {
                    case Trigger_Character.Self:
                        if (acting != self) continue;
                        break;
                    case Trigger_Character.Other:
                        if (acting == self) continue;
                        break;
                }
                effect.duration--;
                if (effect.duration <= 0 && effect.effectData.Removable)
                {
                    expired.Add(effect);
                }
            }
        }

        // 移除過期效果
        foreach (var e in expired)
        {
            if (e.effectData.Removable)
            {
                activeEffects.Remove(e);
                OnEffectExpired?.Invoke(e, self);
                RemoveEffect(e);
            }
        }
    }
    // 僅檢查單一效果
    private void TryTriggerSingle(EffectInstance effect, TriggerTime time, CharacterHealth acting)
    {        
        if (!activeEffects.Contains(effect)) return;

        foreach (var entry in effect.effectData.continuedEffectEntrys)
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
            if (effect.triggerCount >= effect.effectData.TriggerTimes)
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
            Debug.Log($"持續效果:{effect.effectData.EffectName}觸發");
            StartCoroutine(EffectExecutor.ApplyEffects(self, targets, entry.effects));

            // 紀錄觸發次數
            effect.triggerCount++;

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

    private void HandleEffectGot(EffectInstance e, CharacterHealth acting)
    {
        if (acting == self) // 只處理自己身上的效果
            TryTriggerSingle(e, TriggerTime.OnGetEffect, acting);
    }
    private void HandleEffectLosed(EffectInstance e, CharacterHealth acting)
    {
        if (acting == self) // 只處理自己身上的效果
            TryTriggerSingle(e, TriggerTime.OnLoseEffect, acting);
    }

    // 結束
    private void HandleRealTurnEnd(Player player)
    {
        // 回合結束 -> 重置每個效果的觸發次數
        List<EffectInstance> effectsCopy = new(activeEffects);
        foreach (var e in effectsCopy)
        {
            e.triggerCount = 0;
        }
        TryTrigger(TriggerTime.OnRealTurnEnd, null);
    }
}
