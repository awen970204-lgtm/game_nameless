using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;

public class ContinuedEffect_display : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public ContinuedEffect effectData;
    [HideInInspector] public CharacterHealth selfHealth;
    [HideInInspector] public ContinuedEffectCtrl effectCtrl;
    public Image effectPicture;
    public TextMeshProUGUI trun_text;
    public TextMeshProUGUI TriggerTimes_text;
    public TextMeshProUGUI stack_text;

    void Start()
    {
        effectCtrl = selfHealth?.GetComponent<ContinuedEffectCtrl>();
        UpdateDisplay(effectData, selfHealth);
    }
    void OnEnable()
    {
        ContinuedEffectCtrl.OnEffectTriggered += UpdateDisplay;
        ContinuedEffectCtrl.OnEffectExpired += UpdateDisplay;
        ContinuedEffectCtrl.OnEffectRemake += UpdateDisplay;
        ContinuedEffectCtrl.OnEffectGot += UpdateDisplay;
    }
    void OnDisable()
    {
        ContinuedEffectCtrl.OnEffectTriggered -= UpdateDisplay;
        ContinuedEffectCtrl.OnEffectExpired -= UpdateDisplay;
        ContinuedEffectCtrl.OnEffectRemake -= UpdateDisplay;
        ContinuedEffectCtrl.OnEffectGot -= UpdateDisplay;
    }

    public void UpdateDisplay(ContinuedEffect effect, CharacterHealth trigger)// 更新顯示邏輯
    {
        if (effectData == null || effectCtrl == null || selfHealth == null) return;
        // 僅更新自己對應的效果
        if (effect != effectData || trigger != selfHealth) return;

        // 更新回合數
        if (effectData.endable)
        {
            if (effectCtrl.effectDurations.TryGetValue(effectData, out int remainingTurns))
                trun_text.text = $"{remainingTurns}";
            else
                trun_text.text = "-";
        }
        else
            trun_text.text = "";

        // 更新觸發次數
        if (effectCtrl.effectTriggerCounts.TryGetValue(effectData, out int currentTrigger))
        {
            if (effectData.LimitedTimes)
                TriggerTimes_text.text = $"{currentTrigger}/{effectData.TriggerTimes}";
            else
                TriggerTimes_text.text = $"{currentTrigger}";
        }
        else
        {
            TriggerTimes_text.text = "";
        }

        // 更新圖片
        if (effectPicture != null)
            effectPicture.sprite = effectData.EffectImage;

        // 更新堆疊
        if (stack_text != null)
        {
            int stack = 0;
            foreach(var continuedEffect in trigger.effectCtrl.activeEffects)
            {
                if (continuedEffect.EffectName == effectData.EffectName)
                    stack += continuedEffect.stack;
            }
            stack_text.text = stack > 1 ? stack.ToString() : "";
        }
    }
    
    // 懸停事件
    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipUI.Instance.ShowEffectTooltip(effectData, effectCtrl);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance.HideTooltip();
    }
}
