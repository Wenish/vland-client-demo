using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowInfection.Targeting
{
    public static class UnitPointerQuery
    {
        public const string UnitLayerName = "Unit";

        public static LayerMask UnitLayerMask => LayerMask.GetMask(UnitLayerName);

        public static bool TryGetUnitUnderPointer(Camera camera, out UnitController unit)
        {
            return TryGetUnitUnderPointer(camera, UnitLayerMask, out unit);
        }

        public static bool TryGetUnitUnderPointer(Camera camera, LayerMask unitLayer, out UnitController unit)
        {
            unit = null;
            if (camera == null)
                return false;

            if (!TryGetPointerPosition(out var pointerPosition))
                return false;

            var ray = camera.ScreenPointToRay(pointerPosition);
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, unitLayer))
                return false;

            unit = hit.collider != null
                ? hit.collider.GetComponentInParent<UnitController>()
                : null;
            return unit != null;
        }

        public static bool TryGetPointerPosition(out Vector2 pointerPosition)
        {
            if (Pointer.current != null)
            {
                pointerPosition = Pointer.current.position.ReadValue();
                return true;
            }

            if (Mouse.current != null)
            {
                pointerPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Pen.current != null)
            {
                pointerPosition = Pen.current.position.ReadValue();
                return true;
            }

            pointerPosition = default;
            return false;
        }
    }
}
