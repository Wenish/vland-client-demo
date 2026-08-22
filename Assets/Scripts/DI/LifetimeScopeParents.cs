using VContainer.Unity;

namespace ShadowInfection.DI
{
    internal static class LifetimeScopeParents
    {
        public static LifetimeScope GameplayOrGame()
        {
            var gameplay = GameplayLifetimeScope.FindOrCreate();
            if (gameplay != null)
                return gameplay;

            return GameLifetimeScope.FindOrCreate();
        }

        public static LifetimeScope Game()
        {
            return GameLifetimeScope.FindOrCreate();
        }
    }
}
