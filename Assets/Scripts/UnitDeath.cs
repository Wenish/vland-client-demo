using System.Collections;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class UnitDeath : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private float fadeDuration = 0.2f;

    private Material mat;
    private Coroutine fadeRoutine;
    private UnitController unitController;

    [ColorUsage(true, true)]
    public Color ColorDead = Color.black;
    [ColorUsage(true, true)]
    public Color ColorAlive = Color.white;

    private MaterialPropertyBlock _mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP/HDRP
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color"); // Built-in fallback
    private int _activeColorId = BaseColorId;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        // prefer TryGetComponent to avoid exceptions
        TryGetComponent(out unitController);

        // Only fall back to finding a renderer if the inspector field is empty
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

    UpdateActiveColorPropertyId();
    }

    void OnEnable()
    {
        if (unitController != null)
        {
            unitController.OnModelChange += HandleOnModelChange;
            unitController.OnDied += HandleOnDied;
            unitController.OnRevive += HandleOnRevive;
        }
    }

    void OnDisable()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (unitController != null)
        {
            unitController.OnModelChange -= HandleOnModelChange;
            unitController.OnDied -= HandleOnDied;
            unitController.OnRevive -= HandleOnRevive;
        }

        // If we created an instance material via .material, destroy it to avoid leaks
        if (mat != null)
        {
            Destroy(mat);
            mat = null;
        }
    }

    void HandleOnModelChange((UnitController unitController, GameObject modelInstance) obj)
    {
        if (obj.modelInstance == null) return;
        var r = obj.modelInstance.GetComponentInChildren<Renderer>();
        if (r == null) return;

        targetRenderer = r;

        if (mat != null)
        {
            Destroy(mat);
            mat = null;
        }

        // Avoid instantiating materials; use MPB instead
        UpdateActiveColorPropertyId();
    }

    void HandleOnDied()
    {
        FadeToDeath();
    }

    void HandleOnRevive()
    {
        FadeToAlive();
    }

    public void FadeToDeath()
    {
        if (!isActiveAndEnabled) return;
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeCoroutine(ColorDead));
    }

    public void FadeToAlive()
    {
        if (!isActiveAndEnabled) return;
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeCoroutine(ColorAlive));
    }

    private IEnumerator FadeCoroutine(Color targetColor)
    {
        if (targetRenderer == null) yield break;
        var sm = targetRenderer.sharedMaterial;
        if (sm == null) yield break;

        // Ensure we use the correct color property
        UpdateActiveColorPropertyId();

        float t = 0f;
        Color initialColor = sm.HasProperty(_activeColorId)
            ? sm.GetColor(_activeColorId)
            : Color.white;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            var c = Color.Lerp(initialColor, targetColor, t);
            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_activeColorId, c);
            targetRenderer.SetPropertyBlock(_mpb);
            yield return null;
        }

        // Snap to final color to avoid precision drift
        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_activeColorId, targetColor);
        targetRenderer.SetPropertyBlock(_mpb);

        fadeRoutine = null;
    }

    private void UpdateActiveColorPropertyId()
    {
        _activeColorId = BaseColorId;
        if (targetRenderer == null) return;
        var sm = targetRenderer.sharedMaterial;
        if (sm == null) return;

        if (sm.HasProperty(BaseColorId))
        {
            _activeColorId = BaseColorId;
        }
        else if (sm.HasProperty(LegacyColorId))
        {
            _activeColorId = LegacyColorId;
        }
    }

}