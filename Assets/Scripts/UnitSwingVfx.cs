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
        var swinging = unitController.GetWeaponForAttackIndex(obj.attackIndex);
        if (swinging == null) return;

        var swingVfxList = swinging.swingVfxs;
        if (swingVfxList == null || swingVfxList.Count == 0) return;

        var swingVfxListItem = swingVfxList[obj.attackIndex % swingVfxList.Count];
        if (swingVfxListItem.swingVfxPrefab == null) return;

        Transform spawnTransform = transform;

        // Instantiate the swing VFX at the hand position with the specified offsets
        Vector3 spawnPosition = spawnTransform.position + spawnTransform.TransformVector(swingVfxListItem.swingVfxPositionOffset);

        Quaternion spawnRotation = spawnTransform.rotation * Quaternion.Euler(swingVfxListItem.swingVfxRotationOffset);

        GameObject vfxInstance = Instantiate(swingVfxListItem.swingVfxPrefab, spawnPosition, spawnRotation, transform);

        // Destroy the VFX after its lifetime if specified
        if (swingVfxListItem.swingVfxLifetime > 0f)
        {
            Destroy(vfxInstance, swingVfxListItem.swingVfxLifetime);
        }
    }
}