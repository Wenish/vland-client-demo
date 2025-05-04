using UnityEngine;

[System.Serializable]
public class SkillInstance
{
    public string skillName;
    [System.NonSerialized]
    public SkillData skillData;

    public void ResolveData(SkillDatabase db)
    {
        skillData = db.GetSkillByName(skillName);
    }
}