using MyGame.Events;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public Camera mainCamera;
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.1f;
    public float shakeSpeed = 20f;
    public float returnDuration = 0.1f;

    public UnitController myUnit;

    Quaternion restRotation;
    Quaternion returnFromRotation;
    bool hasRestRotation;
    float shakeElapsed;
    float returnElapsed;
    float seed;
    Phase phase;
    private R3.DisposableBag subscriptions;

    enum Phase
    {
        Idle,
        Shaking,
        Returning
    }

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }

        CaptureRestRotation();
        GameMessages.Subscribe<MyPlayerUnitSpawnedEvent>(ref subscriptions, OnPlayerUnitSpawned);
        GameMessages.Subscribe<UnitDamagedEvent>(ref subscriptions, OnUnitDamaged);
    }

    void OnDisable()
    {
        RestoreRestRotation();
        phase = Phase.Idle;
    }

    void OnDestroy()
    {
        subscriptions.Dispose();
    }

    private void OnPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        myUnit = myPlayerUnitSpawnedEvent.PlayerCharacter;
    }

    public void OnUnitDamaged(UnitDamagedEvent unitDamagedEvent)
    {
        var hasMyUnitMadeTheDamage = unitDamagedEvent.Attacker == myUnit;
        // var hasMyUnitReceivedTheDamage = unitDamagedEvent.Unit == myUnit;
        if (hasMyUnitMadeTheDamage /* || hasMyUnitReceivedTheDamage */)
        {
            TriggerShake();
        }
    }

    public void TriggerShake()
    {
        if (phase == Phase.Idle)
        {
            CaptureRestRotation();
        }

        shakeElapsed = 0f;
        seed = Random.value * 100f;
        phase = Phase.Shaking;
    }

    void LateUpdate()
    {
        if (mainCamera == null || phase == Phase.Idle)
        {
            return;
        }

        if (phase == Phase.Shaking)
        {
            shakeElapsed += Time.deltaTime;
            if (shakeElapsed < shakeDuration)
            {
                ApplyShakeOffset(shakeElapsed);
                return;
            }

            if (returnDuration > 0f)
            {
                phase = Phase.Returning;
                returnElapsed = 0f;
                returnFromRotation = mainCamera.transform.localRotation;
                ApplyReturn(0f);
            }
            else
            {
                RestoreRestRotation();
                phase = Phase.Idle;
            }

            return;
        }

        returnElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(returnElapsed / returnDuration);
        ApplyReturn(t);
        if (t >= 1f)
        {
            RestoreRestRotation();
            phase = Phase.Idle;
        }
    }

    void CaptureRestRotation()
    {
        if (mainCamera == null)
        {
            return;
        }

        restRotation = mainCamera.transform.localRotation;
        hasRestRotation = true;
    }

    void RestoreRestRotation()
    {
        if (mainCamera == null || !hasRestRotation)
        {
            return;
        }

        mainCamera.transform.localRotation = restRotation;
    }

    void ApplyShakeOffset(float elapsed)
    {
        float t = elapsed * shakeSpeed;
        float rotX = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * shakeMagnitude * 10f;
        float rotY = (Mathf.PerlinNoise(seed + 1f, t) - 0.5f) * 2f * shakeMagnitude * 10f;
        Vector3 restEuler = restRotation.eulerAngles;
        mainCamera.transform.localRotation = Quaternion.Euler(restEuler.x + rotX, restEuler.y + rotY, restEuler.z);
    }

    void ApplyReturn(float t)
    {
        Vector3 fromEuler = returnFromRotation.eulerAngles;
        Vector3 restEuler = restRotation.eulerAngles;
        mainCamera.transform.localRotation = Quaternion.Euler(
            Mathf.LerpAngle(fromEuler.x, restEuler.x, t),
            Mathf.LerpAngle(fromEuler.y, restEuler.y, t),
            restEuler.z
        );
    }
}
