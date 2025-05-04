using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillDatabase", menuName = "Game/Skills/Database")]
public class SkillDatabase : ScriptableObject
{
    public List<SkillData> allSkills = new List<SkillData>();

    public SkillData GetSkillByName(string name)
    {
        return allSkills.Find(skill => skill.name == name);
    }
}