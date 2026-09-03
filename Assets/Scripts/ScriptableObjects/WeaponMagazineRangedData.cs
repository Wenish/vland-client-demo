using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponMagazineRanged", menuName = "Game/Weapon/Ranged Magazine")]
public class WeaponMagazineRangedData : WeaponRangedData
{
    [Header("Magazine")]
    public int magazineSize = 3;
    public float reloadTime = 1f;
    public float idleReloadDelay = 0.75f;
}
