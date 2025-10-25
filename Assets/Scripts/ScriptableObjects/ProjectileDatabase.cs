using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileDatabase", menuName = "Game/Projectile/Database")]
public class ProjectileDatabase : ScriptableObject
{
    public List<ProjectileData> allProjectiles = new List<ProjectileData>();

    public ProjectileData GetProjectileByName(string name)
    {
        return allProjectiles.Find(projectile => projectile.projectileName == name);
    }
}