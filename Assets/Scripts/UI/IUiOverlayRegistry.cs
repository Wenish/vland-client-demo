namespace ShadowInfection.UI
{
    public interface IUiOverlayRegistry
    {
        bool HasAnyOpen { get; }

        void Register(IUiOverlay overlay);

        void Unregister(IUiOverlay overlay);

        /// <summary>
        /// Closes every registered overlay that is open.
        /// Returns true if at least one was open.
        /// </summary>
        bool TryCloseAll();
    }
}
