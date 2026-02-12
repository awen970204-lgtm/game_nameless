using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillBarCtrl : MonoBehaviour
{
    public GameObject BarPanel;
    public GameObject SkillBarPrefab;
    public GameObject SkillIntroduse;

    private GameObject Pending_SkillBar;

    // 啟動
    void Awake()
    {
        BarPanel?.SetActive(false);
        SkillBarPrefab?.SetActive(false);
        SkillIntroduse?.SetActive(true);
    }
    void Start()// 訂閱事件
    {
        CharacterHealth.Open_SkillBar += OpenSkillBar;
    }
    void OnDisable()// 解除訂閱事件
    {
        CharacterHealth.Open_SkillBar -= OpenSkillBar;    
    }

    // 開啟技能欄
    public void OpenDefaultSkillBar(Character character)
    {
        if (BarPanel == null || SkillBarPrefab == null || character == null) return;
        BarPanel.SetActive(true);

        Transform transform = SkillIntroduse.GetComponent<Transform>();
        GameObject SI = Instantiate(SkillBarPrefab, transform);
        Pending_SkillBar = SI;

        TMP_Text name = SI.transform.GetChild(0).GetComponentInChildren<TMP_Text>();
        name.text = character.characterName;
        switch (character.characterTYPE)
        {
            case Character.CHARACTER_TYPE.MALE:
                name.color = Color.blue;
                break;
            case Character.CHARACTER_TYPE.FEMALE:
                name.color = Color.mediumVioletRed;
                break;
            case Character.CHARACTER_TYPE.ELSE:
                name.color = Color.black;
                break;
        }

        SI.transform.GetChild(1).GetComponent<TMP_Text>().text = $"HP:{character.characterStartHP}/{character.characterMaxHP}";

        GameObject skillsLayout = SI.transform.GetChild(2).transform.GetChild(1).gameObject;
        Transform skillsLayoutTransform = skillsLayout.GetComponent<Transform>();
        GameObject basicValueGO = skillsLayout.transform.GetChild(0).gameObject;
        GameObject skillPrefabGO = skillsLayout.transform.GetChild(1).gameObject;
        GameObject passiveSkillPrefabGO = skillsLayout.transform.GetChild(2).gameObject;
        skillPrefabGO.SetActive(false);
        passiveSkillPrefabGO.SetActive(false);

        // 設定基本數值
        basicValueGO.transform.GetChild(0).GetComponent<TMP_Text>().text = "基本數值";

        basicValueGO.transform.GetChild(1).GetComponent<TMP_Text>().text = 
        $"攻擊:{character.attackPower}\n"+
        $"回復力:{character.healPower}\n"+
        $"防禦:{character.defense}\n"+
        $"傷害倍率:{character.damageMultiplier*100}%\n"+
        $"受傷減免:{character.damageReduction}";
        // 生成技能
        foreach(var skill in character.skills)
        {
            GameObject skillGO = Instantiate(skillPrefabGO, skillsLayoutTransform);

            skillGO.transform.GetChild(0).GetComponent<TMP_Text>().text = $"{skill.skillName}";
            skillGO.transform.GetChild(1).GetComponent<TMP_Text>().text = $"{skill.skillEffect}";

            skillGO.SetActive(true);
        }
        // 生成被動技能
        foreach(var passiveskill in character.passiveSkills)
        {
            GameObject passiveskillGO = Instantiate(passiveSkillPrefabGO, skillsLayoutTransform);

            passiveskillGO.transform.GetChild(0).GetComponent<TMP_Text>().text = $"{passiveskill.skillName}";
            passiveskillGO.transform.GetChild(1).GetComponent<TMP_Text>().text = $"{passiveskill.skillEffect}";

            passiveskillGO.SetActive(true);
        }

        // 卡圖
        SI.transform.GetChild(3).transform.GetChild(0).GetComponent<Image>().sprite = character.characterPicture;

        Pending_SkillBar.SetActive(true);
    }
    public void CloseSkillBar()
    {
        if (Pending_SkillBar != null)
        {
            BarPanel.SetActive(false);
            Destroy(Pending_SkillBar);
        }
    }

    private void OpenSkillBar(CharacterHealth character)
    {
        if (BarPanel == null || SkillBarPrefab == null || character == null) return;
        BarPanel.SetActive(true);

        Transform transform = SkillIntroduse.GetComponent<Transform>();
        GameObject SI = Instantiate(SkillBarPrefab, transform);
        Pending_SkillBar = SI;

        TMP_Text name = SI.transform.GetChild(0).GetComponentInChildren<TMP_Text>();
        name.text = character.character_data.characterName;
        switch (character.character_data.characterTYPE)
        {
            case Character.CHARACTER_TYPE.MALE:
                name.color = Color.blue;
                break;
            case Character.CHARACTER_TYPE.FEMALE:
                name.color = Color.mediumVioletRed;
                break;
            case Character.CHARACTER_TYPE.ELSE:
                name.color = Color.black;
                break;
        }

        SI.transform.GetChild(1).GetComponent<TMP_Text>().text = $"HP:{character.currentHealth}/{character.currentMaxHP}";

        GameObject skillsLayout = SI.transform.GetChild(2).transform.GetChild(1).gameObject;
        Transform skillsLayoutTransform = skillsLayout.GetComponent<Transform>();
        GameObject basicValueGO = skillsLayout.transform.GetChild(0).gameObject;
        GameObject skillPrefabGO = skillsLayout.transform.GetChild(1).gameObject;
        GameObject passiveSkillPrefabGO = skillsLayout.transform.GetChild(2).gameObject;
        skillPrefabGO.SetActive(false);
        passiveSkillPrefabGO.SetActive(false);

        // 設定當前數值
        basicValueGO.transform.GetChild(0).GetComponent<TMP_Text>().text = "當前數值";

        basicValueGO.transform.GetChild(1).GetComponent<TMP_Text>().text = 
        $"攻擊:{character.currentAttackPower}\n"+
        $"回復力:{character.currentHealPower}\n"+
        $"防禦:{character.currentDefense}\n"+
        $"傷害倍率:{character.currentDamageMultiplier*100}%\n"+
        $"受傷減免:{character.currentDamageReduction}";
        // 生成技能
        foreach(var skill in character.currentSkills)
        {
            GameObject skillGO = Instantiate(skillPrefabGO, skillsLayoutTransform);

            skillGO.transform.GetChild(0).GetComponent<TMP_Text>().text = $"{skill.skillName}";
            skillGO.transform.GetChild(1).GetComponent<TMP_Text>().text = $"{skill.skillEffect}";

            skillGO.SetActive(true);
        }
        // 生成被動技能
        foreach(var passiveskill in character.currentPassiveSkills)
        {
            GameObject passiveskillGO = Instantiate(passiveSkillPrefabGO, skillsLayoutTransform);

            passiveskillGO.transform.GetChild(0).GetComponent<TMP_Text>().text = $"{passiveskill.skillName}";
            passiveskillGO.transform.GetChild(1).GetComponent<TMP_Text>().text = $"{passiveskill.skillEffect}";

            passiveskillGO.SetActive(true);
        }

        // 卡圖
        SI.transform.GetChild(3).transform.GetChild(0).GetComponent<Image>().sprite = character.character_data.characterPicture;

        Pending_SkillBar.SetActive(true);
    }
}
