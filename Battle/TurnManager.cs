using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Unity.Mathematics;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    public static bool playerVictory = false;
    // 回合系統
    [Header("UI 綁定")]
    public TMP_Text turnText;
    public TMP_Text UseTips;
    public GameObject skillBarCtrl;
    public InputValue inputValue;
    public GameObject checkButton;
    public GameObject endTurnButton;
    public GameObject battleOverPanel;

    public Player player1;
    public Player player2;
    [HideInInspector] public List<CharacterHealth> selectedTargets = new List<CharacterHealth>();  // 目標清單

    // 角色狀態
    [HideInInspector] public bool waitingForAction = false;         // 等待行動狀態
    [HideInInspector] public bool waitingForTarget = false;         // 等待選擇狀態
    [HideInInspector] public Player actingPlayer;                   // 回合進行中角色
    [HideInInspector] public CharacterHealth pendingUser;           // 技能/卡片的實際使用者
    private EffectEntry pendingEffectEntry;        // 暫存效果組合
    private Skill pendingSkill;                    // 暫存技能
    private PassiveSkill pendingPassive;           // 暫存被動技能
    private PassiveEntry pendingPassiveEntry;      // 暫存被動組合
    private EffectInstance pendingContinuedEffect; // 暫存持續效果

    // 執行狀態
    [HideInInspector] public bool GameStart = false;        // 遊戲開始狀態
    [HideInInspector] public bool TurnEnded = false;        // 回合結束狀態
    [HideInInspector] public bool actionCancelled = false;  // 取消了

    // 技能使用次數
    [HideInInspector] public Dictionary<(CharacterHealth, Skill), int> skillUseCounter
     = new Dictionary<(CharacterHealth, Skill), int>();
    // 被動技能序列
    // private bool isProcessingPassive = false;
    [HideInInspector] 
    public Queue<(List<PassiveSkill>, List<PassiveEntry>, PassiveSkilCtrl, CharacterHealth, CharacterHealth)> passiveQueue 
     = new Queue<(List<PassiveSkill>, List<PassiveEntry>, PassiveSkilCtrl, CharacterHealth, CharacterHealth)>();
    // 效果序列
    private bool isProcessingEntry = false;
    private Queue<ExecutionEffectEntry> effectEntryQueue  = new Queue<ExecutionEffectEntry>();
    private class ExecutionEffectEntry
    {
        public EffectEntry Entry { get; }
        public CharacterHealth User { get; }
        public ActionType ActionType { get; }
        public Skill Skill { get; }
        public PassiveSkill PassiveSkill { get; }
        public PassiveEntry PassiveEntry { get; }
        public CharacterHealth Trigger { get; }
        public EffectInstance ContinuedEffect { get; }

        public ExecutionEffectEntry(
            EffectEntry entry,
            CharacterHealth user,
            ActionType actionType,
            Skill skill,
            PassiveSkill passiveSkill,
            PassiveEntry passiveEntry,
            CharacterHealth trigger,
            EffectInstance continuedEffect)
        {
            Entry = entry;
            User = user;
            ActionType = actionType;
            Skill = skill;
            PassiveSkill = passiveSkill;
            PassiveEntry = passiveEntry;
            Trigger = trigger;
            ContinuedEffect = continuedEffect;
        }

        public void Deconstruct(
            out EffectEntry entry,
            out CharacterHealth user,
            out ActionType actionType,
            out Skill skill,
            out PassiveSkill passiveSkill,
            out PassiveEntry passiveEntry,
            out CharacterHealth trigger,
            out EffectInstance continuedEffect)
        {
            entry = Entry;
            user = User;
            actionType = ActionType;
            skill = Skill;
            passiveSkill = PassiveSkill;
            passiveEntry = PassiveEntry;
            trigger = Trigger;
            continuedEffect = ContinuedEffect;
        }
    }

    #region Event
    public static event System.Action OnBattleBegin;
    public static event System.Action OnCancleChoose;
    public static event System.Action<Player> OnTurnStart;
    public static event System.Action<Player> OnTurnEnd;
    public static event System.Action<Player> OnRealTurnEnd;
    // public static event System.Action<CardCtrl> OnCancelCardChoose;

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

    }
    public void Unregister(CharacterHealth character)// 當角色死亡時移除
    {
        if (IsBattleOver) return;

        character.ownerPlayer.playerCharacters.Remove(character);
        StartCoroutine(CharacterSelectionManager.Instance.ShowTeamMenbers(character.ownerPlayer));

        character.IsAlive = false;
        character.gameObject.SetActive(false);
        CheckBattleEnd();
    }

    public bool CanUseSkill(CharacterHealth user, Skill skill)// 是否能使用技能
    {
        var key = (user, skill);

        if (!skillUseCounter.ContainsKey(key))
            skillUseCounter[key] = 0;

        return (skillUseCounter[key] < skill.maxUsesPerTurn) && skill.LimitedTimes;
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
        waitingForAction = true;
        OnBattleBegin?.Invoke();
        StartCoroutine(StartTurnDelay());
    }

    #region Turn Start

    private IEnumerator StartTurnDelay()
    {
        yield return new WaitForSeconds(0.1f);   // 等待0.1秒
        if (!IsBattleOver)
        {
            yield return new WaitUntil(()=> waitingForAction && !waitingForTarget && WaitCardManager.Instance.IsIdle);
            StartTurn();
        }
    }
    public void StartTurn()
    {
        if (player1.playerCharacters.Count == 0 || player2.playerCharacters.Count == 0) return;
        // 清除實體
        foreach(var character in player1.playerCharacters.Where(c => !c.IsAlive))
        {
            Destroy(character.gameObject);
        }
        foreach(var character in player2.playerCharacters.Where(c => !c.IsAlive))
        {
            Destroy(character.gameObject);
        }

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
            StartCoroutine(actingPlayer.TakeTurnAction());
        }
    }

    #endregion

    #region Skill

    public void OnSkillSelected(Skill skill, CharacterHealth user, Player player)
    {
        if (!waitingForAction || isProcessingEntry || !CanProceed() || !canContinue())
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
        if (skill == null || user == null || player == null) yield break;

        Debug.Log($"{user.character_data.characterName}使用了技能:{skill.skillName}");
        OnAnySkillBegin?.Invoke(user, skill);
        foreach(var entry in skill.effectEntries)
        {
            yield return EnqueueEffectEntry(entry, user, ActionType.Skill, skill, null, null, null, null);
        }
    }

    private bool canContinue()
    {
        if (player1.IsDising || player2.IsDising) return false;
        if (player1.IsStealing || player2.IsStealing) return false;

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
        yield return ApplyCardEntrys(cardCtrl.card_data.effectEntrys, cardCtrl, user, player);
        UseCardOver(cardCtrl, user, player);
    }

    public void BeginTriggerCard(CardCtrl cardCtrl, CharacterHealth user, Player player) // 觸發
    {
        if (cardCtrl == null || user == null || player == null ) return;

        StartCoroutine(ApplyCardEntrys(cardCtrl.card_data.initiativeTiggerEffect, cardCtrl, user, player));
    }

    // 執行卡片效果
    private IEnumerator ApplyCardEntrys(List<EffectEntry> entries, CardCtrl cardCtrl, CharacterHealth user, Player player)
    {
        foreach(var entry in entries)
        {
            yield return EnqueueEffectEntry(entry, user, ActionType.Card, null, null, null, null, null);
        }
        yield return null;
    }
    private List<CharacterHealth> CardEffectTarget(EffectEntry effectEntry, CharacterHealth user, Player player)
    {
        List<CharacterHealth> targets = new List<CharacterHealth>();
        List<CharacterHealth> turnOrder = new List<CharacterHealth>();
        turnOrder.AddRange(player1.playerCharacters);
        turnOrder.AddRange(player2.playerCharacters);

        switch(effectEntry.targetType)
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
        return targets;
    }
    private void UseCardOver(CardCtrl cardCtrl, CharacterHealth user, Player player)
    {
        Debug.Log($"(P{player.Player_nunber}){cardCtrl.card_data.cardName}:Used");
        OnAnyCardPlayed.Invoke(player, cardCtrl.card_data);

        WaitCardManager.Instance.ResolveCard(cardCtrl);
        user.usingCard = null;

        player.UseCardOver(cardCtrl);
    }

    public void ConfirmFold()// 確認棄牌按鈕按下
    {
        if (!player1.DiscardPossible() && !player2.DiscardPossible()) return;

        player1.DiscardConfirm();
        player2.DiscardConfirm();
    }

    public void ConfirmSteal()// 偷牌按鈕按下
    {
        if (!player1.IsStealing && !player2.IsStealing) return;

        player1.ConfirmStealCards();
        player2.ConfirmStealCards();
    }
    #endregion

    #region Effect Apply

    private IEnumerator ApplyEffectImmediate(EffectEntry entry, ActionType actionType,
        CharacterHealth user, CharacterHealth trigger) // 立即生效的效果
    {
        if (user == null)
        {
            Debug.LogWarning("未設定角色");
            yield break;
        }
        List<CharacterHealth> turnOrder = new List<CharacterHealth>();
        turnOrder.AddRange(player1.playerCharacters);
        turnOrder.AddRange(player2.playerCharacters);

        switch (entry.targetType)
        {
            case TargetType.Self:
                selectedTargets = new List<CharacterHealth> { user };
                break;
            case TargetType.AllOther:
                selectedTargets = new List<CharacterHealth>(turnOrder);
                selectedTargets.Remove(user);
                break;
            case TargetType.All:
                selectedTargets = new List<CharacterHealth>(turnOrder);
                break;
            case TargetType.EventTrigger:
                if (actionType == ActionType.Card)
                    selectedTargets = new List<CharacterHealth>{ WaitCardManager.Instance?.currentEvent.actor };
                else if (trigger != null)
                    selectedTargets = new List<CharacterHealth> { trigger };
                break;
            case TargetType.teammate:
                selectedTargets = new List<CharacterHealth>(turnOrder.Where(c => c.team == user.team));
                break;
            case TargetType.enemy:
                selectedTargets = new List<CharacterHealth>(turnOrder.Where(c => c.team != user.team));
                break;
        }
        selectedTargets = new List<CharacterHealth>
        (selectedTargets.Where(c => !LimitChecker.Limited(entry.targetsNeeds, c, user)));

        yield return EffectExecutor.ApplyEffects(user, selectedTargets, entry.effects, entry);
    }

    public IEnumerator EnqueueEffectEntry(EffectEntry entry, CharacterHealth user, ActionType actionType,
        Skill skill,
        PassiveSkill passiveSkill, PassiveEntry passiveEntry, CharacterHealth trigger,
        EffectInstance continuedEffect)
    {
        if (entry == null || user == null || actionType == ActionType.None) yield break;

        var effectEntry = new ExecutionEffectEntry(
            entry,
            user,
            actionType,
            skill,
            passiveSkill,
            passiveEntry,
            trigger,
            continuedEffect
        );

        effectEntryQueue.Enqueue(effectEntry);
        if (!isProcessingEntry)
        {
            yield return (ApplyEffectEntry());
        }
    }
    private IEnumerator ApplyEffectEntry()
    {
        if (isProcessingEntry) yield break;

        isProcessingEntry = true;
        yield return null;

        while (effectEntryQueue.Count > 0)
        {
            var (firstEntry, firstUser, actionType,
                 firstSkill,
                 firstPassiveSkill, firstPassiveEntry, firstTrigger,
                 firstContinuedEffect) 
                = effectEntryQueue.Dequeue();

            pendingEffectEntry = firstEntry;
            pendingUser = firstUser;
            pendingSkill = firstSkill;
            pendingPassive = firstPassiveSkill;
            pendingPassiveEntry = firstPassiveEntry;
            pendingContinuedEffect = firstContinuedEffect;

            waitingForAction = false;
            checkButton.SetActive(false);
            
            // 執行效果
            if (actionType == ActionType.Card)
            {
                Player player = firstUser.ownerPlayer;
                Player enemyPlayer = (player == player1)? player2 : player1;

                List<CharacterHealth> targets = CardEffectTarget(firstEntry, firstUser, player);
                if (targets.Count == 0)
                {
                    continue;
                }

                List<CharacterHealth> aliveTargets = targets.Where(c => c.currentHealth > 0).ToList();
                
                if (aliveTargets.Count == targets.Count || aliveTargets.Count >= firstEntry.maxTargets)
                {
                    targets = aliveTargets;
                }

                targets = targets.Take(firstEntry.maxTargets).ToList();
                
                yield return EffectExecutor.ApplyEffects(firstUser, targets, firstEntry.effects, firstEntry);
            }
            else
            {
                if (firstEntry.NeedChoose)
                {
                    inputValue?.CloseInput();
                    selectedTargets.Clear();
                    actionCancelled = false;
                    waitingForAction = false;
                    waitingForTarget = true;
                    if (firstUser.team != TeamID.Enemy)
                        checkButton.SetActive(true);

                    string TargetTip = "";
                    switch(firstEntry.targetType)
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
                        case TargetType.EventTrigger:
                            TargetTip = "事件觸發者";
                            break;
                        case TargetType.enemy:
                            TargetTip = "敵人";
                            break;
                        case TargetType.teammate:
                            TargetTip = "隊友";
                            break;

                    }
                    switch (actionType)
                    {
                        case ActionType.Skill:
                            if (firstSkill != null)
                                UseTips.text = $"請選擇技能:{firstSkill.skillName}的目標:{TargetTip}";
                            break;
                        case ActionType.PassiveSkill:
                            if (firstPassiveSkill != null)
                                UseTips.text = $"請選擇被動:{firstPassiveSkill.skillName}的目標:{TargetTip}";
                            break;
                    }
                    if (firstEntry.canInputValue) inputValue.CanInput(firstEntry.MaxInputValue);

                    // 自動選擇
                    yield return AutoChoseTarget(firstEntry, firstUser);
                    // 等待玩家完成選擇
                    yield return new WaitUntil(() => !waitingForTarget && canContinue());
                    if (actionCancelled)
                    {
                        continue;
                    }
                    else
                    {
                        yield return EffectExecutor.ApplyEffects(firstUser, selectedTargets, firstEntry.effects, firstEntry);
                    }
                }
                else
                {
                    // 直接套用效果
                    yield return ApplyEffectImmediate(firstEntry, actionType, firstUser, firstTrigger);
                }
            }

            // 結算
            if (actionType == ActionType.Skill && firstSkill != null)
            {
                if (actionCancelled)
                {
                    effectEntryQueue = 
                    new Queue<ExecutionEffectEntry>
                        (
                            effectEntryQueue.Where(e => e.Skill != firstSkill)
                        );
                }
                if (!effectEntryQueue.Any(e => e.Skill == firstSkill))
                {
                    if (!actionCancelled || firstEntry != firstSkill.effectEntries[0])
                    {
                        // 結尾清理
                        Debug.Log($"{firstUser.character_data.characterName}的技能:{firstSkill.skillName}結算完畢");
                        var key = (firstUser, firstSkill);

                        if (!skillUseCounter.ContainsKey(key))
                            skillUseCounter[key] = 0;

                        skillUseCounter[key]++;
                        OnAnySkillEnd?.Invoke(firstUser, firstSkill);
                    }
                }
            }
            else if (actionType == ActionType.PassiveSkill && firstPassiveEntry != null)
            {
                if (actionCancelled)
                {
                    effectEntryQueue = 
                    new Queue<ExecutionEffectEntry>
                        (
                            effectEntryQueue.Where(e => !firstPassiveEntry.effectEntries.Contains(e.Entry))
                        );
                }
                if (!effectEntryQueue.Any(e => firstPassiveEntry.effectEntries.Contains(e.Entry)))
                {
                    if (!actionCancelled || firstEntry != firstPassiveEntry.effectEntries[0])
                    {
                        // 結尾清理
                        firstUser.passiveSkillCtrl.PassiveFinish(firstPassiveSkill);
                        OnAnyPassiveSkillEnd?.Invoke(pendingUser, firstPassiveSkill);
                        Debug.Log($"{firstUser.character_data.characterName}的被動:{firstPassiveSkill.skillName}結算完成");
                    }
                }
            }
            else if (actionType == ActionType.ContinuedEffect && firstContinuedEffect != null)
            {
                if (actionCancelled)
                {
                    effectEntryQueue = 
                    new Queue<ExecutionEffectEntry>
                        (
                            effectEntryQueue.Where(e => e.ContinuedEffect != firstContinuedEffect)
                        );
                }
                if (firstUser != null)
                {
                    firstUser.effectCtrl.ContinuedEffectApplyOver(firstContinuedEffect);
                }
            }

            yield return null;
            yield return null;
        }
        waitingForAction = true;
        waitingForTarget = false;
        isProcessingEntry = false;
        checkButton.SetActive(false);
        endTurnButton.SetActive(true);

        // 回合結束
        if (TurnEnded && effectEntryQueue.Count == 0)
        {
            if (effectEntryQueue.Count == 0)
            {
                if (currentEndTurn != null)
                    StopCoroutine(currentEndTurn);
                
                currentEndTurn = StartCoroutine(EndTurnDelay());
            }
            else
            {
                yield return ApplyEffectEntry();
            }
        }
    }

    #endregion

    #region Target Select

    public void OnTargetToggled(CharacterHealth target)// 技能選中和取消選中(回報動作)
    {
        if (!waitingForTarget)
        {
            Debug.LogWarning("現在不用選擇");
            return;
        }
        if (!target.IsAlive)
        {
            Debug.LogWarning("目標已死亡");
            return;
        }
        
        // 決定目標上限
        int maxTargets = 1;
        if (pendingEffectEntry != null && pendingUser != null)
        {
            maxTargets = pendingEffectEntry.maxTargets;
            switch(pendingEffectEntry.targetType)
            {
                case TargetType.Self:
                    if (target != pendingUser)
                    {
                        Debug.Log($"{target.character_data.characterName}不滿足目標限制");
                        return;
                    }
                    break;
                case TargetType.Other:
                    if (target == pendingUser)
                    {
                        Debug.Log($"{target.character_data.characterName}不滿足目標限制");
                        return;
                    }
                    break;
                case TargetType.teammate:
                    if (target.team != pendingUser.team)
                    {
                        Debug.Log($"{target.character_data.characterName}不滿足目標限制");
                        return;
                    }
                    break;
                case TargetType.enemy:
                    if (target.team == pendingUser.team)
                    {
                        Debug.Log($"{target.character_data.characterName}不滿足目標限制");
                        return;
                    }
                    break;


            }
            if (LimitChecker.Limited(pendingEffectEntry.targetsNeeds, target, pendingUser))
            {
                Debug.Log($"{target.character_data.characterName}不滿足目標限制");
                return;
            }
        }

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
            else
            {
                selectedTargets.Add(target);
                target.ToggleHighlight(true);
                Debug.Log($"選取了 {target.character_data.characterName}");
            }

        }
    }
    public void TryToCancelSelection()// 取消選擇狀態(物件回報動作)
    {
        if (pendingUser != null && pendingUser.team != TeamID.Enemy)
            CancelSelection();
    }
    public void CancelSelection()// 取消選擇狀態
    {
        if (!waitingForTarget) return;
        if (pendingEffectEntry != null && !pendingEffectEntry.canCancle)
        {
            Debug.LogWarning($"本選擇不能取消");
            LogWarning.Instance.Warning($"本選擇不能取消");
            return;
        }
        else
        {
            LogWarning.Instance.Warning("取消選擇目標");
            // 清除選中的角色高亮
            foreach (var target in selectedTargets)
            {
                target.ToggleHighlight(false);
            }
            selectedTargets.Clear();
            UseTips.text = "";
            checkButton.SetActive(false);
            actingPlayer.ISActive = true;
            if (actingPlayer.team != TeamID.Enemy)
                endTurnButton.SetActive(true);
            
            actionCancelled = true;
            waitingForTarget = false;
            OnCancleChoose?.Invoke();
        }
    }

    public void TryToConfirm()// 技能或卡牌確定好目標(回報動作)
    {
        if (pendingUser != null && pendingUser.team != TeamID.Enemy)
            ConfirmTargets();
        else
            Debug.Log("現在無法確認");
    }
    public void ConfirmTargets()// 技能或卡牌確定好目標
    {
        if (!waitingForTarget || pendingUser == null)
        {
            Debug.LogWarning("沒有執行角色");
            return;
        }
        if (selectedTargets.Count == 0)
        {
            LogWarning.Instance.Warning("至少需選擇一個目標");
            Debug.LogWarning("至少需選擇一個目標");
            return;
        }
        // 確認目標
        if (pendingUser != null && pendingEffectEntry != null)
        {
            if (pendingSkill != null 
                && !TragetChecker.SkillCheckTarget(pendingEffectEntry, selectedTargets, pendingUser, pendingSkill))
            {
                Debug.LogWarning($"{pendingSkill.skillName}未達到目標要求");
                LogWarning.Instance.Warning($"{pendingSkill.skillName}未達到目標要求");
                return;
            }
            else if (pendingPassive != null 
                && !TragetChecker.PassiveSkillCheckTarget(pendingEffectEntry, selectedTargets, pendingUser, pendingPassive))
            {
                Debug.LogWarning($"{pendingPassive.skillName}未達到目標要求");
                LogWarning.Instance.Warning($"{pendingPassive.skillName}未達到目標要求");
                return;
            }
            Debug.Log("確定好目標");
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
            actionCancelled = false;
            waitingForTarget = false;
        }
    }
    
    #endregion

    #region Passive

    public void EnqueuePassive(List<PassiveSkill> passiveSkills,
        List<PassiveEntry> entries, PassiveSkilCtrl ctrl, CharacterHealth user, CharacterHealth trigger)
    {
        passiveQueue.Enqueue((passiveSkills, entries, ctrl, user, trigger));
        if (passiveQueue.Count == 1)
        {
            StartCoroutine(TryPassive());
        }
    }
    private IEnumerator TryPassive()
    {
        while(passiveQueue.Count > 0)
        {
            var (firstPassiveSkills, firstEntry, firstCtrl, firstUser, firstTrigger) = passiveQueue.Dequeue();
            yield return HandlePassiveRoutine(firstPassiveSkills, firstEntry, firstCtrl, firstUser, firstTrigger);
        }
    }
    public IEnumerator HandlePassiveRoutine(List<PassiveSkill> passiveSkills,
        List<PassiveEntry> entries, PassiveSkilCtrl ctrl, CharacterHealth user, CharacterHealth trigger)
    {
        foreach (PassiveSkill passiveSkill in passiveSkills)
        {
            // 檢查發動次數
            var key = (user, passiveSkill);
            if (!user.passiveSkillCtrl.passiveUseCounter.ContainsKey(key))
            {
                user.passiveSkillCtrl.passiveUseCounter[key] = 0;
            }
            if (ctrl.passiveUseCounter[key] >= passiveSkill.maxTriggersPerTurn && passiveSkill.LimitedTimes)
            {
                Debug.Log($"{passiveSkill.skillName}已達觸發上限");
                continue;
            }

            Debug.Log($"{user.character_data.characterName}發動了被動:{passiveSkill.skillName}");
            OnAnyPassiveSkillBegin?.Invoke(user, passiveSkill);
            foreach (var passiveEntry in passiveSkill.passiveEntry)
            {
                // 效果是否可執行
                if (!entries.Contains(passiveEntry))
                {
                    Debug.Log("有子類型不滿足");
                    continue;
                }
                foreach (var entry in passiveEntry.effectEntries)
                {
                    Debug.Log($"{passiveSkill.skillName}執行效果集:{passiveEntry.effectEntries.IndexOf(entry)}");
                    yield return 
                    EnqueueEffectEntry(entry, user, ActionType.PassiveSkill,
                         null, passiveSkill, passiveEntry, trigger, null);
                }
            }
        }
    }

    #endregion

    #region Turn end

    private Coroutine currentEndTurn = null;
    public void TryToEndTrun()
    {
        if (player1.IsDising || player2.IsDising || player1.IsStealing || player2.IsStealing) return;
        if (!waitingForAction || waitingForTarget) return;
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
        Debug.Log($"<color=#FFDD55>等待結算結束</color>");
        yield return new WaitForSeconds(0.1f);   // 等待0.1秒
        yield return new WaitUntil(() =>
        !isProcessingEntry &&
        (WaitCardManager.Instance == null || WaitCardManager.Instance.IsIdle)
        );
        currentEndTurn = null;
        yield return new WaitForSeconds(0.1f);   // 等待0.1秒
        if (TurnEnded)
            yield return ContinueTurnEnd();
    }
    private IEnumerator ContinueTurnEnd()// 徹底回合結算結束
    {
        Debug.Log("回合徹底結算結束");
        TurnEnded = false;
        OnRealTurnEnd?.Invoke(actingPlayer);
        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(()=> passiveQueue.Count == 0);
        // 重置狀態
        actingPlayer.ISActive = false;
        waitingForTarget = false;
        waitingForAction = true;
        Remake();

        // 重置技能使用次數
        skillUseCounter.Clear();
        foreach (var ch in player1.playerCharacters)
        {
            var passiveCtrl = ch.GetComponent<PassiveSkilCtrl>();
            if (passiveCtrl != null)
            {
                passiveCtrl.ResetPassives();
            }
        }
        foreach (var ch in player2.playerCharacters)
        {
            var passiveCtrl = ch.GetComponent<PassiveSkilCtrl>();
            if (passiveCtrl != null)
            {
                passiveCtrl.ResetPassives();
            }
        }
        CheckBattleEnd();
        if (IsBattleOver) yield break;

        StartCoroutine(StartTurnDelay());
        
    }
    #endregion

    #region Battle Over

    private bool IsBattleOver { get; set; } = false;

    private void CheckBattleEnd()
    {
        if (IsBattleOver) return;

        List<CharacterHealth> turnOrder = new List<CharacterHealth>();
        turnOrder.AddRange(player1.playerCharacters);
        turnOrder.AddRange(player2.playerCharacters);

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
        pendingPassive = null;
        pendingEffectEntry = null;
    }

    #region Tool
    
    public float CalculateCardScore(Card card, CharacterHealth character)
    {
        float finalScore = 0;
        foreach(var entry in card.effectEntrys)
        {
            List<CharacterHealth> targets = TurnManager.Instance.CardEffectTarget(entry, character, character.ownerPlayer);
            foreach(var target in targets)
            {
                finalScore += CalculateEntryScore(entry, character, target);
            }
        }
        return finalScore;
    }

    private IEnumerator AutoChoseTarget(EffectEntry entry, CharacterHealth user)
    {
        if (!entry.NeedChoose || !waitingForTarget)
            yield break;
        
        foreach(var target in GetBestTargets(entry, user))
        {
            float score = CalculateEntryScore(entry, user, target);
            if (score > 0)
            {
                Debug.Log($"自動選擇目標:{target.character_data.characterName};{math.ceil(score * 100) / 100f}分");
                OnTargetToggled(target);
            }
            else
            {
                Debug.Log($"取消自動選擇目標:{target.character_data.characterName};{math.ceil(score * 100) / 100f}分");
            }

            yield return new WaitForSeconds(0.1f);
        }
        if (entry.canInputValue)
        {
            inputValue.ChangeValueToFixed(entry.MaxInputValue);
        }

        if (user.ownerPlayer.AutoActivity)
        {
            if (selectedTargets.Count > 0)
                ConfirmTargets();
            else 
                CancelSelection();
        }
    }

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
            .Where(c => !LimitChecker.Limited(entry.targetsNeeds, c, self))
            .ToList();
    }
    private float CalculateEntryScore(EffectEntry entry, CharacterHealth self, CharacterHealth target) // 計算一對一分數
    {
        var (isBeneficial, isHarmful) = AnalyzeEffectEntry(entry);

        float total = 0f;

        CharacterHealth finalTarget = target;
        foreach (var effect in entry.effects)
        {
            if (effect.effectTarget == EffectiveTarget.Initiator)
                finalTarget = self;
            else if (effect.effectTarget == EffectiveTarget.target)
                finalTarget = target;
            
            float score = EffectTendency(effect, self, finalTarget, isBeneficial, isHarmful);
            total += score;
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
        List<CharacterHealth> turnOrder = new List<CharacterHealth>();
        turnOrder.AddRange(player1.playerCharacters);
        turnOrder.AddRange(player2.playerCharacters);

        var allUnits = turnOrder
            .Where(c => c.character_data != null && c.IsAlive).ToList();

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
        if (effect == null || self == null || target == null)
            return 0;
        
        float effectScore = 0f;

        float missingHP = target.currentMaxHP - target.currentHealth;
        float changeValue = effect.value * effect.multiplier;
        float targetvalue = EffectExecutor.GetValue(effect.targetValueEntry, self, target);
        int value = Mathf.RoundToInt(changeValue + targetvalue);
        switch (effect.effectType)
        {
            case EffectType.Heal: // 滿血 = 0分
                effectScore = missingHP > 0 ? Mathf.Clamp01((float) missingHP / target.currentMaxHP) * 0.5f +
                Mathf.Min(missingHP , value + self.currentHealPower)*0.3f : 0f;
                break;

            case EffectType.Damage: // 越少血越值得打
                effectScore = target.currentMaxHP > 0 ? Mathf.Clamp01(missingHP / target.currentMaxHP) * 0.5f + 
                (effect.value + self.currentAttackPower - target.currentDefense) * effect.multiplier * 0.3f : 
                (effect.value + self.currentAttackPower - target.currentDefense) * effect.multiplier * 0.3f;
                break;
            case EffectType.ConsumeHP: // 越少血越值得打
                effectScore = target.currentMaxHP > 0 ? Mathf.Clamp01(missingHP / target.currentMaxHP) * 2f + 
                value * 0.3f : value * 0.3f;
                break;

            case EffectType.Discard_Range:
                var handCount = target.ownerPlayer != null ? target.ownerPlayer.hand.Count : 0;
                effectScore = handCount > 0 ? Mathf.Min(handCount, value) * 1.2f : 0f;
                break;

            case EffectType.DrawCard:
                if (target.ownerPlayer != null)
                    effectScore = value * 1.5f;
                break;

            case EffectType.ChangeMaxHP:
                effectScore = value > 0 ? value * 0.3f : value * 0.3f;
                break;
            case EffectType.ChangeAttackPower:
            case EffectType.ChangeHealPower:
            case EffectType.ChangeDefense:
            case EffectType.ChangeDamageReduction:
                effectScore = value > 0 ? value * 1.5f : value * 1.5f;
                break;
            case EffectType.ChangeDamageMultiplier:
                effectScore = effect.multiplier > 0 ? effect.multiplier * 1f : effect.multiplier * 1.2f;
                break;

            case EffectType.GetContinuedEffect:
                if (effect.continuedEffect == null) break;
                float TotalContinuedEffectScore = 0;
                foreach (var continuedEffectEntry in effect.continuedEffect.continuedEffectEntrys)
                {
                    foreach (var continuedEffect in continuedEffectEntry.effectEntry.effects)
                    {
                        float continuedEffectScore = EffectTendency(continuedEffect, self, target, isBeneficial, isHarmful);
                        TotalContinuedEffectScore += continuedEffectScore;
                    }
                }
                effectScore = TotalContinuedEffectScore * (1 / effect.continuedEffect.continuedEffectEntrys.Count) *1.6f;
                break;
            case EffectType.StealCards_Range:
            case EffectType.StealCards_Specific:
                var stealhandCount = target.ownerPlayer != null ? target.ownerPlayer.hand.Count : 0;
                effectScore = stealhandCount > 0 ? Mathf.Min(stealhandCount, value) * 2f : 0f;
                break;
            case EffectType.SummonTeammate:
            case EffectType.SummonEnemy:
                effectScore = 5f;
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
