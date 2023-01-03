using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera CameraMain;
    void Awake()
    {
        CameraMain = Camera.main;
    }
    void LateUpdate()
    {
        transform.LookAt(transform.position + CameraMain.transform.forward);
        //transform.rotation = Quaternion.LookRotation(transform.position - CameraMain.transform.position);
    }
}
