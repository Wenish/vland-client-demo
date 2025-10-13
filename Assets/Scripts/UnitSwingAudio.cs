using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class UnitAttackAudio : MonoBehaviour
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
        unitController.OnAttackStart += HandleOnAttackStart;
        unitController.OnAttackHitReceived += HandleOnAttackHitReceivedEvent;
    }

    private void OnDestroy()
    {
        if (unitController != null)
        {
            unitController.OnAttackSwing -= HandleOnAttackSwing;
            unitController.OnAttackStart -= HandleOnAttackStart;
            unitController.OnAttackHitReceived -= HandleOnAttackHitReceivedEvent;
        }
    }

    private void HandleOnAttackStart((UnitController attacker, int attackIndex) obj)
    {
        if (obj.attacker != unitController) return;
        if (unitController.currentWeapon == null) return;

        var onAttackStartAudioList = unitController.currentWeapon.onAttackStartAudioClips;
        if (onAttackStartAudioList == null || onAttackStartAudioList.Count == 0) return;

        var audioListItem = onAttackStartAudioList[obj.attackIndex % onAttackStartAudioList.Count];
        if (audioListItem.soundData == null || audioListItem.soundData.clip == null) return;

        // Randomize pitch each time within [-pitchOffset, +pitchOffset]
        float randomizedPitchOffset = audioListItem.pitchOffset != 0f
            ? Random.Range(-audioListItem.pitchOffset, audioListItem.pitchOffset)
            : 0f;

        SoundManager.Instance.PlaySound(audioListItem.soundData, unitController.transform.position, transform, randomizedPitchOffset);
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

    public void HandleOnAttackHitReceivedEvent((UnitController target, UnitController attacker) obj)
    {
        if (obj.target != unitController) return;
        var attackerUnit = obj.attacker;
        if (attackerUnit == null) return;
        if (attackerUnit.currentWeapon == null) return;

        var onHitAudioList = attackerUnit.currentWeapon.onHitAudioClips;
        if (onHitAudioList == null || onHitAudioList.Count == 0)
            return;

        var audioListItem = onHitAudioList[Random.Range(0, onHitAudioList.Count)];
        if (audioListItem.soundData == null || audioListItem.soundData.clip == null)
            return;

        // Randomize pitch each time within [-pitchOffset, +pitchOffset]
        float randomizedPitchOffset = audioListItem.pitchOffset != 0f
            ? Random.Range(-audioListItem.pitchOffset, audioListItem.pitchOffset)
            : 0f;

        SoundManager.Instance.PlaySound(audioListItem.soundData, unitController.transform.position, transform, randomizedPitchOffset);
    }
}