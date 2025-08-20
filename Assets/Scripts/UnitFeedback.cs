using System.Collections;
using MyGame.Events;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class UnitFeedback : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private float flashDuration = 0.2f;

    private Material mat;
    private Coroutine flashRoutine;
    private UnitController unitController;

    [ColorUsage(true, true)]
    public Color ColorOnDamaged = Color.white;
    [ColorUsage(true, true)]
    public Color ColorOnHealed = Color.green;
    [ColorUsage(true, true)]
    public Color ColorOnMyUnitDamaged = Color.red;
    public bool IsMyUnit = false;

    private MaterialPropertyBlock _mpb;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        // prefer TryGetComponent to avoid exceptions
        TryGetComponent<UnitController>(out unitController);

        // Only fall back to finding a renderer if the inspector field is empty
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer != null)
        {
            // accessing .material creates an instance; keep reference so we can destroy it later
            mat = targetRenderer.material;
        }

        SetUnitOwnership();
    }

    void OnEnable()
    {
        if (unitController != null)
        {
            unitController.OnModelChange += HandleOnModelChange;
            unitController.OnTakeDamage += HandleOnTakeDamage;
            unitController.OnHealed += HandleOnHeal;
        }

        if (EventManager.Instance != null)
            EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
    }

    void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (unitController != null)
        {
            unitController.OnModelChange -= HandleOnModelChange;
            unitController.OnTakeDamage -= HandleOnTakeDamage;
            unitController.OnHealed -= HandleOnHeal;
        }

        if (EventManager.Instance != null)
            EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);

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

        mat = targetRenderer.material;
    }

    void HandleOnTakeDamage(UnitController unitController)
    {
        FlashDamage();
    }

    void HandleOnHeal(UnitController unitController)
    {
        FlashHeal();
    }

    public void FlashDamage()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashSmooth(IsMyUnit ? ColorOnMyUnitDamaged : ColorOnDamaged));
    }

    public void FlashHeal()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashSmooth(ColorOnHealed));
    }

    private IEnumerator FlashSmooth(Color flashColor)
    {
        if (targetRenderer == null) yield break;

        // Ensure the emission keyword is on (for built-in/URP)
        if (mat != null) mat.EnableKeyword("_EMISSION");

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, flashDuration);
            Color emissionColor = Color.Lerp(flashColor, Color.black, t);

            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionColorId, emissionColor);
            targetRenderer.SetPropertyBlock(_mpb);

            yield return null;
        }

        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(EmissionColorId, Color.black);
        targetRenderer.SetPropertyBlock(_mpb);
        flashRoutine = null;
    }

    private void SetUnitOwnership()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in players)
        {
            if (pc == null) continue;
            if (pc.isLocalPlayer && pc.Unit != null)
            {
                var myUnit = pc.Unit.GetComponent<UnitController>();
                if (myUnit == unitController)
                {
                    IsMyUnit = true;
                    break;
                }
            }
        }
    }

    public void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        if (myPlayerUnitSpawnedEvent == null) return;
        IsMyUnit = myPlayerUnitSpawnedEvent.PlayerCharacter == unitController;
    }

}