using MyGame.Events;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZombieModusLogger : MonoBehaviour
{
    private GameLogManager _gameLogManager;

    private void Awake()
    {
        _gameLogManager = FindFirstObjectByType<GameLogManager>();
        if (_gameLogManager == null)
        {
            Debug.LogError("Kein GameLogManager in der Scene gefunden!");
        }
    }

    private void OnEnable()
    {
        EventManager.Instance?.Subscribe<WaveStartedEvent>(OnWaveStartedEvent);
    }

    private void OnDisable()
    {
        EventManager.Instance?.Unsubscribe<WaveStartedEvent>(OnWaveStartedEvent);
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
        if (!Keyboard.current.leftAltKey.isPressed && !Keyboard.current.rightAltKey.isPressed && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LogKeyboardInput("Fire1", "Mouse0");
        }
        if (!Keyboard.current.leftAltKey.isPressed && !Keyboard.current.rightAltKey.isPressed && Mouse.current.rightButton.wasPressedThisFrame)
        {
            LogKeyboardInput("Fire2", "Mouse1");
        }
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            LogKeyboardInput("Skill1", "Q");
        }
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            LogKeyboardInput("Skill2", "E");
        }
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            LogKeyboardInput("Skill3", "C");
        }
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            LogKeyboardInput("Skill4", "X");
        }
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            LogKeyboardInput("Interact", "F");
        }
        if (Keyboard.current != null && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed) && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LogKeyboardInput("WorldPing", "Alt+Mouse0");
        }
        if (Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame)
        {
            LogKeyboardInput("MoveForward", "W");
        }
        if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            LogKeyboardInput("MoveLeft", "A");
        }
        if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
        {
            LogKeyboardInput("MoveBackward", "S");
        }
        if (Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame)
        {
            LogKeyboardInput("MoveRight", "D");
        }
    }
}