using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor.SceneManagement;

public enum TeamID { Team1, Team2, Enemy }
public enum ActionType
{
    None,
    Card,
    Skill,
    PassiveSkill,
    ContinuedEffect
}
// 掛在玩家上
public class CharacterHealth : MonoBehaviour
{
    [HideInInspector] public Player ownerPlayer;
    [HideInInspector] public CardCtrl usingCard;

    [Header("CharacterSystems")]
    public Character character_data;
    public TeamID team;
    public ContinuedEffectCtrl effectCtrl;
    public PassiveSkilCtrl passiveSkillCtrl;

    [Header("UI")]
    public Image highlightRenderer;
    public Image character_Picture;
    public TMP_Text healthText;
    public TMP_Text AttackPowerText;
    public TMP_Text DefenseText;
    public Transform stateArea;
    public Transform TipsArea;
    [Header("Long Press Settings")]
    public float longPressTime = 0.8f;

    private bool isPressing;
    private float pressTimer;
    private bool longPressTriggered;

    // 角色數值
    [HideInInspector] public int level = 1;
    [HideInInspector] public int currentMaxHP;
    [HideInInspector] public int currentHealth;
    [HideInInspector] public int currentAttackPower;
    [HideInInspector] public int currentHealPower;
    [HideInInspector] public int currentDefense;
    [HideInInspector] public float currentDamageMultiplier;
    [HideInInspector] public float currentDamageReduction;
    [HideInInspector] public List<Skill> currentSkills = new List<Skill>();
    [HideInInspector] public List<PassiveSkill> currentPassiveSkills = new List<PassiveSkill>();
    [HideInInspector] public int DrawCardAtTurnStart = 1;

    // 判定用數值
    [HideInInspector] public List<Skill> invalidSkills = new List<Skill>();
    [HideInInspector] public List<PassiveSkill> invalidPassiveSkills = new List<PassiveSkill>();
    [HideInInspector] public bool IsAlive = true;
    [HideInInspector] public ActionType LastAttackType;
    [HideInInspector] public int lastAttackDamage = 0;
    [HideInInspector] public ActionType LastDamageType;
    [HideInInspector] public int lastBeAttackedDamage = 0;
    [HideInInspector] public ActionType LastHealType;
    [HideInInspector] public int lastHealAmount = 0;
    [HideInInspector] public CharacterHealth killer;
    [HideInInspector] public int EnterValue = 0;
    // 單回合記數
    [HideInInspector] public int AttackValueInTrun = 0;
    [HideInInspector] public int DamageValueInTrun = 0;
    [HideInInspector] public int HealValueInTrun = 0;
    [HideInInspector] public int UseSkillTimesInTrun = 0;
    // 事件宣告
    public static event System.Action<CharacterHealth> Open_SkillBar;
    public static event System.Action<CharacterHealth,Skill> OnGetSkill;
    public static event System.Action<CharacterHealth,Skill> OnLoseSkill;
    public static event System.Action<CharacterHealth,PassiveSkill> OnGetPassiveSkill;
    public static event System.Action<CharacterHealth,PassiveSkill> OnLosePassiveSkill;

    void Awake()
    {
        effectCtrl = GetComponent<ContinuedEffectCtrl>();
        passiveSkillCtrl = GetComponent<PassiveSkilCtrl>();
    }
    void OnEnable()
    {
        TurnManager.OnRealTurnEnd += RemakeAmount;
        TurnManager.OnRealTurnEnd += CheckAlive;
    } 
    void Start()
    {
        if (character_data == null)
        {
            Debug.LogError("no Character data!");
            return;
        }
        TurnManager.Instance?.Register(this);
        // 綁定角色數值
        character_Picture.sprite = character_data.characterPicture;
        character_Picture.enabled = true;

        currentMaxHP = character_data.characterMaxHP;
        currentHealth = character_data.characterStartHP;
        currentAttackPower = character_data.attackPower;
        currentHealPower = character_data.healPower;
        currentDefense = character_data.defense;
        currentDamageMultiplier = character_data.damageMultiplier;
        currentDamageReduction = character_data.damageReduction;
        DrawCardAtTurnStart = character_data.drawCount;

        AttackPowerText.text = $"{currentAttackPower}";
        DefenseText.text = $"{currentDefense}";
        team = ownerPlayer.team;
        
        CharacterSelectionManager.Instance.SetCharacterLevel(this, level);
        UpdateHealthUI();
    }
    void OnDisable()
    {
        TurnManager.OnRealTurnEnd -= RemakeAmount;
        TurnManager.OnRealTurnEnd -= CheckAlive;
    }
    
    void Update()
    {
        if (!isPressing || longPressTriggered) return;

        pressTimer += Time.unscaledDeltaTime;

        if (pressTimer >= longPressTime)
        {
            longPressTriggered = true;
            Open_SkillBar.Invoke(this);
        }
    }

    #region OnPointer
    // Use Event Trigger
    public void OnPointerClick()
    {
        if (TurnManager.Instance.waitingForTarget && TurnManager.Instance.pendingUser?.team != TeamID.Enemy)
        {
            Debug.Log("選擇角色");
            SkillClick();
        }
        else if (!TooltipUI.Instance.CharacterTooltipPanel.activeInHierarchy)
            TooltipUI.Instance.ShowCharacterTooltip(this);
        else 
            TooltipUI.Instance.HideTooltip();
    }
    public void OnPointerEnter()
    {
        if (!TooltipUI.Instance.IsDragging)
            TooltipUI.Instance.ShowCharacterTooltip(this);
    }
    public void OnPointerDown()
    {
        isPressing = true;
        pressTimer = 0f;
        longPressTriggered = false;
    }
    public void OnPointerUp()
    {
        isPressing = false;
        pressTimer = 0f;
    }
    public void OnPointerExit()
    {
        isPressing = false;
        pressTimer = 0f;
        TooltipUI.Instance.HideTooltip();
    }
    #endregion

    private void RemakeAmount(Player player) // 刷新數值
    {
        AttackValueInTrun = 0;
        ownerPlayer.DrawCardsInTrun = 0;
        HealValueInTrun = 0;
        UseSkillTimesInTrun = 0;
    }
    private void CheckAlive(Player player) // 確認存活
    {
        if (currentHealth <= 0 && IsAlive)
        {
            IsAlive = false;
            if (usingCard != null)
            {
                var card = usingCard.card_data;

                ownerPlayer.hand.Remove(card);
                ownerPlayer.handUI.Remove(usingCard);
                
                WaitCardManager.Instance.UnregisterCard(usingCard);
                
                Destroy(usingCard);
            }
            foreach(var skill in new List<Skill>(currentSkills))
            {
                LoseSkill(skill);
            }
            foreach(var passiveSkill in new List<PassiveSkill>(currentPassiveSkills))
            {
                LosePassiveSkill(passiveSkill);
            }

            TurnManager.Instance.RaiseAnyCharacterDead(this);
            TurnManager.Instance?.Unregister(this);
            Debug.Log($"{character_data.characterName} 死亡");
        }
    }

    #region Character Acting
    public IEnumerator ReadyToAttact(int damage, CharacterHealth injured, SpecialEffects special) // 受傷前
    {
        int nowPassives = TurnManager.Instance.pendingPassives;
        int nowEffectEntrys = TurnManager.Instance.pendingEffectEntrys;
        // 插入防止傷害
        yield return new WaitUntil(() => TurnManager.Instance == null ||
         TurnManager.Instance.pendingPassives == nowPassives && TurnManager.Instance.pendingEffectEntrys == nowEffectEntrys);
        yield return injured.TakeDamage(damage, this, special);
    }
    public IEnumerator TakeDamage(int damage, CharacterHealth attacker, SpecialEffects special)// 受到傷害
    {
        attacker.lastAttackDamage = damage;
        attacker.AttackValueInTrun += damage;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, -currentMaxHP, currentMaxHP);
        lastBeAttackedDamage = damage;
        DamageValueInTrun += damage;
        
        // 顯示傷害
        Debug.Log($"{attacker.character_data.characterName} 對 {character_data.characterName} 造成 {damage} 點傷害");
        ownerPlayer.ShowFloatingText($"-{damage}", Color.red, this);
        UpdateHealthUI();
        TurnManager.Instance.RaiseAnyAttackEvent(attacker, this);

        // 是否擊殺
        if (currentHealth <= 0) killer = attacker;

        SpecialDisplay(special);
        yield return new WaitForSeconds(0.1f);
    }
    public void Heal(int amount, SpecialEffects special)// 回血
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, -currentMaxHP, currentMaxHP);
        lastHealAmount = amount;
        HealValueInTrun += amount;
        // 顯示回血
        ownerPlayer.ShowFloatingText($"+{amount}", Color.green, this);
        UpdateHealthUI();
        SpecialDisplay(special);
        TurnManager.Instance.RaiseAnyBeHealed(this);
        // 是否擊殺
        if (currentHealth > 0) killer = null;
    }
    public void ChangeMaxHP(int amount, SpecialEffects special)// 變換血量最大值
    {
        int nowCurrentHealth = currentHealth;
        currentMaxHP += amount;
        currentHealth = Mathf.Clamp(currentHealth, -currentMaxHP, currentMaxHP);
        // 顯示變化
        int change = nowCurrentHealth - currentHealth;
        if (nowCurrentHealth > currentHealth) ownerPlayer.ShowFloatingText($"-{change}", Color.purple, this);
        UpdateHealthUI();

        SpecialDisplay(special);
    }
    public void ConsumeHP(int amount, SpecialEffects special)// 消耗血量
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, -currentMaxHP, currentMaxHP);
        // 顯示變化
        ownerPlayer.ShowFloatingText($"-{amount}", Color.purple, this);
        UpdateHealthUI();
        SpecialDisplay(special);

        TurnManager.Instance.RaiseAnyConsumeHP(this);
    }
    // 技能
    public void ChangeSkillTime(Skill skill, int time)
    {
        if (!currentSkills.Contains(skill))
        {
            Debug.Log($"角色未持有技能:{skill.skillName}");
            return;
        }

        var key = (this, skill);
        if (!TurnManager.Instance.skillUseCounter.ContainsKey(key))
            TurnManager.Instance.skillUseCounter[key] = 0;

        if (TurnManager.Instance.skillUseCounter[key] > 0)
            TurnManager.Instance.skillUseCounter[key] += time;
        if (TurnManager.Instance.skillUseCounter[key] <= 0)
            TurnManager.Instance.skillUseCounter[key] = 0;
        
    }
    public void GetSkill(Skill skill)
    {
        if (currentSkills.Contains(skill)) return;
        CharacterSelectionManager.Instance.SelectSkill(skill, this);
        CharacterSelectionManager.Instance.currentSelectingPlayer = null;
        OnGetSkill?.Invoke(this, skill);
    }
    public void LoseSkill(Skill skill)
    {
        if (!currentSkills.Contains(skill)) return;
        ReplySkill(skill);
        CharacterSelectionManager.Instance.RemoveSkill(skill, this);
        OnLoseSkill?.Invoke(this, skill);
    }
    public void InvalidSkill(Skill skill)
    {
        if (currentSkills.Contains(skill))
            invalidSkills.Add(skill);
    }
    public void ReplySkill(Skill skill)
    {
        if (invalidSkills.Contains(skill))
            invalidSkills.Remove(skill);
    }
    // 被動技能
    public void ChangePassiveSkillTime(PassiveSkill passive, int time)
    {
        if (!currentPassiveSkills.Contains(passive))
        {
            Debug.Log($"角色未持有技能:{passive.skillName}");
            return;
        }
        
        PassiveSkilCtrl psc = GetComponent<PassiveSkilCtrl>();
        var key = (this, passive);
        if (!psc.passiveUseCounter.ContainsKey(key))
            psc.passiveUseCounter[key] = 0;

        if (psc.passiveUseCounter[key] > 0)
            psc.passiveUseCounter[key] += time;
        if (psc.passiveUseCounter[key] <= 0)
            psc.passiveUseCounter[key] = 0;

        psc.PassiveFinish(passive);
    }
    public void GetPassiveSkill(PassiveSkill passive)
    {
        if (currentPassiveSkills.Contains(passive)) return;
        CharacterSelectionManager.Instance.SelectPassiveSkill(passive, this);
        CharacterSelectionManager.Instance.currentSelectingPlayer = null;
        OnGetPassiveSkill?.Invoke(this, passive);
    }
    public void LosePassiveSkill(PassiveSkill passive)
    {
        if (!currentPassiveSkills.Contains(passive)) return;
        ReplyPassiveSkill(passive);
        CharacterSelectionManager.Instance.RemovePassiveSkill(passive, this);
        OnLosePassiveSkill?.Invoke(this, passive);
    }
    public void InvalidPassiveSkill(PassiveSkill passive)
    {
        if (currentPassiveSkills.Contains(passive))
            invalidPassiveSkills.Add(passive);
    }
    public void ReplyPassiveSkill(PassiveSkill passive)
    {
        if (invalidPassiveSkills.Contains(passive))
            invalidPassiveSkills.Remove(passive);
    }
    
    private void SpecialDisplay(SpecialEffects special)// 顯示特效
    {
        switch (special)
        {
            case SpecialEffects.OnDamage_Normal:
                GameObject OD_N = Instantiate(CharacterSelectionManager.Instance.OnDamage, transform);
                OD_N.SetActive(true);
                break;
            case SpecialEffects.OnHeal_Normal:
                GameObject OH_N = Instantiate(CharacterSelectionManager.Instance.OnHeal, transform);
                OH_N.SetActive(true);
                break;

        }
    }

    private void UpdateHealthUI()// 更新血量
    {
        if (healthText != null)
            healthText.text = $"<color=red>{currentHealth}</color>/{currentMaxHP}";
    }
    #endregion

    public void SkillClick()
    {
        if (!IsAlive)
        {
            Debug.LogWarning("該角色已死亡");
            LogWarning.Instance.Warning("該角色已死亡");
            return;
        }
        if (TurnManager.Instance != null && TurnManager.Instance.waitingForTarget)
        {
            TurnManager.Instance.OnTargetToggled(this);
        }
    }

    public void ToggleHighlight(bool active)// 選中後改變顯示
    {
        if (highlightRenderer != null)
            highlightRenderer.enabled = active;
    }

    public void OpenSkillBar()// 開啟技能欄
    {
        Open_SkillBar?.Invoke(this);
    }
}
