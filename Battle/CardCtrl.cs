using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class CardCtrl : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Player ownerPlayer;
    [HideInInspector] public CharacterHealth user;
    [HideInInspector] public Card card_data;

    private RectTransform rectTransform;
    private Transform originalParent;    // 原本的父物件
    private Vector3 originalPosition;    // 記錄初始位置
    private Canvas mainCanvas;           // 主 Canvas (用來顯示在最上層)
    private HandLayoutController handLayout;

    [Header("Display")]
    public GameObject checkButton;
    public GameObject cardAvailableDisplay; // 可用狀態顯示
    public GameObject cardUsingDisplay;     // 使用中狀態顯示
    public GameObject snapDisplay;          // 吸附狀態顯示
    public GameObject LockedDisplay;
    private CanvasGroup canvasGroup;

    [Header("Card")]
    public Image picture;         // 卡圖
    public Image cardTypePicture; // 類型圖
    public TMP_Text card_name;    // 卡名
    public TMP_Text cardTip;      // 用來顯示提示
    public TMP_Text delayTurns;   // 用來顯示提示
    
    [HideInInspector] public bool IsMoving = false;
    [HideInInspector] public bool IsUseing = false;
    private bool isTriggered;

    [Header("UI Snap")]
    [SerializeField] private float snapSpeed = 18f;
    private CharacterCardSlotUI currentSlot;
    private bool isSnapping;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    void OnEnable()// 訂閱事件
    {
        TurnManager.OnTurnStart += StateCheck;
        TurnManager.OnRealTurnEnd += StateCheck;
        TurnManager.OnCancelCardChoose += CancelUseCard;
    }
    void Start()
    {
        cardTip = TurnManager.Instance.UseTips;
        checkButton = TurnManager.Instance.checkButton;
        originalParent = ownerPlayer.handArea;
        originalPosition = rectTransform.anchoredPosition;
        handLayout = ownerPlayer.handArea.GetComponent<HandLayoutController>();
    }
    void OnDisable()// 解除訂閱事件
    {
        TurnManager.OnTurnStart -= StateCheck;
        TurnManager.OnRealTurnEnd -= StateCheck;
        TurnManager.OnCancelCardChoose -= CancelUseCard;
    }

    private void StateCheck(Player player) => DisplayChange();
    private void CancelUseCard(CardCtrl cardCtrl)
    {
        DisplayChange();
    }

    public void Setup(Card data, Player owner)// 初始化 UI 
    {
        card_data = data; ownerPlayer = owner;
        if (picture != null) picture.sprite = card_data.cardPicture;
        if (cardTypePicture != null) cardTypePicture.sprite = card_data.cardTypePicture;
        if (card_name != null) card_name.text = card_data.cardName;

        if (card_data.cardType == Card.CARD_TYPE.WAIT)
        { 
            WaitCardManager.Instance.RegisterWaitCard(this); 
        } 
        LockedDisplay.SetActive(false);

        DisplayChange();
    }
    public void DisplayChange()
    {
        if (IsUseing)
        {
            cardUsingDisplay.SetActive(true);
            cardAvailableDisplay.SetActive(false);
        }
        else
        {
            cardAvailableDisplay.SetActive(TurnManager.Instance.actingPlayer != ownerPlayer);
            cardUsingDisplay.SetActive(false);
            if (isTriggered)
            {
                delayTurns.gameObject.SetActive(true);
                delayTurns.text = $"{WaitCardManager.Instance.GetDelayTurn(this)}";
            }
        }

    }

    #region Pointer

    public void OnClick() // Use Event trigger
    {
        if (!TooltipUI.Instance.CardTooltipPanel.activeInHierarchy)
            TooltipUI.Instance.ShowCardTooltip(card_data);
        else
            TooltipUI.Instance.HideTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!TooltipUI.Instance.IsDragging)
            TooltipUI.Instance.ShowCardTooltip(card_data);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance.HideTooltip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag()) return;
        Debug.Log($"{card_data.cardName}:Start Draging");

        originalPosition = rectTransform.anchoredPosition;
        handLayout.CreatePlaceholder(rectTransform);
        canvasGroup.blocksRaycasts = false;

        TooltipUI.Instance.IsDragging = true;
        TooltipUI.Instance.HideTooltip();

        rectTransform.SetParent(mainCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanStartDrag()) return;

        if (isSnapping && currentSlot != null)
        {
            rectTransform.position = Vector2.Lerp(
                rectTransform.position,
                currentSlot.snapPoint.position,
                Time.deltaTime * snapSpeed);
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.transform as RectTransform,
            mousePos, null, out var pos);

        rectTransform.localPosition = pos;
        handLayout?.UpdateDuringDrag(rectTransform);

        if (user != null)
            user.usingCard = null;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        TooltipUI.Instance.IsDragging = false;
        Debug.Log($"{card_data.cardName}:Over Draging");

        if (isSnapping && currentSlot != null)
        {
            TryUseOnCharacter(currentSlot.character);
        }
        else
        {
            ReturnToHand();
        }

        canvasGroup.blocksRaycasts = true;
        ClearSnap();
    }

    private bool CanStartDrag()
    {
        if (ownerPlayer.team == TeamID.Enemy) return false;
        if (isTriggered) return false;
        return true;
    }

    public void EnterSnap(CharacterCardSlotUI slot)
    {
        currentSlot = slot;
        isSnapping = true;
        rectTransform.SetParent(slot.character.transform, true);
        snapDisplay.SetActive(true);
    }

    public void ExitSnap(CharacterCardSlotUI slot)
    {
        if (currentSlot != slot) return;
        rectTransform.SetParent(mainCanvas.transform, true);
        snapDisplay.SetActive(false);
        
        ClearSnap();
    }

    private void ClearSnap()
    {
        currentSlot = null;
        isSnapping = false;
    }

    public bool CanSnapTo(CharacterHealth target)
    {
        if (target.usingCard != null) return false;
        if (target.team != ownerPlayer.team) return false;
        if (TurnManager.Instance.actingPlayer != ownerPlayer) return false;
        if (!TurnManager.Instance.waitingForAction) return false;
        if (target.usingCard != null) return false;

        return true;
    }

    #endregion

    #region Card Use

    public void TryUseOnCharacter(CharacterHealth target)
    {
        if (!CanUse(target))
        {
            Debug.Log($"{card_data.cardName}:Can not use now");
            ReturnToHand();
            return;
        }

        user = target;
        target.usingCard = this;

        handLayout.RemovePlaceholder();
        rectTransform.SetParent(target.character_Picture.GetComponentInParent<Transform>().transform, true);
        UseCard();
    }

    private void UseCard()
    {
        switch(card_data.cardType)
        {
            case Card.CARD_TYPE.NOW:
                ownerPlayer.PlayCard(user, this);
                break;
            case Card.CARD_TYPE.WAIT:
                WaitCardManager.Instance.RegisterWaitCard(this);
                break;
            case Card.CARD_TYPE.DELAY:
                TriggerCard();
                break;
        }
    }

    private bool CanUse(CharacterHealth target)
    {
        if (ownerPlayer.team == TeamID.Enemy) return false;
        if (target.team != ownerPlayer.team) return false;
        if (TurnManager.Instance.actingPlayer != ownerPlayer) return false;
        if (!TurnManager.Instance.waitingForAction) return false;
        if (target.usingCard != null) return false;

        return true;
    }

    private void TriggerCard()
    {
        if (card_data.cardType != Card.CARD_TYPE.DELAY) return;
        isTriggered = true;
        WaitCardManager.Instance.RegisterDelayCard(this);
        TurnManager.Instance.BeginTriggerCard(this, user, ownerPlayer);
    }

    #endregion

    #region Movement

    private void ReturnToHand()
    {
        if (handLayout != null)
        {
            Debug.Log($"{card_data.cardName}:Back to hand");
            StartCoroutine(handLayout.SmoothInsertToPlaceholder(rectTransform, 0.25f));
            WaitCardManager.Instance.UnregisterCard(this);
            canvasGroup.blocksRaycasts = true;
        }
    }

    #endregion


}
