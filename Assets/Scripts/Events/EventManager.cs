using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Events;

public class EventManager : MonoBehaviour {
    public static EventManager Instance { get; private set; }

    // Dictionary mapping event types to their subscribers.
    private readonly Dictionary<Type, Delegate> eventDictionary = new Dictionary<Type, Delegate>();

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    // Subscribe to an event of type T
    public void Subscribe<T>(Action<T> listener) where T : GameEvent {
        Type eventType = typeof(T);
        if (eventDictionary.TryGetValue(eventType, out Delegate existingDelegate)) {
            eventDictionary[eventType] = Delegate.Combine(existingDelegate, listener);
        } else {
            eventDictionary.Add(eventType, listener);
        }
    }

    // Unsubscribe from an event of type T
    public void Unsubscribe<T>(Action<T> listener) where T : GameEvent {
        Type eventType = typeof(T);
        if (eventDictionary.TryGetValue(eventType, out Delegate existingDelegate)) {
            var newDelegate = Delegate.Remove(existingDelegate, listener);
            if (newDelegate == null) {
                eventDictionary.Remove(eventType);
            } else {
                eventDictionary[eventType] = newDelegate;
            }
        }
    }

    // Publish an event instance
    public void Publish<T>(T gameEvent) where T : GameEvent {
        Type eventType = typeof(T);
        if (eventDictionary.TryGetValue(eventType, out Delegate del)) {
            // Cast and invoke the delegate
            var callback = del as Action<T>;
            callback?.Invoke(gameEvent);
        }
    }
}
