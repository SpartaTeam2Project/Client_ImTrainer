using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 재화 한 칸. 아이콘, 배경, 수량을 보여 준다.
/// </summary>
public class ResourceItem : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _value;

    private void Awake()
    {
        ApplyFont(_value);
    }

    /// <summary>
    /// 아이콘과 수량을 넣는다.
    /// </summary>
    public void Show(Sprite icon, int amount)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _icon.preserveAspect = true;
        }

        if (_value != null)
        {
            _value.text = amount.ToString();
        }
    }

    private static void ApplyFont(TextMeshProUGUI text)
    {
        if (text == null || text.font != null || TMP_Settings.defaultFontAsset == null)
        {
            return;
        }

        text.font = TMP_Settings.defaultFontAsset;
    }
}
