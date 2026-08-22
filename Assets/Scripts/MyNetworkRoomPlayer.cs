using UnityEngine;
using Mirror;
using ShadowInfection.DI;

/// <summary>
/// Room player with character selection for the lobby.
/// </summary>
public class MyNetworkRoomPlayer : NetworkRoomPlayer
{
    [SyncVar]
    public string nickName = "";

    [SyncVar]
    public string selectedCharacterId = "";

    [SyncVar]
    public string characterName = "";

    [SyncVar]
    public CharacterGender characterGender;

    [SyncVar]
    public string modelName = "";

    public bool HasSelectedCharacter => !string.IsNullOrEmpty(selectedCharacterId);

    public override void OnStartLocalPlayer()
    {
        // Character name is set via CmdSelectCharacter; account nickname is unused for display.
    }

    [Command]
    private void CmdSetNickName(string newNickName)
    {
        string sanitized = (newNickName ?? "").Trim();
        if (sanitized.Length > 30)
            sanitized = sanitized.Substring(0, 30);

        nickName = sanitized;
    }

    public void RequestSelectCharacter(CharacterSaveData character)
    {
        if (!isLocalPlayer || character == null)
            return;

        if (readyToBegin)
        {
            Debug.LogWarning("Cannot switch character while Ready.");
            return;
        }

        CmdSelectCharacter(
            character.Id,
            character.Name,
            character.Gender,
            CharacterManager.GetModelName(character.Gender));
    }

    public void RequestClearCharacterSelection()
    {
        if (!isLocalPlayer || readyToBegin)
            return;

        CmdClearCharacterSelection();
    }

    [Command]
    private void CmdSelectCharacter(string id, string name, CharacterGender gender, string model)
    {
        if (readyToBegin)
            return;

        string sanitizedId = (id ?? "").Trim();
        if (string.IsNullOrEmpty(sanitizedId))
            return;

        string sanitizedName = ApplicationSettings.SanitizeNickname(name);
        if (sanitizedName.Length < ApplicationSettings.MinNicknameLength)
            return;

        string sanitizedModel = string.IsNullOrWhiteSpace(model)
            ? CharacterManager.GetModelName(gender)
            : model.Trim();

        selectedCharacterId = sanitizedId;
        characterName = sanitizedName;
        characterGender = gender;
        modelName = sanitizedModel;
        nickName = sanitizedName;

        ServerCharacterSelections.Set(connectionToClient.connectionId, new ServerCharacterSelections.Selection
        {
            CharacterId = sanitizedId,
            CharacterName = sanitizedName,
            Gender = gender,
            ModelName = sanitizedModel
        });

        var units = GameServices.PlayerUnits;
        if (units != null)
            units.SpawnOrRefreshPlayerUnit(connectionToClient, sanitizedModel, sanitizedName);
    }

    [Command]
    private void CmdClearCharacterSelection()
    {
        if (readyToBegin)
            return;

        selectedCharacterId = string.Empty;
        characterName = string.Empty;
        modelName = string.Empty;
        nickName = string.Empty;

        if (connectionToClient != null)
        {
            ServerCharacterSelections.Remove(connectionToClient.connectionId);
            var units = GameServices.PlayerUnits;
            if (units != null)
                units.DespawnPlayerUnit(connectionToClient);
        }
    }

    public override void OnGUI()
    {
        base.OnGUI();
    }
}
