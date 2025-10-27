using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Game/Projectile/Projectile")]
public class ProjectileData : ScriptableObject
{
    public string projectileName;
    public GameObject prefabBody;
    public GameObject prefabTrail;
    public GameObject prefabImpactDefault;
    public GameObject prefabDespawnPoof;
    public GameObject prefabMuzzleFlash;

    public int maxHits = 1;
    public float speed = 10.0f;
    public float range = 20.0f;
    [Tooltip("Delay before destroying the projectile after it reached max hits. Body is hidden during this time but trail stays visible.")]
    public float destroyDelayAfterMaxHits = 0.35f;
}