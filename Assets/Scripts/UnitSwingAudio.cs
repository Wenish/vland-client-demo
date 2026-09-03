using Gapa.Audio.Sfx;
using ShadowInfection.Audio;
using ShadowInfection.DI;
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

    private void HandleOnAttackStart((UnitController unitController, int attackIndex) obj)
    {
        if (obj.unitController != this.unitController) return;
        var swinging = this.unitController.GetWeaponForAttackIndex(obj.attackIndex);
        if (swinging == null) return;

        var onAttackStartAudioList = swinging.onAttackStartAudioClips;
        if (onAttackStartAudioList == null || onAttackStartAudioList.Count == 0) return;

        var audioListItem = onAttackStartAudioList[obj.attackIndex % onAttackStartAudioList.Count];
        Play(audioListItem.soundData);
    }

    private void HandleOnAttackSwing((UnitController attacker, int attackIndex) obj)
    {
        if (obj.attacker != unitController) return;
        var swinging = unitController.GetWeaponForAttackIndex(obj.attackIndex);
        if (swinging == null) return;

        var swingAudioList = swinging.swingAudioClips;
        if (swingAudioList == null || swingAudioList.Count == 0) return;

        var swingAudioListItem = swingAudioList[obj.attackIndex % swingAudioList.Count];
        Play(swingAudioListItem.soundData);
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
        Play(audioListItem.soundData);
    }

    private void Play(SfxDefinition definition)
    {
        if (!GameLifetimeScope.TryResolve<ISfxPlayer>(out var sfx))
            return;

        sfx.Play(definition, unitController.transform.position);
    }
}
