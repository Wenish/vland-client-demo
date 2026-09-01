using System.Collections.Generic;

namespace ShadowInfection.UI
{
    public sealed class UiOverlayRegistry : IUiOverlayRegistry
    {
        private readonly List<IUiOverlay> overlays = new();

        public bool HasAnyOpen
        {
            get
            {
                for (var i = 0; i < overlays.Count; i++)
                {
                    var overlay = overlays[i];
                    if (overlay != null && overlay.IsOpen)
                        return true;
                }

                return false;
            }
        }

        public void Register(IUiOverlay overlay)
        {
            if (overlay == null || overlays.Contains(overlay))
                return;

            overlays.Add(overlay);
        }

        public void Unregister(IUiOverlay overlay)
        {
            if (overlay == null)
                return;

            overlays.Remove(overlay);
        }

        public bool TryCloseAll()
        {
            var closedAny = false;
            for (var i = overlays.Count - 1; i >= 0; i--)
            {
                var overlay = overlays[i];
                if (overlay == null || !overlay.IsOpen)
                    continue;

                overlay.Close();
                closedAny = true;
            }

            return closedAny;
        }
    }
}
