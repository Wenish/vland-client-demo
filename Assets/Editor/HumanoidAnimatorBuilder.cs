using System;
using System.Collections.Generic;
using ShadowInfection.Animations;
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

    private static readonly (WeaponType type, string overridePath)[] StanceOverrides =
    {
        (WeaponType.Daggers, "Assets/Units/Animations/Daggers.overrideController"),
        (WeaponType.Bow, "Assets/Units/Animations/Bow.overrideController"),
        (WeaponType.Gun, "Assets/Units/Animations/Gun.overrideController"),
        (WeaponType.Pistols, "Assets/Units/Animations/Pistols.overrideController"),
        (WeaponType.TwoHandSword, "Assets/Units/Animations/Sword.overrideController"),
    };

    private static readonly string[] UnitOverridePaths =
    {
        "Assets/Units/Animations/ZombieUnarmed.overrideController",
        "Assets/Units/Animations/ZombieCrawlerUnarmed.overrideController",
    };

    [MenuItem("Tools/Animation/Rebuild Humanoid Controller")]
    public static void BuildFromMenu()
    {
        Build();
        Debug.Log("Humanoid animator rebuild finished.");
    }

    public static void Build()
    {
        var unarmed = AssetDatabase.LoadAssetAtPath<AnimatorController>(UnarmedPath);
        if (unarmed == null)
            throw new InvalidOperationException("Missing Unarmed.controller at " + UnarmedPath);

        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(AvatarMaskPath);
        var baseClips = ExtractBaseClips(unarmed);
        if (baseClips.Loco.Length == 0)
            throw new InvalidOperationException("Unarmed.controller has no locomotion blend tree clips.");

        var overrideMaps = LoadOverrideMaps();
        ApplyOneHandSwordRemaps(overrideMaps, baseClips);

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(HumanoidPath) != null)
            AssetDatabase.DeleteAsset(HumanoidPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(HumanoidPath);
        AddParameters(controller);
        controller.AddLayer("Attack");
        controller.AddLayer("Hitted");
        controller.AddLayer("Cast");
        ConfigureLayers(controller, mask);

        BuildBaseLayer(controller, baseClips, overrideMaps);
        BuildAttackLayer(controller, baseClips, overrideMaps);
        BuildHittedLayer(controller, baseClips.Hit);
        BuildCastLayer(controller, baseClips.Idle);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        RetargetUnitOverrides(controller);
        PointDefaultSetAtHumanoid(controller);
        ClearWeaponAnimationOverridesOnModels();
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
        Dictionary<WeaponType, Dictionary<AnimationClip, AnimationClip>> overrideMaps)
    {
        var sm = controller.layers[0].stateMachine;
        ClearStateMachine(sm);

        var movement = sm.AddState("Movement", new Vector3(30, 280, 0));
        var stanceTree = CreateBlendTree(controller, "StanceLocomotion");
        stanceTree.blendType = BlendTreeType.Simple1D;
        stanceTree.blendParameter = "StanceBlend";
        stanceTree.useAutomaticThresholds = false;

        var stanceValues = (WeaponType[])Enum.GetValues(typeof(WeaponType));
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

    private static void BuildAttackLayer(
        AnimatorController controller,
        BaseClipSet baseClips,
        Dictionary<WeaponType, Dictionary<AnimationClip, AnimationClip>> overrideMaps)
    {
        var root = controller.layers[1].stateMachine;
        ClearStateMachine(root);

        var none = root.AddState("None", new Vector3(30, 220, 0));
        root.defaultState = none;

        var clipSet = AssetDatabase.LoadAssetAtPath<HumanoidAttackClipSet>(HumanoidAttackClipSet.AssetPath);
        var stanceValues = (WeaponType[])Enum.GetValues(typeof(WeaponType));
        for (int i = 0; i < stanceValues.Length; i++)
        {
            var stance = stanceValues[i];
            if (HumanoidAttackVariants.UsesSplitHands(stance))
            {
                var mainClips = CollectAttackClips(
                    Remap(baseClips.Attack0, stance, overrideMaps),
                    GetExtraClips(clipSet, stance, false));
                var offClips = CollectAttackClips(
                    Remap(baseClips.Attack1, stance, overrideMaps),
                    GetExtraClips(clipSet, stance, true));

                AddAttackVariantStates(root, none, stance, mainClips, stance + "_Main_", 0, 1);
                AddAttackVariantStates(root, none, stance, offClips, stance + "_Off_", 1, 1);
            }
            else
            {
                var pool = CollectAttackClips(
                    Remap(baseClips.Attack0, stance, overrideMaps),
                    Remap(baseClips.Attack1, stance, overrideMaps),
                    GetExtraClips(clipSet, stance, false));
                AddAttackVariantStates(root, none, stance, pool, stance + "_", 0);
            }
        }
    }

    private static AnimationClip[] GetExtraClips(
        HumanoidAttackClipSet clipSet,
        WeaponType stance,
        bool offHand)
    {
        if (clipSet == null)
            return Array.Empty<AnimationClip>();
        return clipSet.GetExtraClips(stance, offHand);
    }

    private static void AddAttackVariantStates(
        AnimatorStateMachine root,
        AnimatorState none,
        WeaponType stance,
        AnimationClip[] clips,
        string namePrefix,
        int versionStart,
        int maxClips = int.MaxValue)
    {
        int count = Mathf.Min(clips.Length, maxClips);
        for (int i = 0; i < count; i++)
        {
            var state = AddAttackState(root, namePrefix + i, clips[i], none, i);
            AddAttackAnyState(root, state, stance, versionStart + i);
        }
    }

    private static AnimationClip[] CollectAttackClips(AnimationClip baseline, AnimationClip[] extras)
    {
        var clips = new List<AnimationClip> { baseline };
        AppendNonNull(clips, extras);
        return clips.ToArray();
    }

    private static AnimationClip[] CollectAttackClips(
        AnimationClip first,
        AnimationClip second,
        AnimationClip[] extras)
    {
        var clips = new List<AnimationClip> { first, second };
        AppendNonNull(clips, extras);
        return clips.ToArray();
    }

    private static void AppendNonNull(List<AnimationClip> clips, AnimationClip[] extras)
    {
        if (extras == null)
            return;

        for (int i = 0; i < extras.Length; i++)
        {
            if (extras[i] != null)
                clips.Add(extras[i]);
        }
    }

    private static AnimatorState AddAttackState(
        AnimatorStateMachine sm,
        string name,
        AnimationClip clip,
        AnimatorState none,
        int variantIndex)
    {
        var state = sm.AddState(name, new Vector3(300, 40 + variantIndex * 120, 0));
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

    private static void AddAttackAnyState(
        AnimatorStateMachine root,
        AnimatorState dest,
        WeaponType stance,
        int attackVersion)
    {
        var transition = root.AddAnyStateTransition(dest);
        transition.hasExitTime = false;
        transition.duration = 0.032f;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = true;
        transition.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        transition.AddCondition(AnimatorConditionMode.Equals, attackVersion, "AttackVersion");
        transition.AddCondition(AnimatorConditionMode.Equals, (int)stance, "Stance");
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
        WeaponType stance,
        Dictionary<WeaponType, Dictionary<AnimationClip, AnimationClip>> overrideMaps)
    {
        if (original == null)
            return null;
        if (overrideMaps.TryGetValue(stance, out var map)
            && map.TryGetValue(original, out var mapped)
            && mapped != null)
            return mapped;
        return original;
    }

    private static Dictionary<WeaponType, Dictionary<AnimationClip, AnimationClip>> LoadOverrideMaps()
    {
        var maps = new Dictionary<WeaponType, Dictionary<AnimationClip, AnimationClip>>();
        foreach (var (type, path) in StanceOverrides)
        {
            var ovr = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (ovr == null)
            {
                Debug.LogWarning("HumanoidAnimatorBuilder: missing override " + path);
                continue;
            }

            var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            ovr.GetOverrides(list);
            var map = new Dictionary<AnimationClip, AnimationClip>();
            foreach (var pair in list)
            {
                if (pair.Key != null && pair.Value != null)
                    map[pair.Key] = pair.Value;
            }

            maps[type] = map;
        }

        return maps;
    }

    private static void ApplyOneHandSwordRemaps(
        Dictionary<WeaponType, Dictionary<AnimationClip, AnimationClip>> overrideMaps,
        BaseClipSet baseClips)
    {
        var sasMap = LoadOverrideMap(OneHandSwordOverridePath);
        if (sasMap == null)
            return;

        var swordMap = new Dictionary<AnimationClip, AnimationClip>(sasMap);
        if (overrideMaps.TryGetValue(WeaponType.Daggers, out var daggerMap)
            && baseClips.Attack1 != null
            && daggerMap.TryGetValue(baseClips.Attack1, out var daggerOff)
            && daggerOff != null)
        {
            swordMap[baseClips.Attack1] = daggerOff;
        }

        overrideMaps[WeaponType.Sword] = swordMap;
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
        var set = new BaseClipSet();
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
        foreach (var path in UnitOverridePaths)
        {
            var ovr = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (ovr == null)
                continue;

            ovr.runtimeAnimatorController = humanoid;
            EditorUtility.SetDirty(ovr);
        }
    }

    private static void PointDefaultSetAtHumanoid(AnimatorController humanoid)
    {
        var set = AssetDatabase.LoadAssetAtPath<AnimationSetData>(HumanoidSetPath);
        if (set == null)
            return;

        set.animatorController = humanoid;
        EditorUtility.SetDirty(set);
    }

    private static void ClearWeaponAnimationOverridesOnModels()
    {
        var guids = AssetDatabase.FindAssets("t:ModelData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var model = AssetDatabase.LoadAssetAtPath<ModelData>(path);
            if (model == null)
                continue;

            EditorUtility.SetDirty(model);
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
