using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class ControllerCastbar : MonoBehaviour
{
    public Slider slider;
    public RectTransform sliderBackground;
    public RectTransform sliderFill;
    public RectTransform sliderBorder;
    private void Awake()
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        HideCastbar();
        GetComponentInParent<UnitActionState>().OnActionStateChanged += HandleOnActionStateChanged;
    }

    private void HandleOnActionStateChanged(UnitActionState unitActionState)
    {
        if (!gameObject.activeInHierarchy) return;

        StartCoroutine(ChangeCastbar(unitActionState.state));
    }

    private IEnumerator ChangeCastbar(UnitActionState.ActionStateData actionStateData)
    {
        slider.value = 0f;
        ShowCastbar();

        var startTime = actionStateData.startTime;
        var endTime = startTime + actionStateData.duration;
        var currentTime = NetworkTime.time;
    
        while (currentTime < endTime)
        {
            slider.value = (float)((currentTime - startTime) / actionStateData.duration);
            yield return null;
            currentTime = NetworkTime.time;
        }

        HideCastbar();
    }

    private void HideCastbar()
    {
        sliderBackground.gameObject.SetActive(false);
        sliderFill.gameObject.SetActive(false);
        sliderBorder.gameObject.SetActive(false);
    }

    private void ShowCastbar()
    {
        sliderBackground.gameObject.SetActive(true);
        sliderFill.gameObject.SetActive(true);
        sliderBorder.gameObject.SetActive(true);
    }
}
