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
    public GameObject cardDiscardDisplay;   // 棄置狀態顯示
    public GameObject snapDisplay;          // 吸附狀態顯示

    [Header("Card")]
    public Image picture;         // 卡圖
    public Image cardTypePicture; // 類型圖
    public TMP_Text card_name;    // 卡名
    public TMP_Text delayTurns;   // 持續時間
    private Image background;
    
    [HideInInspector] public bool IsMoving = false;
    [HideInInspector] public bool IsUseing = false;
    private bool isTriggered;

    [Header("UI Snap")]
    // [SerializeField] private float snapSpeed = 18f;
    private CharacterCardSlotUI currentSlot;
    private bool isSnapping;
    [Header("Hover Effect")]
    [SerializeField] float hoverScale = 1.2f;
    [SerializeField] float hoverHeight = 35f;
    [SerializeField] float hoverSpeed = 12f;

    private bool isHovering;

    private Vector3 baseScale;
    // private Vector2 basePos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCanvas = GetComponentInParent<Canvas>();
        background = GetComponentInParent<Image>();

        baseScale = rectTransform.localScale;
    }
    void OnEnable()// 訂閱事件
    {
        TurnManager.OnTurnStart += StateCheck;
        TurnManager.OnRealTurnEnd += StateCheck;
    }
    void Start()
    {
        checkButton = TurnManager.Instance.checkButton;
        originalParent = ownerPlayer.handArea;
        originalPosition = rectTransform.anchoredPosition;
        handLayout = ownerPlayer.handArea.GetComponent<HandLayoutController>();
        if (ownerPlayer.team == TeamID.Enemy || ownerPlayer.team == TeamID.Team2)
        {
            hoverHeight *= -1;
        }
    }
    void OnDisable()// 解除訂閱事件
    {
        TurnManager.OnTurnStart -= StateCheck;
        TurnManager.OnRealTurnEnd -= StateCheck;
    }
    void Update()
    {
        if (IsMoving || TooltipUI.Instance.IsDragging)
            return;
        if (handLayout != null && handLayout.HasPlaceholder)
            return;

        Vector3 targetScale = baseScale;
        float targetY = 0f;

        if (isHovering)
        {
            targetScale = baseScale * hoverScale;
            targetY = hoverHeight;
        }

        rectTransform.localScale =
            Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * hoverSpeed);

        Vector3 pos = rectTransform.localPosition;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * hoverSpeed);
        rectTransform.localPosition = pos;
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

        DisplayChange();
    }
    public void DisplayChange()
    {
        if (user != null)
        {
            cardAvailableDisplay.SetActive(false);
        }
        else
        {
            cardAvailableDisplay.SetActive(TurnManager.Instance.actingPlayer == null || 
                TurnManager.Instance.actingPlayer != ownerPlayer);
            if (isTriggered)
            {
                delayTurns.gameObject.SetActive(true);
                delayTurns.text = $"{WaitCardManager.Instance.GetDelayTurn(this)}";
            }
            
            cardDiscardDisplay.SetActive(ownerPlayer.disCard.Contains(this) || ownerPlayer.stealCardBuffer.Contains(this));
        }
    }

    #region Pointer

    public void OnClick() // Use Event trigger
    {
        if (IsUseing) return;
        if (ownerPlayer.IsDising)
        {
            if (ownerPlayer.stealCardBuffer.Contains(this))
                return;
            if (ownerPlayer.disCard.Contains(this))
                ownerPlayer.disCard.Add(this);
            else
                ownerPlayer.disCard.Remove(this);
            DisplayChange();
        }
        if (ownerPlayer.IsStealing)
        {
            if (ownerPlayer.disCard.Contains(this))
                return;
            if (ownerPlayer.stealCardBuffer.Contains(this))
                ownerPlayer.stealCardBuffer.Add(this);
            else
                ownerPlayer.stealCardBuffer.Remove(this);
            DisplayChange();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!TooltipUI.Instance.IsDragging)
        {
            TooltipUI.Instance.ShowCardTooltip(card_data);
            isHovering = true;

            // transform.SetAsLastSibling();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance.HideTooltip();
        isHovering = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag()) return;
        Debug.Log($"{card_data.cardName}:Start Draging");

        originalPosition = rectTransform.anchoredPosition;
        handLayout.CreatePlaceholder(rectTransform);

        TooltipUI.Instance.IsDragging = true;
        TooltipUI.Instance.HideTooltip();
        isHovering = false;
        background.raycastTarget = false;

        rectTransform.SetParent(mainCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanStartDrag()) return;

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
        background.raycastTarget = true;

        if (isSnapping && currentSlot != null)
        {
            TryUseOnCharacter(currentSlot.character);
        }
        else
        {
            ReturnToHand();
        }

        isHovering = false;
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
        // rectTransform.SetParent(slot.character.transform, true);
        snapDisplay.SetActive(true);
    }

    public void ExitSnap(CharacterCardSlotUI slot)
    {
        if (currentSlot != slot) return;
        isSnapping = false;
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
        snapDisplay.SetActive(true);

        handLayout.RemovePlaceholder();
        StartCoroutine(SnapToCharacter(target));
        
    }
    private IEnumerator SnapToCharacter(CharacterHealth target)
    {
        rectTransform.SetParent(mainCanvas.transform, true);
        Vector2 start = this.transform.position;
        Vector2 end = target.character_Picture.GetComponentInParent<Transform>().transform.position;
        int index = target.character_Picture.GetComponentInParent<Transform>().transform.GetSiblingIndex();
        float t = 0f;

        IsMoving = true;
        while (t < 0.25f)
        {
            if (target == null)
            {
                IsMoving = false;
                yield break;
            }

            this.transform.position = Vector3.Lerp(start, end, t / 0.25f);
            t += Time.deltaTime;
            yield return null;
        }
        IsMoving = false;

        this.transform.position = target.character_Picture.GetComponentInParent<Transform>().transform.position;
        this.transform.SetSiblingIndex(index);
        rectTransform.SetParent(target.character_Picture.GetComponentInParent<Transform>().transform, true);

        if (user != null)
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
            isHovering = false;
            rectTransform.localScale = baseScale;
        }
    }

    #endregion


}
