using MyGame.Events;

namespace ShadowInfection.Zombie
{
    public interface IZombieRunEndHost
    {
        bool IsServer { get; }
        bool IsServerOnly { get; }
        bool IsGameOver { get; }
        bool ReturnToLobbyRequested { get; set; }
        void SetGameOver(bool value);
        void StopWaves();
        void SetAutoReturnEnabled(bool value);
        void SetReturnCountdown(float seconds);
        bool ChangeToRoomScene();
        void PublishGameOver(bool isGameOver);
        void PublishRunEnded(ZombieRunEndReason reason);
    }
}
