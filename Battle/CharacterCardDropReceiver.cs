using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterCardSlotUI :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public CharacterHealth character;
    public RectTransform snapPoint;

    private CardCtrl hoveringCard;

    void Awake()
    {
        if (snapPoint == null)
            snapPoint = GetComponent<RectTransform>();
        if (character == null)
            character = GetComponentInParent<CharacterHealth>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!TooltipUI.Instance.IsDragging) return;

        var card = eventData.pointerDrag?.GetComponent<CardCtrl>();
        if (card == null) return;
        
        Debug.Log($"Try to snap:{character.character_data.characterName}");
        if (!card.CanSnapTo(character)) return;

        hoveringCard = card;
        card.EnterSnap(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoveringCard == null) return;
        if (!TooltipUI.Instance.IsDragging) return;
        character.usingCard = null;
        hoveringCard.ExitSnap(this);
        hoveringCard = null;
    }
}
