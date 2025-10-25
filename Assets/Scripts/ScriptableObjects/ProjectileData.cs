using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Game/Projectile/Projectile")]
public class ProjectileData : ScriptableObject
{
    public string projectileName;
    public GameObject prefab;
    public int maxHits = 1;

    public float speed = 10.0f;
    public float range = 20.0f;
}