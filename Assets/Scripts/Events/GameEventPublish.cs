using MessagePipe;

namespace MyGame.Events
{
    internal static class GameEventPublish
    {
        public static void ToMessagePipe<T>(T message)
        {
            if (!GlobalMessagePipe.IsInitialized)
                return;

            GlobalMessagePipe.GetPublisher<T>().Publish(message);
        }

        public static void ToBoth<T>(T message) where T : GameEvent
        {
            if (EventManager.Instance != null)
                EventManager.Instance.Publish(message);

            ToMessagePipe(message);
        }
    }
}
