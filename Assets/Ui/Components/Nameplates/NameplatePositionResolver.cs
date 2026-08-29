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
            float headAnchorHeight,
            VisualElement container,
            Camera camera,
            out Vector2 panelPosition)
        {
            panelPosition = default;

            if (unit == null || container == null || camera == null)
                return false;

            var panel = container.panel;
            if (panel == null)
                return false;

            var worldPoint = unit.transform.position + Vector3.up * headAnchorHeight;
            var viewport = camera.WorldToViewportPoint(worldPoint);
            if (viewport.z <= 0f)
                return false;

            panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(panel, worldPoint, camera);
            panelPosition.y -= settings.ScreenOffsetPixels;
            return true;
        }

        public static float ComputeHeadAnchorHeight(UnitController unit, float headWorldOffset)
        {
            var height = headWorldOffset;
            if (unit == null)
                return height + 2f;

            var collider = unit.GetComponent<Collider>();
            if (collider is CapsuleCollider capsule)
                height += capsule.center.y + capsule.height * 0.5f;
            else if (collider != null)
                height += collider.bounds.size.y;
            else
                height += 2f;

            return height;
        }
    }
}
