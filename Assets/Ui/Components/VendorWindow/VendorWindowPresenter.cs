using System.Collections.Generic;
using System.Threading;
using MessagePipe;
using MyGame.Events;
using MyGame.Events.Ui;
using R3;
using ShadowInfection.UI.ZombieMatch;
using UnityEngine;
using UnityEngine.InputSystem;
using Vland.UI;

namespace ShadowInfection.UI.VendorWindow
{
    internal sealed class VendorWindowPresenter
    {
        private const string PrefPosX = "VendorWindow_PosX";
        private const string PrefPosY = "VendorWindow_PosY";

        private readonly IZombieMatchUiSession zombieMatchSession;
        private readonly ISubscriber<PlayerGoldChangedEvent> goldChanged;
        private readonly ISubscriber<VendorTransactResultEvent> transactResults;
        private readonly ISubscriber<VendorSnapshotEvent> snapshots;
        private readonly ISubscriber<WaveStartedEvent> waveStarted;
        private readonly ISubscriber<RequestCloseVendorWindowEvent> closeRequested;
        private readonly IPublisher<VendorWindowVisibilityChangedEvent> visibilityChanged;
        private readonly IPublisher<SetLoadoutWindowOpenEvent> loadoutOpen;

        private VendorView view;
        private R3.DisposableBag subscriptions;
        private IVendorSession session;
        private PlayerController player;
        private VendorTab tab = VendorTab.Buy;
        private int page;
        private string selectedId;
        private readonly Dictionary<string, int> upgradeCounts = new();
        private readonly Dictionary<string, int> buyStocks = new();
        private readonly List<VendorRowVm> pageRows = new();
        private StatSystem boundStats;
        private int cachedWave = 1;

        private VendorDefinition Catalog => session != null ? session.Catalog : null;

        public bool IsOpen => view != null && view.IsOpen;

        public VendorWindowPresenter(
            IZombieMatchUiSession zombieMatchSession,
            ISubscriber<PlayerGoldChangedEvent> goldChanged,
            ISubscriber<VendorTransactResultEvent> transactResults,
            ISubscriber<VendorSnapshotEvent> snapshots,
            ISubscriber<WaveStartedEvent> waveStarted,
            ISubscriber<RequestCloseVendorWindowEvent> closeRequested,
            IPublisher<VendorWindowVisibilityChangedEvent> visibilityChanged,
            IPublisher<SetLoadoutWindowOpenEvent> loadoutOpen)
        {
            this.zombieMatchSession = zombieMatchSession;
            this.goldChanged = goldChanged;
            this.transactResults = transactResults;
            this.snapshots = snapshots;
            this.waveStarted = waveStarted;
            this.closeRequested = closeRequested;
            this.visibilityChanged = visibilityChanged;
            this.loadoutOpen = loadoutOpen;
        }

        public void Bind(VendorView nextView, CancellationToken token)
        {
            Unbind();
            view = nextView;
            if (view == null)
                return;

            view.CloseClicked += Close;
            view.PositionChanged += PersistPosition;
            view.TabClicked += SetTab;
            view.PagePrevClicked += OnPagePrev;
            view.PageNextClicked += OnPageNext;
            view.RowSelected += OnRowSelected;
            view.RowTransactRequested += OnRowTransactRequested;

            if (zombieMatchSession != null && zombieMatchSession.TryGetSnapshot(out var snapshot))
                cachedWave = Mathf.Max(1, snapshot.Wave);

            subscriptions.Add(goldChanged.Subscribe(OnGoldChanged));
            subscriptions.Add(transactResults.Subscribe(OnVendorTransactResult));
            subscriptions.Add(snapshots.Subscribe(OnVendorSnapshot));
            subscriptions.Add(waveStarted.Subscribe(OnWaveStarted));
            subscriptions.Add(closeRequested.Subscribe(_ => Close()));
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, token)
                    .Subscribe(_ => TickInput()));
        }

        public void Unbind()
        {
            var wasOpen = IsOpen;
            if (wasOpen)
                PersistPosition();

            if (view != null)
            {
                view.CloseClicked -= Close;
                view.PositionChanged -= PersistPosition;
                view.TabClicked -= SetTab;
                view.PagePrevClicked -= OnPagePrev;
                view.PageNextClicked -= OnPageNext;
                view.RowSelected -= OnRowSelected;
                view.RowTransactRequested -= OnRowTransactRequested;
            }

            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            UnbindPlayerStats();
            view = null;
            session = null;
            cachedWave = 1;
            player = null;

            if (wasOpen)
                PublishVisibility(false);
        }

        public void Open(VendorDefinition nextCatalog)
        {
            if (nextCatalog == null)
                return;

            Open(new CatalogVendorSession(nextCatalog), ResolveLocalPlayer());
        }

        public void Open(InteractionZone zone, PlayerController nextPlayer)
        {
            if (zone == null)
                return;

            Open(zone.GetVendorSession(), nextPlayer);
        }

        public void Open(IVendorSession nextSession, PlayerController nextPlayer)
        {
            if (view == null || nextSession == null || nextSession.Catalog == null)
                return;

            loadoutOpen.Publish(new SetLoadoutWindowOpenEvent(false));

            session = nextSession;
            player = nextPlayer != null ? nextPlayer : ResolveLocalPlayer();
            tab = nextSession.OpeningTab;
            page = 0;
            selectedId = null;
            upgradeCounts.Clear();
            buyStocks.Clear();

            var catalog = nextSession.Catalog;
            view.SetVendor(catalog.DisplayName, catalog.subtitle, catalog.portrait);
            view.SetOpen(true);
            PublishVisibility(true);
            RestorePosition();
            BindPlayerStats();
            Refresh();

            if (player != null && player.isLocalPlayer)
            {
                player.CmdBeginVendorTrade(nextSession.VendorId);
                player.CmdRequestVendorSnapshot(nextSession.VendorId);
            }
        }

        public void Close()
        {
            if (view == null || !view.IsOpen)
                return;

            PersistPosition();
            if (player != null && player.isLocalPlayer)
                player.CmdEndVendorTrade();
            UnbindPlayerStats();
            session = null;
            selectedId = null;
            view.SetOpen(false);
            PublishVisibility(false);
        }

        public void CloseIfInteractable(IVendorInteractable interactable)
        {
            if (interactable != null && session != null && session.BelongsToInteractable(interactable))
                Close();
        }

        public void CloseIfZone(InteractionZone zone)
        {
            CloseIfInteractable(zone);
        }

        private void BindPlayerStats()
        {
            UnbindPlayerStats();
            var unit = player != null ? player.GetControlledUnit() : null;
            boundStats = unit != null && unit.unitMediator != null ? unit.unitMediator.Stats : null;
            if (boundStats != null)
                boundStats.OnStatChanged += OnBoundStatChanged;
        }

        private void UnbindPlayerStats()
        {
            if (boundStats != null)
                boundStats.OnStatChanged -= OnBoundStatChanged;
            boundStats = null;
        }

        private void OnBoundStatChanged(StatType _)
        {
            if (IsOpen)
                Refresh();
        }

        private void TickInput()
        {
            if (!IsOpen || Keyboard.current == null)
                return;

            if (Keyboard.current.tabKey.wasPressedThisFrame)
                SetTab(NextVisibleTab());
        }

        private void PublishVisibility(bool isOpen)
        {
            visibilityChanged.Publish(new VendorWindowVisibilityChangedEvent(isOpen));
        }

        private void SetTab(VendorTab nextTab)
        {
            if (Catalog != null && !Catalog.IsTabEnabled(nextTab))
                return;

            if (tab == nextTab && IsOpen)
            {
                Refresh();
                return;
            }

            tab = nextTab;
            page = 0;
            selectedId = null;
            Refresh();
        }

        private void OnPagePrev() => ChangePage(-1);

        private void OnPageNext() => ChangePage(1);

        private void ChangePage(int delta)
        {
            var rows = BuildRows();
            var pageCount = Mathf.Max(1, Mathf.CeilToInt(rows.Count / (float)VendorView.RowsPerPage));
            var next = Mathf.Clamp(page + delta, 0, pageCount - 1);
            if (next == page)
                return;

            page = next;
            selectedId = null;
            RefreshVisibleRows(rows);
        }

        private void OnRowSelected(string id)
        {
            var rows = BuildRows();
            var model = FindRow(rows, id);
            if (model == null || model.Locked)
                return;

            selectedId = id;
            RefreshVisibleRows(rows);
        }

        private void OnRowTransactRequested(string id)
        {
            if (player == null || session == null)
                return;

            var rows = BuildRows();
            var model = FindRow(rows, id);
            if (model == null || model.Locked)
                return;

            selectedId = id;
            if (ShadowInfection.DI.GameLifetimeScope.TryResolve<ShadowInfection.Audio.ISfxPlayer>(out var sfx))
                sfx.Play(ShadowInfection.Audio.SfxPlayer.Ids.UiButtonClick);
            player.CmdVendorTransact(session.VendorId, (byte)model.Tab, model.Id);
            RefreshVisibleRows(rows);
        }

        private void OnVendorTransactResult(VendorTransactResultEvent evt)
        {
            if (!IsOpen || evt == null || player == null || evt.Buyer != player)
                return;

            if (!string.IsNullOrEmpty(evt.EntryId) && evt.TimesBought > 0)
                upgradeCounts[evt.EntryId] = evt.TimesBought;

            Refresh();
            if (player != null && player.isLocalPlayer && session != null)
                player.CmdRequestVendorSnapshot(session.VendorId);
        }

        private void OnVendorSnapshot(VendorSnapshotEvent evt)
        {
            if (!IsOpen || evt == null || player == null)
                return;
            if (evt.Buyer != null && evt.Buyer != player)
                return;

            upgradeCounts.Clear();
            var count = Mathf.Min(evt.UpgradeIds.Length, evt.PurchaseCounts.Length);
            for (var i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(evt.UpgradeIds[i]))
                    upgradeCounts[evt.UpgradeIds[i]] = evt.PurchaseCounts[i];
            }

            buyStocks.Clear();
            if (evt.BuyIds != null && evt.BuyStocks != null)
            {
                var buyCount = Mathf.Min(evt.BuyIds.Length, evt.BuyStocks.Length);
                for (var i = 0; i < buyCount; i++)
                {
                    if (!string.IsNullOrEmpty(evt.BuyIds[i]))
                        buyStocks[evt.BuyIds[i]] = evt.BuyStocks[i];
                }
            }

            Refresh();
        }

        private void OnGoldChanged(PlayerGoldChangedEvent evt)
        {
            if (!IsOpen || evt == null || player == null || evt.Player != player)
                return;

            Refresh();
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            if (evt != null)
                cachedWave = Mathf.Max(1, evt.WaveNumber);

            if (IsOpen)
                Refresh();
        }

        private void Refresh()
        {
            if (view == null || Catalog == null)
                return;

            view.SetTabVisibility(
                Catalog.IsTabEnabled(VendorTab.Buy),
                Catalog.IsTabEnabled(VendorTab.Sell),
                Catalog.IsTabEnabled(VendorTab.Upgrades));
            if (!IsTabAvailable(tab))
                tab = FirstAvailableTab();

            view.SetActiveTab(tab);
            view.SetHint(HintFor(tab));
            view.SetGold(player != null ? player.Gold : 0);

            var rows = BuildRows();
            var pageCount = Mathf.Max(1, Mathf.CeilToInt(rows.Count / (float)VendorView.RowsPerPage));
            page = Mathf.Clamp(page, 0, pageCount - 1);

            if (tab == VendorTab.Sell)
                view.SetBuybackVisible(true);
            else
                view.SetPage(page, pageCount, true);

            RefreshVisibleRows(rows);
        }

        private void RefreshVisibleRows(List<VendorRowVm> rows)
        {
            pageRows.Clear();
            if (tab != VendorTab.Sell)
            {
                var start = page * VendorView.RowsPerPage;
                var end = Mathf.Min(rows.Count, start + VendorView.RowsPerPage);
                for (var i = start; i < end; i++)
                    pageRows.Add(rows[i]);
            }

            view.SetRows(pageRows, selectedId, EmptyMessageFor(tab));
        }

        private int ResolveCurrentWave()
        {
            if (zombieMatchSession != null && zombieMatchSession.TryGetSnapshot(out var snapshot))
                return Mathf.Max(1, snapshot.Wave);

            return Mathf.Max(1, cachedWave);
        }

        private List<VendorRowVm> BuildRows()
        {
            var rows = new List<VendorRowVm>();
            if (Catalog == null)
                return rows;

            switch (tab)
            {
                case VendorTab.Buy:
                    BuildBuyRows(rows);
                    break;
                case VendorTab.Upgrades:
                    BuildUpgradeRows(rows);
                    break;
            }

            return rows;
        }

        private void BuildBuyRows(List<VendorRowVm> rows)
        {
            if (Catalog.buyEntries == null)
                return;

            var gold = player != null ? player.Gold : 0;
            foreach (var entry in Catalog.buyEntries)
            {
                if (entry == null || entry.weapon == null)
                    continue;

                var stock = ResolveBuyStock(entry.ResolvedId);
                var soldOut = stock == 0;
                var shortfall = Mathf.Max(0, entry.goldCost - gold);
                var unaffordable = !soldOut && shortfall > 0;
                string subtitle;
                if (soldOut)
                    subtitle = "Sold out";
                else if (unaffordable)
                    subtitle = $"{shortfall} short";
                else
                    subtitle = entry.weapon.weaponType.ToString();

                rows.Add(new VendorRowVm
                {
                    Id = entry.ResolvedId,
                    Tab = VendorTab.Buy,
                    Icon = entry.weapon.iconTexture,
                    IconClass = "vendor-row__icon--weapon",
                    Name = entry.weapon.weaponName,
                    Subtitle = subtitle,
                    TypeLine = entry.weapon.weaponType.ToString(),
                    StatBlock = $"Damage: +{entry.weapon.attackPower} · Range: {entry.weapon.attackRange}",
                    PriceGold = entry.goldCost,
                    StackCount = stock > 1 ? stock : 1,
                    Dimmed = soldOut || unaffordable,
                    Locked = soldOut,
                    CanTransact = !soldOut,
                    PriceNote = soldOut ? "sold out" : null,
                    TooltipAction = "Right-click to buy"
                });
            }
        }

        private void BuildUpgradeRows(List<VendorRowVm> rows)
        {
            if (Catalog.upgradeEntries == null)
                return;

            var gold = player != null ? player.Gold : 0;
            var wave = ResolveCurrentWave();
            var stats = ResolveStats();

            foreach (var upgrade in Catalog.upgradeEntries)
            {
                if (upgrade == null)
                    continue;

                var locked = !upgrade.IsUnlockedAtWave(wave);
                var shortfall = Mathf.Max(0, upgrade.baseGoldCost - gold);
                var unaffordable = !locked && shortfall > 0;
                upgradeCounts.TryGetValue(upgrade.upgradeId, out var timesBought);

                var modifier = upgrade.statModifiers != null && upgrade.statModifiers.Count > 0
                    ? upgrade.statModifiers[0]
                    : null;
                var current = 0f;
                var after = 0f;
                var statName = "Stat";
                if (modifier != null && stats != null)
                {
                    statName = modifier.Type.ToString();
                    current = stats.GetStat(modifier.Type);
                    after = modifier.ModifierType == ModifierType.Percent
                        ? current * modifier.Value
                        : current + modifier.Value;
                }
                else if (modifier != null)
                {
                    statName = modifier.Type.ToString();
                    after = modifier.Value;
                }

                string subtitle;
                if (locked)
                    subtitle = $"Requires wave {upgrade.minWaveToUnlock}";
                else if (unaffordable)
                    subtitle = $"{shortfall} short";
                else
                    subtitle = $"{statName} {FormatStat(current)} → {FormatStat(after)} · bought {timesBought}×";

                rows.Add(new VendorRowVm
                {
                    Id = upgrade.upgradeId,
                    Tab = VendorTab.Upgrades,
                    IconClass = IconClassFor(modifier),
                    Name = upgrade.DisplayName,
                    Subtitle = subtitle,
                    Flavour = upgrade.description,
                    TypeLine = "Permanent upgrade",
                    StatBlock = $"{statName} {FormatStat(current)} → {FormatStat(after)}",
                    PriceGold = upgrade.baseGoldCost,
                    StackCount = 1,
                    Dimmed = locked || unaffordable,
                    Locked = locked,
                    CanTransact = !locked,
                    PriceNote = locked ? "unavailable" : null,
                    TooltipAction = "Right-click to buy"
                });
            }
        }

        private StatSystem ResolveStats()
        {
            var unit = player != null ? player.GetControlledUnit() : null;
            return unit != null && unit.unitMediator != null ? unit.unitMediator.Stats : null;
        }

        private static string IconClassFor(StatModifier modifier)
        {
            if (modifier == null)
                return "vendor-row__icon--upgrade";

            switch (modifier.Type)
            {
                case StatType.Health:
                    return "vendor-row__icon--health";
                case StatType.AttackPower:
                    return "vendor-row__icon--attack";
                case StatType.Armor:
                case StatType.MagicResist:
                    return "vendor-row__icon--armor";
                default:
                    return "vendor-row__icon--upgrade";
            }
        }

        private static VendorRowVm FindRow(List<VendorRowVm> rows, string id)
        {
            if (string.IsNullOrEmpty(id) || rows == null)
                return null;

            foreach (var row in rows)
            {
                if (row.Id == id)
                    return row;
            }

            return null;
        }

        private static string HintFor(VendorTab currentTab)
        {
            switch (currentTab)
            {
                case VendorTab.Sell:
                    return "Nothing to sell yet.";
                case VendorTab.Upgrades:
                    return "Permanent stat boosts. Right-click to buy.";
                default:
                    return "Right-click to buy. Hover for details.";
            }
        }

        private static string EmptyMessageFor(VendorTab currentTab)
        {
            switch (currentTab)
            {
                case VendorTab.Sell:
                    return "Nothing to sell.";
                case VendorTab.Upgrades:
                    return "No upgrades available.";
                default:
                    return "No items for sale.";
            }
        }

        private static string FormatStat(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.#");
        }

        private static PlayerController ResolveLocalPlayer()
        {
            if (Mirror.NetworkClient.localPlayer == null)
                return null;
            return Mirror.NetworkClient.localPlayer.GetComponent<PlayerController>();
        }

        private bool IsTabAvailable(VendorTab candidate)
        {
            return Catalog != null && Catalog.IsTabEnabled(candidate);
        }

        private VendorTab FirstAvailableTab()
        {
            return Catalog != null ? Catalog.ResolveDefaultTab() : VendorTab.Buy;
        }

        private VendorTab NextVisibleTab()
        {
            var current = (int)tab;
            for (var i = 1; i <= 3; i++)
            {
                var candidate = (VendorTab)((current + i) % 3);
                if (IsTabAvailable(candidate))
                    return candidate;
            }

            return tab;
        }

        private int ResolveBuyStock(string entryId)
        {
            if (buyStocks.TryGetValue(entryId, out var stock))
                return stock;
            return session != null ? session.GetBuyStock(entryId) : VendorStock.Unlimited;
        }

        private void RestorePosition()
        {
            if (view == null)
                return;

            if (TryReadSavedPosition(out var left, out var top))
                view.ApplyPosition(left, top);
            else
                view.ApplyDefaultPosition();
        }

        private void PersistPosition()
        {
            if (view == null || !view.IsOpen || !view.HasUsableLayout())
                return;

            var pos = view.GetPosition();
            PlayerPrefs.SetFloat(PrefPosX, pos.x);
            PlayerPrefs.SetFloat(PrefPosY, pos.y);
        }

        private static bool TryReadSavedPosition(out float left, out float top)
        {
            left = 0f;
            top = 0f;
            if (!PlayerPrefs.HasKey(PrefPosX) || !PlayerPrefs.HasKey(PrefPosY))
                return false;

            left = PlayerPrefs.GetFloat(PrefPosX);
            top = PlayerPrefs.GetFloat(PrefPosY);
            return left > 0f || top > 0f;
        }
    }
}
