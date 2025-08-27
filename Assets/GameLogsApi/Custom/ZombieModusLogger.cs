using MyGame.Events;
using UnityEngine;

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
        if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt) && Input.GetButtonDown("Fire1"))
        {
            LogKeyboardInput("Fire1", "Mouse0");
        }
        if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt) && Input.GetButtonDown("Fire2"))
        {
            LogKeyboardInput("Fire2", "Mouse1");
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            LogKeyboardInput("Skill1", "Q");
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            LogKeyboardInput("Skill2", "E");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            LogKeyboardInput("Skill3", "R");
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            LogKeyboardInput("Skill4", "F");
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            LogKeyboardInput("Interact", "C");
        }
        if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.Mouse0))
        {
            LogKeyboardInput("WorldPing", "Alt+Mouse0");
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            LogKeyboardInput("MoveForward", "W");
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            LogKeyboardInput("MoveLeft", "A");
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            LogKeyboardInput("MoveBackward", "S");
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            LogKeyboardInput("MoveRight", "D");
        }
    }
}