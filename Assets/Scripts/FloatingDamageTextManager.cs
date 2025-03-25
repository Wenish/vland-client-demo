using MyGame.Events;
using Unity.VisualScripting;
using UnityEngine;

public class FloatingDamageTextManager : MonoBehaviour
{
    public GameObject damageTextPrefab;
    private Vector3 textOffset = new Vector3(0, 2, 0); // Adjust the offset if needed

    public UnitController myPlayerUnitController;

    void OnEnable()
    {
        EventManager.Instance.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
        EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
    }

    void OnDisable()
    {
        EventManager.Instance.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
        EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
    }

    public void OnUnitDamaged(UnitDamagedEvent unitDamagedEvent)
    {
        Debug.Log("Unit damaged event received");
        var hasMyUnitMadeTheDamage = unitDamagedEvent.Attacker == myPlayerUnitController;
        var hasMyUnitReceivedTheDamage = unitDamagedEvent.Unit == myPlayerUnitController;
        Color color = hasMyUnitReceivedTheDamage ? Color.red : Color.white;
        if (hasMyUnitMadeTheDamage || hasMyUnitReceivedTheDamage)
        {
            SpawnDamageText(unitDamagedEvent.DamageAmount, unitDamagedEvent.Unit.transform, color);
        }
    }

    public void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        myPlayerUnitController = myPlayerUnitSpawnedEvent.PlayerCharacter;
    }


    public void SpawnDamageText(int damage, Transform textSpawnPoint, Color color)
    {
        GameObject damageText = Instantiate(damageTextPrefab, textSpawnPoint.position, Quaternion.identity);
        damageText.GetComponent<FloatingDamageText>().Initialize(damage, textOffset, color);
    }
}