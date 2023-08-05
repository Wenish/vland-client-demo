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
    // Start is called before the first frame update
    void Start()
    {
        // _labelWave.text = "Hallo";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
