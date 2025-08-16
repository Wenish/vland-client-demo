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
    public RectTransform sliderIcon;
    private Image sliderIconImage;
    private DatabaseManager databaseManager;
    private void Awake()
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        HideCastbar();
        GetComponentInParent<UnitActionState>().OnActionStateChanged += HandleOnActionStateChanged;
        databaseManager = DatabaseManager.Instance;
        sliderIconImage = sliderIcon.GetComponent<Image>();
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

        switch (actionStateData.type)
        {
            case UnitActionState.ActionType.Attacking:
                SetWeaponIcon(actionStateData.name);
                break;
            case UnitActionState.ActionType.Casting:
                SetSkillIcon(actionStateData.name);
                break;
            case UnitActionState.ActionType.Channeling:
                SetSkillIcon(actionStateData.name);
                break;
            default:
                sliderIcon.gameObject.SetActive(false);
                break;
        }

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

    private void SetWeaponIcon(string weaponName)
    {
        var weaponData = databaseManager.weaponDatabase.GetWeaponByName(weaponName);
        sliderIcon.gameObject.SetActive(true);
        var icon = weaponData.iconTexture != null
            ? Sprite.Create(weaponData.iconTexture, new Rect(0, 0, weaponData.iconTexture.width, weaponData.iconTexture.height), Vector2.zero)
            : null;

        if (icon == null)
        {
            sliderIcon.gameObject.SetActive(false);
            return;
        }

        sliderIconImage.sprite = icon;
    }

    private void SetSkillIcon(string skillName)
    {
        var skillData = databaseManager.skillDatabase.GetSkillByName(skillName);
        Debug.Log($"Setting skill icon for: {skillName}, found: {skillData != null}");
        sliderIcon.gameObject.SetActive(true);
        var icon = skillData.iconTexture != null
            ? Sprite.Create(skillData.iconTexture, new Rect(0, 0, skillData.iconTexture.width, skillData.iconTexture.height), Vector2.zero)
            : null;

        if (icon == null)
        {
            sliderIcon.gameObject.SetActive(false);
            return;
        }

        sliderIconImage.sprite = icon;
    }

    private void HideCastbar()
    {
        sliderBackground.gameObject.SetActive(false);
        sliderFill.gameObject.SetActive(false);
        sliderBorder.gameObject.SetActive(false);
        sliderIcon.gameObject.SetActive(false);
    }

    private void ShowCastbar()
    {
        sliderBackground.gameObject.SetActive(true);
        sliderFill.gameObject.SetActive(true);
        sliderBorder.gameObject.SetActive(true);
        sliderIcon.gameObject.SetActive(true);
    }
}
