using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerInput : MonoBehaviour
{

    public float horizontalInput;
    public float verticalInput;
    public Vector3 mousePositionRelativeToCenterOfScreen = Vector3.zero;
    public float angle = 0f;
    public UnitController UnitController;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        InputMouse();
        InputAxis();
        ControlUnit();
    }

    private void InputMouse()
    {
        mousePositionRelativeToCenterOfScreen.x = Mathf.Clamp((Input.mousePosition.x - Screen.width/2) / Screen.width, -0.5f, 0.5f);
        mousePositionRelativeToCenterOfScreen.y = Mathf.Clamp((Input.mousePosition.y - Screen.height/2) / Screen.height, -0.5f, 0.5f);

        Debug.Log($"Mouse X:{mousePositionRelativeToCenterOfScreen.x} Y:{mousePositionRelativeToCenterOfScreen.y}");

        angle = -(Mathf.Atan2(mousePositionRelativeToCenterOfScreen.y, mousePositionRelativeToCenterOfScreen.x) * Mathf.Rad2Deg);
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
    }
}
