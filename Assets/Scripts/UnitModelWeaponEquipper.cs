using ShadowInfection.Items;
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
            Debug.LogWarning("UnitModelWeaponEquipper must be a child of a GameObject with a UnitController component.");
            return;
        }
        RefreshVisuals();
        unitController.OnWeaponChange += HandleOnWeaponChange;
    }

    private void OnDestroy()
    {
        if (unitController != null)
        {
            unitController.OnWeaponChange -= HandleOnWeaponChange;
        }
    }

    private void HandleOnWeaponChange(UnitController _)
    {
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        if (currentWeaponRightHandInstance != null)
        {
            Destroy(currentWeaponRightHandInstance);
            currentWeaponRightHandInstance = null;
        }
        if (currentWeaponLeftHandInstance != null)
        {
            Destroy(currentWeaponLeftHandInstance);
            currentWeaponLeftHandInstance = null;
        }

        var main = unitController != null ? unitController.currentWeapon : null;
        var off = unitController != null ? unitController.offHandItemWeapon : null;

        var rightModel = main != null ? main.weaponModelRightHand : null;
        if (rightModel != null && rightHandTransform != null)
        {
            currentWeaponRightHandInstance = Instantiate(rightModel, rightHandTransform);
            currentWeaponRightHandInstance.transform.localPosition = Vector3.zero + positionOffset;
            currentWeaponRightHandInstance.transform.localRotation = Quaternion.identity * Quaternion.Euler(rotationOffset);
        }

        var leftModel = ResolveLeftHandModel(main, off);
        if (leftModel != null && leftHandTransform != null)
        {
            currentWeaponLeftHandInstance = Instantiate(leftModel, leftHandTransform);
            currentWeaponLeftHandInstance.transform.localPosition = Vector3.zero + positionOffset;
            var leftHandRotationOffset = rotationOffset;
            leftHandRotationOffset.y += 180;
            currentWeaponLeftHandInstance.transform.localRotation = Quaternion.identity * Quaternion.Euler(leftHandRotationOffset);
        }
    }

    private static GameObject ResolveLeftHandModel(WeaponData main, WeaponData off)
    {
        if (off != null)
        {
            if (off.weaponModelLeftHand != null)
                return off.weaponModelLeftHand;
            return off.weaponModelRightHand;
        }

        if (main == null)
            return null;

        if (ItemRules.IsPairedTwoModelWeapon(main.weaponType))
            return main.weaponModelLeftHand;

        if (!ItemRules.IsDualWieldWeapon(main.weaponType))
            return main.weaponModelLeftHand;

        return null;
    }
}
