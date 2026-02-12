using UnityEngine;
using TMPro;

public class PassiveSkill_display : MonoBehaviour
{
    [HideInInspector] public PassiveSkill Skill_data;
    [HideInInspector] public CharacterHealth selfHealth;
    public TextMeshProUGUI skillName;

    private PassiveSkilCtrl passiveCtrl;

    void Start()
    {
        passiveCtrl = selfHealth.GetComponent<PassiveSkilCtrl>();
        UpdateDisplay();
    }

    void OnEnable()
    {
        PassiveSkilCtrl.OnPassiveFinished += UpdateDisplay;
        PassiveSkilCtrl.OnPassiveRemake += UpdateDisplay;
    }

    void OnDisable()
    {
        PassiveSkilCtrl.OnPassiveFinished -= UpdateDisplay;
        PassiveSkilCtrl.OnPassiveRemake -= UpdateDisplay;
    }

    // 刷新顯示
    public void UpdateDisplay()
    {
        if (Skill_data == null || passiveCtrl == null) return;

        var key = (selfHealth, Skill_data);
        int usedCount = 0;
        passiveCtrl.passiveUseCounter.TryGetValue(key, out usedCount);

        // 顯示已觸發次數 / 上限
        if (Skill_data.LimitedTimes)
            skillName.text = $"{Skill_data.skillName}({usedCount}/{Skill_data.maxTriggersPerTurn})";
        else skillName.text = $"{Skill_data.skillName}({usedCount})";
    }
}
