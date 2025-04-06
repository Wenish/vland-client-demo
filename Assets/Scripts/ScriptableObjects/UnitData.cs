using UnityEngine;

[CreateAssetMenu(fileName = "NewUnit", menuName = "Game/Unit/Unit")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public UnitType unitType;
    public int team;
    
    [Header("Stats")]
    public int health;
    public int maxHealth;
    public int shield;
    public int maxShield;
    public float moveSpeed;

    [Header("Weapon")]
    public WeaponData weapon;

    [Header("Model")]
    public ModelData modelData;
}