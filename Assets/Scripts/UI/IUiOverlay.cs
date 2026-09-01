namespace ShadowInfection.UI
{
    public interface IUiOverlay
    {
        bool IsOpen { get; }

        void Close();
    }
}
