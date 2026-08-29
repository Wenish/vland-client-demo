using System;
using System.Collections.Generic;
using UnityEngine;

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

    /// <summary>
    /// Lowest <c>turnSpeedPercent</c> on Cast / Channel / ManualChannel / Delay in the main
    /// cast tree (not recast/tick nested chains). 1 when none are present.
    /// </summary>
    public static float GetMinTurnSpeedPercent(SkillData skillData)
    {
        if (skillData == null)
            return 1f;

        return GetMinTurnSpeedPercent(skillData.castTrigger);
    }

    public static float GetMinTurnSpeedPercent(SkillEffectChainData chain)
    {
        float min = 1f;
        ForEachEffect(chain, effect =>
        {
            if (TryGetTurnSpeedPercent(effect, out float percent))
                min = Mathf.Min(min, percent);
        });
        return min;
    }

    public static bool WillLockTurnSpeed(SkillData skillData)
    {
        return SkillAimUtil.IsTurnSpeedLocked(GetMinTurnSpeedPercent(skillData));
    }

    /// <summary>
    /// True when any effect in the main cast chain spawns/places at <see cref="CastContext.aimPoint"/>.
    /// </summary>
    public static bool HasSpawnAtAimPoint(SkillData skillData)
    {
        if (skillData == null)
            return false;

        return AnyEffect(skillData.castTrigger, IsSpawnAtAimPoint);
    }

    private static void ForEachEffect(SkillEffectChainData chain, Action<SkillEffectData> action)
    {
        if (chain == null || action == null || chain.rootNodes == null)
            return;

        for (int i = 0; i < chain.rootNodes.Count; i++)
            ForEachEffect(chain.rootNodes[i], action);
    }

    private static void ForEachEffect(SkillEffectNodeData node, Action<SkillEffectData> action)
    {
        if (node == null || action == null)
            return;

        if (node.effect != null)
            action(node.effect);

        if (node.children == null)
            return;

        for (int i = 0; i < node.children.Count; i++)
            ForEachEffect(node.children[i], action);
    }

    private static bool TryGetTurnSpeedPercent(SkillEffectData effect, out float percent)
    {
        switch (effect)
        {
            case SkillEffectMechanicCast cast:
                percent = cast.turnSpeedPercent;
                return true;
            case SkillEffectMechanicChannelData channel:
                percent = channel.turnSpeedPercent;
                return true;
            case SkillEffectMechanicManualChannelData manualChannel:
                percent = manualChannel.turnSpeedPercent;
                return true;
            case SkillEffectMechanicDelayData delay:
                percent = delay.turnSpeedPercent;
                return true;
            default:
                percent = 1f;
                return false;
        }
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

    private static bool IsSpawnAtAimPoint(SkillEffectData effect)
    {
        switch (effect)
        {
            case SkillEffectMechanicAreaZone zone:
                return zone.spawnAtAimPoint;
            case SkillEffectMechanicSpawnUnit spawn:
                return spawn.spawnAtAimPoint;
            case SkillEffectMechanicVFXGraph vfx:
                return vfx.spawnAtAimPoint;
            case SkillEffectTargetAreaVFX areaVfx:
                return areaVfx.spawnAtAimPoint;
            default:
                return false;
        }
    }
}
