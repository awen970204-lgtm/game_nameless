using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ContinuedEffectEntry
{
    public Trigger_Character triggerCharacter;
    public TriggerTime triggerTime;
    public TargetType effectTarget;
    public List<NeedState> Needs;
    public List<Effect> effects;
}

public enum EffectTendency
{
    None,
    Beneficial,
    harmful
}

[CreateAssetMenu(menuName = "BattleGameObjects/ContinuedEffect")]
public class ContinuedEffect : ScriptableObject
{
    public Sprite EffectImage;
    public string EffectName;
    [TextArea]
    public string Introduse;

    public EffectTendency tendency;
    public bool Removable = true;
    public int MaxOverlay = 1;
    public bool endable = true;

    public Trigger_Character endtrigger;
    public int Duration;
    public bool LimitedTimes = true;
    public int TriggerTimes;
    public List<ContinuedEffectEntry> continuedEffectEntrys;

    [System.NonSerialized] public int stack = 1;
}
