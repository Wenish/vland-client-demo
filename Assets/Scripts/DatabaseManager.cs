using UnityEngine;

// Scene component kept so existing maps do not show missing scripts.
// Databases are registered on GameLifetimeScope as IGameDatabases.
public class DatabaseManager : MonoBehaviour
{
    public WeaponDatabase weaponDatabase;
    public ModelDatabase modelDatabase;
    public UnitDatabase unitDatabase;
    public SkillDatabase skillDatabase;
    public ProjectileDatabase projectileDatabase;
    public AreaZoneDatabase areaZoneDatabase;
}
