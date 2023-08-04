using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControllerShield : MonoBehaviour
{
    public Slider slider;
    public float updateSpeedSeconds = 0.2f;
    private void Awake()
    {
        GetComponentInParent<UnitController>().OnShieldChange += HandleOnShieldChange;
    }
    private void HandleOnShieldChange((int current, int max) shield)
    {
        slider.maxValue = shield.max;
        
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(ChangeShield(shield.current));
    }

    private IEnumerator ChangeShield(int shield)
    {
        float preChangeShield = slider.value;
        float elapsed = 0f;
        while (elapsed < updateSpeedSeconds)
        {
            elapsed += Time.deltaTime;
            slider.value = Mathf.Lerp(preChangeShield, shield, elapsed / updateSpeedSeconds);
            yield return null;
        }

        slider.value = shield;
    }
}
