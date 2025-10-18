using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(UnitUiBuffBar))]
[DefaultExecutionOrder(100)]
public class UnitUiBuffBarPresenter : MonoBehaviour
{
    public UnitUiBuffBar BuffBar;
    public UnitNetworkBuffs UnitNetworkBuffs;

    List<UiBuffData> _currentBuffs = new();

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
                var buff = UnitNetworkBuffs.NetworkBuffs.FirstOrDefault(b => b.BuffId == buffData.BuffId);
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
    }

    private void OnDisable()
    {
        if (!UnitNetworkBuffs) return;
        UnitNetworkBuffs.NetworkBuffs.OnAdd -= OnBuffAdded;
        UnitNetworkBuffs.NetworkBuffs.OnRemove -= OnBuffRemoved;
    }

    private void OnBuffAdded(int index)
    {
        var buff = UnitNetworkBuffs.NetworkBuffs[index];
        var isInfiniteBuff = buff.Duration == Mathf.Infinity;
        Texture2D iconTexture = DatabaseManager.Instance.skillDatabase.GetSkillByName(buff.SkillName)?.iconTexture;

        var buffData = new UiBuffData
        {
            BuffId = buff.BuffId,
            IconTexture = iconTexture,
            // StackCount = buff.StackCount,
            Duration = buff.Duration,
            TimeRemaining = isInfiniteBuff ? Mathf.Infinity : buff.Remaining
        };
        _currentBuffs.Add(buffData);
        BuffBar.SetBuffs(_currentBuffs);
    }

    private void OnBuffRemoved(int index, UnitNetworkBuffs.NetworkBuffData oldBuff)
    {
        var buffData = _currentBuffs.FirstOrDefault(b => b.BuffId == oldBuff.BuffId);
        if (buffData != null)
        {
            _currentBuffs.Remove(buffData);
            BuffBar.SetBuffs(_currentBuffs);
        }
    }
}