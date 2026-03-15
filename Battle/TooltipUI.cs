using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [Header("Card")]
    public GameObject CardTooltipPanel;
    public TMP_Text cardNameText;
    public TMP_Text cardTooltipText;
    [HideInInspector] public bool IsDragging = false;
    private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;

    [Header("ContinuedEffect")]
    public GameObject EffectTooltipPanel;
    public TMP_Text EffectNameText;
    public TMP_Text DurationText;
    public TMP_Text TriggerTimesText;
    public TMP_Text EffectTooltipText;
    public TMP_Text EffectOverlayText;

    [Header("Character")]
    public GameObject CharacterTooltipPanel;
    public TMP_Text CharacterNameText;
    public TMP_Text CharacterValueText;

    public GameObject characterInformatuon;
    public GameObject skillPrefab;
    public GameObject passiveSkillPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
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
        {
            // FollowMouseAndClamp(EffectTooltipPanel);
        }
        else if (CharacterTooltipPanel.activeSelf)
        {
            // FollowMouseAndClamp(CharacterTooltipPanel);
        }
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

        if (hoverSound != null)
            audioSource.PlayOneShot(hoverSound);
    }

    public void ShowEffectTooltip(EffectInstance Effect, ContinuedEffectCtrl EffectCtrl)
    {
        if (Effect == null) return;

        // 如果之前在等待隱藏，先取消
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (Effect.effectData.Removable)
            EffectNameText.text = $"{Effect.effectData.EffectName}";
        else EffectNameText.text = $"{Effect.effectData.EffectName}(無法移除)";

        // 取得當前的持續回合
        int currentDuration = Effect.duration;
        
        // 取得當前已觸發次數
        int currentTriggers = Effect.triggerCount;
        int remainingTriggers = Mathf.Max(Effect.triggerCount - currentTriggers, 0);

        // 取得同名疊加次數
        int currentOverlay = Effect.stack;

        // 顯示資訊
        if (Effect.effectData.endable)
            DurationText.text = $"剩餘回合數:{currentDuration}";
        else DurationText.text = "";

        if (Effect.stack > 1)
            EffectOverlayText.text = $"疊加層數:{currentOverlay}/{Effect.effectData.MaxOverlay}";
        else EffectOverlayText.text = "";

        TriggerTimesText.text = $"剩餘觸發次數:{remainingTriggers}";
        EffectTooltipText.text = Effect.effectData.Introduse;

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
        // characterInformatuon.SetActive(false);
    }

    public void ShowCharacterInformation(CharacterHealth character)// 角色簡介
    {
        characterInformatuon.transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = 
            character.character_data.characterName;
        switch(character.team)
        {
            case TeamID.Team1:
                characterInformatuon.transform.GetChild(0).GetChild(2).GetComponent<TMP_Text>().text = "P1";
                break;
            case TeamID.Team2:
                characterInformatuon.transform.GetChild(0).GetChild(2).GetComponent<TMP_Text>().text = "P2";
                break;
            case TeamID.Enemy:
                characterInformatuon.transform.GetChild(0).GetChild(2).GetComponent<TMP_Text>().text = "Enemy";
                break;
        }
        characterInformatuon.transform.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = 
            $"LV{character.level}";
        characterInformatuon.transform.GetChild(1).GetChild(0).GetComponent<Image>().sprite = 
            character.character_data.characterPicture;
        // HP
        characterInformatuon.transform.GetChild(2).GetChild(0).GetChild(0).GetComponent<Image>().fillAmount = 
            Mathf.Clamp01((float)character.currentHealth / character.currentMaxHP);
        characterInformatuon.transform.GetChild(2).GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = 
            $"{character.currentHealth}/{character.currentMaxHP}";
        // Amount
        characterInformatuon.transform.GetChild(2).GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = 
            $"{character.currentAttackPower}";
        characterInformatuon.transform.GetChild(2).GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = 
            $"{character.currentDefense}";
        characterInformatuon.transform.GetChild(2).GetChild(3).GetChild(0).GetComponent<TMP_Text>().text = 
            $"{character.currentDamageMultiplier * 100}%";
        characterInformatuon.transform.GetChild(2).GetChild(4).GetChild(0).GetComponent<TMP_Text>().text = 
            $"{character.currentDamageReduction}";
        // skill
        foreach(Transform ob in characterInformatuon.transform.GetChild(3).GetChild(0))
            Destroy(ob.gameObject);
        foreach(Transform ob in characterInformatuon.transform.GetChild(4).GetChild(0))
            Destroy(ob.gameObject);
        
        foreach(var skill in character.currentSkills)
        {
            GameObject PS_B = Instantiate(skillPrefab, characterInformatuon.transform.GetChild(3).GetChild(0));
            PS_B.GetComponentInChildren<SkillCtrl>().Skill_data = skill;
            PS_B.GetComponentInChildren<SkillCtrl>().self = character;
            // PS_B.GetComponentInChildren<TMP_Text>().text = 
            //     $"{skill.skillName}({TurnManager.Instance.skillUseCounter[(character, skill)]})";
            PS_B.SetActive(true);
        }
        foreach(var passiveSkill in character.currentPassiveSkills)
        {
            GameObject PS_B = Instantiate(passiveSkillPrefab, characterInformatuon.transform.GetChild(4).GetChild(0));
            PS_B.GetComponentInChildren<PassiveSkill_display>().Skill_data = passiveSkill;
            PS_B.GetComponentInChildren<PassiveSkill_display>().selfHealth = character;

            PS_B.SetActive(true);
        }

        characterInformatuon.SetActive(true);
    }
}
