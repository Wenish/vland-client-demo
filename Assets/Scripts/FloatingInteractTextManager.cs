using System.Collections.Generic;
using MyGame.Events;
using ShadowInfection.Input;
using TMPro;
using UnityEngine;

public class FloatingInteractTextManager : MonoBehaviour
{
    public GameObject interactTextPrefab;
    [SerializeField]
    private Vector3 textOffset = new Vector3(0, 2, 0);

    [SerializeField]
    private UnitController myPlayerUnitController;

    private readonly Dictionary<InteractionZone, GameObject> activeInteractTexts = new();
    private R3.DisposableBag subscriptions;

    void OnEnable()
    {
        subscriptions.Dispose();
        subscriptions = new R3.DisposableBag();
        GameMessages.Subscribe<UnitEnteredInteractionZone>(ref subscriptions, OnUnitEnteredInteractionZone);
        GameMessages.Subscribe<UnitExitedInteractionZone>(ref subscriptions, OnUnitExitedInteractionZone);
        GameMessages.Subscribe<MyPlayerUnitSpawnedEvent>(ref subscriptions, OnMyPlayerUnitSpawned);
        GameMessages.Subscribe<InputBindingsChangedEvent>(ref subscriptions, OnInputBindingsChanged);
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
            activeInteractTexts[zone.Zone] = textObj;
        }
    }

    private void OnUnitExitedInteractionZone(UnitExitedInteractionZone zone)
    {
        var hasMyUnitExitedTheZone = zone.Unit == myPlayerUnitController;
        if (hasMyUnitExitedTheZone)
        {
            if (activeInteractTexts.TryGetValue(zone.Zone, out GameObject textObj))
            {
                Destroy(textObj);
                activeInteractTexts.Remove(zone.Zone);
            }
        }
    }

    private void OnInputBindingsChanged(InputBindingsChangedEvent _)
    {
        foreach (var pair in activeInteractTexts)
        {
            if (pair.Key == null || pair.Value == null)
                continue;
            var textMeshPro = pair.Value.GetComponent<TextMeshPro>();
            if (textMeshPro != null)
                textMeshPro.text = pair.Key.BuildTooltipText();
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
