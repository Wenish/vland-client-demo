using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimationSet", menuName = "Game/Animation/AnimationSet")]
public class AnimationSetData : ScriptableObject
{
    [Tooltip("Shared Humanoid controller, or a sparse AnimatorOverrideController of it.")]
    public RuntimeAnimatorController animatorController;

    [Tooltip("When a weapon has no clips here, use this set. Zombie overrides Unarmed attacks and falls back to Humanoid for other weapons.")]
    public AnimationSetData fallback;

    [Tooltip("Placeholder clip on Humanoid Attack0. Runtime swaps this for the chosen attack.")]
    public AnimationClip attackMainPlaceholder;

    [Tooltip("Placeholder clip on Humanoid Attack1. Runtime swaps this for the chosen off-hand attack.")]
    public AnimationClip attackOffPlaceholder;

    public WeaponAttackEntry[] weaponAttacks = Array.Empty<WeaponAttackEntry>();

    [Serializable]
    public class WeaponAttackEntry
    {
        public WeaponType weapon;
        public AnimationClip[] mainHand = Array.Empty<AnimationClip>();
        public AnimationClip[] offHand = Array.Empty<AnimationClip>();
    }

    public bool TryGetAttackPlaceholders(out AnimationClip main, out AnimationClip off)
    {
        if (attackMainPlaceholder != null && attackOffPlaceholder != null)
        {
            main = attackMainPlaceholder;
            off = attackOffPlaceholder;
            return true;
        }

        if (fallback != null && fallback != this)
            return fallback.TryGetAttackPlaceholders(out main, out off);

        main = null;
        off = null;
        return false;
    }

    public AnimationClip[] GetAttackClips(WeaponType weapon, bool offHand)
    {
        var clips = FindLocalClips(weapon, offHand);
        if (HasClip(clips))
            return clips;

        if (fallback != null && fallback != this)
            return fallback.GetAttackClips(weapon, offHand);

        return Array.Empty<AnimationClip>();
    }

    public AnimationClip PickAttackClip(WeaponType weapon, bool offHand)
    {
        var clips = GetAttackClips(weapon, offHand);
        int count = CountClips(clips);
        if (count == 0)
            return null;

        int pick = UnityEngine.Random.Range(0, count);
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
                continue;
            if (pick == 0)
                return clips[i];
            pick--;
        }

        return null;
    }

    AnimationClip[] FindLocalClips(WeaponType weapon, bool offHand)
    {
        if (weaponAttacks == null)
            return Array.Empty<AnimationClip>();

        for (int i = 0; i < weaponAttacks.Length; i++)
        {
            var entry = weaponAttacks[i];
            if (entry == null || entry.weapon != weapon)
                continue;

            return offHand
                ? entry.offHand ?? Array.Empty<AnimationClip>()
                : entry.mainHand ?? Array.Empty<AnimationClip>();
        }

        return Array.Empty<AnimationClip>();
    }

    static bool HasClip(AnimationClip[] clips)
    {
        return CountClips(clips) > 0;
    }

    static int CountClips(AnimationClip[] clips)
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
