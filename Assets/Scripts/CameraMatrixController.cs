using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMatrixController : MonoBehaviour
{
    public float fieldOfView = 60.0f;
    public float aspectRatio = 16.0f / 9.0f;
    
    void Awake()
    {
        GetComponent<Camera>().projectionMatrix = Matrix4x4.Perspective(fieldOfView, aspectRatio, 0.1f, 100.0f);
    }
}
