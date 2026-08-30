using System;
using System.Collections.Generic;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.UI.Nameplates
{
    internal sealed class UnitNameplateBuffDriver
    {
        private static readonly Comparison<UiBuffData> ByTimeRemainingDescending =
            (a, b) => b.TimeRemaining.CompareTo(a.TimeRemaining);

        private readonly IGameDatabases databases;
        private readonly Action onChanged;
        private readonly Dictionary<string, UiBuffData> byInstanceId = new();
        private readonly List<UiBuffData> ordered = new();

        private UnitNetworkBuffs networkBuffs;

        public UnitNameplateBuffDriver(IGameDatabases databases, Action onChanged)
        {
            this.databases = databases;
            this.onChanged = onChanged;
        }

        public IReadOnlyList<UiBuffData> Buffs => ordered;

        public void Bind(UnitNetworkBuffs nextBuffs)
        {
            Unbind();

            networkBuffs = nextBuffs;
            if (networkBuffs == null)
                return;

            networkBuffs.NetworkBuffs.OnAdd += OnBuffAdded;
            networkBuffs.NetworkBuffs.OnRemove += OnBuffRemoved;
            networkBuffs.NetworkBuffs.OnSet += OnBuffChanged;
            Seed();
        }

        public void Unbind()
        {
            if (networkBuffs != null)
            {
                networkBuffs.NetworkBuffs.OnAdd -= OnBuffAdded;
                networkBuffs.NetworkBuffs.OnRemove -= OnBuffRemoved;
                networkBuffs.NetworkBuffs.OnSet -= OnBuffChanged;
            }

            byInstanceId.Clear();
            ordered.Clear();
            networkBuffs = null;
        }

        public bool Tick()
        {
            if (networkBuffs == null || ordered.Count == 0)
                return false;

            var changed = false;
            var networkList = networkBuffs.NetworkBuffs;
            for (var i = 0; i < networkList.Count; i++)
            {
                var buff = networkList[i];
                if (string.IsNullOrEmpty(buff.InstanceId))
                    continue;

                if (!byInstanceId.TryGetValue(buff.InstanceId, out var data))
                    continue;

                if (data.Duration <= 0f || data.Duration >= Mathf.Infinity)
                    continue;

                if (Mathf.Approximately(data.TimeRemaining, buff.Remaining))
                    continue;

                data.TimeRemaining = buff.Remaining;
                changed = true;
            }

            if (changed)
                SortOrdered();

            return changed;
        }

        private void Seed()
        {
            var networkList = networkBuffs.NetworkBuffs;
            for (var i = 0; i < networkList.Count; i++)
                TryAddOrUpdate(networkList[i], notify: false);

            SortOrdered();
        }

        private void OnBuffAdded(int index)
        {
            TryAddOrUpdate(networkBuffs.NetworkBuffs[index], notify: true);
        }

        private void OnBuffRemoved(int index, UnitNetworkBuffs.NetworkBuffData oldBuff)
        {
            if (oldBuff == null || string.IsNullOrEmpty(oldBuff.InstanceId))
                return;

            if (!byInstanceId.TryGetValue(oldBuff.InstanceId, out var data))
                return;

            byInstanceId.Remove(oldBuff.InstanceId);
            ordered.Remove(data);
            onChanged();
        }

        private void OnBuffChanged(int index, UnitNetworkBuffs.NetworkBuffData oldBuff)
        {
            var buff = networkBuffs.NetworkBuffs[index];
            if (buff == null || string.IsNullOrEmpty(buff.InstanceId))
                return;

            if (!byInstanceId.TryGetValue(buff.InstanceId, out var data))
                return;

            data.TimeRemaining = buff.Remaining;
            SortOrdered();
            onChanged();
        }

        private void TryAddOrUpdate(UnitNetworkBuffs.NetworkBuffData buff, bool notify)
        {
            if (buff == null || !buff.ShowInUnitUiBuffBar || string.IsNullOrEmpty(buff.InstanceId))
                return;

            if (byInstanceId.TryGetValue(buff.InstanceId, out var existing))
            {
                UpdateBuffData(existing, buff);
            }
            else
            {
                var data = CreateBuffData(buff);
                byInstanceId.Add(buff.InstanceId, data);
                ordered.Add(data);
            }

            SortOrdered();
            if (notify)
                onChanged();
        }

        private UiBuffData CreateBuffData(UnitNetworkBuffs.NetworkBuffData buff)
        {
            var isInfinite = buff.Duration == Mathf.Infinity;
            return new UiBuffData
            {
                InstanceId = buff.InstanceId,
                BuffId = buff.BuffId,
                DisplayName = !string.IsNullOrWhiteSpace(buff.DisplayName) ? buff.DisplayName : buff.BuffId,
                IconTexture = databases?.Skills?.GetSkillByName(buff.SkillName)?.iconTexture,
                Duration = buff.Duration,
                TimeRemaining = isInfinite ? Mathf.Infinity : buff.Remaining,
                IsNegative = buff.IsNegative
            };
        }

        private static void UpdateBuffData(UiBuffData target, UnitNetworkBuffs.NetworkBuffData buff)
        {
            var isInfinite = buff.Duration == Mathf.Infinity;
            target.BuffId = buff.BuffId;
            target.DisplayName = !string.IsNullOrWhiteSpace(buff.DisplayName) ? buff.DisplayName : buff.BuffId;
            target.Duration = buff.Duration;
            target.TimeRemaining = isInfinite ? Mathf.Infinity : buff.Remaining;
            target.IsNegative = buff.IsNegative;
        }

        private void SortOrdered()
        {
            if (ordered.Count > 1)
                ordered.Sort(ByTimeRemainingDescending);
        }
    }
}
