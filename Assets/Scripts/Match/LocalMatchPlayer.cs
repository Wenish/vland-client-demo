using Mirror;
using UnityEngine;

namespace ShadowInfection.Match
{
    internal static class LocalMatchPlayer
    {
        public static int ResolveLocalTeamId()
        {
            return TryGetLocalUnit(out var unit) ? unit.team : -1;
        }

        public static bool TryGetLocalUnit(out UnitController unit)
        {
            unit = null;
            if (!TryGetLocalPlayerInput(out var input) || input.myUnit == null)
                return false;

            unit = input.myUnit.GetComponent<UnitController>();
            return unit != null;
        }

        public static bool TryChooseLocalTeam(int teamId)
        {
            if (!TryGetLocalPlayerLoadout(out var loadout))
                return false;

            loadout.RequestChooseTeam(teamId);
            return true;
        }

        private static bool TryGetLocalPlayerInput(out PlayerInput input)
        {
            input = GetLocalPlayerComponent<PlayerInput>();
            return input != null;
        }

        private static bool TryGetLocalPlayerLoadout(out PlayerLoadout loadout)
        {
            loadout = GetLocalPlayerComponent<PlayerLoadout>();
            return loadout != null && loadout.isLocalPlayer;
        }

        private static T GetLocalPlayerComponent<T>() where T : Component
        {
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer != null)
            {
                var fromIdentity = localPlayer.GetComponent<T>();
                if (fromIdentity != null)
                    return fromIdentity;
            }

            var found = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] is NetworkBehaviour behaviour && behaviour.isLocalPlayer)
                    return found[i];
            }

            return null;
        }
    }
}
