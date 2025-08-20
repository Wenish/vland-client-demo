using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ControllerHealthbar : MonoBehaviour
{
    public Slider slider;
    public Image sliderFill;
    public float updateSpeedSeconds = 0.2f;

    public Color myUnitHealthColor = Color.green;
    private UnitController unitController;
    private void Awake()
    {
        unitController = GetComponentInParent<UnitController>();
        unitController.OnHealthChange += HandleOnHealthChange;
    }
    void Start()
    {
        Color teamColor = TeamColorManager.Instance.GetColorForTeam(unitController.team);
        sliderFill.color = teamColor;

        TryApplyMyPlayerColor();
    }

    void OnEnable()
    {
        slider.value = unitController.maxHealth;
        StartCoroutine(ChangeHealth(unitController.health));
    }

    private void TryApplyMyPlayerColor()
    {
        // Find the local player and check if this unit is theirs
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in players)
        {
            if (pc.isLocalPlayer && pc.Unit != null)
            {
                var myUnit = pc.Unit.GetComponent<UnitController>();
                if (myUnit == unitController)
                {
                    sliderFill.color = myUnitHealthColor;
                    break;
                }
            }
        }
    }
    private void HandleOnHealthChange((int current, int max) health)
    {
        slider.maxValue = health.max;

        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(ChangeHealth(health.current));
    }

    private IEnumerator ChangeHealth(int health)
    {
        float preChangeHealth = slider.value;
        float elapsed = 0f;
        while (elapsed < updateSpeedSeconds)
        {
            elapsed += Time.deltaTime;
            slider.value = Mathf.Lerp(preChangeHealth, health, elapsed / updateSpeedSeconds);
            yield return null;
        }

        slider.value = health;
    }
}
