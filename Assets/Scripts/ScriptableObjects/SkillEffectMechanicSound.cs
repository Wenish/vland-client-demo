using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Audio/Sound")]
public class SkillEffectMechanicSound : SkillEffectMechanic
{
    [Tooltip("Sound to play when the effect is executed")]
    public SoundData soundData;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        if (soundData != null && soundData.clip != null)
        {
            foreach (var target in targets)
            {
                if (Mirror.NetworkServer.active)
                {
                    castContext.skillInstance.Rpc_PlaySound(
                        soundData.soundName,
                        target.transform.position
                    );
                }
            }
        }
        return targets;
    }
}