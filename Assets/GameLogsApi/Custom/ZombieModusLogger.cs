using MyGame.Events;
using R3;
using ShadowInfection.DI;
using ShadowInfection.Input;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZombieModusLogger : MonoBehaviour
{
    private GameLogManager _gameLogManager;
    private DisposableBag subscriptions;

    private void Awake()
    {
        _gameLogManager = FindAnyObjectByType<GameLogManager>();
        if (_gameLogManager == null)
        {
            Debug.LogError("Kein GameLogManager in der Scene gefunden!");
        }
    }

    private void OnEnable()
    {
        subscriptions.Dispose();
        subscriptions = new DisposableBag();
        GameMessages.Subscribe<WaveStartedEvent>(ref subscriptions, OnWaveStartedEvent);
    }

    private void OnDisable()
    {
        subscriptions.Dispose();
        subscriptions = new DisposableBag();
    }

    private struct WaveStartedEventPayload
    {
        public int WaveNumber;
    }

    private void OnWaveStartedEvent(WaveStartedEvent e)
    {
        var payload = new WaveStartedEventPayload
        {
            WaveNumber = e.WaveNumber
        };

        _gameLogManager?.LogEvent(nameof(WaveStartedEvent), payload);
    }

    private struct KeyboardInputPayload
    {
        public string ActionName;
        public string Key;
    }

    private void LogKeyboardInput(string actionName, string key)
    {
        var payload = new KeyboardInputPayload
        {
            ActionName = actionName,
            Key = key,
        };

        _gameLogManager?.LogEvent("KeyboardInput", payload);
    }

    void Update()
    {
        var reader = GameServices.Input;
        if (reader == null)
            return;

        if (reader.WasPressed(PlayerActionId.Attack) && !reader.IsHeld(PlayerActionId.SelfTargetModifier))
            LogKeyboardInput("Fire1", "Attack");
        if (reader.WasPressed(PlayerActionId.CancelCast))
            LogKeyboardInput("Fire2", "CancelCast");
        if (reader.WasPressed(PlayerActionId.Skill1))
            LogKeyboardInput("Skill1", "Skill1");
        if (reader.WasPressed(PlayerActionId.Skill2))
            LogKeyboardInput("Skill2", "Skill2");
        if (reader.WasPressed(PlayerActionId.Skill3))
            LogKeyboardInput("Skill3", "Skill3");
        if (reader.WasPressed(PlayerActionId.Ultimate))
            LogKeyboardInput("Skill4", "Ultimate");
        if (reader.WasPressed(PlayerActionId.Interact))
            LogKeyboardInput("Interact", "Interact");
        if (reader.WasPressed(PlayerActionId.Ping)
            || (reader.IsHeld(PlayerActionId.SelfTargetModifier) && reader.WasMousePressed(PlayerActionId.Attack))
            || (Keyboard.current != null
                && Mouse.current != null
                && Keyboard.current.leftAltKey.isPressed
                && Mouse.current.leftButton.wasPressedThisFrame))
            LogKeyboardInput("WorldPing", "Ping");
        if (reader.WasPressed(PlayerActionId.MoveForward))
            LogKeyboardInput("MoveForward", "MoveForward");
        if (reader.WasPressed(PlayerActionId.MoveLeft))
            LogKeyboardInput("MoveLeft", "MoveLeft");
        if (reader.WasPressed(PlayerActionId.MoveBackward))
            LogKeyboardInput("MoveBackward", "MoveBackward");
        if (reader.WasPressed(PlayerActionId.MoveRight))
            LogKeyboardInput("MoveRight", "MoveRight");
    }
}
