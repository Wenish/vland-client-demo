using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffPeriodicSkillEffectChain : PeriodicBuff
{
    private readonly SkillEffectChainData _effectChainDataOnTick;
    private readonly CastContext _castContext;

    public BuffPeriodicSkillEffectChain(
        string buffId,
        float duration,
        float tickInterval,
        SkillEffectChainData effectChainDataOnTick,
        CastContext castContext,
        UniqueMode uniqueMode = UniqueMode.None,
        UnitMediator caster = null,
        bool tickOnApply = false,
        BuffType buffType = null
    ) : base(buffId, duration, tickInterval, uniqueMode, caster, tickOnApply, buffType)
    {
        _effectChainDataOnTick = effectChainDataOnTick;
        _castContext = castContext;
    }

    public override void OnApply(UnitMediator mediator)
    {
        base.OnApply(mediator);
        // maybe execute OnApply for SkillEffectChain
    }

    public override void OnRemove(UnitMediator mediator)
    {
        base.OnRemove(mediator);
        // maybe execute OnRemove for SkillEffectChain
    }
    public override void OnTick(UnitMediator mediator)
    {
        if (mediator == null || mediator.UnitController == null)
        {
            Debug.LogWarning($"Cannot execute periodic effect chain for buff {BuffId}: mediator or UnitController is null.");
            return;
        }

        var targets = new List<UnitController> { mediator.UnitController };
        var coroutine = _effectChainDataOnTick.ExecuteCoroutine(_castContext, targets);

        if (coroutine == null)
        {
            Debug.LogWarning($"Effect chain coroutine is null for buff {BuffId}.");
            return;
        }

        if (mediator.UnitController is MonoBehaviour mb)
        {
            mb.StartCoroutine(coroutine);
        }
        else
        {
            // Fallback: attempt synchronous execution
            while (coroutine.MoveNext()) { }
            Debug.LogWarning($"UnitController is not a MonoBehaviour; executed coroutine synchronously for buff {BuffId}.");
        }
    }
}