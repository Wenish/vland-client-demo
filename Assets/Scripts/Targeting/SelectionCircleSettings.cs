using UnityEngine;

namespace ShadowInfection.Targeting
{
    [CreateAssetMenu(
        fileName = "SelectionCircleSettings",
        menuName = "Game/Targeting/Selection Circle")]
    public sealed class SelectionCircleSettings : ScriptableObject
    {
        [SerializeField]
        private GameObject prefab;

        [SerializeField]
        private Texture2D texture;

        [SerializeField]
        private Material material;

        [SerializeField]
        [Min(0.05f)]
        private float radius = 0.8f;

        [SerializeField]
        [Tooltip("When enabled, radius grows with the unit's collider extents so bosses get a bigger ring.")]
        private bool scaleWithCollider = true;

        [SerializeField]
        [Min(0.1f)]
        private float colliderRadiusMultiplier = 1.15f;

        [SerializeField]
        private float heightOffset = 0.05f;

        [SerializeField]
        private Color selfColor = new Color(0.15f, 0.85f, 0.35f, 1f);

        [SerializeField]
        private Color allyColor = new Color(0.25f, 0.55f, 1f, 1f);

        [SerializeField]
        private Color enemyColor = new Color(0.9f, 0.2f, 0.15f, 1f);

        [SerializeField]
        [Range(0f, 1f)]
        private float opacity = 0.75f;

        public GameObject Prefab => prefab;
        public Texture2D Texture => texture;
        public Material Material => material;
        public float Radius => radius;
        public bool ScaleWithCollider => scaleWithCollider;
        public float ColliderRadiusMultiplier => colliderRadiusMultiplier;
        public float HeightOffset => heightOffset;
        public Color SelfColor => selfColor;
        public Color AllyColor => allyColor;
        public Color EnemyColor => enemyColor;
        public float Opacity => opacity;
    }
}
