using Game.Scripts.Controllers;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class Billboard : MonoBehaviour
{
    Camera cameraToLookAt;
    RectTransform rectTransform;
    Transform unitRoot;
    public Transform unit;
    public float distanceFromCamera = 1.0f;
    public Vector3 scaleMax = new Vector3(0.01f, 0.01f, 0.01f);
    public Vector3 scaleMin = new Vector3(0.005f, 0.005f, 0.005f);
    public Vector3 positionMax = new Vector3(0, 1, 0);
    public Vector3 positionMin = new Vector3(0, 2, 0);
    public float Zoom = 1f;
    public float SpeedScale = 0.2f;

    [Header("Overhead anchor")]
    [Tooltip("Keep UI above the unit in world space so rotation snaps do not orbit the health bar.")]
    [SerializeField] private bool anchorAboveUnit = true;
    [Tooltip("World-space height above the unit root. When zero, uses the RectTransform anchored Y on Awake.")]
    [SerializeField] private float overheadHeight;

    float zoomVerticalOffset;

    void Awake()
    {
        cameraToLookAt = Camera.main;
        rectTransform = gameObject.GetComponent<RectTransform>();
        cameraToLookAt.GetComponent<ControllerCamera>().OnZoomChange += HandleOnZoomChange;

        unitRoot = unit != null ? unit : transform.parent;
        if (anchorAboveUnit && overheadHeight <= 0f && rectTransform != null)
            overheadHeight = rectTransform.anchoredPosition.y;

        zoomVerticalOffset = Vector3.Lerp(positionMin, positionMax, Zoom).y;
    }

    void LateUpdate()
    {
        ScaleCanvas();
        LookAtCamera();

        if (anchorAboveUnit && unitRoot != null)
            AnchorAboveUnit();
    }

    private void AnchorAboveUnit()
    {
        transform.position = unitRoot.position + Vector3.up * (overheadHeight + zoomVerticalOffset);
    }

    private void ScaleCanvas()
    {
        var t = Time.deltaTime * SpeedScale;

        var desiredScale = Vector3.Lerp(scaleMin, scaleMax, Zoom);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, desiredScale, t);

        var desiredPosition = Vector3.Lerp(positionMin, positionMax, Zoom);
        zoomVerticalOffset = Mathf.Lerp(zoomVerticalOffset, desiredPosition.y, t);
    }

    private void LookAtCamera()
    {
        transform.LookAt(transform.position + cameraToLookAt.transform.rotation * Vector3.forward, cameraToLookAt.transform.rotation * Vector3.up);
    }

    private void HandleOnZoomChange(float zoom)
    {
        Zoom = zoom;
    }
}
