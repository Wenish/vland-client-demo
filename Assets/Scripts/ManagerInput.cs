using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerInput : MonoBehaviour
{

    public float horizontalInput;
    public float verticalInput;
    public Vector3 mousePositionRelativeToCenterOfScreen = Vector3.zero;
    public float angle = 0f;
    public Transform UnitTransform;
    public UnitController UnitController;

    public Vector3 MouseWorldPosition;

    Plane plane = new Plane(Vector3.up, 0);
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SetMouseWorldPosition();
        CalculateAngle();
        InputAxis();
        ControlUnit();
    }

    private void SetMouseWorldPosition()
    {
        float distance;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out distance))
        {
            MouseWorldPosition = ray.GetPoint(distance);
        }
    }

    private void CalculateAngle()
    {
        Vector3 pos = UnitTransform.position - MouseWorldPosition;
        angle = -(Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg) - 90;
    }

    private void InputAxis()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    private void ControlUnit()
    {
        if (!UnitController) return;
        UnitController.horizontalInput = horizontalInput;
        UnitController.verticalInput = verticalInput;
        UnitController.angle = angle;

        
        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("FIRE11111");
            UnitController.weapon.Attack(UnitController.gameObject);
        }
    }
}
