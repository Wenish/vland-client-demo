using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera CameraMain;
    public Transform Target;

    [SerializeField]
    private float Angle;
    void Awake()
    {
        CameraMain = Camera.main;
    }
    void Update()
    {
        CalculateAngle();
    }
    void LateUpdate()
    {
        transform.LookAt(transform.position + CameraMain.transform.forward);
        //transform.rotation = Quaternion.LookRotation(transform.position - CameraMain.transform.position);
    }

    private void CalculateAngle()
    {
        if (Target) {
            Angle = Vector3.SignedAngle((Target.position - CameraMain.transform.position), CameraMain.transform.forward, CameraMain.transform.right);
            Debug.Log("Angle:" + Angle);
        }
    }
}
