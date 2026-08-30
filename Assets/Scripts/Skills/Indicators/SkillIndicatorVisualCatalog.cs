using System.Collections.Generic;
using ShadowInfection.DI;
using UnityEngine;

/// <summary>
/// Client-side lookup of SkillIndicatorData by asset name so TargetRpc payloads
/// can resolve textures/materials without sending UnityEngine.Object references.
/// </summary>
public static class SkillIndicatorVisualCatalog
{
    private const string SkillDatabaseResource = "ScriptableObjects/SkillDatabase";

    private static Dictionary<string, SkillIndicatorData> _byName;
    private static bool _harvested;

    public static SkillIndicatorData Get(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
            return null;

        EnsureDictionary();
        if (_byName.TryGetValue(assetName, out var data))
            return data;

        HarvestFromSkillDatabase();
        return _byName.TryGetValue(assetName, out data) ? data : null;
    }

    public static void Register(SkillIndicatorData data)
    {
        if (data == null || string.IsNullOrEmpty(data.name))
            return;

        EnsureDictionary();
        _byName[data.name] = data;
    }

    private static void EnsureDictionary()
    {
        _byName ??= new Dictionary<string, SkillIndicatorData>();
    }

    private static void HarvestFromSkillDatabase()
    {
        if (_harvested)
            return;

        _harvested = true;
        EnsureDictionary();

        SkillDatabase database = null;
        if (GameLifetimeScope.TryResolve<IGameDatabases>(out var databases))
            database = databases.Skills;
        if (database == null)
            database = Resources.Load<SkillDatabase>(SkillDatabaseResource);
        if (database?.allSkills == null)
            return;

        var visitedChains = new HashSet<SkillEffectChainData>();
        for (int i = 0; i < database.allSkills.Count; i++)
            HarvestFromSkill(database.allSkills[i], visitedChains);
    }

    private static void HarvestFromSkill(SkillData skill, HashSet<SkillEffectChainData> visitedChains)
    {
        if (skill == null)
            return;

        Register(skill.aimPreviewIndicator);
        HarvestFromChain(skill.initTrigger, visitedChains);
        HarvestFromChain(skill.castTrigger, visitedChains);

        var triggers = skill.reactiveTriggers;
        if (triggers == null)
            return;

        for (int i = 0; i < triggers.Count; i++)
        {
            var trigger = triggers[i];
            if (trigger != null)
                HarvestFromChain(trigger.onTrigger, visitedChains);
        }
    }

    private static void HarvestFromChain(
        SkillEffectChainData chain,
        HashSet<SkillEffectChainData> visitedChains)
    {
        if (chain == null || !visitedChains.Add(chain))
            return;

        SkillEffectChainUtil.AnyEffect(chain, effect =>
        {
            CollectFromEffect(effect, visitedChains);
            return false;
        });
    }

    private static void CollectFromEffect(
        SkillEffectData effect,
        HashSet<SkillEffectChainData> visitedChains)
    {
        switch (effect)
        {
            case SkillEffectMechanicShowIndicator show:
                Register(show.indicator);
                break;
            case SkillEffectMechanicRecastWindowData recast:
                Register(recast.aimPreviewIndicator);
                HarvestFromChain(recast.onRecast, visitedChains);
                HarvestFromChain(recast.onExpire, visitedChains);
                break;
            case SkillEffectMechanicChannelData channel:
                HarvestFromChain(channel.tickEffect, visitedChains);
                break;
            case SkillEffectMechanicManualChannelData manual:
                HarvestFromChain(manual.triggerEffect, visitedChains);
                break;
            case SkillEffectMechanicProjectile projectile:
                HarvestFromChain(projectile.onHitEffectChain, visitedChains);
                break;
            case SkillEffectMechanicBuffPeriodicSkillEffectChain periodic:
                HarvestFromChain(periodic.effectChainDataOnTick, visitedChains);
                break;
            case SkillEffectMechanicAreaZone zone:
                HarvestFromChain(zone.onTickEffect, visitedChains);
                break;
            case SkillEffectMechanicConditionalCasterStacksData conditional:
                HarvestFromChain(conditional.onConditionMet, visitedChains);
                HarvestFromChain(conditional.onConditionFailed, visitedChains);
                break;
        }
    }
}
