using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [Header("Card")]
    public GameObject CardTooltipPanel;
    public TMP_Text cardNameText;
    public TMP_Text cardTooltipText;
    [Header("ContinuedEffect")]
    public GameObject EffectTooltipPanel;
    public TMP_Text EffectNameText;
    public TMP_Text DurationText;
    public TMP_Text TriggerTimesText;
    public TMP_Text EffectTooltipText;
    public TMP_Text EffectOverlayText;
    [Header("ContinuedEffect")]
    public GameObject CharacterTooltipPanel;
    public TMP_Text CharacterNameText;
    public TMP_Text CharacterValueText;

    [HideInInspector] public bool IsDragging = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        CardTooltipPanel.SetActive(false);
        EffectTooltipPanel.SetActive(false);
        CharacterTooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (CardTooltipPanel.activeSelf)
        {
            // FollowMouseAndClamp(CardTooltipPanel);
        }
        else if (EffectTooltipPanel.activeSelf)
            FollowMouseAndClamp(EffectTooltipPanel);

        else if (CharacterTooltipPanel.activeSelf)
            FollowMouseAndClamp(CharacterTooltipPanel);
    }
    private void FollowMouseAndClamp(GameObject panel)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        RectTransform parentRect = panel.transform.parent as RectTransform;
        RectTransform panelRect = panel.transform as RectTransform;

        // 把滑鼠轉到 Canvas 座標
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, mousePos, null, out Vector2 anchoredPos);


        // 計算 panel 在這個位置的邊界
        Vector2 panelSize = panelRect.sizeDelta;
        float halfWidth = panelSize.x * 0.5f;
        float halfHeight = panelSize.y * 0.5f;
        // 預設偏移
        anchoredPos += new Vector2(halfWidth + 20f, 0);

        // Canvas 的範圍（左下 -960,-540 / 右上 960,540）
        float leftBound   = -parentRect.rect.width * 0.5f;
        float rightBound  =  parentRect.rect.width * 0.5f;
        float bottomBound = -parentRect.rect.height * 0.5f;
        float topBound    =  parentRect.rect.height * 0.5f;

        // 判斷是否超出右側 → 反向顯示
        if (anchoredPos.x + halfWidth > rightBound)
            anchoredPos -= new Vector2(halfWidth*2 + 40f, 0);

        // 判斷左側
        if (anchoredPos.x - halfWidth < leftBound)
            anchoredPos.x = leftBound + halfWidth;

        // 上
        if (anchoredPos.y + halfHeight > topBound)
            anchoredPos.y = topBound - halfHeight;

        // 下
        if (anchoredPos.y - halfHeight < bottomBound)
            anchoredPos.y = bottomBound + halfHeight;

        // 最終套用位置
        panelRect.anchoredPosition = anchoredPos;
    }

    private Coroutine hideCoroutine;

    public void ShowCardTooltip(Card card_data)
    {
        if (card_data == null) return;

        // 如果之前在等待隱藏，先取消
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        switch (card_data.cardType)
        {
            case Card.CARD_TYPE.NOW:
                cardNameText.text = $"{card_data.cardName}(Now)";
                break;
            case Card.CARD_TYPE.DELAY:
                cardNameText.text = $"{card_data.cardName}(Delay)";
                break;
            case Card.CARD_TYPE.WAIT:
                cardNameText.text = $"{card_data.cardName}(Wait)";
                break;
        }
        cardTooltipText.text = card_data.cardToolTip;
        CardTooltipPanel.SetActive(true);
        EffectTooltipPanel.SetActive(false);
        CharacterTooltipPanel.SetActive(false);
    }

    public void ShowEffectTooltip(ContinuedEffect Effect, ContinuedEffectCtrl EffectCtrl)
    {
        if (Effect == null) return;

        // 如果之前在等待隱藏，先取消
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (Effect.Removable)
            EffectNameText.text = $"{Effect.EffectName}";
        else EffectNameText.text = $"{Effect.EffectName}(無法移除)";

        // 取得當前的持續回合
        int currentDuration = 0;
        if (EffectCtrl.effectDurations.TryGetValue(Effect, out int dur))
            currentDuration = dur;

        // 取得當前已觸發次數
        int currentTriggers = 0;
        if (EffectCtrl.effectTriggerCounts.TryGetValue(Effect, out int trig))
            currentTriggers = trig;
        int remainingTriggers = Mathf.Max(Effect.TriggerTimes - currentTriggers, 0);

        // 取得同名疊加次數
        int currentOverlay = 0;
        foreach (ContinuedEffect effect in EffectCtrl.activeEffects)
        {
            if (effect.EffectName == Effect.EffectName) currentOverlay++;
        }

        // 顯示資訊
        if (Effect.endable)
            DurationText.text = $"剩餘回合數:{currentDuration}";
        else DurationText.text = "";

        if (Effect.MaxOverlay > 1)
            EffectOverlayText.text = $"疊加層數:{currentOverlay}/{Effect.MaxOverlay}";
        else EffectOverlayText.text = "";

        TriggerTimesText.text = $"剩餘觸發次數:{remainingTriggers}";
        EffectTooltipText.text = Effect.Introduse;

        CardTooltipPanel.SetActive(false);
        EffectTooltipPanel.SetActive(true);
        CharacterTooltipPanel.SetActive(false);
    }

    public void ShowCharacterTooltip(CharacterHealth character)
    {
        if (character == null) return;

        // 如果之前在等待隱藏，先取消
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        CharacterNameText.text = $"{character.character_data.characterName}";
        CharacterValueText.text = 
        $"攻擊:{character.currentAttackPower}\n"+
        $"回復力:{character.currentHealPower}\n"+
        $"防禦:{character.currentDefense}\n"+
        $"傷害倍率:{character.currentDamageMultiplier*100}%\n"+
        $"受傷減免:{character.currentDamageReduction}";

        CardTooltipPanel.SetActive(false);
        EffectTooltipPanel.SetActive(false);
        CharacterTooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        hideCoroutine = StartCoroutine(HideAfterDelay(0.1f));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CardTooltipPanel.SetActive(false);
        EffectTooltipPanel.SetActive(false);
        CharacterTooltipPanel.SetActive(false);
    }
}
