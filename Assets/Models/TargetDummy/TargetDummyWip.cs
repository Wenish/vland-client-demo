using UnityEngine;

public class TargetDummyWip : MonoBehaviour
{
    [Header("Wip Rotation Settings")]
    [Tooltip("Maximum angle (in degrees) to rotate left/right around the center on Z.")]
    [SerializeField] private float amplitudeDegrees = 15f;

    [Tooltip("Oscillation speed in cycles per second (Hz).")]
    [SerializeField] private float frequencyHz = 0.5f;

    [Tooltip("Random variation percentage applied to amplitude and frequency (0 = none, 0.1 = ±10%).")]
    [Range(0f, 1f)]
    [SerializeField]
    private float randomOffset = 0.1f;

    // Cache the starting local rotation so we oscillate around it
    private Vector3 _initialLocalEuler;
    private float _initialZ;

    // Actual per-instance values after applying random offset
    private float _amplitudeDegActual;
    private float _frequencyHzActual;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _initialLocalEuler = transform.localEulerAngles;
        _initialZ = _initialLocalEuler.z;

        // Apply a per-instance random percentage to amplitude and frequency
        float offsetPct = Mathf.Abs(randomOffset);
        float ampFactor = 1f + UnityEngine.Random.Range(-offsetPct, offsetPct);
        float freqFactor = 1f + UnityEngine.Random.Range(-offsetPct, offsetPct);

        _amplitudeDegActual = Mathf.Abs(amplitudeDegrees) * ampFactor;
        _frequencyHzActual = Mathf.Max(0f, frequencyHz * freqFactor);
    }

    // Update is called once per frame
    private void Update()
    {
        // Smooth back-and-forth using a sine wave around the initial Z
        float angleOffset = Mathf.Sin(2f * Mathf.PI * Time.time * Mathf.Max(0f, _frequencyHzActual)) * _amplitudeDegActual;
        Vector3 euler = _initialLocalEuler;
        euler.z = _initialZ + angleOffset;
        transform.localRotation = Quaternion.Euler(euler);
    }
}
