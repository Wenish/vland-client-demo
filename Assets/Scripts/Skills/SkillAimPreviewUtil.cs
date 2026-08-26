using System.Collections.Generic;

/// <summary>
/// Resolves which <see cref="SkillIndicatorData"/> to use for Shift+aim preview,
/// including recast windows (Phase 2 uses the recast chain's indicator).
/// </summary>
public static class SkillAimPreviewUtil
{
    public static SkillIndicatorData Resolve(NetworkedSkillInstance instance)
    {
        if (instance == null)
            return null;

        if (instance.skillData == null)
            instance.ResolveSkillData();

        return Resolve(instance.skillData, instance.IsRecastWindowOpen);
    }

    public static SkillIndicatorData Resolve(SkillData skillData, bool isRecastWindowOpen)
    {
        if (skillData == null)
            return null;

        if (isRecastWindowOpen)
        {
            var recast = FindFirstRecastWindow(skillData.castTrigger);
            if (recast != null)
            {
                if (recast.aimPreviewIndicator != null)
                    return recast.aimPreviewIndicator;

                SkillIndicatorData fromRecastChain = FindFirstShowIndicator(recast.onRecast);
                if (fromRecastChain != null)
                    return fromRecastChain;
            }
        }

        return skillData.aimPreviewIndicator;
    }

    public static SkillEffectMechanicRecastWindowData FindFirstRecastWindow(SkillEffectChainData chain)
    {
        SkillEffectMechanicRecastWindowData found = null;
        SkillEffectChainUtil.AnyEffect(
            chain,
            effect =>
            {
                if (effect is SkillEffectMechanicRecastWindowData recast)
                {
                    found = recast;
                    return true;
                }

                return false;
            });
        return found;
    }

    public static SkillIndicatorData FindFirstShowIndicator(SkillEffectChainData chain)
    {
        SkillIndicatorData found = null;
        SkillEffectChainUtil.AnyEffect(
            chain,
            effect =>
            {
                if (effect is SkillEffectMechanicShowIndicator show && show.indicator != null)
                {
                    found = show.indicator;
                    return true;
                }

                return false;
            });
        return found;
    }
}
