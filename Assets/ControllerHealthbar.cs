using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControllerHealthbar : MonoBehaviour
{
    public Slider slider;
    public float updateSpeedSeconds = 0.2f;
    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
    }
    public void SetHealth(int health)
    {
        StartCoroutine(ChangeHealth(health));
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
