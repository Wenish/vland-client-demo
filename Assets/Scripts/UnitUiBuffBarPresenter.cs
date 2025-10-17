using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(UnitUiBuffBar))]
[DefaultExecutionOrder(100)]
public class UnitUiBuffBarPresenter : MonoBehaviour
{
    public UnitUiBuffBar BuffBar;
    public UnitMediator UnitMediator;

    List<UiBuffData> _currentBuffs = new();

    void Awake()
    {
        BuffBar = GetComponent<UnitUiBuffBar>();
        UnitMediator = GetComponentInParent<UnitMediator>();
    }

    private void Update()
    {
        // Update buff timers
        bool needsUpdate = false;
        foreach (var buffData in _currentBuffs)
        {
            if (buffData.Duration > 0f)
            {
                var buff = UnitMediator.Buffs.GetBuffById(buffData.BuffId);
                if (buff != null)
                {
                    buffData.TimeRemaining = buff.Remaining;
                    needsUpdate = true;
                }
            }
        }
        if (needsUpdate)
        {
            BuffBar.SetBuffs(_currentBuffs);
        }
    }

    private void OnEnable()
    {
        if (!UnitMediator)
        {
            UnitMediator = GetComponentInParent<UnitMediator>();
        }

        if (!UnitMediator)
        {
            Debug.LogWarning($"{nameof(UnitUiBuffBarPresenter)}: UnitMediator not found.", this);
            return;
        }

        UnitMediator.Buffs.OnBuffAdded += OnBuffAdded;
        UnitMediator.Buffs.OnBuffRemoved += OnBuffRemoved;
        // Initial population
        _currentBuffs.Clear();
        foreach (var buff in UnitMediator.Buffs.ActiveBuffs)
        {
            OnBuffAdded(buff);
        }
    }

    private void OnDisable()
    {
        if (!UnitMediator) return;
        UnitMediator.Buffs.OnBuffAdded -= OnBuffAdded;
        UnitMediator.Buffs.OnBuffRemoved -= OnBuffRemoved;
    }

    private void OnBuffAdded(Buff buff)
    {

        var isInfiniteBuff = buff.Duration == Mathf.Infinity;

        var buffData = new UiBuffData
        {
            BuffId = buff.BuffId,
            IconTexture = buff.IconTexture,
            // StackCount = buff.StackCount,
            Duration = buff.Duration,
            TimeRemaining = isInfiniteBuff ? Mathf.Infinity : buff.Remaining
        };
        _currentBuffs.Add(buffData);
        BuffBar.SetBuffs(_currentBuffs);
    }

    private void OnBuffRemoved(Buff buff)
    {
        var buffData = _currentBuffs.FirstOrDefault(b => b.BuffId == buff.BuffId);
        if (buffData != null)
        {
            _currentBuffs.Remove(buffData);
            BuffBar.SetBuffs(_currentBuffs);
        }
    }
}