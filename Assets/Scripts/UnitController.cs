using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{

    public float moveSpeed = 5f;
    Rigidbody unitRigidbody;

    float horizontalInput = 0f;
    float verticalInput = 0f;

    // Start is called before the first frame update
    void Start()
    {
        unitRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    // Update is called once per frame
    void Update()
    {
        MyInput();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
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
}
