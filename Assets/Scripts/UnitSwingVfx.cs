using Mirror;
using UnityEngine;

public class UnitSwingVfx : MonoBehaviour
{
    public UnitController unitController;

    void Awake()
    {
        unitController = GetComponent<UnitController>();
        if (unitController == null)
        {
            Debug.LogError("UnitSwingVfx: Missing UnitController reference.", this);
            enabled = false;
            return;
        }
        unitController.OnAttackSwing += HandleOnAttackSwing;
    }

    void OnDestroy()
    {
        if (unitController != null)
        {
            unitController.OnAttackSwing -= HandleOnAttackSwing;
        }
    }

    private void HandleOnAttackSwing((UnitController attacker, int attackIndex) obj)
    {
        if (obj.attacker != unitController) return;
        if (unitController.currentWeapon == null || unitController.currentWeapon.swingVfxPrefab == null) return;

        Transform spawnTransform = transform;

        // Instantiate the swing VFX at the hand position with the specified offsets
        Vector3 spawnPosition = spawnTransform.position + spawnTransform.TransformVector(unitController.currentWeapon.swingVfxPositionOffset);

        Quaternion spawnRotation = spawnTransform.rotation * Quaternion.Euler(unitController.currentWeapon.swingVfxRotationOffset);

        // Flip 180 degrees around Y axis if attack index is odd
        if ((obj.attackIndex & 1) == 1)
        {
            spawnRotation = spawnRotation * Quaternion.Euler(0f, 0f, 180f);
        }


        if ((obj.attackIndex & 1) == 1) {
            spawnRotation = spawnRotation * Quaternion.Euler(0f, 0f, 10f);
        } else {
            spawnRotation = spawnRotation * Quaternion.Euler(0f, 0f, -10f);
        }

        GameObject vfxInstance = Instantiate(unitController.currentWeapon.swingVfxPrefab, spawnPosition, spawnRotation, transform);

        // Destroy the VFX after its lifetime if specified
        if (unitController.currentWeapon.swingVfxLifetime > 0f)
        {
            Destroy(vfxInstance, unitController.currentWeapon.swingVfxLifetime);
        }
    }
}