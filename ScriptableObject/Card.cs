using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WaitEvent
{
    public TriggerTime trigger;             // 觸發類型
    public CharacterHealth actor;           // 引發事件的角色
    // 本次事件涉及的 WAIT 卡
    public List<CardCtrl> relatedCards = new List<CardCtrl>();
    public bool resolved = false;           // 是否處理完成
}

[CreateAssetMenu(menuName = "BattleGameObjects/Card")]
public class Card : ScriptableObject
{
    // 卡牌類型
    public enum CARD_TYPE
    {
        NOW,
        DELAY,
        WAIT,
    }
    // 響應牌時機
    public enum WAIT_TRIGGER
    {
        NotWaitCard,
        
        OnSelfBeDamaged,       // 自己受傷
        OnOtherBeDamaged,      // 其他人受傷
        OnAnyBeDamaged,        // 任何人受傷

        OnSelfTurnStart,       // 自己回合開始
        OnOtherTurnStart,      // 其他人回合開始
        OnAnyTurnStart,        // 任何人回合開始
        
        OnSelfTurnEnd,         // 自己回合結束
        OnOtherTurnEnd,        // 其他人回合結束
        OnAnyTurnEnd,          // 任何人回合結束

        OnAnyCharacterDead,    // 任意角色死亡
    }

    [Header("卡牌屬性")]
    public CARD_TYPE cardType;         // 類型
    public string cardName;            // 卡名
    [TextArea]
    public string cardToolTip;         // 文本介紹
    public Sprite cardPicture;         // 卡圖
    public Sprite cardTypePicture;     // 類型圖
    public Character holderCharacter;
    [Header("卡片需求")]
    public List<NeedState> CardNeeds;
    [Header("WAIT卡觸發")]
    public Trigger_Character waitTrigger;
    public TriggerTime waitTriggerTime;    // 觸發事件
    [Header("Delay卡觸發")]
    public Trigger_Character delayTrigger;
    public TriggerTime delayTriggerTime;
    public int delayTime;
    public List<EffectEntry> initiativeTiggerEffect;
    [Header("卡片效果")]
    public List<EffectEntry> effectEntrys; // 效果組合
}