using ShadowInfection;
using ShadowInfection.DI;
using UnityEngine;
using UnityEngine.UI;

public class UnitUiController : MonoBehaviour
{
    public GameObject FloorCircle;
    public Image FloorCircleImage;

    UnitController _unitController;

    void Start()
    {
        _unitController = GetComponentInParent<UnitController>();
        _unitController.OnDied += HandleOnDied;
        _unitController.OnRevive += HandleOnRevive;
        _unitController.OnTeamChanged += HandleOnTeamChanged;

        if (_unitController.health == 0)
            DisableFloorCircle();

        SetFloorCircleColorFromTeam(_unitController.team);
    }

    void OnDestroy()
    {
        if (_unitController != null)
        {
            _unitController.OnDied -= HandleOnDied;
            _unitController.OnRevive -= HandleOnRevive;
            _unitController.OnTeamChanged -= HandleOnTeamChanged;
        }
    }

    private void HandleOnDied()
    {
        DisableFloorCircle();
    }

    private void HandleOnRevive()
    {
        EnableFloorCircle();
    }

    public void EnableFloorCircle()
    {
        FloorCircle.SetActive(true);
    }

    public void DisableFloorCircle()
    {
        FloorCircle.SetActive(false);
    }

    public void SetFloorCircleColor(Color color)
    {
        color.a = 0.125f;
        var isPlayer = _unitController.unitType == UnitType.Player;
        if (isPlayer)
            color.a = 0.25f;

        if (FloorCircleImage != null)
            FloorCircleImage.color = color;
        else
            Debug.LogWarning("[UnitUiController] SetFloorCircleColor: FloorCircleImage is null.");
    }

    public void SetFloorCircleColorFromTeam(int teamId)
    {
        if (!GameLifetimeScope.TryResolve<ITeamColorService>(out var teamColors))
            return;

        SetFloorCircleColor(teamColors.GetColorForTeam(teamId));
    }

    public void HandleOnTeamChanged(UnitController controller)
    {
        SetFloorCircleColorFromTeam(controller.team);
    }
}
