using ShadowInfection.Items;

namespace ShadowInfection.DI
{
    public interface IGameDatabases
    {
        WeaponDatabase Weapons { get; }
        SkillDatabase Skills { get; }
        UnitDatabase Units { get; }
        ModelDatabase Models { get; }
        ProjectileDatabase Projectiles { get; }
        AreaZoneDatabase AreaZones { get; }
        ItemDatabase Items { get; }
    }

    public sealed class GameDatabases : IGameDatabases
    {
        public GameDatabases(
            WeaponDatabase weapons,
            SkillDatabase skills,
            UnitDatabase units,
            ModelDatabase models,
            ProjectileDatabase projectiles,
            AreaZoneDatabase areaZones,
            ItemDatabase items)
        {
            Weapons = weapons;
            Skills = skills;
            Units = units;
            Models = models;
            Projectiles = projectiles;
            AreaZones = areaZones;
            Items = items;
        }

        public WeaponDatabase Weapons { get; }
        public SkillDatabase Skills { get; }
        public UnitDatabase Units { get; }
        public ModelDatabase Models { get; }
        public ProjectileDatabase Projectiles { get; }
        public AreaZoneDatabase AreaZones { get; }
        public ItemDatabase Items { get; }
    }
}
