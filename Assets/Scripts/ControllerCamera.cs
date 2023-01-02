using UnityEngine;

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

        void Start()
        {
        }

        void Update()
        {
            // Exit Game  
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            }

            // Change Camera Type
            if (Input.GetKeyDown(KeyCode.Z))
            {
                IsFocusingPlayer = !IsFocusingPlayer;
            }

            if (IsFocusingPlayer) {
                // Lookg for target to focusing
            }
            OnScroll();
            MousePositionChange();
        }

        void MousePositionChange()
        {
            mousePositionRelativeToCenterOfScreen.x = Mathf.Clamp((Input.mousePosition.x - Screen.width/2) / Screen.width, -0.5f, 0.5f);
            mousePositionRelativeToCenterOfScreen.y = Mathf.Clamp((Input.mousePosition.y - Screen.height/2) / Screen.height, -0.5f, 0.5f);

            Debug.Log($"Mouse X:{mousePositionRelativeToCenterOfScreen.x} Y:{mousePositionRelativeToCenterOfScreen.y}");
        }

        void OnScroll()
        {
                float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
                Debug.Log(scroll);
                Zoom += -scroll * 100 * Time.deltaTime;

                Zoom = Mathf.Clamp(Zoom, 0.2f, 1f);
                Debug.Log("Zoom" + Zoom);
        }

        void LateUpdate()
        {
            if (IsFocusingPlayer)
            {
                if (CameraTarget != null)
                {
                    Vector3 offset = OffsetCamera;
                    offset.z = offset.z + (mousePositionRelativeToCenterOfScreen.y * MouseOffsetInfluenceZ);
                    offset.x = offset.x + (mousePositionRelativeToCenterOfScreen.x * MouseOffsetInfluenceX);
                    offset.y = offset.y * Zoom;
                    offset.z = offset.z * Zoom;
                    var desiredPosition = CameraTarget.position + offset;
                    var t = Time.deltaTime * SpeedCamera;
                    transform.position = Vector3.Lerp(transform.position, desiredPosition, t);

                    //transform.LookAt(GameManager.CameraTarget);
                }
            } else {
                Vector3 pos = transform.position;
                if (Input.mousePosition.y >= Screen.height - BorderThickness)
                {
                    var t = Time.deltaTime * SpeedCamera;
                    pos.z += t;
                }
                if (Input.mousePosition.y <= BorderThickness)
                {
                    var t = Time.deltaTime * SpeedCamera;
                    pos.z -= t;
                }
                if (Input.mousePosition.x >= Screen.width - BorderThickness)
                {
                    var t = Time.deltaTime * SpeedCamera;
                    pos.x += t;
                }
                if (Input.mousePosition.x <= BorderThickness)
                {
                    var t = Time.deltaTime * SpeedCamera;
                    pos.x -= t;
                }

                float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
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