using System.Collections;
using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class UnitFeedback : MonoBehaviour
{
    // Supports multiple child renderers (MeshRenderer/SkinnedMeshRenderer)
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private float flashDuration = 0.2f;

    // We instantiate materials to toggle emission keyword safely per renderer/material
    private readonly List<Material> _instancedMats = new List<Material>();
    private Coroutine flashRoutine;
    private UnitController unitController;

    [ColorUsage(true, true)]
    public Color ColorOnDamaged = Color.white;
    [ColorUsage(true, true)]
    public Color ColorOnHealed = Color.green;
    [ColorUsage(true, true)]
    public Color ColorOnShielded = Color.cyan;
    [ColorUsage(true, true)]
    public Color ColorOnMyPlayerUnitDamaged = Color.red;
    public bool IsMyPlayerUnit = false;
    public UnitController MyPlayerUnitController;

    private MaterialPropertyBlock _mpb;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        // prefer TryGetComponent to avoid exceptions
        TryGetComponent<UnitController>(out unitController);

        // Find child renderers if none assigned in inspector
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        SetupInstancedMaterials();
    }

    void Start()
    {
        SetUnitOwnership();
    }

    void OnEnable()
    {
        if (unitController != null)
        {
            unitController.OnModelChange += HandleOnModelChange;
            unitController.OnTakeDamage += HandleOnTakeDamage;
            unitController.OnHealed += HandleOnHeal;
            unitController.OnShielded += HandleOnShielded;
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
            unitController.OnShielded -= HandleOnShielded;
        }

        if (EventManager.Instance != null)
            EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);

        // Clean up instanced materials (created via .materials)
        CleanupInstancedMaterials();
    }

    void HandleOnModelChange((UnitController unitController, GameObject modelInstance) obj)
    {
        if (obj.modelInstance == null) return;
        var renderers = obj.modelInstance.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (renderers == null || renderers.Length == 0) return;

        // Assign and (re)setup materials
        targetRenderers = renderers;
        CleanupInstancedMaterials();
        SetupInstancedMaterials();
    }


    void HandleOnTakeDamage((UnitController target, UnitController attacker) obj)
    {
        var isMyPlayerUnitTheAttacker = obj.attacker == MyPlayerUnitController;
        var isMyPlayerUnitTheTarget =  obj.target == MyPlayerUnitController;

        var isMyPlayerUnitInvolved = isMyPlayerUnitTheAttacker || isMyPlayerUnitTheTarget;

        if (!isMyPlayerUnitInvolved) return;

        FlashDamage();
    }

    void HandleOnHeal(UnitController unitController)
    {
        FlashHeal();
    }

    void HandleOnShielded((UnitController caster, int amount) obj)
    {
        FlashShield();
    }

    public void FlashDamage()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashSmooth(IsMyPlayerUnit ? ColorOnMyPlayerUnitDamaged : ColorOnDamaged));
    }

    public void FlashHeal()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashSmooth(ColorOnHealed));
    }

    public void FlashShield()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashSmooth(ColorOnShielded));
    }

    private IEnumerator FlashSmooth(Color flashColor)
    {
        if (targetRenderers == null || targetRenderers.Length == 0) yield break;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, flashDuration);
            Color emissionColor = Color.Lerp(flashColor, Color.black, t);

            // Apply to all renderers
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var r = targetRenderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionColorId, emissionColor);
                r.SetPropertyBlock(_mpb);
            }

            yield return null;
        }

        // Reset to black on all renderers
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionColorId, Color.black);
            r.SetPropertyBlock(_mpb);
        }
        flashRoutine = null;
    }

    private void SetUnitOwnership()
    {
        var players = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (var pc in players)
        {
            if (pc == null) continue;
            if (pc.isLocalPlayer && pc.myUnit != null)
            {
                var myUnit = pc.myUnit.GetComponent<UnitController>();
                MyPlayerUnitController = myUnit;
                if (myUnit == unitController)
                {
                    IsMyPlayerUnit = true;
                    break;
                }
            }
        }
    }

    public void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        if (myPlayerUnitSpawnedEvent == null) return;
        IsMyPlayerUnit = myPlayerUnitSpawnedEvent.PlayerCharacter == unitController;
        MyPlayerUnitController = myPlayerUnitSpawnedEvent.PlayerCharacter;
    }

    private void SetupInstancedMaterials()
    {
        if (targetRenderers == null) return;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;
            // Accessing .materials creates instanced copies per submesh; we keep references to clean up
            var mats = r.materials;
            if (mats == null) continue;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null) continue;
                _instancedMats.Add(mat);
                mat.EnableKeyword("_EMISSION");
            }
        }
    }

    private void CleanupInstancedMaterials()
    {
        for (int i = 0; i < _instancedMats.Count; i++)
        {
            var mat = _instancedMats[i];
            if (mat != null)
            {
                Destroy(mat);
            }
        }
        _instancedMats.Clear();
    }
}