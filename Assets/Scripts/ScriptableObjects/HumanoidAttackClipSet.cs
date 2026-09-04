using System;
using ShadowInfection.Items;
using UnityEngine;

namespace ShadowInfection.Animations
{
    [CreateAssetMenu(fileName = "HumanoidAttackClips", menuName = "Game/Animation/Humanoid Attack Clips")]
    public sealed class HumanoidAttackClipSet : ScriptableObject
    {
        public const string ResourcesPath = "ScriptableObjects/AnimationSets/HumanoidAttackClips";
        public const string AssetPath = "Assets/Resources/ScriptableObjects/AnimationSets/HumanoidAttackClips.asset";

        [Serializable]
        public sealed class StanceEntry
        {
            public WeaponType stance;

            [Tooltip("Optional extra main-hand clips on top of the default already on Humanoid.controller. Leave empty unless you want more random variants.")]
            public AnimationClip[] extraMainHand = Array.Empty<AnimationClip>();

            [Tooltip("Optional extra off-hand clips. Leave empty unless you want more random variants.")]
            public AnimationClip[] extraOffHand = Array.Empty<AnimationClip>();
        }

        [Tooltip("Optional extra auto-attack variants. Empty is fine — default clips live on Humanoid.controller. After adding extras, run Tools/Animation/Rebuild Humanoid Controller.")]
        public StanceEntry[] stances = Array.Empty<StanceEntry>();

        public AnimationClip[] GetExtraClips(WeaponType stance, bool offHand)
        {
            if (stances == null)
                return Array.Empty<AnimationClip>();

            for (int i = 0; i < stances.Length; i++)
            {
                var entry = stances[i];
                if (entry == null || entry.stance != stance)
                    continue;

                var clips = offHand ? entry.extraOffHand : entry.extraMainHand;
                return clips ?? Array.Empty<AnimationClip>();
            }

            return Array.Empty<AnimationClip>();
        }
    }

    public static class HumanoidAttackVariants
    {
        static HumanoidAttackClipSet cachedClipSet;
        static bool clipSetLoadAttempted;

        public static bool UsesSplitHands(WeaponType stance)
        {
            return ItemRules.CanAttackWithOffHand(stance);
        }

        public static int GetCount(WeaponType stance, bool offHand)
        {
            bool split = UsesSplitHands(stance);
            int baseline = split ? 1 : 2;
            bool extrasAreOffHand = split && offHand;
            return baseline + CountNonNull(GetExtraClips(stance, extrasAreOffHand));
        }

        public static int PickRandom(WeaponType stance, bool offHand)
        {
            int count = GetCount(stance, offHand);
            if (count <= 1)
                return 0;
            return UnityEngine.Random.Range(0, count);
        }

        public static AnimationClip[] GetExtraClips(WeaponType stance, bool offHand)
        {
            var set = LoadClipSet();
            if (set == null)
                return Array.Empty<AnimationClip>();
            return set.GetExtraClips(stance, offHand);
        }

        static HumanoidAttackClipSet LoadClipSet()
        {
            if (clipSetLoadAttempted)
                return cachedClipSet;

            clipSetLoadAttempted = true;
            cachedClipSet = Resources.Load<HumanoidAttackClipSet>(HumanoidAttackClipSet.ResourcesPath);
            return cachedClipSet;
        }

        static int CountNonNull(AnimationClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return 0;

            int count = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    count++;
            }

            return count;
        }
    }
}
