using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.Nameplates
{
    public sealed class NameplatePositionResolver
    {
        private readonly NameplateLayerSettings settings;

        public NameplatePositionResolver(NameplateLayerSettings settings)
        {
            this.settings = settings;
        }

        public bool TryResolve(
            UnitController unit,
            float colliderAnchorHeight,
            VisualElement container,
            Camera camera,
            out Vector2 panelPosition)
        {
            panelPosition = default;

            if (unit == null || container == null || camera == null || settings == null)
                return false;

            var panel = container.panel;
            if (panel == null)
                return false;

            var worldPoint = unit.transform.position
                + Vector3.up * (colliderAnchorHeight + settings.HeadWorldOffset);
            var viewport = camera.WorldToViewportPoint(worldPoint);
            if (viewport.z <= 0f)
                return false;

            panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(panel, worldPoint, camera);
            var t = Mathf.Clamp01(viewport.y);
            var screenOffset = Mathf.Lerp(settings.ScreenOffsetPixels, settings.TopScreenOffsetPixels, t);
            panelPosition.y -= screenOffset;
            return true;
        }

        public static float ComputeColliderAnchorHeight(UnitController unit)
        {
            if (unit == null)
                return 2f;

            var collider = unit.GetComponent<Collider>();
            if (collider is CapsuleCollider capsule)
                return capsule.center.y + capsule.height * 0.5f;
            if (collider != null)
                return collider.bounds.size.y;
            return 2f;
        }
    }
}
