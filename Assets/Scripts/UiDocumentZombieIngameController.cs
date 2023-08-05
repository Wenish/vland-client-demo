using UnityEngine;
using UnityEngine.UIElements;

public class UiDocumentZombieIngameController : MonoBehaviour
{
    public ZombieGameManager ZombieGameManager;
    private UIDocument _uiDocument;
    private Label _labelWave;
    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _labelWave = _uiDocument.rootVisualElement.Q<Label>(name: "labelWave");
    }
    void Start()
    {
        var currentWave = ZombieGameManager.currentWave;
        if (currentWave > 0)
        {
            _labelWave.text = ZombieGameManager.currentWave.ToString();
        } else
        {
            _labelWave.text = "";
        }
        ZombieGameManager.OnNewWaveStarted += HandleOnNewWaveStarted;
    }

    void HandleOnNewWaveStarted(int wave)
    {
        _labelWave.text = wave.ToString();
    }
}
