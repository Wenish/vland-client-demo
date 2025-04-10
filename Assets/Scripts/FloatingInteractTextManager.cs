using System;
using System.Collections.Generic;
using MyGame.Events;
using TMPro;
using UnityEngine;

public class FloatingInteractTextManager : MonoBehaviour
{
    public GameObject interactTextPrefab;
    [SerializeField]
    private Vector3 textOffset = new Vector3(0, 2, 0);

    [SerializeField]
    private UnitController myPlayerUnitController;

    // Store references to spawned text per interaction zone
    private Dictionary<Transform, GameObject> activeInteractTexts = new Dictionary<Transform, GameObject>();

    void OnEnable()
    {
        EventManager.Instance.Subscribe<UnitEnteredInteractionZone>(OnUnitEnteredInteractionZone);
        EventManager.Instance.Subscribe<UnitExitedInteractionZone>(OnUnitExitedInteractionZone);
        EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
    }

    void OnDisable()
    {
        EventManager.Instance.Unsubscribe<UnitEnteredInteractionZone>(OnUnitEnteredInteractionZone);
        EventManager.Instance.Unsubscribe<UnitExitedInteractionZone>(OnUnitExitedInteractionZone);
        EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
    }

    private void OnUnitEnteredInteractionZone(UnitEnteredInteractionZone zone)
    {
        var hasMyUnitEnteredTheZone = zone.Unit == myPlayerUnitController;
        if (hasMyUnitEnteredTheZone)
        {
            GameObject textObj = SpawnInteractText($"Press F to interact\n[Cost: {zone.Zone.goldCost} Gold]", zone.Zone.transform);
            activeInteractTexts[zone.Zone.transform] = textObj;
        }
    }

    private void OnUnitExitedInteractionZone(UnitExitedInteractionZone zone)
    {
        var hasMyUnitExitedTheZone = zone.Unit == myPlayerUnitController;
        if (hasMyUnitExitedTheZone)
        {
            if (activeInteractTexts.TryGetValue(zone.Zone.transform, out GameObject textObj))
            {
                Destroy(textObj);
                activeInteractTexts.Remove(zone.Zone.transform);
            }
        }
    }

    public void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        myPlayerUnitController = myPlayerUnitSpawnedEvent.PlayerCharacter;
    }

    public GameObject SpawnInteractText(string text, Transform targetTransform)
    {
        GameObject interactText = Instantiate(interactTextPrefab, targetTransform.position + textOffset, Quaternion.identity);
        var textMeshPro = interactText.GetComponent<TextMeshPro>();
        textMeshPro.text = text;
        textMeshPro.fontSize = 3;
        textMeshPro.color = Color.white;
        return interactText;
    }
}
