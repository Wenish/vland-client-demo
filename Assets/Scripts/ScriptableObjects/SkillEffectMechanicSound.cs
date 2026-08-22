using System.Collections.Generic;
using Gapa.Audio.Sfx;
using ShadowInfection.Audio;
using ShadowInfection.DI;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicSound", menuName = "Game/Skills/Effects/Audio/Sound")]
public class SkillEffectMechanicSound : SkillEffectMechanic
{
    [Tooltip("Sound to play when the effect is executed")]
    public SfxDefinition soundData;

    public bool attachToTarget = true;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        if (soundData == null || soundData.Clips == null || soundData.Clips.Length == 0)
            return targets;

        if (!GameLifetimeScope.TryResolve<ISfxPlayer>(out var sfx))
            return targets;

        foreach (var target in targets)
        {
            if (target == null)
                continue;

            // Online: server broadcasts catalog id so every client hears it (including host).
            if (Mirror.NetworkServer.active && castContext?.skillInstance != null)
            {
                if (!sfx.TryGetId(soundData, out var soundId))
                    continue;

                castContext.skillInstance.Rpc_PlaySound(
                    soundId,
                    target.transform.position,
                    attachToTarget,
                    target.netId);
                continue;
            }

            // Offline / no Mirror.
            if (attachToTarget)
                sfx.PlayAttached(soundData, target.transform);
            else
                sfx.Play(soundData, target.transform.position);
        }

        return targets;
    }
}
