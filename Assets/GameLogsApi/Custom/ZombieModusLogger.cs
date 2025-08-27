using System;
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
}