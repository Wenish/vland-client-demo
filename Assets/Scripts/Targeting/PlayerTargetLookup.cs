using Mirror;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.Targeting
{
    public static class PlayerTargetLookup
    {
        public static UnitController CurrentOrNull()
        {
            if (GameplayLifetimeScope.TryResolve<IPlayerTarget>(out var target) && target != null)
                return target.Current;

            return null;
        }

        public static uint CurrentNetId()
        {
            var unit = CurrentOrNull();
            return unit != null ? unit.netId : 0u;
        }

        public static UnitController FromNetId(uint netId)
        {
            if (netId == 0)
                return null;

            if (NetworkServer.spawned.TryGetValue(netId, out var identity) && identity != null)
                return identity.GetComponent<UnitController>();

            return null;
        }
    }
}
