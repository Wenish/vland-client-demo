using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Controllers;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    Camera cameraToLookAt;
    RectTransform rectTransform;
    public Transform unit;
    public float distanceFromCamera = 1.0f;
    public Vector3 offset;
    public float yOffset = 0.5f;
    public Vector3 scaleMax = new Vector3(0.01f, 0.01f, 0.01f);
    public Vector3 scaleMin = new Vector3(0.005f, 0.005f, 0.005f);
    public Vector3 positionMax = new Vector3(0, 1, 0);
    public Vector3 positionMin = new Vector3(0, 2, 0);
    public float Zoom = 1f;
    public float SpeedScale = 0.2f;
    void Awake()
    {
        cameraToLookAt = Camera.main;
        rectTransform = gameObject.GetComponent<RectTransform>();
        cameraToLookAt.GetComponent<ControllerCamera>().OnZoomChange += HandleOnZoomChange;
    }
    void Update()
    {
        // CalculatePosition();
    }
    void LateUpdate()
    {
        // CalculatePosition();
        ScaleCanvas();


        LookAtCamera();
        // LookAtCamera();
        // transform.LookAt(transform.position + CameraMain.transform.forward);
        //transform.rotation = Quaternion.LookRotation(transform.position - CameraMain.transform.position);
    }

    private void ScaleCanvas()
    {
        var t = Time.deltaTime * SpeedScale;

        var desiredScale = Vector3.Lerp(scaleMin, scaleMax, Zoom);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, desiredScale, t);

        var desiredPosition = Vector3.Lerp(positionMin, positionMax, Zoom);
        Debug.Log(desiredPosition);
        rectTransform.localPosition = Vector3.Lerp(rectTransform.localPosition, desiredPosition, t);

    }

    private void LookAtCamera()
    {
        transform.LookAt(transform.position + cameraToLookAt.transform.rotation * Vector3.forward, cameraToLookAt.transform.rotation * Vector3.up);
    }

    private void CalculatePosition()
    {
        Vector3 screenPos = cameraToLookAt.WorldToViewportPoint(unit.position);
        screenPos.y = Mathf.Abs(screenPos.y * 2 - 1);
        transform.position = unit.position + offset + (transform.up * screenPos.y);
    }

    private void HandleOnZoomChange(float zoom)
    {
        Zoom = zoom;
    }
}
