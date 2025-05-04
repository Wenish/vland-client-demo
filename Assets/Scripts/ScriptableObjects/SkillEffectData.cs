using System.Collections.Generic;
using UnityEngine;

public abstract class SkillEffectData : ScriptableObject
{
    public abstract List<GameObject> Execute(GameObject caster, List<GameObject> targets);
}