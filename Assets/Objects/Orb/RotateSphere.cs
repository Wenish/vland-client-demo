using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateSphere : MonoBehaviour
{
    public float rotationSpeed = 50f;

    public float movementSpeed = 1f;
    public float movementDistance = 0.25f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * Time.deltaTime * rotationSpeed);
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * movementSpeed) * movementDistance;
    }
}