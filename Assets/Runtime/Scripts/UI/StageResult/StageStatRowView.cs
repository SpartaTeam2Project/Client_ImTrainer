using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 왼쪽 스탯 한 줄. 아이콘, 이름, 값.
/// </summary>
public class StageStatRowView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private TMP_Text _value;

    public void Bind(Sprite icon, string label, string value)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        if (_label != null)
        {
            _label.text = label;
        }

        if (_value != null)
        {
            _value.text = value;
        }
    }
}
