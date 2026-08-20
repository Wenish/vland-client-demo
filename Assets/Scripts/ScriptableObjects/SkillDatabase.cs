using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillDatabase", menuName = "Game/Skills/Database")]
public class SkillDatabase : ScriptableObject
{
    [Expandable]
    public List<SkillData> allSkills = new List<SkillData>();

    public SkillData GetSkillByName(string name)
    {
        return allSkills.Find(skill => skill.skillName == name);
    }
}