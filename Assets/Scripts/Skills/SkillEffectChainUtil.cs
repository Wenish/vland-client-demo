using System;
using System.Collections.Generic;

public static class SkillEffectChainUtil
{
    public static bool AnyEffect(SkillEffectChainData chain, Func<SkillEffectData, bool> predicate)
    {
        if (chain == null || predicate == null || chain.rootNodes == null)
            return false;

        for (int i = 0; i < chain.rootNodes.Count; i++)
        {
            if (AnyEffect(chain.rootNodes[i], predicate))
                return true;
        }

        return false;
    }

    public static bool AnyEffect(SkillEffectNodeData node, Func<SkillEffectData, bool> predicate)
    {
        if (node == null || predicate == null)
            return false;

        if (node.effect != null && predicate(node.effect))
            return true;

        if (node.children == null)
            return false;

        for (int i = 0; i < node.children.Count; i++)
        {
            if (AnyEffect(node.children[i], predicate))
                return true;
        }

        return false;
    }

    public static bool UpdatesAimDuringCast(SkillData skillData)
    {
        if (skillData == null)
            return false;

        return AnyEffect(skillData.castTrigger, IsUpdateAimCastOrNestedRecast);
    }

    private static bool IsUpdateAimCastOrNestedRecast(SkillEffectData effect)
    {
        if (effect is SkillEffectMechanicCast cast && cast.updateAimDuringCast)
            return true;

        if (effect is SkillEffectMechanicChannelData channel && channel.updateAimDuringCast)
            return true;

        if (effect is SkillEffectMechanicManualChannelData manualChannel
            && manualChannel.updateAimDuringCast)
            return true;

        if (effect is SkillEffectMechanicRecastWindowData recast)
        {
            if (AnyEffect(recast.onRecast, IsUpdateAimCastOrNestedRecast))
                return true;
            if (AnyEffect(recast.onExpire, IsUpdateAimCastOrNestedRecast))
                return true;
        }

        return false;
    }
}
