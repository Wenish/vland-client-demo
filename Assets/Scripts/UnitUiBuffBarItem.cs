using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitUiBuffBarItem : MonoBehaviour
{
    [Header("References")]
    public Image Icon;
    public TextMeshProUGUI TimeRemainingText;
    public TextMeshProUGUI StackCountText;

    public UiBuffData buffData { get; private set; }

    public void SetBuffData(UiBuffData data)
    {
        buffData = data;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (buffData == null) return;

        if (buffData.IconTexture != null)
        {
            if (Icon.sprite == null || Icon.sprite.texture != buffData.IconTexture)
            {
                Icon.sprite = Sprite.Create(buffData.IconTexture, new Rect(0, 0, buffData.IconTexture.width, buffData.IconTexture.height), new Vector2(0.5f, 0.5f));
            }
        }

        if (buffData.Duration < Mathf.Infinity)
        {
            TimeRemainingText.text = Mathf.CeilToInt(buffData.TimeRemaining).ToString();
            TimeRemainingText.gameObject.SetActive(true);
        }
        else
        {
            TimeRemainingText.gameObject.SetActive(false);
        }

        if (buffData.StackCount > 1)
        {
            StackCountText.text = buffData.StackCount.ToString();
            StackCountText.gameObject.SetActive(true);
        }
        else
        {
            StackCountText.gameObject.SetActive(false);
        }
    }
}