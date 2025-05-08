using System.Collections.Generic;
using UnityEngine;

public abstract class SkillEffectData : ScriptableObject
{
    public abstract SkillEffectType EffectType { get; }
    public abstract List<UnitController> Execute(CastContext castContext, List<UnitController> targets);
}

public enum SkillEffectType
{
    Mechanic,
    Condition,
    Target
}