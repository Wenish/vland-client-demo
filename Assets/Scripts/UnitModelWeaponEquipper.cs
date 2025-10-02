using UnityEngine;

public class UnitModelWeaponEquipper : MonoBehaviour
{
    public Transform rightHandTransform;
    public Transform leftHandTransform;

    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    private GameObject currentWeaponRightHandInstance;
    private GameObject currentWeaponLeftHandInstance;

    private UnitController unitController;

    private void Awake()
    {
        unitController = GetComponentInParent<UnitController>();
        if (unitController == null)
        {
            Debug.LogError("UnitModelWeaponEquipper must be a child of a GameObject with a UnitController component.");
            return;
        }
        EquipWeapon(unitController.currentWeapon);
        unitController.OnWeaponChange += HandleOnWeaponChange;
    }

    private void OnDestroy()
    {
        if (unitController != null)
        {
            unitController.OnWeaponChange -= HandleOnWeaponChange;
        }
    }

    private void HandleOnWeaponChange(UnitController unitController)
    {
        EquipWeapon(unitController.currentWeapon);
    }

    public void EquipWeapon(WeaponData weaponData)
    {
        if (currentWeaponRightHandInstance != null)
        {
            Destroy(currentWeaponRightHandInstance);
        }
        if (currentWeaponLeftHandInstance != null)
        {
            Destroy(currentWeaponLeftHandInstance);
        }

        if (weaponData != null && weaponData.weaponModelRightHand != null)
        {
            currentWeaponRightHandInstance = Instantiate(weaponData.weaponModelRightHand, rightHandTransform);
            currentWeaponRightHandInstance.transform.localPosition = Vector3.zero + positionOffset;
            currentWeaponRightHandInstance.transform.localRotation = Quaternion.identity * Quaternion.Euler(rotationOffset);
        }
        if (weaponData != null && weaponData.weaponModelLeftHand != null && leftHandTransform != null)
        {
            currentWeaponLeftHandInstance = Instantiate(weaponData.weaponModelLeftHand, leftHandTransform);
            currentWeaponLeftHandInstance.transform.localPosition = Vector3.zero + positionOffset;
            var leftHandRotationOffset = rotationOffset;
            leftHandRotationOffset.y += 180;
            currentWeaponLeftHandInstance.transform.localRotation = Quaternion.identity * Quaternion.Euler(leftHandRotationOffset);
        }
    }
}