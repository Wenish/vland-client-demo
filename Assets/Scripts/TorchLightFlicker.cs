using UnityEngine;

public class TorchLightFlicker : MonoBehaviour
{
    public Light torchLight;
    [Header("Intensity Range")]
    public float minIntensity = 0.7f;
    public float maxIntensity = 1.3f;

    [Header("Flicker Speed")]
    [Tooltip("Higher values produce faster intensity changes.")]
    public float flickerSpeed = 3.0f;

    [Header("Irregularity & Jitter")]
    [Range(0f, 0.5f)]
    [Tooltip("Fraction of (max-min) used for subtle jitter.")]
    public float jitterFraction = 0.12f;
    [Tooltip("Multiplier for the Perlin noise time frequency.")]
    public float noiseFrequencyMultiplier = 1.5f;

    [Header("Duration Randomization")]
    [Tooltip("Randomization lower bound around base duration.")]
    public float durationRandomMin = 0.6f;
    [Tooltip("Randomization upper bound around base duration.")]
    public float durationRandomMax = 1.4f;

    [Header("Micro Spikes/Dips")]
    [Range(0f, 1f)]
    [Tooltip("Chance to add a tiny spike/dip when switching targets.")]
    public float microSpikeChance = 0.15f;
    [Range(0f, 0.5f)]
    [Tooltip("Fraction of (max-min) used for micro spike magnitude.")]
    public float microSpikeFraction = 0.08f;

    // Irregular flicker state
    private float currentIntensity;
    private float targetIntensity;
    private float changeDuration;
    private float changeElapsed;
    private float noiseOffset;

    private void Start()
    {
        if (torchLight == null)
        {
            torchLight = GetComponent<Light>();
            if (torchLight == null)
            {
                Debug.LogError("TorchLightFlicker: No Light component found.", this);
                enabled = false;
                return;
            }
        }

        // Initialize irregular flicker state
        currentIntensity = Mathf.Clamp(torchLight.intensity == 0f ? maxIntensity : torchLight.intensity, minIntensity, maxIntensity);
        targetIntensity = Random.Range(minIntensity, maxIntensity);
        changeDuration = GetNextDuration();
        changeElapsed = 0f;
        noiseOffset = Random.value * 10f;
    }

    private float GetNextDuration()
    {
        // Base duration inversely proportional to flickerSpeed (higher speed -> shorter duration)
        float safeSpeed = Mathf.Max(0.001f, flickerSpeed);
        float baseDuration = 1f / safeSpeed;
        // Add irregularity by randomizing the duration around the base, clamped to valid bounds
        float min = Mathf.Min(durationRandomMin, durationRandomMax);
        float max = Mathf.Max(durationRandomMin, durationRandomMax);
        return baseDuration * Random.Range(min, max);
    }

    private void Update()
    {
        if (torchLight == null) return;

        // Progress towards the next random target intensity over an irregular duration
        changeElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(changeElapsed / changeDuration);
        float baseFlicker = Mathf.Lerp(currentIntensity, targetIntensity, t);

        // Add subtle Perlin noise jitter scaled by flickerSpeed for irregular variation
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed * noiseFrequencyMultiplier, noiseOffset) - 0.5f; // range [-0.5, 0.5]
        float jitterAmount = (maxIntensity - minIntensity) * jitterFraction;
        float intensity = Mathf.Clamp(baseFlicker + noise * jitterAmount, minIntensity, maxIntensity);

        torchLight.intensity = intensity;

        // When we reach the target, pick a new one with a new irregular duration
        if (t >= 1f)
        {
            currentIntensity = targetIntensity;
            targetIntensity = Random.Range(minIntensity, maxIntensity);
            changeDuration = GetNextDuration();
            changeElapsed = 0f;

            // Occasionally introduce brief micro-spikes or dips to keep it irregular
            if (Random.value < microSpikeChance)
            {
                float micro = Random.Range(-microSpikeFraction, microSpikeFraction) * (maxIntensity - minIntensity);
                currentIntensity = Mathf.Clamp(currentIntensity + micro, minIntensity, maxIntensity);
            }
        }
    }
}