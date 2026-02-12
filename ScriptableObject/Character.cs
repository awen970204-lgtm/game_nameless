using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "BattleGameObjects/Character")]
public class Character : ScriptableObject
{
    // 角色類型
    public enum CHARACTER_TYPE
    {
        MALE,
        FEMALE,
        ELSE,
    }

    public CHARACTER_TYPE characterTYPE;   //類型 
    public string characterName;           //角色名
    public Sprite characterPicture;        //角色表現圖
    public Sprite characterAvatar;         //角色頭貼

    [Header("Amount")]
    public int characterMaxHP = 20;        // 血量上限
    public int characterStartHP = 20;      // 初始血量
    public int attackPower = 0;            // 攻擊力
    public int healPower = 0;              // 恢復力
    public int defense = 0;                // 防禦力
    public float damageMultiplier = 1f;    // 傷害加成(倍率)
    public float damageReduction = 0f;     // 傷害減免(定值)

    public int drawCount = 1;
    [Header("skills")]
    public List<Skill> skills;                 // 主動技能
    public List<PassiveSkill> passiveSkills;   // 被動技能
}
