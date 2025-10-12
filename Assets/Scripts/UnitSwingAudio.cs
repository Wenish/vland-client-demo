using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class UnitSwingAudio : MonoBehaviour
{
    UnitController unitController;

    void Awake()
    {
        unitController = GetComponent<UnitController>();
        if (unitController == null)
        {
            Debug.LogError("UnitSwingAudio: Missing UnitController reference.", this);
            enabled = false;
            return;
        }
        unitController.OnAttackSwing += HandleOnAttackSwing;
    }

    private void OnDestroy()
    {
        if (unitController != null)
        {
            unitController.OnAttackSwing -= HandleOnAttackSwing;
        }
    }

    private void HandleOnAttackSwing((UnitController attacker, int attackIndex) obj)
    {
        if (obj.attacker != unitController) return;
        if (unitController.currentWeapon == null) return;

        var swingAudioList = unitController.currentWeapon.swingAudioClips;
        if (swingAudioList == null || swingAudioList.Count == 0) return;

        var swingAudioListItem = swingAudioList[obj.attackIndex % swingAudioList.Count];
        if (swingAudioListItem.soundData == null || swingAudioListItem.soundData.clip == null) return;

        // Randomize pitch each time within [-pitchOffset, +pitchOffset]
        float randomizedPitchOffset = swingAudioListItem.pitchOffset != 0f
            ? Random.Range(-swingAudioListItem.pitchOffset, swingAudioListItem.pitchOffset)
            : 0f;

        SoundManager.Instance.PlaySound(swingAudioListItem.soundData, unitController.transform.position, transform, randomizedPitchOffset);
    }
}