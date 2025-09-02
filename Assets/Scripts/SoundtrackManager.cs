using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

public class SoundtrackManager : MonoBehaviour
{
    public static SoundtrackManager Instance { get; private set; }

    [Serializable]
    public class SceneSoundData
    {
        public string sceneName;
        public SoundData soundData;
    }

    [SerializeField]
    private List<SceneSoundData> sceneSoundDataList = new List<SceneSoundData>();

    public SoundData GetSoundDataForScene(string sceneName)
    {
        foreach (var entry in sceneSoundDataList)
        {
            if (entry.sceneName == sceneName)
                return entry.soundData;
        }
        return null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Subscribe to active scene change notifications
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid leaks or double-calls
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        var newSceneSoundData = GetSoundDataForScene(newScene.name);
        if (newSceneSoundData != null)
        {
            AudioManager.Instance?.PlayMusic(newSceneSoundData.clip);
        }
        else
        {
            Debug.LogWarning($"No sound data found for scene '{newScene.name}'");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}