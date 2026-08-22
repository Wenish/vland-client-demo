using System;
using MessagePipe;

namespace MyGame.Events
{
    public static class GameMessages
    {
        public static void Publish<T>(T message)
        {
            if (!GlobalMessagePipe.IsInitialized)
                return;

            GlobalMessagePipe.GetPublisher<T>().Publish(message);
        }

        public static void Subscribe<T>(ref R3.DisposableBag bag, Action<T> handler)
        {
            if (!GlobalMessagePipe.IsInitialized)
                return;

            bag.Add(GlobalMessagePipe.GetSubscriber<T>().Subscribe(handler));
        }
    }
}
