using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;

public class ContinuedEffect_display : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public EffectInstance effectDataInstance;
    [HideInInspector] public CharacterHealth selfHealth;
    [HideInInspector] public ContinuedEffectCtrl effectCtrl;
    public Image effectPicture;
    public TextMeshProUGUI trun_text;
    public TextMeshProUGUI TriggerTimes_text;
    public TextMeshProUGUI stack_text;

    void Start()
    {
        effectCtrl = selfHealth?.GetComponent<ContinuedEffectCtrl>();
        UpdateDisplay(effectDataInstance, selfHealth);
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

    public void UpdateDisplay(EffectInstance effect, CharacterHealth trigger)// 更新顯示邏輯
    {
        if (effectDataInstance == null || effectCtrl == null || selfHealth == null) return;
        // 僅更新自己對應的效果
        if (effect != effectDataInstance || trigger != selfHealth) return;

        // 更新回合數
        if (effectDataInstance.effectData.endable)
        {
            if (effectCtrl.activeEffects.Any(EI => EI == effectDataInstance))
                trun_text.text = $"{effectCtrl.activeEffects.Find(EI => EI == effectDataInstance).duration}";
            else
                trun_text.text = "";
        }
        else
            trun_text.text = "";

        // 更新觸發次數
        if (effectCtrl.activeEffects.Any(EI => EI == effectDataInstance))
        {
            int triggerTime = effectCtrl.activeEffects.Find(EI => EI == effectDataInstance).triggerCount;
            if (effectDataInstance.effectData.LimitedTimes)
                TriggerTimes_text.text = $"{triggerTime}/{effectDataInstance.effectData.TriggerTimes}";
            else
                TriggerTimes_text.text = $"{triggerTime}";
        }
        else
        {
            TriggerTimes_text.text = "";
        }

        // 更新圖片
        if (effectPicture != null)
            effectPicture.sprite = effectDataInstance.effectData.EffectImage;

        // 更新堆疊
        if (stack_text != null)
        {
            int stack = 0;
            if (effectCtrl.activeEffects.Any(EI => EI == effectDataInstance))
            {
                stack = effectCtrl.activeEffects.Find(EI => EI == effectDataInstance).stack;
            }
            stack_text.text = effectDataInstance.effectData.MaxOverlay > 1 ? stack.ToString() : "";
        }
    }
    
    // 懸停事件
    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipUI.Instance.ShowEffectTooltip(effectDataInstance, effectCtrl);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance.HideTooltip();
    }
}
