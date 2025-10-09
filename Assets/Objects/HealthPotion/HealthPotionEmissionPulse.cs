using UnityEngine;

public class HealthPotionEmissionPulse : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("MeshRenderer whose material will be pulsed via MaterialPropertyBlock. If not set, will try to find one on this GameObject.")]
    [SerializeField] private MeshRenderer targetRenderer;

    [Tooltip("Index of the material on the renderer to modify (0-based).")]
    [Min(0)] [SerializeField] private int materialIndex = 0;

    [Header("Emission Pulse")] 

    [Tooltip("Minimum emission intensity multiplier.")]
    [Min(0f)] [SerializeField] private float minIntensity = 0.2f;

    [Tooltip("Maximum emission intensity multiplier.")]
    [Min(0f)] [SerializeField] private float maxIntensity = 1.25f;

    [Tooltip("Speed of the pulse in cycles per second.")]
    [Min(0f)] [SerializeField] private float pulseSpeed = 1.0f;

    [Tooltip("Random fractional offset applied to pulse speed per instance (e.g., 0.1 = ±10%).")]
    [Range(0f, 1f)] [SerializeField] private float pulseSpeedRandomFraction = 0.1f;

    [Header("Advanced")] 
    [Tooltip("Shader color property to write the emission to.")]
    [SerializeField] private string emissionColorProperty = "_EmissionColor"; // Standard/URP Lit

    [Tooltip("Optional shader keyword to enable on the material for emission to take effect (e.g., _EMISSION for Standard/URP Lit). Note: enabling a keyword changes the shared material.")]
    [SerializeField] private string emissionKeyword = "_EMISSION";

    [Tooltip("Enable the emission keyword on Start if provided.")]
    [SerializeField] private bool enableEmissionKeyword = true;

    [Tooltip("Use unscaled time for pulsing (ignores Time.timeScale).")]
    [SerializeField] private bool useUnscaledTime = false;

    private MaterialPropertyBlock _mpb;
    private int _emissionColorId;
    private Color _baseEmissionColor = Color.black;
    private float _speedMultiplier = 1f;

    // Expose a property setter for code-based assignment while keeping Inspector serialization.
    public MeshRenderer TargetRenderer
    {
        get => targetRenderer;
        set => targetRenderer = value;
    }

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _emissionColorId = Shader.PropertyToID(emissionColorProperty);
        EnsureRenderer();
        RefreshBaseEmissionColor();
        SetRandomSpeedMultiplier();
    }

    private void OnEnable()
    {
        EnsureRenderer();
        RefreshBaseEmissionColor();
    SetRandomSpeedMultiplier();
        if (targetRenderer != null && enableEmissionKeyword && !string.IsNullOrEmpty(emissionKeyword))
        {
            // Note: this changes the shared material to make sure emission is evaluated by the shader.
            // We avoid per-instance instantiation by not touching material color directly.
            TryEnableEmissionKeyword(targetRenderer, materialIndex, emissionKeyword);
        }
    }

    private void OnDisable()
    {
        // Clear the property block override to leave the material as-is when disabled.
        if (targetRenderer != null)
        {
            targetRenderer.SetPropertyBlock(null, materialIndex);
        }
    }

    private void Update()
    {
        if (targetRenderer == null)
            return;

        // Compute a smooth 0..1 pulse using sine.
    float t = useUnscaledTime ? Time.unscaledTime : Time.time;
    float effectiveSpeed = Mathf.Max(0.0001f, pulseSpeed * Mathf.Max(0f, _speedMultiplier));
    float phase = (Mathf.Sin(t * Mathf.PI * 2f * effectiveSpeed) + 1f) * 0.5f; // 0..1
        float intensity = Mathf.Lerp(Mathf.Min(minIntensity, maxIntensity), Mathf.Max(minIntensity, maxIntensity), phase);

        _mpb.Clear();
        // Multiply HDR color by intensity; works with Standard/URP Lit using _EmissionColor.
        _mpb.SetColor(_emissionColorId, _baseEmissionColor * intensity);
        targetRenderer.SetPropertyBlock(_mpb, materialIndex);
    }

    private void OnValidate()
    {
        if (maxIntensity < minIntensity)
        {
            maxIntensity = minIntensity;
        }

        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        _emissionColorId = Shader.PropertyToID(emissionColorProperty);
        EnsureRenderer();
    RefreshBaseEmissionColor();

        // In-editor preview while editing values.
        if (Application.isPlaying == false && targetRenderer != null)
        {
            _mpb.Clear();
            _mpb.SetColor(_emissionColorId, _baseEmissionColor * minIntensity);
            targetRenderer.SetPropertyBlock(_mpb, materialIndex);
        }
    }

    private void EnsureRenderer()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<MeshRenderer>();
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<MeshRenderer>();
            }
        }
    }

    private static void TryEnableEmissionKeyword(MeshRenderer renderer, int matIndex, string keyword)
    {
        if (renderer == null) return;
        var materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0) return;
        matIndex = Mathf.Clamp(matIndex, 0, materials.Length - 1);
        var mat = materials[matIndex];
        if (mat != null && !mat.IsKeywordEnabled(keyword))
        {
            mat.EnableKeyword(keyword);
        }
    }

    private void RefreshBaseEmissionColor()
    {
        _baseEmissionColor = Color.black;
        if (targetRenderer == null) return;
        var materials = targetRenderer.sharedMaterials;
        if (materials == null || materials.Length == 0) return;
        int idx = Mathf.Clamp(materialIndex, 0, materials.Length - 1);
        var mat = materials[idx];
        if (mat == null) return;

        // Prefer configured emission property; fall back to _EmissionColor if misconfigured.
        if (mat.HasProperty(_emissionColorId))
        {
            _baseEmissionColor = mat.GetColor(_emissionColorId);
        }
        else
        {
            int fallbackId = Shader.PropertyToID("_EmissionColor");
            if (mat.HasProperty(fallbackId))
            {
                _baseEmissionColor = mat.GetColor(fallbackId);
            }
        }
    }

    private void SetRandomSpeedMultiplier()
    {
        if (!Application.isPlaying)
        {
            // Keep deterministic preview in edit mode.
            _speedMultiplier = 1f;
            return;
        }
        float frac = Mathf.Clamp01(pulseSpeedRandomFraction);
        _speedMultiplier = 1f + Random.Range(-frac, frac);
    }
}
