namespace ShadowInfection.UI.Nameplates
{
    public sealed class UnitNameplateVisibilityState
    {
        public bool ShowRoot = true;
        public bool ShowHealth;
        public bool ShowShield;
        public bool ShowName => ShowHealth;
    }

    public static class UnitNameplateVisibilityPolicy
    {
        public static void ApplyInit(UnitController unit, UnitNameplateVisibilityState state)
        {
            if (unit == null || state == null)
                return;

            state.ShowRoot = unit.health > 0;

            if (unit.maxHealth == 0)
            {
                state.ShowHealth = false;
            }
            else if (unit.health < unit.maxHealth)
            {
                state.ShowHealth = true;
            }
            else
            {
                state.ShowHealth = unit.unitType == UnitType.Player;
            }

            if (unit.maxShield == 0)
            {
                state.ShowShield = false;
            }
            else if (unit.shield < unit.maxShield)
            {
                state.ShowShield = true;
            }
            else
            {
                state.ShowShield = unit.unitType == UnitType.Player;
            }

            var isPlayer = unit.unitType == UnitType.Player;
            var isFullHealth = unit.health == unit.maxHealth;
            var isFullShield = unit.shield == unit.maxShield;
            if (!isPlayer && isFullHealth && isFullShield)
            {
                state.ShowHealth = false;
                state.ShowShield = false;
            }
        }

        public static void OnHealthChange(UnitController unit, (int current, int max) health, UnitNameplateVisibilityState state)
        {
            if (unit == null || state == null)
                return;

            if (health.max == 0)
            {
                state.ShowHealth = false;
                return;
            }

            if (health.current < health.max)
            {
                state.ShowHealth = true;
                if (unit.maxShield > 0)
                    state.ShowShield = true;
                return;
            }

            if (health.current == health.max && unit.unitType != UnitType.Player)
                state.ShowHealth = false;
        }

        public static void OnShieldChange(UnitController unit, (int current, int max) shield, UnitNameplateVisibilityState state)
        {
            if (unit == null || state == null)
                return;

            if (shield.max == 0)
            {
                state.ShowShield = false;
                return;
            }

            if (shield.current < shield.max)
            {
                state.ShowShield = true;
                if (unit.maxHealth > 0)
                    state.ShowHealth = true;
                return;
            }

            if (shield.current == shield.max && unit.unitType != UnitType.Player)
                state.ShowShield = false;
        }

        public static void OnDied(UnitNameplateVisibilityState state)
        {
            if (state == null)
                return;

            state.ShowRoot = false;
        }

        public static void OnRevive(UnitController unit, UnitNameplateVisibilityState state)
        {
            if (state == null)
                return;

            state.ShowRoot = true;
            ApplyInit(unit, state);
        }
    }
}
