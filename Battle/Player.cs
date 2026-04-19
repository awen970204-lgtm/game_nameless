using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;

// 掛在玩家上
public class Player : MonoBehaviour
{
    public int Player_nunber;
    public TeamID team;
    public int playerLevel = 1;
    [Header("UI")]
    public Transform Player_characterTransform;  // 角色區
    public Transform Player_skillTransform;      // 技能欄
    public Transform Player_PassiveSkillTransform;
    public Transform handArea;                   // 手牌顯示區域
    public TMP_Text PlayerMenbers;
    [Header("Prefab")]
    public GameObject cardPrefab;                // 卡片預製物
    public GameObject floatingTextPrefab;        // 血量變化預製物
    public GameObject floatingSkillTextPrefab;   // 技能提示預製物
    [Header("Button")]
    public GameObject discardButton;             // 棄置按鈕
    public GameObject stealCardButton;           // 偷取按鈕
    public Button SelectButton;

    // 判定用
    [HideInInspector] public bool ISActive = false;
    [HideInInspector] public int turnCount = 0;

    // 角色
    [HideInInspector] public int MaxMenber = 1;
    [HideInInspector] public List<CharacterHealth> playerCharacters;       // 玩家操控的角色
    [HideInInspector] public List<Card> hand = new List<Card>();           // 手牌
    [HideInInspector] public List<CardCtrl> handUI = new List<CardCtrl>(); // 手牌
    public bool AutoActivity = false;
    private bool HasTakeAction = false;

    private List<Card> drawPile = new List<Card>();    // 當前可抽取
    private List<Card> discardPile = new List<Card>(); // 棄牌堆
    [HideInInspector] public int DrawCardsInTrun = 0;

    [HideInInspector] public List<CardCtrl> disCard = new List<CardCtrl>();        // 要棄置的牌
    [HideInInspector] public List<CardCtrl> stealCardBuffer = new List<CardCtrl>();// 被選中要偷的牌
    [HideInInspector] public Player Thief = null;            // 偷人的玩家
    [HideInInspector] public bool IsDising = false;          // 是否在被棄牌狀態
    [HideInInspector] public bool IsStealing = false;        // 是否在被偷牌狀態
    [HideInInspector] public int needDis = 0;                // 需要棄多少張
    [HideInInspector] public int needSteal = 0;              // 需要偷多少張

    public static event System.Action<Player, int> OnPlayerDrawCard;

    void Start()
    {
        SelectButton.onClick.AddListener(()=> SelectTeamMenber());
    }
    
    void SelectTeamMenber()
    {
        CharacterSelectionManager.Instance.SetCurrentPlayer(this);
        CharacterSelectionManager.Instance.chosingPamel.SetActive(true);
    }
    public void SetPlayerLevel(int level)
    {
        playerLevel = level;

        if (playerLevel >= 89)
            MaxMenber = 10;
        else if (playerLevel >= 55)
            MaxMenber = 9;
        else if (playerLevel >= 34)
            MaxMenber = 8;
        else if (playerLevel >= 21)
            MaxMenber = 7;
        else if (playerLevel >= 13)
            MaxMenber = 6;
        else if (playerLevel >= 8)
            MaxMenber = 5;
        else if (playerLevel >= 5)
            MaxMenber = 4;
        else if (playerLevel >= 3)
            MaxMenber = 3;
        else 
            MaxMenber = 2;

        PlayerMenbers.text = $"{playerCharacters.Count}/{MaxMenber}";
    }

    #region Card

    public void PlayCard(CharacterHealth user, CardCtrl cardUI) // 使用牌
    {
        var card = cardUI.card_data;
        if (hand.Contains(card) && handUI.Contains(cardUI) && playerCharacters.Contains(user))
        {
            Debug.Log($"P{Player_nunber}:{user.character_data.characterName} use card:{card.cardName}");
            cardUI.IsUseing = true;
            cardUI.DisplayChange();
            TurnManager.Instance.BeginUseCard(cardUI, user, this);
        }
    }
    public void UseCardOver(CardCtrl cardUI)
    {
        DiscardCard(cardUI.card_data);
    }
    
    public void DrawCard(int count) // 抽牌
    {
        int realDrawCard = 0;
        for (int i = 0; i < count; i++)
        {
            Card card = DrawCard();
            if (card != null)
            {
                // 初始化 UI 和擁有者
                hand.Add(card);
                Debug.Log($"P{Player_nunber} 抽到了 {card.cardName}");

                if (cardPrefab != null && handArea != null)
                {
                    GameObject cardGO = Instantiate(cardPrefab, handArea);
                    CardCtrl ctrl = cardGO.GetComponent<CardCtrl>();
                    handUI.Add(ctrl);

                    ctrl.card_data = card;
                    ctrl.ownerPlayer = this;
                    ctrl.Setup(card, this);
                    cardGO.SetActive(true);
                    realDrawCard++;
                    DrawCardsInTrun++;
                }
                else
                {
                    Debug.Log($"P{Player_nunber} 嘗試抽牌，但牌堆已空");
                }
            }
        }
        OnPlayerDrawCard?.Invoke(this, realDrawCard);
    }

    private void RemoveCard(Card card) // 移除卡牌
    {
        foreach (CardCtrl cardCtrl in handUI)
        {
            if (cardCtrl != null && cardCtrl.card_data == card)
            {                
                WaitCardManager.Instance.UnregisterCard(cardCtrl);
                
                hand.Remove(card);
                handUI.Remove(cardCtrl);
                Destroy(cardCtrl.gameObject);
                break;
            }
        }

        TurnManager.Instance.RaiseAnyOnCardRemove(card);
        Debug.Log($"移除卡牌{card.cardName}");
    }

    public void SetDesk(List<Card> list) // 設定牌堆
    {
        drawPile.Clear();
        discardPile.Clear();
        drawPile.AddRange(list);
        Shuffle(drawPile);
    }
    private Card DrawCard() // 抽牌
    {
        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0) return null;
            // 棄牌堆重洗
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }

        Card card = drawPile[0];
        drawPile.RemoveAt(0);
        return card;
    }
    private void DiscardCard(Card card) // 卡片進入棄牌堆
    {
        if (card != null)
        {
            discardPile.Add(card);
            RemoveCard(card);
        }

    }
    private void Shuffle(List<Card> list) // 洗牌
    {
        for (int i = 0; i < list.Count; i++)
        {
            Card temp = list[i];
            int rand = Random.Range(i, list.Count);
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    #endregion
    
    #region AutoTakeTurn

    public IEnumerator TakeTurnAction()
    {
        HasTakeAction = true;
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(()=> TurnManager.Instance.waitingForAction);

        while(HasTakeAction && AutoActivity)
        {
            yield return TakeAction();
            yield return new WaitUntil(()=> TurnManager.Instance.waitingForAction);
            yield return new WaitForSeconds(0.6f);
        }

        yield return new WaitUntil(()=> TurnManager.Instance.waitingForAction);
        if (AutoActivity)
            TurnManager.Instance.EndTurn();
    }
    private IEnumerator TakeAction()
    {
        if (playerCharacters.Count == 0 || !AutoActivity)
            yield break;
        
        HasTakeAction = false;
        yield return new WaitForSeconds(1f);
        // 用技能
        foreach(var character in playerCharacters)
        {
            foreach(var skill in character.currentSkills.Where(s => !character.invalidSkills.Contains(s)))
            {
                if (!TurnManager.Instance.CanUseSkill(character, skill))
                    continue;
                if (LimitChecker.Limited(skill.skillNeed, character, TurnManager.Instance.actingPlayer.playerCharacters[0]))
                {
                    continue;
                }

                TurnManager.Instance.OnSkillSelected(skill, character, this);
            }
        }

        // 使用牌
        if (playerCharacters.Where(c => c.usingCard == null).ToList().Count == 0)
            yield break;

        if (hand.Where(c => c.cardType == Card.CARD_TYPE.NOW).ToList().Count > 0) // 立即牌
        {
            foreach(var card in hand.Where(c => c.cardType == Card.CARD_TYPE.NOW))
            {
                Dictionary<CharacterHealth, float> scores = new();
                foreach(var character in playerCharacters.Where(c => c.usingCard == null))
                {
                    scores[character] = TurnManager.Instance.CalculateCardScore(card, character);
                }
                
                CharacterHealth TopScoreCharacter = 
                    scores.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList()[0];
                if (scores[TopScoreCharacter] > 0)
                {
                    CardCtrl cardCtrl = handUI.Find(c => c.card_data == card);
                    if (cardCtrl != null)
                    {
                        HasTakeAction = true;
                        cardCtrl.TryUseOnCharacter(TopScoreCharacter);
                        yield break;
                    }
                }
            }
        }
        if (hand.Where(c => c.cardType == Card.CARD_TYPE.DELAY || c.cardType == Card.CARD_TYPE.WAIT).ToList().Count > 0)
        {
            foreach(var card in hand.Where(c => c.cardType == Card.CARD_TYPE.DELAY || c.cardType == Card.CARD_TYPE.WAIT))
            {
                Dictionary<CharacterHealth, float> scores = new();
                foreach(var character in playerCharacters.Where(c => c.usingCard == null))
                {
                    scores[character] = TurnManager.Instance.CalculateCardScore(card, character);
                }
                
                CharacterHealth TopScoreCharacter = 
                    scores.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList()[0];
                if (scores[TopScoreCharacter] > 0)
                {
                    CardCtrl cardCtrl = handUI.Find(c => c.card_data == card);
                    if (cardCtrl != null)
                    {
                        HasTakeAction = true;
                        cardCtrl.TryUseOnCharacter(TopScoreCharacter);
                        yield break;
                    }
                }
            }
        }
    }
    #endregion

    #region Discard/steal

    public IEnumerator DiscardSpecificCard(Player thief, int count) // 棄掉指定卡片(開始)
    {
        if (hand.Count <= count || thief.team == TeamID.Enemy)
        {
            DiscardRandomCards(count);
            yield break;
        }

        Thief = thief;
        needDis = count;
        IsDising = true;
        discardButton.SetActive(true);

        yield return new WaitUntil(()=> !IsDising);

        needDis = 0;
        Thief = null;
        disCard.Clear();
        discardButton.SetActive(false);
    }
    public bool DiscardPossible() // 是否能棄置
    {
        if (!IsDising) return true;
        else
        {
            if (disCard.Count < needDis)
            {
                LogWarning.Instance.Warning($"未達所需棄置數");
                discardButton.SetActive(true);
                return false;
            }
            else return true;
        }
    }
    public void DiscardConfirm() // 確定棄置
    {
        if (!IsDising) return;

        foreach (var card in disCard)
        {
            if (handUI.Contains(card))
            {
                discardButton.SetActive(false);
                DiscardCard(card.card_data);
                Debug.Log($"P{Player_nunber} 棄掉了 {card.card_data.cardName}");
            }
        }
        IsDising = false;
    }

    public void DiscardRandomCards(int count) // 隨機棄置卡片
    {
        for (int i = 0; i < count; i++)
        {
            if (hand.Count > 0)
            {
                List<Card> discardable = new List<Card>();
                List<CardCtrl> discardableUI = handUI.Where(c => c.user == null).ToList();
                
                foreach (var c in discardableUI)
                {
                    discardable.Add(c.card_data);
                }

                if (discardable.Count == 0)
                {
                    Debug.Log($"P{Player_nunber} 沒有可以棄置的卡");
                    break;
                }

                // 隨機選一張可棄置的卡
                int randomIndex = UnityEngine.Random.Range(0, discardable.Count);
                Card discarded = discardable[randomIndex];

                DiscardCard(discarded);
            }
        }
    }

    public void StealCardsFrom(Player targetPlayer, int count) // 隨機偷手牌
    {
        if (targetPlayer == null || targetPlayer.handUI.Where(c => c.user == null).ToList().Count == 0)
        {
            Debug.Log($"P{Player_nunber} 嘗試偷牌，但對方沒有手牌！");
            return;
        }

        // 修正偷牌數量，不可超過對方手牌數
        count = Mathf.Min(count, targetPlayer.hand.Count);

        // 隨機選取 count 張牌
        List<Card> stolenCards = new List<Card>();
        List<Card> stolenableCards = new List<Card>(targetPlayer.handUI.Where(c => c.user == null).Select(c => c.card_data));
        for (int i = 0; i < count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, stolenableCards.Count);
            Card selected = stolenableCards[randomIndex];
            stolenCards.Add(selected);
            targetPlayer.RemoveCard(selected); // 先移除目標玩家的牌
        }

        // 加入自己的手牌
        foreach (Card card in stolenCards)
        {
            hand.Add(card);
            if (cardPrefab != null && handArea != null)
            {
                GameObject cardGO = Instantiate(cardPrefab, handArea);
                CardCtrl ctrl = cardGO.GetComponent<CardCtrl>();
                handUI.Add(ctrl);
                cardGO.SetActive(true);
                ctrl.card_data = card;
                ctrl.ownerPlayer = this;
                ctrl.Setup(card, this);
            }
            Debug.Log($"P{Player_nunber} 偷走了 P{targetPlayer.Player_nunber} 的 {card.cardName}");
        }
    }

    public IEnumerator StartBeStealCards(Player thief, int count) // 開始被偷牌
    {
        if (thief == null || hand.Count == 0)
        {
            Debug.Log("目標沒有手牌，無法偷取");
            yield break;
        }
        if (hand.Count <= count || thief.team == TeamID.Enemy)
        {
            thief.StealCardsFrom(this, count);
            yield break;
        }
        Thief = thief;
        needSteal = Mathf.Min(count, hand.Count);
        IsStealing = true;
        stealCardBuffer.Clear();
        Debug.Log($"P{thief.Player_nunber}準備偷P{Player_nunber}的{needSteal}張牌");

        stealCardButton.SetActive(true);
        yield return new WaitUntil(()=> !IsStealing);

        needSteal = 0;
        stealCardBuffer.Clear();
        Thief = null;
        stealCardButton.SetActive(false);

    }
    public void ConfirmStealCards()// 確認被偷牌
    {
        if (!IsStealing || Thief == null) return;
        if (stealCardBuffer.Count < needSteal)
        {
            Debug.Log($"還需要選 {needSteal - stealCardBuffer.Count} 張才能完成偷牌");
            return;
        }

        // 執行偷取
        foreach (var card in stealCardBuffer)
        {
            if (handUI.Contains(card))
            {
                if (cardPrefab != null && handArea != null)
                {
                    GameObject cardGO = Instantiate(cardPrefab, Thief.handArea);
                    CardCtrl ctrl = cardGO.GetComponent<CardCtrl>();
                    Thief.handUI.Add(ctrl);
                    ctrl.card_data = card.card_data;
                    ctrl.ownerPlayer = Thief;
                    cardGO.SetActive(true);
                    ctrl.Setup(card.card_data, Thief);
                }
                RemoveCard(card.card_data);
                Thief.hand.Add(card.card_data);
                Debug.Log($"P{Thief.Player_nunber} 偷走了 P{Player_nunber} 的 {card.card_data.cardName}");
            }
        }

        // 重置狀態
        IsStealing = false;
    }
    #endregion

    #region Showing

    public void ShowFloatingText(string text, Color color, CharacterHealth character)// 顯示血量變化
    {
        if (floatingTextPrefab == null) return;

        GameObject obj = Instantiate(floatingTextPrefab, character.TipsArea);
        TMP_Text tmp = obj.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.color = color;
        }

        obj.SetActive(true);
    }

    public void ShowFloatingSkill(string text, CharacterHealth character)// 顯示技能發動
    {
        if (floatingSkillTextPrefab == null) return;

        GameObject obj = Instantiate(floatingSkillTextPrefab, character.TipsArea);
        TMP_Text tmp = obj.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = text;
        }

        obj.SetActive(true);
    }
    #endregion
}