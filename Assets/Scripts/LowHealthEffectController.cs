using MessagePipe;
using MyGame.Events;
using ShadowInfection.DI;
using UnityEngine;
using UnityEngine.Rendering;

public class LowHealthEffectController : MonoBehaviour
{
    public Volume postProcessVolume;

    [Range(0.05f, 1f)]
    public float startPercent = 0.35f;

    [Range(0f, 1f)]
    public float minWeight = 0.5f;

    [Range(0f, 1f)]
    public float maxWeight = 0.9f;

    [Min(0.1f)]
    public float pulseHz = 0.9f;

    [Min(0.1f)]
    public float fadeSpeed = 2.8f;

    private UnitController myUnit;
    private float displayedWeight;
    private float pulsePhase;
    private R3.DisposableBag subscriptions;

    void Start()
    {
        if (postProcessVolume == null)
            postProcessVolume = GetComponent<Volume>();

        SetWeight(0f);
        if (GameLifetimeScope.TryResolve(out ISubscriber<MyPlayerUnitSpawnedEvent> spawned))
            subscriptions.Add(spawned.Subscribe(OnPlayerUnitSpawned));
        enabled = false;
    }

    void OnDestroy()
    {
        UnbindUnit();
        subscriptions.Dispose();
    }

    void Update()
    {
        if (!TryGetDanger(out var danger))
        {
            SleepWhenIdle();
            return;
        }

        var hz = Mathf.Lerp(pulseHz * 0.75f, pulseHz * 1.45f, danger);
        pulsePhase += Time.unscaledDeltaTime * hz * (Mathf.PI * 2f);
        var pulse = 0.5f + 0.5f * Mathf.Sin(pulsePhase);
        var target = danger * Mathf.Lerp(minWeight, maxWeight, pulse);
        SetWeight(Mathf.MoveTowards(displayedWeight, target, Time.unscaledDeltaTime * fadeSpeed));
    }

    private void OnPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        UnbindUnit();
        myUnit = myPlayerUnitSpawnedEvent.PlayerCharacter;
        if (myUnit == null)
            return;

        myUnit.OnHealthChange += HandleHealthChanged;
        SetPulsing(IsBelowThreshold(myUnit.health, myUnit.maxHealth));
    }

    private void HandleHealthChanged((int current, int max) health)
    {
        if (IsBelowThreshold(health.current, health.max))
            SetPulsing(true);
    }

    private void UnbindUnit()
    {
        if (myUnit != null)
            myUnit.OnHealthChange -= HandleHealthChanged;

        myUnit = null;
        SetWeight(0f);
        enabled = false;
    }

    private void SleepWhenIdle()
    {
        if (displayedWeight > 0.001f)
        {
            SetWeight(Mathf.MoveTowards(displayedWeight, 0f, Time.unscaledDeltaTime * fadeSpeed));
            return;
        }

        SetWeight(0f);
        enabled = false;
    }

    private void SetPulsing(bool pulsing)
    {
        enabled = pulsing;
        if (!pulsing)
            pulsePhase = 0f;
    }

    private bool TryGetDanger(out float danger)
    {
        danger = 0f;
        if (myUnit == null || myUnit.maxHealth <= 0 || myUnit.health <= 0)
            return false;

        var health01 = myUnit.health / (float)myUnit.maxHealth;
        if (health01 >= startPercent)
            return false;

        danger = Mathf.Clamp01(Mathf.InverseLerp(startPercent, 0.12f, health01));
        return danger > 0f;
    }

    private bool IsBelowThreshold(int current, int max)
    {
        return max > 0 && current > 0 && current / (float)max < startPercent;
    }

    private void SetWeight(float weight)
    {
        displayedWeight = weight;
        if (postProcessVolume != null)
            postProcessVolume.weight = weight;
    }
}
