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
        if (_loadoutManager != null)
            _loadoutManager.OnLoadoutChanged -= HandleLocalLoadoutChanged;

        if (_deferredSyncCoroutine != null)
        {
            StopCoroutine(_deferredSyncCoroutine);
            _deferredSyncCoroutine = null;
        }
    }

    public void HandleLocalLoadoutChanged(LocalLoadout newLoadout)
    {
        if (!isLocalPlayer) return;

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

        CmdRequestSetName(ApplicationSettings.GetEffectiveNickname(GameServices.Settings?.Nickname));
        var loadout = _loadoutManager != null ? _loadoutManager.Get() : GameServices.Loadout?.Get();
        SendLoadoutToServer(loadout);
        _deferredSyncCoroutine = null;
    }

    private void SendLoadoutToServer(LocalLoadout newLoadout)
    {
        if (newLoadout == null) return;

        var unitName = ApplicationSettings.GetEffectiveNickname(newLoadout.UnitName);

        CmdRequestSetLoadout(
            unitName,
            newLoadout.WeaponId,
            newLoadout.GetNormals(),
            newLoadout.UltimateId,
            newLoadout.GetPassives()
        );
    }

    [Command]
    public void CmdRequestSetName(string desiredName)
    {
        var unitController = _playerInput.myUnit?.GetComponent<UnitController>();
        if (unitController == null) return;

        // Basic anti-spam: allow 2 req/s per connection (optional: store timestamp per-conn externally)
        // This minimal implementation skips detailed rate limiting for brevity.

        // Validate name
        string sanitized = ApplicationSettings.SanitizeNickname(desiredName);
        if (sanitized.Length < ApplicationSettings.MinNicknameLength || sanitized.Length > ApplicationSettings.MaxNicknameLength)
        {
            Debug.LogWarning("Name must be 3-30 chars.");
            return;
        }

        // Apply to unit (server authoritative)
        unitController.SetUnitName(sanitized);
        Debug.Log($"Set player name to {sanitized}");
    }


    [Command]
    public void CmdRequestSetLoadout(string desiredUnitName, string desiredWeaponName, string[] desiredNormalSkills, string desiredUltimateSkill, string[] desiredPassiveSkills)
    {
        var unitController = _playerInput.myUnit?.GetComponent<UnitController>();
        if (unitController == null) return;

        // Basic anti-spam: allow 2 req/s per connection (optional: store timestamp per-conn externally)
        // This minimal implementation skips detailed rate limiting for brevity.

        // Validate name
        string sanitized = ApplicationSettings.GetEffectiveNickname(desiredUnitName);
        if (sanitized.Length < 3 || sanitized.Length > 20)
        {
            TargetAckSetLoadout(connectionToClient, false, "Name must be 3-20 chars.");
            return;
        }

        // Validate weapon
        var weaponDb = GameServices.Databases?.Weapons;
        var weaponData = weaponDb != null ? weaponDb.GetWeaponByName(desiredWeaponName) : null;
        if (weaponData == null)
        {
            TargetAckSetLoadout(connectionToClient, false, "Unknown weapon.");
            return;
        }

        // Validate skills
        var skillDb = GameServices.Databases?.Skills;
        if (skillDb == null)
        {
            TargetAckSetLoadout(connectionToClient, false, "Skill database missing.");
            return;
        }

        desiredNormalSkills = desiredNormalSkills ?? System.Array.Empty<string>();
        desiredPassiveSkills = desiredPassiveSkills ?? System.Array.Empty<string>();
        // enforce uniqueness on normal skills and cap at 3
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
        // passives (optional cap e.g. 2)
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

        // Apply to unit (server authoritative)
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
            manager = MatchGameManagerBase.ActiveInstance;
        }

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