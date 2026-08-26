using System.Linq;
using Mirror;
using ShadowInfection.DI;
using UnityEngine;

public class PlayerLoadout : NetworkBehaviour
{

    private PlayerInput _playerInput;
    private Coroutine _deferredSyncCoroutine;

    private LoadoutManager _loadoutManager;

    void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlayerInput component missing on PlayerLoadout object.");
        }
    }

    public override void OnStartLocalPlayer()
    {
        EnsurePlayerInput();
        _loadoutManager = GameServices.Loadout;
        if (_loadoutManager != null)
            _loadoutManager.OnLoadoutChanged += HandleLocalLoadoutChanged;
        RestartDeferredSync();
    }

    public override void OnStopLocalPlayer()
    {
        UnsubscribeLoadoutChanged();

        if (_deferredSyncCoroutine != null)
        {
            StopCoroutine(_deferredSyncCoroutine);
            _deferredSyncCoroutine = null;
        }
    }

    void OnDestroy()
    {
        UnsubscribeLoadoutChanged();
    }

    void UnsubscribeLoadoutChanged()
    {
        if (_loadoutManager == null) return;
        _loadoutManager.OnLoadoutChanged -= HandleLocalLoadoutChanged;
        _loadoutManager = null;
    }

    public void HandleLocalLoadoutChanged(LocalLoadout newLoadout)
    {
        // Stale handler: LoadoutManager is DontDestroyOnLoad and may outlive this player.
        if (this == null || !isLocalPlayer) return;

        EnsurePlayerInput();
        if (_playerInput == null || _playerInput.myUnit == null)
        {
            RestartDeferredSync();
            return;
        }

        SendLoadoutToServer(newLoadout);
    }

    private void EnsurePlayerInput()
    {
        if (this == null) return;
        if (_playerInput != null) return;

        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlayerInput component missing on PlayerLoadout object.");
        }
    }

    private void RestartDeferredSync()
    {
        if (!isLocalPlayer) return;

        if (_deferredSyncCoroutine != null)
        {
            StopCoroutine(_deferredSyncCoroutine);
        }

        _deferredSyncCoroutine = StartCoroutine(DeferredSyncWhenUnitReady());
    }

    private System.Collections.IEnumerator DeferredSyncWhenUnitReady()
    {
        while (isLocalPlayer)
        {
            EnsurePlayerInput();
            if (_playerInput != null && _playerInput.myUnit != null)
            {
                break;
            }

            yield return null;
        }

        if (!isLocalPlayer)
        {
            _deferredSyncCoroutine = null;
            yield break;
        }

        CmdRequestSetName(GetLocalCharacterDisplayName());
        var loadout = _loadoutManager != null ? _loadoutManager.Get() : GameServices.Loadout?.Get();
        SendLoadoutToServer(loadout);
        _deferredSyncCoroutine = null;
    }

    private static string GetLocalCharacterDisplayName()
    {
        var active = GameServices.Characters?.GetActive();
        if (active != null && !string.IsNullOrWhiteSpace(active.Name))
            return ApplicationSettings.SanitizeNickname(active.Name);

        var loadoutName = GameServices.Loadout?.Get()?.UnitName;
        return ApplicationSettings.SanitizeNickname(loadoutName);
    }

    private void SendLoadoutToServer(LocalLoadout newLoadout)
    {
        if (newLoadout == null) return;

        var characterName = GetLocalCharacterDisplayName();
        if (string.IsNullOrWhiteSpace(characterName))
            characterName = ApplicationSettings.SanitizeNickname(newLoadout.UnitName);

        if (string.IsNullOrWhiteSpace(characterName)
            || characterName.Length < ApplicationSettings.MinNicknameLength)
        {
            Debug.LogWarning("Cannot sync loadout without a valid character name.");
            return;
        }

        var modelName = GameServices.Characters != null
            ? GameServices.Characters.GetActiveModelName()
            : CharacterManager.MaleModelName;

        CmdRequestSetLoadout(
            characterName,
            newLoadout.WeaponId,
            newLoadout.GetNormals(),
            newLoadout.UltimateId,
            newLoadout.GetPassives(),
            modelName
        );
    }

    [Command]
    public void CmdRequestSetName(string desiredName)
    {
        var unitController = _playerInput.myUnit?.GetComponent<UnitController>();
        if (unitController == null) return;

        string sanitized = ApplicationSettings.SanitizeNickname(desiredName);
        if (sanitized.Length < ApplicationSettings.MinNicknameLength || sanitized.Length > ApplicationSettings.MaxNicknameLength)
        {
            Debug.LogWarning("Character name must be 3-30 chars.");
            return;
        }

        unitController.SetUnitName(sanitized);
        Debug.Log($"Set character name to {sanitized}");
    }


    [Command]
    public void CmdRequestSetLoadout(
        string desiredUnitName,
        string desiredWeaponName,
        string[] desiredNormalSkills,
        string desiredUltimateSkill,
        string[] desiredPassiveSkills,
        string desiredModelName)
    {
        var unitController = _playerInput.myUnit?.GetComponent<UnitController>();
        if (unitController == null) return;

        string sanitized = ApplicationSettings.SanitizeNickname(desiredUnitName);
        if (sanitized.Length < ApplicationSettings.MinNicknameLength || sanitized.Length > ApplicationSettings.MaxNicknameLength)
        {
            TargetAckSetLoadout(connectionToClient, false, "Character name must be 3-30 chars.");
            return;
        }

        var weaponDb = GameServices.Databases?.Weapons;
        var weaponData = weaponDb != null ? weaponDb.GetWeaponByName(desiredWeaponName) : null;
        if (weaponData == null)
        {
            TargetAckSetLoadout(connectionToClient, false, "Unknown weapon.");
            return;
        }

        var skillDb = GameServices.Databases?.Skills;
        if (skillDb == null)
        {
            TargetAckSetLoadout(connectionToClient, false, "Skill database missing.");
            return;
        }

        desiredNormalSkills = desiredNormalSkills ?? System.Array.Empty<string>();
        desiredPassiveSkills = desiredPassiveSkills ?? System.Array.Empty<string>();
        var normalUnique = desiredNormalSkills.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Take(3).ToArray();
        foreach (var name in normalUnique)
        {
            var skill = skillDb.GetSkillByName(name);
            if (skill == null)
            {
                TargetAckSetLoadout(connectionToClient, false, $"Unknown skill: {name}");
                return;
            }

            if (!skill.CanBeUsedWithWeapon(weaponData.weaponType))
            {
                TargetAckSetLoadout(connectionToClient, false, $"Skill '{name}' requires weapon: {skill.GetRequiredWeaponLabel()}");
                return;
            }
        }
        var passiveUnique = desiredPassiveSkills.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Take(2).ToArray();
        foreach (var name in passiveUnique)
        {
            var skill = skillDb.GetSkillByName(name);
            if (skill == null)
            {
                TargetAckSetLoadout(connectionToClient, false, $"Unknown passive: {name}");
                return;
            }

            if (!skill.CanBeUsedWithWeapon(weaponData.weaponType))
            {
                TargetAckSetLoadout(connectionToClient, false, $"Passive '{name}' requires weapon: {skill.GetRequiredWeaponLabel()}");
                return;
            }
        }
        if (!string.IsNullOrWhiteSpace(desiredUltimateSkill))
        {
            var ultimate = skillDb.GetSkillByName(desiredUltimateSkill);
            if (ultimate == null)
            {
                TargetAckSetLoadout(connectionToClient, false, $"Unknown ultimate: {desiredUltimateSkill}");
                return;
            }

            if (!ultimate.CanBeUsedWithWeapon(weaponData.weaponType))
            {
                TargetAckSetLoadout(connectionToClient, false, $"Ultimate '{desiredUltimateSkill}' requires weapon: {ultimate.GetRequiredWeaponLabel()}");
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(desiredModelName))
        {
            var modelDb = GameServices.Databases?.Models;
            if (modelDb != null && modelDb.GetModelByName(desiredModelName) != null)
                unitController.EquipModel(desiredModelName);
        }

        unitController.unitName = sanitized;
        unitController.EquipWeapon(weaponData.weaponName);
        var skills = unitController.unitMediator.Skills;
        skills.ReplaceLoadout(passiveUnique, normalUnique, new[] { desiredUltimateSkill });

        TargetAckSetLoadout(connectionToClient, true, null);
    }

    [TargetRpc]
    private void TargetAckSetLoadout(NetworkConnection target, bool ok, string error)
    {
        // Persist minimal local feedback for any UI to pick up.
        _lastLoadoutOk = ok;
        _lastLoadoutError = error;
        if (!ok && !string.IsNullOrEmpty(error))
        {
            Debug.LogWarning($"Loadout rejected: {error}");
        }
    }

    // Local-only feedback state (not networked)
    private bool _lastLoadoutOk = true;
    private string _lastLoadoutError = null;
    public bool LastLoadoutOk => _lastLoadoutOk;
    public string LastLoadoutError => _lastLoadoutError;

    private bool _lastTeamSelectionOk = true;
    private string _lastTeamSelectionError = null;
    private int _lastRequestedTeamId = -1;

    public bool LastTeamSelectionOk => _lastTeamSelectionOk;
    public string LastTeamSelectionError => _lastTeamSelectionError;
    public int LastRequestedTeamId => _lastRequestedTeamId;

    public void RequestChooseTeam(int desiredTeamId)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        _lastRequestedTeamId = desiredTeamId;
        CmdRequestChooseTeam(desiredTeamId);
    }

    [Command]
    private void CmdRequestChooseTeam(int desiredTeamId)
    {
        MatchGameManagerBase manager = GameServices.Match;
        if (manager == null)
        {
            TargetAckTeamSelection(connectionToClient, false, "No active match manager in scene.", desiredTeamId);
            return;
        }

        bool ok = manager.ServerTryChooseTeam(connectionToClient.connectionId, desiredTeamId, out string reason);
        TargetAckTeamSelection(connectionToClient, ok, reason, desiredTeamId);
    }

    [TargetRpc]
    private void TargetAckTeamSelection(NetworkConnection target, bool ok, string error, int desiredTeamId)
    {
        _lastTeamSelectionOk = ok;
        _lastTeamSelectionError = error;
        _lastRequestedTeamId = desiredTeamId;

        if (!ok && !string.IsNullOrEmpty(error))
        {
            Debug.LogWarning($"Team selection rejected: {error}");
        }
    }

}