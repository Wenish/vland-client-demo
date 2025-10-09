using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectChain", menuName = "Game/Skills/Skill Effect Chain")]
public class SkillEffectChainData : ScriptableObject
{
    [SerializeReference]
    public List<SkillEffectNodeData> rootNodes = new();

    public IEnumerator ExecuteCoroutine(CastContext castContext, List<UnitController> targets)
    {
        var handels = new List<Coroutine>();
        var doneCount = 0;
        Action onRootComplete = () => doneCount++;
        foreach (var rootNode in rootNodes)
        {
            handels.Add(castContext.skillInstance.StartCoroutine(
                WaitOnRoot(rootNode, castContext, targets, onRootComplete)
            ));
        }

        while (doneCount < rootNodes.Count)
        {
            yield return null;
        }

    }

    private IEnumerator WaitOnRoot(SkillEffectNodeData rootNode, CastContext castContext, List<UnitController> targets, Action onComplete)
    {
        yield return rootNode.ExecuteCoroutine(castContext, targets);
        onComplete();
    }
}