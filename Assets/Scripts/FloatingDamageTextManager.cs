using MyGame.Events;
using UnityEngine;

public class FloatingDamageTextManager : MonoBehaviour
{
    public GameObject damageTextPrefab;
    private Vector3 textOffset = new Vector3(0, 2, 0); // Adjust the offset if needed

    public UnitController myPlayerUnitController;

    public float minFontSize = 2f;
    public float maxFontSize = 5f;

    private Color greenColor = new Color(0f / 255f, 201f / 255f, 81f / 255f);
    private Color blueColor = new Color(0f / 255f, 166f / 255f, 244f / 255f);
    private Color orangeColor = new Color(255f / 255f, 105f / 255f, 0f / 255f);
    private Color yellowColor = new Color(253f / 255f, 199f / 255f, 0f / 255f);

    void OnEnable()
    {
        EventManager.Instance.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
        EventManager.Instance.Subscribe<UnitHealedEvent>(OnUnitHealed);
        EventManager.Instance.Subscribe<UnitShieldedEvent>(OnUnitShielded);
        EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
        EventManager.Instance.Subscribe<UnitDiedEvent>(OnUnitDied);
    }

    void OnDisable()
    {
        EventManager.Instance.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
        EventManager.Instance.Unsubscribe<UnitHealedEvent>(OnUnitHealed);
        EventManager.Instance.Unsubscribe<UnitShieldedEvent>(OnUnitShielded);
        EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
        EventManager.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDied);
    }

    public void OnUnitDamaged(UnitDamagedEvent unitDamagedEvent)
    {
        var hasMyUnitMadeTheDamage = unitDamagedEvent.Attacker == myPlayerUnitController;
        var hasMyUnitReceivedTheDamage = unitDamagedEvent.Unit == myPlayerUnitController;
        if (hasMyUnitMadeTheDamage || hasMyUnitReceivedTheDamage)
        {
            SpawnDamageText(unitDamagedEvent.DamageAmount.ToString(), unitDamagedEvent.DamageAmount, unitDamagedEvent.Unit.transform, orangeColor);
        }
    }

    public void OnUnitHealed(UnitHealedEvent unitHealedEvent)
    {
        var hasMyUnitedReceivedTheHeal = unitHealedEvent.Unit == myPlayerUnitController;
        if (hasMyUnitedReceivedTheHeal)
        {
            var text = $"+{unitHealedEvent.HealAmount}";
            SpawnDamageText(text, unitHealedEvent.HealAmount, unitHealedEvent.Unit.transform, greenColor);
        }
    }

    public void OnUnitShielded(UnitShieldedEvent unitShieldedEvent)
    {
        var hasMyUnitedReceivedTheShield = unitShieldedEvent.Unit == myPlayerUnitController;
        if (hasMyUnitedReceivedTheShield)
        {
            var text = $"+{unitShieldedEvent.ShieldAmount}";
            SpawnDamageText(text, unitShieldedEvent.ShieldAmount, unitShieldedEvent.Unit.transform, blueColor);
        }
    }

    public void OnUnitDied(UnitDiedEvent unitDiedEvent)
    {
        var hasMyUnitedKilledTheUnit = unitDiedEvent.Killer == myPlayerUnitController;
        if (hasMyUnitedKilledTheUnit)
        {
            SpawnDamageText("+14 <sprite name=\"gold_coin\">", 14, unitDiedEvent.Unit.transform, yellowColor);
        }
    }

    public void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        myPlayerUnitController = myPlayerUnitSpawnedEvent.PlayerCharacter;
    }


    public void SpawnDamageText(string text, int value, Transform textSpawnPoint, Color color)
    {
        Vector3 spawnPosition = textSpawnPoint.position + new Vector3(Random.Range(-1, 1), 0, 0);
        GameObject damageText = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity);
        Vector3 randomOffset = textOffset + new Vector3(0, Random.Range(-1f, 0.5f), 0);
        float clampedValue = Mathf.Clamp(value, 10, 100);
        float fontSize = Mathf.Lerp(minFontSize, maxFontSize, (clampedValue - 10) / 90);
        damageText.GetComponent<FloatingDamageText>().Initialize(text, randomOffset, color, fontSize);
    }
}