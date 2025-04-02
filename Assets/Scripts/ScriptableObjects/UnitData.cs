using UnityEngine;

[CreateAssetMenu(fileName = "NewUnit", menuName = "Game/Unit")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public UnitType unitType;
    public int team;
    public int health;
    public int maxHealth;
    public int shield;
    public int maxShield;
    public float moveSpeed;
}