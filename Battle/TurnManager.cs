using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    public static bool playerVictory = false;
    // 回合系統
    public Player player1;
    public Player player2;
    [HideInInspector] public List<CharacterHealth> turnOrder = new List<CharacterHealth>();        // 行動清單
    [HideInInspector] public List<CharacterHealth> selectedTargets = new List<CharacterHealth>();  // 目標清單

    // 角色狀態
    [HideInInspector] public bool waitingForAction = false;         // 等待行動狀態
    [HideInInspector] public bool waitingForTarget = false;         // 等待選擇狀態
    [HideInInspector] public Player actingPlayer;                   // 回合進行中角色
    [HideInInspector] public Skill pendingSkill;                    // 暫存技能
    [HideInInspector] public PassiveSkilCtrl pendingPassiveCtrl;    // 暫存被動執行者
    [HideInInspector] public PassiveSkill pendingPassive;           // 暫存被動技能
    [HideInInspector] public EffectEntry pendingEffectEntry;        // 暫存效果組合
    [HideInInspector] public Card pendingCard;                      // 暫存卡片
    [HideInInspector] public CardCtrl pendingCardUI;                // 暫存卡片UI
    [HideInInspector] public Player pendingPlayer;                  // 暫存玩家
    [HideInInspector] public CharacterHealth pendingUser;           // 技能/卡片的實際使用者
    // 執行狀態
    [HideInInspector] public bool GameStart = false;        // 遊戲開始狀態
    [HideInInspector] public bool TurnEnded = false;        // 回合結束狀態
    [HideInInspector] public bool actionCancelled = false;  // 取消了
    [HideInInspector] public int pendingPassives = 0;       // 尚未結算的被動數量
    [HideInInspector] public int pendingEffectEntrys = 0;   // 尚未結算的效果數量

    // 技能使用次數
    [HideInInspector] public Dictionary<(CharacterHealth, Skill), int> skillUseCounter
     = new Dictionary<(CharacterHealth, Skill), int>();
    // 被動技能序列
    [HideInInspector] public Queue<(List<PassiveSkill>,PassiveSkilCtrl,  CharacterHealth)> passiveQueue 
     = new Queue<(List<PassiveSkill>, PassiveSkilCtrl, CharacterHealth)>();

    [Header("UI 綁定")]
    public TMP_Text turnText;
    public TMP_Text UseTips;
    public GameObject skillBarCtrl;
    public InputValue inputValue;
    public GameObject checkButton;
    public GameObject endTurnButton;
    public GameObject battleOverPanel;
    // 共用牌堆
    private List<Card> drawPile = new List<Card>();    // 當前可抽取
    private List<Card> discardPile = new List<Card>(); // 棄牌堆

    #region Event
    public static event System.Action OnBattleBegin;
    public static event System.Action OnCancleChoose;
    public static event System.Action<Player> OnTurnStart;
    public static event System.Action<Player> OnTurnEnd;
    public static event System.Action<Player> OnRealTurnEnd;
    public static event System.Action<CardCtrl> OnCancelCardChoose;

    public static event System.Action<Card> OnCardRemove;
    public static event System.Action<Player, Card> OnAnyCardPlayBegin;
    public static event System.Action<Player, Card> OnAnyCardPlayed;
    public static event System.Action<CharacterHealth, Skill> OnAnySkillBegin;
    public static event System.Action<CharacterHealth, Skill> OnAnySkillEnd;
    public static event System.Action<CharacterHealth, PassiveSkill> OnAnyPassiveSkillBegin;
    public static event System.Action<CharacterHealth, PassiveSkill> OnAnyPassiveSkillEnd;
    public static event System.Action<CharacterHealth> OnAnyConsumeHP;
    public static event System.Action<CharacterHealth> OnAnyBeHealed;
    public static event System.Action<CharacterHealth> OnAnyCharacterDead;

    public static event System.Action<CharacterHealth, CharacterHealth> OnAttackEvent;

    // 外部事件綁定
    public void RaiseAnyAttackEvent(CharacterHealth attacker, CharacterHealth injured) => OnAttackEvent?.Invoke(attacker, injured);
    public void RaiseAnyConsumeHP(CharacterHealth ch) => OnAnyConsumeHP?.Invoke(ch);
    public void RaiseAnyBeHealed(CharacterHealth ch) => OnAnyBeHealed?.Invoke(ch);
    public void RaiseAnyCharacterDead(CharacterHealth ch) => OnAnyCharacterDead?.Invoke(ch);
    public void RaiseAnyOnCardRemove(Card card) => OnCardRemove?.Invoke(card);
    #endregion

    // 啟動
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void OnEnable()
    {
        
    }
    void Start()
    {
        turnText.text = "Chosing Stage";
    }
    void OnDisable()
    {
        
    }
    
    public void Register(CharacterHealth character)// 註冊角色進行動清單
    {
        if (IsBattleOver) return;
        if (!turnOrder.Contains(character))
            turnOrder.Add(character);
    }
    public void Unregister(CharacterHealth character)// 當角色死亡時移除
    {
        if (IsBattleOver) return;

        character.ownerPlayer.playerCharacters.Remove(character);
        character.ownerPlayer.PlayerMenbers.text = 
            $"{character.ownerPlayer.playerCharacters.Count}/{character.ownerPlayer.MaxMenber}";
        turnOrder.Remove(character);

        Destroy(character.gameObject);
        CheckBattleEnd();
    }

    public bool CanUseSkill(CharacterHealth user, Skill skill)// 是否能使用技能
    {
        var key = (user, skill);

        if (!skillUseCounter.ContainsKey(key))
            skillUseCounter[key] = 0;

        return skillUseCounter[key] < skill.maxUsesPerTurn;
    }
    public bool CanProceed()// 是否可以行動
    {
        return WaitCardManager.Instance == null || WaitCardManager.Instance.IsIdle;
    }

    // 遊戲開始
    public void StartBattle()
    {
        if (GameStart) return;

        Remake();
        CheckBattleEnd();
        skillBarCtrl.SetActive(true);
        player1.SelectButton.gameObject.SetActive(false);
        player2.SelectButton.gameObject.SetActive(false);

        player1.MaxMenber *= 2;
        player1.PlayerMenbers.text = $"{player1.playerCharacters.Count}/{player1.MaxMenber}";
        player2.MaxMenber *= 2;
        player2.PlayerMenbers.text = $"{player2.playerCharacters.Count}/{player2.MaxMenber}";

        Debug.Log("遊戲開始");
        GameStart = true;
        OnBattleBegin?.Invoke();
        StartCoroutine(StartTurnDelay());
    }

    #region Turn Start

    private IEnumerator StartTurnDelay()
    {
        yield return new WaitForSeconds(0.1f);   // 等待0.1秒
        if (!IsBattleOver)
        {
            StartTurn();
        }
    }
    public void StartTurn()
    {
        if (turnOrder.Count == 0) return;

        if (actingPlayer == player1)
            actingPlayer = player2;
        else if (actingPlayer == player2 || actingPlayer == null)
            actingPlayer = player1;

        if (actingPlayer.playerCharacters.Count == 0)
        { 
            EndTurn(); 
            return;
        }

        if (actingPlayer.team != TeamID.Enemy)
            turnText.text = $"輪到P{actingPlayer.Player_nunber} 行動";
        else if (actingPlayer.team == TeamID.Enemy)
            turnText.text = $"敵人:P{actingPlayer.Player_nunber} 行動";
        
        Debug.Log($"P{actingPlayer.Player_nunber} 回合開始");
        Debug.Log(turnText.text);
        actingPlayer.ISActive = true;

        skillUseCounter.Clear();
        Remake();
        TurnEnded = false;
        waitingForAction = true;
        endTurnButton.SetActive(true);
        OnTurnStart?.Invoke(actingPlayer);
        foreach(var c in actingPlayer.playerCharacters)
        {
            actingPlayer?.DrawCard(c.DrawCardAtTurnStart);
        }

        // 開AI
        if (actingPlayer.team == TeamID.Enemy)
        {
            endTurnButton.SetActive(false);
        }
    }

    #endregion

    #region Skill

    public void OnSkillSelected(Skill skill, CharacterHealth user, Player player)
    {
        if (!waitingForAction || pendingPassives > 0 || !CanProceed())
        {
            if (user.team != TeamID.Enemy)
                LogWarning.Instance.Warning("當前狀態不可使用技能");
            Debug.LogWarning("當前狀態不可使用技能");
            return;
        }
        // 檢查目前使用次數
        var key = (user, skill);
        if (!skillUseCounter.ContainsKey(key))
            skillUseCounter[key] = 0;
        if (skillUseCounter[key] >= skill.maxUsesPerTurn)
        {
            if (user.team != TeamID.Enemy)
                LogWarning.Instance.Warning($"{skill.skillName} 在本回合已達使用上限");
            Debug.LogWarning($"{skill.skillName} 在本回合已達使用上限");
            return;
        }
        // 執行
        endTurnButton.SetActive(false);
        StartCoroutine(HandleSkillUseRoutine(skill, user, player));
    }
    private IEnumerator HandleSkillUseRoutine(Skill skill, CharacterHealth user, Player player)// 執行技能效果
    {
        Debug.Log($"{user.character_data.characterName}使用了技能:{skill.skillName}");
        OnAnySkillBegin?.Invoke(user, skill);
        pendingEffectEntrys += skill.effectEntries.Count;
        int nowPassives = pendingPassives;
        for (int i = 0; i < skill.effectEntries.Count; i++)
        {
            var entry = skill.effectEntries[i];
            pendingSkill = skill;
            pendingUser = user;
            pendingPlayer = player;

            pendingEffectEntry = entry;

            if (entry.NeedChoose)
            {
                inputValue?.CloseInput();
                selectedTargets.Clear();
                actionCancelled = false;
                waitingForAction = false;
                waitingForTarget = true;
                if (user.team != TeamID.Enemy)
                    checkButton.SetActive(true);

                string TargetTip = "";
                switch(entry.targetType)
                {
                    case TargetType.Self:
                        TargetTip = "自己";
                        break;
                    case TargetType.Other:
                        TargetTip = "其他角色";
                        break;
                    case TargetType.Any:
                        TargetTip = "任何角色";
                        break;
                }
                UseTips.text = $"請選擇技能:{skill.skillName}的目標:{TargetTip}";
                if (entry.canInputValue) inputValue.CanInput(entry.MaxInputValue);

                // 自動選擇
                if (entry.targetType == TargetType.Self) pendingUser.SkillClick();
                // 等待玩家完成選擇
                yield return new WaitUntil(() => !waitingForTarget && canContinue());
                if (actionCancelled)
                {
                    pendingEffectEntrys -= skill.effectEntries.Count - i;
                    yield break;
                } 
            }
            else
            {
                // 直接套用效果
                ApplyEffectImmediate(entry);
            }

            // 等待效果結束（包含WaitCard、特效、被動）
            yield return null;
            yield return new WaitUntil(() => pendingPassives == nowPassives &&
                (WaitCardManager.Instance == null || WaitCardManager.Instance.IsIdle));

            pendingEffectEntrys--;
        }
        // 所有效果跑完
        pendingSkill = skill;
        pendingUser = user;
        pendingPlayer = player;
        Debug.Log($"{user.character_data.characterName}的技能:{skill.skillName}結算完畢");
        FinishSkillUse();
    }
    private void FinishSkillUse() // 結尾清理
    {
        if (pendingSkill != null && pendingPlayer != null && pendingUser != null)
        {
            RegisterSkillUse(pendingUser, pendingSkill);
            OnAnySkillEnd?.Invoke(pendingUser, pendingSkill);
        }
        selectedTargets.Clear();
        inputValue?.CloseInput();
        checkButton.SetActive(false);
        UseTips.text = "";

        endTurnButton.SetActive(pendingUser.team != TeamID.Enemy);
        
        Remake();
        waitingForAction = true;
        waitingForTarget = false;
    }
    private void RegisterSkillUse(CharacterHealth user, Skill skill) // 紀錄技能使用次數
    {
        var key = (user, skill);

        if (!skillUseCounter.ContainsKey(key))
            skillUseCounter[key] = 0;

        skillUseCounter[key]++;
    }
    private bool canContinue()
    {
        foreach (var ch in turnOrder)
        {
            if (ch.ownerPlayer.IsDising || ch.ownerPlayer.IsStealing) return false;
        }
        return true;
    }
    
    #endregion

    #region Card Use

    public void BeginUseCard(CardCtrl cardCtrl, CharacterHealth user, Player player)
    {
        if (cardCtrl == null || user == null || player == null ) return;

        OnAnyCardPlayBegin.Invoke(player, cardCtrl.card_data);

        StartCoroutine(handleUseCard(cardCtrl, user, player));
    }
    private IEnumerator handleUseCard(CardCtrl cardCtrl, CharacterHealth user, Player player)
    {
        waitingForAction = false;
        yield return ApplyCardEntrys(cardCtrl.card_data.effectEntrys, user, player);
        UseCardOver(cardCtrl, user, player);
        waitingForAction = true;
    }

    public void BeginTriggerCard(CardCtrl cardCtrl, CharacterHealth user, Player player) // 觸發
    {
        if (cardCtrl == null || user == null || player == null ) return;

        StartCoroutine(ApplyCardEntrys(cardCtrl.card_data.initiativeTiggerEffect, user, player));
    }

    private IEnumerator ApplyCardEntrys(List<EffectEntry> entries, CharacterHealth user, Player player)
    {
        Player enemyPlayer = (player == player1)? player2 : player2;

        foreach(var entry in entries)
        {
            List<CharacterHealth> targets = new List<CharacterHealth>();
            switch(entry.targetType)
            {
                case TargetType.Self:
                    targets.Add(user);
                    break;
                case TargetType.AllOther:
                    targets.AddRange(turnOrder);
                    targets.Remove(user);
                    break;
                case TargetType.All:
                    targets.AddRange(turnOrder);
                    break;
                case TargetType.teammate:
                    targets.AddRange(turnOrder.Where(c => c.team == user.team));
                    break;
                case TargetType.enemy:
                    targets.AddRange(turnOrder.Where(c => c.team != user.team));
                    break;
                case TargetType.EventTrigger:
                    targets.Add(WaitCardManager.Instance?.currentEvent?.actor);
                    break;
            }
            if (targets.Count == 0)
            {
                yield break;
            }

            List<CharacterHealth> aliveTargets = targets.Where(c => c.currentHealth > 0).ToList();
            
            if (aliveTargets.Count == targets.Count || aliveTargets.Count >= entry.maxTargets)
            {
                targets = aliveTargets;
            }

            targets = targets.Take(entry.maxTargets).ToList();
            
            yield return EffectExecutor.ApplyEffects(user, targets, entry.effects);
        }
        yield return null;
    }
    private void UseCardOver(CardCtrl cardCtrl, CharacterHealth user, Player player)
    {
        Debug.Log($"(P{player.Player_nunber}){cardCtrl.card_data.cardName}:Used");
        OnAnyCardPlayed.Invoke(player, cardCtrl.card_data);

        WaitCardManager.Instance.ResolveCard(cardCtrl);
        user.usingCard = null;

        player.UseCardOver(cardCtrl);
    }

    #endregion

    private void ApplyEffectImmediate(EffectEntry entry) // 立即生效的效果
    {
        if (pendingUser == null)
        {
            Debug.LogWarning("未設定角色");
            return;
        }
        switch (entry.targetType)
        {
            case TargetType.Self:
                selectedTargets = new List<CharacterHealth> { pendingUser };
                break;
            case TargetType.AllOther:
                selectedTargets = new List<CharacterHealth>(turnOrder);
                selectedTargets.Remove(pendingUser);
                break;
            case TargetType.All:
                selectedTargets = new List<CharacterHealth>(turnOrder);
                break;
            case TargetType.EventTrigger:
                selectedTargets = new List<CharacterHealth>{ WaitCardManager.Instance?.currentEvent.actor };
                break;
            case TargetType.teammate:
                selectedTargets = new List<CharacterHealth>(turnOrder.Where(c => c.team == pendingUser.team));
                break;
            case TargetType.enemy:
                selectedTargets = new List<CharacterHealth>(turnOrder.Where(c => c.team != pendingUser.team));
                break;
        }

        StartCoroutine(EffectExecutor.ApplyEffects(pendingUser, selectedTargets, entry.effects));
    }

    #region Target Select

    public void OnTargetToggled(CharacterHealth target)// 技能或卡牌的選中和取消選中(回報動作)
    {
        if (!waitingForTarget)
        {
            Debug.LogWarning("現在不用選擇");
            return;
        }
        if (!target.IsAlive)
        {
            Debug.LogWarning("目標已死亡");
            LogWarning.Instance.Warning($"目標已死亡");
            return;
        }
        // 決定目標上限
        int maxTargets = 1;
        if (pendingSkill != null) maxTargets = pendingEffectEntry.maxTargets;
        if (pendingCard != null) maxTargets = pendingEffectEntry.maxTargets;
        if (pendingPassive != null) maxTargets = pendingEffectEntry.maxTargets;

        if (selectedTargets.Contains(target))
        {
            // 選擇與取消選擇
            selectedTargets.Remove(target);
            target.ToggleHighlight(false);
            Debug.Log($"取消選取 {target.character_data.characterName}");
        }
        else
        {
            if (selectedTargets.Count >= maxTargets)
            {
                Debug.Log($"不能再選更多目標(上限 {maxTargets} 個)");
                LogWarning.Instance.Warning($"不能再選更多目標(上限 {maxTargets} 個)");
                return;
            }

            selectedTargets.Add(target);
            target.ToggleHighlight(true);
            Debug.Log($"選取了 {target.character_data.characterName}");
        }
    }
    public void TryToCancelSelection()// 取消選擇狀態(回報動作)
    {
        if (pendingUser != null && pendingUser.team != TeamID.Enemy)
            CancelSelection();
    }
    public void CancelSelection()// 取消選擇狀態
    {
        if (!waitingForTarget) return;
        if (pendingEffectEntry != null && pendingEffectEntry.canCancle == false)
        {
            LogWarning.Instance.Warning($"本選擇不能取消");
            return;
        }

        LogWarning.Instance.Warning("取消選擇目標");
        if (pendingCardUI != null)
            OnCancelCardChoose?.Invoke(pendingCardUI);

        // 清除選中的角色高亮
        foreach (var target in selectedTargets)
        {
            target.ToggleHighlight(false);
        }
        if (pendingCardUI != null)
        {
            pendingCardUI.DisplayChange();
        }
        selectedTargets.Clear();
        UseTips.text = ("");
        // pendingEffectEntrys = 0;

        // 重置狀態
        checkButton.SetActive(false);
        Remake();
        actingPlayer.ISActive = true;
        if (actingPlayer.team != TeamID.Enemy)
            endTurnButton.SetActive(true);
        
        actionCancelled = true;
        waitingForTarget = false;
        waitingForAction = true;

        OnCancleChoose?.Invoke();
    }
    public void TryToConfirm()// 技能或卡牌確定好目標(回報動作)
    {
        if (pendingUser != null && pendingUser.team != TeamID.Enemy)
            ConfirmTargets();
    }
    public void ConfirmTargets()// 技能或卡牌確定好目標
    {
        if (!waitingForTarget || pendingPlayer == null) return;
        if (selectedTargets.Count == 0)
        {
            LogWarning.Instance.Warning("至少需選擇一個目標");
            return;
        }

        // 執行目標效果
        // 技能
        if (pendingSkill != null && pendingUser != null && pendingEffectEntry != null)
        {
            // 確認目標
            if (TragetChecker.SkillCheckTarget(pendingEffectEntry, selectedTargets, pendingUser, pendingSkill) == false)
            {
                Debug.LogWarning($"{pendingSkill.skillName}未達到目標要求");
                return;
            }
            // 改變紀錄值
            if (pendingEffectEntry.canInputValue && pendingUser.team != TeamID.Enemy)
            {
                inputValue.ChangeEnterValue();
                Debug.Log("數值輸入成功");
            }
            foreach (var target in selectedTargets)
            {
                target.ToggleHighlight(false);
            }
            // 執行效果
            StartCoroutine(EffectExecutor.ApplyEffects(pendingUser, selectedTargets, pendingEffectEntry.effects));
            Debug.Log($"技能 {pendingSkill.skillName} 已施放");
        }
        // 被動技能
        else if (pendingPassiveCtrl != null && pendingPassive != null && pendingEffectEntry != null)
        {
            // 確認目標
            if (TragetChecker.PassiveSkillCheckTarget(pendingEffectEntry, selectedTargets, pendingUser, pendingPassive) == false)
            {
                Debug.LogWarning($"{pendingPassive.skillName}未達到目標要求");
                LogWarning.Instance.Warning($"{pendingPassive.skillName}未達到目標要求");
                return;
            }
            if (pendingEffectEntry.canInputValue && pendingUser.team != TeamID.Enemy)
            {
                inputValue.ChangeEnterValue();
                Debug.Log("數值輸入成功");
            }
            // 執行效果
            foreach (var target in selectedTargets)
            {
                target.ToggleHighlight(false);
            }
            StartCoroutine(EffectExecutor.ApplyEffects(pendingUser, selectedTargets, pendingEffectEntry.effects));
            Debug.Log($"技能 {pendingPassive.skillName} 已施放");
        }
        // 卡片
        else if (pendingCard != null && pendingUser != null && pendingEffectEntry != null)
        {
            // 確認目標
            if (TragetChecker.CardCheckTarget(pendingEffectEntry, selectedTargets, pendingUser, pendingCard) == false)
            {
                Debug.LogWarning($"{pendingCard.cardName}未達到目標要求");
                LogWarning.Instance.Warning($"{pendingCard.cardName}未達到目標要求");
                return;
            }
            if (pendingEffectEntry.canInputValue && pendingUser.team != TeamID.Enemy)
            {
                inputValue.ChangeEnterValue();
                Debug.Log("數值輸入成功");
            }
            // 清掉高亮
            foreach (var target in selectedTargets)
            {
                target.ToggleHighlight(false);
            }
            // 執行
            StartCoroutine(EffectExecutor.ApplyEffects(pendingUser, selectedTargets, pendingEffectEntry.effects));
            Debug.Log($"卡牌 {pendingCard.cardName} 已使用");
        }
        waitingForAction = true;
        waitingForTarget = false;
    }
    #endregion

    #region Passive

    public void EnqueuePassive(List<PassiveSkill> passiveSkills, PassiveSkilCtrl ctrl, CharacterHealth user)
    {
        passiveQueue.Enqueue((passiveSkills, ctrl, user));
        if (passiveQueue.Count == 1)
            StartCoroutine(TryPassive());
    }
    private IEnumerator TryPassive()
    {
        yield return null;
        if (passiveQueue.Count > 0 && pendingPassive == null)
        {
            var (firstPassiveSkills, firstCtrl, firstUser) = passiveQueue.Dequeue();
            StartCoroutine(HandlePassiveRoutine(firstPassiveSkills, firstCtrl, firstUser));
        }
    }
    public IEnumerator HandlePassiveRoutine(List<PassiveSkill> passiveSkills, PassiveSkilCtrl ctrl, CharacterHealth user)
    {
        yield return null;

        pendingPassives += passiveSkills.Count;
        foreach (PassiveSkill passiveSkill in passiveSkills)
        {
            Debug.Log($"{user.character_data.characterName}發動了被動:{passiveSkill.skillName}");
            bool NotWaitingCard = WaitCardManager.Instance.IsIdle;
            OnAnyPassiveSkillBegin?.Invoke(user, passiveSkill);
            foreach (var passiveEntry in passiveSkill.passiveEntry)
            {
                pendingPassive = passiveSkill;
                pendingPassiveCtrl = ctrl;
                pendingUser = user;

                // 效果是否可執行
                if (!ctrl.Executable_PassiveEntry.Contains(passiveEntry))
                {
                    Debug.Log("有子類型不滿足");
                    continue;
                }
                foreach (var entry in passiveEntry.effectEntries)
                {
                    pendingEffectEntry = entry;
                    if (entry.NeedChoose)
                    {
                        // 進入選擇狀態
                        inputValue?.CloseInput();
                        selectedTargets.Clear();
                        actionCancelled = false;
                        waitingForAction = false;
                        waitingForTarget = true;
                        if (user.team != TeamID.Enemy)
                            checkButton.SetActive(true);

                        string TargetTip = "";
                        switch (entry.targetType)
                        {
                            case TargetType.Self:
                                TargetTip = "自己";
                                break;
                            case TargetType.Other:
                                TargetTip = "其他人";
                                break;
                            case TargetType.Any:
                                TargetTip = "任何人";
                                break;
                        }
                        UseTips.text = $"{user.character_data.characterName}的被動{passiveSkill.skillName}觸發" +
                            $"選擇目標:{TargetTip}";
                            
                        if (entry.canInputValue) 
                            inputValue.CanInput(entry.MaxInputValue);
                        if (entry.targetType == TargetType.Self) 
                            user.SkillClick();

                        // 等待玩家完成選擇
                        yield return new WaitUntil(() => !waitingForTarget);
                        if (actionCancelled) break;
                    }
                    else
                    {
                        // 直接套用效果
                        pendingUser = user;
                        ApplyEffectImmediate(entry);
                    }
                }
                // 等待效果結束
                if (!NotWaitingCard)
                    yield return new WaitUntil(() => WaitCardManager.Instance == null || WaitCardManager.Instance.IsIdle);
            }
            yield return null;
            pendingPassives--;
            pendingPassive = passiveSkill;
            pendingPassiveCtrl = ctrl;
            pendingUser = user;
            ctrl.PassiveFinish(passiveSkill);
            OnAnyPassiveSkillEnd?.Invoke(pendingUser, pendingPassive);
            Debug.Log($"{user.character_data.characterName}的被動:{passiveSkill.skillName}結算完成");
        }
        FinishPassive();
    }
    private void FinishPassive() // 結尾清理
    {
        if (pendingPassive != null && pendingPassiveCtrl != null && pendingUser != null)
        {
            pendingPassiveCtrl.Executable_PassiveEntry.Clear();
        }
        selectedTargets.Clear();
        checkButton.SetActive(false);
        UseTips.text = "";

        Remake();
        waitingForAction = true;
        waitingForTarget = false;

        if (passiveQueue.Count > 0 && pendingPassive == null)
        {
            StartCoroutine(TryPassive());
            return;
        }

        if (TurnEnded && passiveQueue.Count == 0)
        {
            // 回合結束後沒有剩餘被動，回合結束
            if (currentEndTurn != null)
                StopCoroutine(currentEndTurn);
                
            currentEndTurn = StartCoroutine(EndTurnDelay());
        }
    }
    #endregion

    #region Turn end

    private Coroutine currentEndTurn = null;
    public void TryToEndTrun()
    {
        if (actingPlayer.team != TeamID.Enemy)
            EndTurn();
    }
    public void EndTurn()
    {
        TurnEnded = true;
        endTurnButton.SetActive(false);
        OnTurnEnd?.Invoke(actingPlayer);
        Debug.Log("<color=#FFDD55>回合結束</color>");
        currentEndTurn = StartCoroutine(EndTurnDelay());
    }
    private IEnumerator EndTurnDelay()
    {
        yield return new WaitForSeconds(0.1f);   // 等待0.1秒
        Debug.Log($"<color=#FFDD55>等待結算結束</color>,當前:{pendingPassives};{pendingEffectEntrys}");
        yield return new WaitUntil(() =>
        pendingPassives == 0 && pendingEffectEntrys == 0 &&
        (WaitCardManager.Instance == null || WaitCardManager.Instance.IsIdle)
        );
        currentEndTurn = null;
        ContinueTurnEnd();
    }
    private void ContinueTurnEnd()// 徹底回合結算結束
    {
        if(TurnEnded)
        {
            Debug.Log("回合徹底結算<color=red>結束</color>");
            OnRealTurnEnd?.Invoke(actingPlayer);

            // 重置狀態
            TurnEnded = false;
            actingPlayer.ISActive = false;
            waitingForTarget = false;
            waitingForAction = true;
            Remake();

            // 重置技能使用次數
            skillUseCounter.Clear();
            foreach (var ch in turnOrder)
            {
                var passiveCtrl = ch.GetComponent<PassiveSkilCtrl>();
                if (passiveCtrl != null)
                {
                    passiveCtrl.ResetPassives();
                }
            }
            pendingPassives = 0;
            pendingEffectEntrys = 0;
            CheckBattleEnd();
            if (IsBattleOver) return;

            StartCoroutine(StartTurnDelay());
        }
    }
    #endregion

    #region Battle Over

    private bool IsBattleOver { get; set; } = false;

    private void CheckBattleEnd()
    {
        if (IsBattleOver) return;

        if (turnOrder.All(c => c.team == TeamID.Team1))
        {
            playerVictory = true;
            IsBattleOver = true;
            battleOverPanel.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
            battleOverPanel.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
            if (GameModeManager.Instance.gameMode == GameMode.story)
            {
                UpdateTurnText("玩家勝利");
                Debug.Log("玩家勝利");
                battleOverPanel.GetComponentInChildren<TMP_Text>().text = "玩家勝利";
            }
            else
            {
                UpdateTurnText("隊伍 1 勝利");
                Debug.Log("隊伍 1 勝利");
                battleOverPanel.GetComponentInChildren<TMP_Text>().text = "隊伍 1 勝利";
            }
            battleOverPanel.SetActive(true);
        }
        else if (turnOrder.All(c => c.team == TeamID.Team2))
        {
            playerVictory = false;
            UpdateTurnText("隊伍 2 勝利");
            Debug.Log("隊伍 2 勝利");
            IsBattleOver = true;
            battleOverPanel.SetActive(true);
            battleOverPanel.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
            battleOverPanel.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
            battleOverPanel.GetComponentInChildren<TMP_Text>().text = "隊伍 2 勝利";
        }
        else if (turnOrder.All(c => c.team == TeamID.Enemy))
        {
            playerVictory = false;
            UpdateTurnText("敵人勝利");
            Debug.Log("敵人勝利");
            IsBattleOver = true;
            battleOverPanel.SetActive(true);
            battleOverPanel.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
            battleOverPanel.transform.GetChild(0).GetChild(2).gameObject.SetActive(true);
            battleOverPanel.GetComponentInChildren<TMP_Text>().text = "敵人勝利";
        }
    }
    public void ForceEndBattle()
    {
        playerVictory = false;
        UpdateTurnText("強制結束");
        Debug.Log("強制結束");
        IsBattleOver = true;
        battleOverPanel.SetActive(true);
        battleOverPanel.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
        battleOverPanel.transform.GetChild(0).GetChild(2).gameObject.SetActive(true);
        battleOverPanel.GetComponentInChildren<TMP_Text>().text = "撤退";
    }

    #endregion

    // 更新回合顯示
    private void UpdateTurnText(string message)
    {
        if (turnText != null)
            turnText.text = message;
    }

    // 重製狀態
    private void Remake()
    {
        UseTips.text = "";
        pendingUser = null;
        pendingSkill = null;
        pendingPassiveCtrl = null;
        pendingPassive = null;
        pendingEffectEntry = null;
        pendingCard = null;
        pendingCardUI = null;
    }

    #region Tool

    private List<CharacterHealth> GetBestTargets(EffectEntry entry, CharacterHealth self) // 取得最佳目標
    {
        var (isBeneficial, isHarmful) = AnalyzeEffectEntry(entry);
        var candidates = GetCandidateTargets(entry, self, isBeneficial, isHarmful);

        Dictionary<CharacterHealth, float> scores = new();

        foreach (var target in candidates)
        {
            float score = CalculateEntryScore(entry, self, target);
            scores[target] = score;
        }

        return scores
            .OrderByDescending(kv => kv.Value)
            .Take(entry.maxTargets)
            .Select(kv => kv.Key)
            .ToList();
    }
    private float CalculateEntryScore(EffectEntry entry, CharacterHealth self, CharacterHealth target) // 計算一對一分數
    {
        var (isBeneficial, isHarmful) = AnalyzeEffectEntry(entry);

        float total = 0f;

        foreach (var effect in entry.effects)
        {
            float score = EffectTendency(effect, self, target, isBeneficial, isHarmful);
            total += score * Mathf.Abs(effect.multiplier);
        }

        return total;
    }

    private (bool isBeneficial, bool isHarmful) AnalyzeEffectEntry(EffectEntry entry) // 分析益害
    {
        bool isBeneficial = false;
        bool isHarmful = false;

        foreach (var effect in entry.effects)
        {
            switch (effect.effectType)
            {
                case EffectType.Heal:
                case EffectType.DrawCard:
                    isBeneficial = true;
                    break;

                case EffectType.ChangeMaxHP:
                case EffectType.ChangeAttackPower:
                case EffectType.ChangeHealPower:
                case EffectType.ChangeDefense:
                case EffectType.ChangeDamageReduction:
                    isBeneficial |= effect.value >= 0;
                    isHarmful |= effect.value < 0;
                    break;

                case EffectType.ChangeDamageMultiplier:
                    isBeneficial |= effect.multiplier >= 0;
                    isHarmful |= effect.multiplier < 0;
                    break;

                case EffectType.Damage:
                case EffectType.ConsumeHP:
                case EffectType.Discard_Range:
                case EffectType.Discard_Specific:
                    isHarmful = true;
                    break;
            }
        }

        return (isBeneficial, isHarmful);
    }
    private List<CharacterHealth> GetCandidateTargets(EffectEntry entry, CharacterHealth self,
         bool isBeneficial, bool isHarmful) // 取得候選目標
    {
        var allUnits = TurnManager.Instance.turnOrder
            .Where(c => c != null && c.IsAlive).ToList();

        var teammates = allUnits.Where(c => c.team == self.team).ToList();
        var enemies = allUnits.Where(c => c.team != self.team).ToList();

        List<CharacterHealth> list;

        if (isBeneficial && !isHarmful)
            list = teammates;
        else if (isHarmful && !isBeneficial)
            list = enemies;
        else
            list = allUnits;

        // override by targetType
        return entry.targetType switch
        {
            TargetType.Self         => new() { self },
            TargetType.Other        => list.Where(c => c != self).ToList(),
            TargetType.AllOther     => allUnits.Where(c => c != self).ToList(),
            TargetType.All          => allUnits,
            TargetType.Any          => allUnits,
            TargetType.EventTrigger => new() { WaitCardManager.Instance.currentEvent.actor },
            TargetType.teammate     => allUnits.Where(c => c.team == self.team).ToList(),
            TargetType.enemy        => allUnits.Where(c => c.team != self.team).ToList(),
            _                       => list,
        };
    }

    private float EffectTendency(Effect effect, CharacterHealth self, CharacterHealth target,
     bool isBeneficial, bool isHarmful) // 判斷效果數值
    {
        float effectScore = 0f;

        float missingHP = target.currentMaxHP - target.currentHealth;
        float value = effect.value * effect.multiplier;
        switch (effect.effectType)
        {
            case EffectType.Heal: // 滿血 = 0分
                effectScore = missingHP > 0 ? (missingHP / target.currentMaxHP)*2f +
                Mathf.Min(missingHP , value + self.currentHealPower)*0.3f : 0f;
                break;

            case EffectType.Damage: // 越少血越值得打
                effectScore = ((missingHP + 1) / target.currentMaxHP) * 2f + 
                (effect.value + self.currentAttackPower - target.currentDefense) * effect.multiplier * 0.3f;
                break;
            case EffectType.ConsumeHP: // 越少血越值得打
                effectScore = ((missingHP + 1) / target.currentMaxHP) * 2f + value * 0.3f;
                break;

            case EffectType.Discard_Range:
                var handCount = target.ownerPlayer != null ? target.ownerPlayer.hand.Count : 0;
                effectScore = handCount > 0 ? Mathf.Min(handCount, value) * 0.3f : 0f;
                break;

            case EffectType.DrawCard:
                if (target.ownerPlayer != null)
                    effectScore = value * 0.3f;
                break;

            case EffectType.ChangeMaxHP:
            case EffectType.ChangeAttackPower:
            case EffectType.ChangeHealPower:
            case EffectType.ChangeDefense:
            case EffectType.ChangeDamageReduction:
                effectScore = value > 0 ? value * 0.25f : value * 0.3f;
                break;
            case EffectType.ChangeDamageMultiplier:
                effectScore = effect.multiplier > 0 ? effect.multiplier * 1f : effect.multiplier * 1.2f;
                break;

            case EffectType.GetContinuedEffect:
                if (effect.continuedEffect == null) break;
                float TotalContinuedEffectScore = 0;
                foreach (var continuedEffectEntry in effect.continuedEffect.continuedEffectEntrys)
                {
                    foreach (var continuedEffect in continuedEffectEntry.effects)
                    {
                        float continuedEffectScore = EffectTendency(continuedEffect, self, target, isBeneficial, isHarmful);
                        TotalContinuedEffectScore += continuedEffectScore;
                    }
                }
                effectScore = TotalContinuedEffectScore * (1 / effect.continuedEffect.continuedEffectEntrys.Count) *1.6f;
                break;
        }
        // 敵人加分 or 隊友加分方向
        if (isBeneficial)
            effectScore *= (target.team == self.team ? 1f : -1f);
        if (isHarmful)
            effectScore *= (target.team == self.team ? -1f : 1f);
        return effectScore;
    }

    #endregion
}
