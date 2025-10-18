using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitUiBuffBar : MonoBehaviour
{

    [Header("Prefab & Limits")]
    public UnitUiBuffBarItem BuffBarItemPrefab;

    // key by unique buff instance id to allow multiple same BuffId
    readonly Dictionary<string, UnitUiBuffBarItem> _activeBuffItems = new();

    readonly Queue<UnitUiBuffBarItem> _itemPool = new();


    public void SetBuffs(IEnumerable<UiBuffData> buffs)
    {
        // Expiring first
        var ordered = buffs.OrderByDescending(b => b.TimeRemaining).ToList();

        // 1) Remove old buffs
        var toRemove = _activeBuffItems.Keys.Except(ordered.Select(b => b.InstanceId)).ToList();
        foreach (var buffId in toRemove)
        {
            RemoveIcon(buffId);
        }

        // 2) Add / update current buffs
        foreach (var buffData in ordered)
        {
            if (_activeBuffItems.TryGetValue(buffData.InstanceId, out var item))
            {
                // update existing
                item.SetBuffData(buffData);
            }
            else
            {
                // add new
                var newItem = GetBuffBarItem();
                newItem.SetBuffData(buffData);
                _activeBuffItems[buffData.InstanceId] = newItem;
            }
        }

        // 3) Reorder UI to match 'ordered'
        for (int i = 0; i < ordered.Count; i++)
        {
            if (_activeBuffItems.TryGetValue(ordered[i].InstanceId, out var ui))
                ui.transform.SetSiblingIndex(i);
        }
    }

    public void RemoveIcon(string instanceId)
    {
        if (_activeBuffItems.TryGetValue(instanceId, out var item))
        {
            _activeBuffItems.Remove(instanceId);
            item.gameObject.SetActive(false);
            _itemPool.Enqueue(item);
        }
    }

    private UnitUiBuffBarItem GetBuffBarItem()
    {
        if (_itemPool.Count > 0)
        {
            var item = _itemPool.Dequeue();
            item.gameObject.SetActive(true);
            return item;
        }
        else
        {
            return Instantiate(BuffBarItemPrefab, transform);
        }
    }

}