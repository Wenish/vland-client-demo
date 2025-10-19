using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(UnitUiBuffBar))]
[DefaultExecutionOrder(100)]
public class UnitUiBuffBarPresenter : MonoBehaviour
{
    public UnitUiBuffBar BuffBar;
    public UnitNetworkBuffs UnitNetworkBuffs;

    [SerializeField]
    public List<UiBuffData> _currentBuffs = new();

    void Awake()
    {
        BuffBar = GetComponent<UnitUiBuffBar>();
        UnitNetworkBuffs = GetComponentInParent<UnitNetworkBuffs>();
    }

    private void Update()
    {
        // Update buff timers
        bool needsUpdate = false;
        foreach (var buffData in _currentBuffs)
        {
            if (buffData.Duration > 0f)
            {
                var buff = UnitNetworkBuffs.NetworkBuffs.FirstOrDefault(b => b.InstanceId == buffData.InstanceId);
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
        if (!UnitNetworkBuffs)
        {
            UnitNetworkBuffs = GetComponentInParent<UnitNetworkBuffs>();
        }

        if (!UnitNetworkBuffs)
        {
            Debug.LogWarning($"{nameof(UnitUiBuffBarPresenter)}: UnitNetworkBuffs not found.", this);
            return;
        }

        UnitNetworkBuffs.NetworkBuffs.OnAdd += OnBuffAdded;
        UnitNetworkBuffs.NetworkBuffs.OnRemove += OnBuffRemoved;
        UnitNetworkBuffs.NetworkBuffs.OnSet += OnBuffChanged;

        // Seed current network buffs into the UI to cover cases where the list
        // was populated before we subscribed (e.g., host mode or late binding)
        for (int i = 0; i < UnitNetworkBuffs.NetworkBuffs.Count; i++)
        {
            var buff = UnitNetworkBuffs.NetworkBuffs[i];
            if (_currentBuffs.Any(b => b.InstanceId == buff.InstanceId))
                continue;

            var isInfiniteBuff = buff.Duration == Mathf.Infinity;
            Texture2D iconTexture = DatabaseManager.Instance.skillDatabase.GetSkillByName(buff.SkillName)?.iconTexture;

            _currentBuffs.Add(new UiBuffData
            {
                InstanceId = buff.InstanceId,
                BuffId = buff.BuffId,
                IconTexture = iconTexture,
                Duration = buff.Duration,
                TimeRemaining = isInfiniteBuff ? Mathf.Infinity : buff.Remaining
            });
        }

        if (_currentBuffs.Count > 0)
            BuffBar.SetBuffs(_currentBuffs);
    }

    private void OnDisable()
    {
        if (!UnitNetworkBuffs) return;
        UnitNetworkBuffs.NetworkBuffs.OnAdd -= OnBuffAdded;
        UnitNetworkBuffs.NetworkBuffs.OnRemove -= OnBuffRemoved;
        UnitNetworkBuffs.NetworkBuffs.OnSet -= OnBuffChanged;
        
        _currentBuffs.Clear();
        BuffBar.SetBuffs(_currentBuffs);
    }

    private void OnBuffAdded(int index)
    {
        var buff = UnitNetworkBuffs.NetworkBuffs[index];
        var isInfiniteBuff = buff.Duration == Mathf.Infinity;
        Texture2D iconTexture = DatabaseManager.Instance.skillDatabase.GetSkillByName(buff.SkillName)?.iconTexture;

        // Avoid duplicates if we seeded earlier or got multiple add events
        var existing = _currentBuffs.FirstOrDefault(b => b.InstanceId == buff.InstanceId);
        if (existing != null)
        {
            existing.BuffId = buff.BuffId;
            existing.IconTexture = iconTexture;
            existing.Duration = buff.Duration;
            existing.TimeRemaining = isInfiniteBuff ? Mathf.Infinity : buff.Remaining;
        }
        else
        {
            var buffData = new UiBuffData
            {
                InstanceId = buff.InstanceId,
                BuffId = buff.BuffId,
                IconTexture = iconTexture,
                // StackCount = buff.StackCount,
                Duration = buff.Duration,
                TimeRemaining = isInfiniteBuff ? Mathf.Infinity : buff.Remaining
            };
            _currentBuffs.Add(buffData);
        }
        BuffBar.SetBuffs(_currentBuffs);
    }

    private void OnBuffRemoved(int index, UnitNetworkBuffs.NetworkBuffData oldBuff)
    {
        var buffData = _currentBuffs.FirstOrDefault(b => b.InstanceId == oldBuff.InstanceId);
        if (buffData != null)
        {
            _currentBuffs.Remove(buffData);
            BuffBar.SetBuffs(_currentBuffs);
        }
    }
    
    public void OnBuffChanged(int index, UnitNetworkBuffs.NetworkBuffData oldBuff)
    {
        var buff = UnitNetworkBuffs.NetworkBuffs[index];
        var buffData = _currentBuffs.FirstOrDefault(b => b.InstanceId == buff.InstanceId);
        if (buffData != null)
        {
            buffData.TimeRemaining = buff.Remaining;
            BuffBar.SetBuffs(_currentBuffs);
        }
    }
}