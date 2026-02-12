using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "BattleGameObjects/Skill")]
public class Skill : ScriptableObject
{
    [Header("skillAmount")]
    public string skillName;         // 技能名
    [TextArea]
    public string skillEffect;
    public bool LimitedTimes = true; // 是否限制次數
    public int maxUsesPerTurn = 1;   // 每回合最多使用幾次
    public List<NeedState> skillNeed;
    [Header("effectEntries")]
    public List<EffectEntry> effectEntries = new List<EffectEntry>();
}
