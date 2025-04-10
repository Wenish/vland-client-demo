using System;
using MyGame.Events;
using TMPro;
using UnityEngine;

public class FloatingInteractTextManager : MonoBehaviour
{
    public GameObject interactTextPrefab;
    private Vector3 textOffset = new Vector3(0, 2, 0);

    [SerializeField]
    private UnitController myPlayerUnitController;

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
            SpawnInteractText($"Press F to interact\n[Cost: {zone.Zone.goldCost} Gold]", zone.Zone.transform);
        }
    }

    private void OnUnitExitedInteractionZone(UnitExitedInteractionZone zone)
    {
        throw new NotImplementedException();
    }
    public void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        myPlayerUnitController = myPlayerUnitSpawnedEvent.PlayerCharacter;
    }
    public void SpawnInteractText(string text, Transform targetTransform)
    {
        GameObject interactText = Instantiate(interactTextPrefab, targetTransform.position + textOffset, Quaternion.identity);
        var textMeshPro = interactText.GetComponent<TextMeshPro>();
        textMeshPro.text = text;
        textMeshPro.fontSize = 3; // Set the font size
        textMeshPro.color = Color.white; // Set the color
    }
}