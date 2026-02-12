using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class WaitCardManager : MonoBehaviour
{
    public static WaitCardManager Instance { get; private set; }

    // 全部玩家的卡
    private List<CardCtrl> waitCards = new List<CardCtrl>();
    private Dictionary<CardCtrl, int> triggerCards = new Dictionary<CardCtrl, int>();
    // 事件序列
    private Queue<WaitEvent> waitEventQueue = new Queue<WaitEvent>();

    public GameObject cancleEvent;                     // 取消事件按鈕
    public TMP_Text eventTips;                         // 事件提示    
    [HideInInspector] public WaitEvent currentEvent;   // 當前執行中的事件
    [HideInInspector] public bool IsIdle = true;       // 是否空閒
    public static event Action OnWaitEventFinished;    // 事件回呼

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void OnEnable()
    {
        TurnManager.OnCardRemove += CheckEventContain;
        TurnManager.OnTurnStart += HandleTurnStart;
        TurnManager.OnTurnEnd += HandleTurnEnd;
        TurnManager.OnRealTurnEnd += HandleRealTurnEnd;

        TurnManager.OnAttackEvent += HandleAttactEvent;
        TurnManager.OnAnyBeHealed += HandleBeHealed;
        TurnManager.OnAnyConsumeHP += HandleConsumeHP;
    }
    void Start()
    {
        currentEvent = null;
        if (eventTips == null || cancleEvent == null)
        {
            Debug.LogError("有物品沒綁定");
            return;
        }
        eventTips.text = "";
    }
    void OnDisable()
    {
        TurnManager.OnCardRemove -= CheckEventContain;
        TurnManager.OnTurnStart -= HandleTurnStart;
        TurnManager.OnTurnEnd -= HandleTurnEnd;
        TurnManager.OnRealTurnEnd -= HandleRealTurnEnd;

        TurnManager.OnAttackEvent -= HandleAttactEvent;
        TurnManager.OnAnyBeHealed -= HandleBeHealed;
        TurnManager.OnAnyConsumeHP -= HandleConsumeHP;
    }

    #region Events Binding

    private void HandleTurnStart(Player player) => EnqueueWaitEvent(TriggerTime.OnTurnStart, 
        TurnManager.Instance.actingPlayer.playerCharacters.Find(c => c.currentHealth > 0));
    private void HandleTurnEnd(Player player) => EnqueueWaitEvent(TriggerTime.OnTurnEnd, 
        TurnManager.Instance.actingPlayer.playerCharacters.Find(c => c.currentHealth > 0));
    private void HandleRealTurnEnd(Player player)
    {
        StartCoroutine(CheckDelayTime(player));
    }
    private void HandleAttactEvent(CharacterHealth attacker, CharacterHealth injured)
    {
        EnqueueWaitEvent(TriggerTime.OnAttact, attacker);
        EnqueueWaitEvent(TriggerTime.OnBeAttacted, injured);
    }
    private void HandleBeHealed(CharacterHealth act) => EnqueueWaitEvent(TriggerTime.OnBeHealed, act);
    private void HandleConsumeHP(CharacterHealth act) => EnqueueWaitEvent(TriggerTime.OnConsumeHP, act);
    
    // 新事件加入序列
    private void EnqueueWaitEvent(TriggerTime triggerTime, CharacterHealth act)
    {
        var related = new List<CardCtrl>();
        foreach (var card in waitCards)
        {
            if (card.card_data.waitTriggerTime != triggerTime) continue;
            if (card.ownerPlayer == null) continue;
            
            if (act == null)
                act = TurnManager.Instance.actingPlayer.playerCharacters[0];
            if (act == null) return;

            if (triggerTime == TriggerTime.OnTurnStart || triggerTime == TriggerTime.OnTurnEnd)
            {
                if (act.team == card.ownerPlayer.team)
                    act = card.user;
            }

            switch (card.card_data.waitTrigger)
            {
                case Trigger_Character.Self:
                    if (card.user == act)
                        related.Add(card);
                    break;
                case Trigger_Character.Other:
                    if (card.user != act)
                        related.Add(card);
                    break;
                case Trigger_Character.All:
                    if (card.user != null)
                        related.Add(card);
                    break;
                case Trigger_Character.teammate:
                    if (card.user.team == act.team)
                        related.Add(card);
                    break;
                case Trigger_Character.enemy:
                    if (card.user.team != act.team)
                        related.Add(card);
                    break;

            }
        }

        if (related.Count > 0)
        {
            Debug.Log("添加事件");
            var newEvent = new WaitEvent
            {
                trigger = triggerTime,
                actor = act,
                relatedCards = new List<CardCtrl>(related)
            };
            waitEventQueue.Enqueue(newEvent);
            TryStartCoroutine();
        }
    }

    private IEnumerator CheckDelayTime(Player acting) // 檢查延時卡
    {
        List<CardCtrl> triggers = new List<CardCtrl>();

        foreach (CardCtrl cardCtrl in triggerCards.Select(kv => kv.Key).Where(c => c.ownerPlayer == acting).ToList())
        {
            if (cardCtrl.ownerPlayer != acting) continue;

            triggerCards[cardCtrl] --;

            if (triggerCards[cardCtrl] <= 0)
            {
                triggers.Add(cardCtrl);
            }
            cardCtrl.DisplayChange();
        }
        if (triggers.Count <= 0) yield break;

        Debug.Log($"<color=#FFDD55>#</color> deal with delayCard");
        IsIdle = false;
        foreach(var cardCtrl in triggers)
        {
            cardCtrl.ownerPlayer.PlayCard(cardCtrl.user, cardCtrl);
            yield return null;
            yield return new WaitUntil(()=> TurnManager.Instance.waitingForAction);
        }
        IsIdle = true;
        Debug.Log($"<color=#FFDD55>#</color> over delayCard");
    }

    #endregion

    // 卡牌註冊
    public void RegisterWaitCard(CardCtrl ctrl)
    {
        if (!waitCards.Contains(ctrl) && ctrl.card_data.cardType == Card.CARD_TYPE.WAIT)
            waitCards.Add(ctrl);
    }
    public void UnregisterCard(CardCtrl ctrl)
    {
        if (ctrl.card_data.cardType == Card.CARD_TYPE.WAIT && waitCards.Contains(ctrl))
            waitCards.Remove(ctrl);
        else if (ctrl.card_data.cardType == Card.CARD_TYPE.DELAY && triggerCards.ContainsKey(ctrl))
            triggerCards.Remove(ctrl);
    }

    public void RegisterDelayCard(CardCtrl ctrl)
    {
        if (!triggerCards.ContainsKey(ctrl) && ctrl.card_data.cardType == Card.CARD_TYPE.DELAY)
        {
            triggerCards[ctrl] = ctrl.card_data.delayTime;
            ctrl.DisplayChange();
        }
    }
    public int GetDelayTurn(CardCtrl ctrl)
    {
        if (!triggerCards.ContainsKey(ctrl))
            triggerCards[ctrl] = ctrl.card_data.delayTime;

        return triggerCards[ctrl];
    }

    // 嘗試執行事件
    private void TryStartCoroutine()
    {
        if (waitEventQueue.Count > 0 && currentEvent == null)
        {
            StartCoroutine(ProcessWaitEvents());
        }
    }
    private IEnumerator ProcessWaitEvents()
    {
        yield return new WaitUntil(() => currentEvent == null);

        if (waitEventQueue.Count <= 0) yield break;
        else currentEvent = waitEventQueue.Dequeue();
        IsIdle = false;
        Debug.Log($"<color=#FFDD55>#</color> deal with event: {currentEvent.trigger}");

        // 顯示提示
        string tip = GetTipText(currentEvent.trigger);
        eventTips.text = $"執行事件: {currentEvent.actor.character_data.characterName}(P{currentEvent.actor.ownerPlayer.Player_nunber}) {tip}";

        TurnManager.Instance.endTurnButton.SetActive(false);
        // 啟用所有相關卡
        foreach (var card in currentEvent.relatedCards)
        {
            card.ownerPlayer.PlayCard(card.user, card);
            yield return null;
            yield return new WaitUntil(()=> TurnManager.Instance.waitingForAction);
        }

        // 等待直到事件被解決
        Debug.Log("等待Wait卡事件解決");
        yield return new WaitUntil(() => currentEvent == null || currentEvent.resolved);

        // 一個事件結束，清理
        eventTips.text = "";
        cancleEvent.SetActive(false);

        EndCurrentEvent();
    }


    // 棄置後檢查卡片
    private void CheckEventContain(Card card)
    {
        if (waitEventQueue.Count == 0) return;

        foreach (var Event in waitEventQueue)
        {
            Event.relatedCards.RemoveAll(c => c.card_data == card);

            if (Event.relatedCards.Count == 0)
            {
                Event.resolved = true;
            }
        }
    }

    // 當某張卡被使用
    public void ResolveCard(CardCtrl ctrl)
    {
        if (currentEvent != null && currentEvent.relatedCards.Contains(ctrl))
        {
            waitCards.Remove(ctrl);
            if (currentEvent.relatedCards.All(c => !waitCards.Contains(c)))
                currentEvent.resolved = true;
        }
        else if (triggerCards.ContainsKey(ctrl))
        {
            triggerCards.Remove(ctrl);
        }
    }

    // 結束當前事件
    private void EndCurrentEvent()
    {
        if (currentEvent == null) return;

        currentEvent.resolved = true;
        currentEvent.relatedCards.Clear();

        // 重製狀態
        Debug.Log($"事件 {currentEvent.trigger} 結束");
        eventTips.text = "";
        currentEvent = null;
        IsIdle = true;

        if (waitEventQueue.Count > 0)
            TryStartCoroutine();
        else OnWaitEventFinished?.Invoke();
    }

    private string GetTipText(TriggerTime trigger)
    {
        switch (trigger)
        {
            case TriggerTime.OnTurnStart:
                return "回合開始";
            case TriggerTime.OnTurnEnd:
                return "回合結束";
            case TriggerTime.OnAttact:
                return "造成傷害";
            case TriggerTime.OnBeAttacted:
                return "受傷";
            case TriggerTime.OnBeHealed:
                return "回血";
            case TriggerTime.OnCardPlayed:
                return "使用牌";
            case TriggerTime.OnCharacterDeath:
                return "死亡";
            case TriggerTime.OnGetEffect:
                return "獲得效果";
            case TriggerTime.OnLoseEffect:
                return "失去效果";
            default:
                return "事件";
        }
    }

}
