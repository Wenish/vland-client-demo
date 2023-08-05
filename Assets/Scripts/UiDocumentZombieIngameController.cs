using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UiDocumentZombieIngameController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Label _labelWave;
    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _labelWave = _uiDocument.rootVisualElement.Q<Label>(name: "labelWave");
    }
    void Start()
    {
        ZombieGameManager.Singleton.OnNewWaveStarted += HandleOnNewWaveStarted;
    }

    void HandleOnNewWaveStarted(int wave)
    {
        _labelWave.text = wave.ToString();
    }
}
