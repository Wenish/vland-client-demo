using System;
using Mirror;
using UnityEngine;

[Serializable]
public class SkillInstance
{
    public string skillName;
    [NonSerialized]
    public SkillData skillData;
    public double lastCastTime = -Mathf.Infinity;
    public bool IsOnCooldown => NetworkTime.time < lastCastTime + skillData.cooldown;
    public float CooldownRemaining => skillData.cooldown - (float)(NetworkTime.time - lastCastTime);
    public float CooldownProgress => (CooldownRemaining / skillData.cooldown) * 100f;
}