using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Controllers
{
    public class ControllerCamera : MonoBehaviour
    {
        public Vector3 OffsetCamera;
        public float SpeedCamera = 3f;
        public float BorderThickness = 10f;
        public bool IsFocusingPlayer = false;
        public Transform CameraTarget;
        public float ScrollSpeed = 20f;
        public float MinCameraDistance = 3f;
        public float MaxCameraDistance = 120f;
        public float MouseOffsetInfluenceZ = 10f;
        public float MouseOffsetInfluenceX = 20f;

        public float Zoom = 1f;

        Vector3 mousePositionRelativeToCenterOfScreen = Vector3.zero;

        public event Action<float> OnZoomChange = delegate { };

        // Spectator mode state
        public bool IsSpectating { get; private set; } = false;
        private Transform _spectateTarget;
        private int _spectateIndex = -1;

        private void RaiseOnZoomChangeEvent()
        {
            OnZoomChange(Zoom);
        }

        void Start()
        {
            RaiseOnZoomChangeEvent();
        }

        void Update()
        {
            // Change Camera Type
            if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
            {
                IsFocusingPlayer = !IsFocusingPlayer;
            }

            if (IsFocusingPlayer)
            {
                // Lookg for target to focusing
            }

            UpdateSpectatorMode();
            OnScroll();
            MousePositionChange();
        }

        /// <summary>
        /// Returns true when the local player has no valid, alive unit to control.
        /// </summary>
        private bool ShouldSpectate()
        {
            if (CameraTarget == null) return true;
            var unit = CameraTarget.GetComponent<UnitController>();
            return unit != null && unit.IsDead;
        }

        /// <summary>
        /// Collects all alive player units currently in the game.
        /// </summary>
        private List<UnitController> GetAlivePlayerUnits()
        {
            var result = new List<UnitController>();
            if (PlayerUnitsManager.Instance == null) return result;

            foreach (var pu in PlayerUnitsManager.Instance.playerUnits)
            {
                if (pu.Unit == null) continue;
                var uc = pu.Unit.GetComponent<UnitController>();
                if (uc == null || uc.IsDead) continue;
                result.Add(uc);
            }
            return result;
        }

        private void UpdateSpectatorMode()
        {
            bool shouldSpectate = ShouldSpectate();

            if (shouldSpectate)
            {
                if (!IsSpectating)
                {
                    EnterSpectatorMode();
                }
                HandleSpectatorInput();
                ValidateSpectateTarget();
            }
            else if (IsSpectating)
            {
                ExitSpectatorMode();
            }
        }

        private void EnterSpectatorMode()
        {
            IsSpectating = true;
            IsFocusingPlayer = true;
            _spectateIndex = -1;
            _spectateTarget = null;
            CycleSpectateTarget(1);
        }

        private void ExitSpectatorMode()
        {
            IsSpectating = false;
            _spectateTarget = null;
            _spectateIndex = -1;
        }

        private void HandleSpectatorInput()
        {
            if (UiPointerState.IsPointerOverBlockingElement) return;
            if (Mouse.current == null) return;

            // Right click = next, Left click = previous
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CycleSpectateTarget(1);
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                CycleSpectateTarget(-1);
            }
        }

        private void CycleSpectateTarget(int direction)
        {
            var units = GetAlivePlayerUnits();
            if (units.Count == 0)
            {
                _spectateTarget = null;
                _spectateIndex = -1;
                return;
            }

            _spectateIndex += direction;

            // Wrap around
            if (_spectateIndex >= units.Count) _spectateIndex = 0;
            if (_spectateIndex < 0) _spectateIndex = units.Count - 1;

            _spectateTarget = units[_spectateIndex].transform;
        }

        /// <summary>
        /// If the current spectate target died or was destroyed, auto-cycle to next.
        /// </summary>
        private void ValidateSpectateTarget()
        {
            if (_spectateTarget == null)
            {
                CycleSpectateTarget(1);
                return;
            }
            var uc = _spectateTarget.GetComponent<UnitController>();
            if (uc != null && uc.IsDead)
            {
                CycleSpectateTarget(1);
            }
        }

        /// <summary>
        /// Returns the effective target the camera should follow (spectate target or normal CameraTarget).
        /// </summary>
        private Transform GetEffectiveTarget()
        {
            if (IsSpectating && _spectateTarget != null)
                return _spectateTarget;
            return CameraTarget;
        }

        // Centralized helper to read mouse position via the new Input System
        private Vector2 GetMousePosition()
        {
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            // Fallback to screen center if no mouse present (e.g., gamepad only)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        // Centralized helper to read scroll delta via the new Input System
        // Normalized approximately to legacy axis behavior by dividing by 120f per notch on many platforms
        private float GetScrollDelta()
        {
            if (Mouse.current != null)
            {
                var scroll = Mouse.current.scroll.ReadValue();
                return scroll.y;
            }
            return 0f;
        }

        void MousePositionChange()
        {
            Vector2 mousePos = GetMousePosition();
            mousePositionRelativeToCenterOfScreen.x = Mathf.Clamp((mousePos.x - Screen.width / 2f) / Screen.width, -0.5f, 0.5f);
            mousePositionRelativeToCenterOfScreen.y = Mathf.Clamp((mousePos.y - Screen.height / 2f) / Screen.height, -0.5f, 0.5f);
        }

        void OnScroll()
        {
            var isPointerOverUi = UiPointerState.IsPointerOverBlockingElement;
            if (isPointerOverUi) return;
            float oldZoom = Zoom;
            float scroll = GetScrollDelta();
            Zoom += -scroll * 100 * Time.deltaTime;
            float newZoom = Mathf.Clamp(Zoom, 0.3f, 1f);
            Zoom = newZoom;
            if (oldZoom != newZoom)
            {
                RaiseOnZoomChangeEvent();
            }
        }

        void LateUpdate()
        {
            if (IsFocusingPlayer)
            {
                var target = GetEffectiveTarget();
                if (target != null)
                {
                    Vector3 offset = OffsetCamera;
                    offset.z = offset.z + (mousePositionRelativeToCenterOfScreen.y * MouseOffsetInfluenceZ);
                    offset.x = offset.x + (mousePositionRelativeToCenterOfScreen.x * MouseOffsetInfluenceX);
                    offset = offset * Zoom;
                    var desiredPosition = target.position + offset;
                    var t = Time.deltaTime * SpeedCamera;
                    transform.position = Vector3.Lerp(transform.position, desiredPosition, t);

                    //transform.LookAt(GameManager.CameraTarget);
                }
            }
            else
            {
                var isPointerOverUi = UiPointerState.IsPointerOverBlockingElement;
                if (isPointerOverUi) return;
                Vector3 pos = transform.position;
                Vector2 mousePos = GetMousePosition();
                if (mousePos.y >= Screen.height - BorderThickness)
                {
                    var t = Time.deltaTime * SpeedCamera;
                    pos.z += t;
                }
                if (mousePos.y <= BorderThickness)
                {
                    var t = Time.deltaTime * SpeedCamera;
                    pos.z -= t;
                }
                if (mousePos.x >= Screen.width - BorderThickness)
                {
                    var t = Time.deltaTime * SpeedCamera;
                    pos.x += t;
                }
                if (mousePos.x <= BorderThickness)
                {
                    var t = Time.deltaTime * SpeedCamera;
                    pos.x -= t;
                }

                float scroll = GetScrollDelta();
                pos.y += -scroll * ScrollSpeed * 100f * Time.deltaTime;

                pos.y = Mathf.Clamp(pos.y, MinCameraDistance, MaxCameraDistance);
                /*
                pos.x = Mathf.Clamp(pos.x, CameraLimit.transform.position.x, CameraLimit.terrainData.size.x);
                pos.z = Mathf.Clamp(pos.z, CameraLimit.transform.position.z, CameraLimit.terrainData.size.z);
                */

                transform.position = pos;
            }
        }
    }
}