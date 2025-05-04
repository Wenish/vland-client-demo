using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }
    
    public WeaponDatabase weaponDatabase;
    public ModelDatabase modelDatabase;
    public UnitDatabase unitDatabase;
    public SkillDatabase skillDatabase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}