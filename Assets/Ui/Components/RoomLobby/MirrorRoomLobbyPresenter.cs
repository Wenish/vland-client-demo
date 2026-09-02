using System;
using System.Collections.Generic;

namespace ShadowInfection.UI.RoomLobby
{
    internal sealed class MirrorRoomLobbyPresenter
    {
        private readonly RoomLobbySettings settings;
        private readonly IRoomLobbySession session;

        private RoomLobbyView view;
        private float nextRefreshTime;
        private bool enabled;
        private int cachedHash;
        private int cachedCharacterHash;
        private bool selectOverlayOpen;
        private bool createOverlayOpen;
        private bool deleteConfirmOpen;
        private bool characterOverlayForced;
        private bool awaitingSelectionSync;
        private string pendingDeleteCharacterId;
        private string pendingDeleteCharacterName;

        public MirrorRoomLobbyPresenter(RoomLobbySettings settings, IRoomLobbySession session)
        {
            this.settings = settings;
            this.session = session;
        }

        public void Bind(RoomLobbyView nextView)
        {
            if (view != null)
            {
                view.ReadyButtonClicked -= OnReadyButtonClicked;
                view.ChangeCharacterClicked -= OnChangeCharacterClicked;
                view.CloseCharacterOverlayClicked -= OnCloseCharacterOverlayClicked;
                view.OpenCreateCharacterClicked -= OnOpenCreateCharacterClicked;
                view.BackFromCreateClicked -= OnBackFromCreateClicked;
                view.CharacterSelected -= OnCharacterSelected;
                view.CreateCharacterClicked -= OnCreateCharacterClicked;
                view.CharacterDeleteRequested -= OnCharacterDeleteRequested;
                view.ConfirmDeleteCharacterClicked -= OnConfirmDeleteCharacterClicked;
                view.CancelDeleteCharacterClicked -= OnCancelDeleteCharacterClicked;
                view.SetCharacterSelectOverlayVisible(false);
                view.SetCharacterCreateOverlayVisible(false);
                view.SetCharacterDeleteConfirmVisible(false, null);
            }

            view = nextView;
            selectOverlayOpen = false;
            createOverlayOpen = false;
            deleteConfirmOpen = false;
            characterOverlayForced = false;
            awaitingSelectionSync = false;
            pendingDeleteCharacterId = null;
            pendingDeleteCharacterName = null;
            cachedHash = 0;
            cachedCharacterHash = 0;

            if (view != null)
            {
                view.ReadyButtonClicked += OnReadyButtonClicked;
                view.ChangeCharacterClicked += OnChangeCharacterClicked;
                view.CloseCharacterOverlayClicked += OnCloseCharacterOverlayClicked;
                view.OpenCreateCharacterClicked += OnOpenCreateCharacterClicked;
                view.BackFromCreateClicked += OnBackFromCreateClicked;
                view.CharacterSelected += OnCharacterSelected;
                view.CreateCharacterClicked += OnCreateCharacterClicked;
                view.CharacterDeleteRequested += OnCharacterDeleteRequested;
                view.ConfirmDeleteCharacterClicked += OnConfirmDeleteCharacterClicked;
                view.CancelDeleteCharacterClicked += OnCancelDeleteCharacterClicked;
                view.SetCharacterSelectOverlayVisible(false);
                view.SetCharacterCreateOverlayVisible(false);
                view.SetCharacterDeleteConfirmVisible(false, null);
            }
        }

        public void Unbind()
        {
            Bind(null);
        }

        public void SetEnabled(bool value)
        {
            enabled = value;
        }

        public void Tick(float unscaledTime)
        {
            if (!enabled || view == null || unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = unscaledTime + settings.RefreshIntervalSeconds;

            if (!session.TryGetState(out var state))
            {
                view.SetVisible(false);
                view.SetReadyButtonEnabled(false);
                view.SetChangeCharacterButtonEnabled(false);
                CloseAllOverlays();
                awaitingSelectionSync = false;
                return;
            }

            if (!state.IsInRoomScene)
            {
                view.SetVisible(false);
                CloseAllOverlays();
                awaitingSelectionSync = false;
                return;
            }

            view.SetVisible(true);
            view.SetReadyButtonEnabled(state.CanToggleReady);
            view.SetLocalReadyState(state.LocalIsReady);
            view.SetChangeCharacterButtonEnabled(state.CanEditCharacter);
            view.SetCharacterControlsEnabled(state.CanEditCharacter && !deleteConfirmOpen);
            view.SetCreateCharacterButtonVisible(state.CanCreateCharacter && state.CanEditCharacter);

            if (state.HasSelectedCharacter)
                awaitingSelectionSync = false;

            if (!state.HasSelectedCharacter && state.CanEditCharacter && !awaitingSelectionSync)
            {
                // Keep create/delete confirm open if the user navigated there; otherwise force select.
                characterOverlayForced = true;
                if (!createOverlayOpen && !deleteConfirmOpen)
                {
                    if (!selectOverlayOpen)
                        cachedCharacterHash = 0;
                    selectOverlayOpen = true;
                }
            }
            else if (state.LocalIsReady || !state.CanEditCharacter)
            {
                selectOverlayOpen = false;
                createOverlayOpen = false;
                deleteConfirmOpen = false;
                characterOverlayForced = false;
                ClearPendingDelete();
            }

            ApplyOverlayVisibility(state);

            var snapshotHash = ComputeSnapshotHash(state.Players);
            if (snapshotHash != cachedHash)
            {
                cachedHash = snapshotHash;
                view.SetPlayers(state.Players);
            }

            var characterHash = ComputeCharacterHash(state.Characters);
            if (characterHash != cachedCharacterHash)
            {
                cachedCharacterHash = characterHash;
                view.SetCharacters(state.Characters);
            }
        }

        private void ApplyOverlayVisibility(RoomLobbyState state)
        {
            view.SetCharacterDeleteConfirmVisible(deleteConfirmOpen, pendingDeleteCharacterName);
            view.SetCharacterSelectOverlayVisible(selectOverlayOpen && !createOverlayOpen);
            view.SetCharacterCreateOverlayVisible(createOverlayOpen);
            view.SetCharacterOverlayCanClose(
                selectOverlayOpen
                && !createOverlayOpen
                && !deleteConfirmOpen
                && state.HasSelectedCharacter
                && !characterOverlayForced);
        }

        private void CloseAllOverlays()
        {
            selectOverlayOpen = false;
            createOverlayOpen = false;
            deleteConfirmOpen = false;
            characterOverlayForced = false;
            ClearPendingDelete();
            view?.SetCharacterSelectOverlayVisible(false);
            view?.SetCharacterCreateOverlayVisible(false);
            view?.SetCharacterDeleteConfirmVisible(false, null);
        }

        private void ClearPendingDelete()
        {
            pendingDeleteCharacterId = null;
            pendingDeleteCharacterName = null;
        }

        private void OnReadyButtonClicked()
        {
            session.ToggleLocalReady();
        }

        private void OnChangeCharacterClicked()
        {
            if (!session.TryGetState(out var state) || !state.CanEditCharacter)
                return;

            selectOverlayOpen = true;
            createOverlayOpen = false;
            deleteConfirmOpen = false;
            ClearPendingDelete();
            characterOverlayForced = !state.HasSelectedCharacter;
            ApplyOverlayVisibility(state);
            cachedCharacterHash = 0;
        }

        private void OnCloseCharacterOverlayClicked()
        {
            if (!session.TryGetState(out var state) || !state.HasSelectedCharacter)
                return;

            CloseAllOverlays();
        }

        private void OnOpenCreateCharacterClicked()
        {
            if (!session.TryGetState(out var state) || !state.CanEditCharacter || !state.CanCreateCharacter)
                return;

            createOverlayOpen = true;
            selectOverlayOpen = true;
            deleteConfirmOpen = false;
            ClearPendingDelete();
            ApplyOverlayVisibility(state);
        }

        private void OnBackFromCreateClicked()
        {
            createOverlayOpen = false;
            selectOverlayOpen = true;
            deleteConfirmOpen = false;
            ClearPendingDelete();
            if (session.TryGetState(out var state))
                ApplyOverlayVisibility(state);
            else
            {
                view?.SetCharacterCreateOverlayVisible(false);
                view?.SetCharacterSelectOverlayVisible(true);
                view?.SetCharacterDeleteConfirmVisible(false, null);
            }
        }

        private void OnCharacterSelected(string characterId)
        {
            if (deleteConfirmOpen)
                return;

            if (!session.TryGetState(out var state) || !state.CanEditCharacter)
                return;

            // Already playing this character — just close the overlay.
            for (var i = 0; i < state.Characters.Count; i++)
            {
                if (state.Characters[i].id == characterId && state.Characters[i].isSelected)
                {
                    CloseAllOverlays();
                    return;
                }
            }

            if (!session.SelectCharacter(characterId))
                return;

            awaitingSelectionSync = true;
            CloseAllOverlays();
            cachedCharacterHash = 0;
        }

        private void OnCreateCharacterClicked()
        {
            if (view == null)
                return;

            if (!session.CreateCharacter(view.CharacterNameInput, view.SelectedGender))
                return;

            view.ClearCharacterNameInput();
            awaitingSelectionSync = true;
            CloseAllOverlays();
            cachedCharacterHash = 0;
        }

        private void OnCharacterDeleteRequested(string characterId)
        {
            if (!session.TryGetState(out var state) || !state.CanEditCharacter)
                return;

            string displayName = null;
            for (var i = 0; i < state.Characters.Count; i++)
            {
                if (state.Characters[i].id == characterId)
                {
                    displayName = state.Characters[i].displayName;
                    break;
                }
            }

            if (string.IsNullOrEmpty(displayName))
                return;

            pendingDeleteCharacterId = characterId;
            pendingDeleteCharacterName = displayName;
            deleteConfirmOpen = true;
            createOverlayOpen = false;
            selectOverlayOpen = true;
            ApplyOverlayVisibility(state);
        }

        private void OnCancelDeleteCharacterClicked()
        {
            deleteConfirmOpen = false;
            ClearPendingDelete();
            selectOverlayOpen = true;
            if (session.TryGetState(out var state))
                ApplyOverlayVisibility(state);
            else
                view?.SetCharacterDeleteConfirmVisible(false, null);
        }

        private void OnConfirmDeleteCharacterClicked()
        {
            if (string.IsNullOrEmpty(pendingDeleteCharacterId))
            {
                OnCancelDeleteCharacterClicked();
                return;
            }

            var deletedId = pendingDeleteCharacterId;
            if (!session.DeleteCharacter(deletedId))
            {
                OnCancelDeleteCharacterClicked();
                return;
            }

            deleteConfirmOpen = false;
            ClearPendingDelete();
            createOverlayOpen = false;
            selectOverlayOpen = true;
            characterOverlayForced = true;
            cachedCharacterHash = 0;

            if (session.TryGetState(out var state))
                ApplyOverlayVisibility(state);
            else
            {
                view?.SetCharacterDeleteConfirmVisible(false, null);
                view?.SetCharacterSelectOverlayVisible(true);
            }
        }

        private static int ComputeSnapshotHash(IReadOnlyList<PlayerRowVm> snapshot)
        {
            if (snapshot == null)
                return 0;

            unchecked
            {
                var hash = 17;
                for (var i = 0; i < snapshot.Count; i++)
                {
                    hash = (hash * 31) + (int)snapshot[i].netId;
                    hash = (hash * 31) + snapshot[i].index;
                    hash = (hash * 31) + (snapshot[i].ready ? 1 : 0);
                    hash = (hash * 31) + (snapshot[i].isLocal ? 1 : 0);
                    hash = (hash * 31) + (snapshot[i].displayName?.GetHashCode() ?? 0);
                }

                return hash;
            }
        }

        private static int ComputeCharacterHash(IReadOnlyList<CharacterRowVm> snapshot)
        {
            if (snapshot == null)
                return 0;

            unchecked
            {
                var hash = 17;
                for (var i = 0; i < snapshot.Count; i++)
                {
                    hash = (hash * 31) + (snapshot[i].id?.GetHashCode() ?? 0);
                    hash = (hash * 31) + (snapshot[i].displayName?.GetHashCode() ?? 0);
                    hash = (hash * 31) + (snapshot[i].genderLabel?.GetHashCode() ?? 0);
                    hash = (hash * 31) + (snapshot[i].isSelected ? 1 : 0);
                }

                return hash;
            }
        }
    }
}
