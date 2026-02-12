using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;



// 掛在技能按鈕上
public class SkillCtrl : MonoBehaviour
{
    public Skill Skill_data;
    public TMP_Text skillName;
    public TMP_Text skillTip;     // 用來顯示提示
    public Button skillButton;    // 綁定 UI Button
    public GameObject checkButton;

    [HideInInspector] public CharacterHealth self; // 持有者

    void Awake()
    {
        checkButton = TurnManager.Instance.checkButton;
        skillTip = TurnManager.Instance.UseTips;
    }
    void OnEnable()
    {
        TurnManager.OnAnySkillBegin += handleSkillBegin;
        TurnManager.OnAnySkillEnd += handleSkillEnd;
        TurnManager.OnCancleChoose += handleCancle;
    }
    void OnDisable()
    {
        TurnManager.OnAnySkillBegin -= handleSkillBegin;
        TurnManager.OnAnySkillEnd -= handleSkillEnd;
        TurnManager.OnCancleChoose -= handleCancle;
    }

    public void Init(Skill skill)// 初始化按鈕
    {
        Skill_data = skill;
        if (skillName != null)
            skillName.text = skill.skillName;
    }

    private void handleSkillBegin(CharacterHealth characterHealth, Skill skill)
    {
        if (self == characterHealth && Skill_data == skill)
            SetSkillNameColor(Color.blue);
    }
    private void handleSkillEnd(CharacterHealth characterHealth, Skill skill)
    {
        if (self == characterHealth && Skill_data == skill)
            SetSkillNameColor(Color.black);
    }
    private void handleCancle() => SetSkillNameColor(Color.black);

    private void SetSkillNameColor(Color color)
    {
        skillName.color = color;
    }
    
    public void TryToUseSkill()// 點擊技能按鈕
    {
        if (self.team != TeamID.Enemy)
            OnClick();
    }
    public void OnClick()
    {
        if (Skill_data == null) return;
        if (TurnManager.Instance == null || !TurnManager.Instance.GameStart) return;
        if (!self.currentSkills.Contains(Skill_data)) return;
        if (self.invalidSkills.Contains(Skill_data))
        {
            LogWarning.Instance.Warning($"技能:{Skill_data.skillName}失效中");
            Debug.Log($"<color=#FFDD55>#</color> 技能:{Skill_data.skillName}失效中");
            return;
        }
        if (Skill_data.skillNeed != null)
        {
            foreach (var limit in Skill_data.skillNeed)
            {
                if (!LimitChecker.CheckLimit(limit, self, self))
                {
                    LogWarning.Instance.Warning($"不滿足技能:{Skill_data.skillName}的需求");
                    Debug.Log($"<color=#FFDD55># 不滿足</color>技能:{Skill_data.skillName}的需求");
                    return;
                }
            }
        }

        if (TurnManager.Instance.waitingForAction == true)
        {
            // 使用上限
            if (!TurnManager.Instance.CanUseSkill(self, Skill_data) && Skill_data.LimitedTimes)
            {
                LogWarning.Instance.Warning($"技能:{Skill_data.skillName} 本回合已達使用次數上限");
                Debug.Log($"<color=#FFDD55># </color>技能:{Skill_data.skillName} 本回合已達使用次數上限");
                return;
            }

            if (self.ownerPlayer.ISActive)
            {
                Debug.Log($"發動了技能:{Skill_data.skillName}");
                TurnManager.Instance.OnSkillSelected(Skill_data, self, self.ownerPlayer);
                checkButton.SetActive(true);
            }
            else if (!self.ownerPlayer.ISActive)
            {
                LogWarning.Instance.Warning($"不處於自身回合");
            }
        }
        else LogWarning.Instance.Warning($"正在進行其他選擇");
    }
}
