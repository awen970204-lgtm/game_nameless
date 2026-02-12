using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Linq;

// 掛在角色上
public class PassiveSkilCtrl : MonoBehaviour
{
    public GameObject checkButton;   // 確認按鈕
    public TMP_Text passiveskillTip; // 用來顯示提示
    private CharacterHealth self;    // 持有角色

    [HideInInspector] public Dictionary<(CharacterHealth, PassiveSkill), int> passiveUseCounter = new();        // 技能次數
    [HideInInspector] public List<CharacterHealth> selectedTargets_PassiveSkill = new List<CharacterHealth>();  // 目標清單
    [HideInInspector] public List<PassiveEntry> Executable_PassiveEntry = new List<PassiveEntry>();  // 可生效效果

    // public static event System.Action OnAnyPassiveFinished; // 事件：有被動完成
    public static event System.Action OnPassiveFinished;    // 事件：被動結束
    public static event System.Action OnPassiveRemake;      // 事件：被動重製

    // 存放需要選擇的被動
    private Queue<PassiveSkill> pendingPassiveQueue = new Queue<PassiveSkill>();
    
    void Awake()
    {
        self = GetComponent<CharacterHealth>();  // 取得持有者
        checkButton = TurnManager.Instance.checkButton;
        passiveskillTip = TurnManager.Instance.UseTips;
    }
    void OnEnable()// 訂閱事件
    {
        if (TurnManager.Instance == null) return;

        TurnManager.OnBattleBegin += HandleBattleStart;
        TurnManager.OnTurnStart += HandleTurnStart;
        TurnManager.OnTurnEnd += HandleTurnEnd;
        TurnManager.OnAnyBeHealed += HandleBeHealed;
        TurnManager.OnAnyCharacterDead += HandleCharacterDead;
        TurnManager.OnAttackEvent += HandleAttactEvent;
        TurnManager.OnAnyConsumeHP += HandleConsumeHP;

    }
    void OnDisable()// 解除訂閱事件
    {
        if (TurnManager.Instance == null) return;

        TurnManager.OnBattleBegin -= HandleBattleStart;
        TurnManager.OnTurnStart -= HandleTurnStart;
        TurnManager.OnTurnEnd -= HandleTurnEnd;
        TurnManager.OnAnyBeHealed -= HandleBeHealed;
        TurnManager.OnAnyCharacterDead -= HandleCharacterDead;
        TurnManager.OnAttackEvent -= HandleAttactEvent;
        TurnManager.OnAnyConsumeHP -= HandleConsumeHP;
    }

    private void HandleBattleStart() => TryTrigger(TriggerTime.OnBattleStart, self);
    private void HandleTurnStart(Player player) => TryTrigger(TriggerTime.OnTurnStart, 
        TurnManager.Instance.actingPlayer.team == self.team ? 
        self : TurnManager.Instance.actingPlayer.playerCharacters.Find(c => c.currentHealth > 0));
    private void HandleTurnEnd(Player player) => TryTrigger(TriggerTime.OnTurnEnd, 
        TurnManager.Instance.actingPlayer.team == self.team ? 
        self : TurnManager.Instance.actingPlayer.playerCharacters.Find(c => c.currentHealth > 0));
    private void HandleAttactEvent(CharacterHealth attacker, CharacterHealth injured)
    {
        TryTrigger(TriggerTime.OnAttact, attacker);
        TryTrigger(TriggerTime.OnBeAttacted, injured);
    }
    private void HandleBeHealed(CharacterHealth acting) => TryTrigger(TriggerTime.OnBeHealed, acting);
    private void HandleCharacterDead(CharacterHealth acting) => TryTrigger(TriggerTime.OnCharacterDeath, acting);
    private void HandleConsumeHP(CharacterHealth acting) => TryTrigger(TriggerTime.OnConsumeHP, acting);

    // 嘗試執行
    private void TryTrigger(TriggerTime time, CharacterHealth acting)
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.GameStart) return;
        if (acting == null) 
            acting = TurnManager.Instance.actingPlayer.playerCharacters[0];
        if (acting == null) return;

        // 會觸發的技能
        List<PassiveSkill> toTrigger = new List<PassiveSkill>();
        // 找符合的技能
        foreach (var skill in self.currentPassiveSkills)
        {
            if (self.invalidPassiveSkills.Contains(skill)) continue;

            foreach (var passive_entry in skill.passiveEntry)
            {
                foreach (var skillTime in passive_entry.passiveTimes)
                {
                    if (skillTime.triggerTime != time) continue;
                    // 檢查技能前提
                    bool restricted = false;
                    if (skillTime.passiveNeed != null)
                    {
                        foreach (var need in skillTime.passiveNeed)
                        {
                            if (!LimitChecker.CheckLimit(need, acting, self)) restricted = true;
                        }
                    }
                    if (restricted) continue;

                    switch (skillTime.trigger)
                    {
                        case Trigger_Character.Self:
                            if (acting == self)
                            {
                                Executable_PassiveEntry.Add(passive_entry);
                                toTrigger.Add(skill);
                            }
                            break;
                        case Trigger_Character.Other:
                            if (acting != self)
                            {
                                Executable_PassiveEntry.Add(passive_entry);
                                toTrigger.Add(skill);
                            }
                            break;
                        case Trigger_Character.All:
                            if (acting != null)
                            {
                                Executable_PassiveEntry.Add(passive_entry);
                                toTrigger.Add(skill);
                            }
                            break;
                    }
                }
            }
        }
        // 統一在最後觸發
        PassiveSkillBegin(toTrigger);
    }

    // 開始被動流程
    private void PassiveSkillBegin(List<PassiveSkill> skills)
    {
        // 會生效的技能
        List<PassiveSkill> toEffective = new List<PassiveSkill>();
        foreach (var skill in skills)
        {
            // 檢查持有
            if (!self.currentPassiveSkills.Contains(skill)) continue;
            if (self.invalidPassiveSkills.Contains(skill)) continue;
                
            // 檢查發動次數
            var key = (self, skill);
            if (!passiveUseCounter.TryGetValue(key, out int usedCount))
            {
                usedCount = 0;
                passiveUseCounter[key] = 0;
            }
            if (usedCount >= skill.maxTriggersPerTurn && skill.LimitedTimes)
            {
                Debug.LogWarning($"{skill.skillName} 已達本回合觸發上限！");
                continue;
            }
            // 紀錄被動
            toEffective.Add(skill);
            TurnManager.Instance.EnqueuePassive(toEffective, this, self);
        }
    }

    public void PassiveFinish(PassiveSkill skill)// 完成被動並顯示
    {
        var key = (self, skill);
        if (!passiveUseCounter.TryGetValue(key, out int usedCount))
        {
            usedCount = 0;
            passiveUseCounter[key] = 0;
        }
        passiveUseCounter[key] = usedCount + 1;

        self.ownerPlayer.ShowFloatingSkill(skill.skillName, self);
        OnPassiveFinished?.Invoke();
    }

    // 重置被動技能次數
    public void ResetPassives()
    {
        passiveUseCounter.Clear();
        OnPassiveRemake?.Invoke();
    }
}
