using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    Camera cameraToLookAt;
    public Transform unit;
    public float distanceFromCamera = 1.0f;
    public Vector3 offset;
    public float yOffset = 0.5f;
    void Awake()
    {
        cameraToLookAt = Camera.main;
    }
    void Update()
    {
        // CalculatePosition();
    }
    void LateUpdate()
    {
        // CalculatePosition();


        LookAtCamera();
        // LookAtCamera();
        // transform.LookAt(transform.position + CameraMain.transform.forward);
        //transform.rotation = Quaternion.LookRotation(transform.position - CameraMain.transform.position);
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
}
