using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolController : MonoBehaviour
{
    public GameObject Toolbar;
    public GameObject Panel;
    private bool PanelClosed = true;
    private bool ToolbarOpened = false;
    public Transform broadcast;
    public TMP_Text broadcastText;

    public Button battleOverButton;

    // 暫停功能
    public GameObject PauseButton;
    public GameObject CanclePauseButton;
    public GameObject PausePanel;
    public static bool isPause = false;

    void Start()
    {
        if (Toolbar == null || Panel == null || broadcastText == null || broadcast == null ||
         PauseButton == null || CanclePauseButton == null || PausePanel == null)
        {
            Debug.LogError("有物品未綁定");
            return;
        }
        Panel.SetActive(false);
        PauseButton.SetActive(true);
        CanclePauseButton.SetActive(false);
        PausePanel.SetActive(false);
        battleOverButton.onClick.AddListener(()=> GameModeManager.Instance.BattleOver());
    }
    void OnEnable()
    {
        if (TurnManager.Instance == null) return;
        // 回合播報
        TurnManager.OnBattleBegin += HandleBattleBegin;
        TurnManager.OnTurnStart += HandleTurnStart;
        TurnManager.OnTurnEnd += HandleTurnEnd;
        // 互動播報
        TurnManager.OnAttackEvent += HandleAttactEvent;
        TurnManager.OnAnyBeHealed += HandleBeHealed;
        TurnManager.OnAnyCharacterDead += HandleCharacterDead;
        TurnManager.OnAnyCardPlayBegin += HandleCardPlayBegin;
        TurnManager.OnAnyCardPlayed += HandleCardPlayed;
        TurnManager.OnAnySkillBegin += HandleSkillBegin;
        TurnManager.OnAnySkillEnd += HandleSkillEnd;
        TurnManager.OnAnyPassiveSkillBegin += HandlePassiveSkillBegin;
        TurnManager.OnAnyPassiveSkillEnd += HandlePassiveSkillEnd;
        ContinuedEffectCtrl.OnEffectGot += HandleEffectGot;
        ContinuedEffectCtrl.OnEffectExpired += HandleEffectLosed;
        Player.OnPlayerDrawCard += HandlePlayerDrawCard;
        CharacterHealth.OnGetSkill += HandleGetSkill;
        CharacterHealth.OnLoseSkill += HandleLoseSkill;
        CharacterHealth.OnGetPassiveSkill += HandleGetPassiveSkill;
        CharacterHealth.OnLosePassiveSkill += HandleLosePassiveSkill;
    }
    void OnDisable()
    {
        Panel.SetActive(false);
        if (TurnManager.Instance == null) return;
        TurnManager.OnBattleBegin -= HandleBattleBegin;
        TurnManager.OnTurnStart -= HandleTurnStart;
        TurnManager.OnTurnEnd -= HandleTurnEnd;
        TurnManager.OnAttackEvent -= HandleAttactEvent;
        TurnManager.OnAnyBeHealed -= HandleBeHealed;
        TurnManager.OnAnyCharacterDead -= HandleCharacterDead;
        TurnManager.OnAnyCardPlayBegin -= HandleCardPlayBegin;
        TurnManager.OnAnyCardPlayed -= HandleCardPlayed;
        TurnManager.OnAnySkillBegin -= HandleSkillBegin;
        TurnManager.OnAnySkillEnd -= HandleSkillEnd;
        TurnManager.OnAnyPassiveSkillBegin -= HandlePassiveSkillBegin;
        TurnManager.OnAnyPassiveSkillEnd -= HandlePassiveSkillEnd;
        ContinuedEffectCtrl.OnEffectGot -= HandleEffectGot;
        ContinuedEffectCtrl.OnEffectExpired -= HandleEffectLosed;
        Player.OnPlayerDrawCard -= HandlePlayerDrawCard;
        CharacterHealth.OnGetSkill -= HandleGetSkill;
        CharacterHealth.OnLoseSkill -= HandleLoseSkill;
        CharacterHealth.OnGetPassiveSkill -= HandleGetPassiveSkill;
        CharacterHealth.OnLosePassiveSkill -= HandleLosePassiveSkill;
    }

    public void ClickToolbar()
    {
        if (ToolbarOpened)
        {
            RectTransform transform = Toolbar.GetComponent<RectTransform>();
            transform.anchoredPosition += new Vector2(-100, 0);
            Panel.SetActive(false);
            PanelClosed = true;
        }
        else
        {
            RectTransform transform = Toolbar.GetComponent<RectTransform>();
            transform.anchoredPosition += new Vector2(100, 0);
            Panel.SetActive(false);
            PanelClosed = true;
        }
        ToolbarOpened = !ToolbarOpened;
    }
    public void OnClickReporter()
    {
        Panel.SetActive(PanelClosed);
        PanelClosed = !PanelClosed;
    }
    public void PauseGame() // 暫停
    {
        isPause = !isPause;

        if (isPause)
        {
            PauseButton.SetActive(false);
            CanclePauseButton.SetActive(true);
            PausePanel.SetActive(true);
            Time.timeScale = 0;
        }
        else
        {
            PauseButton.SetActive(true);
            CanclePauseButton.SetActive(false);
            PausePanel.SetActive(false);
            Time.timeScale = 1;
        }
    }

    private void HandleBattleBegin()
    {
        WriteReport("戰鬥開始");
    }
    private void HandleTurnStart(Player acting)
    {
        WriteReport($" <color=#0080FF>(P{acting.Player_nunber})</color>的回合開始");
    }
    private void HandleTurnEnd(Player acting)
    {
        WriteReport($" <color=#0080FF>(P{acting.Player_nunber})</color>的回合結束");
    }
    private void HandleAttactEvent(CharacterHealth attacker, CharacterHealth injured)
    {
        string att = $"{attacker.character_data.characterName}(P{attacker.ownerPlayer.Player_nunber})";
        string inj = $"{injured.character_data.characterName}(P{injured.ownerPlayer.Player_nunber})";
        WriteReport($" <color=#0080FF>{att}</color>對<color=#0080FF>{inj}</color>\n 造成了{attacker.lastAttackDamage}點傷害");
    }
    private void HandleBeHealed(CharacterHealth act)
    {
        string acting = $"{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})";
        WriteReport($" <color=#0080FF>{acting}</color>回復了{act.lastHealAmount}點血量");
    }
    private void HandleCharacterDead(CharacterHealth act)
    {
        string acting = $"<color=#0080FF>{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})</color>";
        string killer = $"<color=#0080FF>{act.killer.character_data.characterName}(P{act.killer.ownerPlayer.Player_nunber}</color>";
        WriteReport($" {acting}被{killer}殺死");
    }

    private void HandlePlayerDrawCard(Player act, int number)
    {
        string acting = $"<color=#0080FF>(P{act.Player_nunber})</color>";
        WriteReport($" {acting}抽了{number}張牌");
    }
    private void HandleCardPlayBegin(Player act, Card card)
    {
        string acting = $"<color=#0080FF>(P{act.Player_nunber})</color>";
        WriteReport($" {acting}使用了卡片<color=#FFDD55>{card.cardName}</color>");
    }
    private void HandleCardPlayed(Player act, Card card)
    {
        string acting = $"<color=#0080FF>(P{act.Player_nunber})</color>";
        WriteReport($" {acting}使用完卡片<color=#FFDD55>{card.cardName}</color>");
    }

    private void HandleGetSkill(CharacterHealth act, Skill skill)
    {
        string acting = $"{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})";
        WriteReport($" <color=#0080FF>{acting}</color>獲得技能<color=#FFDD55>{skill.skillName}</color>");
    }
    private void HandleLoseSkill(CharacterHealth act, Skill skill)
    {
        string acting = $"{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})";
        WriteReport($" <color=#0080FF>{acting}</color>失去技能<color=#FFDD55>{skill.skillName}</color>");
    }
    private void HandleGetPassiveSkill(CharacterHealth act, PassiveSkill skill)
    {
        string acting = $"{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})";
        WriteReport($" <color=#0080FF>{acting}</color>獲得被動<color=#FFDD55>{skill.skillName}</color>");
    }
    private void HandleLosePassiveSkill(CharacterHealth act, PassiveSkill skill)
    {
        string acting = $"{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})";
        WriteReport($" <color=#0080FF>{acting}</color>失去被動<color=#FFDD55>{skill.skillName}</color>");
    }

    private void HandleSkillBegin(CharacterHealth act, Skill skill)
    {
        string acting = $"<color=#0080FF>{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})</color>";
        WriteReport($" {acting}發動了技能<color=#FFDD55>{skill.skillName}</color>");
    }
    private void HandleSkillEnd(CharacterHealth act, Skill skill)
    {
        string acting = $"<color=#0080FF>{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})</color>";
        WriteReport($" {acting}發動完技能<color=#FFDD55>{skill.skillName}</color>");
    }
    private void HandlePassiveSkillBegin(CharacterHealth act, PassiveSkill skill)
    {
        string acting = $"<color=#0080FF>{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})</color>";
        WriteReport($" {acting} 發動了被動<color=#FFDD55>{skill.skillName}</color>");
    }
    private void HandlePassiveSkillEnd(CharacterHealth act, PassiveSkill skill)
    {
        string acting = $"<color=#0080FF>{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})</color>";
        WriteReport($" {acting} 發動完被動<color=#FFDD55>{skill.skillName}</color>");
    }

    private void HandleEffectGot(ContinuedEffect effect, CharacterHealth act)
    {
        string acting = $"<color=#0080FF>{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})</color>";
        WriteReport($" {acting} 獲得了持續效果:\n <color=#FFDD55>{effect.EffectName}</color>");
    }
    private void HandleEffectLosed(ContinuedEffect effect, CharacterHealth act)
    {
        string acting = $"<color=#0080FF>{act.character_data.characterName}(P{act.ownerPlayer.Player_nunber})</color>";
        WriteReport($" {acting} 失去了持續效果:\n <color=#FFDD55>{effect.EffectName}</color>");
    }

    private void WriteReport(string text)
    {
        if (!TurnManager.Instance.GameStart) return;
        broadcastText.text += $"{text}\n";
    }
}
