using System.Collections;
using MyGame.Events;
using UnityEngine;

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

    void Awake()
    {
        unitController = GetComponent<UnitController>();
        unitController.OnModelChange += HandleOnModelChange;
        unitController.OnTakeDamage += HandleOnTakeDamage;
        unitController.OnHealed += HandleOnHeal;
        EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
        targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer != null)
        {
            mat = targetRenderer.material;
        }
        SetUnitOwnership();
    }

    void OnDestroy()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        unitController.OnModelChange -= HandleOnModelChange;
        unitController.OnTakeDamage -= HandleOnTakeDamage;
        unitController.OnHealed -= HandleOnHeal;
        EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
    }

    void HandleOnModelChange((UnitController unitController, GameObject modelInstance) obj)
    {
        Debug.Log(obj);
        targetRenderer = obj.modelInstance.GetComponentInChildren<Renderer>();
        mat = targetRenderer.material;
    }

    void HandleOnTakeDamage(UnitController unitController)
    {
        if (mat == null) return;
        FlashDamage();
    }

    void HandleOnHeal(UnitController unitController)
    {
        if (mat == null) return;
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
        mat.EnableKeyword("_EMISSION");

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / flashDuration;
            Color emissionColor = Color.Lerp(flashColor, Color.black, t);
            mat.SetColor("_EmissionColor", emissionColor);
            yield return null;
        }

        mat.SetColor("_EmissionColor", Color.black);
        flashRoutine = null;
    }

    private void SetUnitOwnership()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in players)
        {
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
        IsMyUnit = myPlayerUnitSpawnedEvent.PlayerCharacter == unitController;
    }

}