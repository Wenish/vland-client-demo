using System.Collections;
using MyGame.Events;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public Camera mainCamera;
    private Quaternion originalRotation;
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.1f;
    public float shakeSpeed = 20f;
    public float returnDuration = 0.1f;

    public UnitController myUnit;

    private Coroutine shakeRoutine;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }
        originalRotation = mainCamera.transform.localRotation;
        EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);
        EventManager.Instance.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
    }

    void OnDestroy()
    {
        EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);
        EventManager.Instance.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
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
        // Capture the current rotation so we return to the pre-shake pose
        originalRotation = mainCamera.transform.localRotation;

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        float elapsed = 0f;
        float seed = Random.value * 100f;

        while (elapsed < shakeDuration)
        {
            float t = elapsed * shakeSpeed;
            float rotX = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * shakeMagnitude * 10f;
            float rotY = (Mathf.PerlinNoise(seed + 1f, t) - 0.5f) * 2f * shakeMagnitude * 10f;

            mainCamera.transform.localRotation = originalRotation * Quaternion.Euler(rotX, rotY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Smoothly return to the original rotation
        Quaternion startRotation = mainCamera.transform.localRotation;
        if (returnDuration > 0f)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / returnDuration;
                mainCamera.transform.localRotation = Quaternion.Slerp(startRotation, originalRotation, t);
                yield return null;
            }
        }

        mainCamera.transform.localRotation = originalRotation;
        shakeRoutine = null;
    }
}