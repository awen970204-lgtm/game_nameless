using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Data.Common;
using System.Linq;

public class CharacterSelectionManager : MonoBehaviour
{
    public static CharacterSelectionManager Instance;

    [Header("CharaterChose")]
    public SkillBarCtrl skillBarCtrl;
    public List<Character> OptionalCharacters = new List<Character>();
    public Transform choseTransform;
    public GameObject chosingPamel;
    public GameObject characterChosePrefab;
    public GameObject stratButton;
    [Header("Prefabs")]
    public GameObject characterPrefab;    // Prefab 上要掛 CharacterHealth
    public GameObject skillPrefab;        // Prefab 上要掛 SkillCrtl
    public GameObject passiveSkillPrefab; // Prefab 上要掛 text
    [Header("Player")]
    public Player player1;
    public Player player2;
    [Header("special")]
    public GameObject OnDamage;
    public GameObject OnHeal;

    [HideInInspector] public Player currentSelectingPlayer;     // 當前正在選角的玩家
    [HideInInspector] public bool PVP_Mode = false;
    [HideInInspector] public bool canChoseCharacter = false;
    private EventBattleData currentBattleData;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        PVP_Mode = !MenuManager.useEnemyAI;

        OptionalCharacters.Clear();
        if (GameModeManager.Instance.gameMode == GameMode.free)
        {
            OptionalCharacters.AddRange(GameModeManager.Instance.characterDatas);
            player1.team = TeamID.Team1;
            player2.team = PVP_Mode? TeamID.Team2 : TeamID.Enemy;
            player1.SetPlayerLevel(MenuManager.playerLevel);
            player2.SetPlayerLevel(MenuManager.enemyLevel);

            player1.SetDesk(MenuManager.player1Cards);
            player2.SetDesk(MenuManager.player2Cards);

            StartChose();
        }
        else if (GameModeManager.Instance.gameMode == GameMode.story)
        {
            OptionalCharacters.AddRange(StoryModeManager.Instance.characters);
            player1.team = TeamID.Team1;
            player2.team = TeamID.Enemy;
        }

    }

    private void StartChose() // 開始選擇
    {
        if (GameModeManager.Instance.gameMode == GameMode.free)
        {
            TurnManager.Instance.UseTips.text = "添加角色";
                
            foreach(Transform child in choseTransform)
                Destroy(child.gameObject);

            foreach(var character in OptionalCharacters)
            {
                GameObject GO = Instantiate(characterChosePrefab, choseTransform);

                GO.transform.GetChild(0).GetChild(0).GetComponentInChildren<Image>().sprite =
                    character.characterPicture;

                GO.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(
                    ()=> skillBarCtrl.OpenDefaultSkillBar(character));
                GO.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = character.characterName;

                GO.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(()=> SelectCharacter(character));

                GO.SetActive(true);
            }
            canChoseCharacter = true;
        }
        else
        {
            foreach(Transform child in choseTransform)
                Destroy(child.gameObject);

            foreach(var character in OptionalCharacters)
            {
                GameObject GO = Instantiate(characterChosePrefab, choseTransform);

                GO.transform.GetChild(0).GetChild(0).GetComponentInChildren<Image>().sprite =
                    character.characterPicture;

                GO.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(
                    ()=> skillBarCtrl.OpenDefaultSkillBar(character));
                GO.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = character.characterName;

                GO.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(()=> SelectCharacter(character));

                GO.SetActive(true);
            }
            player1.SetDesk(StoryModeManager.Instance.cards);
            player2.SetDesk(currentBattleData?.enemyCards.ToList());
        }
        player1.team = TeamID.Team1;
        player2.team = PVP_Mode ? TeamID.Team2: TeamID.Enemy;
    }
    public void SetCurrentPlayer(Player player) // 設定目前要選角的玩家
    {
        if (canChoseCharacter)
        {
            currentSelectingPlayer = player;
            Debug.Log($"Player{player.Player_nunber} 正在選擇角色...");
        }
    }

    public void SetStoryCharacter(EventBattleData data) // 戰鬥事件敵方
    {
        currentBattleData = data;
        PVP_Mode = false;
        canChoseCharacter = !data.Fixedteammate;

        foreach(var c in data.teammates)
        {
            currentSelectingPlayer = player1;
            SelectCharacter(c);
        }
        foreach(var c in data.enemys)
        {
            currentSelectingPlayer = player2;
            SelectCharacter(c);
        }

        player1.SetPlayerLevel(data.FixedLevel? data.teammateLevel : StoryModeManager.playerLevel);
        player2.SetPlayerLevel(data.enemyLevel);

        StartChose();
    }
    // 當選擇角色按鈕被點擊時，生成角色
    public void SelectCharacter(Character characterData)
    {
        if (currentSelectingPlayer == null)
        {
            Debug.LogWarning("沒有指定玩家就嘗試選角");
            return;
        }
        if (characterPrefab == null)
        {
            Debug.LogError("沒有設定 Character Prefab");
            return;
        }

        if (currentSelectingPlayer.playerCharacters.Count >= currentSelectingPlayer.MaxMenber)
        {
            Debug.Log($"P{currentSelectingPlayer.Player_nunber} team is max:{currentSelectingPlayer.MaxMenber}");
            return;
        }
        
        // 生成角色
        GameObject go = Instantiate(characterPrefab, currentSelectingPlayer.Player_characterTransform);
        CharacterHealth ch_H = go.GetComponent<CharacterHealth>();
        PassiveSkilCtrl ch_PS = go.GetComponent<PassiveSkilCtrl>();
        // 設定屬性
        ch_H.character_data = characterData;
        if (currentSelectingPlayer.Player_nunber == 1)
            ch_H.team = TeamID.Team1;
        else if (PVP_Mode)
            ch_H.team = TeamID.Team2;
        else
            ch_H.team = TeamID.Enemy;

        ch_H.ownerPlayer = currentSelectingPlayer;

        ch_H.level = currentSelectingPlayer.playerLevel;

        currentSelectingPlayer.playerCharacters.Add(ch_H);
        currentSelectingPlayer.PlayerMenbers.text = 
            $"{currentSelectingPlayer.playerCharacters.Count}/{currentSelectingPlayer.MaxMenber}";
        
        // 生成技能按鈕
        foreach (var skill in characterData.skills)
        {
            SelectSkill(skill, ch_H);
        }
        // 生成被動按鈕
        foreach (var passiveSkill in characterData.passiveSkills)
        {
            SelectPassiveSkill(passiveSkill, ch_H);
        }

        Debug.Log($"Player{currentSelectingPlayer.Player_nunber} 選擇了 {characterData.characterName}");
        go.SetActive(true);

        // 選完就清空，避免誤選
        currentSelectingPlayer = null;

        if (!TurnManager.Instance.GameStart && player1.playerCharacters.Count > 0 && player2.playerCharacters.Count > 0)
        {
            stratButton.SetActive(true);
        }
    }
    public void SetCharacterLevel(CharacterHealth character, int originalLevel) // 設定角色等級
    {
        int level = Mathf.Max(1, originalLevel);
        
        character.currentMaxHP += level - 1;
        character.currentHealth += level - 1;

        character.currentAttackPower += (level + 1) / 5;
        character.currentDefense += level / 5;
    }
    
    public void SelectSkill(Skill skill, CharacterHealth ch)// 生成技能
    {
        // 檢查持有
        if (ch.currentSkills.Contains(skill)) return;
        currentSelectingPlayer = ch.ownerPlayer;
        ch.currentSkills.Add(skill);
        GameObject Sk_B = Instantiate(skillPrefab, currentSelectingPlayer.Player_skillTransform);

        Sk_B.GetComponent<SkillCtrl>().Skill_data = skill;
        Sk_B.GetComponent<SkillCtrl>().self = ch;
        Sk_B.GetComponentInChildren<TMP_Text>().text = $"{skill.skillName}";

        Sk_B.SetActive(true);
        Debug.Log($"{ch.character_data.characterName}獲得技能:{skill.skillName}");
    }
    public void RemoveSkill(Skill skill, CharacterHealth ch)// 移除技能
    {
        // 檢查持有
        if (!ch.currentSkills.Contains(skill)) return;

        foreach (Transform child in ch.ownerPlayer.Player_skillTransform)
        {
            SkillCtrl SC = child.GetComponent<SkillCtrl>();
            if (SC != null && SC.Skill_data == skill && SC.self == ch)
            {
                Destroy(child.gameObject); // 銷毀 UI
                ch.currentSkills.Remove(skill);
                Debug.Log($"{ch.character_data.characterName}失去技能:{skill.skillName}");
                break;
            }
        }
    }
    public void SelectPassiveSkill(PassiveSkill skill, CharacterHealth ch)// 生成被動
    {
        // 檢查持有
        if (ch.currentPassiveSkills.Contains(skill)) return;
        currentSelectingPlayer = ch.ownerPlayer;
        ch.currentPassiveSkills.Add(skill);
        GameObject PS_B = Instantiate(passiveSkillPrefab, currentSelectingPlayer.Player_PassiveSkillTransform);

        PS_B.GetComponentInChildren<PassiveSkill_display>().Skill_data = skill;
        PS_B.GetComponentInChildren<PassiveSkill_display>().selfHealth = ch;
        PS_B.GetComponentInChildren<TMP_Text>().text = $"{skill.skillName}(0)";

        PS_B.SetActive(true);
        Debug.Log($"{ch.character_data.characterName}獲得技能:{skill.skillName}");
    }
    public void RemovePassiveSkill(PassiveSkill skill, CharacterHealth ch)// 移除被動
    {
        // 檢查持有
        if (!ch.currentPassiveSkills.Contains(skill)) return;

        foreach (Transform child in ch.ownerPlayer.Player_PassiveSkillTransform)
        {
            PassiveSkill_display PSd = child.GetComponent<PassiveSkill_display>();
            if (PSd != null && PSd.Skill_data == skill && PSd.selfHealth == ch)
            {
                Destroy(child.gameObject); // 銷毀 UI
                ch.currentPassiveSkills.Remove(skill);
                Debug.Log($"{ch.character_data.characterName}失去技能:{skill.skillName}");
                break;
            }
        }
    }

    // 確認棄牌按鈕按下
    public void ConfirmFold()
    {

    }

    // 偷牌按鈕按下
    public void ConfirmSteal()
    {

    }
}
