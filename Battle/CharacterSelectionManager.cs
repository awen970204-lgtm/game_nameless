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
    public GameObject characterPrefab;
    public GameObject skillPrefab;
    public GameObject passiveSkillPrefab;
    [Header("Player")]
    public Player player1;
    public Player player2;
    [Header("special")]
    public GameObject OnDamage;
    public GameObject OnHeal;

    public static Player currentSelectingPlayer;  // 當前正在選角的玩家
    public static bool PVP_Mode = false;
    public static bool canChoseCharacter = false;
    private EventBattleData currentBattleData;

    public static Action<Player, Character> SummonCharacter;

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
            OptionalCharacters.AddRange(StoryModeManager.characters);
            player1.team = TeamID.Team1;
            player2.team = TeamID.Enemy;
        }
        if (player2.team == TeamID.Enemy)
            player2.AutoActivity = true;
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
            player1.SetDesk(StoryModeManager.cards);
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
        player1.SetPlayerLevel(data.FixedLevel? data.teammateLevel : StoryModeManager.playerLevel);
        player2.SetPlayerLevel(data.enemyLevel);
        StartCoroutine(SetStoryCharacters(data));
    }
    private IEnumerator SetStoryCharacters(EventBattleData data)
    {
        player1.SelectButton.gameObject.SetActive(!data.Fixedteammate);
        player2.SelectButton.gameObject.SetActive(false);

        if (data.Fixedteammate && player1.MaxMenber < data.teammates.Length)
            player1.MaxMenber = data.teammates.Length;
        foreach(var c in data.teammates)
        {
            yield return CreateCharacter(c, player1);
        }
        player2.MaxMenber = data.enemys.Length;
        foreach(var c in data.enemys)
        {
            yield return CreateCharacter(c, player2);
        }

        StartChose();
    }

    #region Create Character
    
    public void SelectCharacter(Character characterData)// 當選擇角色按鈕被點擊時，生成角色
    {
        if (currentSelectingPlayer == null || currentSelectingPlayer.Player_characterTransform == null)
        {
            Debug.LogWarning("No current player");
            return;
        }
        if (characterPrefab == null)
        {
            Debug.LogError("No Character Prefab");
            return;
        }
        if (characterData == null)
        {
            Debug.LogError("characterData is null");
            return;
        }
        if (GameModeManager.Instance?.gameMode == GameMode.story)
        {
            if (currentSelectingPlayer.playerCharacters.Any(c => c.character_data == characterData) &&
                !TurnManager.Instance.GameStart)
            {
                Debug.Log("已選擇過該角色");
                LogWarning.Instance.Warning("已選擇過該角色");
                return;
            }
        }

        StartCoroutine(CreateCharacter(characterData, currentSelectingPlayer));
        currentSelectingPlayer = null;
    }
    public IEnumerator CreateCharacter(Character characterData, Player player) // 生成角色
    {
        if (player.playerCharacters.Count >= player.MaxMenber)
        {
            Debug.LogWarning($"P{player.Player_nunber}已達上限, Max:{player.MaxMenber}");
            yield break;
        }
        // 生成角色
        GameObject go = Instantiate(characterPrefab, player.Player_characterTransform);
        CharacterHealth ch_H = go.GetComponent<CharacterHealth>();
        PassiveSkilCtrl ch_PS = go.GetComponent<PassiveSkilCtrl>();
        int insertIndex = player.playerCharacters
            .FindIndex(c => c.character_data.tauntLevel < characterData.tauntLevel);

        if (insertIndex < 0)
            insertIndex = player.playerCharacters.Count;

        player.playerCharacters.Insert(insertIndex, ch_H);
        go.transform.SetSiblingIndex(insertIndex);
        // 設定屬性
        ch_H.character_data = characterData;
        if (player.Player_nunber == 1)
            ch_H.team = TeamID.Team1;
        else if (PVP_Mode)
            ch_H.team = TeamID.Team2;
        else
            ch_H.team = TeamID.Enemy;

        ch_H.ownerPlayer = player;

        ch_H.level = player.playerLevel;

        StartCoroutine(ShowTeamMenbers(player));
        
        if (!TurnManager.Instance.GameStart)
            Debug.Log($"Player{player.Player_nunber} 選擇了 {characterData.characterName}");
        else
            Debug.Log($"Player{player.Player_nunber} 召喚了 {characterData.characterName}");

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

        go.SetActive(true);

        if (!TurnManager.Instance.GameStart && player1.playerCharacters.Count > 0 && player2.playerCharacters.Count > 0)
        {
            stratButton.SetActive(true);
        }
        if (TurnManager.Instance.GameStart)
        {
            SummonCharacter?.Invoke(player, characterData);
        }

        // 排序
        player.playerCharacters = player.playerCharacters
            .OrderByDescending(c => c.character_data.tauntLevel)
            .ToList();

        for (int i = 0; i < player.playerCharacters.Count; i++)
        {
            player.playerCharacters[i].transform.SetSiblingIndex(i);
        }

    }
    public IEnumerator ShowTeamMenbers(Player player) // 顯示隊伍人數
    {
        player.PlayerMenbers.text = $"{player.playerCharacters.Count}/{player.MaxMenber}";
        player.PlayerMenbers.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        player.PlayerMenbers.gameObject.SetActive(false);
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

        Debug.Log($"{ch.character_data.characterName}獲得技能:{skill.skillName}");
    }
    public void RemoveSkill(Skill skill, CharacterHealth ch)// 移除技能
    {
        // 檢查持有
        if (!ch.currentSkills.Contains(skill)) return;

        ch.currentSkills.Remove(skill);
        Debug.Log($"{ch.character_data.characterName}失去技能:{skill.skillName}");
    }
    public void SelectPassiveSkill(PassiveSkill skill, CharacterHealth ch)// 生成被動
    {
        // 檢查持有
        if (ch.currentPassiveSkills.Contains(skill)) return;
        currentSelectingPlayer = ch.ownerPlayer;
        ch.currentPassiveSkills.Add(skill);

        Debug.Log($"{ch.character_data.characterName}獲得技能:{skill.skillName}");
    }
    public void RemovePassiveSkill(PassiveSkill skill, CharacterHealth ch)// 移除被動
    {
        // 檢查持有
        if (!ch.currentPassiveSkills.Contains(skill)) return;

        ch.currentPassiveSkills.Remove(skill);
        Debug.Log($"{ch.character_data.characterName}失去技能:{skill.skillName}");
    }

    #endregion
}
