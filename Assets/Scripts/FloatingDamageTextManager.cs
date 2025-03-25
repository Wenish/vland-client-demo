using MyGame.Events;
using UnityEngine;

public class FloatingDamageTextManager : MonoBehaviour
{
    public GameObject damageTextPrefab;
    public Vector3 textOffset = new Vector3(0, 2f, 0); // Adjust the offset if needed

    void Awake()
    {
        EventManager.Instance.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
    }

    public void OnUnitDamaged(UnitDamagedEvent unitDamagedEvent)
    {
        Debug.Log("Unit damaged event received");
        SpawnDamageText(unitDamagedEvent.DamageAmount, unitDamagedEvent.Unit.transform);
    }


    public void SpawnDamageText(int damage, Transform textSpawnPoint)
    {
        GameObject damageText = Instantiate(damageTextPrefab, textSpawnPoint);
        damageText.GetComponent<FloatingDamageText>().Initialize(damage, textSpawnPoint, textOffset);
    }
}