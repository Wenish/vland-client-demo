using UnityEngine;

[CreateAssetMenu(fileName = "NewModel", menuName = "Game/Model/Model")]
public class ModelData : ScriptableObject
{
    public string modelName;
    public GameObject prefab;

    [Header("Animation")]
    [Tooltip("Unit AnimatorOverrideController (sparse clips) or the shared Humanoid controller. Missing clips fall back to Humanoid defaults.")]
    public AnimationSetData defaultAnimationSet;
}
