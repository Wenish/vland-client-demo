using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillEffectChain", menuName = "Game/Skills/Skill Effect Chain")]
public class SkillEffectChainData : ScriptableObject
{
    [SerializeReference]
    public List<SkillEffectNodeData> rootNodes = new(); 

    public void Execute(CastContext castContext, List<UnitController> targets)
    {
        foreach (var rootNode in rootNodes)
        {
            rootNode.Execute(castContext, targets);
        }
    }
}