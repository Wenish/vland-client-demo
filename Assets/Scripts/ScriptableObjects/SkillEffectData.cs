using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillEffectData : ScriptableObject
{
    public abstract SkillEffectType EffectType { get; }
    public abstract IEnumerator Execute(CastContext castContext, List<UnitController> targets, Action<List<UnitController>> onComplete);
}

public enum SkillEffectType
{
    Mechanic,
    Condition,
    Target
}