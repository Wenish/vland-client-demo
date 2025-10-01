using System.Linq;
using Mirror;
using UnityEngine;

public class PlayerLoadout : NetworkBehaviour
{

    private PlayerInput _playerInput;

    void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlayerInput component missing on PlayerLoadout object.");
        }
    }


    [Command]
    public void CmdRequestSetLoadout(string desiredUnitName, string desiredWeaponName, string[] desiredNormalSkills, string desiredUltimateSkill, string[] desiredPassiveSkills)
    {
        var unitController = _playerInput.myUnit?.GetComponent<UnitController>();
        if (unitController == null) return;

        // Basic anti-spam: allow 2 req/s per connection (optional: store timestamp per-conn externally)
        // This minimal implementation skips detailed rate limiting for brevity.

        // Validate name
        string sanitized = (desiredUnitName ?? "Player Test").Trim();
        if (sanitized.Length < 3 || sanitized.Length > 20)
        {
            TargetAckSetLoadout(connectionToClient, false, "Name must be 3-20 chars.");
        }
        // restrict to alnum, space, _ and -
        sanitized = new string(sanitized.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-').ToArray());
        if (sanitized.Length < 3)
        {
            TargetAckSetLoadout(connectionToClient, false, "Name contains invalid characters.");
        }

        // Validate weapon
        var weaponDb = DatabaseManager.Instance.weaponDatabase;
        var weaponData = weaponDb != null ? weaponDb.GetWeaponByName(desiredWeaponName) : null;
        if (weaponData == null)
        {
            TargetAckSetLoadout(connectionToClient, false, "Unknown weapon.");
            return;
        }

        // Validate skills
        var skillDb = DatabaseManager.Instance.skillDatabase;
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
            if (skillDb.GetSkillByName(name) == null)
            {
                TargetAckSetLoadout(connectionToClient, false, $"Unknown skill: {name}");
                return;
            }
        }
        // passives (optional cap e.g. 2)
        var passiveUnique = desiredPassiveSkills.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Take(2).ToArray();
        foreach (var name in passiveUnique)
        {
            if (skillDb.GetSkillByName(name) == null)
            {
                TargetAckSetLoadout(connectionToClient, false, $"Unknown passive: {name}");
                return;
            }
        }
        if (!string.IsNullOrWhiteSpace(desiredUltimateSkill) && skillDb.GetSkillByName(desiredUltimateSkill) == null)
        {
            TargetAckSetLoadout(connectionToClient, false, $"Unknown ultimate: {desiredUltimateSkill}");
            return;
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

}