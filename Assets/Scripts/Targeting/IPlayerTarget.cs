using UnityEngine;

namespace ShadowInfection.Targeting
{
    public readonly struct PlayerTargetSnapshot
    {
        public PlayerTargetSnapshot(
            UnitController unit,
            string displayName,
            int health,
            int maxHealth,
            int shield,
            int maxShield,
            int team,
            bool isSelf,
            bool isDead)
        {
            Unit = unit;
            DisplayName = displayName ?? string.Empty;
            Health = health;
            MaxHealth = maxHealth;
            Shield = shield;
            MaxShield = maxShield;
            Team = team;
            IsSelf = isSelf;
            IsDead = isDead;
        }

        public UnitController Unit { get; }
        public string DisplayName { get; }
        public int Health { get; }
        public int MaxHealth { get; }
        public int Shield { get; }
        public int MaxShield { get; }
        public int Team { get; }
        public bool IsSelf { get; }
        public bool IsDead { get; }
    }

    public interface IPlayerTarget
    {
        UnitController Current { get; }

        bool HasTarget { get; }

        void Set(UnitController unit);

        void Clear();

        bool TryGetSnapshot(out PlayerTargetSnapshot snapshot);
    }
}
