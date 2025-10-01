using UnityEngine;
using Mirror;

public class TargetDummy : NetworkBehaviour
{
    public UnitController unitController;

    public float resetTimer = 5f;
    private float nextResetTime = 0f;

    public int defaultHealth = 100;
    public int defaultMaxHealth = 100;
    public int defaultShield = 50;
    public int defaultMaxShield = 50;
    public float defaultMoveSpeed = 0f;


    public void Start()
    {
        if (isServer == false) return;

        unitController = GetComponent<UnitController>();
        defaultHealth = unitController.health;
        defaultMaxHealth = unitController.maxHealth;
        defaultShield = unitController.shield;
        defaultMaxShield = unitController.maxShield;
        defaultMoveSpeed = unitController.moveSpeed;

        // schedule first reset
        nextResetTime = Time.time + resetTimer;
    }

    // Ensure resets happen periodically on the server only
    [ServerCallback]
    private void Update()
    {
        if (Time.time >= nextResetTime)
        {
            ResetDummy();
            ResetTimer();
        }
    }

    [Server]
    public void ResetTimer()
    {
        nextResetTime = Time.time + resetTimer;
    }

    [Server]
    public void ResetDummy()
    {
        unitController.SetMaxHealth(defaultMaxHealth);
        unitController.SetMaxShield(defaultMaxShield);
        unitController.SetHealth(defaultHealth);
        unitController.SetShield(defaultShield);
        unitController.moveSpeed = defaultMoveSpeed;
    }
}