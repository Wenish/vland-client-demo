using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(UnitController))]
public class UnitDeath : MonoBehaviour
{
    // Optional: assign specific renderers in the Inspector. If empty, we'll auto-find all mesh/skinned mesh renderers under the model.
    [SerializeField] private Renderer[] targetRenderers;
    // Legacy single-field support to migrate existing serialized data
    [FormerlySerializedAs("targetRenderer")]
    [SerializeField] private Renderer targetRendererLegacy;
    [SerializeField] private float fadeDuration = 0.2f;

    private Coroutine fadeRoutine;
    private UnitController unitController;

    [ColorUsage(true, true)]
    public Color ColorDead = Color.black;

    private MaterialPropertyBlock _mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP/HDRP
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color"); // Built-in fallback
    // Cache the active color property per renderer
    private readonly Dictionary<Renderer, int> _rendererColorId = new Dictionary<Renderer, int>();
    // Cache the original (alive) color per renderer, captured on initialize/model change
    private readonly Dictionary<Renderer, Color> _rendererAliveColor = new Dictionary<Renderer, Color>();

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        // prefer TryGetComponent to avoid exceptions
        TryGetComponent(out unitController);

        // If array is empty but legacy field is set, migrate it
        if ((targetRenderers == null || targetRenderers.Length == 0) && targetRendererLegacy != null)
        {
            targetRenderers = new[] { targetRendererLegacy };
        }

        // If still no renderers assigned in inspector, find all mesh-type renderers under this object
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = FindModelRenderers(gameObject);
        }

        UpdateActiveColorPropertyIds();
        CaptureAliveColors();
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

        _rendererColorId.Clear();
        _rendererAliveColor.Clear();
    }

    void HandleOnModelChange((UnitController unitController, GameObject modelInstance) obj)
    {
        if (obj.modelInstance == null) return;
        // Stop any ongoing fade that might reference old/destroyed renderers
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        var renderers = FindModelRenderers(obj.modelInstance);
        if (renderers == null || renderers.Length == 0) return;

        targetRenderers = renderers;

        // Avoid instantiating materials; use MPB instead, and refresh color property + alive color cache
        UpdateActiveColorPropertyIds();
        CaptureAliveColors();
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

        fadeRoutine = StartCoroutine(FadeCoroutineToAlive());
    }

    private IEnumerator FadeCoroutine(Color targetColor)
    {
        if (targetRenderers == null || targetRenderers.Length == 0) yield break;

        // Ensure we use the correct color property per renderer
        UpdateActiveColorPropertyIds();

        // Prepare initial colors for each renderer
        var activeEntries = new List<(Renderer r, int colorId, Color initial)>();
        foreach (var r in targetRenderers)
        {
            if (r == null) continue;
            var sm = r.sharedMaterial;
            if (sm == null) continue;

            int colorId = GetColorPropertyFor(r);
            Color initialColor = sm.HasProperty(colorId) ? sm.GetColor(colorId) : Color.white;
            activeEntries.Add((r, colorId, initialColor));
        }

        if (activeEntries.Count == 0) yield break;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            var lerp = Mathf.Clamp01(t);
            foreach (var e in activeEntries)
            {
                if (e.r == null) continue; // renderer might have been destroyed
                var c = Color.Lerp(e.initial, targetColor, lerp);
                e.r.GetPropertyBlock(_mpb);
                _mpb.SetColor(e.colorId, c);
                e.r.SetPropertyBlock(_mpb);
            }
            yield return null;
        }

        // Snap to final color to avoid precision drift
        foreach (var e in activeEntries)
        {
            if (e.r == null) continue;
            e.r.GetPropertyBlock(_mpb);
            _mpb.SetColor(e.colorId, targetColor);
            e.r.SetPropertyBlock(_mpb);
        }

        fadeRoutine = null;
    }

    private IEnumerator FadeCoroutineToAlive()
    {
        if (targetRenderers == null || targetRenderers.Length == 0) yield break;

        // Ensure we use the correct color property per renderer
        UpdateActiveColorPropertyIds();

        // Prepare initial and target (alive) colors for each renderer
        var activeEntries = new List<(Renderer r, int colorId, Color initial, Color target)>();
        foreach (var r in targetRenderers)
        {
            if (r == null) continue;
            var sm = r.sharedMaterial;
            if (sm == null) continue;

            int colorId = GetColorPropertyFor(r);
            Color initialColor = sm.HasProperty(colorId) ? sm.GetColor(colorId) : Color.white;
            Color targetColor = _rendererAliveColor.TryGetValue(r, out var alive) ? alive : Color.white;
            activeEntries.Add((r, colorId, initialColor, targetColor));
        }

        if (activeEntries.Count == 0) yield break;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            var lerp = Mathf.Clamp01(t);
            foreach (var e in activeEntries)
            {
                if (e.r == null) continue; // renderer might have been destroyed
                var c = Color.Lerp(e.initial, e.target, lerp);
                e.r.GetPropertyBlock(_mpb);
                _mpb.SetColor(e.colorId, c);
                e.r.SetPropertyBlock(_mpb);
            }
            yield return null;
        }

        // Snap to final color to avoid precision drift
        foreach (var e in activeEntries)
        {
            if (e.r == null) continue;
            e.r.GetPropertyBlock(_mpb);
            _mpb.SetColor(e.colorId, e.target);
            e.r.SetPropertyBlock(_mpb);
        }

        fadeRoutine = null;
    }

    private void UpdateActiveColorPropertyIds()
    {
        _rendererColorId.Clear();
        if (targetRenderers == null) return;
        foreach (var r in targetRenderers)
        {
            if (r == null) continue;
            _rendererColorId[r] = GetColorPropertyFor(r);
        }
    }

    private int GetColorPropertyFor(Renderer r)
    {
        if (r == null) return BaseColorId;
        if (_rendererColorId.TryGetValue(r, out var cached)) return cached;
        var sm = r.sharedMaterial;
        if (sm == null) return BaseColorId;
        if (sm.HasProperty(BaseColorId)) return BaseColorId;
        if (sm.HasProperty(LegacyColorId)) return LegacyColorId;
        return BaseColorId;
    }

    private void CaptureAliveColors()
    {
        _rendererAliveColor.Clear();
        if (targetRenderers == null) return;
        foreach (var r in targetRenderers)
        {
            if (r == null) continue;
            var sm = r.sharedMaterial;
            if (sm == null) continue;
            int colorId = GetColorPropertyFor(r);
            var alive = sm.HasProperty(colorId) ? sm.GetColor(colorId) : Color.white;
            _rendererAliveColor[r] = alive;
        }
    }

    private static Renderer[] FindModelRenderers(GameObject root)
    {
        if (root == null) return null;
        // Only include MeshRenderer and SkinnedMeshRenderer by default
        var all = root.GetComponentsInChildren<Renderer>(true);
        var list = new List<Renderer>(all.Length);
        foreach (var r in all)
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
            {
                list.Add(r);
            }
        }
        return list.ToArray();
    }

}