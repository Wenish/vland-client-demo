using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{

    public float moveSpeed = 5f;
    Rigidbody unitRigidbody;

    public float horizontalInput = 0f;
    public float verticalInput = 0f;
    public float angle = 0f;

    // Start is called before the first frame update
    void Start()
    {
        unitRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void MovePlayer()
    {
        
        Vector3 inputs = Vector3.zero;
        inputs.x = horizontalInput;
        inputs.z = verticalInput;
        inputs = Vector3.ClampMagnitude(inputs, 1f);
        Vector3 moveDirection = inputs * moveSpeed;
        Debug.Log("move");
        Debug.Log(inputs);
        Debug.Log(moveDirection);
        unitRigidbody.velocity = moveDirection;
    }

    private void RotatePlayer()
    {
        float lerpedAngle = Mathf.LerpAngle(transform.rotation.eulerAngles.y, angle, Time.deltaTime * 10);
        transform.rotation = Quaternion.AngleAxis(lerpedAngle, Vector3.up);
    }
}
