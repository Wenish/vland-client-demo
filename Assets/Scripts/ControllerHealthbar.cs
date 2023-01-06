using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ControllerHealthbar : MonoBehaviour
{
    public Slider slider;
    public float updateSpeedSeconds = 0.2f;
    private void Awake()
    {
        GetComponentInParent<UnitController>().OnHealthChange += HandleOnHealthChange;
    }
    private void HandleOnHealthChange((int current, int max) health)
    {
        slider.maxValue = health.max;
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
