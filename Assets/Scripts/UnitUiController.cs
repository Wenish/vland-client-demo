using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitUiController : MonoBehaviour
{
    public GameObject HealthAndShieldBar;
    public GameObject Healthbar;
    public GameObject Shieldbar;
    public GameObject FloorCircle;
    UnitController _unitController;
    // Start is called before the first frame update
    void Start()
    {
        _unitController = GetComponentInParent<UnitController>();
        _unitController.OnDied += HandleOnDied;
        _unitController.OnRevive += HandleOnRevive;
        _unitController.OnShieldChange += HandleOnShieldChange;
        InitUiBars();
    }

    void InitUiBars()
    {
        if(_unitController.maxHealth == 0)
        {
            DisableHealthbar();
        }

        if (_unitController.maxShield == 0)
        {
            DisableShieldbar();
        }
        if (_unitController.health == 0)
        {
            DisableGuiElements();
        }
    }

    private void HandleOnDied()
    {
        DisableGuiElements();
    }

    private void HandleOnRevive()
    {
        EnableGuiElements();
    }
    private void HandleOnShieldChange((int current, int max) shield)
    {
        if (shield.max == 0)
        {
            DisableShieldbar();
        } else {
            EnableShieldbar();
        }
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

    public void EnableHealthbar()
    {
        Healthbar.SetActive(true);
    }

    public void DisableHealthbar()
    {
        Healthbar.SetActive(false);
    }

    public void EnableShieldbar()
    {
        Shieldbar.SetActive(true);
    }

    public void DisableShieldbar()
    {
        Shieldbar.SetActive(false);
    }
}
