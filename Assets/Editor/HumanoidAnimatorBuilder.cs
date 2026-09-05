using System;
using System.Collections.Generic;
using ShadowInfection.Animations;
using ShadowInfection.Items;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class HumanoidAnimatorBuilder
{
    private const string UnarmedPath = "Assets/Units/Animations/Unarmed.controller";
    private const string HumanoidPath = "Assets/Units/Animations/Humanoid.controller";
    private const string AvatarMaskPath = "Assets/Units/Animations/AvatarTop.mask";
    private const string HumanoidSetPath = "Assets/Resources/ScriptableObjects/AnimationSets/HumanoidUnarmed.asset";
    private const string OneHandSwordOverridePath = "Assets/Units/Animations/SwordAndShield.overrideController";

    private static readonly (AnimationStance stance, string overridePath)[] StanceOverrides =
    {
        (AnimationStance.Daggers, "Assets/Units/Animations/Daggers.overrideController"),
        (AnimationStance.Bow, "Assets/Units/Animations/Bow.overrideController"),
        (AnimationStance.Gun, "Assets/Units/Animations/Gun.overrideController"),
        (AnimationStance.Pistols, "Assets/Units/Animations/Pistols.overrideController"),
        (AnimationStance.TwoHandSword, "Assets/Units/Animations/Sword.overrideController"),
    };

    private static readonly (string overridePath, string animationSetPath)[] UnitSets =
    {
        (
            "Assets/Units/Animations/ZombieUnarmed.overrideController",
            "Assets/Resources/ScriptableObjects/AnimationSets/HumanoidZombieUnarmed.asset"
        ),
        (
            "Assets/Units/Animations/ZombieCrawlerUnarmed.overrideController",
            "Assets/Resources/ScriptableObjects/AnimationSets/HumanoidZombieCrawlerUnarmed.asset"
        ),
    };

    [MenuItem("Tools/Animation/Rebuild Humanoid Controller")]
    public static void BuildFromMenu()
    {
        Build();
        Debug.Log("Humanoid animator rebuild finished.");
    }

    [InitializeOnLoadMethod]
    private static void RebuildLegacyAttackLayer()
    {
        EditorApplication.delayCall += TryRebuildLegacyAttackLayer;
    }

    private static int autoRebuildTries;

    private static void TryRebuildLegacyAttackLayer()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            ScheduleAutoRebuildRetry();
            return;
        }

        var unarmed = AssetDatabase.LoadAssetAtPath<AnimatorController>(UnarmedPath);
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(HumanoidPath);
        if (unarmed == null || existing == null || existing.layers.Length < 2)
        {
            ScheduleAutoRebuildRetry();
            return;
        }

        if (existing.layers[1].stateMachine.states.Length <= 3)
            return;

        var preview = ExtractBaseClips(unarmed);
        if (preview.Loco == null || preview.Loco.Length == 0)
        {
            ScheduleAutoRebuildRetry();
            return;
        }

        try
        {
            Build();
            Debug.Log("Humanoid animator rebuilt (legacy per-weapon attack states).");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ScheduleAutoRebuildRetry();
        }
    }

    private static void ScheduleAutoRebuildRetry()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryRebuildLegacyAttackLayer;
            return;
        }

        if (autoRebuildTries >= 30)
            return;
        autoRebuildTries++;
        EditorApplication.delayCall += TryRebuildLegacyAttackLayer;
    }

    public static void Build()
    {
        var unarmed = AssetDatabase.LoadAssetAtPath<AnimatorController>(UnarmedPath);
        if (unarmed == null)
            throw new InvalidOperationException("Missing Unarmed.controller at " + UnarmedPath);

        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(AvatarMaskPath);
        var baseClips = ExtractBaseClips(unarmed);
        if (baseClips.Loco == null || baseClips.Loco.Length == 0)
            throw new InvalidOperationException("Unarmed.controller has no locomotion blend tree clips.");

        var overrideMaps = LoadOverrideMaps();
        ApplyOneHandSwordRemaps(overrideMaps, baseClips);
        ApplyArmedPlaceholder(overrideMaps);

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(HumanoidPath) != null)
            AssetDatabase.DeleteAsset(HumanoidPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(HumanoidPath);
        AddParameters(controller);
        controller.AddLayer("Attack");
        controller.AddLayer("Hitted");
        controller.AddLayer("Cast");
        ConfigureLayers(controller, mask);

        BuildBaseLayer(controller, baseClips, overrideMaps);
        BuildAttackLayer(controller, baseClips);
        BuildHittedLayer(controller, baseClips.Hit);
        BuildCastLayer(controller, baseClips.Idle);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        RetargetUnitOverrides(controller);
        SeedHumanoidUnarmedSet(controller, baseClips, overrideMaps);
        SeedUnitAnimationSets(baseClips);
        StripUnitAttackRemaps(baseClips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void AddParameters(AnimatorController controller)
    {
        AddFloat(controller, "VelocityX", 0f);
        AddFloat(controller, "VelocityZ", 0f);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackVersion", AnimatorControllerParameterType.Int);
        AddFloat(controller, "AttackTime", 1f);
        controller.AddParameter("Health", AnimatorControllerParameterType.Int);
        controller.AddParameter("Hitted", AnimatorControllerParameterType.Trigger);
        AddFloat(controller, "DeadSpeedMultiplier", 1f);
        controller.AddParameter("Stance", AnimatorControllerParameterType.Int);
        AddFloat(controller, "StanceBlend", 0f);
        controller.AddParameter("IsCasting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Cast", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("CastEnd", AnimatorControllerParameterType.Trigger);
    }

    private static void AddFloat(AnimatorController controller, string name, float defaultValue)
    {
        controller.AddParameter(new AnimatorControllerParameter
        {
            name = name,
            type = AnimatorControllerParameterType.Float,
            defaultFloat = defaultValue,
        });
    }

    private static void ConfigureLayers(AnimatorController controller, AvatarMask mask)
    {
        var layers = controller.layers;
        layers[1].defaultWeight = 1f;
        layers[1].avatarMask = mask;
        layers[2].defaultWeight = 0.46f;
        layers[2].avatarMask = mask;
        layers[2].blendingMode = AnimatorLayerBlendingMode.Additive;
        layers[3].defaultWeight = 1f;
        layers[3].avatarMask = mask;
        controller.layers = layers;
    }

    private static void BuildBaseLayer(
        AnimatorController controller,
        BaseClipSet baseClips,
        Dictionary<AnimationStance, Dictionary<AnimationClip, AnimationClip>> overrideMaps)
    {
        var sm = controller.layers[0].stateMachine;
        ClearStateMachine(sm);

        var movement = sm.AddState("Movement", new Vector3(30, 280, 0));
        var stanceTree = CreateBlendTree(controller, "StanceLocomotion");
        stanceTree.blendType = BlendTreeType.Simple1D;
        stanceTree.blendParameter = "StanceBlend";
        stanceTree.useAutomaticThresholds = false;

        var stanceValues = (AnimationStance[])Enum.GetValues(typeof(AnimationStance));
        var children = new ChildMotion[stanceValues.Length];
        for (int i = 0; i < stanceValues.Length; i++)
        {
            var stance = stanceValues[i];
            var locoTree = CreateBlendTree(controller, "Loco_" + stance);
            locoTree.blendType = BlendTreeType.SimpleDirectional2D;
            locoTree.blendParameter = "VelocityX";
            locoTree.blendParameterY = "VelocityZ";
            locoTree.useAutomaticThresholds = false;

            var locoChildren = new ChildMotion[baseClips.Loco.Length];
            for (int c = 0; c < baseClips.Loco.Length; c++)
            {
                locoChildren[c] = new ChildMotion
                {
                    motion = Remap(baseClips.Loco[c].clip, stance, overrideMaps),
                    timeScale = 1f,
                    position = baseClips.Loco[c].position,
                };
            }

            locoTree.children = locoChildren;
            children[i] = new ChildMotion
            {
                motion = locoTree,
                timeScale = 1f,
                threshold = (int)stance,
            };
        }

        stanceTree.children = children;
        movement.motion = stanceTree;

        var dead = sm.AddState("Dead", new Vector3(270, 160, 0));
        dead.motion = baseClips.Dead;
        dead.speedParameterActive = true;
        dead.speedParameter = "DeadSpeedMultiplier";

        var revive = sm.AddState("Revive", new Vector3(390, 30, 0));
        revive.motion = baseClips.Revive;

        var toDead = sm.AddAnyStateTransition(dead);
        toDead.hasExitTime = false;
        toDead.duration = 0.25f;
        toDead.canTransitionToSelf = false;
        toDead.AddCondition(AnimatorConditionMode.Equals, 0, "Health");

        var deadToRevive = dead.AddTransition(revive);
        deadToRevive.hasExitTime = false;
        deadToRevive.duration = 0.25f;
        deadToRevive.AddCondition(AnimatorConditionMode.Greater, 0, "Health");

        var reviveExit = revive.AddExitTransition();
        reviveExit.hasExitTime = true;
        reviveExit.exitTime = 0.79f;
        reviveExit.duration = 0.25f;

        sm.defaultState = movement;
    }

    private static void BuildAttackLayer(AnimatorController controller, BaseClipSet baseClips)
    {
        var root = controller.layers[1].stateMachine;
        ClearStateMachine(root);

        var none = root.AddState("None", new Vector3(30, 220, 0));
        root.defaultState = none;

        var attack0 = AddAttackState(root, "Attack0", baseClips.Attack0, none, new Vector3(300, 40, 0));
        var attack1 = AddAttackState(root, "Attack1", baseClips.Attack1, none, new Vector3(300, 160, 0));
        AddAttackAnyState(root, attack0, 0);
        AddAttackAnyState(root, attack1, 1);
    }

    private static AnimatorState AddAttackState(
        AnimatorStateMachine sm,
        string name,
        AnimationClip clip,
        AnimatorState none,
        Vector3 position)
    {
        var state = sm.AddState(name, position);
        state.motion = clip;
        state.speedParameterActive = true;
        state.speedParameter = "AttackTime";

        var toNone = state.AddTransition(none);
        toNone.hasExitTime = true;
        toNone.exitTime = 0.6875f;
        toNone.duration = 0.25f;
        toNone.hasFixedDuration = true;
        return state;
    }

    private static void AddAttackAnyState(AnimatorStateMachine root, AnimatorState dest, int attackVersion)
    {
        var transition = root.AddAnyStateTransition(dest);
        transition.hasExitTime = false;
        transition.duration = 0.032f;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = true;
        transition.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        transition.AddCondition(AnimatorConditionMode.Equals, attackVersion, "AttackVersion");
        transition.AddCondition(AnimatorConditionMode.Greater, 0, "Health");
    }

    private static void BuildHittedLayer(AnimatorController controller, AnimationClip hitClip)
    {
        var sm = controller.layers[2].stateMachine;
        ClearStateMachine(sm);

        var none = sm.AddState("None", new Vector3(30, 250, 0));
        var hitted = sm.AddState("Hitted", new Vector3(340, 150, 0));
        hitted.motion = hitClip;

        var toHit = none.AddTransition(hitted);
        toHit.hasExitTime = false;
        toHit.duration = 0.12f;
        toHit.AddCondition(AnimatorConditionMode.If, 0, "Hitted");
        toHit.AddCondition(AnimatorConditionMode.Greater, 0, "Health");

        var back = hitted.AddTransition(none);
        back.hasExitTime = true;
        back.exitTime = 0.5f;
        back.duration = 0.25f;

        sm.defaultState = none;
    }

    private static void BuildCastLayer(AnimatorController controller, AnimationClip placeholder)
    {
        var sm = controller.layers[3].stateMachine;
        ClearStateMachine(sm);

        var none = sm.AddState("None", new Vector3(30, 220, 0));
        var castStart = sm.AddState("CastStart", new Vector3(300, 40, 0));
        var casting = sm.AddState("Casting", new Vector3(300, 160, 0));
        var castEnd = sm.AddState("CastEnd", new Vector3(300, 280, 0));

        castStart.motion = placeholder;
        casting.motion = placeholder;
        castEnd.motion = placeholder;

        var toStart = sm.AddAnyStateTransition(castStart);
        toStart.hasExitTime = false;
        toStart.duration = 0.05f;
        toStart.canTransitionToSelf = false;
        toStart.AddCondition(AnimatorConditionMode.If, 0, "Cast");
        toStart.AddCondition(AnimatorConditionMode.Greater, 0, "Health");

        var startToLoop = castStart.AddTransition(casting);
        startToLoop.hasExitTime = true;
        startToLoop.exitTime = 0.15f;
        startToLoop.duration = 0.08f;

        var loopToEnd = casting.AddTransition(castEnd);
        loopToEnd.hasExitTime = false;
        loopToEnd.duration = 0.08f;
        loopToEnd.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCasting");

        var endToNone = castEnd.AddTransition(none);
        endToNone.hasExitTime = true;
        endToNone.exitTime = 0.2f;
        endToNone.duration = 0.1f;

        sm.defaultState = none;
    }

    private static BlendTree CreateBlendTree(AnimatorController controller, string name)
    {
        var tree = new BlendTree
        {
            name = name,
            hideFlags = HideFlags.HideInHierarchy,
        };
        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    private static void ClearStateMachine(AnimatorStateMachine sm)
    {
        var machines = sm.stateMachines;
        for (int i = machines.Length - 1; i >= 0; i--)
            sm.RemoveStateMachine(machines[i].stateMachine);

        var states = sm.states;
        for (int i = states.Length - 1; i >= 0; i--)
            sm.RemoveState(states[i].state);

        var any = sm.anyStateTransitions;
        for (int i = any.Length - 1; i >= 0; i--)
            sm.RemoveAnyStateTransition(any[i]);
    }

    private static AnimationClip Remap(
        AnimationClip original,
        AnimationStance stance,
        Dictionary<AnimationStance, Dictionary<AnimationClip, AnimationClip>> overrideMaps)
    {
        if (original == null)
            return null;
        if (overrideMaps.TryGetValue(stance, out var map)
            && map.TryGetValue(original, out var mapped)
            && mapped != null)
            return mapped;
        return original;
    }

    private static Dictionary<AnimationStance, Dictionary<AnimationClip, AnimationClip>> LoadOverrideMaps()
    {
        var maps = new Dictionary<AnimationStance, Dictionary<AnimationClip, AnimationClip>>();
        foreach (var (stance, path) in StanceOverrides)
        {
            var map = LoadOverrideMap(path);
            if (map != null)
                maps[stance] = map;
        }

        return maps;
    }

    private static void ApplyOneHandSwordRemaps(
        Dictionary<AnimationStance, Dictionary<AnimationClip, AnimationClip>> overrideMaps,
        BaseClipSet baseClips)
    {
        var swordMap = LoadOverrideMap(OneHandSwordOverridePath);
        if (swordMap == null)
            return;

        if (overrideMaps.TryGetValue(AnimationStance.Daggers, out var daggerMap)
            && baseClips.Attack1 != null
            && daggerMap.TryGetValue(baseClips.Attack1, out var daggerOff)
            && daggerOff != null)
        {
            swordMap[baseClips.Attack1] = daggerOff;
        }

        overrideMaps[AnimationStance.Sword] = swordMap;
    }

    private static void ApplyArmedPlaceholder(
        Dictionary<AnimationStance, Dictionary<AnimationClip, AnimationClip>> overrideMaps)
    {
        if (!overrideMaps.TryGetValue(AnimationStance.Daggers, out var daggerMap) || daggerMap == null)
            return;

        overrideMaps[AnimationStance.Armed] = new Dictionary<AnimationClip, AnimationClip>(daggerMap);
    }

    private static Dictionary<AnimationClip, AnimationClip> LoadOverrideMap(string path)
    {
        var ovr = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
        if (ovr == null)
        {
            Debug.LogWarning("HumanoidAnimatorBuilder: missing override " + path);
            return null;
        }

        var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        ovr.GetOverrides(list);
        var map = new Dictionary<AnimationClip, AnimationClip>();
        foreach (var pair in list)
        {
            if (pair.Key != null && pair.Value != null)
                map[pair.Key] = pair.Value;
        }

        return map;
    }

    private static BaseClipSet ExtractBaseClips(AnimatorController unarmed)
    {
        var set = new BaseClipSet { Loco = Array.Empty<LocoClip>() };
        foreach (var layer in unarmed.layers)
        {
            CollectFromStateMachine(layer.stateMachine, set);
        }

        return set;
    }

    private static void CollectFromStateMachine(AnimatorStateMachine sm, BaseClipSet set)
    {
        foreach (var child in sm.states)
        {
            var state = child.state;
            if (state.name == "Movement" && state.motion is BlendTree tree)
            {
                var children = tree.children;
                set.Loco = new LocoClip[children.Length];
                for (int i = 0; i < children.Length; i++)
                {
                    set.Loco[i] = new LocoClip
                    {
                        clip = children[i].motion as AnimationClip,
                        position = children[i].position,
                    };
                    if (children[i].position == Vector2.zero)
                        set.Idle = set.Loco[i].clip;
                }
            }
            else if (state.name == "Attack0")
                set.Attack0 = state.motion as AnimationClip;
            else if (state.name == "Attack1")
                set.Attack1 = state.motion as AnimationClip;
            else if (state.name == "Dead")
                set.Dead = state.motion as AnimationClip;
            else if (state.name == "Revive")
                set.Revive = state.motion as AnimationClip;
            else if (state.name == "Hitted")
                set.Hit = state.motion as AnimationClip;
        }

        foreach (var child in sm.stateMachines)
            CollectFromStateMachine(child.stateMachine, set);
    }

    private static void RetargetUnitOverrides(AnimatorController humanoid)
    {
        foreach (var (overridePath, _) in UnitSets)
        {
            var ovr = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
            if (ovr == null)
                continue;

            ovr.runtimeAnimatorController = humanoid;
            EditorUtility.SetDirty(ovr);
        }
    }

    private static void SeedHumanoidUnarmedSet(
        AnimatorController humanoid,
        BaseClipSet baseClips,
        Dictionary<AnimationStance, Dictionary<AnimationClip, AnimationClip>> overrideMaps)
    {
        var set = AssetDatabase.LoadAssetAtPath<AnimationSetData>(HumanoidSetPath);
        if (set == null)
            return;

        set.animatorController = humanoid;
        set.fallback = null;
        set.attackMainPlaceholder = baseClips.Attack0;
        set.attackOffPlaceholder = baseClips.Attack1;
        set.weaponAttacks = BuildDefaultWeaponAttacks(baseClips, overrideMaps);
        EditorUtility.SetDirty(set);
    }

    private static AnimationSetData.WeaponAttackEntry[] BuildDefaultWeaponAttacks(
        BaseClipSet baseClips,
        Dictionary<AnimationStance, Dictionary<AnimationClip, AnimationClip>> overrideMaps)
    {
        var weapons = (WeaponType[])Enum.GetValues(typeof(WeaponType));
        var entries = new List<AnimationSetData.WeaponAttackEntry>(weapons.Length);
        for (int i = 0; i < weapons.Length; i++)
        {
            var weapon = weapons[i];
            if (weapon == WeaponType.Shield)
                continue;

            var stance = StanceForWeaponPack(weapon);
            var attack0 = Remap(baseClips.Attack0, stance, overrideMaps);
            var attack1 = Remap(baseClips.Attack1, stance, overrideMaps);
            var entry = new AnimationSetData.WeaponAttackEntry { weapon = weapon };

            if (ItemRules.CanAttackWithOffHand(weapon))
            {
                entry.mainHand = attack0 != null ? new[] { attack0 } : Array.Empty<AnimationClip>();
                entry.offHand = attack1 != null ? new[] { attack1 } : Array.Empty<AnimationClip>();
            }
            else
            {
                entry.mainHand = CollectDistinct(attack0, attack1);
                entry.offHand = Array.Empty<AnimationClip>();
            }

            entries.Add(entry);
        }

        return entries.ToArray();
    }

    private static AnimationStance StanceForWeaponPack(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Sword:
                return AnimationStance.Sword;
            case WeaponType.Daggers:
                return AnimationStance.Daggers;
            case WeaponType.Bow:
                return AnimationStance.Bow;
            case WeaponType.Gun:
                return AnimationStance.Gun;
            case WeaponType.Pistols:
                return AnimationStance.Pistols;
            case WeaponType.Staff:
                return AnimationStance.Staff;
            case WeaponType.TwoHandSword:
                return AnimationStance.TwoHandSword;
            default:
                return AnimationStance.Unarmed;
        }
    }

    private static AnimationClip[] CollectDistinct(AnimationClip first, AnimationClip second)
    {
        if (first == null && second == null)
            return Array.Empty<AnimationClip>();
        if (first == null)
            return new[] { second };
        if (second == null || second == first)
            return new[] { first };
        return new[] { first, second };
    }

    private static void SeedUnitAnimationSets(BaseClipSet baseClips)
    {
        var humanoidSet = AssetDatabase.LoadAssetAtPath<AnimationSetData>(HumanoidSetPath);
        foreach (var (overridePath, setPath) in UnitSets)
        {
            var set = AssetDatabase.LoadAssetAtPath<AnimationSetData>(setPath);
            var ovr = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
            if (set == null || ovr == null)
                continue;

            set.fallback = humanoidSet;
            set.animatorController = ovr;
            set.attackMainPlaceholder = null;
            set.attackOffPlaceholder = null;

            var map = LoadOverrideMap(overridePath) ?? new Dictionary<AnimationClip, AnimationClip>();
            var main = RemapClip(baseClips.Attack0, map);
            var off = RemapClip(baseClips.Attack1, map);
            var hasUnitAttackOverride =
                (main != null && main != baseClips.Attack0)
                || (off != null && off != baseClips.Attack1);

            if (hasUnitAttackOverride || set.weaponAttacks == null || set.weaponAttacks.Length == 0)
            {
                set.weaponAttacks = new[]
                {
                    new AnimationSetData.WeaponAttackEntry
                    {
                        weapon = WeaponType.Unarmed,
                        mainHand = main != null ? new[] { main } : Array.Empty<AnimationClip>(),
                        offHand = off != null ? new[] { off } : Array.Empty<AnimationClip>(),
                    },
                };
            }

            EditorUtility.SetDirty(set);
        }
    }

    private static AnimationClip RemapClip(AnimationClip original, Dictionary<AnimationClip, AnimationClip> map)
    {
        if (original == null)
            return null;
        if (map.TryGetValue(original, out var mapped) && mapped != null)
            return mapped;
        return original;
    }

    private static void StripUnitAttackRemaps(BaseClipSet baseClips)
    {
        foreach (var (overridePath, _) in UnitSets)
        {
            var ovr = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
            if (ovr == null)
                continue;

            var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            ovr.GetOverrides(list);
            var changed = false;
            for (int i = 0; i < list.Count; i++)
            {
                var original = list[i].Key;
                if (original != baseClips.Attack0 && original != baseClips.Attack1)
                    continue;
                if (list[i].Value == null)
                    continue;

                list[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, null);
                changed = true;
            }

            if (!changed)
                continue;

            ovr.ApplyOverrides(list);
            EditorUtility.SetDirty(ovr);
        }
    }

    private struct LocoClip
    {
        public AnimationClip clip;
        public Vector2 position;
    }

    private struct BaseClipSet
    {
        public LocoClip[] Loco;
        public AnimationClip Idle;
        public AnimationClip Attack0;
        public AnimationClip Attack1;
        public AnimationClip Dead;
        public AnimationClip Hit;
        public AnimationClip Revive;
    }
}
