using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicSound", menuName = "Game/Skills/Effects/Audio/Sound")]
public class SkillEffectMechanicSound : SkillEffectMechanic
{
    [Tooltip("Sound to play when the effect is executed")]
    public SoundData soundData;

    public bool attachToTarget = true;
    [Range(0f, 0.5f)]
    [Tooltip("Maximum random pitch deviation (±). 0.0 = no variation, 0.5 = noticeable variation.")]
    public float pitchOffset = 0f;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        if (soundData != null && soundData.clip != null)
        {
            foreach (var target in targets)
            {
                if (Mirror.NetworkServer.active)
                {
                    // Randomize pitch each time within [-pitchOffset, +pitchOffset]
                    float randomizedPitchOffset = pitchOffset != 0f
                        ? Random.Range(-pitchOffset, pitchOffset)
                        : 0f;

                    castContext.skillInstance.Rpc_PlaySound(
                        soundData.soundName,
                        target.transform.position,
                        randomizedPitchOffset,
                        attachToTarget,
                        target.netId
                    );
                }
            }
        }
        return targets;
    }
}