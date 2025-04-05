using System.Collections.Generic;
using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Game/Projectile")]
public class ProjectileData : ScriptableObject
{
    public GameObject prefab;
    public int maxHits = 1;
}