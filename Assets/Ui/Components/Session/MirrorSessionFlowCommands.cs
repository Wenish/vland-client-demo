using Mirror;
using ShadowInfection.UI.ZombieMatch;
using UnityEngine;

namespace ShadowInfection.UI.Session
{
    internal sealed class MirrorSessionFlowCommands : ISessionFlowCommands
    {
        private readonly IZombieMatchCommands zombieCommands;

        public MirrorSessionFlowCommands(IZombieMatchCommands zombieCommands)
        {
            this.zombieCommands = zombieCommands;
        }

        public bool TryEndMatch()
        {
            if (!NetworkServer.active)
                return false;

            if (zombieCommands != null && zombieCommands.TryReturnToLobby())
                return true;

            if (NetworkManager.singleton is NetworkRoomManager roomManager)
            {
                roomManager.ServerChangeScene(roomManager.RoomScene);
                return true;
            }

            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.ServerChangeScene(SceneNames.Lobby);
                return true;
            }

            return false;
        }

        public void LeaveClient()
        {
            if (NetworkClient.isConnected && NetworkManager.singleton != null)
                NetworkManager.singleton.StopClient();
        }

        public void StopHost()
        {
            if (NetworkServer.active && NetworkManager.singleton != null)
                NetworkManager.singleton.StopHost();
        }

        public void ExitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
