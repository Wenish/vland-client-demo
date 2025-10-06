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

    [Header("Spin Settings")]
    [Tooltip("Degrees added to the spin target on each hit.")]
    [SerializeField] private float spinDegreesPerHit = 360f;

    [Tooltip("Spin speed in degrees per second when catching up to target.")]
    [SerializeField] private float spinSpeedDegPerSec = 720f;

    // Cache the starting local rotation so we oscillate/spin around it
    private Quaternion _initialLocalRot;

    // Actual per-instance values after applying random offset
    private float _amplitudeDegActual;
    private float _frequencyHzActual;

    // Runtime animated state
    private float _oscAngle;       // Z oscillation angle in degrees
    private float _spinAngle;      // current Y spin angle in degrees
    private float _targetSpinAngle; // target Y spin angle (can accumulate)
    private float _phaseOffset;    // randomizes oscillation start phase

    // Coroutines
    private Coroutine _oscRoutine;
    private Coroutine _spinRoutine;

    private UnitController _unitController;

    private void OnEnable()
    {
        // Capture starting local rotation each (re)enable
        _initialLocalRot = transform.localRotation;

        // Apply a per-instance random percentage to amplitude and frequency
        float offsetPct = Mathf.Abs(randomOffset);
        float ampFactor = 1f + Random.Range(-offsetPct, offsetPct);
        float freqFactor = 1f + Random.Range(-offsetPct, offsetPct);

        _amplitudeDegActual = Mathf.Abs(amplitudeDegrees) * ampFactor;
        _frequencyHzActual = Mathf.Max(0f, frequencyHz * freqFactor);
        _phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        // Reset runtime state
        _oscAngle = 0f;
        _spinAngle = 0f;
        _targetSpinAngle = 0f;

        // Start coroutines
        if (_oscRoutine == null)
            _oscRoutine = StartCoroutine(OscillateCoroutine());

        _unitController = GetComponentInParent<UnitController>();
        if (_unitController)
        {
            _unitController.OnTakeDamage += HandleOnTakeDamage;
        }
    }

    void OnDisable()
    {
        if (_oscRoutine != null)
        {
            StopCoroutine(_oscRoutine);
            _oscRoutine = null;
        }
        if (_spinRoutine != null)
        {
            StopCoroutine(_spinRoutine);
            _spinRoutine = null;
        }

        if (_unitController)
        {
            _unitController.OnTakeDamage -= HandleOnTakeDamage;
        }
    }

    private void HandleOnTakeDamage((UnitController target, UnitController attacker) obj)
    {
        // Only respond to damage intended for this unit (defensive)
        if (obj.target != _unitController) return;

        if (Mathf.Approximately(spinDegreesPerHit, 0f)) return;

        // Count how many full spins are already queued/in-progress
        float remainingDeg = _targetSpinAngle - _spinAngle;
        int queuedSpins = Mathf.FloorToInt(Mathf.Abs(remainingDeg) / Mathf.Abs(spinDegreesPerHit));

        // Allow at most one extra spin queued on top of the current/in-progress spin
        if (queuedSpins < 1)
        {
            QueueSpin(spinDegreesPerHit);
        }
    }

    // ————————————————————————————————————————————————————————————————
    // Coroutines and helpers
    // ————————————————————————————————————————————————————————————————

    private System.Collections.IEnumerator OscillateCoroutine()
    {
        while (enabled)
        {
            // Smooth back-and-forth using a sine wave around Z
            float t = Time.time;
            _oscAngle = Mathf.Sin(2f * Mathf.PI * _frequencyHzActual * t + _phaseOffset) * _amplitudeDegActual;
            ApplyCompositeRotation();
            yield return null;
        }
    }

    private System.Collections.IEnumerator SpinCoroutine()
    {
        while (enabled && Mathf.Abs(_targetSpinAngle - _spinAngle) > 0.01f)
        {
            float step = Mathf.Max(0f, spinSpeedDegPerSec) * Time.deltaTime;
            _spinAngle = Mathf.MoveTowards(_spinAngle, _targetSpinAngle, step);
            ApplyCompositeRotation();
            yield return null;
        }
        _spinRoutine = null; // mark finished
    }

    private void QueueSpin(float degrees)
    {
        if (Mathf.Approximately(degrees, 0f)) return;

        _targetSpinAngle += degrees;

        // Start or restart the spin coroutine if needed
        if (_spinRoutine == null)
        {
            _spinRoutine = StartCoroutine(SpinCoroutine());
        }
    }

    private void ApplyCompositeRotation()
    {
        // Compose rotation so that spin (Y) and oscillation (Z) work together around the initial local rotation
        Quaternion spin = Quaternion.AngleAxis(_spinAngle, Vector3.up);
        Quaternion osc = Quaternion.AngleAxis(_oscAngle, Vector3.forward);
        transform.localRotation = _initialLocalRot * spin * osc;
    }
}
