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

    public bool isPressingFire1 = false;
    
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
        
        if (Input.GetButtonDown("Fire1"))
        {
            isPressingFire1 = true;
        }
        if (Input.GetButtonUp("Fire1"))
        {
            isPressingFire1 = false;
        }
        if(Input.GetKeyDown(KeyCode.I))
        {
            UnitEquipSword();
        }
        if(Input.GetKeyDown(KeyCode.U))
        {
            UnitEquipBow();
        }
    }

    private void UnitEquipSword()
    {
        if(!UnitController) return;
        WeaponMelee weaponMelee = UnitController.GetComponent<WeaponMelee>();
        if(!weaponMelee) return;
        UnitController.weapon = weaponMelee;
    }

    private void UnitEquipBow()
    {
        if(!UnitController) return;
        WeaponRanged weaponRanged = UnitController.GetComponent<WeaponRanged>();
        if(!weaponRanged) return;
        UnitController.weapon = weaponRanged;
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

        
        if (isPressingFire1)
        {
            UnitController.Attack();
        }
    }
}
