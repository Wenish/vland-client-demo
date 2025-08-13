using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillEffectData : ScriptableObject
{
    [Tooltip("If true, when this effect executes, the skill will be considered 'cast' (cooldown starts).")]
    public bool countsAsCasted = false;
    public abstract SkillEffectType EffectType { get; }
    public abstract IEnumerator Execute(CastContext castContext, List<UnitController> targets, Action<List<UnitController>> onComplete);
}

public enum SkillEffectType
{
    Mechanic,
    Condition,
    Target
}