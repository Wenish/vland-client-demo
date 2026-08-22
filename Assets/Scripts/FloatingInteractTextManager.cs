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
    private R3.DisposableBag subscriptions;

    void OnEnable()
    {
        subscriptions.Dispose();
        subscriptions = new R3.DisposableBag();
        GameMessages.Subscribe<UnitEnteredInteractionZone>(ref subscriptions, OnUnitEnteredInteractionZone);
        GameMessages.Subscribe<UnitExitedInteractionZone>(ref subscriptions, OnUnitExitedInteractionZone);
        GameMessages.Subscribe<MyPlayerUnitSpawnedEvent>(ref subscriptions, OnMyPlayerUnitSpawned);
    }

    void OnDisable()
    {
        subscriptions.Dispose();
        subscriptions = new R3.DisposableBag();
    }

    private void OnUnitEnteredInteractionZone(UnitEnteredInteractionZone zone)
    {
        var hasMyUnitEnteredTheZone = zone.Unit == myPlayerUnitController;
        if (hasMyUnitEnteredTheZone)
        {
            var interactionText = zone.Zone.BuildTooltipText();
            GameObject textObj = SpawnInteractText(interactionText, zone.Zone.transform);
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
