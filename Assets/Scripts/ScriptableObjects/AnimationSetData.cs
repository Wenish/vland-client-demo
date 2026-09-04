using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimationSet", menuName = "Game/Animation/AnimationSet")]
public class AnimationSetData : ScriptableObject
{
    [Tooltip("Shared Humanoid controller, or a sparse AnimatorOverrideController of it.")]
    public RuntimeAnimatorController animatorController;

}