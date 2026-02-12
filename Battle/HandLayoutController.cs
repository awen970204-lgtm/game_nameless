using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(HorizontalLayoutGroup))]
public class HandLayoutController : MonoBehaviour
{
    private RectTransform rectTransform;

    private GameObject placeholder;
    private RectTransform placeholderRect;
    private int placeholderIndex = -1;

    private Vector2 dragStartMousePos;
    private bool isVerticalDrag;

    [Header("Drag Detect")]
    public float verticalDragThreshold = 30f;

    [Header("Scroll")]
    public ScrollRect scrollRect;
    public float scrollEdgeThreshold = 80f;
    public float scrollSpeed = 2000f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        scrollRect = GetComponentInParent<ScrollRect>();
    }

    #region Placeholder

    public void CreatePlaceholder(RectTransform draggedCard)
    {
        if (placeholder != null) return;

        placeholder = new GameObject("CardPlaceholder");
        placeholderRect = placeholder.AddComponent<RectTransform>();
        placeholderRect.SetParent(transform);

        placeholderRect.sizeDelta = draggedCard.sizeDelta;

        LayoutElement layout = placeholder.AddComponent<LayoutElement>();
        layout.preferredWidth = draggedCard.rect.width;
        layout.preferredHeight = draggedCard.rect.height;

        Image img = placeholder.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.15f);

        placeholderIndex = draggedCard.GetSiblingIndex();
        placeholderRect.SetSiblingIndex(placeholderIndex);

        dragStartMousePos = Mouse.current.position.ReadValue();
        isVerticalDrag = false;
    }

    #endregion

    #region Drag Update

    public void UpdateDuringDrag(RectTransform draggedCard)
    {
        if (placeholder == null) return;

        Vector2 currentMouse = Mouse.current.position.ReadValue();
        Vector2 delta = currentMouse - dragStartMousePos;

        if (!isVerticalDrag &&
            Mathf.Abs(delta.y) > verticalDragThreshold &&
            Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
        {
            isVerticalDrag = true;
        }

        if (isVerticalDrag)
            return;

        UpdatePlaceholderPosition(draggedCard);
        TryAutoScroll();
    }

    private void UpdatePlaceholderPosition(RectTransform draggedCard) // 找佔位物的目標位置
    {
        if (placeholder == null) return;

        float draggedX = draggedCard.position.x;
        // 默認最後
        int newIndex = transform.childCount - 1;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (child == placeholder.transform) continue;

            if (draggedX < child.position.x)
            {
                newIndex = i;
                
                if (placeholderRect.GetSiblingIndex() < newIndex)
                {
                    newIndex--;
                }
                break;
            }
        }

        if (newIndex != placeholderIndex)
        {
            placeholderIndex = newIndex;
            placeholderRect.SetSiblingIndex(placeholderIndex);
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
    #endregion

    #region Scroll

    private void TryAutoScroll()
    {
        if (scrollRect == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        RectTransform viewport = scrollRect.viewport;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, mousePos, null, out var localMousePos);

        if (localMousePos.x > viewport.rect.width / 2 - scrollEdgeThreshold)
            scrollRect.horizontalNormalizedPosition += Time.deltaTime * (scrollSpeed / 1000f);
        else if (localMousePos.x < -viewport.rect.width / 2 + scrollEdgeThreshold)
            scrollRect.horizontalNormalizedPosition -= Time.deltaTime * (scrollSpeed / 1000f);

        scrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(scrollRect.horizontalNormalizedPosition);
    }

    #endregion

    #region Insert / Return

    public IEnumerator SmoothInsertToPlaceholder(RectTransform draggedCard, float duration = 0.25f)
    {
        if (draggedCard == null || placeholderRect == null)
            yield break;

        CardCtrl ctrl = draggedCard.GetComponent<CardCtrl>();
        if (ctrl == null)
            yield break;

        ctrl.IsMoving = true;

        Vector2 start = draggedCard.position;
        Vector2 target = placeholderRect.position;
        int index = placeholderRect.GetSiblingIndex();
        float t = 0f;

        while (t < duration)
        {
            if (draggedCard == null)
            {
                ctrl.IsMoving = false;
                yield break;
            }

            draggedCard.position = Vector3.Lerp(start, target, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        
        draggedCard.position = target;
        draggedCard.SetParent(transform, false);
        draggedCard.SetSiblingIndex(index);

        RemovePlaceholder();

        ctrl.IsMoving = false;
        ctrl.IsUseing = false;
        ctrl.snapDisplay.SetActive(false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        draggedCard.SetParent(transform, true);
    }

    public void RemovePlaceholder()
    {
        if (placeholder != null)
        {
            Destroy(placeholder);
            placeholder = null;
            placeholderRect = null;
            placeholderIndex = -1;
        }
    }
    public bool HasPlaceholder => placeholder != null;

    #endregion
}
