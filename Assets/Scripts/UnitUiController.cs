using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitUiController : MonoBehaviour
{
    public GameObject HealthAndShieldBar;
    public GameObject Healthbar;
    public GameObject Shieldbar;
    public GameObject BuffBar;
    public GameObject FloorCircle;
    public Image FloorCircleImage;
    public TextMeshProUGUI nameTag;
    UnitController _unitController;
    // Start is called before the first frame update
    void Start()
    {
        _unitController = GetComponentInParent<UnitController>();
        _unitController.OnDied += HandleOnDied;
        _unitController.OnRevive += HandleOnRevive;
        _unitController.OnShieldChange += HandleOnShieldChange;
        _unitController.OnHealthChange += HandleOnHealthChange;
        _unitController.OnNameChanged += HandleOnNameChanged;
        _unitController.OnTeamChanged += HandleOnTeamChanged;
        InitUiBars();
        SetNameTag(_unitController.unitName);
    }
    void OnDestroy()
    {
        if (_unitController != null)
        {
            _unitController.OnDied -= HandleOnDied;
            _unitController.OnRevive -= HandleOnRevive;
            _unitController.OnShieldChange -= HandleOnShieldChange;
            _unitController.OnHealthChange -= HandleOnHealthChange;
            _unitController.OnNameChanged -= HandleOnNameChanged;
            _unitController.OnTeamChanged -= HandleOnTeamChanged;
        }
    }

    private void HandleOnHealthChange((int current, int max) health)
    {
        if (health.max == 0)
        {
            DisableHealthbar();
            return;
        }

        if (health.current < health.max)
        {
            EnableHealthbar();
            if (_unitController.maxShield > 0)
            {
                EnableShieldbar();
            }
            return;
        }

        if (health.current == health.max)
        {
            var isPlayer = _unitController.unitType == UnitType.Player;
            if (!isPlayer)
            {
                DisableHealthbar();
            }
        }
    }

    void InitUiBars()
    {
        if (_unitController.maxHealth == 0)
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
        SetFloorCircleColorFromTeam(_unitController.team);

        var isPlayer = _unitController.unitType == UnitType.Player;
        var isFullHealth = _unitController.health == _unitController.maxHealth;
        var isFullShield = _unitController.shield == _unitController.maxShield;

        if (!isPlayer && isFullHealth && isFullShield)
        {
            DisableHealthbar();
            DisableShieldbar();
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
            return;
        }

        if (shield.current < shield.max)
        {
            EnableShieldbar();
            if (_unitController.maxHealth > 0)
            {
                EnableHealthbar();
            }
            return;
        }

        if (shield.current == shield.max)
        {
            var isPlayer = _unitController.unitType == UnitType.Player;
            if (!isPlayer)
            {
                DisableShieldbar();
            }
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
        EnableNameTag();
    }

    public void DisableHealthbar()
    {
        Healthbar.SetActive(false);
        DisableNameTag();
    }

    public void EnableShieldbar()
    {
        Shieldbar.SetActive(true);
    }

    public void DisableShieldbar()
    {
        Shieldbar.SetActive(false);
    }

    public void EnableNameTag()
    {
        nameTag.gameObject.SetActive(true);
    }

    public void DisableNameTag()
    {
        nameTag.gameObject.SetActive(false);
    }

    public void EnableBuffBar()
    {
        BuffBar.SetActive(true);
    }

    public void DisableBuffBar()
    {
        BuffBar.SetActive(false);
    }

    public void SetFloorCircleColor(Color color)
    {
        color.a = 0.125f; // Set alpha to 12.5%
        var isPlayer = _unitController.unitType == UnitType.Player;
        if (isPlayer)
        {
            color.a = 0.25f; // Set alpha to 25% for player units
        }

        if (FloorCircleImage != null)
        {
            FloorCircleImage.color = color;
        }
        else
        {
            Debug.LogWarning("[UnitUiController] SetFloorCircleColor: FloorCircleImage is null.");
        }
    }
    public void SetFloorCircleColorFromTeam(int teamId)
    {
        Color color = TeamColorManager.Instance.GetColorForTeam(teamId);
        SetFloorCircleColor(color);
    }

    public void SetNameTag(string name)
    {
        nameTag.text = name;
    }
    public void HandleOnNameChanged(UnitController controller)
    {
        SetNameTag(controller.unitName);
    }
    
    public void HandleOnTeamChanged(UnitController controller)
    {
        SetFloorCircleColorFromTeam(controller.team);
    }
}
