using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillEffectChain", menuName = "Game/Skills/Skill Effect Chain")]
public class SkillEffectChainData : ScriptableObject
{
    [SerializeReference]
    public List<SkillEffectNodeData> rootNodes = new(); 

    public void Execute(GameObject caster, List<GameObject> targets)
    {
        foreach (var rootNode in rootNodes)
        {
            rootNode.Execute(caster, targets);
        }
    }
}