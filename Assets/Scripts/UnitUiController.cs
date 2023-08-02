using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitUiController : MonoBehaviour
{
    public GameObject HealthAndShieldBar;
    public GameObject FloorCircle;
    UnitController _unitController;
    // Start is called before the first frame update
    void Start()
    {
        _unitController = GetComponentInParent<UnitController>();
        _unitController.OnDied += HandleOnDied;
        _unitController.OnRevive += HandleOnRevive;
    }

    private void HandleOnDied()
    {
        DisableGuiElements();
    }

    private void HandleOnRevive()
    {
        EnableGuiElements();
    }

    public void EnableGuiElements()
    {
        HealthAndShieldBar.SetActive(true);
        FloorCircle.SetActive(true);
    }

    public void DisableGuiElements()
    {
        HealthAndShieldBar.SetActive(false);
        FloorCircle.SetActive(false);
    }
}
